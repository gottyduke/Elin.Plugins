using System;
using System.Web;
using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine.Networking;

namespace Cwl.Helper.Unity;

public static class UniTaskWebRequest
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
            var queries = HttpUtility.ParseQueryString("");

            foreach (var (k, v) in query.Tokenize()) {
                if (!v.IsEmptyOrNull) {
                    queries[k] = v;
                }
            }

            ub.Query = queries.ToString();

            req.uri = ub.Uri;

            return req;
        }
    }
}