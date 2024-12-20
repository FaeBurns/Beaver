using FFMpeg.Android;
using FFMpeg.Windows;

namespace FFMpeg
{
#if UNITY_EDITOR
    public class PlatformFFMpegService : WindowsFFMpegService
#else
    public class PlatformFFMpegService : AndroidFFMpegService
#endif
    {
    }
}