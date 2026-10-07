using TextureExpand.Textures;

namespace TextureExpand.Helper;

internal static class ActorSprite
{
    internal static void Reload(CharaRenderer renderer)
    {
        if (!renderer.hasActor || renderer.actor is not CharaActor { isPCC: false } actor || !actor.sr) {
            return;
        }

        var sprite = renderer.owner.GetSprite();
        if (!sprite) {
            return;
        }

        actor.sr.sprite = sprite;
        actor.mpb?.SetTexture(RenderDataExpander.MainTex, sprite.texture);
        actor.RefreshSprite();
    }
}