using System.IO;
using System.Net;

namespace FFMpeg
{
    public interface IFFMpegService
    {
        public void Execute(string args);
        public void ExecuteAsync(string args);
        public Stream OpenStreamPipe(IPEndPoint server, int width, int height, int frameRate);
    }
}