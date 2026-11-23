using System;
using System.Collections.Generic;
using System.Linq;
namespace Citron;
internal sealed class AutoSessionDiscovery
{
    internal static readonly TimeSpan SearchInterval = TimeSpan.FromSeconds(30);
    readonly HashSet<string> suppressed = new(StringComparer.Ordinal);
    internal DateTimeOffset NextSearch { get; private set; } = DateTimeOffset.MinValue;
    internal bool IsDue(bool enabled, bool active, bool busy, bool locked, DateTimeOffset now) =>
        enabled && !active && !busy && !locked && now >= NextSearch;
    internal void Schedule(DateTimeOffset now) => NextSearch = now.Add(SearchInterval);
    internal void RequestSearch() => NextSearch = DateTimeOffset.MinValue;
    internal void Reset() { suppressed.Clear(); RequestSearch(); }
    internal void Suppress(string? session) { if (session != null) suppressed.Add(session); }
    internal string? SelectTarget(IEnumerable<string> sessions)
    {
        var found = new HashSet<string>(sessions, StringComparer.Ordinal);
        suppressed.IntersectWith(found);
        return found.Where(s => !suppressed.Contains(s)).OrderBy(s => s, StringComparer.Ordinal).FirstOrDefault();
    }
}
