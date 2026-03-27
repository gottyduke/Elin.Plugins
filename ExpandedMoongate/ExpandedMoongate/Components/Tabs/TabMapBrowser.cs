using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Unity;
using Cwl.LangMod;
using Cysharp.Threading.Tasks;
using Exm.API;
using Exm.Model.Map;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

namespace Exm.Components.Tabs;

internal class TabMapBrowser : TabExMoongateBase
{
    private readonly Dictionary<string, MapCardView> _cachedCards = [];
    private readonly List<MapCardView> _cards = [];
    private bool _dirty;
    private MapServiceOverview? _overview;
    private IMapServiceV1.SortType _sort = IMapServiceV1.SortType.Created;

    private void Update()
    {
        if (_dirty) {
            this.DestroyChildren(true);

            BuildHeaders();
            BuildControlButtons();

            foreach (var card in _cards) {
                card.OnLayout(this);
            }

            _dirty = false;
        }
    }

    public override void OnLayout()
    {
        LoadMaps();
    }

    public override void OnLayoutConfirm()
    {
    }

    private void BuildHeaders()
    {
        var headerGroup = Vertical();

        if (_overview is null) {
            headerGroup.Text("exm_ui_null_overview");
            return;
        }

        headerGroup.Text("exm_ui_maps_total".Loc(_overview.MapsCount));
        headerGroup.Text("exm_ui_visits_total".Loc(_overview.VisitsCount));
        headerGroup.Text("exm_ui_maps_today".Loc(_overview.MapsToday));
        headerGroup.Text("exm_ui_visits_today".Loc(_overview.VisitsToday));
    }

    private void BuildControlButtons()
    {
        var btnGroup = Horizontal();

        var sortOptions = Enum.GetNames(typeof(IMapServiceV1.SortType)).ToList();
        btnGroup.Dropdown(sortOptions, i => _sort = (IMapServiceV1.SortType)i, 0);
    }

    private void LoadMaps()
    {
        _cards.Clear();
        _dirty = false;

        Header("Map Browser");
        TextFlavor("Loading maps...");

        LoadMapsAsyncFunctor()
            .AttachExternalCancellation(this.GetCancellationTokenOnDestroy())
            .ForgetEx();

        return;

        async UniTask LoadMapsAsyncFunctor()
        {
            try {
                var service = ExmService.Provider.GetRequiredService<IMapService>();

                var overviewTask = service.GetMapsOverviewAsync()
                    .Preserve();
                var topMapTask = service.GetTopMapsAsync(_sort, 25, 0, "EN", null)
                    .Preserve();
                var queryTask = UniTask.WhenAll(overviewTask, topMapTask);

                var timeout = Mathf.Max(ExmConfig.Policy.Timeout.Value, 3f);
                var results = await UniTask.WhenAny(queryTask,
                    UniTask.Delay(TimeSpan.FromSeconds(timeout)));

                if (!results.hasResultLeft) {
                    throw new TimeoutException("exm_error_service_timeout".lang());
                }

                (_overview, var maps) = results.result;

                foreach (var map in maps) {
                    if (!map.IsValidVersion) {
                        continue;
                    }

                    var card = new MapCardView(service, map);
                    _cards.Add(card);
                }
            } catch (Exception ex) {
                await UniTask.Yield();
                ExmMod.WarnWithPopup<TabMapBrowser>(ex.Message, ex);
                // noexcept
            } finally {
                await UniTask.Yield();
                _dirty = true;
            }
        }
    }
}