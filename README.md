# AMD_FSR
Amd Fidelity FX Super Resolution
FSR is a two compute shaders algorithm from AMD to produce Upscale images at cheap cost with comparative visuals to its Native counterpart.
In this package you will find two versions of the implementation in Unity. One is OnRenderImage based (only works with Built-in renderer) and the other one is a Post-processing-stack version (should work on all renderers).
In both cases you only have two options to tweak (one per algorithm), Scale Factor and Sharpness.
AMD recommends to run FSR just after Anti-Aliasing and before any other post processing that add noise, banding or distort the image (noise, vignette, chromatic aberration, color compression, etc) otherwise those artefact will get augmented when upscale with FSR.
The input image must also be in sRGB format and normalized in 0-1 range. There's some tools included in UnityCG.cginc and AMD FSR headers to remove banding and convert your input image from linear to gamma and normalize it.
Please refer to the official documentation for guidelines and UI integration of FSR in your application. It's very important to follow those rules or AMD won't like it! :D
https://raw.githubusercontent.com/GPUOpen-Effects/FidelityFX-FSR/master/docs/FidelityFX-FSR-Overview-Integration.pdf
