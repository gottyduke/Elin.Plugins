using System;
using System.Collections.Generic;
using System.Linq;
using ACS.Components;

namespace ACS.API;

public static class AcsAnimator
{
    extension(Card owner)
    {
        /// <summary>
        ///     Play a clip with name.
        /// </summary>
        public void StartAcsClip(string clipName)
        {
            if (!owner.renderer.hasActor || owner.sourceCard.replacer?.data?.sprites?.Length is null or 0) {
                AcsMod.Warn($"failed to play clip for {owner.id}: target has no custom actor");
                return;
            }

            var clip = owner.GetAcsClips(clipName).RandomItem();
            if (clip is null) {
                AcsMod.Warn($"failed to play clip for {owner.id}: target has no clip name {clipName}");
                return;
            }

            owner.GetAcsController()?.StartClip(clip, true);
        }

        /// <summary>
        ///     Play a clip with name.
        /// </summary>
        public void StartAcsClip(AcsAnimationType clipType)
        {
            if (!owner.renderer.hasActor || owner.sourceCard.replacer?.data?.sprites?.Length is null or 0) {
                AcsMod.Warn($"failed to play clip for {owner.id}: target has no custom actor");
                return;
            }

            var clip = owner.GetAcsClips(clipType).RandomItem();
            if (clip is null) {
                AcsMod.Warn(
                    $"failed to play clip for {owner.id}: target has no clip type {Enum.GetName(typeof(AcsAnimationType), clipType)}");
                return;
            }

            owner.GetAcsController()?.StartClip(clip, true);
        }

        /// <summary>
        ///     Play a clip.
        /// </summary>
        public void StartAcsClip(AcsClip clip)
        {
            if (!owner.renderer.hasActor || owner.sourceCard.replacer?.data?.sprites?.Length is null or 0) {
                AcsMod.Warn($"failed to play clip for {owner.id}: target has no custom actor");
                return;
            }

            owner.GetAcsController()?.StartClip(clip, true);
        }

        /// <summary>
        ///     Stop animation on target.
        /// </summary>
        public void StopAcsClip()
        {
            owner.GetAcsController()?.StopClip();
        }

        /// <summary>
        ///     Get first clip with name.
        /// </summary>
        public AcsClip? GetAcsClip(string clipName)
        {
            return owner.GetAcsClips(clipName).FirstOrDefault();
        }

        /// <summary>
        ///     Get first clip with type.
        /// </summary>
        public AcsClip? GetAcsClip(AcsAnimationType type)
        {
            return owner.GetAcsClips(type).FirstOrDefault();
        }

        /// <summary>
        ///     Get all clips with the same clip name.
        /// </summary>
        public IEnumerable<AcsClip> GetAcsClips(string clipName)
        {
            var clips = owner.GetAllAcsClips();
            return clips.Where(c => c.name == clipName);
        }

        /// <summary>
        ///     Get all clips with the animation type.
        /// </summary>
        public IEnumerable<AcsClip> GetAcsClips(AcsAnimationType type)
        {
            var clips = owner.GetAllAcsClips();
            return clips.Where(c => c.type == type);
        }

        /// <summary>
        ///     Get all clips.
        /// </summary>
        public IEnumerable<AcsClip> GetAllAcsClips()
        {
            return AcsController.Clips.GetValueOrDefault(owner.id, []);
        }

        /// <summary>
        ///     Get the controller
        /// </summary>
        public AcsController? GetAcsController()
        {
            return owner.renderer.actor?.GetOrCreate<AcsController>();
        }
    }
}