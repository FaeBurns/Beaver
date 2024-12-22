using UnityEngine;

public class RenderToVrScreen : MonoBehaviour
{
    private static readonly int s_eyeIndex = Shader.PropertyToID("_eyeIndex");
    private Camera m_camera = null!;

    public RenderTexture SourceTexture { get; set; } = null;

    [SerializeField]
    private Shader m_blitEyeShader;

    private Material m_blitEyeMaterial = null!;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();

        m_blitEyeMaterial = new Material(m_blitEyeShader);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (SourceTexture == null)
            return;

        if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
        {
            m_blitEyeMaterial.SetInteger(s_eyeIndex, 0);
            Graphics.Blit(SourceTexture, destination, m_blitEyeMaterial);
        }
        else if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
        {
            m_blitEyeMaterial.SetInteger(s_eyeIndex, 1);
            Graphics.Blit(SourceTexture, destination, m_blitEyeMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
            Debug.LogError("Invalid active eye");
        }
    }
}