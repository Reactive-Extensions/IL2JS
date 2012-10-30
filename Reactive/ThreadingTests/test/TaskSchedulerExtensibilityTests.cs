using System;
using System.Linq;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Text;

// TPL namespaces
using System.Threading;
using System.Threading.Tasks;

// CDS namespaces
using System.Collections.Concurrent;

namespace plinq_devtests
{
    //////////////////////////////////////////////////////////////////////////////////////////
    //
    // Sample for a custom TaskScheduler implementation. This is both meant as a proof concept for
    // the extensible scheduler, and will be utilized in dev unit tests that focus on the task scheduler
    // arguments in various TPL APIS
    //
    // The CustomThreadsTaskScheduler provides a basic thread based scheduler, and uses a blocking collection
    // as its global task queue. Blocking shutdown is supported through the IDisposable interface.
    //
    public class CustomThreadsTaskScheduler : TaskScheduler, IDisposable
    {
        private List<Thread> _threads;
        private CountdownEvent _shutdownEvent;

        private BlockingCollection<Task> _tasks =  new BlockingCollection<Task>();

        public CustomThreadsTaskScheduler(): this( Environment.ProcessorCount)
        {
        }

        public CustomThreadsTaskScheduler(int threadCount) 
        {
            if (threadCount <= 0)
            {
                throw new ArgumentOutOfRangeException("threadCount");
            }
            
            _threads = Enumerable.Range(0, threadCount).Select(i =>
                {
                    Thread t = new Thread(() =>
                    { 
                        this.DispatchLoop();
                    });

                    t.IsBackground = true;
                    t.Start();
                    return t;
                }).ToList();

            _shutdownEvent = new CountdownEvent(_threads.Count);
        }

        protected void DispatchLoop()
        {
            foreach(var task in _tasks.GetConsumingEnumerable()) 
            {
                TryExecuteTask(task); // this can throw
            }

            _shutdownEvent.Signal();
        }

        protected BlockingCollection<Task> Tasks { get { return _tasks; } }

        // The method we are overriding, TaskScheduler.QueueTask is "protected internal". That means that any
        // overriding method must be marked as "protected internal" if it is in the assembly that defines
        // TaskScheduler, or an assembly that can see internals of the assembly that defines TaskScheduler.
        // Otherwise, the overriding QueueTask must be just "protected".
        //
        // The legacy build assembly exposes its internals to dev unit tests. On the other hand, in our regular
        // build, mscorlib.dll does not expose its internals to the tests (unlike System.Core.dll). Hence, we have
        // to mark QueueTask with a different modifier in each case.
#if PFX_LEGACY_3_5
        protected internal override void QueueTask(Task task) 
#else
        protected override void QueueTask(Task task)
#endif
        { 
            _tasks.Add(task); 
        }

        public override int MaximumConcurrencyLevel 
        { 
            get { return _threads.Count; } 
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyScheduled) 
        { 
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks() 
        { 
            return _tasks; 
        }

        public void Dispose()
        {
            _tasks.CompleteAdding();
            _shutdownEvent.Wait();
        }
    }


}
