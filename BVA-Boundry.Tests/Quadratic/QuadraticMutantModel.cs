using BVA_Boundry.Quadratic;

namespace BVA_Boundry.Tests.Quadratic;

/// <summary>The ten quadratic mutants of report.tex §5.11 (Table "Quadratic solver mutants").</summary>
public enum QuadraticMutant { None, N1, N2, N3, N4, N5, N6, N7, N8, N9, N10 }

/// <summary>
/// Independent re-implementation of the classifier (report.tex §5.2) that can inject exactly one mutant.
/// The strict branches are evaluated as "Δ &lt; 0, then Δ &gt; 0, else Δ = 0", the order N2/N3 are defined against.
/// No domain guard: the model is only fed suite inputs.
/// </summary>
internal static class QuadraticMutantModel
{
    public static QuadraticResult Solve(QuadraticMutant m, int a, int b, int c)
    {
        bool linear = m == QuadraticMutant.N4 ? a != 0 : a == 0;

        if (linear)
        {
            if (b != 0)
                return m == QuadraticMutant.N9 ? QuadraticResult.NoSolution : QuadraticResult.OneLinearRoot;
            if (c != 0)
                return m == QuadraticMutant.N8 ? QuadraticResult.InfinitelyManySolutions : QuadraticResult.NoSolution;
            return m == QuadraticMutant.N7 ? QuadraticResult.NoSolution : QuadraticResult.InfinitelyManySolutions;
        }

        long delta = m switch
        {
            QuadraticMutant.N1 => (long)b * b - 2L * a * c,
            QuadraticMutant.N6 => 2L * b - 4L * a * c,
            QuadraticMutant.N10 => (long)b * b - 4L * b * c,
            _ => (long)b * b - 4L * a * c,
        };

        var (negative, positive) = m == QuadraticMutant.N5
            ? (QuadraticResult.TwoDistinctRealRoots, QuadraticResult.NoRealRoots)
            : (QuadraticResult.NoRealRoots, QuadraticResult.TwoDistinctRealRoots);

        if (m == QuadraticMutant.N3 ? delta <= 0 : delta < 0)
            return negative;
        if (m == QuadraticMutant.N2 ? delta >= 0 : delta > 0)
            return positive;
        return QuadraticResult.OneRepeatedRealRoot;
    }
}
