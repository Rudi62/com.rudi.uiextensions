Shader "Rudi/UI/DottedLine"
{
    Properties
    {
        [PerRendererData] _MainTex ( "Sprite Texture" , 2D ) = "white" {}
        _Color ( "Tint" , Color ) = ( 1 , 1 , 1 , 1 )

        // Mask compatibility
        _StencilComp      ( "Stencil Comparison" , Float ) = 8
        _Stencil          ( "Stencil ID"         , Float ) = 0
        _StencilOp        ( "Stencil Operation"  , Float ) = 0
        _StencilWriteMask ( "Stencil Write Mask" , Float ) = 255
        _StencilReadMask  ( "Stencil Read Mask"  , Float ) = 255
        _ColorMask        ( "Color Mask"         , Float ) = 15

        _BgColor       ( "Background Color" , Color  ) = ( 0 , 0 , 0 , 0 )
        _UVToPixels    ( "UVToPixels"       , Vector ) = ( 100 , 0 , 0 , 10 )
        _StripParams   ( "StripParams"      , Vector ) = ( 50 , 5 , 0 , -5 )
        _PatternParams ( "PatternParams"    , Vector ) = ( 20 , 0.05 , 5 , 5 )
        _FadeParams    ( "FadeParams"       , Vector ) = ( -1 , 100000 , 1 , 1 )

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ( "Use Alpha Clip" , Float ) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
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
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION ;
                float4 color    : COLOR ;
                float2 texcoord : TEXCOORD0 ;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            } ;

            struct v2f
            {
                float4 vertex        : SV_POSITION ;
                fixed4 color         : COLOR ;
                float2 posPixels     : TEXCOORD0 ;
                float4 worldPosition : TEXCOORD1 ;
                UNITY_VERTEX_OUTPUT_STEREO
            } ;

            fixed4 _Color ;
            float4 _ClipRect ;
            float4 _BgColor ;

            // 2x2-Matrix (m_xx, m_xy, m_yx, m_yy) bildet UV auf (longPx, shortPx) inkl. Richtung ab
            float4 _UVToPixels ;

            // x = activeHalfLenPx, y = halfThicknessPx, z = offsetX, w = offsetY
            float4 _StripParams ;

            // x = periodPx, y = invPeriodPx, z = halfDashPx, w = cornerRadPx
            float4 _PatternParams ;

            // x = -1 / smoothingPx, y = maxCell, z = fgSubPixelFade, w = stripSubPixelFade
            float4 _FadeParams ;

            v2f vert ( appdata_t v )
            {
                v2f OUT ;
                UNITY_SETUP_INSTANCE_ID ( v ) ;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO ( OUT ) ;

                OUT.worldPosition = v.vertex ;
                OUT.vertex        = UnityObjectToClipPos ( OUT.worldPosition ) ;

                // posPixels.x = 0 am gewählten Startpunkt (UV 0,0 oder UV 1,1), posPixels.y = 0 auf der Mittelachse
                OUT.posPixels = float2 (
                    dot ( v.texcoord , _UVToPixels.xy ) ,
                    dot ( v.texcoord , _UVToPixels.zw )
                ) + _StripParams.zw ;

                OUT.color = v.color * _Color ;
                return OUT ;
            }

            fixed4 frag ( v2f IN ) : SV_Target
            {
                float2 pos = IN.posPixels ;

                // 1. SDF des gesamten Linienstreifens: x in [0, activeLen], y in [-halfThick, +halfThick]
                float2 pStrip    = float2 ( pos.x - _StripParams.x , pos.y ) ;
                float2 qStrip    = abs ( pStrip ) - _StripParams.xy ;
                float  distStrip = min ( max ( qStrip.x , qStrip.y ) , 0.0 ) + length ( max ( qStrip , 0.0 ) ) ;

                // 2. Zellwiederholung ab pos.x = 0 (Element k startet bei k * period, Zentrum bei k * period + halfDash)
                float cell   = clamp ( floor ( ( pos.x - _PatternParams.z ) * _PatternParams.y + 0.5 ) , 0.0 , _FadeParams.y ) ;
                float localX = pos.x - ( cell * _PatternParams.x + _PatternParams.z ) ;

                // 3. SDF für Punkt (Kreis), Dash (Rechteck) oder Rund-Dash (Kapsel), geclippt auf den Streifen
                float  rShape    = _PatternParams.w ;
                float2 bShape    = float2 ( _PatternParams.z , _StripParams.y ) - rShape ;
                float2 qShape    = abs ( float2 ( localX , pos.y ) ) - bShape ;
                float  distShape = min ( max ( qShape.x , qShape.y ) , 0.0 ) + length ( max ( qShape , 0.0 ) ) - rShape ;
                distShape        = max ( distShape , distStrip ) ;

                // 4. Antialiasing-Deckung (Vordergrund ist Teilmenge des Streifens -> saumfrei)
                float negInvSmooth = _FadeParams.x ;
                float covFg        = saturate ( distShape * negInvSmooth + 0.5 ) * _FadeParams.z ;
                float covStrip     = max ( saturate ( distStrip * negInvSmooth + 0.5 ) * _FadeParams.w , covFg ) ;

                // 5. Alpha-gewichtetes Over-Blending: RawImage.color (IN.color) über _BgColor
                float wFg  = covFg * IN.color.a ;
                float wBg  = ( covStrip - wFg ) * _BgColor.a ;
                float outA = wFg + wBg ;

                half4 color ;
                color.rgb = ( IN.color.rgb * wFg + _BgColor.rgb * wBg ) * ( 1.0 / max ( outA , 1e-5 ) ) ;
                color.a   = outA ;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping ( IN.worldPosition.xy , _ClipRect ) ;
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
