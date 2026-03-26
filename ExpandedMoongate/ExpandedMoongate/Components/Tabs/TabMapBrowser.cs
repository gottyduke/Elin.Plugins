using System;
using System.Collections.Generic;
using System.Linq;
using Cwl.Helper.Unity;
using Cysharp.Threading.Tasks;
using Exm.API;
using Exm.Helper;
using Microsoft.Extensions.DependencyInjection;

namespace Exm.Components.Tabs;

internal class TabMapBrowser : TabExMoongateBase
{
    private readonly Dictionary<string, MapCardView> _cachedCards = [];
    private readonly List<MapCardView> _cards = [];
    private bool _dirty;
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
        var headerGroup = this.MakeEqualWidthGroup();
        headerGroup.Text("total maps: 204");
        headerGroup.Text("maps added last 24 hours: 7");
    }

    private void BuildControlButtons()
    {
        var btnGroup = this.MakeEqualWidthGroup();

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
                var maps = await service.GetTopMapsAsync(_sort, 25, 0, "EN", null);

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