using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cwl;

internal class CwlPipe : EMono
{
    private const string PipeName = @"Elin\Console";
    private static readonly CancellationTokenSource _cts = new();

    private readonly ConcurrentQueue<string> _commands = new();
    private readonly List<NamedPipeServerStream> _connections = [];

    private void Awake()
    {
        StartConsoleServer().Forget();
        StartCoroutine(ProcessCommands());
        CwlMod.Log<CwlPipe>($@"Started external console server at \\.\pipe\{PipeName}");
    }

    private void OnApplicationQuit()
    {
        _cts.Cancel();
        CloseAllConnections();
    }

    private void CloseAllConnections()
    {
        lock (_connections) {
            foreach (var connection in _connections.ToArray()) {
                try {
                    connection.Dispose();
                } catch {
                    // noexcept
                }
            }

            _connections.Clear();
        }
    }

    private IEnumerator ProcessCommands()
    {
        var wait = new WaitForSeconds(0.2f);

        while (!_cts.IsCancellationRequested) {
            while (_commands.TryDequeue(out var cmd)) {
                cmd.ExecuteAsCommand();
            }

            yield return wait;
        }
    }

    private async UniTaskVoid StartConsoleServer()
    {
        while (!_cts.IsCancellationRequested) {
            var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.In,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            lock (_connections) {
                _connections.Add(server);
            }

            try {
                await server.WaitForConnectionAsync(_cts.Token);
            } catch {
                // noexcept
                await DisposeServer(server);
                continue;
            }

            if (_cts.IsCancellationRequested) {
                await DisposeServer(server);
                break;
            }

            HandleConnection(server).Forget();
            CwlMod.Log<CwlPipe>("external console connected");
        }
    }

    private async UniTask DisposeServer(NamedPipeServerStream server)
    {
        try {
            await server.DisposeAsync();
        } catch {
            // noexcept
        }

        lock (_connections) {
            _connections.Remove(server);
        }
    }

    private async UniTaskVoid HandleConnection(NamedPipeServerStream server)
    {
        try {
            var buffer = new byte[1024];
            var decoder = Encoding.UTF8.GetDecoder();
            using var sb = StringBuilderPool.Get();

            while (!_cts.IsCancellationRequested && server.IsConnected) {
                int read;
                try {
                    read = await server.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                } catch {
                    break;
                }

                if (read == 0) {
                    continue;
                }

                var charBuf = new char[decoder.GetCharCount(buffer, 0, read)];
                decoder.GetChars(buffer, 0, read, charBuf, 0);
                sb.Append(charBuf);

                var current = sb.ToString();
                int newline;
                while ((newline = current.IndexOf('\n')) >= 0) {
                    var line = current[..newline].TrimEnd('\r');
                    if (!line.IsEmpty()) {
                        _commands.Enqueue(line);
                    }

                    current = current[(newline + 1)..];
                }

                sb.Clear();
                sb.Append(current);
            }
        } finally {
            await DisposeServer(server);
        }
    }
}