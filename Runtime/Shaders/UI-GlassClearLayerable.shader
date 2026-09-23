Shader "Rudi/UI/GlassClearLayerable" 
{ 
	// written by Rudi Weinacker
	// inspired by:
	// https://github.com/Winchy/Unity3d-Lessons/blob/master/Unity3d%20Lessons/Assets/GrabPass/SimpleGrabPassBlur.shader
	// https://www.rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
    // https://gist.github.com/JohannesMP/7d62f282705169a2855a0aac315ff381
    // https://docs.unity3d.com/550/Documentation/Manual/SL-GrabPass.html
    // https://stackoverflow.com/questions/14942210/opengl-shading-language-transform-tex

    Properties 
	{ 
        _Color    ( "Tint"     , Color ) = ( 1,1,1,1 )
        _Sigma    ( "Sigma"    , Range ( 0 , 32 ) ) = 10
        _Sigma4   ( "Sigma4"   , Vector ) = ( 0 , 0 , 0 , 0 )
        _Step     ( "Step"     , Range ( 1 ,  4 ) ) = 1
        _ColorMul ( "ColorMul" , Color ) = ( 1,1,1,1 )
        _ColorAdd ( "ColorAdd" , Color ) = ( 0,0,0,0 )
        _Clamp    ( "Clamp"    , Range ( 0 , 1 ) ) = 1
        _Compress ( "Compress" , Range ( 0 , 0.2 ) ) = 0.1
        _ShadowDarkening ( "ShadowDarkening" , Range ( 0 , 1 ) ) = 0.1
        _Caustic ( "Caustic" , Vector ) = ( 0 , 0 , 0 , 0 )
        _CausticPosition ( "CausticPosition" , Range ( 0 , 1 ) ) = 0.65
        _ShadowOffsetY ( "ShadowOffsetY" , Range ( 0 , 1 ) ) = 1

        // mask compatibility
        //_StencilComp ( "Stencil Comparison", Float ) = 8
        //_Stencil ( "Stencil ID", Float ) = 0
        //_StencilOp ( "Stencil Operation", Float ) = 0
        //_StencilWriteMask ( "Stencil Write Mask", Float ) = 255
        //_StencilReadMask ( "Stencil Read Mask", Float ) = 255
        //_ColorMask ( "Color Mask", Float ) = 15


        //_HalfScreenParams ( "HalfScreenParams" , Vector ) = ( 0 , 0 , 0 , 0 )
        _BorderWidth ( "BorderWidth" , Vector ) = ( 0 , 0 , 0 , 0 )
        //_BorderColor ( "BorderColor", Color ) = ( 0,0,0,1 )
        //_FillColor ( "FillColor", Color ) = ( 1,1,1,1 )
        _Radius ( "Radius" , Vector ) = ( 0 , 0 , 0 , 0 )
        _Rectangle ( "Rectangle" , Vector ) = ( 1 , 1 , 1 , 1 )
        _Scale ( "Scale" , Vector ) = ( 1 , 1 , 1 , 1 )
        _CenterXAlpha ( "CenterXAlpha" , Vector ) = ( 0 , 0 , 0 , 0 )
        _CenterYAlpha ( "CenterYAlpha" , Vector ) = ( 0 , 0 , 0 , 0 )
        _CenterX ( "CenterX" , Vector ) = ( 0 , 0 , 0 , 0 )
        _CenterY ( "CenterY" , Vector ) = ( 0 , 0 , 0 , 0 )
        //_AlphaMap ( "AlphaMap" , Vector ) = ( 1 , 0 , 1 , 0 )
        //_ColorAlpha0 ( "ColorAlpha0" , Vector ) = ( 1 , 1 , 1 , 0 )
        //_ColorAlpha1 ( "ColorAlpha1" , Vector ) = ( 1 , 1 , 1 , 1 )
        // Refractive power
        _RefractivePower ( "Refractive Power" , Range ( 0 , 1 ) ) = 0.5
        _Thickness ( "Thickness" , Range ( 0 , 1 ) ) = 0
        // reflexion
        //_ReflexionBorderPos ( "Reflexion Border Pos" , Range ( 0 , 1 ) ) = 1
        //_ReflexionWidth     ( "Reflexion Width" , Range ( 0.01 , 500 ) ) = 2.6
        //_LightDir2D         ( "Light Direction 2D" , Vector ) = ( 0.7071 , -0.7071 , 0 , 0 )
        _ReflectionVars  ( "Reflexion Border Pos" , Vector ) = ( 0 , 0 , 0 , 0 )
        _ReflectionColor ( "Reflexion Color"      , Color ) = ( 1 , 1 , 1 , 1 )
        _Rotation ( "Rotation" , Vector ) = ( 1 , 0 , 0 , 1 )

        //[ Toggle ( UNITY_UI_ALPHACLIP ) ] _UseUIAlphaClip ( "Use Alpha Clip", Float ) = 0

    }

	CGINCLUDE
#include "glass_shared.cginc"	
//#include "UnityUI.cginc"

#pragma target 3.0
#pragma multi_compile_local _ USE_LINEAR_SAMPLING
#pragma multi_compile_local _ DRAW_BORDER
#pragma multi_compile_local _ ROTATION
#pragma multi_compile_local _ DROP_SHADOW
#pragma multi_compile_local _ USE_CAUSTIC
#pragma multi_compile_local _ DRAW_REFLECTION
#pragma multi_compile_local _ CALC_REFLECTION_HORIZONTAL
#pragma multi_compile_local BP_ROUND BP_BULGE BP_BEVEL
//#pragma fragmentoption ARB_precision_hint_fastest

    //sampler2D _BackgroundTexture ;
    //float4 _BackgroundTexture_TexelSize ;
    sampler2D _GrabTexture ;
    float4 _GrabTexture_TexelSize;

#define MyGrabTexture          _GrabTexture
#define MyGrabTextureTexelSize _GrabTexture_TexelSize

    ENDCG

    SubShader
    {
        //Tags { "Queue" = "Overlay" }
        Tags
        {
            "Queue" = "Transparent"

            //"Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent" // Transparent Overlay 
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "DisableBatching" = "True"
        }

        //Lighting Off
        //Cull Off
        //ZWrite Off
        //ZTest Always
        //Blend SrcAlpha OneMinusSrcAlpha

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Off  // [ unity_GUIZTestMode ]
        Blend SrcAlpha OneMinusSrcAlpha
        //Blend One OneMinusSrcAlpha
        //ColorMask [ _ColorMask ]

        GrabPass
        {
            //Tags
            //{
            //    "LightMode" = "Always"
            //    "Queue" = "Overlay"
            //}
            //"_BackgroundTexture"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag ( v2f i ) : COLOR
            {
                texel_data td = getBorderData_sdf ( i , MyGrabTextureTexelSize ) ;
                //float alpha = td.alpha ;
                float2 texelpos = td.grabPos.xy ;

#if defined ( DROP_SHADOW ) && defined ( USE_CAUSTIC )
                float4 colOrig = tex2D ( MyGrabTexture , i.grabPos.xy ) ; // i.grabPos.xy td.grabPos.xy
#endif


                float4 col = tex2D ( MyGrabTexture , texelpos ) ;

                col.xyz = col.xyz * _ColorMul.xyz + _ColorAdd.xyz ;
                float b = _Clamp - _Compress ;
                col.xyz = min ( col.xyz , col.xyz * _Compress + b ) ;
                col *= i.color ;

//#if DROP_SHADOW
//                col.xyz *= 1.0f - td.shadow_alpha ;
//#endif
#if defined ( DROP_SHADOW ) && defined ( USE_CAUSTIC )
                col = getImageShadowColorWithCaustic3 ( col , td.alpha , td.shadow_alpha , colOrig ) ;
#elif defined ( DROP_SHADOW )
                col.xyz *= 1.0f - td.shadow_alpha ;
#endif


#if DRAW_REFLECTION && DRAW_BORDER
                //col.xyz += _ReflectionColor.xyz * td.gloss ;
                col.xyz += _ReflectionColor.xyz * td.gloss * td.alpha ;
#endif


#if defined ( DROP_SHADOW ) && !defined ( USE_CAUSTIC )
                col = getImageShadowColor ( col , td.alpha , td.shadow_alpha ) ;
#elif !defined ( DROP_SHADOW )
                col.w = td.alpha ;
#endif


//#if DROP_SHADOW
//                col = getImageShadowColor ( col , td.alpha , td.shadow_alpha ) ;
//#else
//                col.w = td.alpha ;
//#endif

                //col.w = alpha ;
                return col ;
            }
			ENDCG
		}
	}
}
