using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class PostProcessingEffectSource : MonoBehaviour
{
    private Material m_material;

    [FormerlySerializedAs("UseDestinationTextureDescriptor")]
    public bool UseFullResolutionTarget;

    [SerializeField]
    private Shader m_shader;

    [SerializeField]
    private PostProcessingEffectParameter[] m_defaultParameters = Array.Empty<PostProcessingEffectParameter>();

    [SerializeField]
    private PostProcessingEffect[] m_effects = Array.Empty<PostProcessingEffect>();

    private int m_activeEffectIndex = 0;

    private void OnEnable()
    {
        m_material = new Material(m_shader);
    }

    public void Render(RenderTexture source, RenderTexture destination)
    {
        ApplyCurrentParameters();
        Graphics.Blit(source, destination, m_material);
    }

    public void AdvanceEffect()
    {
        m_activeEffectIndex = (m_activeEffectIndex + 1) % m_effects.Length;
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