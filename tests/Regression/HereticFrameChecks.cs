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
        foreach (var fps in new[]{30,60,144,240})
        {
            var mouse=new HereticFrameStepper();int total=0;
            for(int i=0;i<fps;i++){mouse.QueueFrameTurn(1200.0/fps);mouse.Advance(1.0/fps,default,c=>total+=c.Turn);}
            // Flush any fractional scheduling remainder, with no new mouse motion.
            mouse.Advance(1.0/35,default,c=>total+=c.Turn);
            Check(Math.Abs(total-1200)<=1,"Mouse turn depends on frame rate.");
        }
        var burst=new HereticFrameStepper();commands.Clear();burst.QueueFrameTurn(900);
        burst.Advance(3.0/35,new HereticCommand{Turn=10},commands.Add);
        Check(commands.Count==3 && commands[0].Turn==910 && commands[1].Turn==10 && commands[2].Turn==10,"Frame mouse motion repeated across ticks.");
        commands.Clear();burst.QueueFrameTurn(.4);burst.Advance(1.0/35,default,commands.Add);
        burst.QueueFrameTurn(.7);burst.Advance(1.0/35,default,commands.Add);
        Check(commands[0].Turn==0 && commands[1].Turn==1,"Fractional mouse motion lost.");
        burst.QueueFrameTurn(1000);burst.SetPaused(true);burst.QueueFrameTurn(500);burst.SetPaused(false);commands.Clear();
        burst.Advance(1.0/35,default,commands.Add);Check(commands[0].Turn==0,"Paused mouse motion leaked on resume.");
        burst.QueueFrameTurn(1000);burst.ClearFrameTurn();commands.Clear();burst.Advance(1.0/35,default,commands.Add);
        Check(commands[0].Turn==0,"Cleared automap/released mouse motion leaked.");
        burst.QueueFrameTurn(40000);commands.Clear();burst.Advance(2.0/35,new HereticCommand{Turn=1000},commands.Add);
        Check(commands.Sum(c=>(int)c.Turn)==42000 && commands[0].Turn==short.MaxValue,"Mouse/keyboard saturation wrapped or discarded turn.");
        bool invalidTurn=false;try{burst.QueueFrameTurn(double.NaN);}catch(ArgumentOutOfRangeException){invalidTurn=true;}
        Check(invalidTurn,"Nonfinite mouse input accepted.");
        Console.WriteLine("PASS Heretic frame mouse turning: frame rates, one-time consumption, fractions, pause/clear and saturated keyboard composition");
        Console.WriteLine("PASS Heretic frame scheduling: 30/60/144/240 FPS, short taps, repeat/held use, one-shot actions and stall bounds");
    }
}
