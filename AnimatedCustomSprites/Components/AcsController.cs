using System;
using System.Collections.Generic;
using ACS.API;
using UnityEngine;

namespace ACS.Components;

internal class AcsController : MonoBehaviour
{
    internal static readonly Dictionary<string, Sprite> Cached = [];
    internal static readonly Dictionary<string, List<AcsClip>> Clips = [];
    private CardActor? _actor;

    private float _elapsed;
    private bool _playing;
    private SpriteReplacer? _replacer;

    private SpriteRenderer? _sr;

    internal int CurrentIndex { get; private set; }
    internal AcsClip? CurrentClip { get; private set; }
    internal bool ExternalOverride { get; set; }
    internal float Interval => CurrentClip?.interval ?? 0f;
    internal bool IsPlaying => _playing && _replacer?.data is not null;

    private void Awake()
    {
        _sr ??= GetComponent<SpriteRenderer>();
        _actor ??= GetComponent<CardActor>();
        _replacer ??= _actor.owner.sourceCard.replacer;

        if (_replacer?.data?.sprites?.Length is null or 0) {
            _playing = false;
            return;
        }

        _actor.owner.CreateAcsClips(_replacer.data.sprites);

        var idleClip = _actor.owner.GetAcsClip(AcsAnimationType.Idle);
        if (idleClip is null) {
            return;
        }

        StartClip(idleClip);
    }

    private void Update()
    {
        if (CurrentClip?.sprites?.Length is not > 1) {
            _playing = false;
            return;
        }

        if (!_playing || _actor?.owner is null) {
            return;
        }

        if (!ExternalOverride) {
            var chara = _actor.owner as Chara;
            if (chara is null && _actor.owner is Thing { parentCard: Chara wielder }) {
                chara = wielder;
            }

            AcsClip? newClip = null;
            if (chara?.IsInCombat ?? false) {
                newClip = _actor.owner.GetAcsClip(AcsAnimationType.Combat);
            }
            newClip ??= _actor.owner.GetAcsClip(AcsAnimationType.Idle);

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

    internal Sprite CurrentFrame()
    {
        if (!IsPlaying || CurrentClip?.sprites?.Length is not > 1) {
            return _sr!.sprite;
        }

        return CurrentClip.sprites[CurrentIndex];
    }

    internal void StartClip(AcsClip clip)
    {
        StopClip();
        CurrentClip = clip;
        _playing = CurrentClip is not null;
    }

    internal void StopClip()
    {
        _playing = false;
        ResetClip();
    }

    internal void ResetClip()
    {
        CurrentIndex = 0;
        _elapsed = 0f;
    }
}