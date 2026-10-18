namespace SoftwareTesting.Quadratic;

/// <summary>Output domain D_out of the solver (report.tex §5.2).</summary>
public enum QuadraticResult
{
    NoRealRoots,
    OneRepeatedRealRoot,
    TwoDistinctRealRoots,
    OneLinearRoot,
    NoSolution,
    InfinitelyManySolutions
}