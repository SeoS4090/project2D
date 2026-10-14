/// <summary>One fresh press, measured in unscaled seconds. Direction is resolved by the caller at execution.</summary>
public sealed class DashInputBuffer
{
    private bool pending;
    private double expiresAt;
    private double pressedAt;

    public void Press(double now, double window, int charges)
    {
        pending = charges > 0;
        pressedAt = now;
        expiresAt = now + window;
    }

    public bool Consume(double now, bool dashing, int charges)
    {
        if (!pending) return false;
        // Fixed-step catch-up must not consume a press from a later render timestamp.
        if (now < pressedAt) return false;
        if (now > expiresAt || charges <= 0) { Clear(); return false; }
        if (dashing) return false;
        Clear();
        return true;
    }

    public void Clear() { pending = false; pressedAt = expiresAt = 0; }
}
