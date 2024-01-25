Shader "Mobile/DiffuseWithColorAdjust" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Color("Color Channel Adjust", Color) = (0,0,0,0)
		_BlendFactor("Blend Factor", Vector) = (1,1,1,1)
	}
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 150

	CGPROGRAM
	#pragma surface surf Lambert noforwardadd

	sampler2D _MainTex;
	fixed4 _Color;
	float _BlendFactor;

	struct Input {
		float2 uv_MainTex;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
		c = lerp(c, _Color, _BlendFactor);
		o.Albedo = c.rgb;
		o.Alpha = c.a;
	}
	ENDCG
	}

	Fallback "Mobile/VertexLit"
}