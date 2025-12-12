using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cwl.Components;

internal class CwlPipe : EMono
{
    private const string PipeName = @"Elin\Console";
    private static readonly CancellationTokenSource _cts = new();
    private readonly List<NamedPipeServerStream> _activeServers = [];

    private readonly ConcurrentQueue<string> _commands = new();
    private readonly List<UniTask> _connections = [];

    private void Awake()
    {
        StartConsoleServer().Forget();
        StartCoroutine(ProcessCommands());
        CwlMod.Log<CwlPipe>($@"external console opened \\.\pipe\{PipeName}");
    }

    private void OnApplicationQuit()
    {
        _cts.Cancel();

        lock (_connections) {
            UniTask.WhenAll(_connections).Forget();
        }
    }

    public async UniTaskVoid Notify(NamedPipeServerStream server, string msg)
    {
        if (server is not { IsConnected: true }) {
            return;
        }

        var data = Encoding.UTF8.GetBytes(msg + "\n");

        try {
            await server.WriteAsync(data, 0, data.Length, _cts.Token);
            await server.FlushAsync(_cts.Token);
        } catch {
            // noexcept
        }
    }

    public void Notify(string msg)
    {
        List<NamedPipeServerStream> snapshot;

        lock (_connections) {
            snapshot = _activeServers.ToList();
        }

        foreach (var server in snapshot) {
            Notify(server, msg).Forget();
        }
    }

    private IEnumerator ProcessCommands()
    {
        var wait = new WaitForSeconds(0.2f);

        while (!_cts.IsCancellationRequested) {
            while (_commands.TryDequeue(out var cmd)) {
                cmd.ExecuteAsCommand(true);
            }

            yield return wait;
        }
    }

    private async UniTaskVoid StartConsoleServer()
    {
        while (!_cts.IsCancellationRequested) {
            var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try {
                await server.WaitForConnectionAsync(_cts.Token);
            } catch {
                await DisposeServer(server);
                continue;
                // noexcept
            }

            if (_cts.IsCancellationRequested) {
                await DisposeServer(server);
                break;
            }

            lock (_connections) {
                _activeServers.Add(server);
                _connections.Add(HandleConnection(server));
            }

            CwlMod.Log<CwlPipe>("external console connected");
        }
    }

    private static async UniTask DisposeServer(NamedPipeServerStream server)
    {
        try {
            await server.DisposeAsync();
        } catch {
            // noexcept
        }
    }

    private async UniTask HandleConnection(NamedPipeServerStream server)
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
                    if (!line.IsEmptyOrNull) {
                        _commands.Enqueue(line);
                    }

                    current = current[(newline + 1)..];
                }

                sb.Clear();
                sb.Append(current);
            }
        } catch {
            // noexcept
        } finally {
            lock (_connections) {
                _activeServers.Remove(server);
            }

            await DisposeServer(server);
        }
    }
}