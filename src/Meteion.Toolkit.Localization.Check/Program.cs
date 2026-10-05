using Meteion.Toolkit.Localization.Check;

// Usage: meteion-loc-check [root] [--assembly-name <name>] [--root-namespace <ns>]
//                          [--warnaserror] [--error <LOCxxx>]... [--no-orphans] [--no-xaml]
//                          [--check-literals] [--literal-property <name>]... [--no-format]
string? rootArgument = null;
string? assemblyName = null;
string? rootNamespace = null;
var warningsAsErrors = false;
var errorCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var checkOrphans = true;
var checkXaml = true;
var checkLiterals = false;
var checkFormat = true;
var literalProperties = new List<string>();

for (var i = 0; i < args.Length; i++)
{
    // Consumes and returns the argument following the current option.
    string NextValue() => i + 1 < args.Length
        ? args[++i]
        : throw new ArgumentException($"Missing value for '{args[i]}'.");

    switch (args[i])
    {
        case "--assembly-name": assemblyName = NextValue(); break;
        case "--root-namespace": rootNamespace = NextValue(); break;
        case "--warnaserror": warningsAsErrors = true; break;
        case "--error":
            // Accept "--error LOC003;LOC004" as well as repeated flags, since MSBuild item
            // lists and properties are ';'-separated.
            foreach (var code in NextValue().Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                errorCodes.Add(code);
            }

            break;
        case "--no-orphans": checkOrphans = false; break;
        case "--no-xaml": checkXaml = false; break;
        case "--check-literals": checkLiterals = true; break;
        case "--no-format": checkFormat = false; break;
        case "--literal-property":
            literalProperties.AddRange(NextValue().Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            break;
        default:
            if (args[i].StartsWith('-'))
            {
                Console.Error.WriteLine($"meteion-loc-check: unknown option '{args[i]}'.");
                return 2;
            }

            rootArgument ??= args[i];
            break;
    }
}

var rootPath = Path.GetFullPath(rootArgument ?? Directory.GetCurrentDirectory());

var options = new LocalizationCheckOptions
{
    CheckOrphanKeys = checkOrphans,
    CheckXamlUsages = checkXaml,
    CheckLiterals = checkLiterals,
    CheckFormatStrings = checkFormat,
    AdditionalLiteralProperties = literalProperties,
    AssemblyName = string.IsNullOrWhiteSpace(assemblyName) ? null : assemblyName,
    RootNamespace = string.IsNullOrWhiteSpace(rootNamespace) ? null : rootNamespace,
};

var result = LocalizationKeyChecker.CheckDirectory(rootPath, options);
var errorCount = 0;

// MSBuild canonical format - Visual Studio's Error List and `dotnet build` both recognize
// "<origin>: warning|error <code>: <text>" and surface it without any extra parsing.
// Returns "error" or "warning" for an issue code and counts errors so the exit code can reflect them.
string Severity(string code)
{
    if (warningsAsErrors || errorCodes.Contains(code))
    {
        errorCount++;
        return "error";
    }

    return "warning";
}

foreach (var issue in result.ResourceIssues)
{
    var neutralFileName = Path.GetFileName(issue.NeutralResourcePath);
    var message = issue.Kind switch
    {
        LocalizationKeyIssueKind.MissingKey =>
            $"Key '{issue.Key}' is defined in '{neutralFileName}' but is missing from the '{issue.CultureName}' locale.",
        LocalizationKeyIssueKind.PlaceholderMismatch =>
            $"Key '{issue.Key}' has different format placeholders per locale: {issue.Detail}.",
        LocalizationKeyIssueKind.InvalidFormat =>
            $"Key '{issue.Key}' is not a valid format string ({issue.Detail}). Escape literal braces as '{{{{' and '}}}}' if it is formatted.",
        LocalizationKeyIssueKind.PlaceholderGap =>
            $"Key '{issue.Key}' skips a format placeholder: {issue.Detail}.",
        _ =>
            $"Key '{issue.Key}' exists in the '{issue.CultureName}' locale but is not defined in '{neutralFileName}' (possible typo or leftover key).",
    };

    Console.WriteLine($"{issue.LocaleResourcePath}: {Severity(issue.Code)} {issue.Code}: {message}");
}

foreach (var usage in result.UsageIssues)
{
    var message = usage.Kind switch
    {
        LocalizationKeyUsageIssueKind.UndefinedKey =>
            $"Key '{usage.Key}' is used here but is not defined in the .resx it resolves to and will throw or fail to resolve at runtime.",
        LocalizationKeyUsageIssueKind.AmbiguousUnqualifiedKey =>
            $"Unqualified key '{usage.Key}' is ambiguous because this project has more than one .resx file. Use a generated key ({{x:Static ...Keys.Key}}) or set Source.",
        LocalizationKeyUsageIssueKind.QualifiedKeyWithPrefix =>
            $"Qualified key '{usage.Key}' can't be combined with KeyPrefix. Use Source with an unqualified key instead.",
        LocalizationKeyUsageIssueKind.UnknownSource =>
            $"Source '{usage.Key}' does not name a .resx file in this project.",
        LocalizationKeyUsageIssueKind.UnverifiableCrossAssemblyKey =>
            $"'{usage.Key}' refers to another assembly, so it can't be checked from this project.",
        LocalizationKeyUsageIssueKind.SourceWithAssembly =>
            $"Source '{usage.Key}' can't be combined with Assembly, since Source already names its assembly. Remove Assembly.",
        LocalizationKeyUsageIssueKind.UnlocalizedLiteral =>
            $"Un-localized literal {usage.Key}. Use a localized value ({{lx:LocalizedValue ...}}) or add '<!-- loc-ignore: reason -->' before the element.",
        LocalizationKeyUsageIssueKind.IgnoreWithoutReason =>
            "'loc-ignore' has no reason. Write '<!-- loc-ignore: reason -->' to explain why the element is not localized.",
        LocalizationKeyUsageIssueKind.ArgumentCountMismatch =>
            $"Key '{usage.Key}': {usage.Detail}.",
        _ => $"Problem with key '{usage.Key}'.",
    };

    // Informational notes are never promoted - there's nothing wrong to fail on.
    var severity = usage.IsInformational ? "info" : Severity(usage.Code);
    Console.WriteLine($"{usage.XamlFilePath}({usage.LineNumber}): {severity} {usage.Code}: {message}");
}

var missingKeyCount = result.ResourceIssues.Count(i => i.Kind == LocalizationKeyIssueKind.MissingKey);
var orphanKeyCount = result.ResourceIssues.Count(i => i.Kind == LocalizationKeyIssueKind.OrphanKey);
var formatIssueCount = result.ResourceIssues.Count(i => i.Kind is LocalizationKeyIssueKind.PlaceholderMismatch
    or LocalizationKeyIssueKind.InvalidFormat or LocalizationKeyIssueKind.PlaceholderGap);
var usageIssueCount = result.UsageIssues.Count(i => !i.IsInformational);

if (result.IsClean)
{
    Console.WriteLine($"meteion-loc-check: no localization issues found under '{rootPath}'.");
}
else
{
    Console.WriteLine(
        $"meteion-loc-check: {missingKeyCount} missing, {orphanKeyCount} orphaned, {formatIssueCount} format issue(s), {usageIssueCount} XAML usage issue(s) found under '{rootPath}'.");
}

// Exit codes: 0 = success (warnings only), 1 = at least one issue was reported as an error, 2 = bad command line.
return errorCount > 0 ? 1 : 0;
