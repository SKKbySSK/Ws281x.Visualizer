using System;

namespace Spectro.Core
{
    public class ValueAnimator<T>
    {
        private DateTime? _lastTick = null;
        
        public ValueAnimator(Func<double, T> updateCallback)
        {
            UpdateCallback = updateCallback;
        }

        public T From
        {
            get => _from;
            set
            {
                _from = value;
                _lastTick = DateTime.Now;
            }
        }

        public T To
        {
            get => _to;
            set
            {
                _to = value;
                _lastTick = DateTime.Now;
            }
        }

        public T Current { get; set; }
        
        public Func<double, T> UpdateCallback { get; }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                _lastTick = DateTime.Now;
            }
        }

        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(100);

        private double _progress;
        private T _to;
        private T _from;

        public void Tick()
        {
            if (_lastTick == null)
            {
                _lastTick = DateTime.Now;
            }

            var delta = DateTime.Now - _lastTick.Value;
            var prog = delta.TotalMilliseconds / Duration.TotalMilliseconds;
            _progress = Math.Min(1, Math.Max(0, prog));
            Current = UpdateCallback(_progress);
        }
    }
}