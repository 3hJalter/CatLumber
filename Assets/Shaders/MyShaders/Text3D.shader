Shader "Custom/Text Shader" {
	Properties{
		//The texture here is the FontTexture in the font
		_MainTex("Font Texture", 2D) = "white" {}
		_Color("Text Color", Color) = (1,1,1,1)
	}

	SubShader{

		Tags {
			//Rendering queue-usually this index is used to render objects with transparency blending
			"Queue" = "Transparent"
			//Projector is a projector, this setting will make the object ignore the influence of any projection type material or texture
			"IgnoreProjector" = "True"
			//Used when rendering transparent objects
			"RenderType" = "Transparent"
			//Preview-plane
			"PreviewType" = "Plane"
		}
		//Turn off the light, culling off (all the front and back are displayed) Depth test is on, Depth writing is off
		//Depth test is displayed when the object is closer to the camera than the pixels in the depth buffer, otherwise it is not displayed
		Lighting Off Cull Off ZWrite On
		//Based on the a value of this object, set the color in the color buffer to 1- the a value of this object
		Blend SrcAlpha OneMinusSrcAlpha
		//The overall setting here is not affected by light, and all rendering. Transparency is to turn on the depth test, and turn off the depth to write your own color mixing (take the object a as the standard)

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			//#pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed4 _Color;

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color * _Color;
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = i.color;
				col.a *= tex2D(_MainTex, i.texcoord).a;
				return col;
			}
			ENDCG
		}
	}
}