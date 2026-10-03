namespace SwtBlackbox.Quadratic.Tests;

/// <summary>
///     Layer ablation of the quadratic suite (report.tex, Case Study II): which of the ten mutants (none is
///     equivalent) are killed when the suite is cut down to the tests of the first layers only, and by each
///     layer on its own. A test with several steps (TC-QE-28) kills a mutant when any step differs.
/// </summary>
public class QuadraticLayerAblationTests
{
    private static readonly string[] Layers = ["L1", "L2", "L3", "L4"];

    private static readonly QuadraticMutant[] Mutants =
        [.. Enum.GetValues<QuadraticMutant>().Where(m => m != QuadraticMutant.None)];

    /// <summary>L1 alone, then L1-L2, L1-L3 and L1-L4: the mutants killed cumulatively, and how many.</summary>
    [Fact]
    public void CumulativeLayers_KillExpectedMutants()
    {
        QuadraticMutant[][] expected = [Mutants, Mutants, Mutants, Mutants];

        var actual = Layers.Select((_, i) => KilledBy(Layers[..(i + 1)])).ToArray();

        foreach (var (layer, killed) in Layers.Zip(actual))
            TestContext.Current.SendDiagnosticMessage(
                $"cumulative up to {layer}: {killed.Length} killed = {string.Join(", ", killed)}");

        Assert.Equal(expected, actual);
        Assert.Equal([10, 10, 10, 10], actual.Select(k => k.Length));
    }

    /// <summary>The mutants killed by the tests of one layer alone, ignoring all other layers.</summary>
    [Fact]
    public void EachLayerAlone_KillsExpectedMutants()
    {
        QuadraticMutant[][] expected =
        [
            Mutants,
            [
                QuadraticMutant.N2, QuadraticMutant.N3, QuadraticMutant.N4, QuadraticMutant.N5,
                QuadraticMutant.N6, QuadraticMutant.N8, QuadraticMutant.N9, QuadraticMutant.N10
            ],
            [
                QuadraticMutant.N1, QuadraticMutant.N2, QuadraticMutant.N3, QuadraticMutant.N4,
                QuadraticMutant.N5, QuadraticMutant.N6, QuadraticMutant.N10
            ],
            []
        ];

        var actual = Layers.Select(layer => KilledBy([layer])).ToArray();

        foreach (var (layer, killed) in Layers.Zip(actual))
            TestContext.Current.SendDiagnosticMessage(
                $"{layer} alone: {killed.Length} killed = {string.Join(", ", killed)}");

        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     The minimal decision table has six rules (QR1-QR3 and one rule per sign of the discriminant), because
    ///     QR7-QR10 only refine the sign rules QR4-QR6 by b = 0 and c = 0; its L1 tests are those of QR1-QR6.
    /// </summary>
    [Fact]
    public void MinimalTableL1_KillsAllTenMutants()
    {
        string[] rules = ["QR1", "QR2", "QR3", "QR4", "QR5", "QR6"];
        var cases = QuadraticPaperSuiteTests.Cases.Where(c => c.Layer == "L1" && rules.Contains(c.Rule)).ToArray();

        var killed = KilledBy(cases);

        TestContext.Current.SendDiagnosticMessage(
            $"minimal L1: {cases.Length} cases, killed = {killed.Length} = {string.Join(", ", killed)}");

        Assert.Equal(6, cases.Length);
        Assert.Equal(Mutants, killed);
    }

    private static QuadraticMutant[] KilledBy(string[] layers)
    {
        return KilledBy(QuadraticPaperSuiteTests.Cases.Where(c => layers.Contains(c.Layer)).ToArray());
    }

    private static QuadraticMutant[] KilledBy(QuadraticPaperCase[] cases)
    {
        return
        [
            .. Mutants.Where(mutant => cases.Any(c =>
            {
                var mutated = c.Steps.Select(s => QuadraticMutantModel.Solve(mutant, s.A, s.B, s.C));
                return !mutated.SequenceEqual(c.Expected);
            }))
        ];
    }
}
