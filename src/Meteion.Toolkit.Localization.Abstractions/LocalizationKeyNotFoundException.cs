using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meteion.Toolkit.Localization.Abstractions;

/// <summary>
/// Thrown when a localization key has no value in the searched resource assembly and the configured
/// <see cref="MissingResourceBehavior"/> is <see cref="MissingResourceBehavior.ThrowException"/>.
/// </summary>
public class LocalizationKeyNotFoundException : Exception
{
    /// <summary>The key that could not be resolved.</summary>
    public string Key { get; }

    /// <summary>The assembly that was searched for the key.</summary>
    public Assembly ResourceAssembly { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationKeyNotFoundException"/> class.
    /// </summary>
    /// <param name="key">The key that could not be resolved.</param>
    /// <param name="resourceAssembly">The assembly that was searched for the key.</param>
    public LocalizationKeyNotFoundException(string key, Assembly resourceAssembly)
        : base($"No localized string found for key '{key}' in assembly '{resourceAssembly.GetName().Name}'.")
    {
        Key = key;
        ResourceAssembly = resourceAssembly;
    }
}