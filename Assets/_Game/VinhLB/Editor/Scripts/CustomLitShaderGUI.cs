using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace VinhLB
{
    public class CustomLitShaderGUI : ShaderGUI
    {
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (newShader.name == "Custom/Lit")
            {
                UpdateSurfaceType(material);
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material material = materialEditor.target as Material;
            MaterialProperty surfaceProp = BaseShaderGUI.FindProperty("_SurfaceType", properties, true);

            EditorGUI.BeginChangeCheck();
            surfaceProp.floatValue =
                (int)(SurfaceType)EditorGUILayout.EnumPopup("Surface Type", (SurfaceType)surfaceProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateSurfaceType(material);
            }

            base.OnGUI(materialEditor, properties);
        }

        private void UpdateSurfaceType(Material material)
        {
            SurfaceType surfaceType = (SurfaceType)material.GetInt("_SurfaceType");
            switch (surfaceType)
            {
                case SurfaceType.Opaque:
                    material.renderQueue = (int)RenderQueue.Geometry;
                    material.SetOverrideTag("RenderType", "Opaque");
                    break;
                case SurfaceType.TransparentCutout:
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    break;
                case SurfaceType.TransparentBlend:
                    material.renderQueue = (int)RenderQueue.Transparent;
                    material.SetOverrideTag("RenderType", "Transparent");
                    break;
            }

            switch (surfaceType)
            {
                case SurfaceType.Opaque:
                case SurfaceType.TransparentCutout:
                    material.SetFloat("_SourceBlend", (int)BlendMode.One);
                    material.SetFloat("_DestBlend", (int)BlendMode.Zero);
                    material.SetFloat("_ZWrite", 1);
                    break;
                case SurfaceType.TransparentBlend:
                    material.SetFloat("_SourceBlend", (int)BlendMode.SrcAlpha);
                    material.SetFloat("_DestBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0);
                    break;
            }

            material.SetShaderPassEnabled("ShadowCaster", surfaceType != SurfaceType.TransparentBlend);

            if (surfaceType == SurfaceType.TransparentCutout)
            {
                material.EnableKeyword("_ALPHA_CUTOUT");
            }
            else
            {
                material.DisableKeyword("_ALPHA_CUTOUT");
            }
        }

        public enum SurfaceType
        {
            Opaque = 0,
            TransparentCutout = 1,
            TransparentBlend = 2
        }
    }
}