using Vlg.Core.Config;

namespace Vlg.Core.Run;

/// <summary>Changes resolved skill forms (socketed modifier items). SkillSystem re-resolves when Version changes.</summary>
public interface IFormModifier
{
    int Version { get; }
    SkillForm Apply(int slot, SkillForm form);
}
