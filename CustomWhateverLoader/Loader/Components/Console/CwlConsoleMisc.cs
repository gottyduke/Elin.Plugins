using System;
using System.Linq;
using System.Text;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using ReflexCLI.Attributes;
using UnityEngine;

namespace Cwl.Components;

internal partial class CwlConsole
{
    /// <summary>
    ///     help debug upload issues
    /// </summary>
    [ConsoleCommand("ugc.get_my_thingy")]
    internal static void CheckWorkshopItems()
    {
        var query = UgcQuery.GetMyPublished();
        query.SetReturnKeyValueTags(true);
        query.SetReturnMetadata(true);
        query.Execute(OnQueryComplete);

        return;

        void OnQueryComplete(UgcQuery q)
        {
            var list = q.ResultsList;
            var owner = App.Client.Owner.id;
            var login = User.Client.Id;

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
                    sb.AppendLineColor("ID MISMATCH", 0xff0000);
                }

                if (ugc.Owner.id != login) {
                    sb.AppendLineColor("STEAM MISMATCH", 0xff0000);
                }

                sb.AppendLine($" meta\t'{meta.TagColor(0xcc5500)}'");
                sb.AppendLine($" id\t'{id.TagColor(0x008b8b)}'");
                sb.AppendLine($" owner\t'{ugc.Owner.id.TagColor(0x7a59ff)}'");
            }

            var scrollPosition = Vector2.zero;
            ProgressIndicator
                .CreateProgress(() => new(header), _ => false)
                .OnHover(p => {
                    scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300f), GUILayout.MinWidth(800f));
                    {
                        GUILayout.Label(sb.ToString(), p.GUIStyle);
                    }
                    GUILayout.EndScrollView();

                    GUILayout.Label("cwl_ui_exception_copy".lang(), p.GUIStyle);
                })
                .OnEvent((p, e) => {
                    switch (e.button) {
                        case 0:
                            GUIUtility.systemCopyBuffer = sb.RemoveTagColor();
                            break;
                        case 2:
                            p.Kill();
                            break;
                    }
                });
        }
    }
}