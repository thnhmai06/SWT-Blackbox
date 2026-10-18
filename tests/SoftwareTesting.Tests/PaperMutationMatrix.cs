namespace SoftwareTesting.Tests;

/// <summary>
///     Reads a Test×Mutant matrix transcribed verbatim from report.tex
///     (one line per test: "TC-xx-nn: c1 c2 …", cell K = killed, S or -- = not killed).
/// </summary>
public static class PaperMutationMatrix
{
    public static IReadOnlyList<(string TestId, string[] Cells)> Parse(string matrix)
    {
        return
        [
            .. matrix.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split(':', 2))
                .Select(parts => (parts[0].Trim(), parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)))
        ];
    }

    /// <summary>Test IDs whose cell in the given 0-based mutant column is K.</summary>
    public static string[] KilledBy(string matrix, int column)
    {
        return [.. Parse(matrix).Where(row => row.Cells[column] == "K").Select(row => row.TestId).Order()];
    }
}