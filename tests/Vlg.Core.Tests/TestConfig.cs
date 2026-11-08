using System;
using System.IO;
using Vlg.Core.Config;

namespace Vlg.Core.Tests;

static class TestConfig
{
    public static string GameJson => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "stress.json"));

    public static StressConfig Game => StressConfig.Parse(GameJson);

    public static SimTuning Sim => Game.Sim;

    public static string RunJson => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "run.json"));

    public static RunConfig Run => RunConfig.Parse(RunJson);

    public static string HeroJson => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "heroes", "lowkey.json"));

    public static HeroDef Hero => HeroDef.Parse(HeroJson, Run);

    public static string PyromancerJson => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "heroes", "pyromancer.json"));

    public static HeroDef Pyromancer => HeroDef.Parse(PyromancerJson, Run);

    public static string ItemsJson => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "items.json"));

    public static ItemsConfig Items => ItemsConfig.Parse(ItemsJson);
}
