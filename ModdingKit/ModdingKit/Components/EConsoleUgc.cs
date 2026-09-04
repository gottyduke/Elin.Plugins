using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EModding.Helper;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using ReflexCLI.Attributes;
using UnityEngine;

namespace EModding.Components;

internal partial class EConsole
{
    /// <summary>
    ///     help debug upload issues
    /// </summary>
    [ConsoleCommand("ugc.list")]
    internal static void CheckWorkshopItems()
    {
        Steam.QueryAllMyPublished(OnQueryComplete, ex => EGui.CreatePopup(ex));
        return;

        void OnQueryComplete(List<WorkshopItem> list)
        {
            var owner = App.Client.Owner;
            var login = UserData.Me;

            var header = $"Owner:\t{owner} \n" +
                         $"Login:\t{login}\n" +
                         $"Items: {list.Count}";

            if (list.Count == 0) {
                return;
            }

            var sb = new StringBuilder();
            foreach (var ugc in list) {
                var id = ugc.keyValueTags.FirstOrDefault(kv => kv.key == "id").value;
                var meta = ugc.metadata;

                sb.AppendLine($"{ugc.FileId.TagColor(0x708090)}\t{ugc.Title}");

                // the spaces from id might be trimmed during initial ugc upload
                if (!string.Equals(id, meta, StringComparison.Ordinal)) {
                    sb.AppendLine("ID MISMATCH".TagColor(0xff0000));
                }

                if (ugc.Owner != login) {
                    sb.AppendLine("STEAM MISMATCH".TagColor(0xff0000));
                }

                sb.AppendLine($" meta\t'{meta.TagColor(0xcc5500)}'");
                sb.AppendLine($" id\t'{id.TagColor(0x008b8b)}'");
                sb.AppendLine($" owner\t'{ugc.Owner.TagColor(0x7a59ff)}'");
            }

            var scrollPosition = Vector2.zero;
            EGui
                .CreatePopup(() => new(header), _ => false)
                .OnHover(p => {
                    scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300f), GUILayout.MinWidth(800f));
                    {
                        GUILayout.Label(sb.ToString(), p.GUIStyle);
                    }
                    GUILayout.EndScrollView();

                    GUILayout.Label("es_ui_exception_copy".lang(), p.GUIStyle);
                })
                .OnEvent((p, e) => {
                    switch (e.button) {
                        case 0:
                            GUIUtility.systemCopyBuffer = sb.RemoveTagColor();
                            break;
                        case 2:
                            p.Kill();
                            break;
                        default:
                            return;
                    }
                    e.Use();
                });
        }
    }
}