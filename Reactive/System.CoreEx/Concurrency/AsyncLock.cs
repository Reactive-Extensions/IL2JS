using System;
using System.Collections.Generic;
using System.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Diagnostics;


namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Concurrency
{
    public sealed class AsyncLock
    {
        Queue<Action> queue = new Queue<Action>();
        bool isAcquired = false;
        bool hasFaulted = false;

        public void Wait(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var isOwner = false;
            lock (queue)
            {
                if (!hasFaulted)
                {
                    queue.Enqueue(action);
                    isOwner = !isAcquired;
                    isAcquired = true;
                }
            }

            if (isOwner)
            {
                while (true)
                {
                    var work = default(Action);
                    lock (queue)
                    {
                        if (queue.Count > 0)
                            work = queue.Dequeue();
                        else
                        {
                            isAcquired = false;
                            break;
                        }
                    }

                    try
                    {
                        work();
                    }
                    catch (Exception ex)
                    {
                        lock (queue)
                        {
                            queue.Clear();
                            hasFaulted = true;
                        }
                        throw ex.PrepareForRethrow();
                    }
                }
            }
        }
    }
}
