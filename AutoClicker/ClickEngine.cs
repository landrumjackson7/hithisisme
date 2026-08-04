using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace AstryxAutoClicker
{
    public enum MouseButtonKind
    {
        Left,
        Right,
        Middle
    }

    /// <summary>
    /// Emits synthesized mouse clicks on a dedicated background thread using a
    /// high-resolution <see cref="Stopwatch"/> pacing loop so it can sustain rates
    /// far above the ~64 Hz ceiling of a WinForms timer (target range 1-10000 CPS).
    /// </summary>
    public sealed class ClickEngine
    {
        private readonly object sync = new object();
        private Thread worker;
        private volatile bool running;
        private volatile int targetCps = 10;
        private volatile int startDelayMs = 300;
        private volatile MouseButtonKind button = MouseButtonKind.Left;

        public bool IsRunning
        {
            get { return running; }
        }

        public int TargetCps
        {
            get { return targetCps; }
            set { targetCps = Clamp(value, 1, 10000); }
        }

        public MouseButtonKind Button
        {
            get { return button; }
            set { button = value; }
        }

        /// <summary>
        /// Grace period after <see cref="Start"/> before the first click fires, so the
        /// user can move the cursor off the Start button (which the click would otherwise hit).
        /// </summary>
        public int StartDelayMs
        {
            get { return startDelayMs; }
            set { startDelayMs = Clamp(value, 0, 5000); }
        }

        public void Start()
        {
            lock (sync)
            {
                if (running)
                {
                    return;
                }

                running = true;
                worker = new Thread(Loop);
                worker.IsBackground = true;
                worker.Priority = ThreadPriority.Highest;
                worker.Start();
            }
        }

        public void Stop()
        {
            Thread toJoin;
            lock (sync)
            {
                if (!running)
                {
                    return;
                }

                running = false;
                toJoin = worker;
                worker = null;
            }

            if (toJoin != null && toJoin.IsAlive && toJoin != Thread.CurrentThread)
            {
                toJoin.Join(500);
            }
        }

        public void Toggle()
        {
            if (running)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        private void Loop()
        {
            int delay = startDelayMs;
            while (running && delay > 0)
            {
                int slice = Math.Min(25, delay);
                Thread.Sleep(slice);
                delay -= slice;
            }

            long ticksPerSecond = Stopwatch.Frequency;
            var stopwatch = Stopwatch.StartNew();
            long next = stopwatch.ElapsedTicks;

            while (running)
            {
                long interval = ticksPerSecond / Math.Max(1, targetCps);
                Click(button);
                next += interval;

                // Pace to the next scheduled click. Sleep away the bulk of the wait
                // to stay CPU-friendly, then spin for sub-millisecond accuracy.
                long remaining = next - stopwatch.ElapsedTicks;
                if (remaining <= 0)
                {
                    // Behind schedule (very high CPS): resync to avoid runaway drift.
                    next = stopwatch.ElapsedTicks;
                    continue;
                }

                long oneMs = ticksPerSecond / 1000;
                if (remaining > 2 * oneMs)
                {
                    Thread.Sleep((int)((remaining - oneMs) / oneMs));
                }

                while (running && stopwatch.ElapsedTicks < next)
                {
                    Thread.SpinWait(16);
                }
            }
        }

        private static void Click(MouseButtonKind kind)
        {
            uint down;
            uint up;
            switch (kind)
            {
                case MouseButtonKind.Right:
                    down = NativeMethods.MOUSEEVENTF_RIGHTDOWN;
                    up = NativeMethods.MOUSEEVENTF_RIGHTUP;
                    break;
                case MouseButtonKind.Middle:
                    down = NativeMethods.MOUSEEVENTF_MIDDLEDOWN;
                    up = NativeMethods.MOUSEEVENTF_MIDDLEUP;
                    break;
                default:
                    down = NativeMethods.MOUSEEVENTF_LEFTDOWN;
                    up = NativeMethods.MOUSEEVENTF_LEFTUP;
                    break;
            }

            var inputs = new NativeMethods.INPUT[2];
            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].mi.dwFlags = down;
            inputs[1].type = NativeMethods.INPUT_MOUSE;
            inputs[1].mi.dwFlags = up;

            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
