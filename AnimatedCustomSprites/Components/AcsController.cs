using System.Collections;
using System.Collections.Generic;
using ACS.API;
using UnityEngine;

namespace ACS.Components;

public class AcsController : MonoBehaviour
{
    internal static readonly Dictionary<string, List<AcsClip>> Clips = [];

    private float _elapsed;
    private bool _playing;

    public int CurrentIndex { get; private set; }
    public AcsClip? CurrentClip { get; private set; }
    public bool ExternalOverride { get; set; }
    public float Interval => CurrentClip?.interval ?? 0f;
    public bool IsPlaying => _playing && Replacer.data is not null;

    private CardActor Actor => GetComponent<CardActor>();
    private SpriteReplacer Replacer => Actor.owner.sourceCard.replacer;
    private SpriteRenderer Sr => GetComponent<SpriteRenderer>();

    private void Update()
    {
        if (CurrentClip?.sprites?.Length is not > 0) {
            _playing = false;
            return;
        }

        if (!_playing || Actor.owner is null) {
            return;
        }

        if (!ExternalOverride) {
            var chara = Actor.owner as Chara;
            if (chara is null && Actor.owner is Thing { parentCard: Chara wielder }) {
                chara = wielder;
            }

            AcsClip? newClip = null;
            var inCombat = chara?.IsInCombat ?? false;
            var newClipType = CurrentClip.type;

            var shouldSwitchToCombat = CurrentClip.type != AcsAnimationType.Combat && inCombat;
            var shouldSwitchToIdle = CurrentClip.type == AcsAnimationType.Combat && !inCombat;

            if (shouldSwitchToCombat) {
                newClipType = AcsAnimationType.Combat;
            } else if (shouldSwitchToIdle) {
                newClipType = AcsAnimationType.Idle;
            }

            if (newClipType != CurrentClip.type) {
                newClip = Actor.owner.GetAcsClip(newClipType);
            }

            if (newClip?.sprites?.Length is > 0 && newClip != CurrentClip) {
                StartClip(newClip);
            }
        }

        _elapsed += Time.deltaTime;
        if (_elapsed < Interval) {
            return;
        }

        _elapsed -= Interval;
        CurrentIndex = (CurrentIndex + 1) % CurrentClip.sprites.Length;
    }

    private void OnEnable()
    {
        // pool chara has its card set 1 frame afterward
        StartCoroutine(DelayedValidate());
    }

    private IEnumerator DelayedValidate()
    {
        yield return null;

        if (Replacer.data?.sprites?.Length is not > 1) {
            _playing = false;
            yield break;
        }

        if (!Clips.ContainsKey(Actor.owner.id)) {
            Clips[Actor.owner.id] = Actor.owner.CreateAcsClips(Replacer.data.sprites);
        }

        var idleClip = Actor.owner.GetAcsClip(AcsAnimationType.Idle);
        if (idleClip is null) {
            yield break;
        }

        StartClip(idleClip);
    }

    public Sprite CurrentFrame()
    {
        if (!IsPlaying || CurrentClip?.sprites?.Length is not > 1) {
            return Sr.sprite;
        }

        return CurrentClip.sprites[CurrentIndex];
    }

    public void StartClip(AcsClip clip, bool external = false)
    {
        StopClip();

        CurrentClip = clip;
        _playing = CurrentClip is not null;

        if (_playing) {
            ExternalOverride = external;
        }
    }

    public void StopClip()
    {
        _playing = false;
        ResetClip();
    }

    public void ResetClip()
    {
        CurrentIndex = 0;
        _elapsed = 0f;
    }
}