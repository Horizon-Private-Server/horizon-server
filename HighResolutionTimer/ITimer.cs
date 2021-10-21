using System;

namespace Haukcode.HighResolutionTimer
{
    public interface ITimer : IDisposable
    {
        void SetPeriod(int periodMS);

        void WaitForTrigger();

        void Start();

        void Stop();
    }
}
