using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Testing;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public class PostProcessingEffectSource : MonoBehaviour
{
    private Material m_material;

    [FormerlySerializedAs("UseDestinationTextureDescriptor")]
    public bool UseFullResolutionTarget;

    [SerializeField]
    private Shader m_shader;

    [SerializeField]
    private int m_shaderPasses = 1;

    [SerializeField]
    private PostProcessingEffectParameter[] m_defaultParameters = Array.Empty<PostProcessingEffectParameter>();

    [SerializeField]
    private PostProcessingEffect[] m_effects = Array.Empty<PostProcessingEffect>();

    [SerializeField]
    private string m_logName = "unnamed";

    private int m_activeEffectIndex = 0;

    private void OnEnable()
    {
        m_material = new Material(m_shader);
    }

    public void Render(CommandBuffer buffer, RenderTexture source, RenderTexture destination)
    {
        ApplyCurrentParameters();
        int monitor = Tester.BeginTimeMonitor(buffer, $"PP_{m_logName}");
        Stack<RenderTexture> temporaryTextures = new Stack<RenderTexture>();
        temporaryTextures.Push(source);
        for (int i = 0; i < m_shaderPasses; i++)
        {
            RenderTexture target = RenderTexture.GetTemporary(source.descriptor);
            buffer.Blit(temporaryTextures.Peek(), target, m_material, i); // i = pass

            temporaryTextures.Push(target);
        }

        buffer.Blit(temporaryTextures.Peek(), destination);

        // don't release the last one as that's the source
        while (temporaryTextures.Count > 1)
        {
            RenderTexture.ReleaseTemporary(temporaryTextures.Pop());
        }

        Tester.EndTimeMonitor(buffer, monitor);
    }

    private IEnumerable<PostProcessingEffectParameter> GetActiveEffectParameters()
    {
        if (m_effects.Length == 0)
            return Enumerable.Empty<PostProcessingEffectParameter>();

        return m_effects[m_activeEffectIndex].Parameters;
    }

    private void ApplyCurrentParameters()
    {
        HashSet<string> visitedNames = new HashSet<string>();
        foreach (PostProcessingEffectParameter parameter in GetActiveEffectParameters())
        {
            visitedNames.Add(parameter.Name);

            ApplyParameter(parameter);
        }

        foreach (PostProcessingEffectParameter defaultParameter in m_defaultParameters)
        {
            if (visitedNames.Contains(defaultParameter.Name))
                continue;

            ApplyParameter(defaultParameter);
        }
    }

    private void ApplyParameter(PostProcessingEffectParameter parameter)
    {
        switch (parameter.Type)
        {
            case EffectParameterType.FLAG:
                if (parameter.FlagValue)
                    m_material.EnableKeyword(parameter.Name);
                else
                    m_material.DisableKeyword(parameter.Name);
                break;
            case EffectParameterType.INT:
                m_material.SetInt(parameter.Name, parameter.IntValue);
                break;
            case EffectParameterType.FLOAT:
                m_material.SetFloat(parameter.Name, parameter.FloatValue);
                break;
            case EffectParameterType.COLOR:
                m_material.SetColor(parameter.Name, parameter.ColorValue);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}