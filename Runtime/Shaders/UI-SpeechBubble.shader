// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// Loaded from https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/UI/UI-Default.shader
// https://discussions.unity.com/t/ui-mask-with-shader/140418
Shader "Rudi/UI/SpeechBubble"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ( "Tint", Color ) = ( 1,1,1,1 )

        // mask compatibility
        _StencilComp      ( "Stencil Comparison", Float ) = 8
        _Stencil          ( "Stencil ID", Float ) = 0
        _StencilOp        ( "Stencil Operation", Float ) = 0
        _StencilWriteMask ( "Stencil Write Mask", Float ) = 255
        _StencilReadMask  ( "Stencil Read Mask", Float ) = 255
        _ColorMask        ( "Color Mask", Float ) = 15

        _HalfScreenParams ( "HalfScreenParams" , Vector ) = ( 0 , 0 , 0 , 0 )
        _Triangle02  ( "Triangle02"  , Vector ) = ( 0 , 0 , 0 , 0 )
        _Triangle1   ( "Triangle1"   , Vector ) = ( 0 , 0 , 0 , 0 )
        _BorderWidth ( "BorderWidth" , Vector ) = ( 0 , 0 , 0 , 0 )
        _BorderColor ( "BorderColor" , Color  ) = ( 0 , 0 , 0 , 1 )
        _FillColor   ( "FillColor"   , Color  ) = ( 1 , 1 , 1 , 1 )
        //_Radius      ( "Radius"      , Vector ) = ( 0 , 0 , 0 , 0 )
        _Radius      ( "Radius"      , Float ) = 0
        _Rectangle   ( "Rectangle"   , Vector ) = ( 1 , 1 , 1 , 1 )
        _Scale       ( "Scale"       , Vector ) = ( 1 , 1 , 1 , 1 )
        _CenterX     ( "CenterX"     , Vector ) = ( 0 , 0 , 0 , 0 )
        _DropShadow  ( "DropShadow"  , Vector ) = ( 0 , 0 , 0 , 0 )
        _ShadowDir   ( "ShadowDir"   , Vector ) = ( 0 , 0 , 0 , 0 )
        _LightDir4   ( "LightDir4"   , Vector ) = ( 0 , 0 , 0 , 0 )
        _AlphaMap    ( "AlphaMap"    , Vector ) = ( 1 , 0 , 1 , 0 )
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0 // original 2.0
            //#pragma target 5.0 // original 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            //#pragma multi_compile_local _ MAP_TEX_ALPHA
            //#pragma multi_compile_local _ MAP_TEX_COLOR_WITH_TEX_ALPHA
            //#pragma multi_compile_local _ MAP_CORNER_ALPHA
            //#pragma multi_compile_local _ PIXEL_CORRECT
            #pragma multi_compile_local _ DRAW_BORDER
            #pragma multi_compile_local _ DRAW_BORDER_FADED
            //#pragma multi_compile_local _ ROUND_CORNERS
            //#pragma multi_compile_local _ DROP_SHADOW


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
            //float4 _Radius ;
            float  _Radius ;
            float4 _Rectangle ;
            float4 _Scale ;
            float4 _CenterX ;
            float4 _DropShadow ;
            float4 _ShadowDir ;
            float4 _LightDir4  ;
            float4 _AlphaMap  ;
            fixed4 _ColorAlpha0 ;
            fixed4 _ColorAlpha1 ;
            float4 _Triangle02  ;
            float4 _Triangle1  ;


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

            #define CENTER_OFFSET      _CenterX.xy
            #define RectangleHalfSize _CenterX.zw

            #define ShadowAlpha        _DropShadow.x
            #define ShadowSigma        _DropShadow.y
            #define ShadowBySqrSigma   _DropShadow.z
            //#define ShadowOffsetLength _DropShadow.w

            #define SHADOW_OFFSET      _ShadowDir.xy

            #define LIGHT_DIR_4 _LightDir4 // horX , horY , sin (vert) , cos (vert)
            #define LIGHT_DIR_HOR _LightDir4.xy
            #define LIGHT_DIR_VERT _LightDir4.zw
            #define LIGHT_DIR_3D ( float3 ( _LightDir4.xy * _LightDir4.z , _LightDir4.w ) )

            #define TrianglePoint0 _Triangle02.xy
            #define TrianglePoint1 _Triangle1.xy
            #define TrianglePoint2 _Triangle02.zw

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
                // r.xyzw:
                //     y---z
                //     |   |
                //     x---w
                // original code
                //     z---x
                //     |   |
                //     w---y
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

            float sdRoundBox ( float2 p , float2 b , float r )
            {
                float2 q = abs ( p ) - b + r ;
                return min ( max ( q.x , q.y ) , 0.0 ) + length ( max ( q , 0.0 ) ) - r ;
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

            float sdTriangle ( float2 p , float2 p0 , float2 p1 , float2 p2 )
            {
                // https://iquilezles.org/articles/distfunctions2d/
                float2 e0 = p1 - p0 , e1 = p2 - p1 , e2 = p0 - p2 ;
                float2 v0 = p - p0 , v1 = p - p1 , v2 = p - p2 ;
                float2 pq0 = v0 - e0 * clamp ( dot ( v0 , e0 ) / dot ( e0 , e0 ) , 0.0f , 1.0f ) ;
                float2 pq1 = v1 - e1 * clamp ( dot ( v1 , e1 ) / dot ( e1 , e1 ) , 0.0f , 1.0f ) ;
                float2 pq2 = v2 - e2 * clamp ( dot ( v2 , e2 ) / dot ( e2 , e2 ) , 0.0f , 1.0f ) ;
                float s = sign ( e0.x * e2.y - e0.y * e2.x ) ;
                float2 d = min ( min (
                    float2 ( dot ( pq0 , pq0 ) , s * ( v0.x * e0.y - v0.y * e0.x ) ) ,
                    float2 ( dot ( pq1 , pq1 ) , s * ( v1.x * e1.y - v1.y * e1.x ) ) ) ,
                    float2 ( dot ( pq2 , pq2 ) , s * ( v2.x * e2.y - v2.y * e2.x ) ) ) ;
                return -sqrt ( d.x ) * sign ( d.y ) ;
            }

            float sd_V ( float2 p , float4 d12 , float4 p1_dot12 )
            {
                // for speech bubble tail, an optimized test is sufficient, no need for full triangle calculation
                // 
                //    p0    p2        d1 = p0 - p1
                //     \   /          d2 = p2 - p1
                //      \ /           d12 = ( d1.x , d1.y , d2.x , d2.y )
                //       V            p1_dot12 = ( p1.x , p1.y , 1 / dot ( d1 , d1 ) , 1 / dot ( d2 , d2 ) )
                //       p1

                // fetch variables out of function parameters
                float2 d1        = d12.xy ;
                float2 d2        = d12.zw ;
                float2 p1        = p1_dot12.xy ;
                float by_dot_d12 = p1_dot12.z ;
                float dot_d1_d2  = p1_dot12.w ;

                float2 v = p - p1 ;                                                    // vector p1 to p
                float2 t12 = float2 ( dot ( v , d1 ) , dot ( v , d2 ) ) * by_dot_d12 ; // t12 are the parameters for projection on both lines
                //float2 t12_clamped = clamp ( t12 , 0 , 1 ) ;                         // t12_clamped are the parameters for projection within line segments
                t12 = min ( t12 , 1 ) ;
                // clamping to 0 not necessary as p1 is on edge of rect

                // experimental approach
                float4 np12 = p1.xyxy + d12 * t12.xxyy ; // np12 = ( np1.x , np1.y , np2.x , np2.y )
                float4 dnp12 = np12 - p.xyxy ;
                float4 Dist2_np12 = dnp12 * dnp12 ;
                Dist2_np12.xy = Dist2_np12.xz + Dist2_np12.yw ;
                float dist = sqrt ( min ( Dist2_np12.x , Dist2_np12.y ) ) ;
                // check is inside
                float isInside_Length = dot ( v , v ) * by_dot_d12 - 1 ;            // must be < 0
                float isInside_Position = dot_d1_d2 * dot ( dnp12.xy , dnp12.zw ) ; // must be < 0
                bool isInside = max ( isInside_Length , isInside_Position ) < 0 ;
                return isInside ? -dist : dist ;

                // conventional approach
                //float2 np1 = p1 + d1 * t12_clamped.x ;                                 // projected point 1
                //float2 np2 = p1 + d2 * t12_clamped.y ;                                 // projected point 2
                //float2 dnp1 = np1 - p ;                                                // vector p to projected point 1
                //float2 dnp2 = np2 - p ;                                                // vector p to projected point 2
                //float dist2_np1 = dot ( dnp1 , dnp1 ) ;                                // squared pistance p to projected point 1
                //float dist2_np2 = dot ( dnp2 , dnp2 ) ;                                // squared pistance p to projected point 1
                //float dist = sqrt ( min ( dist2_np1 , dist2_np2 ) ) ;                  // get the shortest distance
                //float isInside = ( dot ( dnp1 , dnp2 ) ) ;                             // dot is negative if p is between both lines (for small angles)

                //if ( any ( t12_clamped != t12 ) ) return dist ;                        // p is outside because too far -> return positive dist
                //return isInside < 0 ? -dist : dist ;                                   // return negative dist if p is inside
            }

            // suggestion of Google-AI - failed
            float sd_V1 ( float2 p , float4 d12 , float4 p1_dot12 )
            {
                // d12 = ( d1.x , d1.y , d2.x , d2.y )
                // p1_dot12 = ( p1.x , p1.y , 1 / dot(d1, d1) , dot(d1, d2) )
                float2 d1 = d12.xy ;
                float2 d2 = d12.zw ;
                float2 p1 = p1_dot12.xy ;
                float by_dot_d1 = p1_dot12.z ; // 1.0f / dot(d1, d1)
                float dot_d1_d2 = p1_dot12.w ; // dot(d1, d2) von C# vorberchnet

                // Vektor von der Spitze (p1) zum aktuellen Pixel
                float2 v = p - p1 ;

                // --- FLANKE 1 PROJEKTION ---
                // Nutzt das vorberechnete 1/dot(d1,d1) aus C# für die Projektion!
                float h1 = max ( dot ( v , d1 ) * by_dot_d1 , 0.0 );
                float2 pq1 = v - d1 * h1;
                float distSq1 = dot ( pq1 , pq1 );

                // --- FLANKE 2 PROJEKTION ---
                // Da wir dot(d2, d2) nicht in den Registern haben, aber dot(d1, d1) identisch ist
                // (da der Pfeil symmetrisch ist), können wir by_dot_d1 auch für d2 nutzen!
                float h2 = max ( dot ( v , d2 ) * by_dot_d1 , 0.0 );
                float2 pq2 = v - d2 * h2;
                float distSq2 = dot ( pq2 , pq2 );

                // Kleinste quadrierte Distanz zu den Flanken ermitteln
                float minD2 = min ( distSq1 , distSq2 );

                // --- INNERHALB/AUSSEN SIGNAL (Kreuzprodukt/Determinante) ---
                // Da v der Vektor von der Spitze ist, testen wir, ob das Pixel 
                // mathematisch im Raum zwischen d1 und d2 liegt.
                float s1 = v.x * d1.y - v.y * d1.x;
                float s2 = v.x * d2.y - v.y * d2.x;
                float sign = ( s1 * s2 < 0.0 ) ? -1.0 : 1.0;

                // Nur ein einziges Mal am Ende die Wurzel ziehen
                return sqrt ( minD2 ) * sign;
            }

            // suggestion of Google-AI - failed more
            float sd_V2 ( float2 p , float4 d12 , float4 p1_dot12 )
            {
                float2 d1 = d12.xy ;
                float2 d2 = d12.zw ;
                float2 p1 = p1_dot12.xy ;
                float by_dot_d1 = p1_dot12.z ;
                float dot_d1_d2 = p1_dot12.w ;

                // Vektor von der Spitze (p1) zum Pixel
                float2 v = p - p1 ;

                // --- FLANKE 1 PROJEKTION ---
                float h1 = max ( dot ( v , d1 ) * by_dot_d1 , 0.0 );
                float2 pq1 = v - d1 * h1;
                float distSq1 = dot ( pq1 , pq1 );

                // --- FLANKE 2 PROJEKTION ---
                float h2 = max ( dot ( v , d2 ) * by_dot_d1 , 0.0 );
                float2 pq2 = v - d2 * h2;
                float distSq2 = dot ( pq2 , pq2 );

                // Kleinste ununterbrochene Distanz zu den Flanken
                float minD2 = min ( distSq1 , distSq2 );

                // --- INNEN/AUSSEN WEICHE (Keil) ---
                float s1 = v.x * d1.y - v.y * d1.x;
                float s2 = v.x * d2.y - v.y * d2.x;
                float sign = ( s1 * s2 < 0.0 ) ? -1.0 : 1.0;

                float finalSDF = sqrt ( minD2 ) * sign;

                // --- DER HALBRAUM-SCHILD (NEU!) ---
                // Der Vektor der Basis-Kante ist d2 - d1 (von p0 zu p2)
                float2 baseEdge = d2 - d1;
                // Wir projizieren das Pixel relativ zum linken Basispunkt (p0)
                // p0 selbst ist einfach p1 + d1
                float2 vBase = p - ( p1 + d1 );

                // Das Kreuzprodukt zeigt uns, ob das Pixel hinter der Basis-Linie liegt
                // (also tief im Inneren der Sprechblase)
                float baseSign = vBase.x * baseEdge.y - vBase.y * baseEdge.x;

                // Da wir wissen, auf welcher Seite der Basis die Spitze liegt,
                // schneiden wir den unendlichen Keil hart ab, sobald wir die Basis überschreiten.
                // Wenn wir hinter der Basis sind, wird die Pfeil-SDF fließend neutralisiert.
                if ( baseSign < 0.0 )
                {
                    // Wir setzen die Distanz auf einen großen positiven Wert, 
                    // damit die anschließende opSmoothUnion mit der Box den Keil hier ignoriert!
                    finalSDF = 9999.0;
                }

                return finalSDF;
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

                // calc distance to rectangle

//#if defined ( DRAW_BORDER ) || defined ( ROUND_CORNERS ) || defined ( DROP_SHADOW ) || !defined ( DROP_SHADOW )
                const float2 Pos = getPosPixels ( IN ) ;

                //float ra = min ( _Radius , min ( RectangleHalfSize.x , RectangleHalfSize.y ) ) ;
                float ra = _Radius ;

                float DistBox = sdRoundBox ( Pos + CENTER_OFFSET , RectangleHalfSize , ra ) ;
                //float DistTriangle = sdTriangle ( Pos , TrianglePoint0 , TrianglePoint1 , TrianglePoint2 ) ;
                float DistTriangle = sd_V ( Pos , _Triangle02 , _Triangle1 ) ;
                float Dist = min ( DistBox , DistTriangle ) ; // union
                //Dist = DistTriangle ;
                float CornerAlphaMultiplier = lin_step_inv_width_mla ( OUTLINE_LO_MLA , BY_SMOOTHING , Dist ) ;

                // map corner alpha (e.g. for holes)
#if defined ( MAP_CORNER_ALPHA )
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

                float2 halfSize = max ( RectangleHalfSize - ShadowSigma , 0.0f ) ; // ShadowSoftness
                //ra += ShadowSigma * 0.3f ;
#ifdef ROUND_CORNERS

                ra = min ( _Radius , min ( halfSize.x , halfSize.y ) ) ;
                float sdShadow = sdRoundBox_outside_only ( Pos + CENTER_OFFSET + SHADOW_OFFSET , halfSize , ra ) ;
#else
                float sdShadow = sdBox_outside_only ( Pos + CENTER_OFFSET + SHADOW_OFFSET , halfSize ) ;
                //float sdShadow = sdRoundBox_outside_only ( Pos + CenterOffset + ShadowOffset , halfSize , _Radius ) ;
#endif
                sdShadow = max ( sdShadow , 0.0 ) ;
                float shadowAlpha = ShadowAlpha * ( gaussWeight ( sdShadow , ShadowBySqrSigma ) ) ;

                // make anti-alias look nicely

                //float BySumAlpha = 1.0f / ( CornerAlphaMultiplier + shadowAlpha ) ;
                float BySumAlpha = rcp ( CornerAlphaMultiplier + shadowAlpha ) ;
                float fIn = CornerAlphaMultiplier * BySumAlpha ; // is 1 inside, 0 outside, between in smoothing transition
                float fOut = shadowAlpha * BySumAlpha ;

                float4 colMix = ( color * fIn + shadowAlpha * fOut ) * fIn ;
                colMix.a = shadowAlpha ;
                color = lerp ( colMix , color , CornerAlphaMultiplier );

//#elif defined ( ROUND_CORNERS )
#else
                // no shadow but round corners.
                // There we can use CornerAlphaMultiplier directly
                color.a *= CornerAlphaMultiplier ;
#endif

                // unity 2d clipping

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
