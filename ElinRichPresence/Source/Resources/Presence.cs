using DiscordRPC;

namespace Erpc.Resources;

internal static class Presence
{
    internal static string LogString(this RichPresence presence)
    {
        return $"{presence.Details} | {presence.State}\n" +
               $"{presence.Assets.LargeImageKey} | {presence.Assets.LargeImageText}\n" +
               $"{presence.Assets.SmallImageText} | {presence.Assets.SmallImageText}\n";
    }

    // why is it not a record?
    internal static bool Unchanged(this RichPresence lhs, RichPresence? rhs)
    {
        return rhs is not null &&
               lhs.Details == rhs.Details &&
               lhs.State == rhs.State &&
               lhs.Assets?.LargeImageKey == rhs.Assets?.LargeImageKey &&
               lhs.Assets?.LargeImageText == rhs.Assets?.LargeImageText &&
               lhs.Assets?.SmallImageKey == rhs.Assets?.SmallImageKey &&
               lhs.Assets?.SmallImageText == rhs.Assets?.SmallImageText;
    }
}