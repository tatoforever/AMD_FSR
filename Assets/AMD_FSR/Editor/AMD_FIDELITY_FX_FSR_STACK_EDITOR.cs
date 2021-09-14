using UnityEditor;
using UnityEditor.Rendering.PostProcessing;
using UnityEngine;

namespace AMD_FIDELITY_FX
{
    [PostProcessEditor(typeof(AMD_FIDELITY_FX_FSR_STACK))]
    public sealed class AMD_FIDELITY_FX_FSR_STACK_EDITOR : PostProcessEffectEditor<AMD_FIDELITY_FX_FSR_STACK>
    {
        //private SerializedProperty m_computeShaderEASU;
        //private SerializedProperty m_computeShaderRCAS;
        private SerializedParameterOverride m_scaleFactor;
        private SerializedParameterOverride m_sharpening;
        private SerializedParameterOverride m_sharpness;

        public override void OnEnable()
        {
            //m_computeShaderEASU = FindProperty(x => x.computeShaderEASU);
            //m_computeShaderRCAS = FindProperty(x => x.computeShaderRCAS);
            m_scaleFactor = FindParameterOverride(x => x.scaleFactor);
            m_sharpening = FindParameterOverride(x => x.sharpening);
            m_sharpness = FindParameterOverride(x => x.sharpness);
        }

        public override void OnInspectorGUI()
        {
            //EditorGUILayout.PropertyField(m_computeShaderEASU);
            //EditorGUILayout.PropertyField(m_computeShaderRCAS);
            GUILayout.Label("This filter requires dynamic resolution support.");
            PropertyField(m_scaleFactor);
            PropertyField(m_sharpening);
            PropertyField(m_sharpness);
            GUILayout.Space(5);
        }
    }
}