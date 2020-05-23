using System;

namespace Ws281x.Visualizer.Commands
{
    public interface ICommand : IDisposable
    {
        void Execute(string[] args);
    }
}