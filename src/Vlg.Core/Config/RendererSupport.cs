namespace Vlg.Core.Config;

/// <summary>
/// Preset options that only the Forward+ renderer has. Mobile (default renderer, design/engine.md §7)
/// gets the closest supported option instead of an engine warning.
/// </summary>
public static class RendererSupport
{
    public static AntiAliasing AntiAliasingFor(AntiAliasing requested, bool forwardPlus) =>
        requested == AntiAliasing.Taa && !forwardPlus ? AntiAliasing.Fxaa : requested;

    public static bool SsaoFor(bool requested, bool forwardPlus) => requested && forwardPlus;
}
