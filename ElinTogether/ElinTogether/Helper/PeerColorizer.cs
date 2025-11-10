using Cwl.Helper.String;
using ElinTogether.Net.Steam;
using UnityEngine;

namespace ElinTogether.Helper;

internal static class PeerColorizer
{
    extension(ISteamNetPeer peer)
    {
        internal string Colorize(object input)
        {
            // should be colorblind friendly?
            var color = peer.Id switch {
                1 => 0x0072b2,
                2 => 0xe69f00,
                3 => 0xcc79a7,
                4 => 0x009e73,
                _ => HSVColorInt(peer.Id * 0.618f % 1f, 0.65f, 0.85f),
            };

            return input.TagColor(color);

            static int HSVColorInt(float h, float s, float v)
            {
                var c = Color.HSVToRGB(h, s, v);
                return ((int)(c.r * 255) << 16) | ((int)(c.g * 255) << 8) | (int)(c.b * 255);
            }
        }
    }
}