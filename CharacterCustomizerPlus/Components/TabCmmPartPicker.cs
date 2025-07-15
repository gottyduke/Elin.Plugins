using System;
using System.Collections.Generic;
using CustomizerMinus.API;
using CustomizerMinus.Helper;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Empyrean.Utils;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace CustomizerMinus.Components;

internal class TabCmmPartPicker : YKLayout<LayerCreationData>
{
    private const int CellWidth = 128;
    private const int CellHeight = 196;

    private static GameObject? _prefabCell;
    internal static readonly Dictionary<string, Vector2> BrowsedPositions = [];

    private readonly List<SpriteStateAnimator> _animators = [];
    private readonly HashSet<Outline> _outlines = [];
    private YKGrid? _grid;

    private void Update()
    {
        var provider = Layer.Data.UiPcc.actor.provider;
        foreach (var animator in _animators) {
            // ?! why is there offset 2
            var frame = (provider.currentFrame + 2) % 4;
            animator.SetSprite(provider.currentDir, frame);
        }

        BrowsedPositions[Layer.Data.IdPartsSet] = GetComponentInParent<UIScrollView>().normalizedPosition;
    }

    public override void OnLayout()
    {
        if (_prefabCell == null) {
            return;
        }

        _grid = Grid()
            .WithCellSize(CellWidth, CellHeight)
            .WithConstraintCount(5);

        var data = Layer.Data;
        if (data.IdPartsSet != "body") {
            AddCell(null, data.IdPartsSet);
        }

        var parts = PCC.GetAvailableParts(data.UiPcc.pcc.GetBodySet(), data.IdPartsSet);
        parts.Sort((lhs, rhs) => {
            if (!int.TryParse(lhs.id, out var lhsId)) {
                lhsId = int.MaxValue;
            }

            if (!int.TryParse(rhs.id, out var rhsId)) {
                rhsId = int.MaxValue;
            }

            return lhsId - rhsId;
        });

        foreach (var part in parts) {
            try {
                AddCell(part, data.IdPartsSet);
            } catch (Exception ex) {
                CmmMod.Log($"failed to add cell for {part.id} / {data.IdPartsSet}\n{ex}");
                // noexcept
            }
        }
    }

    private void AddCell(PCC.Part? part, string idPartsSet)
    {
        if (_grid == null) {
            return;
        }

        var btn = Instantiate(_prefabCell, _grid.transform)?.GetComponent<ButtonGeneral>();
        if (btn == null) {
            return;
        }

        var uiPcc = Layer.Data.UiPcc;
        var outline = btn.GetComponent<Outline>();
        if (part is null) {
            btn.icon.sprite = "cmm_null".LoadSprite();

            btn.name += "_null";
            btn.SetTooltipLang("cmm_ui_remove".Loc());
            btn.SetOnClick(() => {
                uiPcc.pcc.data.RemovePart(idPartsSet);
                uiPcc.actor.Reset();
                SetOutline(outline);
            });
        } else {
            var texItem = part.modTextures.TryGetValue("walk");
            if (texItem == null) {
                return;
            }

            btn.name += $"_{part.id}";
            btn.SetTooltipLang($"{part.id}     " +
                               $"<i>{part.GetPartProvider().TagColor(0x4ffff9)}</i>\n" +
                               $"{part.dir.ShortPath()}");
            btn.SetOnClick(() => {
                uiPcc.pcc.data.SetPart(part);
                uiPcc.actor.Reset();
                SetOutline(outline);
            });

            var color = uiPcc.pcc.data.GetColor(idPartsSet).ToHsv();
            color.v = 1f;
            color.a = 1f;
            btn.icon.color = color.ToRGB();

            var animator = btn.icon.gameObject.AddComponent<SpriteStateAnimator>();
            // use CWL LoadSprite to cache the sprite,
            // so that it won't get garbage collected
            animator.SliceSheet(texItem.fileInfo.FullName.LoadSprite()!.texture);
            _animators.Add(animator);
        }

        if (uiPcc.pcc.data.GetPart(idPartsSet) == part) {
            SetOutline(outline);
        }

        btn.SetActive(true);
    }

    private void SetOutline(Outline outline)
    {
        _outlines.Add(outline);
        foreach (var toDisable in _outlines) {
            toDisable.enabled = false;
        }

        outline.enabled = true;
    }

    internal static void InitPrefabCell(ButtonGeneral shared)
    {
        try {
            _prefabCell = Instantiate(shared.gameObject);
            _prefabCell.name = "cmm_cell";

            var image = _prefabCell.GetComponent<Image>();
            image.sprite = shared.GetComponent<Image>().sprite;

            var btn = _prefabCell.GetComponent<ButtonGeneral>();
            btn.icon.rectTransform.sizeDelta = new(CellWidth * 0.8f, CellHeight * 0.8f);
            btn.icon.sprite = null;

            btn.soundClick = SoundManager.current.GetData("click_tab");
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = shared.spriteState;

            var scaler = ELayer.ui.canvasScaler.scaleFactor;
            btn.onClick = new();
            btn.tooltip = new() {
                enable = false,
                offset = new(0f, scaler * -70f),
                id = "cmm_tooltip_cell",
                text = "",
            };

            var outline = _prefabCell.AddComponent<Outline>();
            outline.effectDistance = new(2f, 2f);
            outline.effectColor = Color.cyan;
            outline.enabled = false;

            _prefabCell.SetActive(false);
        } catch (Exception ex) {
            CmmMod.Log($"failed to init prefab cell\n{ex}");
            _prefabCell = null;
            // noexcept
        }
    }
}