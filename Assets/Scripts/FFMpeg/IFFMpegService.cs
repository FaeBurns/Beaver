using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FFMpeg
{
    public interface IFFMpegService
    {
        public void Execute(string args);
        public void ExecuteAsync(string args);
        public Task<Stream> OpenStreamServer(IPEndPoint server, int width, int height, int frameRate);
    }
}