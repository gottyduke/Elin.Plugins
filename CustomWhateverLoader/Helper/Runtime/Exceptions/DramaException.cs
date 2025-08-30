﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cwl.API.Drama;
using Cwl.Helper.String;
using HarmonyLib;

namespace Cwl.Helper.Exceptions;

public class DramaActionArgumentException(int count, string[] parameters) :
    Exception($"expected {count}, got [{parameters.Join()}");

public class DramaActionInvokeException(string callName) :
    Exception(callName);

public class DramaActorMissingException(string actorId) :
    Exception($"couldn't find actor {actorId}");

public class DramaRerouteException(Chara chara, Exception innerException) :
    Exception($"{chara.Name} / {chara.id}", innerException);

public class DramaParseLineException(DramaExpansion.ActionCookie cookie, Exception innerException) :
    Exception(GetLineDetail(cookie, innerException))
{
    private static Dictionary<string, string> _line = [];

    private static string GetLineDetail(DramaExpansion.ActionCookie cookie, Exception inner)
    {
        _line = cookie.Line;
        var dm = cookie.Dm;

        var cacheEntry = DramaManager.dictCache
            .LastOrDefault(kv => kv.Key.EndsWith($"{dm.setup.book}.xlsx", StringComparison.Ordinal)).Value;
        var bookPath = cacheEntry.path.ShortPath();
        var setup = dm.setup;

        return new StringBuilder()
            .AppendLine(inner.Message)
            .AppendLine()
            .AppendLine($"{setup.person.Name} / {setup.person.chara.id}")
            .AppendLine($"{bookPath}#{dm.countLine + cacheEntry.startIndex}")
            .AppendLine($"{Index("step")} {Index("jump")} {Index("if")} {Index("actor")} {Index("id")}")
            .AppendLine($"{Index("action")} {Index("param")}")
            .ToString();
    }

    private static string Index(string column)
    {
        return $"{column} [{_line.GetValueOrDefault(column)}]";
    }
}