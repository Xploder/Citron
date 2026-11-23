namespace Citron;
internal sealed class TrayClickSequence
{
    long? due;
    bool ignoreRelease;
    internal void ButtonDown() => ignoreRelease = false;
    internal bool QueueSingle(long now, uint doubleClickTime)
    {
        if (ignoreRelease || due.HasValue) return false;
        due = now + doubleClickTime;
        return true;
    }
    internal void DoubleClick() { due = null; ignoreRelease = true; }
    internal void Cancel() { due = null; ignoreRelease = false; }
    internal bool ConsumeSingle(long now)
    {
        if (!due.HasValue || now < due.Value) return false;
        due = null;
        return true;
    }
}
