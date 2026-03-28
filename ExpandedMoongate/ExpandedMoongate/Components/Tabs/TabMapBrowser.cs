using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cysharp.Threading.Tasks;
using Exm.API;
using Exm.API.Exceptions;
using Exm.Model.Map;
using UnityEngine;
using YKF;

namespace Exm.Components.Tabs;

internal class TabMapBrowser : TabExMoongateBase
{
    private static readonly List<string> _sortNames = Enum
        .GetNames(typeof(IMapServiceV1.SortType))
        .Select(n => $"exm_ui_sort_type_{n}".lang())
        .ToList();
    private static readonly List<string> _langFilterNames = Enum
        .GetNames(typeof(IMapServiceV1.LangFilter))
        .Select(n => $"exm_ui_lang_filter_{n}".lang())
        .ToList();

    protected readonly Dictionary<string, MapCardView> _cachedCards = [];
    protected readonly List<MapCardView> _cards = [];
    protected int CurrentPage;
    protected bool DirtyData;
    protected bool DirtyLayout;
    protected MapServiceOverview? Overview;

    protected virtual void Update()
    {
        if (DirtyData) {
            this.DestroyChildren(true);
            RefreshMapData();
        }

        if (DirtyLayout) {
            this.DestroyChildren(true);

            BuildHeaders();
            BuildControlButtons();

            foreach (var card in _cards) {
                card.OnLayout(this);
            }

            DirtyLayout = false;
        }
    }

    public override void OnLayout()
    {
        DirtyData = true;
    }

    public override void OnLayoutConfirm()
    {
    }

    private void BuildHeaders()
    {
        var headerGroup = Horizontal();
        headerGroup.Layout.childAlignment = TextAnchor.MiddleCenter;

        if (Overview is null) {
            headerGroup.Text("exm_ui_null_overview");
            return;
        }

        headerGroup.Spacer(0).LayoutElement().flexibleWidth = 1f;

        headerGroup.Text($"{"exm_ui_maps_total".Loc(Overview.MapsCount)}\n" +
                         $"{"exm_ui_visits_total".Loc(Overview.VisitsCount)}")
            .alignment = TextAnchor.MiddleCenter;

        headerGroup.Spacer(0).LayoutElement().flexibleWidth = 1f;

        headerGroup.Text($"{"exm_ui_maps_today".Loc(Overview.MapsToday)}\n" +
                         $"{"exm_ui_visits_today".Loc(Overview.VisitsToday)}")
            .alignment = TextAnchor.MiddleCenter;

        headerGroup.Spacer(0).LayoutElement().flexibleWidth = 1f;
    }

    private void BuildControlButtons()
    {
        var btnGroup = Horizontal();
        btnGroup.Layout.spacing = 10f;

        BuildSortGroup(btnGroup);
        BuildFilterGroup(btnGroup);

        btnGroup.Spacer(0).LayoutElement().flexibleWidth = 1f;

        BuildPageGroup(btnGroup);
    }

    private void BuildSortGroup(YKLayout group)
    {
        var sortGroup = group.Horizontal();

        sortGroup.Text("exm_ui_sort_type")
            .WithMinWidth(0)
            .alignment = TextAnchor.MiddleLeft;
        sortGroup.Dropdown(_sortNames, SwitchSort, (int)Sort)
            .WithMinWidth(200);

        return;

        void SwitchSort(int sortType)
        {
            Sort = (IMapServiceV1.SortType)sortType;
            CurrentPage = 0;
            DirtyData = true;
        }

    }

    private void BuildFilterGroup(YKLayout group)
    {
        var filterGroup = group.Horizontal();

        filterGroup.Text("exm_ui_lang_filter")
            .WithMinWidth(0)
            .alignment = TextAnchor.MiddleLeft;
        filterGroup.Dropdown(_langFilterNames, SwitchFilter, (int)LangFilter)
            .WithMinWidth(50);
        filterGroup.Toggle("exm_ui_adult_filter", NoAdult, SwitchAdult);

        return;

        void SwitchFilter(int filterType)
        {
            LangFilter = (IMapServiceV1.LangFilter)filterType;
            CurrentPage = 0;
            DirtyData = true;
        }

        void SwitchAdult(bool on)
        {
            NoAdult = on;
            CurrentPage = 0;
            DirtyData = true;
        }
    }

    private void BuildPageGroup(YKLayout group)
    {
        if (Overview is null) {
            return;
        }

        TotalPages = Overview.MapsCount / ExmConfig.Display.MapsPerPage.Value + 1;
        CurrentPage = Mathf.Clamp(CurrentPage, 0, TotalPages);

        var pageGroup = group.Horizontal();
        if (CurrentPage > 0) {
            pageGroup.Button($"{CurrentPage}", () => SwitchPage(CurrentPage - 1));
        }

        var pages = Enumerable
            .Range(1, TotalPages)
            .Select(i => i.ToString())
            .ToList();
        pageGroup.Dropdown(pages, SwitchPage, CurrentPage);

        if (CurrentPage < TotalPages - 1) {
            pageGroup.Button($"{CurrentPage + 2}", () => SwitchPage(CurrentPage + 1));
        }

        return;

        void SwitchPage(int page)
        {
            CurrentPage = page;
            DirtyData = true;
        }
    }

    protected void RefreshMapData()
    {
        _cards.Clear();
        Overview = null;
        DirtyData = false;
        DirtyLayout = false;

        Header("Loading maps...");

        RefreshMapDataAsync()
            .AttachExternalCancellation(this.GetCancellationTokenOnDestroy())
            .ForgetEx();
    }

    protected virtual async UniTask RefreshMapDataAsync()
    {
        try {
            var service = ExmService.MapService;

            var pageSize = ExmConfig.Display.MapsPerPage.Value;
            var adultFilter = NoAdult ? "adult" : null;
            var topMapTask = service.GetTopMapsAsync(Sort, pageSize, CurrentPage, LangFilter, adultFilter)
                .Preserve();
            var overviewTask = service.GetMapsOverviewAsync(LangFilter, adultFilter)
                .Preserve();
            var queryTask = UniTask.WhenAll(overviewTask, topMapTask);

            var timeout = Mathf.Max(ExmConfig.Policy.Timeout.Value, 3f);
            var results = await UniTask.WhenAny(queryTask,
                UniTask.Delay(TimeSpan.FromSeconds(timeout)));

            if (!results.hasResultLeft) {
                throw new TimeoutException("exm_error_service_timeout".lang());
            }

            (Overview, var maps) = results.result;

            if (maps is not { Length: > 0 }) {
                throw new MoongateException("server responds with empty data");
            }

            foreach (var map in maps) {
                if (!map.IsValidVersion) {
                    continue;
                }

                if (!_cachedCards.TryGetValue(map.Id, out var card)) {
                    card = _cachedCards[map.Id] = new(map);
                }

                _cards.Add(card);
            }
        } catch (Exception ex) {
            await UniTask.Yield();
            ExmMod.WarnWithPopup<TabMapBrowser>(ex.Message, ex);
            // noexcept
        } finally {
            await UniTask.Yield();
            DirtyData = false;
            DirtyLayout = true;
        }
    }

    #region Options

    protected IMapServiceV1.SortType Sort = IMapServiceV1.SortType.Created;
    protected IMapServiceV1.LangFilter LangFilter = IMapServiceV1.LangFilter.All;
    protected int TotalPages;
    protected bool NoAdult = EClass.core.config.net.noAdult;

    #endregion
}