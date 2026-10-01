namespace Meteion.Toolkit.WPF.Controls;

/// <summary>
/// The state a <see cref="StatefulContainer"/> is displaying.
/// </summary>
public enum ViewStatus
{
    /// <summary>Work is in progress; the loading indicator is shown.</summary>
    Loading,

    /// <summary>Work finished successfully; the container's content is shown.</summary>
    Loaded,

    /// <summary>Work failed; the error state is shown.</summary>
    Error,
}
