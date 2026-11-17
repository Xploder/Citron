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
Check(LocalWakePolicy.Flags(true, false, true, false) == 0x80000001, "System-only mode must allow display timeout.");
Check(LocalWakePolicy.Flags(true, false, true, true) == 0x80000003, "Screen mode must request both system and display.");
foreach (bool screen in new[] { false, true })
{
    Check(LocalWakePolicy.Flags(false, false, true, screen) == 0x80000000, "Paused mode must release the request.");
    Check(LocalWakePolicy.Flags(true, true, true, screen) == 0x80000000, "Locking must release the request.");
    Check(LocalWakePolicy.Flags(true, false, false, screen) == 0x80000000, "Disabling local wake must release both requests.");
}
Check(!oldSettings.KeepDisplayOn, "Old settings must not implicitly enable the screen request.");
var displaySettings = JsonSerializer.Serialize(new Preferences { KeepAwake = true, KeepDisplayOn = true }, JsonContext.Default.Preferences);
Check(JsonSerializer.Deserialize(displaySettings, JsonContext.Default.Preferences)!.KeepDisplayOn, "Screen preference must persist.");
Console.WriteLine("PASS separate system/display requests, pause/lock/disable release, and screen-preference compatibility/persistence.");
Check(SessionPolicy.TargetExists("123", new[] { "123", "456" }), "Existing target must remain active.");
Check(!SessionPolicy.TargetExists("123", new[] { "456" }), "A different session must not keep the target active.");
Check(!SessionPolicy.TargetExists("123", Array.Empty<string>()), "An empty snapshot must stop activity.");
Check(!SessionPolicy.TargetExists(null, new[] { "123" }), "No target must not become active automatically.");
Check(SessionPolicy.CheckInterval == TimeSpan.FromSeconds(15), "Presence polling must be throttled.");
var executable = @"C:\Program Files\Citron\Citron.exe";
Check(StartupRegistration.Command(executable) == "\"" + executable + "\" --tray", "Startup must quote the real portable executable and request tray mode.");
Console.WriteLine("PASS session disappearance/selection isolation, polling cadence, and quoted startup command.");

sealed class RejectRandom : Random { public override double NextDouble() => throw new Exception("RNG used in fixed mode."); }
sealed class FixedRandom(double sample) : Random { public override double NextDouble() => sample; }
