using Citron;
using System.Text.Json;

void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

Check(ActivityTiming.NextDelay(60, false, new RejectRandom()).TotalSeconds == 60, "Fixed mode must not consume random samples.");
foreach (int baseline in new[] { 15, 60, 3600 })
{
    var rng = new Random(42);
    var samples = Enumerable.Range(0, 10000).Select(_ => ActivityTiming.NextDelay(baseline, true, rng).TotalSeconds).ToArray();
    Check(samples.All(s => s >= baseline * .9 - .000001 && s <= baseline * 1.1 + .000001), "A randomized delay escaped its bounds.");
    Check(samples.Any(s => s < baseline) && samples.Any(s => s > baseline), "Randomized delays must vary in both directions.");
    Check(samples.Distinct().Count() > 9000, "New delays must be drawn for successive attempts.");
    Check(Math.Abs(samples.Average() - baseline) < baseline * .002, "The sampled mean must remain close to the configured interval.");
}
Check(ActivityTiming.NextDelay(60, true, new FixedRandom(0)).TotalSeconds == 54, "The shortest 60-second delay must be 54 seconds.");
Check(Math.Abs(ActivityTiming.NextDelay(60, true, new FixedRandom(.999999)).TotalSeconds - 66) < .0001, "The longest delay must approach 66 seconds.");
Check(ActivityTiming.NextDelay(-1, false).TotalSeconds == 15 && ActivityTiming.NextDelay(int.MaxValue, false).TotalSeconds == 3600, "Invalid saved intervals must be bounded.");
var oldSettings = JsonSerializer.Deserialize("{\"Interval\":60}", JsonContext.Default.Preferences)!;
Check(!oldSettings.RandomizeInterval, "Existing settings must default to fixed timing.");
foreach (bool enabled in new[] { false, true })
{
    var encoded = JsonSerializer.Serialize(new Preferences { RandomizeInterval = enabled }, JsonContext.Default.Preferences);
    Check(JsonSerializer.Deserialize(encoded, JsonContext.Default.Preferences)!.RandomizeInterval == enabled, "The checkbox preference must survive a JSON round trip.");
}
Console.WriteLine("PASS fixed timing, jitter bounds/variation/mean, invalid settings, and preference compatibility/persistence.");

sealed class RejectRandom : Random { public override double NextDouble() => throw new Exception("RNG used in fixed mode."); }
sealed class FixedRandom(double sample) : Random { public override double NextDouble() => sample; }
