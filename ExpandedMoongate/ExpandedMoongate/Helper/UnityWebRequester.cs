using Cwl.Helper.String;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine.Networking;

namespace EGate.Helper;

public static class UnityWebRequester
{
    extension(UnityWebRequest req)
    {
        public async UniTask SendRequestEx()
        {
            req.SendWebRequest();
            await UniTask.WaitUntil(() => req.isDone);
        }

        public void SetStandardHandler(string contentType)
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", contentType);
            req.SetRequestHeader("x-request-id", SteamUser.GetSteamID().ToString());
            req.SetRequestHeader("x-debugging-key", "EGateDebuggingAuthorKey".EnvVar);
        }
    }
}