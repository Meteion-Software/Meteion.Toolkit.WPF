namespace Meteion.Toolkit.WPF.Controls;

/// <summary>
/// How a <see cref="StatefulContainer"/> presents its loading and error states relative to its content.
/// </summary>
public enum StatefulContainerMode
{
    /// <summary>The content is collapsed (but kept alive) while loading or in error, and the state visual is shown instead.</summary>
    Replace,

    /// <summary>The content stays visible and the state visual is shown on top of it, behind a dimmed layer that blocks input.</summary>
    Overlay,
}
