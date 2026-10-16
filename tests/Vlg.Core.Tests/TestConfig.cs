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
}
