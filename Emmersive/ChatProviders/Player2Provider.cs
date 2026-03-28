using System;
using System.Collections.Generic;
using System.Net.Http;
using Cwl.Helper.Unity;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace Emmersive.ChatProviders;

public class Player2Provider() : OpenAIProvider("")
{
    public const string ElinGameClientId = "019d3468-2e95-7c1f-afa2-e3cd0fab3a88";

    private DateTime _lastPing = DateTime.MinValue;

    [JsonProperty]
    public override string Alias { get; set; } = "Player2";

    [JsonProperty]
    public override string CurrentModel { get; set; } = "player-2-app";

    [JsonProperty]
    public override string EndPoint => "http://127.0.0.1:4315/v1";

    public override void MergeExtensionRequest(IDictionary<string, object> data, HttpRequestMessage request)
    {
        base.MergeExtensionRequest(data, request);
        request.Headers.Add("player2-game-key", ElinGameClientId);
    }

    protected override void HandleRequestActivity(ChatMessageContent response, EmActivity activity)
    {
        base.HandleRequestActivity(response, activity);

        var elapsed = DateTime.UtcNow - _lastPing;
        if (elapsed.Seconds < 60) {
            return;
        }

        PingAsync().ForgetEx();
        _lastPing = DateTime.UtcNow;

        return;

        async UniTask PingAsync()
        {
            var req = UnityWebRequest.Get($"{EndPoint}/health");
            req.SetRequestHeader("player2-game-key", ElinGameClientId);
            await req.SendWebRequest();
        }
    }
}