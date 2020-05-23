using NUnit.Framework;

namespace Spectro.Core.Test
{
    public class BufferSinkTest
    {
        [Test]
        public void Test()
        {
            var sink = new BufferSink<int>(10);

            var step = 7;
            for (int i = 0; 2048 > i; i += step)
            {
                var data = new int[step];
                for (int j = 0; j < step; j++)
                {
                    data[j] = i + j;
                }
                
                sink.PushCopied(data, 0, data.Length, false);
            }

            int index = 0;
            while (sink.IsFilled)
            {
                var buffer = sink.Pop();
                foreach (var element in buffer)
                {
                    Assert.AreEqual(index++, element);
                }
            }
        }
    }
}