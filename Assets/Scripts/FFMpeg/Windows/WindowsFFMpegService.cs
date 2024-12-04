using System;

namespace FFMpeg.Windows
{
    public class WindowsFFMpegService : IFFMpegService
    {
        public void Execute(string args)
        {
            WindowsFFMpegExec exec = new WindowsFFMpegExec(args, true);
        }

        public void ExecuteAsync(string args)
        {
            WindowsFFMpegExec exec = new WindowsFFMpegExec(args);
        }
    }
}