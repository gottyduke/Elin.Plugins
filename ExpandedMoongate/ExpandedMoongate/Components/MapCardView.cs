using System;
using System.Net;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Exm.Helper;
using Exm.Model.Map;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Exm.Components;

public class MapCardView(MapMeta meta)
{
    private UIText? _author;
    private UIItem? _bg;
    private UIText? _date;
    private YKVertical? _detailGroup;

    private bool _expanded;
    // primary
    private UIItem? _name;
    // secondary
    private UIText? _rating;
    private Rect _refSize = LayerExpandedMoongate.Instance!.Bound;
    private UIText? _visits;

    public void OnLayout(YKLayout layout)
    {
        var card = layout.MakeCard();

        var bannerGroup = card.Horizontal();
        bannerGroup.Layout.childAlignment = TextAnchor.MiddleLeft;
        bannerGroup.Layout.childControlHeight = true;
        bannerGroup.Layout.spacing = 15f;
        bannerGroup.Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildPreview(bannerGroup, bannerGroup.Layout);
        BuildPrimaryStat(bannerGroup);
        BuildSecondaryStat(bannerGroup);
        BuildControlButtons(bannerGroup);
    }

    private void BuildPreview(YKLayout group, Component parent)
    {
        // TODO api v2 load preview key
        var bgSprite = "exm_no_preview".LoadSprite(resizeHeight: 128, resizeWidth: 128);
        var bgSize = (int)(_refSize.width * 0.15f);
        _bg = group.AddImageCard(parent, bgSprite)
            .WithMinWidth(bgSize)
            .WithMinHeight(bgSize)
            .WithWidth(bgSize)
            .WithHeight(bgSize);
        _bg.LayoutElement().flexibleWidth = 0f;
        _bg.image1.preserveAspect = false;
    }

    private void BuildPrimaryStat(YKLayout group)
    {
        var primary = group.Vertical();
        primary.Layout.childForceExpandWidth = false;
        primary.Layout.childAlignment = TextAnchor.MiddleLeft;
        primary.Layout.spacing = 5f;

        var le = primary.LayoutElement();
        le.flexibleWidth = 1f;
        le.minWidth = 0f;

        _name = primary.Header(WebUtility.HtmlDecode(meta.Title));
        _name.text1.fontSize *= 2;
        _name.text1.alignment = TextAnchor.MiddleLeft;

        _author = primary.TextFlavor(WebUtility.HtmlDecode(meta.Author), FontColor.Myth);

        BuildSubStatBar(primary);
    }

    private void BuildSubStatBar(YKLayout group)
    {
        var subStat = group.Horizontal();
        subStat.Layout.childForceExpandWidth = false;
        subStat.Layout.childAlignment = TextAnchor.MiddleLeft;

        var le = subStat.LayoutElement();
        le.preferredWidth = _refSize.width * 0.35f;
        le.flexibleWidth = 0f;

        var visitSprite = UIHelper.FindSprite("Media/Graphics/Icon/icons_48", "icon_toGlobalMap");
        var visit = subStat.AddImageCard(subStat.Layout, visitSprite);

        var size = _refSize.width * 0.055f;

        le = visit.LayoutElement();
        le.preferredHeight = size;
        le.preferredWidth = size;
        le.flexibleWidth = 0f;

        _visits = subStat.Text("exm_ui_visits".Loc(meta.VisitCount));
        _visits.alignment = TextAnchor.MiddleLeft;

        var width = _refSize.width * 0.093f;

        le = _visits.LayoutElement();
        le.preferredWidth = width;
        le.flexibleWidth = 0f;

        var ratingSprite = UIHelper.FindSprite("Media/Graphics/Icon/icons_48 static", "icons_48 static_4");
        var rating = subStat.AddImageCard(subStat.Layout, ratingSprite);

        le = rating.LayoutElement();
        le.preferredHeight = size;
        le.preferredWidth = size;
        le.flexibleWidth = 0f;

        _rating = subStat.Text("exm_ui_likes".Loc(meta.RatingCount));
        _rating.alignment = TextAnchor.MiddleLeft;

        le = _rating.LayoutElement();
        le.preferredWidth = width;
        le.flexibleWidth = 0f;
    }

    private void BuildSecondaryStat(YKLayout group)
    {
        var statGroup = group.Vertical();
        statGroup.Layout.childAlignment = TextAnchor.LowerCenter;
        statGroup.Layout.spacing = 2f;

        var createTime = meta.Date;
        if (DateTime.TryParse(meta.Date, out var date)) {
            createTime = date.ToString("yyyy-MM-dd HH:mm");
        }

        _date = statGroup.TextFlavor(createTime);
        _date.alignment = TextAnchor.LowerRight;

        statGroup.TextFlavor(Version.Get(meta.Version).GetText())
            .alignment = TextAnchor.LowerRight;

        if (meta.MyRating is null) {
            return;
        }

        if (DateTime.TryParse(meta.MyRating.RatedAt, out _)) {
            statGroup.TextFlavor("exm_ui_last_liked", FontColor.Good)
                .alignment = TextAnchor.LowerRight;
        }

        if (DateTime.TryParse(meta.MyRating.VisitedAt, out var visitDate)) {
            statGroup.TextFlavor("exm_ui_last_visit".Loc(visitDate.ToString("yyyy-MM-dd")), FontColor.Great)
                .alignment = TextAnchor.LowerRight;
        }
    }

    private void BuildControlButtons(YKLayout group)
    {
        var btnSize = (int)(_refSize.width * 0.15f);
        var controlGroup = group.Vertical()
            .WithMinWidth(btnSize)
            .WithMinHeight(btnSize)
            .WithWidth(btnSize)
            .WithHeight(btnSize);
        controlGroup.LayoutElement().flexibleWidth = 0f;

        if (_detailGroup == null) {
            _detailGroup = group.transform.parent.GetComponent<YKLayout>().Vertical();
            _detailGroup.Layout.childAlignment = TextAnchor.MiddleCenter;
        }

        BuildDetailGroup();

        _detailGroup.SetActive(false);

        controlGroup.Button("exm_ui_btn_enter".lang(), () => {
            ExmService.MapController.LoadMap(meta);
        });
        controlGroup.Button("exm_ui_btn_expand".lang(), () => {
            _detailGroup.SetActive(!_expanded);
            _expanded = !_expanded;
            Canvas.ForceUpdateCanvases();
            group.transform.parent.RebuildLayout(true);
        });
    }

    private void BuildDetailGroup()
    {
        var copyGroup = _detailGroup!.Horizontal();
        copyGroup.Layout.childAlignment = TextAnchor.MiddleCenter;
        copyGroup.Layout.childForceExpandWidth = true;

        copyGroup.Text("exm_ui_map_view".Loc(meta.ViewId))
            .alignment = TextAnchor.MiddleCenter;
        copyGroup.Button("exm_ui_copy_view".lang(), () => {
            GUIUtility.systemCopyBuffer = meta.ViewId;
        });

        copyGroup.Button("exm_ui_copy_name".lang(), () => {
            GUIUtility.systemCopyBuffer = meta.Title;
        });

        copyGroup.Button("exm_ui_copy_author".lang(), () => {
            GUIUtility.systemCopyBuffer = meta.Author;
        });

        var miscGroup = _detailGroup!.Horizontal();
        miscGroup.Layout.childAlignment = TextAnchor.MiddleCenter;
        miscGroup.Layout.childForceExpandWidth = true;

        miscGroup.Text("exm_ui_file_size".Loc(StringHelper.ToAllocateString(meta.FileSize)));
    }
}