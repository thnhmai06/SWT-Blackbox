// Counts the Stryker.NET mutants that the designed paper suite kills (report.tex, Executed Validation).
// Usage: dotnet run tools/StrykerDesignedKills.cs -- <path to reports/mutation-report.json>
// Run Stryker with disable-bail (and optionally SWT_LAYERS=L1,L2,...) so that killedBy lists every killing test.
// A test belongs to the designed suite when its method name contains "PaperSuiteCase" or "TC_QE_28".

using System.Text.Json;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: dotnet run tools/StrykerDesignedKills.cs -- <mutation-report.json>");
    return 1;
}

using var report = JsonDocument.Parse(File.ReadAllText(args[0]));
var root = report.RootElement;

var testNames = new Dictionary<string, string>();
foreach (var testFile in root.GetProperty("testFiles").EnumerateObject())
foreach (var test in testFile.Value.GetProperty("tests").EnumerateArray())
    testNames[test.GetProperty("id").GetString()!] = test.GetProperty("name").GetString()!;

var statuses = new Dictionary<string, int>();
var killed = 0;
var killedByDesigned = 0;
foreach (var file in root.GetProperty("files").EnumerateObject())
foreach (var mutant in file.Value.GetProperty("mutants").EnumerateArray())
{
    var status = mutant.GetProperty("status").GetString()!;
    statuses[status] = statuses.GetValueOrDefault(status) + 1;
    if (status != "Killed")
        continue;

    killed++;
    var designed = mutant.TryGetProperty("killedBy", out var killers)
                   && killers.EnumerateArray().Any(k =>
                       testNames.TryGetValue(k.GetString()!, out var name)
                       && (name.Contains("PaperSuiteCase") || name.Contains("TC_QE_28")));
    if (designed)
        killedByDesigned++;
}

Console.WriteLine(string.Join(", ", statuses.Select(s => $"{s.Key}: {s.Value}")));
Console.WriteLine($"killed by the designed suite: {killedByDesigned} of {killed} killed mutants");
return 0;
