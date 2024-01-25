Shader "Custom/SimpleWaterShader"
{
    Properties
    {
		[Header(Densities)]
		_DistanceDensity("Distance Densities", Range(0.0, 1.0)) = 0.1

		_DepthValue("Depth value", float) = 0.5

		[Header(Base Color)]
		_ShallowColor("Shallow", Color) = (0.44, 0.95, 0.36, 1.0)
		_DeepColor("Deep", Color) = (0.0, 0.05, 0.19, 1.0)
		_FarColor("Far", Color) = (0.04, 0.27, 0.75, 1.0)

		_AlphaDistance("Alpha Distance",float) = 1.0
		_AlphaFar("Alpha Far",Range(0.0, 1.0)) = 1.0
		_AlphaNear("Alpha Near",Range(0.0, 1.0)) = 0.95

		/*[Header(Horizon)]
		_HorizonColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_HorizonDepth("Depth", float) = 1.0
		_HorizonSmooth("Smooth", float) = 0.1*/

		[Header(Foam)]
		_FoamContribution("Contribution", Range(0.0, 1.0)) = 1.0
		_FoamContrast("Hardness", float) = 0.0
		[NoScaleOffset]
		_FoamTexture("Texture", 2D) = "black"{}
		_FoamScale("Scale", float) = 1.0
		_FoamSpeed("Speed", float) = 1.0
		_FoamColor("Color", Color) = (1.0,1.0,1.0,1.0)

		/*[Header(Sun Specular)]
		[HDR]
		_SunSpecularColor("Color", Color) = (1, 1, 1, 1)
		_SunSpecularExponent("Exponent", float) = 1000*/
    }
    SubShader
    {
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}
		Blend SrcAlpha OneMinusSrcAlpha
		Pass{
			////Tags { "RenderType"="Opaque" }
			//Tags
			//{
			//	"RenderType" = "Geometry"
			//}
			LOD 100

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog  // Make fog work.

			#include "UnityCG.cginc"

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			// Densities.
			float _DistanceDensity;
			float _DepthValue;
			// Base Color.
			float3 _ShallowColor;
			float3 _DeepColor;
			float3 _FarColor;

			float _AlphaDistance;
			fixed _AlphaFar;
			fixed _AlphaNear;
			// Foam.
			sampler2D _FoamTexture;
			float _FoamScale;
			float _FoamSpeed;
			float _FoamContribution;
			float _FoamContrast;
			fixed4 _FoamColor;

			/*float _HorizonColor;
			float _HorizonDepth;
			float _HorizonSmooth;*/

			// Sun Specular.
			/*float3 _SunSpecularColor;
			float _SunSpecularExponent;*/

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
				UNITY_FOG_COORDS(3)
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
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// Calculate the view vector.
				//float3 viewDirWS = normalize(i.worldPosition - _WorldSpaceCameraPos);

				// Also calculate how far away the fragment is from the camera.
				float distanceMask = exp(-_DistanceDensity * length(i.worldPosition - _WorldSpaceCameraPos));

				// ---------- //
				// BASE COLOR //
				// ---------- //

				// Calculate the base color based on the transmittance and distance mask.
				float3 baseColor = _ShallowColor;
				baseColor = lerp(_DeepColor, baseColor, _DepthValue);
				baseColor = lerp(_FarColor, baseColor, distanceMask);

				// ---------- //
				// FOAM COLOR //
				// ---------- //

				// Distort the world space uv coordinates by the normal map.
				float2 foamUV = i.worldPosition.xz / _FoamScale;

				// Sample the foam texture and modulate the result by the distance mask and shadow mask.
				float3 foamColor = MotionFourWayChaos(_FoamTexture, foamUV, _FoamSpeed);
				foamColor = saturate(lerp(half3(0.5, 0.5, 0.5), foamColor, _FoamContrast)) * _FoamContribution * _FoamColor.xyz;

				//// ------------------ //
				//// SUN SPECULAR COLOR //
				//// ------------------ //

				//// Reflect the viewing vector by the normal.
				////float3 viewR = viewDirWS;
				//float3 viewR = reflect(viewDirWS, float3(0,1,0));

				//// Calculate the specular mask.
				//float sunSpecularMask = saturate(dot(viewR, _WorldSpaceLightPos0));
				//sunSpecularMask = round(saturate(pow(sunSpecularMask, _SunSpecularExponent)));

				//// Get the sun specular color to add into the final color later on.
				//float3 sunSpecularColor = lerp(0, _SunSpecularColor, sunSpecularMask);

				// --------------- //
				// EDGE FOAM COLOR //
				// --------------- //

				// Calculate edge foam mask, by on clipping the optical depth.
				/*float opticalDepth = abs(LinearEyeDepth(fragDepth) - LinearEyeDepth(i.vertex.z));
				float edgeFoamMask = round(exp(-opticalDepth / _EdgeFoamDepth));
				float3 edgeFoamColor = lerp(0, _EdgeFoamColor, edgeFoamMask);*/

				// ----------- //
				// FINAL COLOR //
				// ----------- //

				float3 color = baseColor + foamColor;
				//color += sunSpecularColor;

				// Apply fog.
				UNITY_APPLY_FOG(i.fogCoord, color);

				return float4(color, lerp(_AlphaFar, _AlphaNear, clamp(i.vertex.z * _AlphaDistance, 0.0, 1.0)));
			}
			ENDCG
		}
    }
    FallBack "Diffuse"
}
