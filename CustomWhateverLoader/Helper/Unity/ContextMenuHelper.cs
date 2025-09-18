using System;
using System.Collections.Generic;
using System.Reflection;
using Cwl.API.Attributes;
using Cwl.Helper.String;
using Cwl.LangMod;
using MethodTimer;

namespace Cwl.Helper.Unity;

public class ContextMenuHelper
{
    internal static readonly List<ContextMenuProxy> EntryProxies = [];
    private static readonly List<(MethodInfo, CwlContextMenu)> _delayedEvents = [];

    public static void Add(string entry, string displayName, Func<object?>? onClick = null)
    {
        if (entry.IsEmpty()) {
            CwlMod.WarnWithPopup<ContextMenuHelper>("entry is empty");
            return;
        }

        string[] parts = entry.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var menus = EntryProxies;

        for (var i = 0; i < parts.Length; ++i) {
            var subMenu = i < parts.Length - 1 || onClick == null;
            var part = parts[i];

            var item = menus.Find(p => p.Entry == part);
            if (item is null) {
                item = new(part, subMenu ? part : displayName) {
                    OnClick = i == parts.Length - 1 && subMenu ? null : Process,
                    IsPopper = subMenu,
                };
                menus.Add(item);
            } else if (item.IsPopper != subMenu) {
                CwlMod.WarnWithPopup<ContextMenuHelper>("attempt to add entry with same name but different type\n" +
                                                        $"{part} -> {(item.IsPopper ? "submenu" : "button")}");
                return;
            }

            menus = item.Children;
        }

        return;

        void Process()
        {
            try {
                var result = onClick?.Invoke();
                if (result is not null) {
                    ProgressIndicator.CreateProgressScoped(() => new(result.ToString()), 5f);
                }
            } catch (Exception ex) {
                CwlMod.WarnWithPopup<ContextMenuHelper>("cwl_warn_processor".Loc("context_menu", entry, ex.Message), ex);
                // noexcept
            }
        }
    }

    public static void Remove(string entry)
    {
        string[] parts = entry.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) {
            return;
        }

        var menus = EntryProxies;
        ContextMenuProxy? target = null;
        List<ContextMenuProxy>? parent = null;

        foreach (var part in parts) {
            target = menus.Find(p => p.Entry == part);
            if (target is null) {
                return;
            }

            parent = menus;
            menus = target.Children;
        }

        parent?.Remove(target!);
    }

    [Time]
    internal static void RegisterEvents(MethodInfo method, CwlContextMenu ctx)
    {
        _delayedEvents.Add((method, ctx));
    }

    [CwlSceneInitEvent(Scene.Mode.Title, true)]
    private static void AddDelayedContextMenu()
    {
        foreach (var (method, ctx) in _delayedEvents) {
            Add(ctx.Entry, ctx.BtnName, () => method.FastInvokeStatic());
            CwlMod.Log<ContextMenuHelper>("cwl_log_processor_add".Loc("context_menu", ctx.Entry, method.GetAssemblyDetail(false)));
        }
    }

    public class ContextMenuProxy(string entry, string name)
    {
        public readonly List<ContextMenuProxy> Children = [];
        public string DisplayName => name;
        public string Entry => entry;
        public Action? OnClick { get; init; }
        public bool IsPopper { get; init; }
    }
}