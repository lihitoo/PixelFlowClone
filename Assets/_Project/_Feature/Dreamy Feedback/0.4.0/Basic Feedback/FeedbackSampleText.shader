Shader "Dreamy/Feedback/SampleText"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _GradientScale ("SDF Gradient", Float) = 5
        _TextureWidth ("Atlas Width", Float) = 1024
        _TextureHeight ("Atlas Height", Float) = 1024
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        _ZTestMode ("ZTest", Float) = 4
        _CullMode ("Cull Mode", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull [_CullMode]
        ZWrite Off
        ZTest [_ZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPosition : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _FaceColor;
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o; o.worldPosition = v.vertex; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color * _FaceColor; o.uv = v.uv; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float sdf = tex2D(_MainTex, i.uv).a;
                float softness = max(fwidth(sdf), .001);
                fixed4 color = i.color; color.a *= smoothstep(.5 - softness, .5 + softness, sdf);
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
