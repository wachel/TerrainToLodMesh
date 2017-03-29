Shader "Unlit/TerrainFirst"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Gamma]_SplatAlpha("Splat Alpha",2D) = "black"{}
		_SplatIndex("SplatIndex",float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque"  "Queue" = "Geometry+1" "LightMode" = "ForwardBase" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal:NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv2: TEXCOORD2;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				fixed4 diff : COLOR0;
			};

			sampler2D _MainTex;
			sampler2D _SplatAlpha;
			float4 _MainTex_ST;
			float4 _SplatAlpha_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv2 = TRANSFORM_TEX(v.uv, _SplatAlpha);

				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0;
				o.diff.rgb += ShadeSH9(half4(worldNormal, 1));

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			float _SplatIndex;
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 alpha = tex2D(_SplatAlpha, i.uv2);
				col *= i.diff;
			    return fixed4(lerp(fixed3(0,0,0), col.rgb, alpha[_SplatIndex]),1);
			}
			ENDCG
		}
	}
}
