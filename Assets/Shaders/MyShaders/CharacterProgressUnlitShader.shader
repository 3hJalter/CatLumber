Shader "Unlit/CharacterProgressUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Progress ("Progress", Range(0.0, 1.0)) = 0.5
		_Smooth("Smooth", Range(0.0, 1.0)) = 0.1
		_BlendFactor("Blend Factor", Range(0.0, 1.0)) = 0.9
		_HeightUpperBound("Height Upper Bound", float) = 2.0
		_HeightLowerBound ("Height Lower Bound", float) = 0.0
		_HiddenColor("Hidden Color", Color) = (0.0,0.0,0.0,1.0)
		_ScaleY("Height Scale", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				//fixed4 color : COLOR;
				float height : HPOS;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed _Progress;
			float _HeightLowerBound;
			float _HeightUpperBound;
			float _ScaleY;
			fixed4 _HiddenColor;
			fixed _Smooth;
			fixed _BlendFactor;

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				//o.color = fixed4(v.vertex.y, v.vertex.y, v.vertex.y,1.0);
				o.height = (v.vertex.y / _ScaleY - _HeightLowerBound) / (_HeightUpperBound - _HeightLowerBound);
				o.vertex = UnityObjectToClipPos(v.vertex);

				// for testing
				//o.height = v.vertex.y / _ScaleY;
                
				return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
				//col = i.color;
				
				col = lerp(col, _HiddenColor, smoothstep(_Progress - _Smooth, _Progress + _Smooth, i.height) * _BlendFactor);

				// for testing
				/*col.x = 0.0;
				col.y = 0.0;
				col.z = 0.0;
				col.a = 1.0;
				if (i.height < -1.0) {
					col.y = -i.height - 1.0;
				}
				else if (i.height < 0.0) {
					col.x = -i.height;
				}
				else if (i.height < 1.0) {
					col.z = i.height;
				}
				else {
					col.y = i.height - 1.0;
				}*/

                return col;
            }
            ENDCG
        }
    }
	Fallback "Mobile/VertexLit"
}
