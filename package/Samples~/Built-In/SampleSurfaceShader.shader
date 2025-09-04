Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        [ToggleUI]Boolean_7d219f41571b4f2bb6122a5d13c66ae6("[Markdown for ShaderGraph by @NeedleTools / @hybridherbst](https://twitter.com/hybridherbst)", Float) = 0
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_8("[ShaderGraph Markdown on GitHub](https://github.com)", Float) = 0
        [ToggleUI]Boolean_154f08e800204de482d649fbea24cb0a("## A Header", Float) = 0
        _SomeColor("Some Color", Color) = (0, 0, 0, 0)
        _SomeVector("Some Vector", Vector) = (0, 0, 0, 0)
        [ToggleUI]Boolean_1("## Another Header", Float) = 0
        [NoScaleOffset]Texture2D_8fd65cbf681547a190549617f2df0aee("Some Texture &", 2D) = "white" {}
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75("# Basic Properties", Float) = 0
        [ToggleUI]Boolean_2("## Colors", Float) = 0
        Color_c2cb65b47e23464f8188f662697561ce("Night", Color) = (0, 0, 0, 0)
        _DayColor("Day", Color) = (0, 0, 0, 0)
        [ToggleUI]Boolean_3("## Numbers", Float) = 0
        _SmallNumber("Small", Float) = 0
        LargeNumber("Large", Float) = 0
        [ToggleUI]Boolean_8("# Custom Drawers", Float) = 0
        [ToggleUI]Boolean_7("## Single-Line Textures", Float) = 0
        [NoScaleOffset]_MyOtherTexture("Some Texture &&", 2D) = "white" {}
        _MyTextureScale("Texture Scale", Range(0, 1)) = 0
        [NoScaleOffset]_YetAnotherTexture("Yet Another Texture &&", 2D) = "white" {}
        _YetAnotherTextureScale("Vector4", Vector) = (0, 0, 0, 0)
        [NoScaleOffset]_MyTexture("My Texture &&", 2D) = "white" {}
        _MyTextureColor("Texture Color", Color) = (0.8584906, 0.05264331, 0.05264331, 0)
        [NoScaleOffset]_DoubleTex1("Double Texture &&", 2D) = "white" {}
        [NoScaleOffset]_DoubleTex2("DoubleTexture2", 2D) = "white" {}
        [ToggleUI]Boolean_4("## Gradients", Float) = 0
        [ToggleUI]Boolean_5("!DRAWER Gradient _MyGradientTexture_1", Float) = 0
        [ToggleUI]Boolean_6("!DRAWER Gradient _MyGradientTexture_2", Float) = 0
        [NoScaleOffset]_MyGradientTexture_1("My Gradient Texture 1", 2D) = "white" {}
        [NoScaleOffset]_MyGradientTexture_2("My Gradient Texture 2", 2D) = "white" {}
        [ToggleUI]Boolean_9("## MinMax Sliders", Float) = 0
        [ToggleUI]Boolean_10("!DRAWER MinMax _MinMaxVal1 _MinMaxVal2", Float) = 0
        _MinMaxVal1("Min Value", Range(0, 1)) = 0
        _MinMaxVal2("Max Value", Range(0, 1)) = 0
        [ToggleUI]Boolean_11("!DRAWER MinMax _MinMaxVector.x _MinMaxVector.y", Float) = 0
        [ToggleUI]Boolean_12("!DRAWER MinMax _MinMaxVector.z _MinMaxVector.w", Float) = 0
        _MinMaxVector("Remap Glossiness", Vector) = (0, 0, 0, 0)
        _VectorSliders2_1("Vector with Sliders (Amplitude, Frequency, Pattern) &", Vector) = (0, 0, 0, 0)
        _VectorSliders2_2("Another Vector with Sliders &", Vector) = (0, 0, 0, 0)
        [ToggleUI]Boolean_13("!DRAWER VectorSlider _VectorSliders", Float) = 0
        _VectorSliders("Vector Sliders", Vector) = (0, 0, 0, 0)
        [ToggleUI]Boolean_14("!DRAWER VectorSlider _VectorSliders2", Float) = 0
        _VectorSliders2("Another Vector (Amplitude, Frequency, Pattern)", Vector) = (0, 0, 0, 0)
        [ToggleUI]Boolean_15("!DRAWER VectorSlider _Amplitude _Frequency _Pattern", Float) = 0
        _Amplitude("Amplitude", Float) = 0.5
        _Frequency("Frequency", Range(5, 10)) = 5
        _Pattern("Pattern", Int) = 0
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_1("# Keywords and Conditional Properties", Float) = 0
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_3("!REF _SOME_ENUM_KEYWORD", Float) = 0
        _BGColor1("Color 1 [_SOME_ENUM_KEYWORD_COLORS]", Color) = (0, 0, 0, 0)
        Color_bb0fd3a78c4c4806821da359fe068876("Color 2 [_SOME_ENUM_KEYWORD_COLORS]", Color) = (0, 0, 0, 0)
        [NoScaleOffset]_BGTexture("Gradient Texture [_SOME_ENUM_KEYWORD_TEXTURE]", 2D) = "white" {}
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_4("!DRAWER GradientGenerator [_SOME_ENUM_KEYWORD_GRADIENT]", Float) = 0
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_6("#", Float) = 0
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_2("## Main Region", Float) = 0
        _YetAnotherColor("Important Color", Color) = (0, 0, 0, 0)
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_7("!NOTE You can also add notes. You can include hints about your shader here!", Float) = 0
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_9("## Conditional Textures/Booleans", Float) = 0
        [ToggleUI]Boolean_5b03dc86eb2e496b9dd760faf6162b83("!REF _SIMPLE_OPTION", Float) = 0
        _OptionalColor("-Optional Color [_SIMPLE_OPTION_ON]", Color) = (0, 0, 0, 0)
        _AdditionalValue("-Additional Value [_SIMPLE_OPTION_ON]", Range(0, 1)) = 0.5
        [ToggleUI]_NonKeywordOption("A Bool Value as Condition", Float) = 0
        Color_b3dac903f448497b916fdfae4d78dd0f("-Optional Color 2 [_NonKeywordOption]", Color) = (0, 0, 0, 0)
        [NoScaleOffset]_TextureWithCondition("A Texture as Condition &", 2D) = "white" {}
        [NoScaleOffset]_AnotherTextureWithCondition("-Another Texture & [_TextureWithCondition]", 2D) = "white" {}
        _OptionalColor_1("--A Color [_AnotherTextureWithCondition]", Color) = (0, 0, 0, 0)
        [ToggleUI]Boolean_b33b62171e084c9f9c7664243b215c75_5("--!NOTE This color only shows up when both textures are set.", Float) = 0
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
        [KeywordEnum(Colors, Texture, Gradient)]_SOME_ENUM_KEYWORD("Important Option", Float) = 0
        [Toggle]_SIMPLE_OPTION("Simple Option", Float) = 0
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

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

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
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    
    CustomEditor "Needle.MarkdownShaderGUI"
    FallBack "Diffuse"
}
