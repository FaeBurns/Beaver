namespace FFMpeg
{
    public interface IFFMpegService
    {
        public void Execute(string args);
        public void ExecuteAsync(string args);
    }
}