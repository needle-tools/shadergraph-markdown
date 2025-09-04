// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Markdown Amplify Shader"
{
	Properties
	{
		_Header("# Header", Int) = 0
		_InlineTexture("Inline Texture &", 2D) = "white" {}
		_MyColor("MyColor", Color) = (0,0,0,0)
		_MyVectorMetallicSmoothness("My Vector (Metallic,Smoothness) &", Vector) = (0,0,0,0)
		_NOTEMarkdowninAmplifyShaders("!NOTE Markdown in Amplify Shaders!", Int) = 0
		[RemapSliders]_Vector0("Vector 0", Vector) = (0,0,0,0)
		[RemapSlidersFull]_Vector1("Vector 1", Vector) = (0,0,0,0)
		_Texture0("Texture 0 &&", 2D) = "white" {}
		_DRAWERMinMax_Vector1("!DRAWER MinMax _Vector1", Float) = 0
		_Texture1("Texture 1", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _DRAWERMinMax_Vector1;
		uniform int _NOTEMarkdowninAmplifyShaders;
		uniform sampler2D _Texture1;
		uniform int _Header;
		uniform sampler2D _Texture0;
		uniform float2 _Vector0;
		uniform float4 _Vector1;
		uniform sampler2D _InlineTexture;
		uniform float4 _InlineTexture_ST;
		uniform float4 _MyColor;
		uniform float4 _MyVectorMetallicSmoothness;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_InlineTexture = i.uv_texcoord * _InlineTexture_ST.xy + _InlineTexture_ST.zw;
			o.Albedo = ( tex2D( _InlineTexture, uv_InlineTexture ) * _MyColor ).rgb;
			float4 break14 = _MyVectorMetallicSmoothness;
			o.Metallic = break14;
			o.Smoothness = break14.y;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "Needle.MarkdownShaderGUI"
}
/*ASEBEGIN
Version=18800
270;178;1710;929;1817.91;233.9422;2.865409;True;True
Node;AmplifyShaderEditor.TexturePropertyNode;9;-398.5,30;Inherit;True;Property;_InlineTexture;Inline Texture &;1;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;10;-164.5,91;Inherit;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;12;-77.5,330;Inherit;False;Property;_MyColor;MyColor;2;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;13;-179.7,508.8;Inherit;False;Property;_MyVectorMetallicSmoothness;My Vector (Metallic,Smoothness) &;3;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;20;-283.3334,743.4744;Inherit;False;Property;_Vector1;Vector 1;6;1;[RemapSlidersFull];Create;True;0;0;0;True;0;False;0,0,0,0;0.3832281,0.7502816,0,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TexturePropertyNode;19;-729.927,473.5834;Inherit;True;Property;_Texture0;Texture 0 &&;7;0;Create;True;0;0;0;True;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.BreakToComponentsNode;14;166.9597,405.4537;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.Vector2Node;18;-448.927,505.5834;Inherit;False;Property;_Vector0;Vector 0;5;1;[RemapSliders];Create;True;0;0;0;True;0;False;0,0;0.1784809,0.5065001;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.IntNode;16;-449.2403,330.0538;Inherit;False;Property;_Header;# Header;0;0;Create;True;0;0;0;True;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.TexturePropertyNode;22;-731.597,847.0576;Inherit;True;Property;_Texture1;Texture 1;9;0;Create;True;0;0;0;True;0;False;None;None;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.IntNode;17;-454.4402,404.1537;Inherit;False;Property;_NOTEMarkdowninAmplifyShaders;!NOTE Markdown in Amplify Shaders!;4;0;Create;True;0;0;0;True;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode;21;-609.5972,710.0576;Inherit;False;Property;_DRAWERMinMax_Vector1;!DRAWER MinMax _Vector1;8;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;162.5,206;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;23;352,141;Float;False;True;-1;2;Needle.MarkdownShaderGUI;0;0;Standard;Markdown Amplify Shader;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;10;0;9;0
WireConnection;14;0;13;0
WireConnection;11;0;10;0
WireConnection;11;1;12;0
WireConnection;23;0;11;0
WireConnection;23;3;14;0
WireConnection;23;4;14;1
ASEEND*/
//CHKSM=2C3FAD99BF51D1CF95CE31A5E17AC1F99680E81D