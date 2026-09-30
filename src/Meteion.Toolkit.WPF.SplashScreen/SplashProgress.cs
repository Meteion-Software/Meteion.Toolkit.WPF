namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>
/// A single progress report. Created only through the static factories so each report states explicitly what to
/// set and what to leave unchanged. A <c>null</c> status means "unchanged"; an empty string clears the text.
/// </summary>
public readonly struct SplashProgress
{
    internal enum BarChange : byte
    {
        Unchanged,
        Determinate,
        Indeterminate,
    }

    private SplashProgress(BarChange bar, double value, string? status)
    {
        Bar = bar;
        Value = value;
        StatusText = status;
    }

    internal BarChange Bar { get; }

    internal double Value { get; }

    internal string? StatusText { get; }

    /// <summary>Sets the bar to <paramref name="value"/> (0.0–1.0, clamped) and optionally the status text.</summary>
    public static SplashProgress Determinate(double value, string? status = null) => new(BarChange.Determinate, value, status);

    /// <summary>Switches the bar to indeterminate mode and optionally sets the status text.</summary>
    public static SplashProgress Indeterminate(string? status = null) => new(BarChange.Indeterminate, 0, status);

    /// <summary>Sets the status text and leaves the bar unchanged.</summary>
    public static SplashProgress Status(string status) => new(BarChange.Unchanged, 0, status ?? string.Empty);
}
