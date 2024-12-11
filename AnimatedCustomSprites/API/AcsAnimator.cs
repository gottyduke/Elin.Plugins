using System;
using System.Collections.Generic;
using System.Linq;
using ACS.Components;

namespace ACS.API;

public static class AcsAnimator
{
    /// <summary>
    ///     Play a clip with name.
    /// </summary>
    public static void StartAcsClip(this Card owner, string clipName)
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

        owner.renderer.actor.GetComponent<AcsController>()?.StartClip(clip);
    }

    /// <summary>
    ///     Play a clip with name.
    /// </summary>
    public static void StartAcsClip(this Card owner, AcsAnimationType clipType)
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

        owner.renderer.actor.GetComponent<AcsController>()?.StartClip(clip);
    }

    /// <summary>
    ///     Play a clip.
    /// </summary>
    public static void StartAcsClip(this Card owner, AcsClip clip)
    {
        if (!owner.renderer.hasActor || owner.sourceCard.replacer?.data?.sprites?.Length is null or 0) {
            AcsMod.Warn($"failed to play clip for {owner.id}: target has no custom actor");
            return;
        }

        owner.renderer.actor.GetComponent<AcsController>()?.StartClip(clip);
    }

    /// <summary>
    ///     Stop animation on target.
    /// </summary>
    public static void StopAcsClip(this Card owner)
    {
        owner.renderer.actor?.GetComponent<AcsController>()?.StopClip();
    }

    /// <summary>
    ///     Get first clip with name.
    /// </summary>
    public static AcsClip? GetAcsClip(this Card owner, string clipName)
    {
        return owner.GetAcsClips(clipName).FirstOrDefault();
    }

    /// <summary>
    ///     Get first clip with type.
    /// </summary>
    public static AcsClip? GetAcsClip(this Card owner, AcsAnimationType type)
    {
        return owner.GetAcsClips(type).FirstOrDefault();
    }

    /// <summary>
    ///     Get all clips with the same clip name.
    /// </summary>
    public static IEnumerable<AcsClip> GetAcsClips(this Card owner, string clipName)
    {
        var clips = owner.GetAllAcsClips();
        return clips.Where(c => c.name == clipName);
    }

    /// <summary>
    ///     Get all clips with the animation type.
    /// </summary>
    public static IEnumerable<AcsClip> GetAcsClips(this Card owner, AcsAnimationType type)
    {
        var clips = owner.GetAllAcsClips();
        return clips.Where(c => c.type == type);
    }

    /// <summary>
    ///     Get all clips.
    /// </summary>
    public static IEnumerable<AcsClip> GetAllAcsClips(this Card owner)
    {
        return AcsController.Clips.GetValueOrDefault(owner.id, []);
    }
}