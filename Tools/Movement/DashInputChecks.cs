using System;

static class DashInputChecks
{
    static int passed;
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        passed++; Console.WriteLine("PASS " + name);
    }
    static void Main()
    {
        var b = new DashInputBuffer();
        Check(!b.Consume(0, false, 2), "No press never starts dash");
        b.Press(1, .15, 2);
        Check(b.Consume(1, false, 2), "Idle fresh press starts immediately");
        Check(!b.Consume(1.01, false, 1), "Held input cannot repeat consumed press");
        b.Press(2, .15, 1);
        Check(!b.Consume(2.05, true, 1), "Current dash retains reservation");
        Check(b.Consume(2.1, false, 1), "End of dash consumes valid reservation");
        b.Press(3, .15, 1);
        Check(!b.Consume(3.16, false, 1), "Real-time expiry rejects stale press");
        b.Press(4, .15, 0);
        Check(!b.Consume(4.01, false, 1), "Zero-charge press cannot revive after recharge");
        b.Press(5, .15, 1);
        Check(!b.Consume(5.01, true, 0) && !b.Consume(5.02, false, 1), "Lost charge clears reservation");
        b.Press(6, .15, 1); b.Press(6.1, .15, 1);
        Check(b.Consume(6.2, false, 1) && !b.Consume(6.21, false, 1), "Latest press refreshes one reservation only");
        b.Press(7, .15, 1); b.Clear();
        Check(!b.Consume(7.01, false, 1), "Reset clears reservation");
        b.Press(8, 0, 1);
        Check(b.Consume(8, false, 1), "Zero window supports immediate press");
        b.Press(9, .125, 1);
        Check(b.Consume(9.125, false, 1), "Exact expiry boundary is accepted");
        Console.WriteLine(passed + " checks passed; no Unity physics or live input exercised.");
        DashMotionChecks.Run();
    }
}
