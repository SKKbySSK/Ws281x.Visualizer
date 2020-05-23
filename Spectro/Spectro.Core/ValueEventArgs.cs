namespace Spectro.Core
{
    public delegate void ValueEventHandler<T>(object sender, ValueEventArgs<T> e);
    
    public class ValueEventArgs<T>
    {
        public ValueEventArgs(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
