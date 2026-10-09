// ReSharper disable InconsistentNaming

namespace Emmersive.API.Plugins;

public partial class SceneDirector
{
    public enum CharacterEmote
    {
        Angry = Emo.angry,
        Sad = Emo.sad,
        Hungry = Emo.hungry,
        Love = Emo.love,
        Happy = Emo.happy,
        Idea = Emo.idea,
    }

    public void DoEmote(int uid, CharacterEmote emote, float delay = 0f)
    {
        if (!FindSameMapChara(uid, out var chara)) {
            return;
        }

        CoroutineHelper.Deferred(() => {
            if (chara is { isDestroyed: false, ExistsOnMap: true }) {
                chara.ShowEmo((Emo)emote);
            }
        }, delay);

        EmMod.Debug<SceneDirector>($"{chara.Name} emotes (delay: {delay}): {emote}");
    }
}