using System;

namespace Vlg.Core.Run;

/// <summary>domain.md MOBA rule: a skill's level can't exceed ceil(hero level / divisor).</summary>
public static class LevelRule
{
    public static int SkillCap(int heroLevel, int divisor, int maxSkillLevel) =>
        Math.Min(maxSkillLevel, (heroLevel + divisor - 1) / divisor);
}
