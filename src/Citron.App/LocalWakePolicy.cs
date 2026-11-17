namespace Citron;
internal static class LocalWakePolicy
{
    internal static uint Flags(bool active, bool locked, bool keepAwake, bool keepDisplayOn)
    {
        const uint continuous = 0x80000000;
        if (!active || locked || !keepAwake) return continuous;
        return continuous | 1u | (keepDisplayOn ? 2u : 0u);
    }
}
