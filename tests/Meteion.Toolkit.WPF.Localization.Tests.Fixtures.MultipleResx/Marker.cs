namespace Meteion.Toolkit.WPF.Localization.Tests.Fixtures.MultipleResx;

/// <summary>
/// Marker type whose assembly deliberately embeds two unrelated resx families
/// (First.resx, Second.resx). Used by ResxLocalizationProviderTests to exercise
/// the "ambiguous, configure ResourceBaseNameSelector" failure path.
/// </summary>
public sealed class Marker;
