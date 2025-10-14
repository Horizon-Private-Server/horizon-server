using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Haukcode.HighResolutionTimer
{
    internal class OsxTimer : ITimer, IDisposable
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly ManualResetEvent triggerEvent = new ManualResetEvent(false);
        private bool isRunning;
        private uint period;
        private Stopwatch sw = Stopwatch.StartNew();
        private long lastElapsedTicks;

        public OsxTimer()
        {
            lastElapsedTicks = sw.Elapsed.Ticks;
            ThreadPool.QueueUserWorkItem(Scheduler);
        }

        public void WaitForTrigger()
        {
            this.triggerEvent.WaitOne();
            this.triggerEvent.Reset();
        }

        public void SetPeriod(int periodMS)
        {
            SetFrequency((uint)periodMS * 1_000);
        }

        private void Scheduler(object state)
        {
            while (!this.cts.IsCancellationRequested)
            {
                Wait();

                if (this.isRunning)
                    this.triggerEvent.Set();
            }
        }

        private void SetFrequency(uint period)
        {
            Console.WriteLine("freq: " + period/1000);
            this.period = period;
        }

        private long Wait()
        {
            do
            {
                double microseconds = (double)(sw.ElapsedTicks - lastElapsedTicks) * 1_000_000 / Stopwatch.Frequency;
                var dt = period - microseconds;
                if (dt <= 0) break;

                Thread.Sleep(dt > 1000 ? 1 : 0);
            } while (true);

            lastElapsedTicks = sw.ElapsedTicks;

            return 0;
        }

        public void Dispose()
        {
            this.cts.Cancel();

            // Release trigger
            this.triggerEvent.Set();
        }

        public void Start()
        {
            this.isRunning = true;
        }

        public void Stop()
        {
            this.isRunning = false;
        }
    }
}
