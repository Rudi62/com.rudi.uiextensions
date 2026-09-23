#include "UnityCG.cginc"	
//#include "UnityUI.cginc"

float4 _Color ;
uniform float _Sigma ;
uniform float4 _Sigma4 ;
uniform float _Step ;
uniform float4 _ColorMul ;
uniform float4 _ColorAdd ;
uniform float _Clamp ;
uniform float _Compress ;
uniform float _ShadowDarkening ;
uniform float _ShadowOffsetY ;
//uniform float _Precision ;
//sampler2D _GrabTexture ;
//float4 _GrabTexture_TexelSize;

//uniform float4 _HalfScreenParams ; // for pixel correct
float4 _BorderWidth ;
//fixed4 _BorderColor ;
//fixed4 _FillColor ;
uniform float4 _Radius ;
uniform float4 _Rectangle ;
uniform float4 _Scale ;
uniform float4 _CenterX ;
uniform float4 _CenterY ;
uniform float4 _CenterXAlpha ;
uniform float4 _CenterYAlpha ;
//float4 _AlphaMap ;
//fixed4 _ColorAlpha0 ;
//fixed4 _ColorAlpha1 ;
uniform float _RefractivePower ;
uniform float _CausticPower ;
uniform float _Thickness ;
//uniform float _ReflexionBorderPos ;
//uniform float _ReflexionWidth ;
uniform float4 _ReflectionVars ;
uniform float4 _ReflectionColor ;
uniform float4 _Rotation ;
//uniform float2x2 _Rotation ;

#define REFLEXION_BORDER_POS _ReflectionVars.z
#define REFLEXION_WIDTH _ReflectionVars.w
#define LIGHT_DIR_2D _ReflectionVars.xy

static const float minWeight = 0.1 ; // 0.004 = 1/250 0.1
static const float4 Constants = { 0 , 0.5 , 1 , -1.0 } ;
static const float4 Constants2 = { 999999 , -999999 , 0 , 0 } ;
// abs ( Pos.x , wy - Pos.y , wx - Pos.x , Pos.y ) in pixels
    
//static const float4 EdgeNormalsX = { -1 , 0 , 1 ,  0 } ;
//static const float4 EdgeNormalsY = {  0 , 1 , 0 , -1 } ;
static const float4 EdgeNormalsX = { 1 , 0 , -1 ,  0 } ;
static const float4 EdgeNormalsY = {  0 , -1 , 0 , 1 } ;
static const float4 CornerNormalsX = { 1 , 1 , -1 ,  -1 } ;
static const float4 CornerNormalsY = {  1 , -1 , -1 , 1 } ;

uniform float4 _Caustic ;
uniform float _CausticPosition ;

//uniform float2 StepLinear = float2 ( _Step , _Step + 1.0f ) ;

//static const float2 LightDir2D = { 0.7071 , -0.7071 } ;

#define ZERO Constants.x
#define ZERO4 Constants.xxxx
#define HALF Constants.y
#define HALF2 Constants.yy
#define HALF4 Constants.yyyy
#define ONE Constants.z
#define ONE4 Constants.zzzz
#define MINUS_ONE Constants.w
#define BIG_NUMBER Constants2.x
#define BIG_NUMBER4 Constants2.xxxx
#define LOW_NUMBER Constants2.y
#define LOW_NUMBER4 Constants2.yyyy

#define BORDER_WIDTH          _BorderWidth.x
#define BORDER_WIDTH2         _BorderWidth.xx
#define SMOOTHED_BORDER_WIDTH _BorderWidth.w  // BorderWidth / Smoothing
#define BORDER_FADE           _BorderWidth.y  // if borderFade: 1 / BorderWidth  else  1
#define SMOOTHED_BORDER_FADE  _BorderWidth.z  // if borderFade: 1 / Smoothing  else  fBorderWidth / Smoothing

#define BY_SMOOTHING    _Rectangle.z
#define SMOOTHING       _Rectangle.w
#define HALF_SMOOTHING (_Rectangle.w*0.5f)

#define CENTER_OFFSET       _CenterX.xy
#define RECTANGLE_HALF_SIZE _CenterX.zw

#define GLASS_SIGMA      _Sigma4.x
#define GLASS_BY_SIGMA2  _Sigma4.y
#define SHADOW_SIGMA     _Sigma4.z
#define SHADOW_BY_SIGMA2 _Sigma4.w

#define CausticPower     _Caustic.x
#define CausticSharpness _Caustic.y
#define CausticAsym      _Caustic.zw

//#pragma multi_compile_local _ USE_LINEAR_SAMPLING
//#pragma multi_compile_local _ DRAW_BORDER
//#pragma multi_compile_local _ ROTATION
//#pragma multi_compile_local _ DROP_SHADOW
//#pragma multi_compile_local _ USE_CAUSTIC
//#pragma multi_compile_local _ DRAW_REFLECTION
//#pragma multi_compile_local _ CALC_REFLECTION_HORIZONTAL
//#pragma multi_compile_local BP_ROUND BP_BULGE BP_BEVEL


struct appdata_t
{
    float4 vertex   : POSITION;
    float4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
};

struct v2f
{
    float4 pos       : SV_POSITION ;
    float2 texcoord  : TEXCOORD0 ;
    float4 grabPos   : TEXCOORD1 ;
    float4 color     : COLOR;
};

struct texel_data
{
    float alpha ;
    float shadow_alpha ;
    //float caustic ;
#if DRAW_REFLECTION
    float gloss ;
#endif
    float4 grabPos : TEXCOORD1 ;
};

// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// vertexshader

v2f vert ( appdata_t v )
{
    // from https://docs.unity3d.com/550/Documentation/Manual/SL-GrabPass.html
    v2f o;
    // use UnityObjectToClipPos from UnityCG.cginc to calculate 
    // the clip-space of the vertex
    o.pos = UnityObjectToClipPos ( v.vertex );
    // use ComputeGrabScreenPos function from UnityCG.cginc
    // to get the correct texture coordinate
    o.grabPos = ComputeGrabScreenPos ( o.pos );
    o.texcoord = v.texcoord ; // no tiling
    o.color = v.color * _Color;

    return o;
}

// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// pixelshader

// blur functions
//float getCaustic ( float delta , float sharpness )
//{
//    float causticIntensity = exp ( -( delta * delta ) * sharpness );
//    return causticIntensity ;
//}

float gaussWeight ( float x )
{
	//   exp ( x² / ( 2 * sigma² ) )
	// = exp₂ ( x² * ( ( -log₂ ( e ) / 2 ) / ( sigma² ) ) )
	//                                                         ( -log₂ ( e ) / 2 ) = -0.72134752
	return exp2 ( x * x * ( -0.72134752f / ( _Sigma * _Sigma ) ) ) ;
}

float gaussWeight ( float x , float by_sigma2 )
{
    //   exp ( x² / ( 2 * sigma² ) )
    // = exp₂ ( x² * ( ( -log₂ ( e ) / 2 ) / ( sigma² ) ) )
    //                                                         ( -log₂ ( e ) / 2 ) = -0.72134752
    //return exp2 ( x * x * ( -0.72134752f * by_sigma2 ) ) ;
    return exp2 ( x * x * by_sigma2 ) ;
}

float2 gaussWeight ( float2 x , float by_sigma2 )
{
    //   exp ( x² / ( 2 * sigma² ) )
    // = exp₂ ( x² * ( ( -log₂ ( e ) / 2 ) / ( sigma² ) ) )
    //                                                         ( -log₂ ( e ) / 2 ) = -0.72134752
    //return exp2 ( x * x * ( -0.72134752f * by_sigma2 ) ) ;
    return exp2 ( x * x * by_sigma2 ) ;
}

float4 gaussWeight ( float4 x , float by_sigma2 )
{
    //   exp ( x² / ( 2 * sigma² ) )
    // = exp₂ ( x² * ( ( -log₂ ( e ) / 2 ) / ( sigma² ) ) )
    //                                                         ( -log₂ ( e ) / 2 ) = -0.72134752
    //return exp2 ( x * x * ( -0.72134752f * by_sigma2 ) ) ;
    return exp2 ( x * x * by_sigma2 ) ;
}


float4 gaussDiscrete ( float2 texelpos , float2 dir , sampler2D grab_texture )
{
	float4 o = tex2D ( grab_texture , texelpos ) ;
	//if ( _Sigma < 0.01 ) return o ;
	float sum = 0.5 ;
	float2 lo = texelpos ;
	float2 hi = texelpos ;
	float pos = 0 ;
	float weight = 1 ;
	while ( weight > minWeight )
	{
		weight = gaussWeight ( pos += _Step ) ;
		sum += weight ;
		lo -= dir * _Step ;
		hi += dir * _Step ;
		o += ( tex2D ( grab_texture , lo ) + tex2D ( grab_texture , hi ) ) * weight ;
	}
	o *= ( 0.5 / sum ) ;
	return o ;
}			
float4 gaussDiscrete ( float2 texelpos , float2 dir , sampler2D grab_texture , float by_sigma2 )
{
	float4 o = tex2D ( grab_texture , texelpos ) ;
	//if ( _Sigma < 0.01 ) return o ;
	float sum = 0.5 ;
	float2 lo = texelpos ;
	float2 hi = texelpos ;
	float pos = 0 ;
	float weight = 1 ;
	while ( weight > minWeight )
	{
		weight = gaussWeight ( pos += _Step , by_sigma2 ) ;
		sum += weight ;
		lo -= dir * _Step ;
		hi += dir * _Step ;
		o += ( tex2D ( grab_texture , lo ) + tex2D ( grab_texture , hi ) ) * weight ;
	}
	o *= ( 0.5 / sum ) ;
	return o ;
}			
	
float4 gaussLinear ( float2 texelpos , float2 dir , sampler2D grab_texture )
{
	float4 o = tex2D ( grab_texture , texelpos ) ;
	//if ( _Sigma < 0.01 ) return o ;
	float weightSum = 0.5 ;
	float2 offset ;
	float weight1 = 1 ;
	float weight ;
	float pos = 0 ;
	while ( weight1 > minWeight )
	{
		weight  = gaussWeight ( pos += _Step ) ;
        weight1 = gaussWeight ( pos += 1 ) ;
		weight += weight1 ;
		offset = dir * ( ( pos - 1 ) + weight1 / weight ) ;
		o += ( tex2D ( grab_texture , texelpos + offset ) + tex2D ( grab_texture , texelpos - offset ) ) * weight ;
		weightSum += weight ;
	}
	o *= ( 0.5 / weightSum ) ;
	return o ;
}

float4 gaussLinear_o ( float2 texelpos , float2 dir , sampler2D grab_texture , float by_sigma2 )
{
    float4 o = tex2D ( grab_texture , texelpos ) ;
    //if ( _Sigma < 0.01 ) return o ;
    float weightSum = 0.5 ;
    float2 offset ;
    float weight1 = 1 ;
    float weight ;
    float pos = 0 ;
    while ( weight1 > minWeight )
    {
        weight = gaussWeight ( pos += _Step , by_sigma2 ) ;
        weight1 = gaussWeight ( pos += 1 , by_sigma2 ) ;
        weight += weight1 ;
        offset = dir * ( ( pos - 1 ) + weight1 / weight ) ;
        o += ( tex2D ( grab_texture , texelpos + offset ) + tex2D ( grab_texture , texelpos - offset ) ) * weight ;
        weightSum += weight ;
    }
    o *= ( 0.5 / weightSum ) ;
    return o ;
}

float4 gaussLinear_2 ( float2 texelpos , float2 dir , sampler2D grab_texture , float by_sigma2 )
{
    float4 o = tex2D ( grab_texture , texelpos ) ;
    float TotalSumWeight = 0.5 ;
    float2 offset ;
    float2 weight = 1 ;
    float2 pos = 0 ;
    float2 Step = float2 ( _Step , _Step + 1.0f ) ;
    while ( weight.y > minWeight )
    {
        pos = pos.yy + Step ;
        weight = gaussWeight ( pos , by_sigma2 ) ;
        float SumWeight = weight.x + weight.y ;
        offset = dir * ( pos.x + weight.y / SumWeight ) ;
        o += ( tex2D ( grab_texture , texelpos + offset ) + tex2D ( grab_texture , texelpos - offset ) ) * SumWeight ;
        TotalSumWeight += SumWeight ;
    }
    o *= ( 0.5 / TotalSumWeight ) ;
    return o ;
}

float4 gaussLinear ( float2 texelpos , float2 dir , sampler2D grab_texture , float by_sigma2 )
{
    float4 o = tex2D ( grab_texture , texelpos ) ;
    float TotalSumWeight = 0.5 ;
    //float2 offset ;
    float4 weight = 1 ;
    float4 pos = 0 ;
    float4 Step = float4 ( _Step , _Step + 1.0f , _Step + _Step + 1.0f , _Step + _Step + 2.0f ) ;
    while ( weight.w > minWeight )
    {
        pos = pos.wwww + Step ;
        weight = gaussWeight ( pos , by_sigma2 ) ;
        float2 SumsWeight = weight.xz + weight.yw ;
        float4 offset = dir.xyxy * ( pos.xxzz + weight.yyww / SumsWeight.xxyy ) ;
        o += ( tex2D ( grab_texture , texelpos + offset.xy ) + tex2D ( grab_texture , texelpos - offset.xy ) ) * SumsWeight.x ;
        o += ( tex2D ( grab_texture , texelpos + offset.zw ) + tex2D ( grab_texture , texelpos - offset.zw ) ) * SumsWeight.y ;
        TotalSumWeight += SumsWeight.x + SumsWeight.y ;
    }
    o *= ( 0.5 / TotalSumWeight ) ;
    return o ;
}

float4 getImageShadowColor ( float4 color , float alphaImage , float alphaShadow )
{
    float BySumAlpha = rcp ( alphaImage + alphaShadow ) ;
    float fIn = alphaImage * BySumAlpha ; // is 1 inside, 0 outside, between in smoothing transition
    float fOut = alphaShadow * BySumAlpha ;

    float4 colMix = ( color * fIn + alphaShadow * fOut ) * fIn ;
    colMix.a = alphaShadow ;
    return lerp ( colMix , color , alphaImage ) ;
}

float4 getImageShadowColor ( float4 baseColor , float alphaImage , float alphaShadow , float causticIntensity )
{
    float BySumAlpha = rcp ( alphaImage + alphaShadow ) ;
    float fIn = alphaImage * BySumAlpha ; // is 1 inside, 0 outside, between in smoothing transition
    float fOut = alphaShadow * BySumAlpha ;

    float4 colMix = ( baseColor * fIn + alphaShadow * fOut ) * fIn ;
    colMix.a = alphaShadow ;
    return lerp ( colMix , baseColor , alphaImage ) ;

    // 1. Ganz normaler uGUI-Kanten-Schnitt (alphaImage)
    float4 col = baseColor ;
    col.a *= alphaImage ;

    // 2. Regulärer Schatten (Verdunklung)
    // Wir dunkeln die RGB-Kanäle basierend auf dem positiven alphaShadow ab
    col.rgb *= ( 1.0f - alphaShadow );

    // 3. KAUSTIK-TRICK (Aufhellung ohne Übersteuern)
    // Wenn causticIntensity > 0 ist, hellen wir das Bild auf.
    // Formel: col = col + (1.0 - col) * causticIntensity * Stärke
    // Das bewegt die Pixelfarbe elegant in Richtung Weiß, überschreitet aber niemals 1.0!
    float causticStrength = _ShadowDarkening * 0.5f; // Justierfaktor für die Helligkeit
    col.rgb += ( 1.0f - col.rgb ) * ( causticIntensity * causticStrength );

    return col;
}
float4 getImageShadowColor1 ( float4 baseColor , float alphaImage , float alphaShadow , float causticIntensity )
{
    // 1. Ganz normaler uGUI-Kanten-Schnitt (alphaImage)
    float4 col = baseColor ;
    col.a *= alphaImage ;

    // 2. Regulärer Schatten (Verdunklung)
    // Wir dunkeln die RGB-Kanäle basierend auf dem positiven alphaShadow ab
    col.rgb *= ( 1.0f - alphaShadow );

    // 3. KAUSTIK-TRICK (Aufhellung ohne Übersteuern)
    // Wenn causticIntensity > 0 ist, hellen wir das Bild auf.
    // Formel: col = col + (1.0 - col) * causticIntensity * Stärke
    // Das bewegt die Pixelfarbe elegant in Richtung Weiß, überschreitet aber niemals 1.0!
    float causticStrength = _ShadowDarkening * 0.5f; // Justierfaktor für die Helligkeit
    col.rgb += ( 1.0f - col.rgb ) * ( causticIntensity * causticStrength );

    return col;
}

float4 getImageShadowColorWithCaustic ( float4 color , float alphaImage , float alphaShadow , float causticIntensity , float4 colOrig )
{
    // 1. Das Innere des Glases (Deine originale uGUI-Logik)
    // 'color.rgb' enthält durch die Änderung im Hauptshader bereits Schatten UND Kaustik für innen!
    float totalAlpha = alphaImage + alphaShadow;
    if ( totalAlpha <= 0.0001f )
    {
        return float4 ( colOrig.rgb , 0.0f ) ;
    }
    // 1. Das Innere des Glases (Bleibt exakt deine originale uGUI-Logik)
    float BySumAlpha = rcp ( alphaImage + alphaShadow ) ;
    float fIn = alphaImage * BySumAlpha ;
    float fOut = alphaShadow * BySumAlpha ;

    float3 colMixRGB = ( color.rgb * fIn + alphaShadow * fOut ) * fIn ;
    float3 standardUGUIRGB = lerp ( colMixRGB , color.rgb , alphaImage ) ;

    // 2. DAS ÄUSSERE (Schatten UND Kaustik auf dem unberührten Hintergrund anwenden)
    // Wir nehmen das scharfe Originalpixel
    float3 outerBg = colOrig.rgb;

    // A) Erst dunkeln wir es für den Schatten ab
    outerBg *= ( 1.0f - alphaShadow );

    // B) Dann hellen wir es für die Kaustik auf (Kontrast-Trick gegen Weiß)
    // Dadurch wird die Kaustik auch auf dem Schatten perfekt sichtbar!
    outerBg += ( 1.0f - outerBg ) * causticIntensity;

    // 3. DIE WEICHE ÜBER DIE KANTENMASKE
    // Wenn alphaImage = 1 (innen) -> standardUGUIRGB (Dein Glas)
    // Wenn alphaImage = 0 (außen) -> outerBg (Hintergrund + Schatten + Kaustik)
    float3 finalRGB = lerp ( outerBg , standardUGUIRGB , alphaImage ) ;

    // Wir geben durchgehend Alpha = 1 aus, damit uGUI nichts mehr wegschneidet
    return float4 ( finalRGB , 1.0f ) ;
}

float4 getImageShadowColorWithCaustic1 ( float4 color , float alphaImage , float alphaShadow , float causticIntensity , float4 colOrig )
{
    // 1. Sicheres Beenden bei absolut leeren Pixeln (Dein bewährter Sum-Test)
    float totalAlpha = alphaImage + alphaShadow;
    if ( totalAlpha <= 0.0001f )
    {
        return float4( colOrig.rgb , 0.0f );
    }

    // 2. Das Innere des Glases (Originale uGUI-Logik)
    float BySumAlpha = rcp ( totalAlpha ) ;
    float fIn = alphaImage * BySumAlpha ;
    float fOut = alphaShadow * BySumAlpha ;

    float3 colMixRGB = ( color.rgb * fIn + alphaShadow * fOut ) * fIn ;
    float3 standardUGUIRGB = lerp ( colMixRGB , color.rgb , alphaImage ) ;

    // 3. DAS ÄUSSERE (Die pure physikalische Lichtbündelung)
    float3 outerBg = colOrig.rgb;

    // A) Erst der Schatten: Abdunkeln (Multiplikation mit Werten < 1.0)
    outerBg *= ( 1.0f - alphaShadow );

    // B) Jetzt der "gute Stoff": Kaustik (Multiplikation mit Werten > 1.0)
    // Wir definieren einen Faktor, der bei 1.0 startet und mit der Intensität wächst.
    // Ein Multiplikator von z.B. 2.0 verdoppelt die vorhandene Lichtmenge des Pixels!
    float causticBoost = 2.0f; // Hier kannst du die Wucht der Aufhellung kalibrieren
    float3 causticMultiplier = 1.0f + ( causticIntensity * causticBoost );

    outerBg *= causticMultiplier;

    // 4. WEICHE ÜBER DIE KANTENMASKE DES GLASES
    float3 finalRGB = lerp ( outerBg , standardUGUIRGB , alphaImage ) ;

    // 5. DYNAMISCHES ALPHA
    float finalAlpha = saturate ( alphaImage + alphaShadow );

    return float4 ( finalRGB , finalAlpha ) ;
}


// pixelshader helper functions

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

//float smoothstep1 ( float border , float width , float value )
//{
//    return smoothstep ( border - width , border + width , value ) ;
//}

//#define sqr(x) ((x)*(x))
float sqr ( float v ) { return v * v ; }

#define linstep(lo,hi,value) saturate(((value)-(lo))/((hi)-(lo)))
#define linstep_halfwidth(border,halfwidth,value) (linstep(((border)-(halfwidth)),((border)+(halfwidth)),(value)))
#define linstep_inv_width(lo,invwidth,value) saturate(((value)-(lo))*(invwidth))
#define linstep_inv_width_mla(lo_invwidth,invwidth,value) saturate((value)*(invwidth)-(lo_invwidth))

#define smoothstep_halfwidth(border,halfwidth,value) (smoothstep((border)-(halfwidth),(border)+(halfwidth),(value)))
#define smoothstep_clamped(x) ((3.0f-2.0f*(x))*(x)*(x))
#define smoothstep_inv_width(lo,invwidth,value) (smoothstep_clamped(linstep_inv_width((lo),(invwidth),(value))))
#define smoothstep_inv_width_mla(lo_invwidth,invwidth,value) (smoothstep_clamped(linstep_inv_width_mla((lo_invwidth),(invwidth),(value))))



//float lin_step_inv_width ( float lo , float invwidth , float value )
//{
//    return saturate ( ( value - lo ) * invwidth ) ;
//}
//
//float lin_step_inv_width_mla ( float lo_invwidth , float invwidth , float value )
//{
//    return saturate ( value * invwidth - lo_invwidth ) ;
//}



float getSum ( float4 v )
{
    return dot ( v , ONE4 ) ;
    //float2 sum1 = v.xy + v.zw ;
    //return sum1.x + sum1.y ;
}

float2 getPosPixels ( v2f IN )
{
    return IN.texcoord.xy * _Scale.zw - _Scale.xy ;
}

float4 getEdgeDist4 ( float2 pos )
{
    // Vector4 rectangle = new Vector4 ( SizePixel.x , SizePixel.y , 1f / Smoothing , 0f ) ;
    return abs ( _Rectangle.wyxw - pos.xyxy ) ; // ( -pos.x , wy - pos.y , wx - pos.x , -pos.y ) -> ( pos.x , wy - pos.y , wx - pos.x , pos.y )
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
    r.x = ( p.y > 0.0 ) ? r.x : r.y  ;
    float2 q = abs ( p ) - b + r.x ;
    return min ( max ( q.x , q.y ) , 0.0 ) + length ( max ( q , 0.0 ) ) - r.x ;
}

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
    float2 s = sign ( p );//vec2(p.x<0.0?-1:1,p.y<0.0?-1:1);

    float g = max ( w.x , w.y );
    float2  q = max ( w , 0.0 );
    float l = length ( q );

    return float3 ( ( g > 0.0 ) ? l - r : g - r ,
                  s * ( ( g > 0.0 ) ? q / l : ( ( w.x > w.y ) ? float2 ( 1 , 0 ) : float2 ( 0 , 1 ) ) ) );
}

float4 isInCorner4 ( float4 edgeDist4 )
{
    //return step ( max ( edgeDist.xxzz , edgeDist.wyyw ) , _Radius ) ;
    return saturate ( ( _Radius - max ( edgeDist4.xxzz , edgeDist4.wyyw ) ) * BIG_NUMBER4 ) ;
}

float4 isInEdge4 ( float4 edgeDist4 , float mindist )
{
    //float Min = getMinVal ( edgeDist4 ) ;
    //float4 r = cmp ( edgeDist4 - Min , float4 ( 0 , 0 , 0 , 0 ) , float4 ( 1 , 2 , 3 , 4 ) ) ;
    return edgeDist4 == mindist ? ONE4 : ZERO4 ;
}

float4 getCircleCenterDiffX4 ( float2 pos , float4 centerx ) { return centerx - pos.xxxx ; }
float4 getCircleCenterDiffY4 ( float2 pos , float4 centery ) { return centery - pos.yyyy ; }
float4 getCircleCenterDist4 ( float4 diffx , float4 diffy ) { return sqrt ( diffx * diffx + diffy * diffy ) ; }

float4 getCircleCenterDist4 ( float2 pos , float4 centerx , float4 centery )
{
    float4 diffx = ( centerx - pos.xxxx ) ;
    float4 diffy = ( centery - pos.yyyy ) ;
    float4 dist = sqrt ( diffx * diffx + diffy * diffy ) ;
    return dist ;
}

float getCornerAlpha ( v2f IN )
{
    float2 Pos = getPosPixels ( IN ) ;
    float4 EdgeDist4 = getEdgeDist4 ( Pos ) ;
    float4 IsInCorner4 = isInCorner4 ( EdgeDist4 ) ;
    float4 DiffX4 = getCircleCenterDiffX4 ( Pos , _CenterXAlpha ) ; // _CenterX - pos.xxxx
    float4 DiffY4 = getCircleCenterDiffY4 ( Pos , _CenterYAlpha ) ; // _CenterY - pos.yyyy


#if BP_BEVEL
    float4 CenterDist4 = abs ( DiffX4 ) + abs ( DiffY4 ) - HALF4 ; // ONE4 HALF4

#else
    float4 CenterDist4 = getCircleCenterDist4 ( DiffX4 , DiffY4 ) ;
#endif


    //float4 CenterDist4 = getCircleCenterDist4 ( Pos ) ;
    float4 CircleDist4 = _Radius - CenterDist4 ;
    float4 SmoothedCircleDist4 = CircleDist4 * BY_SMOOTHING + ONE4 ;

    //float CornerAlphaMultiplier = saturate ( getMinVal ( IsInCorner4 > HALF4 ? SmoothedCircleDist4 : ONE4 ) ) ;
    float CornerAlphaMultiplier = any ( IsInCorner4 ) ? saturate ( getMaxVal ( SmoothedCircleDist4 * IsInCorner4 ) ) : ONE ;
    return CornerAlphaMultiplier ;
}

float getCornerAlpha_sdf ( v2f IN )
{
    float2 Pos = getPosPixels ( IN ) ;
    //float4 ra = min ( _Radius , min ( RectangleHalfSize.x , RectangleHalfSize.y ) ) ;
    float DistBox = sdRoundBox ( Pos + CENTER_OFFSET , RECTANGLE_HALF_SIZE , _Radius ) ;
    float CornerAlphaMultiplier = smoothstep_halfwidth ( 0.0 , HALF_SMOOTHING , -DistBox ) ;
    return CornerAlphaMultiplier ;
}

//static const float3 SphereCenter = { 0 , 0 , 1 } ;

float getSphericalDisplacement ( float d , float h , float n )
{
    // https://gitlab.mi.hdm-stuttgart.de/aa065/gameprojekt_sem6/-/blob/master/Handsome_Hell/Library/PackageCache/com.unity.render-pipelines.core@6.9.2/ShaderLibrary/Refraction.hlsl

    float s = sqrt ( 1 - d * d ) ;
    float3 Normal1 = float3 ( d , 0 , s ) ;
    float3 Dir0 = float3 ( 0 , 0 , -1 ) ;
    float3 SphereCenter = float3 ( 0 , 0 , 1 ) ;
    float3 Pos1 = Normal1 ;
    Pos1.z += 1 ;
    // refraction
    float3 Dir1 = refract ( Dir0 , Normal1 , 1 / n ) ;
    // Optical depth within the sphere
    float dist1 = -2 * dot ( Normal1 , Dir1 ) ;
    // Out hit point in the tangent sphere
    float3 Pos2 = Pos1 + Dir1 * dist1 ;
    // Out normal
    float3 Normal2 = SphereCenter - Pos2 ; // no need for normalize
    // Out refracted ray
    float3 Dir2 = refract ( Dir1 , Normal2 , n ) ;
    // factor for hitting floor
    float dist2 = ( Pos2.z + h ) / Dir2.z ;
    return d - ( Pos2.x - dist2 * Dir2.x ) ;
}


float getSphericalDisplacement ( float portion )
{
    return getSphericalDisplacement ( portion , _Thickness , 1 + _RefractivePower ) ;
}

// ... (Hier kommen deine ultra-optimierten mathematischen Funktionen getOffsetUltra und calculateUICaustic rein) ...
// developed with google ai
float getOffsetUltra ( float d , float h , float n )
{
    d = clamp ( d , 0.0 , 0.999 );
    float2 s = float2 ( d , d / n );
    float2 c = sqrt ( max ( 1.0 - s * s , 0.0001 ) );
    float2 cb_sb = float2 ( c.y , s.y );
    float s_ab = dot ( float2 ( s.x , -c.x ) , cb_sb );
    float c_ab = dot ( float2 ( c.x , s.x ) , cb_sb );
    float s_theta = 2.0 * s_ab * c_ab;
    float c_theta = 1.0 - 2.0 * s_ab * s_ab;
    float tan_theta = s_theta / max ( c_theta , 0.0001 );
    return ( h + 1.0 ) * tan_theta + d - ( dot ( float2 ( s.y , -c.y ) , float2 ( c_ab , s_ab ) ) ) - ( cb_sb.x * c_ab + cb_sb.y * s_ab ) * tan_theta ;
}

// Universelle gebrochen-rationale Naeherung nach deinen Vorgaben:
// - Polstelle liegt strikt exakt bei d = 1
// - d ist absolut (0 bis 1)
// - Komplett wurzelfrei und extrem schnell
float getOffsetRationalUniversalCorrect ( float d , float h , float n )
{
    // Da d der absolute Abstand ist, sichern wir d >= 0 ab.
    // Gleichzeitig deckeln wir knapp vor 1.0 ab, um Division durch Null zu verhindern.
    d = clamp ( d , 0.0 , 0.99 );

    float d2 = d * d;
    float H = h + 1.0;

    // 1. Der feste Nenner erzwingt die Polstelle exakt bei d = 1
    float nenner = 1.0 - d2;

    // 2. Dynamische, vom Brechungsindex abhängige Zähler-Koeffizienten
    // Garantiert die korrekte paraxiale Steigung bei d -> 0
    float factorA = 2.0 * ( n - 1.0 ) / n;

    // Korrekturfaktor für den Kurvenverlauf bei ansteigendem d
    float factorB = -1.2 * ( n - 1.0 ) / ( n * n );

    // 3. Der kombinierte Zähler
    float zaehler = ( factorA * d * H ) + ( factorB * d * d2 );

    // 4. Finale Division
    return zaehler / max ( nenner , 0.0001 );
}
// Universelle gebrochen-rationale Approximation 3. Grades
// - Polstelle liegt strikt exakt bei d = 1
// - Zaehler nutzt maximal den Grad 3 (d3)
// - Extrem praezise bei d = 0.5 und paraxial
float getOffsetRationalDegree3 ( float d , float h , float n )
{
    // Absicherung fuer den absoluten Radius und die Polstelle
    d = clamp ( d , 0.0 , 0.99 );

    float d2 = d * d;
    float d3 = d * d2;
    float H = h + 1.0;

    // 1. Der feste Nenner sichert die Wand bei d=1
    float nenner = 1.0 - d2;

    // 2. Dynamische, vom Brechungsindex abhaengige Koeffizienten
    float factorA1 = 2.0 * ( n - 1.0 ) / n;
    float factorA3 = -0.42 * ( n - 1.0 );
    float factorB3 = -0.85 * ( n - 1.0 ) / ( n * n );

    // 3. Kombinierte Zaehlerberechnung (Maximal Grad 3)
    float zaehlerA = ( factorA1 * d + factorA3 * d3 ) * H;
    float zaehlerB = factorB3 * d3;

    // 4. Finale Division
    return ( zaehlerA + zaehlerB ) / max ( nenner , 0.0001 );
}
// Hochpraezise, wurzelfreie gebrochen-rationale Approximation
// - Erfuellt deine Bedingung: Polstelle exakt bei d = 1
// - Korrigiert den Fehler in der Mitte (d = 0.5) durch hoehere Potenzen
float getOffsetRationalHighPrecision ( float d , float h , float n )
{
    d = clamp ( d , 0.0 , 0.99 ); // d >= 0 abgesichert durch clamp

    float d2 = d * d;
    float d3 = d * d2;
    float d5 = d3 * d2;
    float H = h + 1.0;

    // 1. Der feste Nenner sichert die Wand bei d=1
    float nenner = 1.0 - d2;

    // 2. Dynamische, vom Brechungsindex abhaengige Kurven-Tuner
    float factorA1 = 2.0 * ( n - 1.0 ) / n;
    float factorA3 = -0.52 * ( n - 1.0 );

    float factorB3 = -1.0 * ( n - 1.0 ) / ( n * n );
    float factorB5 = 0.23 * ( n - 1.0 ) / n;

    // 3. Kombinierte Zaehlerberechnung
    float zaehlerA = ( factorA1 * d + factorA3 * d3 ) * H;
    float zaehlerB = ( factorB3 * d3 + factorB5 * d5 );

    // 4. Finale Division
    return ( zaehlerA + zaehlerB ) / max ( nenner , 0.0001 );
}

// Universelle kubische Approximation (3. Grades)
// - Extrem schnell: Benötigt KEINE Wurzeln, KEINE Trigonometrie, KEINE Divisionen!
// - Optimal für d = 0.0 bis 0.85
float getOffsetCubicUniversal ( float d , float h , float n )
{
    d = clamp ( d , 0.0 , 0.95 );

    float d3 = d * d * d;
    float H = h + 1.0;

    // 1. Berechne die dynamischen, vom Brechungsindex abhängigen Koeffizienten
    // (Könnten zur Not auch im Vertex-Shader vorbereitet werden)
    float a1 = 0.5 * n - 0.22;
    float a3 = 2.2 * n - 2.42;

    float b1 = 0.34 * n - 0.41;
    float b3 = -2.15 * n + 2.53;

    // 2. Die beiden ungeraden Polynome 3. Grades berechnen
    float A = a1 * d + a3 * d3;
    float B = b1 * d + b3 * d3;

    // 3. Zusammenführen
    return H * A + B;
}

// SIMD-vektorisierte kubische Approximation (3. Grades)
// Berechnet A und B parallel in einem einzigen float2-Vektor
float getOffsetCubicSIMD ( float d , float h , float n )
{
    d = clamp ( d , 0.0 , 0.95 );

    // 1. Erzeuge die Potenzen (Skalar-Operationen)
    float d3 = d * d * d;
    float H = h + 1.0;

    // 2. SIMD-Bündelung der linearen und kubischen Koeffizienten
    // x-Komponente rechnet für A, y-Komponente rechnet für B
    float2 coeff1 = float2( 0.5 , 0.34 ) * n + float2( -0.22 , -0.41 ); // float2(a1, b1)
    float2 coeff3 = float2( 2.2 , -2.15 ) * n + float2( -2.42 , 2.53 ); // float2(a3, b3)

    // 3. Parallele Berechnung von A und B mittels Hardware-Vektorisierung
    // Die GPU führt die Multiplikationen für beide Komponenten zeitgleich aus
    float2 AB = coeff1 * d + coeff3 * d3; // x = A, y = B

    // 4. Finale Zusammenführung (Skalarprodukt-Struktur via dot)
    // Entspricht mathematisch: H * AB.x + AB.y
    return dot ( AB , float2( H , 1.0 ) );
}

// DEINE GEBROCHEN-RATIONALE NÄHERUNGSFUNKTION (FEINGETUNT)
// Wurzelfrei, stabil bei n -> 1, Pol virtuell verschoben auf 1.21
float getOffsetYourFormula ( float d , float h , float n )
{
    d = clamp ( d , 0.0 , 0.99 ); // d >= 0 durch clamp gesichert

    float d2 = d * d;
    float d3 = d * d2;
    float H = h + 1.0;

    // Paraxialer Term (Steigung im Zentrum)
    float paraxial = 2.0 * H * ( n - 1.0 ) / max ( n , 0.001 ) * d;

    // Rationale Korrektur für den progressiven "Bauch" zum Rand hin
    float zaehler = H * ( n - 1.0 ) * 0.51 * d3;
    float nenner = 1.21 - d2;

    return paraxial + ( zaehler / max ( nenner , 0.0001 ) );
}

// DEINE MAXIMAL OPTIMIERTE UND FAKTORISIERTE FORMEL
// Keine unnötigen Divisionen, ausgeklammertes 'd' und single commonFactor
float getOffsetOptimized ( float d , float h , float n )
{
    d = clamp ( d , 0.0 , 0.99 );

    float d2 = d * d;

    // Faktor-Optimierungen
    float commonFactor = ( h + 1.0 ) * ( n - 1.0 ) ;
    float rcpN = 1.0 / max ( n , 0.001 ); // Ersetzt Division durch Multiplikation

    // Mathematische Ausklammerung
    float paraxialTeil = 2.0 * rcpN;
    float rationalTeil = 0.51 * d2 / max ( 1.21 - d2 , 0.0001 );

    return commonFactor * d * ( paraxialTeil + rationalTeil );
}

float getSphericalDisplacement_KI ( float portion )
{
    bool UseApprox = false;
    //UseApprox = true ;
    if ( UseApprox ) return getOffsetYourFormula ( portion , _Thickness , 1 + _RefractivePower ) ;
    else return getOffsetUltra ( portion , _Thickness , 1 + _RefractivePower ) ;
}

float getDisplacement ( float portion )
{
#if BP_ROUND
    return getSphericalDisplacement_KI ( portion ) ;

    // formula is: cos(asin(x))*tan(asin(x)-asin(n*x))
    // simulation: 0.62 * (x - x²) / (1.04 - x)
    //return _RefractivePower * ( portion - portion * portion ) / ( 1.04 - portion ) ;

    //float2 SinAlphaBeta = portion ;
    //SinAlphaBeta.y *= 1 - _RefractivePower ;
    //float2 AlphaBeta = asin ( SinAlphaBeta ) ;
    //float gamma = AlphaBeta.x - AlphaBeta.y ;
    //float h = sqrt ( 1 - portion * portion ) ;
    //return ( h + _Thickness ) * tan ( gamma ) ;

#elif BP_BULGE
    return _RefractivePower * portion * portion * portion / ( 1.2 - portion ) ;
    //return _RefractivePower * 4 * portion * portion * portion * portion * portion ;
#elif BP_BEVEL
    return portion > 0 ? ( 1 - portion ) * _RefractivePower  : 0 ;
#else
    return 0;
#endif
    //return _RefractivePower * portion * portion / ( 1.2 - portion ) ;
    //return _RefractivePower * portion / ( 1.2 - portion ) ;
}

float getGlossPower ( float portion , float2 normal )
{
    //portion *= portion ;
    //float x = portion + 0.5 - REFLEXION_BORDER_POS ;

    portion = sqrt ( 1 - portion * portion ) ;
    //float x = portion + 0.5 - ( 1 - REFLEXION_BORDER_POS ) ;
    //float x = portion + REFLEXION_BORDER_POS - 0.5 ;
    float x = portion + 0.5 - REFLEXION_BORDER_POS ;

    float light = saturate ( 4 * ( x - x * x ) ) ;
    float lightPortion = pow ( light , REFLEXION_WIDTH ) ;
#if CALC_REFLECTION_HORIZONTAL
    lightPortion *= abs ( dot ( normal , LIGHT_DIR_2D ) ) ;
#endif
    return lightPortion ;
}

//#define linstep(lo,hi,v) saturate((v-lo)/(hi-lo))
//#define sqr(x) ((x)*(x))
float4 getImageShadowColorWithCaustic2 ( float4 color , float alphaImage , float alphaShadow , float causticIntensity , float4 colOrig )
{
    // 1. Dein bewährter Schutzschild gegen Division durch Null
    float totalAlpha = alphaImage + alphaShadow;
    if ( totalAlpha <= 0.0001f )
    {
        return float4( colOrig.rgb , 0.0f );
    }

    // 2. DIE REINE RECHTECK-KANTEN-MISCHUNG (Nur für das Anti-Aliasing)
    // Wir trennen hier strikt das Glas-Element vom Hintergrund, 
    // OHNE den Schatten hier schon mathematisch zu verzerren.
    float BySumAlpha = rcp ( totalAlpha ) ;
    float fIn = alphaImage * BySumAlpha ;
    float fOut = alphaShadow * BySumAlpha ;

    // colMixRGB mischt jetzt nur noch die puren Farben an der Kante
    float3 colMixRGB = ( color.rgb * fIn + colOrig.rgb * fOut ) * fIn ;
    float3 standardUGUIRGB = lerp ( colMixRGB , color.rgb , alphaImage ) ;

    // 3. DIE WEICHE ZWISCHEN INNEN UND AUSSEN
    // alphaImage = 1 (voll innen, geblurtes Glas) | alphaImage = 0 (voll außen, scharfer Hintergrund)
    float3 baseCombinedRGB = lerp ( colOrig.rgb , standardUGUIRGB , alphaImage ) ;

    // 4. UNIFORMER SCHATTEN (Für innen und außen absolut identisch!)
    // Jetzt wenden wir den Schatten absolut linear auf das GESAMTE Bild an.
    // Dadurch dunkelt es im Inneren exakt genauso stark ab wie im Äußeren.
    baseCombinedRGB *= ( 1.0f - alphaShadow );

    // 5. UNIFORME MULTIPLIKATIVE KAUSTIK
    // Auch die Kaustik rechnet sich jetzt sauber auf die einheitliche Schattenbasis drauf.
    float causticBoost = 2.0f;
    float3 causticMultiplier = 1.0f + ( causticIntensity * causticBoost );
    baseCombinedRGB *= causticMultiplier;

    // 6. ABSOLUTE DECKKRAFT (Wie von dir völlig richtig korrigiert!)
    return float4 ( baseCombinedRGB , 1.0f ) ;
}

float4 getImageShadowColorWithCaustic3 ( float4 color , float alphaImage , float alphaShadow , float4 colOrig )
{
    // 1. Schutzschild: Wenn absolut kein Element-Alpha vorhanden ist,
    // geben wir das Original-Hintergrundpixel zurück (mit Alpha 1.0f wegen der festen Deckkraft)
    if ( alphaImage <= 0.0001f && alphaShadow == 0.0f )
    {
        return float4( colOrig.rgb , 1.0f );
    }

    // 2. DIE REINE RECHTECK-KANTEN-MISCHUNG (Nur für das Anti-Aliasing)
    // Wir fangen ein mathematisches totalAlpha <= 0 ab, falls alphaShadow negativ ist
    float totalAlpha = max ( alphaImage + abs ( alphaShadow ) , 0.0001f );
    float BySumAlpha = rcp ( totalAlpha ) ;
    float fIn = alphaImage * BySumAlpha ;
    float fOut = abs ( alphaShadow ) * BySumAlpha ; // abs() sichert das Blending an der Kante

    // colMixRGB mischt die puren Farben an der Kante
    float3 colMixRGB = ( color.rgb * fIn + colOrig.rgb * fOut ) * fIn ;
    float3 standardUGUIRGB = lerp ( colMixRGB , color.rgb , alphaImage ) ;

    // 3. DIE WEICHE ZWISCHEN INNEN UND AUSSEN
    float3 baseCombinedRGB = lerp ( colOrig.rgb , standardUGUIRGB , alphaImage ) ;

    // 4. DIE MATHE-MEISTERFORMEL (Behandelt Schatten UND Kaustik zeitgleich!)
    // Wenn alphaShadow positiv ist (Schatten) -> Wert < 1.0 (Verdunklung)
    // Wenn alphaShadow negativ ist (Kaustik)  -> Wert > 1.0 (Multiplikative Aufhellung)
    baseCombinedRGB *= ( 1.0f - alphaShadow );

    // 5. ABSOLUTE DECKKRAFT
    return float4 ( baseCombinedRGB , 1.0f ) ;
}

float getCaustic ( float delta , float sharpness )
{
    float causticIntensity = exp ( -( delta * delta ) * sharpness );
    return causticIntensity ;
}
float getCausticLorentz ( float delta , float sharpness )
{
    // delta*delta sorgt für Symmetrie.
    // sharpness multipliziert das Delta: Je höher, desto schmaler der Kern.
    // Durch das "+ 1.0f" im Nenner ist das Maximum bei delta=0 exakt 1.0.
    //float d2 = delta * delta * sharpness;
    float d2 = abs ( delta ) * sharpness;
    return 10.0f / ( 1.0f + d2 );
}
float getCausticHyperbolic ( float delta , float thickness , float overload )
{
    // thickness kontrolliert die Dicke des Kerns (z.B. 0.05f)
    // Wenn delta = 0, steht dort thickness / thickness = 1.0
    return overload * thickness / ( thickness + abs ( delta ) );
}
float getCausticLorentzNormalized ( float delta , float width , float energy1 )
{
    // Keine PI-Division mehr! Pure, pfeilschnelle Register-Mathematik:
    float numerator = energy1 * sqrt ( width );

    // f(x) = (E1 * sqrt(w)) / (w + x²)
    //return numerator / ( width + ( delta * delta ) );
    return numerator / ( width + abs ( delta ) );
}
float getCausticHyperbolicNormalized ( float delta , float width , float energy )
{
    float numerator = energy * sqrt ( width );
    return numerator / ( width + abs ( delta ) );
}

float getPeak_Hyperbole ( float height , float width , float x )
{
    return height * width / ( width + abs ( x ) )  ;
}

float getPeakHeight_Hyperbole ( float width , float energy , float range )
{
    return energy / ( width * log ( 1.0f + range / width ) ) ;
}

float getPeak_Lorentz ( float height , float width , float x )
{
    return height * width / ( width + abs ( x ) )  ;
}

float getPeakHeight_Lorentz ( float width , float energy , float range )
{
    return energy / ( sqrt ( width ) * 3.14f ) ;
}

// =================================================================
// 5. ASYMMETRISCHE HYPERBEL (Transparent & Vektorisiert via asym2)
// =================================================================
float getPeakHeight_AsymmetricHyperbole ( float width , float energy , float range , float2 asym2 )
{
    // Keine Divisionen für die Breite dank rcp
    float byWidth = rcp ( width );

    // Parallele X-Berechnung für beide Richtungen gleichzeitig (SIMD)
    float2 x_vector = asym2 * ( range * byWidth );

    // Der native SIMD-Vector-Logarithmus (beide ln in einem einzigen Taktzyklus)
    float2 log_vector = log ( 1.0f + x_vector );

    // Das kostenlose Swizzling (.yx) multipliziert parallel über Kreuz
    float2 area_components = log_vector * asym2.yx;

    // Blitzschnelles Aufaddieren der Flächenhälften im Register
    float totalArea = width * ( area_components.x + area_components.y );

    return energy / max ( totalArea , 0.0001f );
}

float getPeak_AsymmetricHyperbole ( float height , float width , float x , float2 asym2 )
{
    float byWidth = rcp ( width );

    // Logische Weiche ohne 'abs()':
    // Wenn x > 0 (innen) -> nutze s_inner (asym2.x)
    // Wenn x < 0 (außen) -> nutze -s_outer (-asym2.y)
    float currentSteepness = ( x > 0.0f ) ? asym2.x : -asym2.y;

    // f(x) = h / (1.0 + (x * s) * byWidth)
    return height / ( 1.0f + ( x * currentSteepness ) * byWidth );
}

// =================================================================
// 6. ASYMMETRISCHE LORENTZ-KURVE (Unendlich normiert -> Ohne atan!)
// =================================================================
float getPeakHeight_AsymmetricLorentz ( float width , float energy , float range , float2 asym2 )
{
    // Die Konstante PI / 2
    const float HALF_PI = 1.57079632f;

    // Das Aufaddieren der Steilheiten (s_inner + s_outer) ist ein genialer, 
    // nativer Vektor-Zusatzbefehl auf der GPU: (asym2.x + asym2.y)
    float s_sum = asym2.x + asym2.y;

    // Gesamtfläche laut unendlicher Stammfunktion:
    float totalArea = HALF_PI * sqrt ( width ) * s_sum;

    // Höhe = Energie / Gesamtfläche
    return energy / max ( totalArea , 0.0001f );
}

float getPeak_AsymmetricLorentz ( float height , float width , float x , float2 asym2 )
{
    // Logische Weiche ohne 'abs()':
    float s = ( x > 0.0f ) ? asym2.x : asym2.y;

    // f(x) = h * w / (w + x² * s²)
    return ( height * width ) / ( width + sqr ( x * s ) );
}
// =================================================================
// 7. ASYMMETRISCHE GAUSS-KURVE (Unendlich normiert -> Blitzschnell!)
// =================================================================
float getPeakHeight_AsymmetricGauss ( float width , float energy , float range , float2 asym2 )
{
    const float PI = 3.14159265f;

    // Nutzt die native, pfeilschnelle Vektor-Addition (s_inner + s_outer)
    float s_sum = asym2.x + asym2.y;

    // Das exakte Integral der asymmetrischen Gauß-Glocke
    float totalArea = 0.5f * sqrt ( PI * width ) * s_sum;

    // Höhe = Energie / Gesamtfläche
    return energy / max ( totalArea , 0.0001f );
}

float getPeak_AsymmetricGauss ( float height , float width , float x , float2 asym2 )
{
    // Logische Weiche ohne 'abs()':
    float s = ( x > 0.0f ) ? asym2.x : asym2.y;

    // rcp(width) für die Multiplikation statt Division auf der GPU
    float byWidth = rcp ( max ( width , 0.0001f ) );

    // DEINE MEISTER-OPTIMIERUNG: Wir nutzen sqr(x * s) im Exponenten!
    // f(x) = h * exp( -sqr(x * s) / w )
    return height * exp ( -sqr ( x * s ) * byWidth );
}

#define PEAK_MODE_ASYMMETRIC_HYPER   1
#define PEAK_MODE_ASYMMETRIC_LORENTZ 2
#define PEAK_MODE_ASYMMETRIC_GAUSS   3

#define CURRENT_CAUSTIC_MODE         PEAK_MODE_ASYMMETRIC_HYPER

#if CURRENT_CAUSTIC_MODE == PEAK_MODE_ASYMMETRIC_HYPER
    #define getPeakHeight getPeakHeight_AsymmetricHyperbole
    #define getPeakValue getPeak_AsymmetricHyperbole
#elif CURRENT_CAUSTIC_MODE == PEAK_MODE_ASYMMETRIC_LORENTZ
    #define getPeakHeight getPeakHeight_AsymmetricLorentz
    #define getPeakValue getPeak_AsymmetricLorentz
#elif CURRENT_CAUSTIC_MODE == PEAK_MODE_ASYMMETRIC_GAUSS
    #define getPeakHeight getPeakHeight_AsymmetricGauss
    #define getPeakValue getPeak_AsymmetricGauss
#else
    float getPeakHeight ( float width , float energy , float range , float2 asym2 ) { return 0.0f ; }
    float getPeakValue ( float height , float width , float x , float2 asym2 ) { return 0.0f ; }
#endif

texel_data getBorderData_sdf ( v2f IN , float4 grab_texture_texel_size )
{
    float2 Pos = getPosPixels ( IN ) ;
    texel_data o ;
    o.grabPos = IN.grabPos ;
    float3 DistGradBox = sdgBox ( Pos + CENTER_OFFSET , RECTANGLE_HALF_SIZE , _Radius ) ;
    float DistBox = DistGradBox.x ;
    float CornerAlphaMultiplier = linstep_halfwidth ( 0.0f , HALF_SMOOTHING , -DistBox ) ;
    float BorderPortion = saturate ( ( DistBox + BORDER_WIDTH ) * BORDER_FADE ) ;
    float Displacement = getDisplacement ( BorderPortion ) * BORDER_WIDTH ;
    float2 OffsetDir = -DistGradBox.yz ;
#if ROTATION
    OffsetDir = mul ( ( float2x2 ) _Rotation , OffsetDir ) ;
#endif
    float2 Offset = OffsetDir * Displacement ;
    o.grabPos.xy += Offset * grab_texture_texel_size.xy ;
#if DRAW_REFLECTION
    o.gloss = getGlossPower ( BorderPortion , OffsetDir ) ;
#endif
    o.alpha = CornerAlphaMultiplier ;
    // shadow

#if DROP_SHADOW
    {
        float MinHalfSize = min ( RECTANGLE_HALF_SIZE.x , RECTANGLE_HALF_SIZE.y ) ;

        float ShadowWidth = BORDER_WIDTH * 0.5f ;
        Offset *= CornerAlphaMultiplier > 0.25f ;

        float2 ReducedHalfSize = max ( RECTANGLE_HALF_SIZE - ShadowWidth , 0.0f ) ;
        float4 radii = max ( _Radius - ShadowWidth , 0.0f ) ;
        float Dist = sdRoundBox ( Pos + Offset + CENTER_OFFSET + float2 ( ShadowWidth * 0.0f , ShadowWidth * 0.5f * _ShadowOffsetY ) , ReducedHalfSize , radii ) ;

        float shadowCenter = abs ( Dist - ShadowWidth * 0.75f ) ;
        float mainShadow = smoothstep ( ShadowWidth , 0 , shadowCenter ) ;
        float baseShadow = _ShadowDarkening * mainShadow;

        // caustic
#if USE_CAUSTIC
        float delta = Dist - ShadowWidth * _CausticPosition ;

        // Grenzwerte für die Breite
        float w0 = BORDER_WIDTH * 1.5f;
        float w1 = BORDER_WIDTH * 0.001f;

        float PeakWidth = ( w1 - w0 ) * CausticSharpness + w0 ;

        float visualBoost = ( 3.0f - 1.5f * CausticSharpness ) * CausticPower * 2.0f ;
        float Energy = 0.5f * _ShadowDarkening * ShadowWidth * visualBoost;
        float Range = ShadowWidth; // necessairy for hyperbolic peaks which cannot be normalized to infinity

        float PeakHeight = getPeakHeight ( PeakWidth , Energy , Range , CausticAsym );
        float causticIntensity = getPeakValue ( PeakHeight , PeakWidth , delta , CausticAsym );

        // Dein genialer Vierfach-Schnitt für das perfekte Helligkeits-Plateau
        float shadowCut = mainShadow * mainShadow;
        causticIntensity *= shadowCut * shadowCut;

        // Die funktionelle One-Line-Verschmelzung
        baseShadow = 1.0f - ( 1.0f - baseShadow ) * ( 1.0f + causticIntensity );
#endif
        o.shadow_alpha = baseShadow  ;// *sqrt ( _RefractivePower ) ;
    }
#else
    {
        o.shadow_alpha = 0.0f ;
    }
#endif
    return o ;
}

// https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-intrinsic-functions
// https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx9-graphics-reference-asm-ps-instructions-ps-3-0
texel_data getBorderData_Rounded ( v2f IN , float4 grab_texture_texel_size )
{
    float2 Pos = getPosPixels ( IN ) ;

    float4 EdgeDist4 = getEdgeDist4 ( Pos ) ; // abs ( Pos.x , wy - Pos.y , wx - Pos.x , Pos.y ) in pixels
    float4 IsInCorner4 = isInCorner4 ( EdgeDist4 ) ;


    // border
    bool IsInCorner = any ( IsInCorner4 ) ;
    //float2 Offset ( 0 , 0 ) ;
    texel_data o ;
    o.grabPos = IN.grabPos ;
    //o.gloss = 0 ;
    // calculate displacement

    // SMOOTHED_BORDER_WIDTH _BorderWidth.w = BorderWidth / Smoothing
    // BORDER_FADE           _BorderWidth.y = 1 / BorderWidth
    // SMOOTHED_BORDER_FADE  _BorderWidth.z = 1 / Smoothing  else  fBorderWidth / Smoothing


    if ( IsInCorner )
    {
        float4 DiffX4 = getCircleCenterDiffX4 ( Pos , _CenterXAlpha ) ; // _CenterX - pos.xxxx
        float4 DiffY4 = getCircleCenterDiffY4 ( Pos , _CenterYAlpha ) ; // _CenterY - pos.yyyy
#if BP_BEVEL
        float4 CenterDist4 = abs ( DiffX4 ) + abs ( DiffY4 ) - HALF4 ; // ONE4 HALF4

#else
        float4 CenterDist4 = getCircleCenterDist4 ( DiffX4 , DiffY4 ) ;
#endif
        float4 CircleDist4 = _Radius - CenterDist4 ;
        float4 SmoothedCircleDist4 = CircleDist4 * BY_SMOOTHING + ONE4 ; // BY_SMOOTHING = 1f / Smoothing
        float CornerAlphaMultiplier = any ( IsInCorner4 ) ? saturate ( getMaxVal ( SmoothedCircleDist4 * IsInCorner4 ) ) : ONE ;
        //float EdgeDist = -BY_SMOOTHING * getMaxVal ( CircleDist4 * IsInCorner4 )  ;
        //float BorderPortion = saturate ( SMOOTHED_BORDER_FADE + EdgeDist * BORDER_FADE ) ;

        //float EdgeDist = -BY_SMOOTHING * getMaxVal ( CircleDist4 * IsInCorner4 )  ;
        //float EdgeDist = SMOOTHED_BORDER_FADE * 0.5 ;
        //CenterDist4 = getCircleCenterDist4 ( DiffX4 + SMOOTHED_BORDER_FADE * 5 , DiffY4 + SMOOTHED_BORDER_FADE * 5 ) ;
        //CircleDist4 = _Radius - CenterDist4 ;

        DiffX4 = getCircleCenterDiffX4 ( Pos , _CenterX ) ; // _CenterX - pos.xxxx
        DiffY4 = getCircleCenterDiffY4 ( Pos , _CenterY ) ; // _CenterY - pos.yyyy
#if BP_BEVEL
        CenterDist4 = abs ( DiffX4 ) + abs ( DiffY4 ) - HALF4 ; // ONE4 HALF4

#else
        CenterDist4 = getCircleCenterDist4 ( DiffX4 , DiffY4 ) ;
#endif
        CircleDist4 = _Radius - CenterDist4 ;

        float EdgeDist = -getSum ( CircleDist4 * IsInCorner4 )  ;
        //EdgeDist += SMOOTHED_BORDER_FADE * -10 ;
        //EdgeDist += 0.5 ;
        float BorderPortion = saturate ( ONE + EdgeDist * BORDER_FADE ) ;
        //float BorderPortion = saturate ( ONE + EdgeDist ) ;
        //float BorderPortion = saturate ( ONE + EdgeDist ) ;

        float Displacement = getDisplacement ( BorderPortion ) * BORDER_WIDTH ;

#if BP_BEVEL
        float2 OffsetDir = normalize ( float2 ( getSum ( CornerNormalsX * IsInCorner4 ) , getSum ( CornerNormalsY * IsInCorner4 ) ) ) ;
#else
        float2 OffsetDir = normalize ( float2 ( getSum ( DiffX4 * IsInCorner4 ) , getSum ( DiffY4 * IsInCorner4 ) ) ) ;
#endif

#if ROTATION
        OffsetDir = mul ( ( float2x2 ) _Rotation , OffsetDir ) ;
#endif

        float2 Offset = OffsetDir * grab_texture_texel_size.xy * Displacement ;
        o.grabPos.xy += Offset ;
#if DRAW_REFLECTION
        o.gloss = getGlossPower ( BorderPortion , OffsetDir ) ;
#endif
        o.alpha = CornerAlphaMultiplier ;
    }
    else
    {
        //float EdgeDist = HALF4 - getMinVal ( EdgeDist4 ) * BY_SMOOTHING ; // = 0.5 - mindist / Smoothing
        //float BorderPortion = saturate ( SMOOTHED_BORDER_FADE + EdgeDist * BORDER_FADE ) ;

        float EdgeDist = getMinVal ( EdgeDist4 ) ; // = 0.5 - mindist / Smoothing
        float BorderPortion = saturate ( ONE4 - ( EdgeDist  ) * BORDER_FADE ) ; //  BORDER_FADE = _BorderWidth.y = 1 / BorderWidth
        //float BorderPortion = saturate ( ONE + EdgeDist ) ;

        float4 IsInEdge4 = isInEdge4 ( EdgeDist4 , EdgeDist ) ;
        float2 OffsetDir = normalize ( float2 ( getSum ( EdgeNormalsX * IsInEdge4 ) , getSum ( EdgeNormalsY * IsInEdge4 ) ) ) ;
#if ROTATION
        OffsetDir = mul ( ( float2x2 ) _Rotation , OffsetDir ) ;
#endif

        float Displacement = getDisplacement ( BorderPortion ) * BORDER_WIDTH ;

        //float2 OffsetDir = sign ( HALF2 - IN.texcoord.xy ) ;
        //if ( ( IN.texcoord.y > IN.texcoord.x ) == ( IN.texcoord.y > ( ONE - IN.texcoord.x ) ) ) OffsetDir.x = 0 ;
        //else OffsetDir.y = 0 ;
        o.grabPos.xy += OffsetDir * grab_texture_texel_size.xy * Displacement ;
#if DRAW_REFLECTION
        o.gloss = getGlossPower ( BorderPortion , OffsetDir ) ;
#endif
        o.alpha = 1 ;
    }


    //o.grabPos = IN.grabPos ;
    return o ;
}

texel_data getBorderData_Edgy ( v2f IN , float4 grab_texture_texel_size )
{
    float2 Pos = getPosPixels ( IN ) ;

    float4 EdgeDist4 = getEdgeDist4 ( Pos ) ; // ( Pos.x , wy - Pos.y , wx - Pos.x , Pos.y )
    float4 DiffX4 = getCircleCenterDiffX4 ( Pos , _CenterX ) ;
    float4 DiffY4 = getCircleCenterDiffY4 ( Pos , _CenterY ) ;

    // calculate displacement

    //float EdgeDist = HALF4 - getMinVal ( EdgeDist4 ) * BY_SMOOTHING ; // = 0.5 - mindist / Smoothing
    //float BorderPortion = saturate ( SMOOTHED_BORDER_FADE + EdgeDist * BORDER_FADE ) ;

    //float EdgeDist = HALF4 - getMinVal ( EdgeDist4 ) ; // = 0.5 - mindist / Smoothing
    //float BorderPortion = saturate ( ONE + EdgeDist * BORDER_FADE ) ;

    float EdgeDist = getMinVal ( EdgeDist4 ) ; // = 0.5 - mindist / Smoothing
    float BorderPortion = saturate ( ONE4 - ( EdgeDist ) *BORDER_FADE ) ; //  BORDER_FADE = _BorderWidth.y = 1 / BorderWidth
    //float BorderPortion = saturate ( ONE + EdgeDist ) ;

    float4 IsInEdge4 = isInEdge4 ( EdgeDist4 , EdgeDist ) ;
    float2 OffsetDir = normalize ( float2 ( getSum ( EdgeNormalsX * IsInEdge4 ) , getSum ( EdgeNormalsY * IsInEdge4 ) ) ) ;
#if ROTATION
    OffsetDir = mul ( ( float2x2 ) _Rotation , OffsetDir ) ;
#endif

    float Displacement = getDisplacement ( BorderPortion ) * BORDER_WIDTH ;

    //float2 OffsetDir = sign ( HALF2 - IN.texcoord.xy ) * _GrabTexture_TexelSize.xy * Displacement * BorderWidth2 ;
    //if ( ( IN.texcoord.y > IN.texcoord.x ) == ( IN.texcoord.y > ( ONE - IN.texcoord.x ) ) ) OffsetDir.x = 0 ;
    //else OffsetDir.y = 0 ;
    texel_data o ;
    o.grabPos = IN.grabPos ;
    o.grabPos.xy += OffsetDir * grab_texture_texel_size.xy * Displacement ;
#if DRAW_REFLECTION
    o.gloss = getGlossPower ( BorderPortion , OffsetDir ) ;
#endif

    return o ;
}

