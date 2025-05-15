using Cwl.Helper.Runtime.Exceptions;

namespace Cwl.API.Drama;

public static class ActionParameterHelper
{
    public static void RequiresMoreThan(this string[] parameters, int count)
    {
        if (parameters.Length < count) {
            throw new DramaActionArgumentException(parameters);
        }
    }

    public static void Requires(this string[] parameters, out string a1)
    {
        parameters.RequiresMoreThan(1);
        a1 = parameters[0];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2)
    {
        parameters.RequiresMoreThan(2);
        a1 = parameters[0];
        a2 = parameters[1];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2, out string a3)
    {
        parameters.RequiresMoreThan(3);
        a1 = parameters[0];
        a2 = parameters[1];
        a3 = parameters[2];
    }

    public static void Requires(this string[] parameters, out string a1, out string a2, out string a3, out string a4)
    {
        parameters.RequiresMoreThan(4);
        a1 = parameters[0];
        a2 = parameters[1];
        a3 = parameters[2];
        a4 = parameters[3];
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
}