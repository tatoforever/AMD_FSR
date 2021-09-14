using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace AMD_FIDELITY_FX
{
    [Serializable]
    [PostProcess(typeof(AMD_FIDELITY_FX_FSR_STACK_Renderer), PostProcessEvent.AfterStack,
        "Psychose Interactive/AMD Fidelity FX/FSR")]
    public sealed class AMD_FIDELITY_FX_FSR_STACK : PostProcessEffectSettings
    {
        //[Range(0f, 1f), Tooltip("Grayscale effect intensity.")]
        //public FloatParameter blend = new FloatParameter { value = 0.5f };

        [Header("FSR Compute Shaders")]
        //[Reload("Assets/Shaders/AMD_FSR/EdgeAdaptiveSpatialUpsampling.compute")]//Todo: to implement
        public ComputeShader computeShaderEASU;

        //[Reload("Assets/Shaders/AMD_FSR/RobustContrastAdaptiveSharpen.compute")]//Todo: to implement
        public ComputeShader computeShaderRCAS;

        [Header("Edge Adaptive Scale Upsampling")]
        [Range(1.3f, 2f), Tooltip("Ultra Quality 1.3, Quality 1.5, Balanced 1.7, Performance 2")]
        public FloatParameter scaleFactor = new FloatParameter { value = 1.3f };

        [Header("Robust Contrast Adaptive Sharpen")]
        public BoolParameter sharpening = new BoolParameter { value = true };

        [Range(0f, 2f), Tooltip("0 = sharpest, 2 = less sharp")]
        public FloatParameter sharpness = new FloatParameter { value = 0.2f };
    }

    public sealed class AMD_FIDELITY_FX_FSR_STACK_Renderer : PostProcessEffectRenderer<AMD_FIDELITY_FX_FSR_STACK>
    {
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

        //private Camera cam;
        private int scaledPixelWidth = 0;
        private int scaledPixelHeight = 0;
        private bool isRCASSetup = false;

        public override void Release()
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

            ScalableBufferManager.ResizeBuffers(1f, 1f);

            isRCASSetup = false;
            base.Release();
        }

        public override void Init()
        {
            //cam = GetComponent<Camera>();

            settings.computeShaderEASU = (ComputeShader)Resources.Load("EdgeAdaptiveScaleUpsampling");
            settings.computeShaderRCAS = (ComputeShader)Resources.Load("RobustContrastAdaptiveSharpen");

            EASUParametersCB = new ComputeBuffer(4, sizeof(uint) * 4);
            EASUParametersCB.name = "EASU Parameters";

            RCASParametersCB = new ComputeBuffer(1, sizeof(uint) * 4);
            RCASParametersCB.name = "RCAS Parameters";

            base.Init();
        }

        public override void Render(PostProcessRenderContext context)
        {
            //var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Grayscale"));
            //sheet.properties.SetFloat("_Blend", settings.blend);
            //context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            //context.resources.computeShaders.

            ScalableBufferManager.ResizeBuffers(1f / settings.scaleFactor, 1f / settings.scaleFactor); //DX12 only

            if (outputImage == null || scaledPixelWidth != context.camera.scaledPixelWidth || scaledPixelHeight != context.camera.scaledPixelHeight || isRCASSetup == false && settings.sharpening)
            {
                //context.camera.allowDynamicResolution = true;
                scaledPixelWidth = context.camera.scaledPixelWidth;
                scaledPixelHeight = context.camera.scaledPixelHeight;
                float normalizedScale = (settings.scaleFactor - 1.3f) / (2f - 1.3f);
                float mipBias = -Mathf.Lerp(0.38f, 1f, normalizedScale); //Ultra Quality -0.38f, Quality -0.58f, Balanced -0.79f, Performance -1f

                //EASU
                if (outputImage) outputImage.Release();
                outputImage = new RenderTexture(context.camera.pixelWidth, context.camera.pixelHeight, 0, context.sourceFormat, RenderTextureReadWrite.sRGB); //, RenderTextureFormat.ARGB32);
                outputImage.enableRandomWrite = true;
                outputImage.mipMapBias = mipBias; //Ultra Quality -0.38f, Quality -0.58f, Balanced -0.79f, Performance -1f
                outputImage.Create();

                //RCAS
                if (settings.sharpening)
                {
                    //isRCASSetup = true;
                    if (outputImage2) outputImage2.Release();
                    outputImage2 = new RenderTexture(context.camera.pixelWidth, context.camera.pixelHeight, 0, context.sourceFormat, RenderTextureReadWrite.sRGB); //, RenderTextureFormat.ARGB32);
                    outputImage2.enableRandomWrite = true;
                    //outputImage2.mipMapBias = mipBias;//Ultra Quality -0.38f, Quality -0.58f, Balanced -0.79f, Performance -1f
                    outputImage2.Create();
                }
            }
            
            //EASU
            context.command.SetComputeVectorParam(settings.computeShaderEASU, _EASUViewportSize, new Vector4(context.camera.pixelWidth, context.camera.pixelHeight));
            context.command.SetComputeVectorParam(settings.computeShaderEASU, _EASUInputImageSize, new Vector4(context.camera.scaledPixelWidth, context.camera.scaledPixelHeight));
            context.command.SetComputeVectorParam(settings.computeShaderEASU, _EASUOutputSize, new Vector4(outputImage.width, outputImage.height, 1f / outputImage.width, 1f / outputImage.height));
            context.command.SetComputeBufferParam(settings.computeShaderEASU, 1, _EASUParameters, EASUParametersCB);

            context.command.DispatchCompute(settings.computeShaderEASU, 1, 1, 1, 1); //init

            context.command.SetComputeTextureParam(settings.computeShaderEASU, 0, InputTexture, context.source);
            context.command.SetComputeTextureParam(settings.computeShaderEASU, 0, OutputTexture, outputImage);

            const int ThreadGroupWorkRegionRim = 8;
            int dispatchX = (outputImage.width + ThreadGroupWorkRegionRim - 1) / ThreadGroupWorkRegionRim;
            int dispatchY = (outputImage.height + ThreadGroupWorkRegionRim - 1) / ThreadGroupWorkRegionRim;

            context.command.SetComputeBufferParam(settings.computeShaderEASU, 0, _EASUParameters, EASUParametersCB);
            context.command.DispatchCompute(settings.computeShaderEASU, 0, dispatchX, dispatchY, 1); //main

            //RCAS
            if (settings.sharpening)
            {
                context.command.SetComputeBufferParam(settings.computeShaderRCAS, 1, _RCASParameters, RCASParametersCB);
                context.command.SetComputeFloatParam(settings.computeShaderRCAS, _RCASScale, settings.sharpness);
                context.command.DispatchCompute(settings.computeShaderRCAS, 1, 1, 1, 1); //init

                context.command.SetComputeBufferParam(settings.computeShaderRCAS, 0, _RCASParameters, RCASParametersCB);
                context.command.SetComputeTextureParam(settings.computeShaderRCAS, 0, InputTexture, outputImage);
                context.command.SetComputeTextureParam(settings.computeShaderRCAS, 0, OutputTexture, outputImage2);

                context.command.DispatchCompute(settings.computeShaderRCAS, 0, dispatchX, dispatchY, 1); //main
            }

            context.command.BlitFullscreenTriangle(settings.sharpening ? outputImage2 : outputImage, context.destination, false, new Rect(0f, 0f, context.camera.pixelWidth, context.camera.pixelHeight));
        }
    }
}