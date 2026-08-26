using Meteion.Toolkit.Localization.Abstractions;
using System.Diagnostics;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization;

/// <summary>
/// Routes localization key-resolution failures through WPF's own binding-diagnostics
/// <see cref="TraceSource"/> (<see cref="PresentationTraceSources.DataBindingSource"/>) —
/// the same channel a genuinely failed <c>{Binding}</c> uses — so a missing or
/// misconfigured key shows up in Visual Studio's "XAML Binding Failures" window /
/// Output pane alongside ordinary binding errors, instead of failing silently.
/// </summary>
/// <remarks>
/// This only produces visible output when binding tracing is actually turned on for the
/// running app — the same prerequisite ordinary WPF binding-failure trace output has.
/// In Visual Studio that's on by default while debugging (Debug &gt; Windows &gt; XAML
/// Binding Failures, or the Output window's Debug pane); outside the debugger it
/// requires a listener on <see cref="PresentationTraceSources.DataBindingSource"/>
/// (e.g. via app.config) to go anywhere.
/// </remarks>
internal static class LocalizationTraceSource
{
    public static void TraceMissingKey(string key, Assembly assembly, MissingResourceBehavior behavior)
    {
        var source = PresentationTraceSources.DataBindingSource;
        if (!source.Switch.ShouldTrace(TraceEventType.Error))
        {
            return;
        }

        source.TraceEvent(TraceEventType.Error, 0,
            $"Meteion.Toolkit.WPF.Localization Error: No localized string found for key '{key}' " +
            $"in assembly '{assembly.GetName().Name}' (MissingKeyBehavior: {behavior}). Verify the key " +
            "exists in Resources.resx (or the resolved culture's satellite .resx) for that assembly.");
    }
}
