using System.Linq;
using ElinTogether.Net;

namespace ElinTogether.Helper;

internal static class RemoteCardHelper
{
    extension(Chara chara)
    {
        public bool IsRemoteChara =>
            NetSession.Instance.Connection switch {
                null => false,
                ElinNetHost  host => host.ActiveRemoteCharas.Values.Contains(chara),
                ElinNetClient  => !chara.IsPC,
                _ => false,
            };
    }
}