using Vlg.Core.Config;
using Xunit;

namespace Vlg.Core.Tests;

public class RendererSupportTests
{
    [Theory]
    [InlineData(AntiAliasing.Taa, true, AntiAliasing.Taa)]
    [InlineData(AntiAliasing.Taa, false, AntiAliasing.Fxaa)]
    [InlineData(AntiAliasing.Fxaa, false, AntiAliasing.Fxaa)]
    [InlineData(AntiAliasing.None, false, AntiAliasing.None)]
    public void Taa_falls_back_to_fxaa_without_forward_plus(AntiAliasing requested, bool forwardPlus, AntiAliasing expected) =>
        Assert.Equal(expected, RendererSupport.AntiAliasingFor(requested, forwardPlus));

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void Ssao_needs_forward_plus(bool requested, bool forwardPlus, bool expected) =>
        Assert.Equal(expected, RendererSupport.SsaoFor(requested, forwardPlus));
}
