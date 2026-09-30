using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using UnityEngine.Events;
using Rudi.Extensions;
using Rudi.Core;

namespace Rudi.UI
{
    [ExecuteAlways]
    [AddComponentMenu ( "Rudi/UI/Raw Image V2" )]
    public class RawImageV2 : RawImage, ISoftShadow
    {
        private static readonly string TAG = "Rudis RawImageV2" ;

        public enum Gradient
        {
            None,
            Right,
            Down,
        }

        public enum LoadState
        {
            NotLoaded,
            Loading,
            Loaded,
        }

        public enum FitMode
        {
            StretchToFit,
            ShrinkToFit,
            CropToFit,
        }

        public enum AlphaTexMap
        {
            None,
            MapTexAlpha,
            MapTexColor,
        }

        [ SerializeField ] public bool m_DrawTextureOnly = false ;
        [ SerializeField ] public bool m_RecalcLayoutAfterTextureLoad = false ;

        [ SerializeField ] private Gradient m_Gradient = Gradient.None ;
        [ SerializeField ] private bool m_GradientLightning = false ;
        [ SerializeField ] private Color m_GradeColor = Color.white ;
        [ Range ( -100 , 100 ) ]
        [ SerializeField ] private int m_Saturating = 75 ;
        [ Range ( -100 , 100 ) ]
        [ SerializeField ] private int m_Lightning = 62 ;

        [ SerializeField ] FitMode m_FitMode = FitMode.ShrinkToFit ;
        [ SerializeField ] bool m_PixelCorrect = false ;
        [ Range ( 0 , 3 ) ]
        [ SerializeField ] int m_Rotation = 0 ;
        [ SerializeField ] bool m_MirrorVertical = false ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_CropX = 0 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_CropY = 0 ;

        // alpha map
        [ SerializeField ] AlphaTexMap m_MapTexAlpha = AlphaTexMap.None ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_TexAlpha0 = 0 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_TexAlpha1 = 1 ;
        [ SerializeField ] Color m_ColorAlpha0 = new Color ( 1 , 1 , 1 , 0 ) ;
        [ SerializeField ] Color m_ColorAlpha1 = new Color ( 1 , 1 , 1 , 1 ) ;

        // Border
        [ SerializeField ] bool m_DrawBorder = false ;
        [ Range ( 0 , 20 ) ]
        [ SerializeField ] float m_BorderWidth = 0 ;
        [ SerializeField ] Color m_BorderColor = new Color32 ( 50 , 50 , 50 , 255 ) ;
        [ SerializeField ] Color m_FillColor = Color.white ;
        [ SerializeField ] bool m_FadeBorder = false ;

        [ SerializeField ] bool m_RelativeRadius = false ;
        [ Range ( 0 , 50 ) ]
        [ SerializeField ] float m_Radius = 0 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_RadiusRelative = 0.5f ;
        [ SerializeField ] bool m_LimitToHalfRound = false ;
        [ Range ( 0 , 2 ) ]
        [ SerializeField ] float m_Smoothing = 1f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_RadiusScale1 = 1 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_RadiusScale2 = 1 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_RadiusScale3 = 1 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_RadiusScale4 = 1 ;

        [ SerializeField ] bool m_MapCornerAlpha = false ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_CornerAlpha0 = 0 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_CornerAlpha1 = 1 ;

        [ SerializeField ] private DropShadow m_DropShadow = new () ;

        [ SerializeField ] UnityEvent m_OnTextureLoaded = new UnityEvent () ;

        private void log ( string msg ) => Log.i ( TAG , gameObject , msg );

        public UnityEvent onTextureLoaded => m_OnTextureLoaded;


        // shadow methods

        private DropShadow shadowProperties => m_DropShadow;

        public void shadowChanged ()
        {
            if ( !showShadow )
            {
                //Log.i ( TAG , "showShadow = false" ) ;
                return;
            }

            //Log.i ( TAG , "shadowChanged ()" ) ;
            SetVerticesDirty ();
            SetMaterialDirty ();
        }

        public bool showShadow
        {
            get => m_DropShadow.showShadow;
            set
            {
                if ( value == m_DropShadow.showShadow ) return;
                m_DropShadow.showShadow = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }
        public float distance => m_DropShadow.distance;
        public float elevation
        {
            get => m_DropShadow.elevation;
            set
            {
                m_DropShadow.elevation = value;
                shadowChanged ();
            }
        }

        //public float multipliedElevation => m_DropShadow.multipliedElevation ;


        public float elevationMultiplier
        {
            get => shadowProperties.elevationMultiplier;
            set
            {
                if ( shadowProperties.elevationMultiplier == value ) return;
                shadowProperties.elevationMultiplier = value;
                shadowChanged ();
            }
        }

        private TweenRunnerFloat m_ElevationTweener ;
        private TweenRunnerFloat elevationTweener
        {
            get
            {
                if ( null == m_ElevationTweener )
                {
                    m_ElevationTweener = new ( this , () => elevationMultiplier );
                    m_ElevationTweener.onValueChanged.AddListener ( ( val ) => elevationMultiplier = val );
                }
                return m_ElevationTweener;
            }
        }
        public void CrossFadeElevationMultiplier ( float dest , float duration ) => elevationTweener.tween ( dest , duration );
        public bool omitIfZeroDistance
        {
            get => true;
            set { }
        }
        public bool hasEventuallyShadow => showShadow;
        public bool hasShadow
        {
            get
            {
                if ( !showShadow ) return false;
                if ( 0.0f == multipliedElevation ) return false;
                //if ( 0 == m_ShadowDarkening ) return false;
                return true;
            }
        }

        bool ISoftShadow.isEnabled => showShadow ;

        private static float secureInverse ( float v ) => 0 != v ? 1f / v : 0f;
        private float multipliedElevation => shadowProperties.multipliedElevation;
        private float shadowDistance => shadowProperties.shadowDistance; //multipliedDistance * byCosAngleVertical * 2.0f;
        private float darkening => shadowProperties.darkening;

        private float diffusivity => shadowProperties.diffusivity;
        private float sigma => shadowDistance * diffusivity * 0.1f;
        private float pixel_sigma => sigma * ppu;
        public Vector2 dirHorizontal => shadowProperties.dirHorizontal;
        private float shadowAlpha => shadowProperties.shadowAlpha;

        private Vector2 shadowOffset => shadowProperties.shadowOffset;
        public float shadow_offset_factor => shadowDistance;
        public float shadow_offset_factor_pixels => shadow_offset_factor * ppu;
        public Vector3 lightDir => shadowProperties.lightDir;
        // texture methods

        public FitMode fitModeToUse => null != texture ? m_FitMode : FitMode.StretchToFit;
        public bool cropToFit => fitModeToUse == FitMode.CropToFit;
        public bool shrinkToFit => fitModeToUse == FitMode.ShrinkToFit;

        public FitMode fFitMode
        {
            get => m_FitMode;
            set
            {
                if ( value != m_FitMode )
                {
                    m_FitMode = value;
                    SetVerticesDirty ();
                    SetMaterialDirty ();
                }
            }
        }

        private Color GetGradientColor ()
        {
            if ( m_GradientLightning )
            {
                return Core.ColorTools.getLightenedColor ( color , m_Saturating * 0.01f , m_Lightning * 0.01f );
            }
            else
            {
                return m_GradeColor;
            }
        }

        public bool drawTextureOnly
        {
            get => m_DrawTextureOnly;
            set
            {
                if ( m_DrawTextureOnly == value ) return;
                m_DrawTextureOnly = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        public Color gradientColor
        {
            get => m_GradeColor;
            set
            {
                if ( m_GradeColor == value ) return;
                m_GradeColor = value;
                if ( m_Gradient != Gradient.None )
                {
                    SetVerticesDirty ();
                    SetMaterialDirty ();
                }
            }
        }
        public bool pixel_Correct
        {
            get => m_PixelCorrect;
            set
            {
                if ( value == m_PixelCorrect ) return;
                m_PixelCorrect = value;
                SetMaterialDirty ();
            }
        }

        public bool drarBorder
        {
            get => m_DrawBorder;
            set
            {
                if ( value == m_DrawBorder ) return;
                m_DrawBorder = value;
                SetMaterialDirty ();
            }
        }
        public float borderWidth
        {
            get => m_BorderWidth;
            set
            {
                if ( value == m_BorderWidth ) return;
                m_BorderWidth = value;
                SetMaterialDirty ();
            }
        }

        public bool borderFade
        {
            get => m_FadeBorder;
            set
            {
                if ( value == m_FadeBorder ) return;
                m_FadeBorder = value;
                SetMaterialDirty ();
            }
        }

        public Color borderColor
        {
            get => m_BorderColor;
            set
            {
                if ( value.Equals ( m_BorderColor ) ) return;
                m_BorderColor = value;
                SetMaterialDirty ();
            }
        }
        public Color fillColor
        {
            get => m_FillColor;
            set
            {
                if ( value.Equals ( m_FillColor ) ) return;
                m_FillColor = value;
                SetMaterialDirty ();
            }
        }

        public bool halfRound
        {
            set
            {
                if ( value )
                {
                    m_RelativeRadius = true;
                    m_RadiusRelative = 0.5f;
                }
            }
        }

        public bool relativeRadius
        {
            get => m_RelativeRadius;
            set
            {
                m_RelativeRadius = value;
                SetMaterialDirty ();
            }
        }

        public bool limitToHalfRound
        {
            get => m_LimitToHalfRound;
            set
            {
                m_LimitToHalfRound = value;
                SetMaterialDirty ();
            }
        }

        public int rotation
        {
            get => m_Rotation;
            set
            {
                if ( value == m_Rotation ) return;
                m_Rotation = value;
                SetVerticesDirty ();
                SetMaterialDirty ();

            }
        }

        public float cropX
        {
            get
            {
                if ( !cropToFit ) return m_CropX;
                var rect = rectTransform.rect ;
                var spriteRatio = isSwapped ? ( float ) numPixelsY_raw / numPixelsX_raw : ( float ) numPixelsX_raw / numPixelsY_raw ;
                var rectRatio = rect.width / rect.height ;
                return rectRatio < spriteRatio ? 1f - rectRatio / spriteRatio : 0f;
            }

            set
            {
                if ( value == m_CropX ) return;
                m_CropX = Mathf.Clamp01 ( value );
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        public float cropY
        {
            get
            {
                if ( !cropToFit ) return m_CropY;
                //return 0f ;
                var rect = rectTransform.rect ;
                var spriteRatio = isSwapped ? ( float ) numPixelsY_raw / numPixelsX_raw : ( float ) numPixelsX_raw / numPixelsY_raw ;
                var rectRatio = rect.width / rect.height ;
                return rectRatio > spriteRatio ? 1f - spriteRatio / rectRatio : 0f;
            }
            set
            {
                if ( value == m_CropY ) return;
                m_CropY = Mathf.Clamp01 ( value );
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        public bool mirrorV
        {
            get => m_MirrorVertical;
            set
            {
                if ( value == m_MirrorVertical ) return;
                m_MirrorVertical = value;
                SetVerticesDirty ();
            }
        }

        public float radius
        {
            get => m_Radius;
            set
            {
                m_RelativeRadius = false;
                //if ( value == m_Radius ) return ;
                m_Radius = value;
                SetMaterialDirty ();
            }
        }
        public float radiusRelative
        {
            get => m_RadiusRelative;
            set
            {
                m_RelativeRadius = true;
                //if ( value == m_Radius ) return ;
                m_RadiusRelative = value;
                SetMaterialDirty ();
            }
        }

        public Vector4 radii
        {
            get => new Vector4 ( m_RadiusScale1 , m_RadiusScale2 , m_RadiusScale3 , m_RadiusScale4 );
            set
            {
                m_RadiusScale1 = value [0];
                m_RadiusScale2 = value [1];
                m_RadiusScale3 = value [2];
                m_RadiusScale4 = value [3];
                SetMaterialDirty ();
            }
        }

        public bool recalcLayoutAfterTextureLoad
        {
            get => m_RecalcLayoutAfterTextureLoad ;
            set => m_RecalcLayoutAfterTextureLoad = value ;
        }

        private Texture m_LoadedTexture = null ;
        private void DestroyLoadedTexture ()
        {
            if ( null != m_LoadedTexture )
            {
                // formerly impemented as:
                // (don't have any idea why...)
                //bool needSetNull = m_LoadedTexture == texture ;
                //Utils.Destroy ( m_LoadedTexture );
                //m_LoadedTexture = null;
                //if ( needSetNull )
                //{
                //    texture = null;
                //}

                // new approach, suggested by Google-AI
                // Decouple from the UI first before destroying the underlying memory asset
                if ( texture == m_LoadedTexture ) texture = null ;
                Utils.DestroyObjectAndZero ( ref m_LoadedTexture );
            }
        }

        public LoadState getLoadState ()
        {
            if ( null != m_LoadedTexture ) return LoadState.Loaded;
            if ( isLoading ) return LoadState.Loading;
            return LoadState.NotLoaded;
        }

        public Texture2D loadedTexture => m_LoadedTexture as Texture2D;

        public void clearLoadedTexture ()
        {
            if ( null != m_LoadedTexture )
            {
                DestroyLoadedTexture ();
                texture = null;
            }
        }

        private static readonly string materialName = "Rudi/UI/RoundCorner4" ;

        private Material m_MaterialRoundedCorner = null ;
        private Material matRoundedCorner
        {
            get
            {
                if ( null == m_MaterialRoundedCorner )
                {
                    m_MaterialRoundedCorner = Utils.createMaterial ( materialName );
                    if ( null != m_MaterialRoundedCorner && !CanvasUpdateRegistry.IsRebuildingGraphics () ) SetMaterialDirty ();
                }
                return m_MaterialRoundedCorner;
            }
        }

        public override Material material => matRoundedCorner;

        private static Vector2 getSwapped ( Vector2 v ) => new Vector2 ( v.y , v.x );
        private static Vector2Int getSwapped ( Vector2Int v ) => new Vector2Int ( v.y , v.x );
        private static Vector2 toVector2 ( Vector2Int v ) => new Vector2 ( v.x , v.y );
        private static Rect getWidened ( Rect rect , float widen_by )
        {
            widen_by = Mathf.Max ( widen_by , 0 );
            var v = new Vector2 ( widen_by , widen_by ) ;
            var Result = rect ;
            Result.min -= v;
            Result.max += v;
            return Result;
        }
        private float ppu => null != canvas ? canvas.scaleFactor : 1f;

        private bool hasRoundCorners
        {
            get
            {
                //if ( m_MapCornerAlpha ) return true ;
                if ( m_RelativeRadius )
                {
                    if ( 0 >= m_RadiusRelative ) return false;
                }
                else
                {
                    if ( 0 >= m_Radius ) return false;
                }
                if ( m_RadiusScale1 > 0 ) return true;
                if ( m_RadiusScale2 > 0 ) return true;
                if ( m_RadiusScale3 > 0 ) return true;
                if ( m_RadiusScale4 > 0 ) return true;
                return false;
            }
        }

        private bool hasBorder => m_DrawBorder && m_BorderWidth > 0f;

        private float getRelativeRadius ( float t )
        {
            var s = visibleSize ;
            return Mathf.Min ( s.x , s.y ) * t;
        }

        public float getRadiusToDraw ()
        {
            if ( m_RelativeRadius )
            {
                return getRelativeRadius ( m_RadiusRelative );
            }
            if ( m_LimitToHalfRound )
            {
                var hr = getRelativeRadius ( 0.5f ) ;
                return Mathf.Min ( hr , m_Radius );
            }
            return m_Radius;
        }

        //private const bool m_MyOwnMaterialFilled = false ; // don't remember, why I included this...

        private void updateCornerMaterial ()
        {
            //if ( !m_MyOwnMaterialFilled ) updateCornerMaterial ( matRoundedCorner ); // why..?
            updateCornerMaterial ( materialForRendering );
        }

        private static float getBySigma2 ( float sigma )
        {
            const float minus_log2e_half = -0.72134752044448170367996234050095f ; // ( -log₂ ( e ) / 2 ) = -0.72134752
            return minus_log2e_half / ( sigma * sigma + 0.00001f );
        }

        private static float get2BySigma ( float sigma )
        {
            const float minus_log2e_two = -0.9184481885265703424881975025952f ; // ( -2 * log₂ ( e ) / pi ) = -0.72134752
            var result = minus_log2e_two / ( sigma * sigma + 0.00001f ) ;
            return 4f * result;  // don't know why the 4 is necessary
        }

        private static float getBySigma ( float sigma )
        {
            return 1f / Mathf.Max ( sigma , 0.00001f );
        }

        private static float secureInv ( float v ) => 1.0f / Mathf.Max ( v , 0.00001f );

        private static Vector2 getRotated ( Vector2 v , int r )
        {
            switch ( r & 3 )
            {
                case 0: return v;
                case 1: return new Vector2 ( -v.y , v.x );
                case 2: return -v;
                case 3: return new Vector2 ( v.y , -v.x );
                default: return v;
            }
        }

        private void updateCornerMaterial ( Material mat )
        {
            //Material mat = materialForRendering ;

            const bool UseOptimizedRect = true ;
            var optimizeRect = UseOptimizedRect && !hasBorder &&  hasRoundCorners  ;
            var Round = hasRoundCorners ;
            var HasShadow = hasShadow ;
            mat.EnableKeyword ( "MAP_TEX_ALPHA" , m_MapTexAlpha == AlphaTexMap.MapTexAlpha );
            mat.EnableKeyword ( "MAP_TEX_COLOR_WITH_TEX_ALPHA" , m_MapTexAlpha == AlphaTexMap.MapTexColor );
            mat.EnableKeyword ( "ROUND_CORNERS" , Round );
            mat.EnableKeyword ( "PIXEL_CORRECT" , m_PixelCorrect && !hasEventuallyShadow );
            mat.EnableKeyword ( "MAP_CORNER_ALPHA" , m_MapCornerAlpha );
            mat.EnableKeyword ( "DRAW_BORDER" , hasBorder );
            mat.EnableKeyword ( "DRAW_BORDER_FADED" , hasBorder && m_FadeBorder );
            mat.EnableKeyword ( "DROP_SHADOW" , HasShadow );

            if ( m_PixelCorrect )
            {
                float sw = Screen.width ;
                float sh = Screen.height ;
                var ss = new Vector4 ( sw * 0.5f , sh * 0.5f , 2f/sw , 2f/sh ) ;
                //Log.i ( TAG , "ss = " + ss ) ;
                mat.SetVector ( "_HalfScreenParams" , ss ); // mat m_MaterialRoundedCorner ????????????????????????????????????????
            }
            var Size_unrotated = isSwapped ? getSwapped ( rectImage.size ) : rectImage.size ;
            //Vector2 size = rect.size ;
            if ( !Size_unrotated.hasArea () ) return; // size = Vector2.one;
                                                      //Log.i ( TAG , gameObject , "size = " + size ) ;
            var Ppu = ppu ;
            //log ( $"ppu = { Ppu }" ) ;
            var fBorderWidth = m_BorderWidth * Ppu ;
            if ( m_PixelCorrect && !m_FadeBorder ) fBorderWidth = Mathf.Round ( fBorderWidth );
            //float Smoothing = m_Smoothing ;
            var HalfSmoothing = m_Smoothing * 0.5f ;
            var InvSmoothing = secureInv ( m_Smoothing ) ; // 1.0f / Mathf.Max ( m_Smoothing , 0.0001f ) ;
            //Vector2 HalfSmoothing2 = Vector2.one * HalfSmoothing;
            //Vector2 Smoothing2 = Vector2.one * m_Smoothing;
            var ImageSizeInPixels_unrotated = Size_unrotated * Ppu ;
            var DrawingRectSize_unrotated = ImageSizeInPixels_unrotated ;
            if ( optimizeRect )
            {
                DrawingRectSize_unrotated = Vector2.Max ( DrawingRectSize_unrotated - Vector2.one * m_Smoothing * 1.0f , Vector2.zero );
            }
            var HalfSize = Mathf.Min ( ImageSizeInPixels_unrotated.x , ImageSizeInPixels_unrotated.y ) * 0.5f ;
            var Smoothing = optimizeRect ? m_Smoothing : HalfSmoothing ;
            var rectangle = new Vector4 ( HalfSize , - Smoothing * InvSmoothing , - InvSmoothing , HalfSmoothing ) ;

            //Log.i ( TAG , "HalfSize = " + HalfSize ) ;

            mat.SetVector ( "_Rectangle" , rectangle );
            {
                // scale
                var offset = uvOffset ;
                if ( isSwapped ) offset = offset.getSwapped ();
                var scaleX = ImageSizeInPixels_unrotated.x / ( 1f - offset.x * 2f ) ;
                var scaleY = ImageSizeInPixels_unrotated.y / ( 1f - offset.y * 2f ) ;
                var offsetX = offset.x * scaleX ;
                var offsetY = offset.y * scaleY ;
                var scale = new Vector4 ( offsetX , offsetY , scaleX , scaleY ) ;
                mat.SetVector ( "_Scale" , scale );
            }

            {
                // light
                var LightDir4 = shadowProperties.lightDir4 ;
                //Log.i ( TAG , "LightDir4 = " + LightDir4 );
                mat.SetVector ( "_LightDir4" , LightDir4 );
            }


            {
                // _AlphaMap
                var TexAlphaMul = m_MapTexAlpha == AlphaTexMap.MapTexAlpha ? m_TexAlpha1 - m_TexAlpha0 : 1 ;
                var TexAlphaAdd = m_MapTexAlpha == AlphaTexMap.MapTexAlpha ? m_TexAlpha0 : 0 ;
                var CornerAlphaMul = m_MapCornerAlpha ? m_CornerAlpha1 - m_CornerAlpha0 : 1 ;
                var CornerAlphaAdd = m_MapCornerAlpha ?  m_CornerAlpha0 : 0 ;
                var _AlphaMap = new Vector4 ( TexAlphaMul , TexAlphaAdd , CornerAlphaMul , CornerAlphaAdd ) ;
                mat.SetVector ( "_AlphaMap" , _AlphaMap );
                mat.SetColor ( "_ColorAlpha0" , m_ColorAlpha0 );
                mat.SetColor ( "_ColorAlpha1" , m_ColorAlpha1 );
            }

            if ( Round || hasBorder || HasShadow )
            {
                var radius = getRadiusToDraw () ;
                var radiusPixels4 = Vector4.one * radius * Ppu ;
                var RadiusScale = new Vector4 ( m_RadiusScale1 , m_RadiusScale2 , m_RadiusScale3 , m_RadiusScale4  ) ;
                radiusPixels4 = Vector4.Scale ( radiusPixels4 , RadiusScale );
                //if ( m_PixelCorrect ) radiusPixels4 = rounded ( radiusPixels4 * 2f ) * 0.5f ;
                if ( m_PixelCorrect ) radiusPixels4 = ( radiusPixels4 * 2f ).getRounded () * 0.5f;
                // new since using sdf
                //Vector4 rectangle = new Vector4 ( SizePixel.x , SizePixel.y , 1f / Smoothing , m_Smoothing * 0.5f ) ;
                var CenterX = new Vector4 ( - ImageSizeInPixels_unrotated.x * 0.5f , - ImageSizeInPixels_unrotated.y * 0.5f , DrawingRectSize_unrotated.x * 0.5f , DrawingRectSize_unrotated.y * 0.5f ) ;
                var ZeroOffset = 0.0f ;
                if ( optimizeRect )
                {
                    radiusPixels4 = Vector4.Max ( radiusPixels4 - Vector4.one * HalfSmoothing , Vector4.zero );
                    ZeroOffset = HalfSmoothing;
                }
                if ( Round && !hasBorder && !hasEventuallyShadow )
                {
                    if ( radiusPixels4.x == 0.0f && radiusPixels4.w == 0.0f )
                    {
                        // top round
                        CenterX.y = -ZeroOffset; // offset y -> 0
                        CenterX.w = DrawingRectSize_unrotated.y;
                    }
                    else if ( radiusPixels4.y == 0.0f && radiusPixels4.z == 0.0f )
                    {
                        // bottom round
                        CenterX.y = ZeroOffset - ImageSizeInPixels_unrotated.y; // offset y -> full
                        CenterX.w = DrawingRectSize_unrotated.y;
                    }
                    else if ( radiusPixels4.x == 0.0f && radiusPixels4.y == 0.0f )
                    {
                        // right round
                        CenterX.x = -ZeroOffset; // offset x -> 0
                        CenterX.z = DrawingRectSize_unrotated.x;
                    }
                    else if ( radiusPixels4.z == 0.0f && radiusPixels4.w == 0.0f )
                    {
                        // left round
                        CenterX.x = ZeroOffset - ImageSizeInPixels_unrotated.x; // offset x -> full
                        CenterX.z = DrawingRectSize_unrotated.x;
                    }
                }
                //radiusPixels4 = Vector4.Max ( radiusPixels4 , new Vector4 ( 0.001f , 0.001f , 0.001f , 0.001f ) ) ;
                mat.SetVector ( "_Radius" , radiusPixels4 );
                mat.SetVector ( "_CenterX" , CenterX );
                //mat.SetVector ( "_CenterY" , CenterY ) ;
            }
            if ( HasShadow )
            {
                var ShadowAlpha = shadowAlpha ;
                var Sigma = pixel_sigma ;
                //Log.i ( TAG , "Sigma = " + Sigma ) ;
                var ShadowBySqrSigma = getBySigma ( Sigma ) ;
                //Log.i ( TAG , "ShadowBySqrSigma = " + ShadowBySqrSigma );
                var ShadowOffset = getRotated ( shadowOffset , - m_Rotation ) ;
                //Log.i ( TAG , "ShadowOffset = " + ShadowOffset );
                //Log.i ( TAG , $"ShadowOffset = { ShadowOffset }" ) ;
                var ShadowOffsetLength = ShadowOffset.magnitude ;

                var SignElevation = Mathf.Sign ( multipliedElevation ) ;
                //Log.i ( TAG , "SignElevation = " + SignElevation );
                //float SignElevation = 1f ;

                //Vector4 DropShadow = new Vector4 ( ShadowAlpha , Sigma , ShadowBySqrSigma , ShadowOffsetLength ) ;
                var DropShadow = new Vector4 ( ShadowAlpha , Sigma , ShadowBySqrSigma , SignElevation ) ;
                mat.SetVector ( "_DropShadow" , DropShadow );
                var ShadowOffset4 = new Vector4 ( -ShadowOffset.x , -ShadowOffset.y , 0 , 0 ) ;
                mat.SetVector ( "_ShadowDir" , ShadowOffset4 );
            }

            if ( hasBorder )
            {
                //float borderFade = m_FadeBorder ? 1f / fBorderWidth : 1f ;
                //float BorderInside = m_FadeBorder ? -fBorderWidth : -fBorderWidth - Smoothing ;
                //float BorderOutside = m_FadeBorder ? 0.0f : -fBorderWidth + Smoothing ;
                //Vector4 BorderWidth = new Vector4 ( fBorderWidth , BorderInside , BorderOutside , 0f ) ;

                var Width = m_FadeBorder ? fBorderWidth : m_Smoothing ;
                var ByWidth = 1.0f / Mathf.Max ( Width , 0.00001f ) ;
                var BorderLo = m_FadeBorder ? -fBorderWidth : -fBorderWidth - HalfSmoothing ;
                var BorderLoByWidth = BorderLo * ByWidth ;
                //Vector4 BorderWidth = new Vector4 ( BorderLoByWidth , BorderLo , Width , ByWidth ) ;
                var BorderWidth = new Vector4 ( BorderLoByWidth , BorderLo , fBorderWidth , ByWidth ) ;
                mat.SetVector ( "_BorderWidth" , BorderWidth );
                mat.SetColor ( "_BorderColor" , m_BorderColor );
                mat.SetColor ( "_FillColor" , m_FillColor );
            }

            mat.SetFloat ( "_InvertTexAlpha" , m_MapTexAlpha == AlphaTexMap.MapTexAlpha ? 1 : 0 );
        }

        protected override void UpdateMaterial ()
        {
            base.UpdateMaterial ();
            updateCornerMaterial ();
        }

        private void OnTextureLoad_Finished ( Texture2D tex )
        {
            if ( null == this ) // already destroyed?
            {
                Utils.DestroyObject ( tex );
                return;
            }
            m_LoadingUrl = null;
            m_TextureLoader = null;
            if ( null == tex ) return;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.filterMode = FilterMode.Bilinear;
            texture = tex;
            if ( m_RecalcLayoutAfterTextureLoad ) SetLayoutDirty ();
            DestroyLoadedTexture ();
            m_LoadedTexture = tex;
            m_OnTextureLoaded.Invoke ();
        }

        private string m_LoadingUrl = null ;
        private TextureLoader m_TextureLoader = null ;
        bool isLoading => m_TextureLoader?.isLoading ?? false;

        [System.Obsolete ( "This will be deprecated. Please use loadFromFile instead" , false )]
        public bool load ( string filepath ) => loadFromFile ( filepath );

        public bool loadFromFile ( string filepath )
        {
            if ( !File.Exists ( filepath ) ) return false;
            return loadFromWeb ( TextureLoader.getUrlFromFilePath ( filepath ) );
        }

        public bool loadFromWeb ( string url )
        {
            if ( isLoading ) return false;
            if ( string.IsNullOrEmpty ( url ) ) return false;
            m_LoadingUrl = url;
            if ( !m_Started ) return true;
            return startOutstandingTextureLoad ();
        }
        //                 Log.i ( TAG , $"OnDestroy ABORT on Object: {gameObject.name} (ID: {gameObject.GetInstanceID ()})" );

        private bool startOutstandingTextureLoad ()
        {
            if ( string.IsNullOrEmpty ( m_LoadingUrl ) ) return false;
            //Log.i ( TAG , $"startOutstandingTextureLoad () on Object: {gameObject.name} (ID: {gameObject.GetInstanceID ()} - url = " + m_LoadingUrl ) ;
            return null != ( m_TextureLoader = TextureLoader.Load ( m_LoadingUrl , OnTextureLoad_Finished ) );
        }

        [System.Obsolete ( "This will be deprecated. Please use loadFromFileIfNotLoaded instead" , false )]
        public void loadIfNotLoaded ( string filepath ) => loadFromFileIfNotLoaded ( filepath );

        public void loadFromFileIfNotLoaded ( string filepath )
        {
            if ( getLoadState () == LoadState.NotLoaded ) loadFromFile ( filepath );
        }

        private bool isSwapped => 0 != ( m_Rotation & 1 );

        private float pixelCorrected ( float length )
        {
            if ( null == canvas ) return length;
            var ppu = canvas.scaleFactor ;
            return Mathf.Round ( length * ppu ) / ppu;
        }

        public override Texture mainTexture
        {
            get
            {
                if ( m_DrawTextureOnly ) return texture;
                if ( texture == null )
                {
                    return s_WhiteTexture;
                }
                return texture;
            }
        }

        private void shrinkToPreserveSpriteAspectRatio ( ref Rect rect , Vector2 textureSizeRotated )
        {
            if ( null == texture ) return;
            var spriteRatio = textureSizeRotated.x / textureSizeRotated.y ;
            var rectRatio = rect.width / rect.height ;

            if ( spriteRatio > rectRatio )
            {
                var oldHeight = rect.height ;
                rect.height = rect.width * ( 1.0f / spriteRatio );
                rect.y += ( oldHeight - rect.height ) * rectTransform.pivot.y;
            }
            else
            {
                var oldWidth = rect.width ;
                rect.width = rect.height * spriteRatio;
                rect.x += ( oldWidth - rect.width ) * rectTransform.pivot.x;
            }
        }

        private Vector2 visibleSize
        {
            get
            {
                var r = GetPixelAdjustedRect () ;
                if ( shrinkToFit ) shrinkToPreserveSpriteAspectRatio ( ref r , numPixels_rotated_cropped );
                pixelCorrect ( ref r );
                return r.size;
            }
        }

        private void pixelCorrect ( ref Rect rect )
        {
            rect.width = pixelCorrected ( rect.width );
            rect.height = pixelCorrected ( rect.height );
        }

        private Rect rectImage
        {
            get
            {
                var Result = rectTransform.rect ;
                if ( shrinkToFit )
                {
                    shrinkToPreserveSpriteAspectRatio ( ref Result , numPixels_rotated_cropped );
                }
                pixelCorrect ( ref Result );
                return Result;
            }
        }


        private static readonly Vector2 [] uv_standard  = { Vector2.zero , Vector2.up , Vector2.one , Vector2.right } ; // 00 , 01 , 11 , 10
        private static readonly Vector2 [] uv_Vmirrored = { Vector2.up , Vector2.zero , Vector2.right , Vector2.one } ; // 01 , 00 , 10 , 11

        private Vector2Int getSwapped_if_swapped ( Vector2Int v ) => isSwapped ? getSwapped ( v ) : v;
        private Vector2 getSwapped_if_swapped ( Vector2 v ) => isSwapped ? getSwapped ( v ) : v;

        private int numPixelsX_raw => null != texture ? texture.width : 0;
        private int numPixelsY_raw => null != texture ? texture.height : 0;
        private Vector2Int numPixels_raw => null != texture ? new Vector2Int ( texture.width , texture.height ) : Vector2Int.zero;
        private Vector2Int numPixels_rotated => getSwapped_if_swapped ( numPixels_raw );

        private int numPixelsX_rotated => isSwapped ? numPixelsY_raw : numPixelsX_raw;
        private int numPixelsY_rotated => isSwapped ? numPixelsX_raw : numPixelsY_raw;

        private int numPixelsX_rotated_cropped
        {
            get
            {
                if ( null == texture ) return 0;
                var Result = numPixelsX_rotated >> 1 ;
                var r = Mathf.Lerp ( Result , 1 , cropX ) ;
                return Mathf.RoundToInt ( r ) * 2;
            }
        }

        private int numPixelsY_rotated_cropped
        {
            get
            {
                if ( null == texture ) return 0;
                var Result = numPixelsY_rotated >> 1 ;
                var r = Mathf.Lerp ( Result , 1 , cropY ) ;
                return Mathf.RoundToInt ( r ) * 2;
            }
        }

        private Vector2Int numPixels_rotated_cropped
        {
            get
            {
                if ( null == texture ) return Vector2Int.zero;
                var np = numPixels_rotated ;
                var rx = np.x >> 1 ;
                var ry = np.y >> 1 ;
                var resx = Mathf.Lerp ( rx , 1 , cropX ) ;
                var resy = Mathf.Lerp ( ry , 1 , cropY ) ;
                var ix = Mathf.RoundToInt ( resx ) * 2 ;
                var iy = Mathf.RoundToInt ( resy ) * 2 ;
                return new Vector2Int ( ix , iy );
            }
        }

        //public Vector2Int textureSize_rotated_cropped => new Vector2Int ( numPixelsX_rotated_cropped , numPixelsY_rotated_cropped );
        public Vector2Int textureSize_rotated_cropped => numPixels_rotated_cropped;
        private Vector2Int numPixels_rotated_cropped_unrotated => isSwapped ? new Vector2Int ( numPixelsY_rotated_cropped , numPixelsX_rotated_cropped ) : new Vector2Int ( numPixelsX_rotated_cropped , numPixelsY_rotated_cropped );

        private Vector2 uvWidth
        {
            get
            {
                if ( null == texture ) return Vector2.one;
                return toVector2 ( numPixels_rotated_cropped ) / toVector2 ( numPixels_rotated );
            }
        }

        private Vector2 uvOffset
        {
            get
            {
                if ( null == texture ) return Vector2.zero;
                return getUVOffset_from_width ( uvWidth );
            }
        }

        private static float getUVWidth_from_offset ( float offset ) => 1f - 2f * offset;
        private static Vector2 getUVWidth_from_offset ( Vector2 offset ) => Vector2.one - offset * 2f;
        private static Vector2 getUVOffset_from_width ( Vector2 width ) => ( Vector2.one - width ) * 0.5f;

        private Vector2 getUV ( Vector2 txy )
        {
            var width = uvWidth ;
            var offset = getUVOffset_from_width ( width ) ;
            return getSwapped_if_swapped ( getSwapped_if_swapped ( txy ) * width + offset );
        }

        private Vector2 [ ] uv_array => m_MirrorVertical ? uv_Vmirrored : uv_standard;

        private Vector2 getUV ( int id )
        {
            var uv_raw = uv_array [  id + m_Rotation  & 3 ] ;
            return getUV ( uv_raw );
        }

        private void getVertices ( VertexHelper vh , bool preserveAspect )
        {
            var rect = GetPixelAdjustedRect () ;
            if ( preserveAspect ) shrinkToPreserveSpriteAspectRatio ( ref rect , numPixels_rotated_cropped );
            pixelCorrect ( ref rect );
            {
                Color32 color_a = color ;
                var color_b = color_a ;
                var color_c = color_a ;
                var color_d = color_a ;

                switch ( m_Gradient )
                {
                    case Gradient.Right: color_c = GetGradientColor (); color_d = color_c; break;
                    case Gradient.Down: color_a = GetGradientColor (); color_d = color_a; break;
                }

                var Mapper = new VertexUVMapper ( rect , getUV ( 0 ) , getUV ( 2 ) , isSwapped ) ;

                if ( hasShadow ) Mapper.setRect ( shadowProperties.getWidened ( rect ) );

                Mapper.addVertex ( vh , 0 , color_a );
                Mapper.addVertex ( vh , 1 , color_b );
                Mapper.addVertex ( vh , 2 , color_c );
                Mapper.addVertex ( vh , 3 , color_d );

                vh.AddTriangle ( 0 , 1 , 2 );
                vh.AddTriangle ( 2 , 3 , 0 );
            }
        }

        private static VertexHelper m_VertexHelper = new () ;
        private static Mesh m_Mesh = null ;
        private static Mesh myMesh
        {
            get
            {
                if ( null == m_Mesh )
                {
                    m_Mesh = new ();
                }
                return m_Mesh;
            }
        }


        private Mesh getMesh ()
        {
            m_VertexHelper.Clear ();
            if ( mainTexture != null )
            {
                getVertices ( m_VertexHelper , false );
            }
            m_VertexHelper.FillMesh ( myMesh );
            return myMesh;
        }

        protected override void OnPopulateMesh ( VertexHelper vh )
        {
            vh.Clear ();
            if ( mainTexture != null )
            {
                getVertices ( vh , shrinkToFit );
            }
        }

        protected override void OnRectTransformDimensionsChange ()
        {
            base.OnRectTransformDimensionsChange ();
            SetMaterialDirty ();
        }

        private static void LogMesh ( Mesh mesh )
        {
            var vertices  = mesh.vertices  ;
            //var indices   = mesh.triangles ;
            //var colors    = mesh.colors32  ;
            var texcoords = mesh.uv        ;
            int num = vertices.Length ;
            for ( int i = 0 ; i < num ; i++ )
            {
                Log.i ( TAG , $"vert [ {i} ] : { vertices[i] } , uv : { texcoords[i] }" ) ;
            }
        }

        public Texture2D getImageAsTexture ( bool native = true )
        {
            var mesh = getMesh () ;
            if ( !mesh ) return null ;
            Material mat = new Material ( material ) ;
            mat.mainTexture = mainTexture ;
            if ( native )
            {
                mat.EnableKeyword ( "ROUND_CORNERS"    , false ) ;
                mat.EnableKeyword ( "MAP_CORNER_ALPHA" , false ) ;
                mat.EnableKeyword ( "DRAW_BORDER"      , false ) ;
            }

            var dstSize = numPixels_rotated_cropped ;
            if ( !dstSize.hasArea () )
            {
                log ( "bad size: " + dstSize ) ;
                return null;
            }
            var ModelMatrix = GraphicRenderTools.getModelMatrix ( rectTransform.rect ) ;
#if UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
             ModelMatrix.m11 *= -1f ;
             ModelMatrix.m13 = 1f - ModelMatrix.m13 ;
#endif
            RenderTexture renderTexture = RenderTexture.GetTemporary ( dstSize.x , dstSize.y , 0 , RenderTextureFormat.ARGB32 ) ;
            var rt = GraphicRenderTools.DrawMeshToTexture ( renderTexture , mesh , ModelMatrix , mat ) ;

            Texture2D Result = null ;
            if ( rt != null ) Result = GraphicRenderTools.ToTexture ( rt ) ;
            RenderTexture.ReleaseTemporary ( renderTexture ) ;
            Utils.DestroyObject ( mat ) ;
            return Result ;
        }


        protected override void OnDestroy ()
        {
            //if ( m_TextureLoader != null )
            //{
            //    //Log.i ( TAG , $"OnDestroy ABORT on Object: {gameObject.name} (ID: {gameObject.GetInstanceID ()})" );
            //    m_TextureLoader.abort ();
            //}

            m_TextureLoader?.abort ();
            DestroyLoadedTexture ();
            Utils.DestroyObjectAndZero ( ref m_MaterialRoundedCorner );
            base.OnDestroy ();
        }

        private bool m_Started = false ;

        protected override void OnEnable ()
        {
            base.OnEnable ();
            if ( m_Started )
            {
                m_DropShadow.OnEnable () ;
            }
        }

        protected override void OnDisable ()
        {
            base.OnDisable () ;
            m_DropShadow.OnDisable () ;
        }

        protected override void Start ()
        {
            base.Start ();
            m_Started = true;
            startOutstandingTextureLoad ();
            m_DropShadow.canSink = true;
            m_DropShadow.setObject ( this );
            m_DropShadow.OnEnable ();
        }

        public MonoBehaviour getMonoObject () => this ;

        public void propertiesObjectChanged ( DropShadow.MessageReason reason )
        {
            if ( DropShadow.MessageReason.ObjectChanged == reason )
            {
                m_DropShadow.resetPropertiesObject ();
            }
            shadowChanged ();
        }

#if UNITY_EDITOR
        protected override void OnValidate ()
        {
            base.OnValidate ();
            m_DropShadow.canSink = true;
            m_DropShadow.setObject ( this );
            m_DropShadow.updated ();
            if ( null != m_MaterialRoundedCorner && !material.shader.name.Equals ( materialName ) )
            {
                Utils.DestroyObjectAndZero ( ref m_MaterialRoundedCorner );
            }
        }
        [ContextMenu ( "Reset Shadow Properties" )]
        public void ResetShadowProperties ()
        {
            m_DropShadow.resetProperties ();
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor ( typeof ( RawImageV2 ) )]
    [CanEditMultipleObjects]
    public class RawImageV2Editor : UnityEditor.UI.GraphicEditor
    {
        SerializedProperty m_Texture ;
        SerializedProperty m_DrawTextureOnly ;
        SerializedProperty m_OnTextureLoaded ;
        SerializedProperty m_RecalcLayoutAfterTextureLoad ;

        SerializedProperty m_Gradient ;
        SerializedProperty m_GradientLightning ;
        SerializedProperty m_GradeColor ;
        SerializedProperty m_Saturating ;
        SerializedProperty m_Lightning ;

        SerializedProperty m_FitMode ;
        //SerializedProperty m_PreserveAspectRatio ;
        //SerializedProperty m_Fill ;
        SerializedProperty m_PixelCorrect ;
        SerializedProperty m_MirrorVertical ;
        SerializedProperty m_MapTexAlpha ;
        SerializedProperty m_TexAlpha0 ;
        SerializedProperty m_TexAlpha1 ;
        SerializedProperty m_ColorAlpha0 ;
        SerializedProperty m_ColorAlpha1 ;


        SerializedProperty m_Rotation ;
        SerializedProperty m_CropX ;
        SerializedProperty m_CropY ;

        SerializedProperty m_DrawBorder ;
        SerializedProperty m_BorderWidth ;
        SerializedProperty m_BorderColor ;
        SerializedProperty m_FillColor ;
        SerializedProperty m_FadeBorder ;

        SerializedProperty m_RelativeRadius ;
        SerializedProperty m_Radius ;
        SerializedProperty m_RadiusRelative ;
        SerializedProperty m_LimitToHalfRound ;
        SerializedProperty m_RadiusScale1 ;
        SerializedProperty m_RadiusScale2 ;
        SerializedProperty m_RadiusScale3 ;
        SerializedProperty m_RadiusScale4 ;
        SerializedProperty m_Smoothing ;


        SerializedProperty m_MapCornerAlpha ;
        SerializedProperty m_CornerAlpha0 ;
        SerializedProperty m_CornerAlpha1 ;

        SerializedProperty m_DropShadow ;

        protected override void OnEnable ()
        {
            base.OnEnable ();
            m_Texture = serializedObject.FindProperty ( "m_Texture" );
            m_DrawTextureOnly = serializedObject.FindProperty ( "m_DrawTextureOnly" );
            m_OnTextureLoaded = serializedObject.FindProperty ( "m_OnTextureLoaded" );
            m_RecalcLayoutAfterTextureLoad = serializedObject.FindProperty ( "m_RecalcLayoutAfterTextureLoad" );

            m_Gradient = serializedObject.FindProperty ( "m_Gradient" );
            m_GradientLightning = serializedObject.FindProperty ( "m_GradientLightning" );
            m_GradeColor = serializedObject.FindProperty ( "m_GradeColor" );
            m_Saturating = serializedObject.FindProperty ( "m_Saturating" );
            m_Lightning = serializedObject.FindProperty ( "m_Lightning" );


            m_FitMode = serializedObject.FindProperty ( "m_FitMode" );
            //m_PreserveAspectRatio = serializedObject.FindProperty ( "m_PreserveAspectRatio" );
            //m_Fill = serializedObject.FindProperty ( "m_Fill" );
            m_PixelCorrect = serializedObject.FindProperty ( "m_PixelCorrect" );
            m_MirrorVertical = serializedObject.FindProperty ( "m_MirrorVertical" );
            m_MapTexAlpha = serializedObject.FindProperty ( "m_MapTexAlpha" );
            m_TexAlpha0 = serializedObject.FindProperty ( "m_TexAlpha0" );
            m_TexAlpha1 = serializedObject.FindProperty ( "m_TexAlpha1" );
            m_ColorAlpha0 = serializedObject.FindProperty ( "m_ColorAlpha0" );
            m_ColorAlpha1 = serializedObject.FindProperty ( "m_ColorAlpha1" );

            m_Rotation = serializedObject.FindProperty ( "m_Rotation" );
            m_CropX = serializedObject.FindProperty ( "m_CropX" );
            m_CropY = serializedObject.FindProperty ( "m_CropY" );

            m_DrawBorder = serializedObject.FindProperty ( "m_DrawBorder" );
            m_BorderWidth = serializedObject.FindProperty ( "m_BorderWidth" );
            m_BorderColor = serializedObject.FindProperty ( "m_BorderColor" );
            m_FillColor = serializedObject.FindProperty ( "m_FillColor" );
            m_FadeBorder = serializedObject.FindProperty ( "m_FadeBorder" );

            m_RelativeRadius = serializedObject.FindProperty ( "m_RelativeRadius" );
            m_Radius = serializedObject.FindProperty ( "m_Radius" );
            m_RadiusRelative = serializedObject.FindProperty ( "m_RadiusRelative" );
            m_LimitToHalfRound = serializedObject.FindProperty ( "m_LimitToHalfRound" );
            m_RadiusScale1 = serializedObject.FindProperty ( "m_RadiusScale1" );
            m_RadiusScale2 = serializedObject.FindProperty ( "m_RadiusScale2" );
            m_RadiusScale3 = serializedObject.FindProperty ( "m_RadiusScale3" );
            m_RadiusScale4 = serializedObject.FindProperty ( "m_RadiusScale4" );
            m_Smoothing = serializedObject.FindProperty ( "m_Smoothing" );


            m_MapCornerAlpha = serializedObject.FindProperty ( "m_MapCornerAlpha" );
            m_CornerAlpha0 = serializedObject.FindProperty ( "m_CornerAlpha0" );
            m_CornerAlpha1 = serializedObject.FindProperty ( "m_CornerAlpha1" );
            //m_TintAlpha = serializedObject.FindProperty ( "m_TintAlpha" );

            //if ( RawImageV2.UseShaderV4 )
            {
                m_DropShadow = serializedObject.FindProperty ( "m_DropShadow" );
            }

        }
        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            EditorGUILayout.PropertyField ( m_DrawTextureOnly );
            EditorGUILayout.PropertyField ( m_Texture );
            EditorGUILayout.PropertyField ( m_RecalcLayoutAfterTextureLoad );
            EditorGUILayout.PropertyField ( m_OnTextureLoaded );
            EditorGUILayout.PropertyField ( m_Color );

            EditorGUILayout.PropertyField ( m_Gradient );
            if ( m_Gradient.intValue != ( int ) RawImageV2.Gradient.None )
            {
                EditorGUILayout.PropertyField ( m_GradientLightning );
                if ( m_GradientLightning.boolValue )
                {
                    EditorGUILayout.PropertyField ( m_Saturating );
                    EditorGUILayout.PropertyField ( m_Lightning );
                }
                else
                {
                    EditorGUILayout.PropertyField ( m_GradeColor );
                }
            }

            EditorGUILayout.Space ();

            EditorGUILayout.PropertyField ( m_FitMode );
            //EditorGUILayout.PropertyField ( m_PreserveAspectRatio ) ;
            //         if ( m_PreserveAspectRatio.boolValue )
            //         {
            //             EditorGUILayout.PropertyField ( m_Fill ) ;
            //         }

            EditorGUILayout.PropertyField ( m_PixelCorrect );
            EditorGUILayout.PropertyField ( m_MirrorVertical );
            EditorGUILayout.PropertyField ( m_MapTexAlpha );
            if ( m_MapTexAlpha.intValue == ( int ) RawImageV2.AlphaTexMap.MapTexAlpha )
            {
                EditorGUILayout.PropertyField ( m_TexAlpha0 );
                EditorGUILayout.PropertyField ( m_TexAlpha1 );
            }
            else if ( m_MapTexAlpha.intValue == ( int ) RawImageV2.AlphaTexMap.MapTexColor )
            {
                EditorGUILayout.PropertyField ( m_ColorAlpha0 );
                EditorGUILayout.PropertyField ( m_ColorAlpha1 );
            }

            EditorGUILayout.Space ();
            EditorGUILayout.PropertyField ( m_Rotation );

            if ( m_FitMode.intValue != ( int ) RawImageV2.FitMode.CropToFit )
            {
                EditorGUILayout.PropertyField ( m_CropX );
                EditorGUILayout.PropertyField ( m_CropY );
            }

            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ( "Border" , EditorStyles.boldLabel );
            EditorGUILayout.PropertyField ( m_DrawBorder );
            if ( m_DrawBorder.boolValue )
            {
                EditorGUILayout.PropertyField ( m_BorderWidth );
                EditorGUILayout.PropertyField ( m_BorderColor );
                EditorGUILayout.PropertyField ( m_FillColor );
                EditorGUILayout.PropertyField ( m_FadeBorder );
            }

            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ( "Rounded Corners" , EditorStyles.boldLabel );
            EditorGUILayout.PropertyField ( m_RelativeRadius );

            var IsRound = false ;
            if ( !m_RelativeRadius.boolValue )
            {
                EditorGUILayout.PropertyField ( m_Radius );
                EditorGUILayout.PropertyField ( m_LimitToHalfRound );
                if ( m_Radius.floatValue > 0f ) IsRound = true;
            }
            else
            {
                EditorGUILayout.PropertyField ( m_RadiusRelative );
                if ( m_RadiusRelative.floatValue > 0f ) IsRound = true;
            }

            //EditorGUILayout.PropertyField ( m_Radius ) ;
            //EditorGUILayout.PropertyField ( m_LimitToHalfRound ) ;
            if ( IsRound )
            {
                EditorGUILayout.Space ();
                EditorGUILayout.PropertyField ( m_RadiusScale1 );
                EditorGUILayout.PropertyField ( m_RadiusScale2 );
                EditorGUILayout.PropertyField ( m_RadiusScale3 );
                EditorGUILayout.PropertyField ( m_RadiusScale4 );
                EditorGUILayout.Space ();
                EditorGUILayout.PropertyField ( m_Smoothing );

                EditorGUILayout.PropertyField ( m_MapCornerAlpha );
                if ( m_MapCornerAlpha.boolValue )
                {
                    EditorGUILayout.PropertyField ( m_CornerAlpha0 );
                    EditorGUILayout.PropertyField ( m_CornerAlpha1 );
                }

            }

            // drop shadow
            //if ( RawImageV2.UseShaderV4 )
            {
                EditorGUILayout.Space ();
                EditorGUILayout.LabelField ( "Drop Shadow" , EditorStyles.boldLabel );
                EditorGUILayout.PropertyField ( m_DropShadow );
            }


            //EditorGUILayout.PropertyField ( m_TintAlpha ) ;
            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ( "Raycast Controls" , EditorStyles.boldLabel );
            RaycastControlsGUI ();
            MaskableControlsGUI ();
            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif

}
