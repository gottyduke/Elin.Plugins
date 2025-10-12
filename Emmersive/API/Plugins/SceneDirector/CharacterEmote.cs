using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

// ReSharper disable InconsistentNaming

namespace Emmersive.API.Plugins.SceneDirector;

public partial class SceneDirector
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CharacterEmote
    {
        Angry = Emo.angry,
        Sad = Emo.sad,
        Hungry = Emo.hungry,
        Love = Emo.love,
        Happy = Emo.happy,
        Idea = Emo.idea,
    }

    [KernelFunction("character_emote")]
    [Description("Display a visual emotion or expression (emote) above a character's head.")]
    public void DoEmote([Description("The unique identifier (uid) of the character who will emote.")] int uid,
                        [Description("The specific type of emotion or expression to display.")] CharacterEmote emote,
                        [Description("How long, in seconds, the emote should be displayed. Default is 1 second.")]
                        float duration = 1f,
                        [Description("Delay, in seconds, before executing this action. Use for chaining actions naturally.")]
                        float delay = 0f)
    {
        if (!FindSameMapChara(uid, out var chara)) {
            return;
        }

        DeferAction(() => chara.ShowEmo((Emo)emote, duration), delay);
        EmMod.Debug<SceneDirector>($"{chara.Name} emotes (delay: {delay}): {emote}");
    }
}