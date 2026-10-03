namespace SwtBlackbox.Quadratic.Tests;

/// <summary>
///     Executes the collapsed quadratic decision table (QR1..QR10) of report.tex, Case Study II, as an oracle over
///     the whole coefficient domain [-100, 100]^3. Every triple is evaluated against the condition entries of every
///     rule; it must match exactly one rule, the solver must return that rule's outcome, and the five infeasible
///     (a ≠ 0) columns of the full table must contain no triple.
/// </summary>
public class QuadraticDecisionTableTests
{
    // Triples per rule over the 201^3 = 8,120,601 coefficient triples, in the order QR1..QR10.
    private static readonly int[] ExpectedTriplesPerRule =
        [40_200, 200, 1, 3_003_112, 904, 4_995_984, 40_000, 20_000, 20_000, 200];

    // q1: a = 0, q2: b = 0, q3: c = 0, q4: sign of Δ (only for a ≠ 0). A null entry is a don't-care or n/a.
    private static readonly Rule[] Rules =
    [
        new("QR1", true, false, null, null, QuadraticResult.OneLinearRoot),
        new("QR2", true, true, false, null, QuadraticResult.NoSolution),
        new("QR3", true, true, true, null, QuadraticResult.InfinitelyManySolutions),
        new("QR4", false, false, false, -1, QuadraticResult.NoRealRoots),
        new("QR5", false, false, false, 0, QuadraticResult.OneRepeatedRealRoot),
        new("QR6", false, false, false, 1, QuadraticResult.TwoDistinctRealRoots),
        new("QR7", false, false, true, 1, QuadraticResult.TwoDistinctRealRoots),
        new("QR8", false, true, false, -1, QuadraticResult.NoRealRoots),
        new("QR9", false, true, false, 1, QuadraticResult.TwoDistinctRealRoots),
        new("QR10", false, true, true, 0, QuadraticResult.OneRepeatedRealRoot)
    ];

    // The five infeasible a ≠ 0 columns (b = 0, c = 0, sign of Δ) of the full table.
    private static readonly (bool BZero, bool CZero, int Sign)[] InfeasibleColumns =
    [
        (false, true, -1), (false, true, 0), (true, false, 0), (true, true, -1), (true, true, 1)
    ];

    /// <summary>
    ///     Every triple matches exactly one rule and the solver returns its outcome; the per-rule counts are as
    ///     reported, and the full table has 16 columns of which exactly the five infeasible ones are empty.
    /// </summary>
    [Fact]
    public void ExhaustiveDomain_EveryTripleMatchesExactlyOneRule()
    {
        var triplesPerRule = new int[Rules.Length];
        var triplesPerColumn = new Dictionary<(bool AZero, bool BZero, bool CZero, int Sign), int>();

        for (var a = -100; a <= 100; a++)
        for (var b = -100; b <= 100; b++)
        for (var c = -100; c <= 100; c++)
        {
            var conditions = Conditions.Of(a, b, c);

            var matched = -1;
            for (var i = 0; i < Rules.Length; i++)
            {
                if (!Rules[i].Matches(conditions))
                    continue;
                if (matched >= 0)
                    Assert.Fail($"({a},{b},{c}) matches both {Rules[matched].Id} and {Rules[i].Id}");
                matched = i;
            }

            if (matched < 0)
                Assert.Fail($"({a},{b},{c}) matches no rule");

            triplesPerRule[matched]++;
            var column = (conditions.AZero, conditions.BZero, conditions.CZero, conditions.Sign ?? 0);
            triplesPerColumn[column] = triplesPerColumn.GetValueOrDefault(column) + 1;

            if (QuadraticSolver.Solve(a, b, c) != Rules[matched].Outcome)
                Assert.Fail($"({a},{b},{c}): solver disagrees with {Rules[matched].Id}");
        }

        TestContext.Current.SendDiagnosticMessage(
            $"Per-rule triples: {string.Join(", ", Rules.Select((r, i) => $"{r.Id}={triplesPerRule[i]}"))}");

        Assert.Equal(8_120_601, triplesPerRule.Sum());
        Assert.Equal(ExpectedTriplesPerRule, triplesPerRule);

        // a = 0: the four (b, c) columns are all reached (Δ is not evaluated, recorded as 0 here).
        // a ≠ 0: 2 x 2 x 3 = 12 columns, of which exactly the five infeasible ones contain no triple.
        var aNonZeroColumns =
            from bZero in new[] { false, true }
            from cZero in new[] { false, true }
            from sign in new[] { -1, 0, 1 }
            select (bZero, cZero, sign);
        var empty = aNonZeroColumns.Where(col => !triplesPerColumn.ContainsKey((false, col.bZero, col.cZero, col.sign)));

        Assert.Equal(InfeasibleColumns.Order(), empty.Order());
        Assert.Equal(4, triplesPerColumn.Keys.Count(k => k.AZero));
        Assert.Equal(7, triplesPerColumn.Keys.Count(k => !k.AZero));
    }

    /// <summary>Every single-step suite case lands in the rule its Rule column names.</summary>
    [Fact]
    public void SuiteCases_MapToTheirRule()
    {
        foreach (var testCase in QuadraticPaperSuiteTests.Cases.Where(c => c.Steps.Length == 1))
        {
            var (a, b, c) = testCase.Steps[0];
            var conditions = Conditions.Of(a, b, c);

            var rule = Assert.Single(Rules, r => r.Matches(conditions));

            Assert.Equal(testCase.Rule, rule.Id);
        }
    }

    /// <summary>Condition values of one triple; <see cref="Sign" /> is null when a = 0 (q4 is not evaluated).</summary>
    private readonly record struct Conditions(bool AZero, bool BZero, bool CZero, int? Sign)
    {
        public static Conditions Of(int a, int b, int c)
        {
            int? sign = a == 0 ? null : Math.Sign((long)b * b - 4L * a * c);
            return new Conditions(a == 0, b == 0, c == 0, sign);
        }
    }

    /// <summary>A collapsed rule; a null entry is a don't-care (q3 in QR1) or n/a (q4 when a = 0).</summary>
    private sealed record Rule(string Id, bool AZero, bool BZero, bool? CZero, int? Sign, QuadraticResult Outcome)
    {
        public bool Matches(Conditions x)
        {
            return AZero == x.AZero
                   && BZero == x.BZero
                   && (CZero is null || CZero == x.CZero)
                   && (Sign is null || Sign == x.Sign);
        }
    }
}
