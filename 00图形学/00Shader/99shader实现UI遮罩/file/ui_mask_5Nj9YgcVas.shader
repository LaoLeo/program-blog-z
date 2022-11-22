// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "pjg/ui/ui_mask"
{
	Properties
	{
		[PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
		[NoScaleOffset]_MaskTex("Mask Texture", 2D) = "white"{}
		//无法控制顶点颜色时，备选颜色
		_Color ("Tint", Color) = (1,1,1,1)
		
		[HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector]_Stencil ("Stencil ID", Float) = 0
		[HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255

		[HideInInspector]_ColorMask ("Color Mask", Float) = 15

//		[HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

//		_UVRange ("UV Range",vector) = (0,0,1,1)
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]

//			Comp	Greater 	
//			Comp	GEqual 	
//			Comp	Less 	
//			Comp	LEqual 	
//			Comp	Equal 	
//			Comp	NotEqual 	
//			Comp	Always 	
//			Comp	Never 
			Comp  [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;//第一版本要使用
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				half2 maskTexcoord : TEXCOORD2;
			};
//			float _StencilComp;
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;

			sampler2D _MainTex;
			sampler2D _MaskTex;
//			float4 _MainTex_ST;
//			float4 _UVRange;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;
//				if (IN.texcoord1.x >= 1 && IN.texcoord1.y >=1){
//					OUT.maskTexcoord = IN.texcoord1 - 1.0;
//				}
//				else
//				{
//					OUT.maskTexcoord = IN.texcoord;
//				}
				
				//第一版本
				//加1的目的，是为了判断出0，0的uv是有意义的顶点。c#脚本会故意加大1来传入，为了兼容无需uv1的情况
				fixed tmp = dot(IN.texcoord1,float2(1,1));
//				fixed tmp = IN.texcoord1 + IN.texcoord;
				fixed zero = step(tmp,0);
				OUT.maskTexcoord = lerp(IN.texcoord1 - float2(1,1),IN.texcoord,zero);

				/*
				//第二版
				//在顶点着色这里来换算回原来的uv值，提供给mask采样
				float uLen = _UVRange.z - _UVRange.x;
				float vLen = _UVRange.w - _UVRange.y;

				float u = (IN.texcoord.x - _UVRange.x)/uLen;
				float v = (IN.texcoord.y - _UVRange.y)/vLen;
				OUT.maskTexcoord = half2(u,v);
				*/

				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
				#endif
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
				half4 mask = tex2D(_MaskTex, IN.maskTexcoord);
				
				color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect) * mask.a;

//				clip(1 - mask.a - 0.00001);
				
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;// fixed4(_StencilComp / 255, 0,0,1);
			}
		ENDCG
		}
	}
}
