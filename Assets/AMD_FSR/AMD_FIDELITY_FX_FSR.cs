
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace AMD_FIDELITY_FX
{
    [ExecuteInEditMode]
    public class AMD_FIDELITY_FX_FSR : MonoBehaviour
    {
        [Header("FSR Compute Shaders")] public ComputeShader computeShaderEASU;
        public ComputeShader computeShaderRCAS;

        [Header("Edge Adaptive Scale Upsampling")] [Range(1.3f, 2f), Tooltip("Ultra Quality 1.3, Quality 1.5f, Balanced 1.7f, Performance 2f")]
        public float scaleFactor = 1.3f;

        [Header("Robust Contrast Adaptive Sharpen")]
        public bool sharpening = true;

        [Range(0f, 2f), Tooltip("0 = sharpest, 2 = less sharp")]
        public float sharpness = 0.2f;

        /*********************************************************************************************************/
        // Robust Contrast Adaptive Sharpening
        private static readonly int _RCASScale = Shader.PropertyToID("_RCASScale");
        private static readonly int _RCASParameters = Shader.PropertyToID("_RCASParameters");

        // Edge Adaptive Spatial Upsampling
        private static readonly int _EASUViewportSize = Shader.PropertyToID("_EASUViewportSize");
        private static readonly int _EASUInputImageSize = Shader.PropertyToID("_EASUInputImageSize");
        private static readonly int _EASUOutputSize = Shader.PropertyToID("_EASUOutputSize");
        private static readonly int _EASUParameters = Shader.PropertyToID("_EASUParameters");

        private static readonly int InputTexture = Shader.PropertyToID("InputTexture");
        private static readonly int OutputTexture = Shader.PropertyToID("OutputTexture");

        private RenderTexture outputImage, outputImage2;
        private ComputeBuffer EASUParametersCB, RCASParametersCB;
        private Camera cam;
        private int scaledPixelWidth = 0;
        private int scaledPixelHeight = 0;
        private int scaledPixelWidthPrev = 0;
        private int scaledPixelHeightPrev = 0;
        private bool isRCASSetup = false;

        private void OnDisable()
        {
            if (outputImage)
            {
                outputImage.Release();
                outputImage = null;
            }

            if (EASUParametersCB != null)
            {
                EASUParametersCB.Dispose();
                EASUParametersCB = null;
            }

            if (outputImage2)
            {
                outputImage2.Release();
                outputImage2 = null;
            }

            if (RCASParametersCB != null)
            {
                RCASParametersCB.Dispose();
                RCASParametersCB = null;
            }

            isRCASSetup = false;
        }

        private void OnEnable()
        {
            cam = GetComponent<Camera>();
            EASUParametersCB = new ComputeBuffer(4, sizeof(uint) * 4);
            EASUParametersCB.name = "EASU Parameters";

            RCASParametersCB = new ComputeBuffer(1, sizeof(uint) * 4);
            RCASParametersCB.name = "RCAS Parameters";
        }

        private RenderTexture rt;
        private void OnPreRender()
        {
            //render the scene to a downsised RT
            scaledPixelWidth = (int)(cam.pixelWidth / scaleFactor);
            scaledPixelHeight = (int)(cam.pixelHeight / scaleFactor);
            rt = RenderTexture.GetTemporary(scaledPixelWidth, scaledPixelHeight);//, 0, cam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, RenderTextureReadWrite.Default, QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1);
            cam.targetTexture = rt;
        }
    
        private void OnPostRender()
        {
            RenderTexture.ReleaseTemporary(rt);
            cam.targetTexture = null;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            // RenderTextureDescriptor rtDsc = new RenderTextureDescriptor(src.width * 2, src.height * 2, src.format, 0);
            // rtDsc.enableRandomWrite = true;
            // rtDsc.sRGB = true;
            // RenderTexture outputImage = RenderTexture.GetTemporary(rtDsc);
            
            if (outputImage == null || scaledPixelWidthPrev != scaledPixelWidth || scaledPixelHeightPrev != scaledPixelHeight || isRCASSetup == false && sharpening)
            {
                //cam.allowDynamicResolution = true;
                scaledPixelWidthPrev = scaledPixelWidth;
                scaledPixelHeightPrev = scaledPixelHeight;

                float normalizedScale = (scaleFactor - 1.3f) / (2f - 1.3f);
                float mipBias = -Mathf.Lerp(0.38f, 1f, normalizedScale); //Ultra Quality -0.38f, Quality -0.58f, Balanced -0.79f, Performance -1f

                //EASU
                if (outputImage) outputImage.Release();
                outputImage = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, src.format, RenderTextureReadWrite.sRGB); //, RenderTextureFormat.ARGB32);
                //outputImage.useDynamicScale = false;
                outputImage.enableRandomWrite = true;
                //outputImage.mipMapBias = mipBias; //Ultra Quality -0.38f, Quality -0.58f, Balanced -0.79f, Performance -1f
                outputImage.Create();

                //RCAS
                if (sharpening)
                {
                    isRCASSetup = true;
                    if (outputImage2) outputImage2.Release();
                    outputImage2 = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0, src.format, RenderTextureReadWrite.sRGB); //, RenderTextureFormat.ARGB32);
                    //outputImage.useDynamicScale = false;
                    outputImage2.enableRandomWrite = true;
                    //outputImage2.mipMapBias = mipBias;//Ultra Quality -0.38f, Quality -0.58f, Balanced -0.79f, Performance -1f
                    outputImage2.Create();
                }
            }

            //EASU
            computeShaderEASU.SetVector(_EASUViewportSize, new Vector4(cam.pixelWidth, cam.pixelHeight));
            computeShaderEASU.SetVector(_EASUInputImageSize, new Vector4(cam.pixelWidth, cam.pixelHeight));
            computeShaderEASU.SetVector(_EASUOutputSize, new Vector4(outputImage.width, outputImage.height, 1f / outputImage.width, 1f / outputImage.height));
            computeShaderEASU.SetBuffer(1, _EASUParameters, EASUParametersCB);

            computeShaderEASU.Dispatch(1, 1, 1, 1); //init

            computeShaderEASU.SetTexture(0, InputTexture, src);
            computeShaderEASU.SetTexture(0, OutputTexture, outputImage);

            const int ThreadGroupWorkRegionRim = 8;
            int dispatchX = (outputImage.width + ThreadGroupWorkRegionRim - 1) / ThreadGroupWorkRegionRim;
            int dispatchY = (outputImage.height + ThreadGroupWorkRegionRim - 1) / ThreadGroupWorkRegionRim;

            computeShaderEASU.SetBuffer(0, _EASUParameters, EASUParametersCB);
            computeShaderEASU.Dispatch(0, dispatchX, dispatchY, 1); //main

            //RCAS
            if (sharpening)
            {
                computeShaderRCAS.SetBuffer(1, _RCASParameters, RCASParametersCB);
                computeShaderRCAS.SetFloat(_RCASScale, sharpness);
                computeShaderRCAS.Dispatch(1, 1, 1, 1); //init

                computeShaderRCAS.SetBuffer(0, _RCASParameters, RCASParametersCB);
                computeShaderRCAS.SetTexture(0, InputTexture, outputImage);
                computeShaderRCAS.SetTexture(0, OutputTexture, outputImage2);

                computeShaderRCAS.Dispatch(0, dispatchX, dispatchY, 1); //main
            }

            Graphics.Blit(sharpening ? outputImage2 : outputImage, dest);//, new Vector2(1f / scaleFactor, 1f / scaleFactor), new Vector2(0f, 0f));
            //RenderTexture.ReleaseTemporary(outputImage);
        }
    }
}