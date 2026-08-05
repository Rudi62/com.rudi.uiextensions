// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// Loaded from https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/UI/UI-Default.shader
// https://discussions.unity.com/t/ui-mask-with-shader/140418
Shader "Rudi/UI/RoundCorner2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ( "Tint", Color ) = ( 1,1,1,1 )

        // mask compatibility
        _StencilComp ( "Stencil Comparison", Float ) = 8
        _Stencil ( "Stencil ID", Float ) = 0
        _StencilOp ( "Stencil Operation", Float ) = 0
        _StencilWriteMask ( "Stencil Write Mask", Float ) = 255
        _StencilReadMask ( "Stencil Read Mask", Float ) = 255
        _ColorMask ("Color Mask", Float ) = 15

        _HalfScreenParams ( "HalfScreenParams" , Vector ) = ( 0 , 0 , 0 , 0 )
        _BorderWidth ( "BorderWidth" , Vector ) = ( 0 , 0 , 0 , 0 )
        _BorderColor ( "BorderColor", Color ) = ( 0,0,0,1 )
        _FillColor ( "FillColor", Color ) = ( 1,1,1,1 )
        _Radius ( "Radius" , Vector ) = ( 0 , 0 , 0 , 0 )
        _Rectangle ( "Rectangle" , Vector ) = ( 1 , 1 , 1 , 1 )
        _CenterX ( "CenterX" , Vector ) = ( 0 , 0 , 0 , 0 )
        _CenterY ( "CenterY" , Vector ) = ( 0 , 0 , 0 , 0 )
        _AlphaMap ( "AlphaMap" , Vector ) = ( 1 , 0 , 1 , 0 )


        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.5 // original 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ MAP_TEX_ALPHA
            #pragma multi_compile_local _ MAP_CORNER_ALPHA
            #pragma multi_compile_local _ PIXEL_CORRECT
            #pragma multi_compile_local _ DRAW_BORDER
            #pragma multi_compile_local _ ROUND_CORNERS


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex ;
            fixed4 _Color ;
            fixed4 _TextureSampleAdd ;
            float4 _ClipRect ;
            float4 _MainTex_ST ;
            float4 _HalfScreenParams ;
            float4 _BorderWidth ;
            fixed4 _BorderColor ;
            fixed4 _FillColor ;
            float4 _Radius ;
            float4 _Rectangle ;
            float4 _CenterX ;
            float4 _CenterY ;
            float4 _AlphaMap ;

            static const float4 Constants = { 0 , 0.5 , 1 , -1.0 } ;
            static const float4 Constants2 = { 999999 , -999999 , 0 , 0 } ;

            #define ZERO Constants.x
            #define ZERO4 Constants.xxxx
            #define HALF Constants.y
            #define HALF4 Constants.yyyy
            #define ONE Constants.z
            #define ONE4 Constants.zzzz
            #define MINUS_ONE Constants.w
            #define BIG_NUMBER Constants2.x
            #define BIG_NUMBER4 Constants2.xxxx
            #define LOW_NUMBER Constants2.y
            #define LOW_NUMBER4 Constants2.yyyy

            #define BorderWidth _BorderWidth.x
            #define SMOOTHED_BORDER_WIDTH _BorderWidth.w
            #define BORDER_FADE _BorderWidth.y
            #define SMOOTHED_BORDER_FADE _BorderWidth.z

            //#define SMOOTHING _Rectangle.w
            #define BY_SMOOTHING _Rectangle.z

            float getMinVal ( float4 v )
            {
                float2 p1 = min ( v.xy , v.zw ) ;
                return min ( p1.x , p1.y ) ;
            }
            float getMaxVal ( float4 v )
            {
                float2 p1 = max ( v.xy , v.zw ) ;
                return max ( p1.x , p1.y ) ;
            }

            //float gaussWeight ( float x )
            //{
            //    //   exp ( x² / ( 2 * sigma² ) )
            //    // = exp₂ ( x² * ( ( -log₂ ( e ) / 2 ) / ( sigma² ) ) )
            //    //                                                         ( -log₂ ( e ) / 2 ) = -0.72134752
            //    return exp2 ( x * x * ( -0.72134752f * ByBorderSigma2 ) ) ;
            //}


            // ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // vertexshader
            // vertexshader helper functions

            float2 GetPixelAligned1 ( float2 pos )
            {
                float2 pixels = pos * _HalfScreenParams.xy + _HalfScreenParams.xy ;
                return round ( pixels ) * _HalfScreenParams.zw + MINUS_ONE ;
            }

            v2f vert ( appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;

                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                #ifdef PIXEL_CORRECT
                OUT.vertex.xy = GetPixelAligned1 ( OUT.vertex.xy ) ;
                #endif

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }


            // ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // pixelshader

            // https://docs.unity3d.com/Manual/SL-Properties.html
            // https://docs.unity3d.com/Manual/built-in-shader-examples-vertex-data.html
            // https://docs.unity3d.com/510/Documentation/Manual/SL-BuiltinIncludes.html
            // https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/CGIncludes/UnityCG.cginc

            // https://learn.microsoft.com/de-de/windows/win32/direct3dhlsl/dx-graphics-hlsl-intrinsic-functions?redirectedfrom=MSDN

            // pixelshader helper functions


            //float getCircleCenterDist ( float cx , float cy , v2f IN )
            //{
            //    float diffX = ( cx - IN.texcoord.x ) * _Rectangle.x ;
            //    float diffY = ( cy - IN.texcoord.y ) * _Rectangle.y ;
            //    float dist = sqrt ( diffX * diffX + diffY * diffY ) ;
            //    return dist ;
            //}

            //float getEdgeDist ( float cx , float cy , float mulx , float muly , v2f IN )
            //{
            //    float diffX = abs ( cx - IN.texcoord.x ) * _Rectangle.x * mulx ;
            //    float diffY = abs ( cy - IN.texcoord.y ) * _Rectangle.y * muly ;
            //    return diffX + diffY ;
            //}

            //float isInCorner ( float cx , float cy , float radius , v2f IN )
            //{
            //    float diffX = abs ( cx - IN.texcoord.x ) * _Rectangle.x ;
            //    float diffY = abs ( cy - IN.texcoord.y ) * _Rectangle.y ;
            //    return radius >= max ( diffX , diffY ) ? 1 : 0 ; // returns 1 or 0
            //}

            float4 isInCorner4 ( v2f IN )
            {
                float4 diff = abs ( _Rectangle.wyxw - IN.texcoord.xyxy * _Rectangle.xyxy ) ;
                //return step ( max ( diff.xxzz , diff.wyyw ) , _Radius ) ;
                return saturate ( ( _Radius - max ( diff.xxzz , diff.wyyw ) ) * BIG_NUMBER4 ) ;
            }

            float4 getEdgeDist4 ( v2f IN )
            {
                return abs ( _Rectangle.wwxy - IN.texcoord.xyxy  * _Rectangle.xyxy ) ; // use mla instruction
            }

            float4 getCircleCenterDist4 ( v2f IN )
            {
                float4 diffx = ( _CenterX - IN.texcoord.xxxx * _Rectangle.xxxx ) ;
                float4 diffy = ( _CenterY - IN.texcoord.yyyy * _Rectangle.yyyy )  ;
                float4 dist = sqrt ( diffx * diffx + diffy * diffy ) ;
                return dist ;
            }

            fixed4 frag ( v2f IN ) : SV_Target
            {
                half4 color = tex2D ( _MainTex , IN.texcoord ) + _TextureSampleAdd ;

                #ifdef MAP_TEX_ALPHA
                color.a = color.a * _AlphaMap.x + _AlphaMap.y ;
                #endif

                //#ifdef UNITY_UI_CLIP_RECT
                //color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                //#endif

                //#ifdef UNITY_UI_ALPHACLIP
                //clip (color.a - 0.001);
                //#endif

                /////////////////////////////////////////////////////////////////////////////////////////////////////
                // Border

                #ifdef DRAW_BORDER

                float4 EdgeDist4 = getEdgeDist4 ( IN ) ;
                float EdgeDist = HALF4 - getMinVal ( EdgeDist4 ) * BY_SMOOTHING ;

                #endif // DRAW_BORDER

                /////////////////////////////////////////////////////////////////////////////////////////////////////
                // round corners

                #ifdef ROUND_CORNERS

                float4 IsInCorner4 = isInCorner4 ( IN ) ;

                float4 CenterDist4 = getCircleCenterDist4 ( IN ) ;
                float4 CircleDist4 = _Radius - CenterDist4 ;
                float4 SmoothedCircleDist4 = CircleDist4 * BY_SMOOTHING + ONE4 ;

                //float CornerAlphaMultiplier = saturate ( getMinVal ( IsInCorner4 > HALF4 ? SmoothedCircleDist4 : ONE4 ) ) ;
                float CornerAlphaMultiplier =  any ( IsInCorner4 ) ? saturate ( getMaxVal ( SmoothedCircleDist4 * IsInCorner4 ) ) : ONE ;


                #ifdef MAP_CORNER_ALPHA
                //CornerAlphaMultiplier = ONE - CornerAlphaMultiplier ;
                CornerAlphaMultiplier = CornerAlphaMultiplier * _AlphaMap.z + _AlphaMap.w ;
                #endif

                #ifdef DRAW_BORDER

                //float4 BorderPortionCorner4 = saturate ( ( SMOOTHED_BORDER_WIDTH + SmoothedCircleDist4 ) * BORDER_FADE ) ;
                //float BorderPortionCorner = saturate ( dot ( IsInCorner4 , BorderPortionCorner4 ) ) ; // portion value are valid only if pixel is in a corner
                //float CircleDist = dot ( SmoothedCircleDist4 , IsInCorner4 ) ;
                //EdgeDist = ( 1f - any ( IsInCorner4 ) ) * EdgeDist ;
                //EdgeDist += CircleDist ;
                //float4 NotInCorner = ( ONE4 - IsInCorner4 ) * BIG_NUMBER4 ;
                //float4 NotInCorner = IsInCorner4 ? ZERO4 : LOW_NUMBER4 ; // BIG_NUMBER4 BIG_NUMBER4
                //float CircleDist = getMaxVal ( IsInCorner4 > HALF4 ?  - CircleDist4  : LOW_NUMBER4 ) * BY_SMOOTHING ;
                float CircleDist = - BY_SMOOTHING * getMaxVal ( CircleDist4 * IsInCorner4 )  ;
                EdgeDist = any ( IsInCorner4 ) ? CircleDist : EdgeDist ;
                //float BorderPortionCorner = gaussWeight ( dot ( SmoothedCircleDist4 , IsInCorner4 ) ) ;
                //BorderPortion = max ( BorderPortionCorner , BorderPortion ) ; // calculate final border portion

                #endif // DRAW_BORDER
                #endif // ROUND_CORNERS

                ////////////////////////////////////////////////////////
                // calculate border and round corner into color

                #ifdef DRAW_BORDER
                //float BorderPortion = gaussWeight ( EdgeDist ) ;
                float BorderPortion = saturate ( SMOOTHED_BORDER_FADE + EdgeDist * BORDER_FADE ) ;
                BorderPortion *= BorderPortion ;
                color *= lerp ( _FillColor , _BorderColor , BorderPortion  ) ; // color _BorderColor
                #endif

                color *= IN.color ;

                #ifdef ROUND_CORNERS
                color.a *= CornerAlphaMultiplier ;
                #endif

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping ( IN.worldPosition.xy , _ClipRect );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip ( color.a - 0.001 );
                #endif


                return color;
            }
        ENDCG
        }
    }
}
