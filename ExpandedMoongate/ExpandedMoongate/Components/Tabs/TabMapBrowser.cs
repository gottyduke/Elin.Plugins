using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cysharp.Threading.Tasks;
using Exm.API;
using Exm.Helper;
using Exm.Model.Map;
using UnityEngine;
using UnityEngine.UI;
using YKF;

namespace Exm.Components.Tabs;

internal class TabMapBrowser : TabExMoongateBase
{
    protected readonly Dictionary<string, MapCardView> _cachedCards = [];
    protected readonly List<MapCardView> _cards = [];
    private Rect _refSize = LayerExpandedMoongate.Instance!.Bound;
    protected bool DirtyData;
    protected bool DirtyLayout;
    protected MapServiceOverview? Overview;

    private static List<string> SortNames => field ??= Enum
        .GetNames(typeof(IMapServiceV1.SortType))
        .Select(n => $"exm_ui_sort_type_{n}".lang())
        .ToList();

    private static List<string> LangFilterNames => field ??= Enum
        .GetNames(typeof(IMapServiceV1.LangFilter))
        .Select(n => $"exm_ui_lang_filter_{n}".lang())
        .ToList();

    private static List<string> TimePeriodNames => field ??= Enum
        .GetNames(typeof(IMapServiceV1.TimePeriod))
        .Select(n => $"exm_ui_time_period_{n}".lang())
        .ToList();

    private static Array DaysValues => field ??= Enum
        .GetValues(typeof(IMapServiceV1.TimePeriod));

    protected virtual void Update()
    {
        if (DirtyData) {
            this.DestroyChildren(true);
            RefreshMapData();
        }

        if (DirtyLayout) {
            this.DestroyChildren(true);

            BuildHeaderGroup();
            BuildControlButtons();

            if (_cards.Count > 0) {
                foreach (var card in _cards) {
                    card.OnLayout(this);
                }
            } else {
                Header("exm_ui_no_map_data");
            }

            DirtyLayout = false;

            Canvas.ForceUpdateCanvases();
            transform.RebuildLayout(true);
        }
    }

    public override void OnLayout()
    {
        DirtyData = true;
    }

    public override void OnLayoutConfirm()
    {
    }

    private void BuildHeaderGroup()
    {
        var headerGroup = Horizontal();
        BuildServerOverview(headerGroup);
    }

    private void BuildServerOverview(YKLayout group)
    {
        var card = group.MakeCard();

        var overviewGroup = card.Horizontal();
        overviewGroup.Layout.childAlignment = TextAnchor.MiddleCenter;

        overviewGroup.FlexWidth();

        if (SearchMode) {
            overviewGroup.Text($"{"exm_ui_maps_total".Loc(_cards.Count)}")
                .alignment = TextAnchor.MiddleCenter;
            overviewGroup.FlexWidth();
            return;
        }

        if (Overview is null) {
            overviewGroup.Text("exm_ui_null_overview")
                .alignment = TextAnchor.MiddleCenter;
            overviewGroup.FlexWidth();
            return;
        }

        overviewGroup.Text($"{"exm_ui_maps_total".Loc(Overview.MapsCount)}\n" +
                           $"{"exm_ui_visits_total".Loc(Overview.VisitsCount)}")
            .alignment = TextAnchor.MiddleCenter;

        overviewGroup.FlexWidth();

        overviewGroup.Text($"{"exm_ui_maps_today".Loc(Overview.MapsToday)}\n" +
                           $"{"exm_ui_visits_today".Loc(Overview.VisitsToday)}")
            .alignment = TextAnchor.MiddleCenter;

        overviewGroup.FlexWidth();
    }

    private void BuildControlButtons()
    {
        var controlGroup = this.MakeCard();
        controlGroup.Layout.spacing = 10f;

        if (SearchMode) {
            var cancelGroup = controlGroup.Horizontal();

            BuildSearchGroup(cancelGroup);

            cancelGroup.FlexWidth();

            cancelGroup.Button("exm_ui_btn_search_clear".lang(), ClearSearch)
                .WithMinWidth((int)(_refSize.width * 0.25f))
                .GetComponent<Image>().color = Color.red;
        } else {
            BuildFilterGroup(controlGroup);

            var naviGroup = controlGroup.Horizontal();

            BuildSearchGroup(naviGroup);

            naviGroup.FlexWidth();

            BuildPageGroup(naviGroup);
        }

        return;

        void ClearSearch()
        {
            SearchMode = false;
            DirtyData = true;
        }
    }

    private void BuildFilterGroup(YKLayout group)
    {
        var filterGroup = group.Horizontal();
        filterGroup.Layout.spacing = 10f;

        filterGroup.Text("exm_ui_sort_type")
            .WithMinWidth(0)
            .alignment = TextAnchor.MiddleLeft;
        filterGroup.Dropdown(SortNames, SwitchSort, (int)Sort)
            .WithMinWidth((int)(_refSize.width * 0.2f));

        filterGroup.Text("exm_ui_time_period")
            .WithMinWidth(0)
            .alignment = TextAnchor.MiddleLeft;
        filterGroup.Dropdown(TimePeriodNames, SwitchTime, Array.IndexOf(DaysValues, Days))
            .WithMinWidth((int)(_refSize.width * 0.15f));

        filterGroup.Text("exm_ui_lang_filter")
            .WithMinWidth(0)
            .alignment = TextAnchor.MiddleLeft;
        filterGroup.Dropdown(LangFilterNames, SwitchLang, (int)Lang)
            .WithMinWidth((int)(_refSize.width * 0.06f));

        filterGroup.Toggle("exm_ui_adult_filter", NoAdult, SwitchAdult);

        return;

        void SwitchSort(int sortType)
        {
            Sort = (IMapServiceV1.SortType)sortType;
            CurrentPage = 0;
            DirtyData = true;
        }

        void SwitchTime(int timePeriod)
        {
            Days = (IMapServiceV1.TimePeriod)DaysValues.GetValue(timePeriod);
            CurrentPage = 0;
            DirtyData = true;
        }

        void SwitchLang(int langFilter)
        {
            Lang = (IMapServiceV1.LangFilter)langFilter;
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

    private void BuildSearchGroup(YKLayout group)
    {
        var searchGroup = group.Horizontal();
        var search = searchGroup.InputText(Search)
            .WithWidth((int)(_refSize.width * 0.5f));

        if (Search.IsEmptyOrNull) {
            Search = "exm_ui_search_hint".lang();
        }

        search.field.characterLimit = 100;
        search.field.contentType = InputField.ContentType.Standard;
        search.field.text = Search;
        search.field.onValueChanged.AddListener(query => Search = query);

        searchGroup.Button("exm_ui_btn_search".lang(), RunSearch)
            .WithMinWidth((int)(_refSize.width * 0.25f))
            .GetComponent<Image>().color = Color.green;

        return;

        void RunSearch()
        {
            if (!Search.IsEmptyOrNull) {
                SearchMode = true;
                DirtyData = true;
            }
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
            pageGroup.Button($"{CurrentPage}", () => SwitchPage(CurrentPage - 1))
                .WithMinWidth(0)
                .GetComponent<Image>().color = Color.cyan;
        }

        var pages = Enumerable
            .Range(1, TotalPages)
            .Select(i => i.ToString())
            .ToList();
        pageGroup.Dropdown(pages, SwitchPage, CurrentPage)
            .WithMinWidth(0);

        if (CurrentPage < TotalPages - 1) {
            pageGroup.Button($"{CurrentPage + 2}", () => SwitchPage(CurrentPage + 1))
                .WithMinWidth(0)
                .GetComponent<Image>().color = Color.cyan;
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

            MapMeta[]? maps;

            if (SearchMode) {
                var searchTask = service.GetMapMetaByQueryAsync(Search);

                var results = await UniTask.WhenAny(searchTask,
                    UniTask.Delay(TimeSpan.FromSeconds(ExmConfig.Policy.Timeout.Value)));
                if (!results.hasResultLeft) {
                    throw new TimeoutException("exm_error_service_timeout".lang());
                }

                maps = results.result;
            } else {
                var topMapTask = service.GetTopMapsAsync(Sort, pageSize, CurrentPage, Lang, Days, adultFilter)
                    .Preserve();
                var overviewTask = service.GetMapsOverviewAsync(Lang, Days, adultFilter)
                    .Preserve();
                var refreshMetaTask = UniTask.WhenAll(overviewTask, topMapTask)
                    .Preserve();

                var results = await UniTask.WhenAny(refreshMetaTask,
                    UniTask.Delay(TimeSpan.FromSeconds(ExmConfig.Policy.Timeout.Value)));
                if (!results.hasResultLeft) {
                    throw new TimeoutException("exm_error_service_timeout".lang());
                }

                (Overview, maps) = results.result;
            }

            foreach (var map in maps ?? []) {
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

    protected static IMapServiceV1.SortType Sort = IMapServiceV1.SortType.Created;
    protected static IMapServiceV1.LangFilter Lang = IMapServiceV1.LangFilter.All;
    protected static IMapServiceV1.TimePeriod Days = IMapServiceV1.TimePeriod.AllTime;
    protected static bool NoAdult = EClass.core.config.net.noAdult;
    protected int CurrentPage;
    protected int TotalPages;
    protected string Search = "";
    protected bool SearchMode;

    #endregion
}