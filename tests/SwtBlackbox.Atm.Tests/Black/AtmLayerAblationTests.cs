namespace SwtBlackbox.Atm.Tests.Black;

/// <summary>
///     Layer ablation of the ATM suite (report.tex, Case Study I, Executed Validation): which non-equivalent designed
///     mutants (M4 is equivalent) are killed by the tests of the cumulative layers, by each layer on its own, and by a
///     size-matched random baseline. Layer L3 is split into L3a (boundary combinations, AC*) and L3b (don't-care
///     expansion, AX*). A mutant is killed by a test when the mutant model's result differs from the test's
///     expected result.
/// </summary>
public class AtmLayerAblationTests
{
    private static readonly string[] Layers = ["L1", "L2", "L3a", "L3b", "L4"];

    private static readonly AtmMutant[] Mutants =
        [.. Enum.GetValues<AtmMutant>().Where(m => m is not (AtmMutant.None or AtmMutant.M4))];

    /// <summary>L1, then L1-L2, L1-L3a, L1-L3b and L1-L4: the mutants killed cumulatively.</summary>
    [Fact]
    public void CumulativeLayers_KillExpectedNonEquivalentMutants()
    {
        AtmMutant[] l1 = [AtmMutant.M5, AtmMutant.M9, AtmMutant.M12];
        AtmMutant[] l2 =
        [
            AtmMutant.M1, AtmMutant.M2, AtmMutant.M3, AtmMutant.M5, AtmMutant.M6, AtmMutant.M7,
            AtmMutant.M9, AtmMutant.M10, AtmMutant.M12, AtmMutant.M13
        ];
        AtmMutant[] l3b =
        [
            AtmMutant.M1, AtmMutant.M2, AtmMutant.M3, AtmMutant.M5, AtmMutant.M6, AtmMutant.M7,
            AtmMutant.M9, AtmMutant.M10, AtmMutant.M11, AtmMutant.M12, AtmMutant.M13, AtmMutant.M14
        ];
        AtmMutant[][] expected = [l1, l2, l2, l3b, Mutants];

        var actual = Layers.Select((_, i) => KilledBy(CasesOf(Layers[..(i + 1)]))).ToArray();

        foreach (var (layer, killed) in Layers.Zip(actual))
            TestContext.Current.SendDiagnosticMessage(
                $"cumulative up to {layer}: {killed.Length} killed = {string.Join(", ", killed)}");

        Assert.Equal(expected, actual);
        Assert.Equal([3, 10, 10, 12, 13], actual.Select(k => k.Length));
    }

    /// <summary>The mutants killed by the tests of one layer alone, ignoring all other layers.</summary>
    [Fact]
    public void EachLayerAlone_KillsExpectedNonEquivalentMutants()
    {
        AtmMutant[][] expected =
        [
            [AtmMutant.M5, AtmMutant.M9, AtmMutant.M12],
            [
                AtmMutant.M1, AtmMutant.M2, AtmMutant.M3, AtmMutant.M5, AtmMutant.M6, AtmMutant.M7,
                AtmMutant.M9, AtmMutant.M10, AtmMutant.M12, AtmMutant.M13
            ],
            [AtmMutant.M1, AtmMutant.M2, AtmMutant.M3, AtmMutant.M6, AtmMutant.M9],
            [AtmMutant.M5, AtmMutant.M9, AtmMutant.M11, AtmMutant.M12, AtmMutant.M14],
            [AtmMutant.M5, AtmMutant.M8, AtmMutant.M9]
        ];

        var actual = Layers.Select(layer => KilledBy(CasesOf([layer]))).ToArray();

        foreach (var (layer, killed) in Layers.Zip(actual))
            TestContext.Current.SendDiagnosticMessage(
                $"{layer} alone: {killed.Length} killed = {string.Join(", ", killed)}");

        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     The minimal decision table has seven rules, because DR8 and DR9 (external) differ from DR6 and DR7
    ///     (internal) only in the fee and merge with them; its L1 tests are those of DR1-DR7, i.e. the L1 tests
    ///     without DR8 and DR9.
    /// </summary>
    [Fact]
    public void MinimalTableL1_KillsTheSameNonEquivalentMutantsAsTheRefinedL1()
    {
        var cases = AtmPaperSuiteTests.Cases
            .Where(c => c.Layer == "L1" && c.Rule is not ("DR8" or "DR9")).ToArray();

        var killed = KilledBy(cases);

        TestContext.Current.SendDiagnosticMessage(
            $"minimal L1: {cases.Length} cases, killed = {killed.Length} = {string.Join(", ", killed)}");

        Assert.Equal(7, cases.Length);
        Assert.Equal([AtmMutant.M5, AtmMutant.M9, AtmMutant.M12], killed);
    }

    /// <summary>
    ///     Size-matched baseline for the L1 → L1-L2 step: the 9 L1 tests plus 19 tests drawn at random from the
    ///     interiors of the same nine rules (rule chosen uniformly; inputs uniform within the rule, see
    ///     <see cref="RandomMemberOf" />), repeated 1,000 times with a fixed seed. The boundary layer adds 7 mutants
    ///     (3 → 10); the baseline's distribution of killed mutants is asserted as observed.
    /// </summary>
    [Fact]
    public void RandomInteriorBaseline_SameSizeAsBoundaryLayer()
    {
        const int draws = 1_000;
        const int extraTests = 19;
        var random = new Random(20261002);
        var l1 = CasesOf(["L1"]);
        var totals = new int[draws];
        var perMutant = Mutants.ToDictionary(m => m, _ => 0);

        for (var draw = 0; draw < draws; draw++)
        {
            var suite = l1.Concat(Enumerable.Range(0, extraTests).Select(_ => RandomMemberOf(random))).ToArray();
            var killed = KilledBy(suite);
            totals[draw] = killed.Length;
            foreach (var mutant in killed)
                perMutant[mutant]++;
        }

        var summary = $"mean {totals.Average():F3}, min {totals.Min()}, max {totals.Max()}; " +
                      string.Join(", ", perMutant.Select(p => $"{p.Key}={p.Value}"));
        TestContext.Current.SendDiagnosticMessage($"random baseline (L1 + {extraTests} random, {draws} draws): {summary}");

        // Observed with seed 20261002: number of draws (of 1,000) in which each mutant is killed, in the order
        // M1, M2, M3, M5, M6, ..., M14 (M4 excluded), and the sum / min / max of the killed count per draw.
        Assert.Equal([0, 17, 20, 1000, 6, 5, 0, 1000, 0, 500, 1000, 12, 785], Mutants.Select(m => perMutant[m]));
        Assert.Equal((4345, 3, 8), (totals.Sum(), totals.Min(), totals.Max())); // mean 4.345
    }

    /// <summary>
    ///     A random input from the interior of a uniformly chosen rule DR1-DR9. Amount: A1 uniform in [0, 49,999];
    ///     A2 a uniform multiple k·50,000, k in [1, 400]; A3 uniform in [50,001, 19,999,999] without multiples;
    ///     A4 uniform in [20,000,001, 40,000,000]. dailyUsed uniform in [0, 20,000,000 − amount] when the daily
    ///     condition must hold, in [20,000,001 − amount, 20,000,000] when it must fail (at least 0), and in
    ///     [0, 20,000,000] when it is a don't-care. balance uniform in [T, T + 10,000,000] when R5 must hold,
    ///     [T − 10,000,000, T − 1] when it must fail, and [0, 30,000,000] when it is a don't-care.
    /// </summary>
    private static AtmPaperCase RandomMemberOf(Random random)
    {
        var rule = random.Next(1, 10);
        var status = rule == 1 ? AccountStatus.Frozen : AccountStatus.Active;
        var amountClass = rule switch
        {
            1 => random.Next(1, 5),
            2 => 1,
            3 => 3,
            4 => 4,
            _ => 2
        };
        var amount = amountClass switch
        {
            1 => random.NextInt64(0, 50_000),
            2 => 50_000L * random.Next(1, 401),
            3 => RandomNonMultiple(random),
            _ => random.NextInt64(20_000_001, 40_000_001)
        };
        bool? dailyOk = rule switch { 5 => false, >= 6 => true, _ => null };
        var dailyUsed = dailyOk switch
        {
            true => random.NextInt64(0, 20_000_000 - amount + 1),
            false => random.NextInt64(Math.Max(0, 20_000_001 - amount), 20_000_001),
            null => random.NextInt64(0, 20_000_001)
        };
        var isInternal = rule switch { 6 or 7 => true, 8 or 9 => false, _ => random.Next(2) == 0 };
        bool? balanceOk = rule switch { 6 or 8 => false, 7 or 9 => true, _ => null };
        var threshold = amount + (isInternal ? 0 : AtmService.ExternalFee);
        var balance = balanceOk switch
        {
            true => random.NextInt64(threshold, threshold + 10_000_001),
            false => random.NextInt64(threshold - 10_000_000, threshold),
            null => random.NextInt64(0, 30_000_001)
        };

        var expected = AtmMutantModel.Withdraw(AtmMutant.None, balance, dailyUsed, amount, status, isInternal);
        return new AtmPaperCase($"RND-DR{rule}", "random interior member", balance, dailyUsed, amount, status,
            isInternal, expected, "R", $"DR{rule}", null, null);
    }

    private static long RandomNonMultiple(Random random)
    {
        while (true)
        {
            var amount = random.NextInt64(50_001, 20_000_000);
            if (amount % 50_000 != 0)
                return amount;
        }
    }

    private static AtmPaperCase[] CasesOf(string[] layers)
    {
        return [.. AtmPaperSuiteTests.Cases.Where(c => layers.Contains(SubLayer(c)))];
    }

    // L3 is split by its combination label: AC* are boundary combinations (L3a), AX* don't-care expansions (L3b).
    private static string SubLayer(AtmPaperCase c)
    {
        if (c.Layer != "L3")
            return c.Layer;
        return c.Combo!.StartsWith("AX", StringComparison.Ordinal) ? "L3b" : "L3a";
    }

    private static AtmMutant[] KilledBy(AtmPaperCase[] cases)
    {
        return
        [
            .. Mutants.Where(mutant => cases.Any(c =>
                AtmMutantModel.Withdraw(mutant, c.Balance, c.DailyUsed, c.Amount, c.AccountStatus, c.IsInternal)
                != c.Expected))
        ];
    }
}
