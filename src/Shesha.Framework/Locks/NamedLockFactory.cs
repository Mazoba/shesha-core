using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Shesha.Locks
{
    public class NamedLockFactory : ILockFactory
    {

        private readonly ConcurrentDictionary<string, object> _lockDict = new ConcurrentDictionary<string, object>();

        public Task<bool> DoExclusiveAsync(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, Func<Task> action)
        {
            var lockObj = _lockDict.GetOrAdd(resource, s => new object());

            bool lockTaken = false;
            bool isAcquired = false;

            try
            {
                Monitor.TryEnter(lockObj, waitTime, ref lockTaken);
                if (lockTaken)
                {
                    var task = action.Invoke();
                    try
                    {
                        if (expiryTime == TimeSpan.MinValue || expiryTime == TimeSpan.MaxValue)
                        {
                            task.Wait();
                            isAcquired = true;
                        }
                        else
                        {
                            isAcquired = task.Wait(expiryTime);
                            if (!isAcquired)
                                throw new TimeoutException("The time allotted for a locked operation has expired.");
                        }
                    }
                    finally
                    {
                        if (task.IsCompleted)
                            task.Dispose();
                    }
                }
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(lockObj);
            }

            return Task.FromResult(isAcquired);
        }

        public bool DoExclusive(string resource, TimeSpan expiryTime, TimeSpan waitTime, TimeSpan retryTime, Action action)
        {
            var lockObj = _lockDict.GetOrAdd(resource, s => new object());

            bool lockTaken = false;
            bool isAcquired = false;

            try
            {
                Monitor.TryEnter(lockObj, waitTime, ref lockTaken);
                if (lockTaken)
                {
                    // using separate task to monitor expiry time
                    var task = Task.Factory.StartNew(action.Invoke);
                    try
                    {
                        if (expiryTime == TimeSpan.MinValue || expiryTime == TimeSpan.MaxValue)
                        {
                            task.Wait();
                            isAcquired = true;
                        }
                        else
                        {
                            isAcquired = task.Wait(expiryTime);
                            if (!isAcquired)
                                throw new TimeoutException("The time allotted for a locked operation has expired.");
                        }
                    }
                    finally
                    {
                        if (task.IsCompleted)
                            task.Dispose();
                    }
                }
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(lockObj);
            }

            return isAcquired;
        }
    }
}