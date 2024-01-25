Shader "Custom/SimpleWaterIntersectionFoam"
{
    Properties
    {
		_ScaleRatio("Scale Ratio", float) = 1.0
		_InnerAlpha("Inner Alpha", Range(0.0,1.0)) = 1.0
		_OuterAlpha("Outer Alpha", Range(0.0,1.0)) = 0.0

		_Smooth("Smooth", Range(0.001,1.0)) = 0.2

		_InnerContrast("Inner Hardness", float) = 0.0
		_OuterContrast("Outer Hardness", float) = 1.0

		[NoScaleOffset]
		_FoamTexture("Texture", 2D) = "black"{}
		_FoamScale("Scale", float) = 1.0
		_FoamSpeed("Speed", float) = 1.0

		/*[Header(Sun Specular)]
		[HDR]
		_SunSpecularColor("Color", Color) = (1, 1, 1, 1)
		_SunSpecularExponent("Exponent", float) = 1000*/
    }
    SubShader
    {
		Pass{
			//Tags { "RenderType"="Opaque" }
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			ZWrite Off
			Blend One One
			LOD 100

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog  // Make fog work.

			#include "UnityCG.cginc"

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			float _ScaleRatio;
			fixed _InnerAlpha;
			fixed _OuterAlpha;
			float _InnerContrast;
			float _OuterContrast;
			float _Smooth;

			sampler2D _FoamTexture;
			float _FoamScale;
			float _FoamSpeed;
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldPosition : TEXCOORD1;
			};

			float2 Panner(float2 uv, float2 direction, float speed)
			{
				return uv + normalize(direction) * speed * _Time.y;
			}

			float3 MotionFourWayChaos(sampler2D tex, float2 uv, float speed)
			{
				float2 uv1 = Panner(uv + float2(0.000, 0.000), float2(0.1, 0.1), speed);
				float2 uv2 = Panner(uv + float2(0.418, 0.355), float2(-0.1, -0.1), speed);
				float2 uv3 = Panner(uv + float2(0.865, 0.148), float2(-0.1, 0.1), speed);
				float2 uv4 = Panner(uv + float2(0.651, 0.752), float2(0.1, -0.1), speed);
				
				float3 sample1 = tex2D(tex, uv1).rgb;
				float3 sample2 = tex2D(tex, uv2).rgb;
				float3 sample3 = tex2D(tex, uv3).rgb;
				float3 sample4 = tex2D(tex, uv4).rgb;

				return (sample1 + sample2 + sample3 + sample4) / 4.0;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
				//o.vertex = mul(UNITY_MATRIX_VP, float4(o.worldPosition, 1));
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float smooth = 1.0 - smoothstep(0.0,1.0,min((0.5 - abs(0.5 - i.uv.x)) * _ScaleRatio, 0.5 - abs(0.5 - i.uv.y)) / _Smooth);

				// Distort the world space uv coordinates by the normal map.
				float2 foamUV = i.worldPosition.xz / _FoamScale;

				// Sample the foam texture and modulate the result by the distance mask and shadow mask.
				fixed3 foamColor = MotionFourWayChaos(_FoamTexture, foamUV, _FoamSpeed);

				float contrast = lerp(_InnerContrast, _OuterContrast, smooth);
				foamColor = saturate(lerp(half3(0.5, 0.5, 0.5), foamColor, contrast));

				return fixed4(foamColor, 1.0) * lerp(_InnerAlpha, _OuterAlpha, smooth);

				//return fixed4(foamColor, 1.0);
			}
			ENDCG
		}
    }
    FallBack "Diffuse"
}
