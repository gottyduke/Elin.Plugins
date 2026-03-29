using System;
using Cysharp.Threading.Tasks;
using Steamworks;

namespace Exm.Components.Tabs;

internal class TabMapHistory : TabMapBrowser
{
    protected override void Update()
    {
        if (DirtyData) {
            this.DestroyChildren(true);
            RefreshMapData();
        }

        if (DirtyLayout) {
            this.DestroyChildren(true);

            if (_cards.Count == 0) {
                Header("exm_ui_no_history");
                return;
            }

            foreach (var card in _cards) {
                card.OnLayout(this);
            }

            DirtyLayout = false;
        }
    }

    protected override async UniTask RefreshMapDataAsync()
    {
        try {
            var service = ExmService.MapService;

            var topMapTask = service.GetMapHistoryByUserAsync(SteamUser.GetSteamID().ToString())
                .Preserve();

            var (hasResultLeft, maps) = await UniTask.WhenAny(topMapTask,
                UniTask.Delay(TimeSpan.FromSeconds(ExmConfig.Policy.Timeout.Value)));

            if (!hasResultLeft) {
                throw new TimeoutException("exm_error_service_timeout".lang());
            }

            if (maps is not { Length: > 0 }) {
                return;
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
}