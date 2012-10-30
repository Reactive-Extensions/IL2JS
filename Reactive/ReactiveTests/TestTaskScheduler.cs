#if !SILVERLIGHT && !NETCF37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveTests
{
    class TestTaskScheduler : TaskScheduler
    {
        protected override void QueueTask(Task task)
        {
            TryExecuteTaskInline(task, false);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return null;
        }
    }
}
#endif