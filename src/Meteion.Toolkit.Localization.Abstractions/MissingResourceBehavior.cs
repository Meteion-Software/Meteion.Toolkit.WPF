namespace Meteion.Toolkit.Localization.Abstractions
{
    /// <summary>
    /// Specifies what the localization service does when a key has no value in the resources.
    /// </summary>
    public enum MissingResourceBehavior
    {
        /// <summary>Throw a <see cref="LocalizationKeyNotFoundException"/>.</summary>
        ThrowException,

        /// <summary>Return the key itself as the displayed text.</summary>
        ReturnKey,

        /// <summary>Return an empty string.</summary>
        ReturnEmptyString
    }
}
