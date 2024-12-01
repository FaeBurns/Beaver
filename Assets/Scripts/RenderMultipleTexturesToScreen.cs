using UnityEngine;

public class RenderMultipleTexturesToScreen : MonoBehaviour
{
    [SerializeField]
    private RenderTexture m_leftEyeSource;

    [SerializeField]
    private RenderTexture m_rightEyeSource;

    private Camera m_camera = null!;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
            Graphics.Blit(m_leftEyeSource, destination);
        else if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
            Graphics.Blit(m_rightEyeSource, destination);
        else
        {
            Graphics.Blit(source, destination);
            Debug.LogError("Invalid active eye");
        }
    }
}