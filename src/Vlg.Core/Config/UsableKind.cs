namespace Vlg.Core.Config;

/// <summary>Usable item effect; numbers in <see cref="UsableDef"/>.</summary>
public enum UsableKind
{
    /// <summary>Energy Drink: skills and actives recharge faster, faster move, then a slow crash.</summary>
    Rush,
    /// <summary>Smoke Bomb: enemies chase the spot where it was thrown instead of the hero; faster move.</summary>
    Smoke,
    /// <summary>Holy Water: burning pool thrown along the camera; extra damage to undead.</summary>
    HolyWater,
    /// <summary>Pocket Singularity: pulls enemies to a point along the camera, then pops.</summary>
    Singularity,
}
