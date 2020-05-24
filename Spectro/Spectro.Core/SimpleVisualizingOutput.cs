using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Spectro.Core
{
    public abstract class SimpleVisualizingOutput : IVisualizingOutput
    {
        class AnalysisItem
        {
            public AnalysisItem(AnalysisResult result, Stopwatch stopwatch)
            {
                Result = result;
                Stopwatch = stopwatch;
            }

            public Stopwatch Stopwatch { get; }
        
            public AnalysisResult Result { get; }
        }
        
        private Thread _thread;
        private object _threadLock = new object();
        private bool _start = false;
        private ConcurrentQueue<AnalysisItem> _results = new ConcurrentQueue<AnalysisItem>();
        
        public TimeSpan? ActualLatency { get; } = TimeSpan.FromMilliseconds(1);

        public TimeSpan WriteDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        public TimeSpan WriteLimit { get; set; } = TimeSpan.FromMilliseconds(30);

        public bool Enabled { get; set; } = true;

        public void Start()
        {
            if (_start)
            {
                return;
            }
            
            lock (_threadLock)
            {
                _start = true;
                _thread = new Thread(RunThread);
                _thread.Start();
            }
        }

        public void Stop(bool waitForExit = false)
        {
            _start = false;
            if (waitForExit)
            {
                lock (_threadLock) { }
            }
        }

        private void RunThread()
        {
            lock (_threadLock)
            {
                ThreadStarted();
                
                while (_start)
                {
                    bool handled = false;
                    while (_results.Count >= 1)
                    {
                        handled = false;
                        if (_results.TryPeek(out var item))
                        {
                            if (item.Stopwatch.Elapsed >= WriteDelay && _results.TryDequeue(out _))
                            {
                                var delta = item.Stopwatch.Elapsed - WriteDelay;
                                item.Stopwatch.Stop();
                                if (delta <= WriteLimit)
                                {
                                    WriteLed(item.Result);
                                }
                                handled = true;
                            }
                        }

                        if (!handled)
                        {
                            break;
                        }
                    }

                    if (!handled)
                    {
                        Thread.Yield();
                    }
                }
                
                ThreadFinished();
            }
        }

        protected abstract void WriteLed(AnalysisResult result);

        protected virtual void ThreadStarted()
        {
        }

        protected virtual void ThreadFinished()
        {
        }

        public void Update(AnalysisResult result)
        {
            if (!Enabled)
            {
                return;
            }
            
            var sw = new Stopwatch();
            sw.Start();
            _results.Enqueue(new AnalysisItem(result, sw));
        }

        ~SimpleVisualizingOutput()
        {
            Stop(true);
        }
    }
}