using System;
using System.Collections.Generic;
using System.Text;

namespace Meteion.Toolkit.Localization.Abstractions;

public class LocalizationConfigurationException : Exception
{
    public LocalizationConfigurationException(string? message) : base(message)
    {
    }
}
