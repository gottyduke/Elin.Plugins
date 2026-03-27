using System;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine.Networking;

namespace Exm.Helper;

public static class UnityWebRequester
{
    extension(UnityWebRequest req)
    {
        public async UniTask SendRequestEx()
        {
            req.SendWebRequest();
            await UniTask.WaitUntil(() => req.isDone);
        }

        public UnityWebRequest SetStandardHandler(string contentType)
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("content-type", contentType);
            req.SetRequestHeader("x-request-id", SteamUser.GetSteamID().ToString());
            req.SetRequestHeader("x-debugging-key", "EGateDebuggingAuthorKey".EnvVar);

            return req;
        }

        public UnityWebRequest SetUploaderBytes(byte[] bytes)
        {
            req.uploadHandler = new UploadHandlerRaw(bytes);
            return req;
        }

        public UnityWebRequest SetParams(object query)
        {
            var ub = new UriBuilder(req.uri);
            using var sb = StringBuilderPool.Get();

            var first = true;
            foreach (var (k, v) in query.Tokenize()) {
                if (v.IsEmptyOrNull) {
                    continue;
                }

                if (!first) {
                    sb.Append("&");
                }

                sb.Append(Uri.EscapeDataString(k));
                sb.Append("=");
                sb.Append(Uri.EscapeDataString(v));

                first = false;
            }

            ub.Query = sb.ToString();

            req.uri = ub.Uri;
            req.url = ub.Uri.ToString();

            return req;
        }
    }
}