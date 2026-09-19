using ManagedDoom;
static class HereticFrameChecks
{
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Verify()
    {
        foreach (var fps in new[] { 30, 60, 144, 240 })
        {
            var clock = new HereticFrameStepper(); var count = 0;
            for (var i = 0; i < fps * 10; i++) clock.Advance(1.0 / fps, default, _ => count++);
            Check(count == 350 && clock.TickCount == 350, "Heretic tick rate depends on frame rate.");
        }
        var stepper = new HereticFrameStepper(); var commands = new List<HereticCommand>();
        stepper.Advance(0.001, new HereticCommand { Use = true, CenterLook = true, Land = true }, commands.Add);
        stepper.Advance(0.03, default, commands.Add);
        Check(commands.Count == 1 && commands[0].Use && commands[0].CenterLook && commands[0].Land, "Short action lost between ticks.");
        stepper.Advance(0.001, new HereticCommand { Use = true }, commands.Add);
        stepper.Advance(0.06, default, commands.Add);
        Check(commands.Count == 3 && !commands[1].Use && commands[2].Use, "Second use press lacks release edge.");
        Check(!commands[1].CenterLook && !commands[2].Land, "One-shot action repeated.");
        stepper = new HereticFrameStepper(); commands.Clear();
        for (var i = 0; i < 60; i++) stepper.Advance(1.0 / 60, new HereticCommand { Use = true }, commands.Add);
        Check(commands.Count == 35 && commands.All(c => c.Use), "Held use introduced false edges.");
        var before = stepper.TickCount;
        stepper.Advance(30, default, commands.Add);
        Check(stepper.TickCount - before <= 9, "Stalled frame caused unbounded catch-up.");
        foreach (var invalid in new[] { -1.0, double.NaN, double.PositiveInfinity })
        {
            before = stepper.TickCount;
            try { stepper.Advance(invalid, default, commands.Add); throw new Exception("Invalid time accepted."); }
            catch (ArgumentOutOfRangeException) { }
            Check(before == stepper.TickCount, "Invalid time changed simulation.");
        }
        Console.WriteLine("PASS Heretic frame scheduling: 30/60/144/240 FPS, short taps, repeat/held use, one-shot actions and stall bounds");
    }
}
