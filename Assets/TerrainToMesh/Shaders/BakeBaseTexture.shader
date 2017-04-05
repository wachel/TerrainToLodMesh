// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/BakeBaseTexture"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Gamma]_SplatAlpha("Splat Alpha",2D) = "black"{}
		_SplatIndex("SplatIndex",float) = 0
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
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
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(0,0,0,1);
			}
			ENDCG
		}
		Pass
		{
			Blend One One
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
				float2 uv2: TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _SplatAlpha;
			float4 _SplatAlpha_ST;
			float4 _MainTexST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv * _MainTexST.xy + _MainTexST.zw;
				o.uv2 = TRANSFORM_TEX(v.uv, _SplatAlpha);
				return o;
			}
			
			float _SplatIndex;
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 alpha = tex2D(_SplatAlpha, i.uv2);
			    return fixed4(lerp(fixed3(0,0,0), col.rgb, alpha[_SplatIndex]),1);
			}
			ENDCG
		}
	}
}
