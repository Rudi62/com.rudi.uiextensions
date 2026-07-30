// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Rudi/UI/PixelAlign"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask    ("Color Mask", Float) = 15
        _MyScreenSize ( "MyScreenSize" , Vector ) = ( 0 , 0 , 0 , 0 )

        //[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha // Zero One
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_EDITOR

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex ;
            fixed4 _Color ;
            fixed4 _TextureSampleAdd ;
            float4 _ClipRect ;
            float4 _MainTex_ST ;
            float4 _MainTex_TexelSize ;
            float4 _MyScreenSize ;

            float2 Origin = float2 ( 0 , 0 ) ;

            // https://discussions.unity.com/t/unitypixelsnap-in-surface-shader-2d-sprites/625296
            // https://discussions.unity.com/t/please-explain-unitypixelsnap-in-builtin-shader/197298
            // https://discussions.unity.com/t/correct-way-of-converting-to-from-screenspace-screen-width-screen-height-position-in-a-shader/627468/2

            float4 GetPixelAligned ( float4 pos )
            {
                return UnityPixelSnap ( pos ) ;
            }
            float2 GetPixelAligned ( float2 pos )
            {
                // https://stackoverflow.com/questions/34952184/pixel-perfect-shader-in-unity-shaderlab

                // clip space goes from -1 , 1 , screen pixels from 0 to num-pixels
                // so to align we have to:
                //  - transform from clip space to screen space
                //  - round to pixels
                //  - transform back to clip space
                // _ScreenParams is defined:
                //  - x: the width of the camera’s target texture in pixels
                //  - y: the height of the camera’s target texture in pixels
                //  - z: 1.0 + 1.0 / width
                //  - w: 1.0 + 1.0 / height
                // _HalfScreenParams should have:
                //  - x: width  / 2
                //  - y: height / 2
                //  - z: 2 / width
                //  - z: 2 / height
                // clip to screen:
                // screen_xy = ( clip_xy * 0.5 + 0.5 ) * screen_size_xy
                //           = clip_xy * ( 0.5 * screen_size_xy ) + ( 0.5 * screen_size_xy )
                // clip_xy = ( screen_xy / screen_size_xy ) * 2 - 1
                //         = screen_xy * 2 / screen_size_xy - 1
                //
                float4 _HalfScreenParams = ( _ScreenParams - float4 ( 0 , 0 , 1 , 1 ) ) * float4 ( 0.5f , 0.5f , 2.0f , 2.0f ) ;
                float2 pixels = pos * _HalfScreenParams.xy + _HalfScreenParams.xy ; // pixels goes from 0 to num_pixels
                return ( round ( pixels ) ) * _HalfScreenParams.zw - float2 ( 1 , 1 ) ;
                //return pos ;
            }

            bool isSceneView ()
            {
                //float2 diff = abs ( _ScreenParams.xy - _MyScreenSize.xy ) ;
                //return diff.x + diff.y > 0 ;
                return any ( _ScreenParams.xy != _MyScreenSize.xy ) ;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v) ;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT) ;
                OUT.worldPosition = v.vertex ;
                OUT.vertex = UnityObjectToClipPos ( OUT.worldPosition ) ;
#ifdef UNITY_EDITOR 
                OUT.vertex = isSceneView () ? OUT.vertex : UnityPixelSnap ( OUT.vertex ) ;
                //OUT.vertex.xy = isSceneView () ? OUT.vertex.xy : GetPixelAligned ( OUT.vertex.xy ) ;
#else
                OUT.vertex = UnityPixelSnap ( OUT.vertex ) ;
                //OUT.vertex.xy = GetPixelAligned ( OUT.vertex.xy ) ;
#endif
                OUT.texcoord = TRANSFORM_TEX ( v.texcoord , _MainTex ) ;
                //OUT.texcoord = v.texcoord ;

                //float2 Aligned = GetPixelAligned ( OUT.vertex.xy ) ;
                //OUT.vertex.xy = OUT.texcoord.xy < 0.5f ? round ( OUT.vertex.xy ) : OUT.vertex.xy ;
                //OUT.texcoord.xy = OUT.texcoord.xy < 0.5f ? _MainTex_TexelSize.xy * 0.5f : OUT.texcoord.xy ;

                //if ( OUT.texcoord.x < 0.5f )
                //{
                //    //OUT.vertex.x = round ( OUT.vertex.x ) ;
                //    OUT.vertex.x = Aligned.x ;
                //    //OUT.texcoord.x = _MainTex_TexelSize.x * 0.5f ;
                //    //OUT.texcoord.x = 0 ;
                //}
                //if ( OUT.texcoord.y < 0.5f )
                //{
                //    //OUT.vertex.y = round ( OUT.vertex.y ) ;
                //    OUT.vertex.y = Aligned.y ;
                //    //OUT.texcoord.y = _MainTex_TexelSize.y * 0.5f ;
                //    //OUT.texcoord.y = 0 ;
                //}
                //OUT.vertex.xy = Aligned ;

                //OUT.worldPosition.xy = OUT.vertex.xy ;
                //if ( any ( OUT.texcoord < float2 ( 0.5f , 0.5f ) ) )
                //{
                //    Origin = OUT.worldPosition.xy ;
                //}
                //else
                //{
                //    OUT.worldPosition.xy = Origin ;
                //}

                OUT.color = v.color * _Color ;
                return OUT;
            }

            float getUvOutsideAlpha ( float2 uv )
            {
                //float2 Mirror = float2 ( 1.0f , 1.0f ) -
                float MaxUV = max ( uv.x , uv.y ) ;
                float MinUV = min ( uv.x , uv.y ) ;
                float MinMin = min ( 1.0f - MaxUV , MinUV ) ;

                return MinMin < 0 ? 0 : 1 ;
            }


            float2 sqr ( float2 v ) { return v * v ; }

            fixed4 frag ( v2f IN ) : SV_Target
            {
                //float2 uv = IN.texcoord ;
                float2 duv = abs ( float2 ( ddx ( IN.texcoord.x ) , ddy ( IN.texcoord.y ) ) ) ; // ddx_fine ddx ddy_fine ddy
                //float2 ByDuv = rcp ( duv ) ;
                float2 index = floor ( IN.texcoord / duv ) ;
                float2 uv_aligned = ( index + 0.5f ) * _MainTex_TexelSize.xy ;
#ifdef UNITY_EDITOR 
                float2 uv = isSceneView () ? IN.texcoord.xy : uv_aligned ;
#else
                float2 uv = uv_aligned ;
#endif
                half4 color = ( tex2D ( _MainTex , uv ) + _TextureSampleAdd ) * IN.color ;
                color.a *= getUvOutsideAlpha ( uv ) ;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip ( color.a - 0.001 ) ;
                #endif

                return color ;
            }
        ENDCG
        }
    }
}
