namespace Spectro.Core
{
    public class Range
    {
        public Range(double from, double to)
        {
            From = from;
            To = to;
        }

        public Range()
        {
        }

        public double From { get; set; }
        
        public double To { get; set; }
    }
}