using System.Linq;
using System.Runtime.CompilerServices;
using Cwl.Helper.String;
using Cwl.Helper.Unity;
using Serilog.Core;
using Serilog.Events;
using UnityEngine;

#pragma warning disable CA2254

namespace ElinTogether;

internal static partial class EmpLogger
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Debug(string messageTemplate)
    {
        Popup(LogEventLevel.Debug, messageTemplate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Debug<T0>(string messageTemplate, T0 property0)
    {
        Popup(LogEventLevel.Debug, messageTemplate, property0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1)
    {
        Popup(LogEventLevel.Debug, messageTemplate, property0, property1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Debug(string messageTemplate, params object?[]? propertyValues)
    {
        Popup(LogEventLevel.Debug, messageTemplate, propertyValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Information(string messageTemplate)
    {
        Popup(LogEventLevel.Information, messageTemplate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Information<T0>(string messageTemplate, T0 property0)
    {
        Popup(LogEventLevel.Information, messageTemplate, property0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1)
    {
        Popup(LogEventLevel.Information, messageTemplate, property0, property1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Information(string messageTemplate, params object?[]? propertyValues)
    {
        EmpLog.Write(LogEventLevel.Information, messageTemplate, propertyValues);
        PopupInternal(messageTemplate, propertyValues);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Popup(LogEventLevel logLevel, string messageTemplate)
    {
        EmpLog.Write(logLevel, messageTemplate);
        PopupInternal(messageTemplate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Popup<T0>(LogEventLevel logLevel, string messageTemplate, T0 property0)
    {
        EmpLog.Write(logLevel, messageTemplate, property0);
        PopupInternal(messageTemplate, property0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Popup<T0, T1>(LogEventLevel logLevel, string messageTemplate, T0 property0, T1 property1)
    {
        EmpLog.Write(logLevel, messageTemplate, property0, property1);
        PopupInternal(messageTemplate, property0, property1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void Popup(LogEventLevel logLevel, string messageTemplate, params object?[]? propertyValues)
    {
        EmpLog.Write(logLevel, messageTemplate, propertyValues);
        PopupInternal(messageTemplate, propertyValues);
    }

    [MessageTemplateFormatMethod("messageTemplate")]
    internal static void PopupInternal(string messageTemplate, params object?[]? propertyValues)
    {
        if (!EmpLog.BindMessageTemplate(messageTemplate, propertyValues, out var template, out var captured)) {
            return;
        }

        var rendered = template.Render(captured.ToDictionary(p => p.Name, p => p.Value)).Replace("\"", "");
        var truncation = rendered.Length > 150;
        var header = rendered;
        if (truncation) {
            var truncated = rendered.ToTruncateString(150);
            if (!ReferenceEquals(truncated, header)) {
                header = truncated;
            } else {
                truncation = false;
            }
        }

        using var progress = ProgressIndicator.CreateProgressScoped(() => new(header));

        if (truncation) {
            var footer = rendered.RemoveTagColor()[150..];
            progress.Get<ProgressIndicator>().OnHover(p => GUILayout.Label(footer, p.GUIStyle));
        }
    }
}