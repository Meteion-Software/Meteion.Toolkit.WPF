namespace Meteion.Toolkit.WPF.Localization.Tests.Fixtures.MultipleResx;

/// <summary>
/// Marker type whose assembly deliberately embeds two unrelated resx families
/// (First.resx, Second.resx). Used by ResxLocalizationProviderTests to exercise
/// both the "ambiguous unqualified key" failure path and qualified multi-resx lookups.
/// </summary>
public sealed class Marker;
