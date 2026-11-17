using System;
using System.Collections.Generic;
using System.Linq;
namespace Citron;
internal static class SessionPolicy
{
    internal static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);
    internal static bool TargetExists(string? target, IEnumerable<string> sessions) =>
        target != null && sessions.Contains(target, StringComparer.Ordinal);
}
