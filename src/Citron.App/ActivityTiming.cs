using System;

namespace Citron;
internal static class ActivityTiming
{
    internal static TimeSpan NextDelay(int intervalSeconds, bool randomize, Random? random = null)
    {
        double seconds = Math.Clamp(intervalSeconds, 15, 3600);
        if (randomize) seconds *= 0.9 + (random ?? Random.Shared).NextDouble() * 0.2;
        return TimeSpan.FromSeconds(seconds);
    }
}
