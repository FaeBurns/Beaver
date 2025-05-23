﻿using UnityEngine;

public class RenderToVrScreen : MonoBehaviour
{
    private static readonly int s_eyeIndex = Shader.PropertyToID("_eyeIndex");
    private Camera m_camera = null!;
    private RenderTexture m_effectTexture = null!;

    public RenderTexture SourceTexture { get; set; } = null;

    [SerializeField]
    private Shader m_blitEyeShader;

    [SerializeField]
    private RenderTexture m_debugRenderTexture;

    [SerializeField]
    private PostProcessingEffectScheduler m_postProcessingScheduler;

    private Material m_blitEyeMaterial = null!;

    private int m_leftEyeRenderCount;
    private int m_rightEyeRenderCount;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();

        m_blitEyeMaterial = new Material(m_blitEyeShader);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (SourceTexture == null)
            return;

        if (destination == null)
        {
            Debug.LogError("Render to screen - destination null");
            return;
        }

        // no renders have been done yet, do preprocessing on frame
        if (m_leftEyeRenderCount == 0 && m_rightEyeRenderCount == 0)
        {
            // write to debug texture
            if (m_debugRenderTexture != null)
                Graphics.Blit(SourceTexture, m_debugRenderTexture);

            // it's apparently better to create a new temporary texture each time than using the same one
            RenderTextureDescriptor descriptor = destination.descriptor;
            descriptor.width *= 2; // double width as destination is for one eye not both
            m_effectTexture = RenderTexture.GetTemporary(descriptor);

            // render the post-processing effects
            m_postProcessingScheduler.Render(SourceTexture, m_effectTexture);
        }

        if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
        {
            m_blitEyeMaterial.SetInteger(s_eyeIndex, 0);
            Graphics.Blit(m_effectTexture, destination, m_blitEyeMaterial);
            m_leftEyeRenderCount++;
        }
        else if (m_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
        {
            m_blitEyeMaterial.SetInteger(s_eyeIndex, 1);
            Graphics.Blit(m_effectTexture, destination, m_blitEyeMaterial);
            m_rightEyeRenderCount++;
        }
        else
        {
            Graphics.Blit(source, destination);
            Debug.LogError("Invalid active eye");
        }

        if (m_leftEyeRenderCount > 0 && m_rightEyeRenderCount > 0)
        {
            if (m_leftEyeRenderCount > 1 || m_rightEyeRenderCount > 1)
                Debug.LogError($"More than one of each eye has been rendered per push. left: {m_leftEyeRenderCount} | right: {m_rightEyeRenderCount}");

            m_leftEyeRenderCount = 0;
            m_rightEyeRenderCount = 0;

            RenderTexture.ReleaseTemporary(m_effectTexture);
        }
    }
}