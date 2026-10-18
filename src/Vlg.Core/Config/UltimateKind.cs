namespace Vlg.Core.Config;

/// <summary>Ultimate behavior; numbers per level in <see cref="UltimateDef.Levels"/>.</summary>
public enum UltimateKind
{
    /// <summary>Hit + stun every enemy within radius: damage, radius, stun; tick + duration = shaking ground zone (per beat, beats).</summary>
    SubQuake,
    /// <summary>Drop forced on for duration beats, skills recharge (1 + speed) times as fast; heal = HP per kill meanwhile.</summary>
    Encore,
    /// <summary>1 bar windup, then a beam along the camera for duration beats that kills what it touches: range, width; tick = burning line per beat.</summary>
    BassCannon,
}
