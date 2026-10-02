using System.Numerics;
using BVA_Boundry.Quadratic;

namespace BVA_Boundry.Tests.Quadratic;

/// <summary>
/// Exhaustive sweep of every (a, b, c) in [-100, 100]³ (8,120,601 triples): totality, class counts,
/// a BigInteger re-statement of the spec, and the §5.8 discriminant reachability argument.
/// </summary>
public class QuadraticExhaustiveTests
{
    private const int Min = -100;
    private const int Max = 100;
    private const long Total = 8_120_601;

    // The oracle uses the same formula as production; its value is catching overflow /
    // branch-order errors and proving the classifier total over the domain, not independent
    // verification of the formula.
    private static QuadraticResult Oracle(int a, int b, int c)
    {
        if (a == 0)
        {
            if (b != 0)
                return QuadraticResult.OneLinearRoot;
            return c != 0 ? QuadraticResult.NoSolution : QuadraticResult.InfinitelyManySolutions;
        }

        var delta = (BigInteger)b * b - 4 * (BigInteger)a * c;
        return delta.Sign switch
        {
            -1 => QuadraticResult.NoRealRoots,
            0 => QuadraticResult.OneRepeatedRealRoot,
            _ => QuadraticResult.TwoDistinctRealRoots
        };
    }

    [Fact]
    public void Exhaustive_Solve_MatchesOracle_OverFullDomain()
    {
        var counts = new long[6];
        long total = 0;
        (int A, int B, int C)? firstMismatch = null;
        (int A, int B, int C)? firstMod4Violation = null;
        var gate = new object();

        Parallel.For(Min, Max + 1, a =>
        {
            var localCounts = new long[6];
            (int A, int B, int C)? localMismatch = null;
            (int A, int B, int C)? localMod4 = null;
            var localTotal = 0L;

            for (int b = Min; b <= Max; b++)
            for (int c = Min; c <= Max; c++)
            {
                var actual = QuadraticSolver.Solve(a, b, c);
                if (actual != Oracle(a, b, c) && localMismatch is null)
                    localMismatch = (a, b, c);
                localCounts[(int)actual]++;
                localTotal++;

                var mod = (QuadraticSolver.Discriminant(a, b, c) % 4 + 4) % 4;
                if (mod is not (0 or 1) && localMod4 is null)
                    localMod4 = (a, b, c);
            }

            lock (gate)
            {
                total += localTotal;
                for (var i = 0; i < 6; i++)
                    counts[i] += localCounts[i];
                if (localMismatch is not null && firstMismatch is null)
                    firstMismatch = localMismatch;
                if (localMod4 is not null && firstMod4Violation is null)
                    firstMod4Violation = localMod4;
            }
        });

        Assert.Null(firstMismatch);
        Assert.Null(firstMod4Violation);
        Assert.Equal(Total, total);
        Assert.Equal(Total, counts.Sum());
        Assert.All(counts, c => Assert.True(c > 0, "every QuadraticResult value must occur at least once"));
        Assert.Equal(1L, counts[(int)QuadraticResult.InfinitelyManySolutions]);
        Assert.Equal(200L, counts[(int)QuadraticResult.NoSolution]);
        Assert.Equal(200L * 201, counts[(int)QuadraticResult.OneLinearRoot]);
    }

    [Fact]
    public void Exhaustive_Discriminant_NearZero_ReachableSet()
    {
        var seen = new bool[9]; // index = Δ + 4, for Δ in [-4, 4]
        var gate = new object();

        Parallel.For(Min, Max + 1, a =>
        {
            var local = new bool[9];
            for (int b = Min; b <= Max; b++)
            for (int c = Min; c <= Max; c++)
            {
                var d = QuadraticSolver.Discriminant(a, b, c);
                if (d is >= -4 and <= 4)
                    local[d + 4] = true;
            }

            lock (gate)
            {
                for (var i = 0; i < 9; i++)
                    seen[i] |= local[i];
            }
        });

        var sortedSet = seen
            .Select((hit, idx) => (hit, delta: (long)(idx - 4)))
            .Where(x => x.hit)
            .Select(x => x.delta)
            .ToArray();

        Assert.Equal(new[] { -4L, -3L, 0L, 1L, 4L }, sortedSet);
    }
}
