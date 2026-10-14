using System;

/// <summary>Consumes dash time only when a physics interval is integrated.</summary>
public sealed class DashMotionClock
{
    public double Remaining { get; private set; }
    public bool IsDashing => Remaining > 0;

    public void Start(double duration) => Remaining = duration;
    public void Clear() => Remaining = 0;

    public readonly struct Step
    {
        public readonly double PreviousDashSeconds, NextDashSeconds, MoveSeconds;
        public readonly bool StartedNext;
        public Step(double previous, double next, double move, bool started)
        { PreviousDashSeconds = previous; NextDashSeconds = next; MoveSeconds = move; StartedNext = started; }
    }

    public Step Advance(double dt, double unscaledStart, double unscaledDt,
        double duration, DashInputBuffer input, int charges)
    {
        if (dt <= 0) return new Step(0, 0, 0, false);
        double previous = Math.Min(Remaining, dt);
        Remaining = Math.Max(0, Remaining - previous);
        double left = dt - previous;
        // A reservation is evaluated at the actual dash boundary, not the render frame.
        bool started = input.Consume(unscaledStart + unscaledDt * (previous / dt), IsDashing, charges);
        double next = 0;
        if (started)
        {
            Start(duration);
            next = Math.Min(Remaining, left);
            Remaining = Math.Max(0, Remaining - next);
            left -= next;
        }
        return new Step(previous, next, left, started);
    }
}
