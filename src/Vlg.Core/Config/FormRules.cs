using System;

namespace Vlg.Core.Config;

/// <summary>Stat rules per skill kind, shared by hero skills (every branch form) and item actives.</summary>
static class FormRules
{
    /// <param name="at">Error prefix naming the config and the form.</param>
    public static void Validate(SkillForm f, string at)
    {
        var st = f.Stats;
        Require(st.Damage > 0 && st.Cooldown > 0, $"{at}: damage and cooldown must be > 0");
        Require(st.Pierce >= 0 && st.Bounces >= 0 && st.Falloff is >= 0 and < 1 && st.Slow is >= 0 and <= 1
            && st.SlowSec >= 0 && st.Stun >= 0 && st.Pull >= 0 && st.Burn >= 0 && st.BurnSec >= 0 && st.Knockback >= 0,
            $"{at}: pierce/bounces/slowSec/stun/pull/burn/knockback must be >= 0, falloff 0..<1, slow 0..1");
        if (st.Burn > 0) Require(st.BurnSec > 0, $"{at}: burn needs burnSec > 0");
        if (st.Slow > 0) Require(st.SlowSec > 0, $"{at}: slow needs slowSec > 0");
        if (f.Has(SkillFlag.BigPulse) || f.Has(SkillFlag.Aftershock) || f.Has(SkillFlag.Scratch))
            Require(st.Period >= 0.5f, $"{at}: period must be >= 0.5");
        if (f.Has(SkillFlag.ExplodeOnHit)) Require(st.Blast > 0, $"{at}: explosions need blast > 0");
        switch (f.Kind)
        {
            case SkillKind.Projectile:
            case SkillKind.Boomerang:
                Require(st.Amount is >= 1 and <= RunConfig.MaxProjectilesPerCast && st.Speed > 0 && st.Range > 0 && st.Radius >= 0,
                    $"{at}: needs count 1..{RunConfig.MaxProjectilesPerCast}, speed > 0, range > 0, radius >= 0");
                break;
            case SkillKind.Ring:
                Require(st.Radius > 0 && st.Speed > 0 && st.Knockback < st.Speed, $"{at}: needs radius > 0, speed > 0, knockback < speed");
                break;
            case SkillKind.Speaker:
                Require(st.Amount is >= 1 and <= RunConfig.MaxSummonsPerSkill && st.Duration > 0 && st.Radius > 0 && st.Range > 0,
                    $"{at}: needs count 1..{RunConfig.MaxSummonsPerSkill}, duration > 0, radius > 0, range > 0");
                break;
            case SkillKind.Aura:
                Require(st.Radius > 0 && st.Vuln >= 0, $"{at}: needs radius > 0, vuln >= 0");
                if (f.Has(SkillFlag.Trail)) Require(st.Duration > 0, $"{at}: trail needs duration > 0");
                break;
            case SkillKind.Beam:
                Require(st.Range > 0 && st.Width > 0 && st.Amount is >= 1 and <= RunConfig.MaxBeamDirections,
                    $"{at}: needs range > 0, width > 0, count 1..{RunConfig.MaxBeamDirections}");
                break;
            case SkillKind.Orbit:
                int objects = st.Amount * (f.Has(SkillFlag.DoubleDeck) ? 2 : 1);
                Require(objects is >= 1 and <= RunConfig.MaxBlades && st.Speed > 0 && st.Range > 0 && st.Radius > 0,
                    $"{at}: needs 1..{RunConfig.MaxBlades} objects (double deck counts twice), speed > 0, range > 0, radius > 0");
                break;
            case SkillKind.BellDrop:
                Require(st.Amount is >= 1 and <= RunConfig.MaxSummonsPerSkill && st.Radius > 0 && st.Range > 0,
                    $"{at}: needs count 1..{RunConfig.MaxSummonsPerSkill}, radius > 0, range > 0");
                break;
            case SkillKind.Quake:
                Require(st.Radius > 0 && st.Duration > 0, $"{at}: needs radius > 0, duration > 0");
                break;
            case SkillKind.Drone:
                Require(st.Amount is >= 1 and <= RunConfig.MaxDrones && st.Speed > 0 && st.Range > 0,
                    $"{at}: needs count 1..{RunConfig.MaxDrones}, speed > 0, range > 0");
                break;
            case SkillKind.Chain:
                Require(st.Amount is >= 1 and <= RunConfig.MaxProjectilesPerCast && st.Range > 0 && st.Radius > 0,
                    $"{at}: needs count 1..{RunConfig.MaxProjectilesPerCast} (targets), range > 0 (first target), radius > 0 (jump)");
                break;
            case SkillKind.Shield:
                Require(st.Radius > 0, $"{at}: needs radius > 0");
                break;
            case SkillKind.Sweep:
                Require(st.Range > 0 && st.Width > 0 && st.Arc is > 0 and <= 360 && st.Duration > 0 && st.Amount is >= 1 and <= RunConfig.MaxBeamDirections,
                    $"{at}: needs range > 0, width > 0, arc 0..360, duration > 0, count 1..{RunConfig.MaxBeamDirections}");
                break;
            default:
                throw new FormatException($"{at}: unknown kind {f.Kind}");
        }
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new FormatException(message);
    }
}
