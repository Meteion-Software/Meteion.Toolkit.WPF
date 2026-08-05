using System;
using System.Collections.Generic;
using System.Text;

namespace Meteion.Toolkit.WPF.SampleApp.Services
{
    public class ScopeIdService : IScopeIdService
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public interface IScopeIdService
    {
        Guid Id { get; set; }
    }
}
