using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveTests.Mocks
{
    class MockDisposable : List<int>, IDisposable
    {
        TestScheduler scheduler;

        public MockDisposable(TestScheduler scheduler)
        {
            this.scheduler = scheduler;
            Add(scheduler.Ticks);
        }

        public void Dispose()
        {
            Add(scheduler.Ticks);
        }
    }
}
