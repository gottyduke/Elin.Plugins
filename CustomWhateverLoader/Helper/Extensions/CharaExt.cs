namespace Cwl.Helper.Extensions;

public static class CharaExt
{
    public static bool IsBoss(this Chara chara, bool hostileOnly = false)
    {
        var bossType = chara.source.tag.Contains("boss") || chara.c_bossType is BossType.Boss or BossType.Evolved;
        var hostile = !hostileOnly || chara.IsHostile();
        return bossType && hostile;
    }
}