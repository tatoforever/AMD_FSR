Shader "Custom/CausticsShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Normal]_Normal("Normal", 2D) = "bump" {}
        _NormalStrength("Normal strength", Range(0.0,1.0)) = 1
        _CausticsTex("Caustics texture", 2D) = "black" {}
        _CausticsProperties("Caustics properties (scaleA, scaleB, speedA, speedB)", Vector) = (0,0,0,0)
        _CausticsHeight("Caustics height", float) = 0
        _CausticsHeightSmoothness("Caustics height smoothness", float) = 0
        _CausticsStrength("Caustics strength", float) = 1
        _CausticsColorSplit("Caustics color split", float) = 0
        _DisplacementGuide("Displacement guide", 2D) = "white" {}
        _DisplacementAmount("Displacement amount", float) = 0
        _DisplacementSpeed("Displacement speed", float) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Normal;
        sampler2D _DisplacementGuide;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_Normal;
            float2 uv_DisplacementGuide;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _NormalStrength;
        sampler2D _CausticsTex;
        float4 _CausticsProperties;
        float _CausticsHeight;
        float _CausticsHeightSmoothness;
        float _CausticsStrength;
        float _CausticsColorSplit;
        float _DisplacementSpeed;
        float _DisplacementAmount;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            fixed3 normal = UnpackNormal(tex2D(_Normal, IN.uv_Normal));
            normal.xy *= _NormalStrength;
            o.Normal = normalize(normal);
            float2 displ = tex2D(_DisplacementGuide, IN.uv_DisplacementGuide + _Time.y * _DisplacementSpeed);
            displ = (displ * 2.0 - 1.0) * _DisplacementAmount;
            float causticsR = min(tex2D(_CausticsTex, IN.worldPos.xz * _CausticsProperties.x + displ + _Time.y * _CausticsProperties.z + _CausticsColorSplit), tex2D(_CausticsTex, IN.worldPos.xz * _CausticsProperties.y + displ + _Time.y * _CausticsProperties.w + _CausticsColorSplit)).r;
            float causticsG = min(tex2D(_CausticsTex, IN.worldPos.xz * _CausticsProperties.x + displ + _Time.y * _CausticsProperties.z), tex2D(_CausticsTex, IN.worldPos.xz * _CausticsProperties.y + displ + _Time.y * _CausticsProperties.w)).r;
            float causticsB = min(tex2D(_CausticsTex, IN.worldPos.xz * _CausticsProperties.x + displ + _Time.y * _CausticsProperties.z - _CausticsColorSplit), tex2D(_CausticsTex, IN.worldPos.xz * _CausticsProperties.y + displ + _Time.y * _CausticsProperties.w - _CausticsColorSplit)).r;
            fixed3 caustics = fixed3(causticsR, causticsG, causticsB);
            o.Emission = caustics * _CausticsStrength * smoothstep(IN.worldPos.y, IN.worldPos.y + _CausticsHeightSmoothness, _CausticsHeight);
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
