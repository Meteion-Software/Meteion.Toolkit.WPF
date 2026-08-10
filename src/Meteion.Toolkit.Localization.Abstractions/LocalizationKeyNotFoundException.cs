using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meteion.Toolkit.Localization.Abstractions;

public class LocalizationKeyNotFoundException : Exception
{
    public string Key { get; }
    public Assembly ResourceAssembly { get; }

    public LocalizationKeyNotFoundException(string key, Assembly resourceAssembly)
        : base($"No localized string found for key '{key}' in assembly '{resourceAssembly.GetName().Name}'.")
    {
        Key = key;
        ResourceAssembly = resourceAssembly;
    }
}