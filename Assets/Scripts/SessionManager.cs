using System.IO;
using System.Net;
using System.Threading.Tasks;
using FFMpeg;
using JetBrains.Annotations;
using Network;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class SessionManager : MonoBehaviour
{
    [CanBeNull]
    private StreamToTextureHandler m_streamToTextureHandler;

    [SerializeField]
    private RenderToVrScreen m_vrScreenTarget;

    public float ResolutionMultiplier = 1.0f;

    private readonly ClientSessionCommunicator m_session = new ClientSessionCommunicator();

    private async void Start()
    {
        File.Delete(Application.persistentDataPath + "/log.txt");
        Application.logMessageReceived += ApplicationOnlogMessageReceived;

        Debug.Log("Waiting for XR to start");
        // wait for xr to start
        // https://discussions.unity.com/t/how-can-i-get-actual-screen-resolution-of-vr-while-using-pc-streaming/902254/3
        // not sure if necessary but gonna keep
        while (XRSettings.eyeTextureWidth == 0)
            await Awaitable.NextFrameAsync();

        Debug.Log("Trying to start stream client...");
        while (Application.isPlaying)
        {
            if (await BeginStream())
                break;
            await Awaitable.NextFrameAsync();
        }
    }

    private async Task<bool> BeginStream()
    {
        await m_session.ConnectAsync(9944);

#if UNITY_EDITOR
        // constant for editor
        float framerate = 90;
#else
        // float framerate = 72;
        // Unity.XR.Oculus.Performance.TryGetDisplayRefreshRate(out float framerate);
        float framerate = 90;
#endif
        Debug.Log($"Read Target: {Application.targetFrameRate}");

        // multiply width by 2 to allow for both eyes
        int width = (int)(XRSettings.eyeTextureWidth * 2 * ResolutionMultiplier);
        int height = (int)(XRSettings.eyeTextureHeight * ResolutionMultiplier);

        Application.targetFrameRate = (int)framerate;

        Debug.Log("[SessionManager] Negotiating connection");
        Debug.Log($"[SessionManager] Sending {width}x{height} @{framerate}");
        if (!await m_session.NegotiateConnectionAsync(width, height, framerate))
        {
            Debug.Log("Failed to connect to the server");
            return false;
        }

        Debug.Log("[SessionManager] Connection negotiated. Beginning stream receive");

        // create platform service and render texture
        // render texture should automatically be disposed of when play ends
        PlatformFFMpegService service = new PlatformFFMpegService();
        RenderTexture texture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32);

        // assign texture to screen
        m_vrScreenTarget.SourceTexture = texture;

        // run ffmpeg
        Stream streamInputPipe = await service.OpenStreamServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9943), width, height, (int)framerate);

        // init stream handler
        m_streamToTextureHandler = new StreamToTextureHandler(streamInputPipe, texture, width, height);

        return true;
    }

    private void Update()
    {
        m_streamToTextureHandler?.OnUpdate();
    }

    private static void ApplicationOnlogMessageReceived(string condition, string stacktrace, LogType type)
    {
        using StreamWriter sw = new StreamWriter(Application.persistentDataPath + "/log.txt", true);
        sw.WriteLine($"[{type}] | {condition}");
    }
}