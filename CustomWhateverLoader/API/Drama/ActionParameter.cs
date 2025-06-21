using System;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;

namespace Cwl.API.Drama;

public static class ActionParameterHelper
{
    public static void RequiresAtLeast(this string[] parameters, int count)
    {
        if (parameters.Length < count) {
            throw new DramaActionArgumentException(count, parameters);
        }
    }

    public static void Requires(this string[] parameters, out string a1)
    {
        parameters.RequiresAtLeast(1);
        a1 = parameters[0];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2)
    {
        parameters.RequiresAtLeast(2);
        a1 = parameters[0];
        a2 = parameters[1];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2, out string a3)
    {
        parameters.RequiresAtLeast(3);
        a1 = parameters[0];
        a2 = parameters[1];
        a3 = parameters[2];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2, out string a3, out string a4)
    {
        parameters.RequiresAtLeast(4);
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

    public static void RequiresActor(this DramaManager dm, out Chara actor)
    {
        RequiresPerson(dm, out var person);

        actor = person.chara ?? throw new DramaActionInvokeException("actor.chara");
    }

    public static void RequiresPerson(this DramaManager dm, out Person person)
    {
        var actorId = DramaExpansion.Cookie?.Line["actor"].Replace("?", "") ?? "tg";
        if (dm.sequence.GetActor(actorId) is not { owner: { } owner }) {
            throw new DramaActionInvokeException("actor");
        }

        person = owner;
    }

    public class OptParam(string? value)
    {
        public bool Provided => !value.IsEmpty();
        public string Value => value!;

        public string Get(string fallback)
        {
            return value.IsEmpty(fallback);
        }

        public int AsInt(int fallback = 0)
        {
            return value?.AsInt(fallback) ?? fallback;
        }

        public float AsFloat(float fallback = 0f)
        {
            return value?.AsFloat(fallback) ?? fallback;
        }
    }
}