using System;
using System.Collections.Generic;
using Cwl.Helper.Unity;
using Cysharp.Threading.Tasks;
using EGate.API;
using EGate.Service;

namespace EGate.Components.Tabs;

internal class TabMapBrowser : TabExMoongateBase
{
    private readonly Dictionary<string, MapCard> _cachedCards = [];
    private readonly List<MapCard> _cards = [];
    private readonly IMapService _service = new CloudMapService();
    private readonly IMapService.SortType _sort = IMapService.SortType.Created;
    private bool _dirty;

    private void Update()
    {
        if (_dirty) {
            this.DestroyChildren();
            _dirty = false;
        } else {
            foreach (var card in _cards) {
                if (card.IsDirty) {
                    card.Refresh();
                    card.OnLayout(this);
                }
            }
        }
    }

    public override void OnLayout()
    {
        LoadMaps();
    }

    public override void OnLayoutConfirm()
    {
    }

    private void LoadMaps()
    {
        _cards.Clear();
        _dirty = false;

        this.DestroyChildren();

        Header("Map Browser");
        TextFlavor("Loading maps...");

        LoadMapsAsyncFunctor().AttachExternalCancellation(this.GetCancellationTokenOnDestroy()).ForgetEx();

        return;

        async UniTask LoadMapsAsyncFunctor()
        {
            try {
                var maps = await _service.GetTopMapsAsync(_sort, 25, 0);

                foreach (var map in maps) {
                    if (!map.IsValidVersion) {
                        continue;
                    }

                    if (!_cachedCards.TryGetValue(map.Id, out var cached)) {
                        cached = _cachedCards[map.Id] = new(_service, map);
                    }

                    _cards.Add(cached);
                }

                _dirty = true;
            } catch (Exception ex) {
                EgMod.WarnWithPopup<TabMapBrowser>(ex.Message, ex);
                // noexcept
            }
        }
    }
}