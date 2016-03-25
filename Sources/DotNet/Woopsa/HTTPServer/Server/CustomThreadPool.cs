using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Woopsa
{
    internal class CustomThreadPoolThread : IDisposable
    {
        public CustomThreadPoolThread(ThreadPriority priority)
        {
            _startEvent = new AutoResetEvent(false);
            Priority = priority;
            _isIdle = true;
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
            if (!_aborting)
            {
                _aborting = true;
                if (_startEvent != null)
                    _startEvent.Dispose();
            }
        }

        public bool Join(TimeSpan timeout)
        {
            return _thread.Join(timeout);
        }

        public bool IsIdle { get { return _isIdle; } }

        public event EventHandler Idle;

        public ThreadPriority Priority { get; private set; }

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
                    _startEvent.WaitOne();
                    if (_aborting)
                        break;
                    _callBack(_parameter);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                OnIdle();
            }
        }

        public void Dispose()
        {
            Terminate();
        }

        bool _aborting;
        bool _isIdle;
        Thread _thread;
        AutoResetEvent _startEvent;
        WaitCallback _callBack;
        object _parameter;
    }
    public class CustomThreadPool : IDisposable
    {
        public const int DefaultThreadPoolSize = -1; // Use the operating system default value
        public CustomThreadPool(int threadPoolSize = DefaultThreadPoolSize, ThreadPriority priority = ThreadPriority.Normal)
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
        }

        public ThreadPriority Priority { get; private set; }
        public bool StartUserWorkItem(WaitCallback callBack, object parameter, TimeSpan timeout)
        {
            CustomThreadPoolThread thread = GetNextIdleThread(timeout);
            if (thread != null)
                thread.ExecuteUserWorkItem(callBack, parameter);
            return thread != null;
        }

        public bool StartUserWorkItem(WaitCallback callBack, object parameter = null)
        {
            return StartUserWorkItem(callBack, parameter, Timeout.InfiniteTimeSpan);
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
                        thread = new CustomThreadPoolThread(Priority);
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
            
            lock (_threads)
            {
                _aborting = true;
                foreach (var item in _threads)
                {
                    TimeSpan remaining;
                    if (timeout != Timeout.InfiniteTimeSpan)
                    {
                        TimeSpan elapsed = DateTime.Now - waitStart;
                        remaining = timeout - elapsed;
                        if (remaining < TimeSpan.Zero)
                            remaining = TimeSpan.Zero;
                    }
                    else
                        remaining = Timeout.InfiniteTimeSpan;
                    if (!item.Join(remaining))
                        return false;
                }
            }
            return true;
        }
        public void Join()
        {
            Join(Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            Terminate();
            foreach (var item in _threads)
            {
                item.Dispose();
            }
        }

        private List<CustomThreadPoolThread> _threads;        
        private Semaphore _semaphore;
        private bool _aborting;
        private int _threadPoolSize;
    }
}
