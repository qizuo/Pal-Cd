using System;
using Houyi.NineSuns;

class ShiftInputCheck
{
    static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
    static void Main()
    {
        var input = new PrologueShiftInput();
        Check(!input.Process(true, 0, true) && !input.Sprinting, "press waits for tap or hold");
        Check(input.Process(false, .1f, true) && !input.Sprinting, "short release dodges");
        Check(!input.Process(false, .11f, true), "tap only fires once");
        input.Process(true, 1, true);
        Check(!input.Process(true, 1.3f, true) && input.Sprinting, "hold sprints without dodge");
        Check(!input.Process(false, 1.4f, true) && !input.Sprinting, "sprint release never dodges");
        input.Process(true, 2, true);
        Check(!input.Process(false, 3, true), "long press between frames never becomes tap");
        input.Process(true, 4, true); input.Process(true, 4.05f, false);
        Check(!input.Process(false, 4.1f, true), "pause suppresses pending tap");
        input.Process(true, 5, false);
        Check(!input.Process(true, 5.4f, true) && !input.Sprinting, "held key on resume must release first");
        Check(!input.Process(false, 5.5f, true), "suppressed release never dodges");
        input.Process(true, 6, true); Check(input.Process(false, 6.1f, true), "fresh tap after resume works");
        input.Process(true, 7, true); input.Cancel(true);
        Check(!input.Process(false, 7.1f, true), "focus loss cancels pending dodge");
    }
}
