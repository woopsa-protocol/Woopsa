using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Woopsa
{
    internal class CustomThreadPoolThread : IDisposable
    {
        public CustomThreadPoolThread(string threadName, ThreadPriority priority)
        {
            _startEvent = new AutoResetEvent(false);
            _terminateEvent = new ManualResetEvent(false);
            Priority = priority;
            _isIdle = true;
            ThreadName = threadName;
        }

        public void ExecuteUserWorkItem(WaitCallback callBack, object parameter = null)
        {
            if (_isIdle)
            {
                _isIdle = false;
                if (_thread == null)
                {
                    _thread = new Thread(Execute);
                    _thread.Priority = Priority;
                    _thread.Name = ThreadName;
                    _thread.Start();
                }
                _callBack = callBack;
                _parameter = parameter;
                _startEvent.Set();
            }
            else
                throw new Exception("Cannot execute user work item on a busy CustomThreadPoolThread");
        }
        public void Terminate()
        {
            _terminateEvent.Set();
        }

        public bool Join(TimeSpan timeout)
        {
            return _thread.Join(timeout);
        }

        public bool IsIdle { get { return _isIdle; } }

        public event EventHandler Idle;

        public ThreadPriority Priority { get; private set; }
        public string ThreadName { get; private set; }

        private void OnIdle()
        {
            _isIdle = true;
            if (Idle != null)
                Idle(this, new EventArgs());
        }
        private void Execute()
        {
            for (;;)
            {
                try
                {
                    int index = WaitHandle.WaitAny(new WaitHandle[] { _terminateEvent, _startEvent });
                    if (index == 0) // _terminateEvent
                        break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                _callBack(_parameter);
                OnIdle();
            }
        }

        public void Dispose()
        {
            Terminate();
            _thread.Join();
            _startEvent.Dispose();
            _terminateEvent.Dispose();
        }

        bool _isIdle;
        Thread _thread;
        AutoResetEvent _startEvent;
        ManualResetEvent _terminateEvent;
        WaitCallback _callBack;
        object _parameter;
    }
    public class CustomThreadPool : IDisposable
    {
        public const int DefaultThreadPoolSize = -1; // Use the operating system default value
        public CustomThreadPool(string name, int threadPoolSize = DefaultThreadPoolSize, ThreadPriority priority = ThreadPriority.Normal)
        {
            if (threadPoolSize == DefaultThreadPoolSize)
            {
                int n;
                ThreadPool.GetMaxThreads(out _threadPoolSize, out n);
            }
            else
            {
                Debug.Assert(threadPoolSize > 0);
                _threadPoolSize = threadPoolSize;
            }
            _threads = new List<CustomThreadPoolThread>();
            _semaphore = new Semaphore(_threadPoolSize, _threadPoolSize);
            Priority = priority;
            Name = name;
        }

        public ThreadPriority Priority { get; private set; }
        public string Name { get; private set; }

        public bool StartUserWorkItem(WaitCallback callBack, object parameter, TimeSpan timeout)
        {
            CustomThreadPoolThread thread = GetNextIdleThread(timeout);
            if (thread != null)
                thread.ExecuteUserWorkItem(callBack, parameter);
            return thread != null;
        }

        public bool StartUserWorkItem(WaitCallback callBack, object parameter = null)
        {
            return StartUserWorkItem(callBack, parameter, InfiniteTimeSpan);
        }

        public bool StartUserWorkItem(WaitCallback callBack, TimeSpan timeout)
        {
            return StartUserWorkItem(callBack, null, timeout);
        }

        private void OnIdle(object sender, EventArgs e)
        {
            _semaphore.Release();
        }
        private CustomThreadPoolThread GetNextIdleThread(TimeSpan timeout)
        {
            if (_semaphore.WaitOne(timeout))
            {
                lock (_threads)
                {
                    if (_aborting)
                        return null;
                    CustomThreadPoolThread thread = null;
                    foreach (var item in _threads)
                        if (item.IsIdle)
                        {
                            thread = item;
                            break;
                        }
                    if (thread == null)
                    {
                        Debug.Assert(_threads.Count <= _threadPoolSize);
                        _lastThreadIndex++;
                        thread = new CustomThreadPoolThread(Name + _lastThreadIndex.ToString(), Priority);
                        thread.Idle += OnIdle;
                        _threads.Add(thread);
                    }
                    return thread;
                }
            }
            else
                return null;
        }

        public void Terminate()
        {
            lock (_threads)
            {
                _aborting = true;
                foreach (var item in _threads)
                {
                    item.Terminate();
                }
            }
        }

        public bool Join(TimeSpan timeout)
        {
            DateTime waitStart = DateTime.Now;
            CustomThreadPoolThread[] threads;
            lock (_threads)
                threads = _threads.ToArray();
            _aborting = true;
            foreach (var item in threads)
            {
                TimeSpan remaining;
                if (timeout != InfiniteTimeSpan)
                {
                    TimeSpan elapsed = DateTime.Now - waitStart;
                    remaining = timeout - elapsed;
                    if (remaining < TimeSpan.Zero)
                        remaining = TimeSpan.Zero;
                }
                else
                    remaining = InfiniteTimeSpan;
                if (!item.Join(remaining))
                    return false;
            }

            return true;
        }
        public void Join()
        {
            Join(InfiniteTimeSpan);
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Terminate();
                foreach (var item in _threads)
                {
                    item.Dispose();
                }
                _semaphore.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private int _lastThreadIndex;
        private List<CustomThreadPoolThread> _threads;
        private Semaphore _semaphore;
        private bool _aborting;
        private int _threadPoolSize;

        static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, Timeout.Infinite);

    }
}
