using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace SwtBlackbox.Quadratic.Tests.Black;

/// <summary>
///     Property-based tests for <see cref="QuadraticSolver" /> (report.tex §5.2, §5.8).
/// </summary>
public class QuadraticProperties
{
    private static Arbitrary<int> CoeffArb()
    {
        return Arb.From(Gen.Choose(-100, 100));
    }

    private static Arbitrary<(int A, int B, int C)> CoeffTripleArb()
    {
        return Arb.From(
            from a in Gen.Choose(-100, 100)
            from b in Gen.Choose(-100, 100)
            from c in Gen.Choose(-100, 100)
            select (a, b, c));
    }

    // Multiplying the equation by -1 does not change its roots.
    [Property(MaxTest = 2000)]
    public Property Negation_DoesNotChangeRoots()
    {
        return Prop.ForAll(CoeffArb(), CoeffArb(), CoeffArb(), (a, b, c) =>
            QuadraticSolver.Solve(a, b, c) == QuadraticSolver.Solve(-a, -b, -c));
    }

    // x -> -x mirrors the roots, so flipping the sign of b does not change the classification.
    [Property(MaxTest = 2000)]
    public Property SignFlipOfB_MirrorsRoots()
    {
        return Prop.ForAll(CoeffArb(), CoeffArb(), CoeffArb(), (a, b, c) =>
            QuadraticSolver.Solve(a, b, c) == QuadraticSolver.Solve(a, -b, c));
    }

    // For a != 0 and c != 0 the roots are reciprocal, so swapping a and c preserves the classification.
    [Property(MaxTest = 2000)]
    public Property Reciprocal_DoesNotChangeRoots()
    {
        return Prop.ForAll(CoeffArb(), CoeffArb(), CoeffArb(), (a, b, c) =>
            a == 0 || c == 0 || QuadraticSolver.Solve(a, b, c) == QuadraticSolver.Solve(c, b, a));
    }

    // Scaling all coefficients by a non-zero k (keeping them in range) does not change the roots.
    [Property(MaxTest = 2000)]
    public Property Scaling_DoesNotChangeRoots()
    {
        return Prop.ForAll(CoeffTripleArb(), Arb.From(Gen.Choose(-3, 3)), (triple, k) =>
        {
            var (a, b, c) = triple;
            if (k == 0 || Math.Abs((long)k * a) > 100 || Math.Abs((long)k * b) > 100 || Math.Abs((long)k * c) > 100)
                return true;
            return QuadraticSolver.Solve(k * a, k * b, k * c) == QuadraticSolver.Solve(a, b, c);
        });
    }

    // §5.8: Δ = b² − 4ac ≡ 0 or 1 (mod 4) for integer coefficients.
    [Property(MaxTest = 2000)]
    public Property Discriminant_Mod4_IsZeroOrOne()
    {
        return Prop.ForAll(CoeffArb(), CoeffArb(), CoeffArb(), (a, b, c) =>
        {
            var mod = (QuadraticSolver.Discriminant(a, b, c) % 4 + 4) % 4;
            return mod is 0 or 1;
        });
    }

    // The classifier is total: a != 0 yields a quadratic result, a == 0 yields a linear/degenerate result.
    [Property(MaxTest = 2000)]
    public Property Result_MatchesCase()
    {
        return Prop.ForAll(CoeffArb(), CoeffArb(), CoeffArb(), (a, b, c) =>
        {
            var result = QuadraticSolver.Solve(a, b, c);
            return a != 0
                ? result is QuadraticResult.NoRealRoots or QuadraticResult.OneRepeatedRealRoot
                    or QuadraticResult.TwoDistinctRealRoots
                : result is QuadraticResult.OneLinearRoot or QuadraticResult.NoSolution
                    or QuadraticResult.InfinitelyManySolutions;
        });
    }
}