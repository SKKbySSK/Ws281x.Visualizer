using System;
using System.Threading.Tasks;

namespace Spectro.Core
{
    public interface IVisualizingOutput
    {
        TimeSpan? ActualLatency { get; }

        void Update(AnalysisResult result);
    }
}
