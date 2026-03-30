using System;
using Cysharp.Threading.Tasks;
using Exm.API;

namespace Exm.Components.Tabs;

internal class TabModeration : TabMapBrowser
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
            var service = ExmService.MapService as IModerationService;

            var topMapTask = service!.GetUnpreparedListAsync()
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

                _cards.Add(new(map));
            }
        } catch (Exception ex) {
            await UniTask.Yield();
            ExmMod.WarnWithPopup<TabModeration>(ex.Message, ex);
            // noexcept
        } finally {
            await UniTask.Yield();
            DirtyData = false;
            DirtyLayout = true;
        }
    }
}