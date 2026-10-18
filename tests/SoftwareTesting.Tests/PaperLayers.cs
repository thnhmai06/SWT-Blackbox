namespace SoftwareTesting.Tests;

/// <summary>
///     Restricts the designed paper suites to a set of construction layers (L1–L4), so that a mutation tool can be run
///     against the cumulative layers of the layer ablation (report.tex, Executed Validation).
///     Set the environment variable <c>SWT_LAYERS</c> to a comma-separated list such as <c>L1,L2</c>;
///     when it is unset, every layer is included.
/// </summary>
public static class PaperLayers
{
    private static readonly string[]? Selected = Environment.GetEnvironmentVariable("SWT_LAYERS")
        ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static bool Includes(string layer)
    {
        return Selected is null || Selected.Contains(layer);
    }
}
