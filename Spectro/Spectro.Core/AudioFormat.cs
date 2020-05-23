namespace Spectro.Core
{
    public class AudioFormat
    {
        public AudioFormat(int sampleRate = 44100, int channels = 2, int bitDepth = 16, bool signed = true)
        {
            SampleRate = sampleRate;
            Channels = channels;
            BitDepth = bitDepth;
            Signed = signed;
        }

        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public int BitDepth { get; set; }
        
        public bool Signed { get; set; }
    }

    public enum FormatResult
    {
        Ok,
        UnsupportedSampleRate,
        UnsupportedChannel,
        UnsupportedBitDepth,
    }
}
