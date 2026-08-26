using Meteion.Toolkit.Localization.Check;

var positional = args.Where(a => !a.StartsWith('-')).ToArray();
var rootPath = Path.GetFullPath(positional.Length > 0 ? positional[0] : Directory.GetCurrentDirectory());
var strict = args.Contains("--strict");

var options = new LocalizationCheckOptions
{
    CheckOrphanKeys = !args.Contains("--no-orphans"),
    CheckXamlUsages = !args.Contains("--no-xaml"),
};

var result = LocalizationKeyChecker.CheckDirectory(rootPath, options);

foreach (var issue in result.ResourceIssues)
{
    var neutralFileName = Path.GetFileName(issue.NeutralResourcePath);
    var message = issue.Kind == LocalizationKeyIssueKind.MissingKey
        ? $"Key '{issue.Key}' is defined in '{neutralFileName}' but is missing from the '{issue.CultureName}' locale."
        : $"Key '{issue.Key}' exists in the '{issue.CultureName}' locale but is not defined in '{neutralFileName}' (possible typo or leftover key).";
    var code = issue.Kind == LocalizationKeyIssueKind.MissingKey ? "LOC001" : "LOC002";

    // MSBuild canonical format - Visual Studio's Error List and `dotnet build` both
    // recognize this and surface it as a warning without any extra parsing.
    Console.WriteLine($"{issue.LocaleResourcePath}: warning {code}: {message}");
}

foreach (var usage in result.UsageIssues)
{
    Console.WriteLine(
        $"{usage.XamlFilePath}({usage.LineNumber}): warning LOC003: Key '{usage.Key}' is used here but is not defined in any scanned .resx file and will throw or fail to resolve at runtime.");
}

var missingKeyCount = result.ResourceIssues.Count(i => i.Kind == LocalizationKeyIssueKind.MissingKey);
var orphanKeyCount = result.ResourceIssues.Count(i => i.Kind == LocalizationKeyIssueKind.OrphanKey);

if (result.IsClean)
{
    Console.WriteLine($"meteion-loc-check: no localization issues found under '{rootPath}'.");
}
else
{
    Console.WriteLine(
        $"meteion-loc-check: {missingKeyCount} missing, {orphanKeyCount} orphaned, {result.UsageIssues.Count} undefined-usage issue(s) found under '{rootPath}'.");
}

var hasBlockingIssues = missingKeyCount > 0 || result.UsageIssues.Count > 0;
return strict && hasBlockingIssues ? 1 : 0;
