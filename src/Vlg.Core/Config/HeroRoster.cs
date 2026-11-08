using System;
using System.Collections.Generic;

namespace Vlg.Core.Config;

public static class HeroRoster
{
    public const string DefaultId = "lowkey";
    public static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(new[] { DefaultId, "pyromancer" });

    public static bool Contains(string? id) => id is DefaultId or "pyromancer";
}
