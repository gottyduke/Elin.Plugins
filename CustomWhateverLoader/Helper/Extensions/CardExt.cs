using System;
using System.Collections.Generic;
using Cwl.Helper.String;
using Cwl.Helper.Unity;

namespace Cwl.Helper.Extensions;

public static class CardExt
{
    extension(Card owner)
    {
        public CardDir CardDir => (CardDir)owner.dir;
        public string HashKey => $"{owner.id}/{owner.uid}";

        /// <summary>
        ///     Get a flag value from character, 0 if not set
        /// </summary>
        public int GetFlagValue(string flag)
        {
            var key = flag.GetHashCode();
            var value = owner.mapInt.GetValueOrDefault(key);

            if (EClass.core.IsGameStarted && owner.IsPC) {
                EClass.player.dialogFlags.TryAdd(flag, 0);
                value = EClass.player.dialogFlags.GetValueOrDefault(flag, 0);
            }

            return value;
        }

        /// <summary>
        ///     Set a flag value on character
        /// </summary>
        public void SetFlagValue(string flag, int value = 1)
        {
            var key = flag.GetHashCode();
            owner.mapInt[key] = value;

            if (EClass.core.IsGameStarted && owner.IsPC) {
                EClass.player.dialogFlags[flag] = value;
            }
        }

        /// <summary>
        ///     Refresh the sprite renderer if character has one active
        /// </summary>
        public void RefreshSpriteRenderer()
        {
            var actor = owner.renderer?.actor;
            if (actor?.sr == null) {
                return;
            }

            actor.sr.sprite = owner.GetSprite();
            if (actor.sr.sprite == null || actor.mpb is null) {
                return;
            }

            actor.mpb.SetTexture(SpriteCreator.MainTex, actor.sr.sprite.texture);
            actor.RefreshSprite();
        }

        /// <summary>
        ///     Set a sprite override for this character, or reset with null
        /// </summary>
        public void SetSpriteOverride(string? spriteId)
        {
            if (spriteId.IsEmptyOrNull) {
                owner.mapStr.Remove("sprite_override");
            } else {
                owner.mapStr.Set("sprite_override", spriteId);
            }

            owner.RefreshSpriteRenderer();
        }

        public IEnumerable<Thing> FindAllThings(Func<Thing, bool> predicate)
        {
            if (owner.things is null) {
                yield break;
            }

            foreach (var thing in owner.things) {
                if (predicate(thing)) {
                    yield return thing;
                }

                if (!thing.CanSearchContents) {
                    continue;
                }

                foreach (var subThing in thing.FindAllThings(predicate)) {
                    yield return subThing;
                }
            }
        }

        public IEnumerable<Thing> FindAllThings(string id)
        {
            return owner.FindAllThings(t => string.Equals(t.id, id, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}