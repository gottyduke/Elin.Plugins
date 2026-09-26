using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EModding.Components;

internal class EPipe : EMono
{
    private const string PipeName = @"Elin\Console";
    private readonly List<NamedPipeServerStream> _clients = [];
    private readonly CancellationTokenSource _cts = new();

    private void Awake()
    {
        Listen().Forget();
        Debug.Log($@"#pipe external console opened \\.\pipe\{PipeName}");
    }

    private void OnDestroy()
    {
        _cts.Cancel();

        foreach (var client in _clients) {
            client.Dispose();
        }

        _clients.Clear();
    }

    public void Notify(string msg)
    {
        foreach (var client in _clients) {
            Send(client, msg).Forget();
        }
    }

    private static async UniTaskVoid Send(NamedPipeServerStream client, string msg)
    {
        var data = Encoding.UTF8.GetBytes(msg + "\n");

        try {
            await client.WriteAsync(data, 0, data.Length);
        } catch {
            // noexcept
        }
    }

    private async UniTaskVoid Listen()
    {
        while (!_cts.IsCancellationRequested) {
            var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                4096,
                4096);

            try {
                await server.WaitForConnectionAsync(_cts.Token);
            } catch {
                server.Dispose();
                await UniTask.Delay(1000);
                continue;
            }

            await UniTask.SwitchToMainThread();
            _clients.Add(server);
            Serve(server).Forget();
            Debug.Log("#pipe external console connected");
        }
    }

    private async UniTaskVoid Serve(NamedPipeServerStream client)
    {
        using var reader = new StreamReader(client);

        try {
            while (await reader.ReadLineAsync() is { } line) {
                await UniTask.SwitchToMainThread();

                line = line.Trim();
                if (line.Length == 0) {
                    continue;
                }

                var result = line.EvaluateAsCommand(true);
                if (result.Length > 0) {
                    Send(client, result).Forget();
                }
            }
        } catch {
            // noexcept
        }

        await UniTask.SwitchToMainThread();
        _clients.Remove(client);
        Debug.Log("#pipe external console disconnected");
    }
}