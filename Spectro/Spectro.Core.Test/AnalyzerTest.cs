using System;
using System.Linq;
using NUnit.Framework;

namespace Spectro.Core.Test
{
    public class AnalyzerTest
    {
        private int sampleRate = 100 * 40;
        private Analyzer analyzer;
        
        [SetUp]
        public void Setup()
        {
            analyzer = new Analyzer(4096, sampleRate);
            Console.WriteLine("Resolution : " + analyzer.FrequencyResolution + "Hz");
        }

        [Test]
        public void TestDBFS()
        {
            var sine = MathNet.Numerics.Generate.SinusoidalSequence(sampleRate, 500, 1);
            analyzer.Fft(sine.Take(8000).Select(d => (byte)(d * Byte.MaxValue)).ToArray(), 0, 16);
            var index1 = analyzer.GetIndex(490);
            var index2 = analyzer.GetIndex(510);
            var dbfs = analyzer.GetDBFS(index1, index2);

            foreach (var value in dbfs)
            {
                Console.WriteLine(value);
            }
        }
    }
}