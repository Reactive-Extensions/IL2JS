using System;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Linq;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Collections.Generic;

namespace ReactiveTests
{
    public class TestScheduler : IScheduler
    {
        ushort scheduled = 0;

        PriorityQueue<Item> queue = new PriorityQueue<Item>((x, y) =>
            {
                if (x.Time < y.Time)
                    return true;
                else if (x.Time > y.Time)
                    return false;
                else
                    return x.ID <= y.ID;
            });

        public IDisposable Schedule(Action action)
        {
            return Schedule(action, TimeSpan.Zero);
        }

        public IDisposable Schedule(Action action, ushort dueTime)
        {
            if (dueTime == 0)
                dueTime = 1;

            var disposable = new BooleanDisposable();

            var runAt = (ushort)Math.Min(Ticks + dueTime, ushort.MaxValue);

            var run = new Action(() =>
            {
                if (!disposable.IsDisposed)
                    action();
            });

            queue.Enqueue(new Item(runAt, scheduled++, run));

            return disposable;
        }

        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            var ticks = Math.Max(dueTime.Ticks, 0);
            return Schedule(action, (ushort)ticks);
        }

        public void Run()
        {
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                Ticks = Math.Max(item.Time, Ticks);
                item.Action();
            }
        }

        public void Sleep(TimeSpan ts)
        {
            Sleep((ushort)ts.Ticks);
        }

        public void Sleep(ushort ticks)
        {
            Ticks += ticks;
        }

        public ushort Ticks { get; private set; }

        public DateTimeOffset Now
        {
            get { return new DateTimeOffset(Ticks, TimeSpan.Zero); }
        }

        class Item
        {
            public ushort Time { get; private set; }
            public ushort ID { get; private set; }
            public Action Action { get; private set; }

            public Item(ushort time, ushort order, Action action)
            {
                Time = time;
                ID = order;
                Action = action;
            }
        }
    }
}
