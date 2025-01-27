using UnityEngine;
using UnityEngine.Networking;

namespace Cwl.Helper.Unity;

public static class AudioClipStream
{
    public static UnityWebRequest GetAudioClip(string uri, AudioType audioType, bool compressed = false, bool stream = true)
    {
        var downloadHandler = new DownloadHandlerAudioClip(uri, audioType);
        downloadHandler.compressed = compressed;
        downloadHandler.streamAudio = stream;
        return new(uri, UnityWebRequest.kHttpVerbGET, downloadHandler, null);
    }
}