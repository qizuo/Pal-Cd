namespace Houyi.NineSuns
{
    // Release a short press to dodge; a held press becomes sprint and never dodges on release.
    public sealed class PrologueShiftInput
    {
        public const float HoldDelay = .22f;
        bool tracking, blockedUntilRelease;
        float pressedAt;
        public bool Sprinting { get; private set; }

        public bool Process(bool held, float time, bool enabled)
        {
            if (!enabled) { Cancel(held); return false; }
            if (blockedUntilRelease)
            {
                if (!held) blockedUntilRelease = false;
                return false;
            }
            if (held)
            {
                if (!tracking) { tracking = true; pressedAt = time; }
                Sprinting = time - pressedAt >= HoldDelay;
                return false;
            }
            bool tap = tracking && time - pressedAt < HoldDelay;
            tracking = false; Sprinting = false;
            return tap;
        }
        public void Cancel(bool held)
        {
            tracking = Sprinting = false;
            blockedUntilRelease = held;
        }
    }
}
