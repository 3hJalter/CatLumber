Shader "Custom/Overdraw" {
	Properties
	{
	}
	SubShader
	{
		//Tags { "RenderType"="Opaque" }
		Tags
		{
			"Queue" = "Transparent"
		}
		//ZTest Always
		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha, One One

		Pass{
			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = fixed4(1,0,0,0.25);
				return col;
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}