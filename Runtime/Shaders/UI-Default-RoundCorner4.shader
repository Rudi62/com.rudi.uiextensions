// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// Loaded from https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/UI/UI-Default.shader
// https://discussions.unity.com/t/ui-mask-with-shader/140418
Shader "Rudi/UI/RoundCorner4"
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
        _Radius      ( "Radius" , Vector ) = ( 0 , 0 , 0 , 0 )
        _Rectangle   ( "Rectangle" , Vector ) = ( 1 , 1 , 1 , 1 )
        _Scale       ( "Scale" , Vector ) = ( 1 , 1 , 1 , 1 )
        _CenterX     ( "CenterX" , Vector ) = ( 0 , 0 , 0 , 0 )
        _DropShadow  ( "DropShadow" , Vector ) = ( 0 , 0 , 0 , 0 )
        _ShadowDir   ( "ShadowDir" , Vector ) = ( 0 , 0 , 0 , 0 )
        _LightDir4   ( "LightDir4" , Vector ) = ( 0 , 0 , 0 , 0 )
        _AlphaMap    ( "AlphaMap" , Vector ) = ( 1 , 0 , 1 , 0 )
        _ColorAlpha0 ( "ColorAlpha0" , Vector ) = ( 1 , 1 , 1 , 0 )
        _ColorAlpha1 ( "ColorAlpha1" , Vector ) = ( 1 , 1 , 1 , 1 )


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
        //Blend SrcAlpha OneMinusSrcAlpha // ohne pma
        Blend One OneMinusSrcAlpha // mit pma
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.5 // original 2.0
            #pragma target 5.0 // original 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ MAP_TEX_ALPHA
            #pragma multi_compile_local _ MAP_TEX_COLOR_WITH_TEX_ALPHA
            #pragma multi_compile_local _ MAP_CORNER_ALPHA
            #pragma multi_compile_local _ PIXEL_CORRECT
            #pragma multi_compile_local _ DRAW_BORDER
            #pragma multi_compile_local _ DRAW_BORDER_FADED
            #pragma multi_compile_local _ ROUND_CORNERS
            #pragma multi_compile_local _ DROP_SHADOW


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
            float4 _Scale ;
            float4 _CenterX ;
            float4 _DropShadow ;
            float4 _ShadowDir ;
            float4 _LightDir4  ;
            float4 _AlphaMap  ;
            fixed4 _ColorAlpha0 ;
            fixed4 _ColorAlpha1 ;


            static const float4 Constants = { 0 , 0.5 , 1 , -1.0 } ;
            //static const float4 Constants2 = { 999999 , -999999 , 0 , 0 } ;
            ////static const float2 LightDir = { -1.41421356f , 1.41421356f } ;

            #define ZERO Constants.x
            #define ZERO4 Constants.xxxx
            #define HALF Constants.y
            #define HALF4 Constants.yyyy
            #define ONE Constants.z
            #define ONE4 Constants.zzzz
            #define MINUS_ONE Constants.w
            //#define BIG_NUMBER Constants2.x
            //#define BIG_NUMBER4 Constants2.xxxx
            //#define LOW_NUMBER Constants2.y
            //#define LOW_NUMBER4 Constants2.yyyy
            // Vector4 BorderWidth = new Vector4 ( fBorderWidth , BORDER_LOW , BORDER_HI , 0f ) ;
                            /*
                float Width = m_FadeBorder ? fBorderWidth : Smoothing ;
                float ByWidth = 1.0f / Mathf.Max ( Width , 0.00001f ) ;
                float BorderLo = m_FadeBorder ? -fBorderWidth : -fBorderWidth - HalfSmoothing ;
                float BorderLoByWidth = BorderLo * ByWidth ;
                Vector4 BorderWidth = new Vector4 ( BorderLoByWidth , BorderLo , Width , ByWidth ) ;

                */

            //#define BORDER_WIDTH          _BorderWidth.x
            //#define BORDER_LOW            _BorderWidth.y
            //#define BORDER_HI             _BorderWidth.z
            //#define SMOOTHED_BORDER_WIDTH _BorderWidth.w

            #define BORDER_LO_BY_WIDTH _BorderWidth.x
            #define BORDER_LO          _BorderWidth.y
            #define BORDER_WIDTH       _BorderWidth.z
            #define BORDER_BY_WIDTH    _BorderWidth.w


            #define HALF_SIZE       _Rectangle.x
            #define OUTLINE_LO_MLA  _Rectangle.y
            #define BY_SMOOTHING    _Rectangle.z
            #define HALF_SMOOTHING  _Rectangle.w

            #define CENTER_OFFSET     _CenterX.xy
            #define RectangleHalfSize _CenterX.zw

            #define ShadowAlpha        _DropShadow.x
            #define ShadowSigma        _DropShadow.y
            #define ShadowBySqrSigma   _DropShadow.z
            #define SignElevation      _DropShadow.w

            #define SHADOW_OFFSET      _ShadowDir.xy

            #define LIGHT_DIR_4 _LightDir4 // horX , horY , sin (vert) , cos (vert)
            #define LIGHT_DIR_HOR _LightDir4.xy
            #define LIGHT_DIR_VERT _LightDir4.zw
            #define LIGHT_DIR_3D ( float3 ( _LightDir4.xy * _LightDir4.z , _LightDir4.w ) )

            float compmin ( float4 v )
            {
                float2 p1 = min ( v.xy , v.zw ) ;
                return min ( p1.x , p1.y ) ;
            }
            float compmax ( float4 v )
            {
                float2 p1 = max ( v.xy , v.zw ) ;
                return max ( p1.x , p1.y ) ;
            }

            float gaussWeight ( float x , float by_sigma2 )
            {
                //   exp ( x² / ( 2 * sigma² ) )
                // = exp₂ ( x² * ( ( -log₂ ( e ) / 2 ) / ( sigma² ) ) )
                //                                                         ( -log₂ ( e ) / 2 ) = -0.72134752
                //return exp2 ( x * x * ( -0.72134752f * by_sigma2 ) ) ;
                return exp2 ( x * x * by_sigma2 ) ;
            }

            float polya ( float x )
            {
                float TWO_OVER_PI = -0.6366197723 * 4.0 ;
                float approximation = 0.5 * ( 1.0 + sqrt ( 1.0 - exp2 ( TWO_OVER_PI * x * x ) ) ) ;
                return x > 0.0 ? approximation : 1.0 - approximation ;

                //return 0.5 + 0.5 * sign ( x ) * sqrt ( 1.0 - exp2 ( x * x * by_sigma ) ) ;
            }

            float polya ( float x , float by_sigma ) { return polya ( x * by_sigma ) ; }

            float logisticFunction ( float x )
            {
                return 1.0 / ( 1.0 + exp ( -x ) ) ;
            }

            float logisticFunction ( float x , float by_sigma ) { return logisticFunction ( x * by_sigma * 1.7 * 2.0 ) ; }


            // ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // vertexshader
            // vertexshader helper functions

            float2 GetPixelAligned ( float2 pos )
            {
                float2 pixels = pos * _HalfScreenParams.xy + _HalfScreenParams.xy ;
                return round ( pixels ) * _HalfScreenParams.zw + MINUS_ONE ;
            }

            v2f vert ( appdata_t v )
            {
                v2f OUT ;
                UNITY_SETUP_INSTANCE_ID ( v ) ;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO ( OUT ) ;
                OUT.worldPosition = v.vertex ;

                OUT.vertex = UnityObjectToClipPos ( OUT.worldPosition ) ;

                #ifdef PIXEL_CORRECT
                OUT.vertex.xy = GetPixelAligned ( OUT.vertex.xy ) ;
                #endif

                OUT.texcoord = TRANSFORM_TEX ( v.texcoord , _MainTex ) ;

                OUT.color = v.color * _Color ;
                return OUT ;
            }


            // ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // pixelshader

            // https://docs.unity3d.com/Manual/SL-Properties.html
            // https://docs.unity3d.com/Manual/built-in-shader-examples-vertex-data.html
            // https://docs.unity3d.com/510/Documentation/Manual/SL-BuiltinIncludes.html
            // https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/CGIncludes/UnityCG.cginc

            // https://learn.microsoft.com/de-de/windows/win32/direct3dhlsl/dx-graphics-hlsl-intrinsic-functions?redirectedfrom=MSDN

            // sdf functions
            // thanks to Inigo Quilez
            // https://www.shadertoy.com/view/4llXD7

            // https://www.shadertoy.com/view/wlcXD2
            // .x = f(p)
            // .y = ∂f(p)/∂x
            // .z = ∂f(p)/∂y
            // .yz = ∇f(p) with ‖∇f(p)‖ = 1
            float3 sdgBox ( float2 p , float2 b , float4 ra )
            {
                //ra.xy = ( p.x > 0.0 ) ? ra.xy : ra.zw; // original code
                ra.xy = ( p.x > 0.0 ) ? ra.zw : ra.yx ; // remapping radii positions
                float r = ( p.y > 0.0 ) ? ra.x : ra.y;

                float2 w = abs ( p ) - ( b - r );
                float2 s = sign ( p );//float2(p.x<0.0?-1:1,p.y<0.0?-1:1);
                //float2 s = float2 ( p.x < 0.0 ? -1 : 1 , p.y < 0.0 ? -1 : 1 ) ;

                float g = max ( w.x , w.y );
                float2  q = max ( w , 0.0 );
                float l = length ( q );

                return float3 ( ( g > 0.0 ) ? l - r : g - r ,
                                s * ( ( g > 0.0 ) ? q / l : ( ( w.x > w.y ) ? float2 ( 1 , 0 ) : float2 ( 0 , 1 ) ) ) );
            }


            // b.x = half width
            // b.y = half height
            // r.x = roundness bottom-left
            // r.y = roundness top-left
            // r.z = roundness top-right  
            // r.w = roundness bottom-right
            float sdRoundBox ( float2 p , float2 b , float4 r )
            {
                // original code
                //r.xy = ( p.x > 0.0 ) ? r.xy : r.zw ;
                //r.x  = ( p.y > 0.0 ) ? r.x  : r.y  ;

                // must map to
                // r.x = roundness top-right  
                // r.y = roundness boottom-right
                // r.z = roundness top-left
                // r.w = roundness bottom-left

                r.xy = ( p.x > 0.0 ) ? r.zw : r.yx ;
                r.x  = ( p.y > 0.0 ) ? r.x  : r.y  ;
                float2 q = abs ( p ) - b + r.x ;
                return min ( max ( q.x , q.y ) , 0.0 ) + length ( max ( q , 0.0 ) ) - r.x ;
            }

            float sdBox ( float2 p , float2 b )
            {
                // original code
                //r.xy = ( p.x > 0.0 ) ? r.xy : r.zw ;
                //r.x  = ( p.y > 0.0 ) ? r.x  : r.y  ;

                // must map to
                // r.x = roundness top-right  
                // r.y = roundness boottom-right
                // r.z = roundness top-left
                // r.w = roundness bottom-left

                float2 q = abs ( p ) - b ;
                return min ( max ( q.x , q.y ) , 0.0 ) + length ( max ( q , 0.0 ) ) ;
            }


            float sdRoundBox_outside_only ( float2 p , float2 b , float4 r )
            {
                // original code
                //r.xy = ( p.x > 0.0 ) ? r.xy : r.zw ;
                //r.x  = ( p.y > 0.0 ) ? r.x  : r.y  ;

                // must map to
                // r.x = roundness top-right  
                // r.y = roundness boottom-right
                // r.z = roundness top-left
                // r.w = roundness bottom-left

                r.xy = ( p.x > 0.0 ) ? r.zw : r.yx ;
                r.x = ( p.y > 0.0 ) ? r.x : r.y  ;
                float2 q = abs ( p ) - b + r.x ;
                return length ( max ( q , 0.0 ) ) - r.x ;
            }

            float sdBox_inside_only ( float2 p , float2 b )
            {
                // original code
                //r.xy = ( p.x > 0.0 ) ? r.xy : r.zw ;
                //r.x  = ( p.y > 0.0 ) ? r.x  : r.y  ;

                // must map to
                // r.x = roundness top-right  
                // r.y = roundness boottom-right
                // r.z = roundness top-left
                // r.w = roundness bottom-left

                float2 q = abs ( p ) - b ;
                return min ( max ( q.x , q.y ) , 0.0 ) ;
            }
            float sdBox_outside_only ( float2 p , float2 b )
            {
                // original code
                //r.xy = ( p.x > 0.0 ) ? r.xy : r.zw ;
                //r.x  = ( p.y > 0.0 ) ? r.x  : r.y  ;

                // must map to
                // r.x = roundness top-right  
                // r.y = roundness boottom-right
                // r.z = roundness top-left
                // r.w = roundness bottom-left

                float2 q = abs ( p ) - b ;
                return length ( max ( q , 0.0 ) ) ;
            }
            bool sdBox_is_inside ( float2 p , float2 b )
            {
                // original code
                //r.xy = ( p.x > 0.0 ) ? r.xy : r.zw ;
                //r.x  = ( p.y > 0.0 ) ? r.x  : r.y  ;

                // must map to
                // r.x = roundness top-right  
                // r.y = roundness boottom-right
                // r.z = roundness top-left
                // r.w = roundness bottom-left

                float2 q = abs ( p ) - b ;
                return max ( q.x , q.y ) < 0.0f ;
                //return min ( max ( q.x , q.y ) , 0.0 ) ;
            }



            float smoothstep1 ( float border , float width , float value )
            {
                return smoothstep ( border - width , border + width , value ) ;
            }

            float lin_step_inv_width ( float lo , float invwidth , float value )
            {
                return saturate ( ( value - lo ) * invwidth ) ;
            }

            float lin_step_inv_width_mla ( float lo_invwidth , float invwidth , float value )
            {
                return saturate ( value * invwidth - lo_invwidth ) ;
            }

            float2 getPosPixels ( v2f IN )
            {
                return IN.texcoord.xy * _Scale.zw - _Scale.xy ;
            }

            fixed4 frag ( v2f IN ) : SV_Target
            {
                half4 color = tex2D ( _MainTex , IN.texcoord ) + _TextureSampleAdd ;

                #ifdef MAP_TEX_COLOR_WITH_TEX_ALPHA
                color = lerp ( _ColorAlpha0 , _ColorAlpha1 , color.a ) ;
                #else
                #ifdef MAP_TEX_ALPHA
                color.a = color.a * _AlphaMap.x + _AlphaMap.y ;
                #endif
                #endif

                // calc distance to rectangle

                #if defined ( DRAW_BORDER ) || defined ( ROUND_CORNERS ) || defined ( DROP_SHADOW ) || !defined ( DROP_SHADOW )
                const float2 Pos = getPosPixels ( IN ) ;

                #ifdef ROUND_CORNERS
                float4 ra = min ( _Radius , min ( RectangleHalfSize.x , RectangleHalfSize.y ) ) ;
                #else
                float4 ra = 0.0f ;
                #endif // ROUND_CORNERS

                #ifdef DRAW_BORDER

                #ifdef ROUND_CORNERS
                // corner and border
                float Dist = sdRoundBox ( Pos + CENTER_OFFSET , RectangleHalfSize , ra ) ;
                float CornerAlphaMultiplier = lin_step_inv_width_mla ( OUTLINE_LO_MLA , BY_SMOOTHING , Dist ) ;
                //CornerAlphaMultiplier = 0.0f ;
                #else
                // no corner but border
                float Dist = sdBox_inside_only ( Pos + CENTER_OFFSET , RectangleHalfSize ) ;
                float CornerAlphaMultiplier = Dist < 0.0f ? 1.0f : 0.0f ;
                #endif

                // sdBox_inside_only
                #else // DRAW_BORDER

                #ifdef ROUND_CORNERS
                // corner and no border
                float Dist = sdRoundBox_outside_only ( Pos + CENTER_OFFSET , RectangleHalfSize , ra ) ;
                float CornerAlphaMultiplier = lin_step_inv_width_mla ( OUTLINE_LO_MLA , BY_SMOOTHING , Dist ) ;
                #else // ROUND_CORNERS
                // no corner and no border
                float CornerAlphaMultiplier = sdBox_is_inside ( Pos + CENTER_OFFSET , RectangleHalfSize ) ? 1.0f : 0.0f ;

                //float Dist = sdBox_inside_only ( Pos + CENTER_OFFSET , RectangleHalfSize ) ;
                //float CornerAlphaMultiplier = Dist < 0.0f ? 1.0f : 0.0f ;

                #endif // ROUND_CORNERS
                //float CornerAlphaMultiplier = smoothstep1 ( 0.0 , HALF_SMOOTHING , -Dist ) ;
                //float CornerAlphaMultiplier = lin_step_inv_width ( HALF_SMOOTHING , BY_SMOOTHING , -Dist ) ;
                #endif // DRAW_BORDER
                #endif // defined ( DRAW_BORDER ) || defined ( ROUND_CORNERS ) || defined ( DROP_SHADOW )

                // map corner alpha (e.g. for holes)
                #if defined ( ROUND_CORNERS ) && defined ( MAP_CORNER_ALPHA )
                CornerAlphaMultiplier = CornerAlphaMultiplier * _AlphaMap.z + _AlphaMap.w ;
                #endif

                #ifdef DRAW_BORDER
                //float BorderPortion = smoothstep1 ( -BorderWidth , SMOOTHING , Dist ) ;
                //float BorderPortion = smoothstep ( BORDER_LOW , BORDER_HI , Dist ) ;
                float BorderPortion = lin_step_inv_width_mla ( BORDER_LO_BY_WIDTH , BORDER_BY_WIDTH , Dist ) ;
                #ifdef DRAW_BORDER_FADED
                //float BorderPortion = saturate ( 1.0f / ( 1.0f - Dist * BORDER_BY_WIDTH  ) ) ;
                BorderPortion *= BorderPortion ;
                #endif
                // lighten border color
                //float3 bc = _BorderColor.xyz ;
                //float2 Gradient = sdgBox ( Pos + CENTER_OFFSET , RectangleHalfSize , ra ).yz ;
                //float BorderLight = 1.0f - 0.2f * ( dot ( LIGHT_DIR_HOR , Gradient ) + 1.0f * 0.5f ) ;
                //float4 BorderColor = float4 ( _BorderColor.xyz * BorderLight , _BorderColor.w );
                color *= lerp ( _FillColor , _BorderColor , BorderPortion  ) ;
                #endif // DRAW_BORDER

                ////////////////////////////////////////////////////////////////////////////////////////////
                // apply vertex color
                color *= IN.color ;

                // pma code

                // Das originale, unmultiplizierte RGB sichern, bevor wir es für PMA umwandeln!
                float3 rawRGB = color.rgb;
                color.rgb *= color.a ; // mit pma

                                       
                // lighten border color
                //#ifdef DRAW_BORDER
                //{
                //    float2 Gradient = sdgBox ( Pos + CENTER_OFFSET , RectangleHalfSize , ra ).yz ;
                //    float BorderLight = 1.0f + 0.3f * dot ( LIGHT_DIR_HOR , -Gradient ) ;
                //    //BorderLight = ( BorderPortion > 0.0f ) ? BorderLight : 1.0f ;
                //    BorderLight = lerp ( 1.0f , BorderLight , BorderPortion ) ;
                //    color.xyz *= BorderLight ;
                //}
                //#endif

                ;/*
#ifdef DRAW_BORDER
                float RoundWidth = BORDER_WIDTH ;
#else
                float RoundWidth = HALF_SIZE ;
#endif
                // BORDER_WIDTH
                ////////////////////////////////////////////////////////////////////////////////////////////
                // inside shadow
                if ( false ) // true false
                {
                    const float Offset = 25.0f ;
                    float4 ra = min ( _Radius , min ( RectangleHalfSize.x , RectangleHalfSize.y ) ) ;
                    const float Dist = sdRoundBox ( Pos + CENTER_OFFSET + float2 ( 0 , Offset ), RectangleHalfSize , ra ) ;
                    float Darkening = 1.0f - 0.2f * smoothstep1 ( 0 , Offset , -Dist ) ;
                    color.xyz *= Darkening ;
                }

                ////////////////////////////////////////////////////////////////////////////////////////////
                // lightning
                if ( false )
                {
                    float4 ra = min ( _Radius , min ( RectangleHalfSize.x , RectangleHalfSize.y ) ) ;
                    const float3 DistGrad = sdgBox ( Pos + CENTER_OFFSET , RectangleHalfSize , ra ) ;
                    const float Dist = DistGrad.x ;
                    //const float Dist = sdRoundBox ( Pos + CENTER_OFFSET , RectangleHalfSize , ra ) ;
                    //float HalfSize = min ( RectangleHalfSize.x , RectangleHalfSize.y ) ;
                    const float2 Gradient =  ( DistGrad.yz ) ;
                    const float portion = 1.0f * ( 1.0f - saturate ( -Dist / RoundWidth ) ) ; // 0 is center, 1 is border - saturate not necessary
                    float Cos = sqrt ( 1.0f - portion * portion ) ;
                    float Sin = portion ;
                    float3 LightDir3 = float3 ( LIGHT_DIR_4.xy * LIGHT_DIR_4.z , LIGHT_DIR_4.w ) ;
                    float3 Normal = ( float3 ( -Gradient * Sin , Cos ) ) ;
                    // darkening
                    float Dot = saturate ( dot ( Normal , LightDir3 ) ) ;
                    float Darkening = ( Dot + 1.0f ) * 0.5f ;
                    //color.xyz *= Darkening ;

                    const float SpotPow = 50.0f ;
                    // gloss spot
                    float GlossSpot = 0.5f * pow ( saturate ( Dot ) , SpotPow ) ;
                    color.xyz = saturate ( color.xyz + GlossSpot ) ;

                    // gloss frame
                    float2 NormalV = float2 ( Sin , Cos ) ;
                    //float2 LightDirV = float2 ( sqrt ( 1.0f - LightDir.z * LightDir.z ) , LightDir.z ) ;
                    float2 LightDirV = normalize ( float2 ( 0.8f , 0.15f ) ) ;
                    float GlossFrameLightDot = dot ( NormalV , LightDirV ) ;
                    float GlossFrameLight = 0.5f * pow ( saturate ( GlossFrameLightDot ) , SpotPow ) ;
                    float2 LightDirH = LIGHT_DIR_4.xy ;
                    float GlossFrame = abs ( dot ( Gradient , LightDirH ) ) * GlossFrameLight ;
                    color.xyz = saturate ( color.xyz + GlossFrame ) ;
                }

                // */


                ////////////////////////////////////////////////////////////////////////////////////////////
                // shadow

#ifdef DROP_SHADOW

                //float2 halfSize = max ( RectangleHalfSize - ShadowSigma , 0.0f ) ; // ShadowSoftness
                float reduction = ShadowSigma * 1.0 ;
                float2 halfSize = RectangleHalfSize - reduction ; // ShadowSoftness


#ifdef ROUND_CORNERS

                //ra = min ( _Radius , min ( halfSize.x , halfSize.y ) ) ;
                ra = max ( ra - reduction , 0.0 ) ;

                float sdShadow = sdRoundBox ( Pos + CENTER_OFFSET + SHADOW_OFFSET , halfSize , ra ) ;
#else
                float sdShadow = sdBox ( Pos + CENTER_OFFSET + SHADOW_OFFSET , halfSize ) ;
                //float sdShadow = sdRoundBox_outside_only ( Pos + CenterOffset + ShadowOffset , halfSize , _Radius ) ;
#endif
                //sdShadow = max ( sdShadow , 0.0 ) ;
                //float shadowAlpha = ShadowAlpha * ( gaussWeight ( sdShadow , ShadowBySqrSigma ) ) ;
                //sdShadow = max ( sdShadow , 0.0 ) ;
                float shadowAlpha = ShadowAlpha * ( polya ( SignElevation * ( reduction - sdShadow ) , ShadowBySqrSigma ) ) ;

                // make anti-alias look nicely

                if ( SignElevation > 0.0 )
                {
                    //float BySumAlpha = rcp ( CornerAlphaMultiplier + shadowAlpha ) ;
                    //float fIn = CornerAlphaMultiplier * BySumAlpha ; // is 1 inside, 0 outside, between in smoothing transition
                    //float fOut = shadowAlpha * BySumAlpha ;

                    //float4 colMix = ( color * fIn + shadowAlpha * fOut ) * fIn ;
                    //colMix.a = shadowAlpha ;
                    //color = lerp ( colMix , color , CornerAlphaMultiplier );

                    // KI Vorschlag
                    //// 1. Beide Alphas parallel berechnen
                    //// alphas.x = elementAlpha, alphas.y = adjustedShadowAlpha
                    //float2 alphas = float2 ( CornerAlphaMultiplier , shadowAlpha ) * color.a ;

                    //// 2. Alpha-Blending mit den Vektor-Komponenten
                    //// 2. Deine Entdeckung: Das lerp ersetzt die mathematische Zeile komplett
                    //float finalAlpha = lerp ( alphas.y , 1.0 , alphas.x ) ;

                    //// 3. Finale RGB-Mischung
                    //float3 finalRGB = color.rgb * ( alphas.x * rcp ( max ( finalAlpha , 0.0001 ) ) ) ;

                    //color = float4 ( finalRGB , finalAlpha ) ;
                    // 
                    // farbige Schatten
                    // 1. Beide Alphas parallel berechnen (wie in deiner schnellen Lieblingsversion)
                    //float2 alphas = float2( CornerAlphaMultiplier , shadowAlpha ) * color.a;

                    //// 2. Das lerp für das finale Alpha
                    //float finalAlpha = lerp ( alphas.y , 1.0 , alphas.x );

                    //// 3. DIE RETTUNG FÜR DIE KANTE:
                    //// Wir nutzen exakt deine ursprüngliche RGB-Formel für die Kante!
                    //// Wenn das Element deckend ist (color.a = 1), ist shadowTint = 0.
                    //// Der Term reduziert sich zu exakt: alphas.x * rcp(finalAlpha). Die Kante bleibt perfekt!
                    //// Wenn das Element transparent ist, sorgt der shadowTint-Zusatz dafür, dass der Schatten farbig wird.
                    //float shadowTint = 1.0 - color.a;
                    //float rgbFactor = alphas.x + ( alphas.y * shadowTint * ( 1.0 - CornerAlphaMultiplier ) );

                    //float3 finalRGB = color.rgb * ( rgbFactor * rcp ( max ( finalAlpha , 0.0001 ) ) );

                    //color = float4( finalRGB , finalAlpha );

                    // Kombi
                    //// 1. Die beiden Alphas parallel berechnen
                    //float2 alphas = float2( CornerAlphaMultiplier , shadowAlpha ) * color.a;

                    //// 2. Mathematisch korrektes Alpha-Blending für das finale Alpha
                    //float finalAlpha = alphas.x + alphas.y * ( 1.0 - alphas.x );

                    //// 3. DER DYNAMISCHE ELEMENT-SCHUTZ:
                    //// Der Schatten wird im Inneren (CornerAlphaMultiplier == 1) NUR in dem Maße verdeckt, 
                    //// wie das Element tatsächlich deckend ist (color.a). 
                    //// Ist color.a = 1.0 (undurchsichtig), wird der Schatten im Element zu 0.
                    //// Ist color.a = 0.5 (halbtransparent), bleibt 50% des Schattens im Element aktiv!
                    //float occlusionFactor = CornerAlphaMultiplier * color.a;
                    //float cleanShadowAlpha = alphas.y * ( 1.0 - occlusionFactor );

                    //// 4. Deine originale, weiche Kante – absolut perfekt für beide Fälle
                    //float shadowTint = 1.0 - color.a;
                    //float3 finalRGB = color.rgb * lerp ( shadowTint , 1.0 , alphas.x * rcp ( max ( alphas.x + cleanShadowAlpha , 0.0001 ) ) );

                    //color = float4( finalRGB , finalAlpha );

                    // 

                    // //1. Die beiden Alphas parallel berechnen
                    //float2 alphas = float2( CornerAlphaMultiplier , shadowAlpha ) * color.a;

                    //// 2. Das finale Alpha (Over-Operator)
                    //float finalAlpha = alphas.x + alphas.y * ( 1.0 - alphas.x );

                    //// --- ZWEI WELTEN MATHEMATISCH TRENNEN ---

                    //// WELT A: Wenn das Element VÖLLIG UNDURCHSICHTIG ist (color.a == 1.0)
                    //float3 rgbUndurchsichtig = color.rgb * ( alphas.x * rcp ( max ( finalAlpha , 0.0001 ) ) );

                    //// WELT B: Wenn das Element TRANSPARENT ist (color.a < 1.0)
                    //// Schattenfarbe mit physikalisch korrekter Abdunklung
                    //float3 shadowRGB = color.rgb * ( 1.0 - color.a );
                    //float3 rgbTransparent = ( color.rgb * alphas.x ) + ( shadowRGB * alphas.y * ( 1.0 - alphas.x ) );
                    //rgbTransparent *= rcp ( max ( finalAlpha , 0.0001 ) );

                    //// --- DEINE OPTIMIERTE QUADRATISCHE KURVE ---
                    //// Schaltet den farbigen Schatten viel früher und knackiger ein!
                    //float blendCurve = color.a * color.a;
                    //float3 finalRGB = lerp ( rgbTransparent , rgbUndurchsichtig , blendCurve );

                    //color = float4( finalRGB , finalAlpha );

                    //

                    //// 1. Alphas parallel in einem Takt berechnen
                    //float2 alphas = float2( CornerAlphaMultiplier , shadowAlpha ) * color.a;

                    //// 2. Das finale Alpha bleibt unverändert blitzschnell
                    //float finalAlpha = alphas.x + alphas.y * ( 1.0 - alphas.x );
                    //float rcpAlpha = rcp ( max ( finalAlpha , 0.0001 ) );

                    //// 3. MATHEMATISCHE SYNTHESE: Wir dampfen Welt A und Welt B in eine einzige Zeile ein!
                    //// Anstatt zwei getrennte RGB-Vektoren zu berechnen und zu lerpen, 
                    //// interpolieren wir nur noch einen einzigen Skalar-Faktor vor der Farb-Multiplikation.
                    //float shadowFactor = ( 1.0 - color.a ) * alphas.y * ( 1.0 - alphas.x );
                    //float rgbFactor = lerp ( alphas.x + shadowFactor , alphas.x , color.a * color.a );

                    //// 4. Finale RGB-Mischung: Nur noch EINE einzige Vektor-Multiplikation für die Farbe!
                    //float3 finalRGB = color.rgb * ( rgbFactor * rcpAlpha );

                    //color = float4( finalRGB , finalAlpha );

                    //
                    
                    //// 1. Die beiden Alphas parallel in einem Takt berechnen
                    //float2 alphas = float2 ( CornerAlphaMultiplier , shadowAlpha ) * color.a ;

                    //// 2. Beide Invertierungen parallel in einem einzigen Takt berechnen
                    //float2 invValues = 1.0 - float2 ( color.a , alphas.x ) ; // invValues.x = 1 - color.a | invValues.y = 1 - alphas.x

                    //// 3. DEINE ENTDECKUNG: finalAlpha als reine, blitzschnelle MLA-Instruktion!
                    //float finalAlpha = alphas.x + alphas.y * invValues.y ;
                    //float rcpAlpha = rcp ( max ( finalAlpha , 0.0001 ) ) ;

                    //// 4. Deine quadratische Kurve
                    //float blendCurve = color.a * color.a ;

                    //// 5. Skalar-Faktoren berechnen (factorTransparent nutzt jetzt ebenfalls MLA-Struktur)
                    //float factorUndurchsichtig = alphas.x ;
                    //float factorTransparent = alphas.x + ( invValues.x * alphas.y ) * invValues.y ;

                    //// 6. Finalen Farbmultiplikator ermitteln und anwenden
                    //float finalFactor = lerp ( factorTransparent , factorUndurchsichtig , blendCurve ) ;
                    //float3 finalRGB = color.rgb * ( finalFactor * rcpAlpha ) ;

                    //color = float4( finalRGB , finalAlpha ) ;

                    // pma
                    // --- UNSERE OPTIMIERTE MATH-PIPELINE (PMA-VERSION) ---

                    // 1. Die beiden Alphas parallel berechnen (wie gehabt)
                    float2 alphas = float2( CornerAlphaMultiplier , shadowAlpha ) * color.a ;

                    // 2. Beide Invertierungen parallel berechnen
                    float2 invValues = 1.0 - float2( color.a , alphas.x ) ;

                    // 3. finalAlpha berechnen (MLA-Instruktion)
                    float finalAlpha = alphas.x + alphas.y * invValues.y ;

                    // 4. Deine quadratische Kurve
                    float blendCurve = color.a * color.a ;

                    // 5. Die Skalar-Faktoren für die Farbstärke (ohne Division!)
                    float factorUndurchsichtig = alphas.x ;
                    float factorTransparent = alphas.x + ( invValues.x * alphas.y ) * invValues.y ;
                    float finalFactor = lerp ( factorTransparent , factorUndurchsichtig , blendCurve ) ;

                    // 6. FINALES PRE-MULTIPLIED ALPHA ERGEBNIS:
                    // WICHTIG: Wir nutzen hier 'rawRGB' (das unmultiplizierte RGB). 
                    // Da finalFactor bereits alle Alphas enthält, liefert das das perfekte PMA-Ergebnis!
                    float3 finalRGB = rawRGB * finalFactor ;

                    color = float4 ( finalRGB , finalAlpha ) ;

                }
                else
                {
                    //color.xyz = lerp ( color.xyz , 0.0 , shadowAlpha ) ; // pma
                    color.xyz *= ( 1.0 - shadowAlpha ) ; // pma
                    //color.a *= CornerAlphaMultiplier ; // ohne pma
                    color *= CornerAlphaMultiplier ; // mit pma
                }


#elif defined ( ROUND_CORNERS )
                // no shadow but round corners.
                // There we can use CornerAlphaMultiplier directly
                //color.a *= CornerAlphaMultiplier ; // ohne pma
                color *= CornerAlphaMultiplier ; // mit pma
#endif

                // unity 2d clipping

                #ifdef UNITY_UI_CLIP_RECT
                //color.a *= UnityGet2DClipping ( IN.worldPosition.xy , _ClipRect ); // ohne pma
                color *= UnityGet2DClipping ( IN.worldPosition.xy , _ClipRect ); // mit pma
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
