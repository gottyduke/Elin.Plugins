namespace ElinTogether.Models;

public enum AttackSourceByte : byte
{
    None,
    Melee,
    Range,
    Hunger,
    Fatigue,
    Condition,
    WeaponEnchant,
    Burden,
    Trap,
    Fall,
    BurdenStairs,
    BurdenFallDown,
    Throw,
    Finish,
    Hang,
    Wrath,
    ManaBackfire,

    // ReSharper disable once IdentifierTypo
    DeathSentense,
    Shockwave,
    MagicSword,
    MoonSpear,
    MagicArrow,
    MagicHand,
}