using System;

static class DashMotionChecks
{
    static int passed;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        passed++;
    }
    static void Near(double actual, double expected, string name) =>
        Check(Math.Abs(actual - expected) < 1e-6, $"{name}: {actual:R} != {expected:R}");

    // Unity ordering: zero or more physics steps, then a rendered Update which samples input.
    // Uses production clock/buffer. This is an unobstructed numerical integration, not PhysX/Box2D.
    static (double distance, double x, int starts) Replay(int fps, double fixedDt, double duration, bool chain, bool hitch, bool reverse = false)
    {
        var clock = new DashMotionClock();
        var buffer = new DashInputBuffer();
        int charges = 2, starts = 1;
        double render = 0, fixedEnd = fixedDt, distance = 0, x = 0;
        int direction = 1, inputDirection = 1;
        bool sent = false, hitched = false;
        clock.Start(duration); charges--;
        while (render < 1)
        {
            double renderDt = 1d / fps;
            if (hitch && !hitched && render >= .14) { renderDt = .12; hitched = true; }
            render += renderDt;
            while (fixedEnd <= render + 1e-10)
            {
                var step = clock.Advance(fixedDt, fixedEnd - fixedDt, fixedDt, duration, buffer, charges);
                double displacement = direction * 13 * step.PreviousDashSeconds;
                if (step.StartedNext) { charges--; starts++; direction = inputDirection; }
                displacement += direction * 13 * step.NextDashSeconds;
                x += displacement;
                // The Rigidbody receives one averaged velocity, hence one net displacement per tick.
                distance += Math.Abs(displacement);
                Near(step.PreviousDashSeconds + step.NextDashSeconds + step.MoveSeconds, fixedDt, "interval conserved");
                fixedEnd += fixedDt;
            }
            if (chain && !sent && render >= .10)
            {
                inputDirection = reverse ? -1 : 1;
                buffer.Press(render, .15, charges); sent = true;
                if (buffer.Consume(render, clock.IsDashing, charges))
                { clock.Start(duration); charges--; starts++; direction = inputDirection; }
            }
        }
        return (distance, x, starts);
    }

    static double LegacyDistance(int fps, double fixedDt, double duration)
    {
        double render = 0, end = fixedDt, remaining = duration, distance = 0;
        while (render < .6)
        {
            render += 1d / fps;
            while (end <= render + 1e-10)
            { if (remaining > 0) distance += 13 * fixedDt; end += fixedDt; }
            remaining = Math.Max(0, remaining - 1d / fps);
        }
        return distance;
    }

    public static void Run()
    {
        // Match the float duration actually loaded into TrainingPlayerController.
        double duration = (float).16;
        Console.WriteLine("FPS | fixed seconds | single distance | chained path | starts | legacy single");
        foreach (int fps in new[] { 30, 60, 120 })
        foreach (double dt in new[] { (double)(float).02, (double)(float)(1d / 60), (double)(float)(1d / 30) })
        {
            var single = Replay(fps, dt, duration, false, false);
            var chain = Replay(fps, dt, duration, true, false);
            Near(single.distance, 13 * duration, "single distance");
            Near(chain.distance, 26 * duration, "chained path distance");
            var reversed = Replay(fps, dt, duration, true, false, true);
            Near(reversed.x, 0, "second dash uses newly sampled opposite direction");
            Check(reversed.starts == 2, "opposite direction consumes reservation once");
            Check(single.starts == 1 && chain.starts == 2, "one press per dash");
            var hitch = Replay(fps, dt, duration, true, true);
            Near(hitch.distance, 26 * duration, "hitch catch-up retains boundary reservation");
            Check(hitch.starts == 2, "hitch consumes one reservation");
            var partial = Replay(fps, dt, .173, true, false);
            Near(partial.distance, 26 * .173, "non-integral duration");
            Console.WriteLine($"{fps} | {dt:F6} | {single.distance:F6} | {chain.distance:F6} | {chain.starts} | {LegacyDistance(fps, dt, duration):F6}");
        }

        var clock = new DashMotionClock(); var input = new DashInputBuffer();
        clock.Start(.015);
        var tail = clock.Advance(.02, 0, .02, .16, input, 1);
        Near(tail.PreviousDashSeconds, .015, "partial last dash interval");
        Near(tail.MoveSeconds, .005, "normal movement gets tail only");
        Near(13 * tail.PreviousDashSeconds + 5 * tail.MoveSeconds, .22, "weighted tail displacement");
        clock.Start(.015); input.Press(0, .15, 1);
        var connected = clock.Advance(.02, 0, .02, .16, input, 1);
        Check(connected.StartedNext, "chain starts inside physics interval");
        Near(connected.NextDashSeconds, .005, "chain inherits remaining interval");
        Near(clock.Remaining, .155, "new dash clock matches consumed movement");
        clock.Clear(); input.Clear();
        Near(clock.Advance(.02, 0, .02, .16, input, 2).MoveSeconds, .02, "reset leaves no dash");

        foreach (double offset in new[] { -.0001, 0, .0001 })
        {
            clock.Start(.02); input.Press(.01, .01 + offset, 1);
            var boundary = clock.Advance(.02, 0, .02, .16, input, 1);
            Check(boundary.StartedNext == (offset >= 0), "expiry before/at/after boundary");
        }
        clock.Clear(); input.Press(.03, .15, 1);
        Check(!clock.Advance(.02, 0, .02, .16, input, 1).StartedNext, "future press not consumed in catch-up");
        Check(clock.Advance(.02, .04, .02, .16, input, 1).StartedNext, "future press retained until eligible");
        clock.Start(.01); input.Press(0, .15, 0);
        Check(!clock.Advance(.02, 0, .02, .16, input, 1).StartedNext, "zero-charge input does not revive");
        clock.Start(.01); input.Press(0, .015, 1);
        Check(!clock.Advance(.02, 0, .04, .16, input, 1).StartedNext, "slow motion expiry stays unscaled");
        clock.Start(.1); input.Clear();
        clock.Advance(0, 0, 0, .16, input, 1);
        Near(clock.Remaining, .1, "zero elapsed simulation time freezes motion");
        Console.WriteLine($"{passed} motion assertions passed (includes per-interval conservation).");
    }
}
