using System;
using Cwl.Helper.Exceptions;
using Cwl.Helper.Extensions;
using Cwl.Helper.String;

namespace Cwl.API.Drama;

public static class ActionParameterHelper
{
    extension(string[] parameters)
    {
        public void RequiresAtLeast(int count)
        {
            if (parameters.Length < count) {
                throw new DramaActionArgumentException(count, parameters);
            }
        }

        public void Requires(out string a1)
        {
            parameters.RequiresAtLeast(1);
            a1 = parameters[0];
        }

        public void Requires(out string a1, out string a2)
        {
            parameters.RequiresAtLeast(2);
            a1 = parameters[0];
            a2 = parameters[1];
        }

        public void Requires(out string a1, out string a2, out string a3)
        {
            parameters.RequiresAtLeast(3);
            a1 = parameters[0];
            a2 = parameters[1];
            a3 = parameters[2];
        }

        public void Requires(out string a1, out string a2, out string a3, out string a4)
        {
            parameters.RequiresAtLeast(4);
            a1 = parameters[0];
            a2 = parameters[1];
            a3 = parameters[2];
            a4 = parameters[3];
        }

        public void RequiresOpt(out OptParam a1)
        {
            a1 = new(parameters.TryGet(0, true));
        }

        public void RequiresOpt(out OptParam a1, out OptParam a2)
        {
            Array.Resize(ref parameters, 2);
            a1 = new(parameters[0]);
            a2 = new(parameters[1]);
        }

        public void RequiresOpt(out OptParam a1, out OptParam a2, out OptParam a3)
        {
            Array.Resize(ref parameters, 3);
            a1 = new(parameters[0]);
            a2 = new(parameters[1]);
            a3 = new(parameters[2]);
        }

        public void RequiresOpt(out OptParam a1, out OptParam a2, out OptParam a3, out OptParam a4)
        {
            Array.Resize(ref parameters, 4);
            a1 = new(parameters[0]);
            a2 = new(parameters[1]);
            a3 = new(parameters[2]);
            a4 = new(parameters[3]);
        }
    }

    extension(DramaManager dm)
    {
        public void RequiresActor(out Chara actor)
        {
            dm.RequiresPerson(out var person);

            actor = person?.chara ?? throw new DramaActorMissingException("actor.chara");
        }

        public void RequiresPerson(out Person? person)
        {
            var actorId = DramaExpansion.Cookie?.Line["actor"].Replace("?", "") ?? "tg";
            if (DramaExpansion.Cookie?.Dm.tg.chara.id == actorId) {
                actorId = "tg";
            }

            person = dm.GetPerson(actorId);
        }

        public Chara GetChara(string actorId)
        {
            return dm.GetPerson(actorId).chara ?? throw new DramaActorMissingException(actorId);
        }

        public Person GetPerson(string actorId)
        {
            return dm.GetActor(actorId) is not { owner: { } owner }
                ? throw new DramaActorMissingException(actorId)
                : owner;
        }
    }

    public class OptParam(string? value)
    {
        public bool Provided => !value.IsEmptyOrNull;
        public string Value => value!;

        public string Get(string fallback)
        {
            return value.OrIfEmpty(fallback);
        }

        public int AsInt(int fallback = 0)
        {
            return value?.AsInt(fallback) ?? fallback;
        }

        public float AsFloat(float fallback = 0f)
        {
            return value?.AsFloat(fallback) ?? fallback;
        }

        public double AsDouble(double fallback = 0d)
        {
            return value?.AsDouble(fallback) ?? fallback;
        }

        public bool AsBool(bool fallback = false)
        {
            return value?.AsBool(fallback) ?? fallback;
        }
    }
}