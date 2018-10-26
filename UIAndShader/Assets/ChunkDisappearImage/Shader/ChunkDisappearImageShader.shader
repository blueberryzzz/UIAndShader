// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI/ChunkDisappearImage"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
	_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
	}

		SubShader
	{
		Tags
	{
		"Queue" = "Transparent"
		"IgnoreProjector" = "True"
		"RenderType" = "Transparent"
		"PreviewType" = "Plane"
		"CanUseSpriteAtlas" = "True"
	}

		Stencil
	{
		Ref[_Stencil]
		Comp[_StencilComp]
		Pass[_StencilOp]
		ReadMask[_StencilReadMask]
		WriteMask[_StencilWriteMask]
	}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

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
		//额外信息，传入每个小方块的左下角方块的信息，以此为标准进行移动
		float2 extraInfo : TEXCOORD1;
	};

	struct v2f
	{
		float4 vertex   : SV_POSITION;
		fixed4 color : COLOR;
		half2 texcoord  : TEXCOORD0;
		float4 worldPosition : TEXCOORD1;
	};

	fixed4 _Color;
	fixed4 _TextureSampleAdd;
	float4 _ClipRect;

	//消散的目标位置
	uniform float2 _Target;
	//移动参数
	uniform float _Delta;
	//arch坐标到canvas坐标的变换矩阵
	uniform float4x4 _LocalToCanvas;
	//速度比率参数
	uniform float _SpeedArg;
	//间隔
	uniform float _Interval;

	v2f vert(appdata_t IN)
	{
		v2f OUT;		
		//改下位置
		//计算左下角的canvas坐标，作为小方块的坐标,leftBottom.w是齐次坐标，要赋为1，不然平移信息会丢失
		float4 leftBottom = float4(IN.extraInfo, 0, 1);
		leftBottom = mul(_LocalToCanvas, leftBottom);
		//计算目标点的canvas坐标
		float4 target = float4(_Target,0,1);
		target = mul(_LocalToCanvas, target);
		//计算该方块到目标点的距离，以此为标准作为每个方块移动时间的延迟
		float distance = length(target.xyz - leftBottom.xyz);
		//距离越远的方块是否运行越快，_SpeedArg为1时，所有方块运行速度基本相同，值越小，距离越远的方块运行的越快
		float tempDis = 1 + distance * _SpeedArg;
		//计算一下小方块的位置 
		float f = clamp((_Delta - distance * _Interval) / tempDis, 0, 1);
		float3 position;
		position.x = lerp(leftBottom.x, target.x, f);
		position.y = lerp(leftBottom.y, target.y, f);
		position.z = lerp(leftBottom.z, target.z, f);
		//计算偏移量
		float3 delta = position.xyz - leftBottom.xyz;
		//设置透明度
		IN.color.w = clamp(lerp(10,0,f), 0, IN.color.w);
		OUT.worldPosition = IN.vertex;
		//把偏移量加到坐标上
		OUT.worldPosition.xyz += delta;

		OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

		OUT.texcoord = IN.texcoord;

#ifdef UNITY_HALF_TEXEL_OFFSET
		OUT.vertex.xy += (_ScreenParams.zw - 1.0)*float2(-1,1);
#endif

		OUT.color = IN.color * _Color;
		return OUT;
	}

	sampler2D _MainTex;

	fixed4 frag(v2f IN) : SV_Target
	{
		half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

		color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

#ifdef UNITY_UI_ALPHACLIP
		clip(color.a - 0.001);
#endif

		return color;
	}
		ENDCG
	}
	}
}