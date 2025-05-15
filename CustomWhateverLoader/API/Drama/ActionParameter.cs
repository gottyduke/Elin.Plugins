using System;
using Cwl.Helper.Runtime.Exceptions;

namespace Cwl.API.Drama;

public static class ActionParameterHelper
{
    public static void RequiresAtleast(this string[] parameters, int count)
    {
        if (parameters.Length < count) {
            throw new DramaActionArgumentException(parameters);
        }
    }

    public static void Requires(this string[] parameters, out string a1)
    {
        parameters.RequiresAtleast(1);
        a1 = parameters[0];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2)
    {
        parameters.RequiresAtleast(2);
        a1 = parameters[0];
        a2 = parameters[1];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2, out string a3)
    {
        parameters.RequiresAtleast(3);
        a1 = parameters[0];
        a2 = parameters[1];
        a3 = parameters[2];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2, out string a3, out string a4)
    {
        parameters.RequiresAtleast(4);
        a1 = parameters[0];
        a2 = parameters[1];
        a3 = parameters[2];
        a4 = parameters[3];
    }

    public static void RequiresOpt(this string[] parameters, out OptParam a1)
    {
        a1 = new(parameters.TryGet(0, true));
    }

    public static void RequiresOpt(this string[] parameters, out OptParam a1, out OptParam a2)
    {
        Array.Resize(ref parameters, 2);
        a1 = new(parameters[0]);
        a2 = new(parameters[1]);
    }

    public static void RequiresOpt(this string[] parameters, out OptParam a1, out OptParam a2, out OptParam a3)
    {
        Array.Resize(ref parameters, 3);
        a1 = new(parameters[0]);
        a2 = new(parameters[1]);
        a3 = new(parameters[2]);
    }

    public static void RequiresOpt(this string[] parameters, out OptParam a1, out OptParam a2, out OptParam a3, out OptParam a4)
    {
        Array.Resize(ref parameters, 4);
        a1 = new(parameters[0]);
        a2 = new(parameters[1]);
        a3 = new(parameters[2]);
        a4 = new(parameters[3]);
    }

    public static void RequiresPerson(this DramaManager dm, out Person person)
    {
        if (dm.sequence.GetActor(DramaExpansion.Cookie?.Line["actor"] ?? "tg") is not { owner: { } owner }) {
            throw new DramaActionInvokeException("actor");
        }

        person = owner;
    }

    public static void RequiresActor(this DramaManager dm, out Chara actor)
    {
        if (dm.sequence.GetActor(DramaExpansion.Cookie?.Line["actor"] ?? "tg") is not { owner.chara: { } chara }) {
            throw new DramaActionInvokeException("actor");
        }

        actor = chara;
    }

    public class OptParam(string? value)
    {
        public bool Provided => !value.IsEmpty();
        public string Value => value!;

        public string Get(string fallback)
        {
            return value.IsEmpty(fallback);
        }
    }
}