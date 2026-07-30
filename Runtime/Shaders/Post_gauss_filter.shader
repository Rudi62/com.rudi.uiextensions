Shader "Rudi/post/gauss_filter" 
{ 
	// written by Rudi Weinacker
	// inspired by:
	// https://github.com/Winchy/Unity3d-Lessons/blob/master/Unity3d%20Lessons/Assets/GrabPass/SimpleGrabPassBlur.shader
	// https://www.rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
	Properties 
	{ 
		_MainTex   ( "Base (RGB)", 2D    ) = "white" {}
		_Sigma     ( "Sigma"     , Float ) = 3
	}

	CGINCLUDE
	#include "UnityCG.cginc"	

	uniform sampler2D _MainTex ;
	uniform float4 _MainTex_TexelSize ;
	uniform float _Sigma ;
	uniform float _Precision ;

	static const float minWeight = 0.004 ;

	float gaussWeight ( float x )
	{
		//   exp ( x² / ( 2 * sigma² ) )
		// = exp₂ ( x² * ( ( -log₂ ( e ) / 2 ) / ( sigma² ) ) )
		//                                                         ( -log₂ ( e ) / 2 ) = -0.72134752
		return exp2 ( x * x * ( -0.72134752f / ( _Sigma * _Sigma ) ) ) ;
	}

	float4 gaussDiscrete ( v2f_img i , float2 dir )
	{
		float4 o = tex2D ( _MainTex , i.uv ) ;
		if ( _Sigma < 0.01 ) return o ;
		float sum = 0.5 ;
		float2 lo = i.uv ;
		float2 hi = i.uv ;
		float pos = 0 ;
		float weight = 1 ;
		while ( weight > minWeight )
		{
			weight = gaussWeight ( ++pos ) ;
			sum += weight ;
			lo -= dir ;
			hi += dir ;
			o += ( tex2D ( _MainTex , lo ) + tex2D ( _MainTex , hi ) ) * weight ;
		}
		o *= ( 0.5 / sum ) ;
		return o ;
	}			
	
	float4 gaussLinear ( v2f_img i , float2 dir )
	{
		float4 o = tex2D ( _MainTex , i.uv ) ;
		if ( _Sigma < 0.01 ) return o ;
		float weightSum = 0.5 ;
		float2 offset ;
		float weight1 = 1 ;
		float weight ;
		float pos = 0 ;
		while ( weight1 > minWeight )
		{
			weight  = gaussWeight ( ++pos ) ;
			weight1 = gaussWeight ( ++pos ) ;
			weight += weight1 ;
			offset = dir * ( ( pos - 1 ) + weight1 / weight ) ;
			o += ( tex2D ( _MainTex , i.uv + offset ) + tex2D ( _MainTex , i.uv - offset ) ) * weight ;
			weightSum += weight ;
		}
		o *= ( 0.5 / weightSum ) ;
		return o ;
	}

	float4 frag_horizontal_discrete ( v2f_img i ) : COLOR
	{
		return gaussDiscrete ( i , float2 ( _MainTex_TexelSize.x , 0 ) ) ;
	}
	
	float4 frag_vertical_discrete ( v2f_img i ) : COLOR
	{		
		return gaussDiscrete ( i , float2 ( 0 , _MainTex_TexelSize.y ) ) ;
	}

	float4 frag_horizontal_linear ( v2f_img i ) : COLOR
	{
		return gaussLinear ( i , float2 ( _MainTex_TexelSize.x , 0 ) ) ;
	}

	float4 frag_vertical_linear ( v2f_img i ) : COLOR
	{
		return gaussLinear ( i , float2 ( 0 , _MainTex_TexelSize.y ) ) ;
	}
	ENDCG
	
	SubShader 
	{
		Tags { "Queue" = "Overlay" }
		Lighting Off 
		Cull Off 
		ZWrite Off 
		ZTest Always 

	    Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment frag_horizontal_discrete
			ENDCG
		}
		
		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment frag_vertical_discrete
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment frag_horizontal_linear
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert_img
			#pragma fragment frag_vertical_linear
			ENDCG
		}
	}
}
