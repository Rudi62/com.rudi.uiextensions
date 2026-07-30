using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEditor;
using System.ComponentModel;
using System.Reflection;

using System;
using Rudi.Extensions;
using Rudi.Core;

namespace Rudi.UI
{
    [RequireComponent ( typeof ( CanvasRenderer ) )]
    [AddComponentMenu ( "Rudi/UI/Glass/Glass Background" , 12 )]

    public class GlassBackground : Graphic
    {
        private static readonly string TAG = "Rudis GlassBackground" ;
        private static readonly Color StdReflexionColor = new Color ( 0.6f , 0.6f , 0.6f , 1f ) ;
        public enum BorderProfile
        {
            [ Description ( "BP_ROUND" ) ] Round,
            [ Description ( "BP_BULGE" ) ] Bulge,
            //[ Description ( "BP_BEVEL" ) ] Bevel ,
        }
        // BEVELED edge
        public enum BorderWidthSrc
        {
            RelativeToObjectSize,
            RelativeToRadius,
            Independent,
        }

        //[ SerializeField ] private bool m_Show = true ;
        [ Range ( 0 , 360 ) ]
        [ SerializeField ] private float m_Rotation = 0f ;

        [ SerializeField ] private bool m_Layerable = true ;
        [ SerializeField ] private bool m_NoBlur = false ;

        [ SerializeField ] private bool m_LinearSampling = true ;
        [ Range ( 0 , 32 ) ]
        [ SerializeField ] private float m_Sigma = 2 ;
        [ Range ( 1 , 20 ) ]
        [ SerializeField ] private int m_Step = 1 ;
        [ Range ( 0 , 150 ) ]
        [ Tooltip ( "100 is neutral" ) ]
        [ SerializeField ] private int m_Contrast = 100 ;
        [ Range ( -100 , 100 ) ]
        [ Tooltip ( "0 is neutral" ) ]
        [ SerializeField ] private int m_Brightness = 0 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_Clamp = 1 ;
        [ Range ( 0 , 0.2f ) ]
        [ SerializeField ] private float m_Compress = 0.1f ;

        // round corners

        [ SerializeField ] bool m_RelativeRadius = false ;
        [ Range ( 0 , 50 ) ]
        [ SerializeField ] float m_Radius = 15 ;
        [ Range ( 0 , 0.5f ) ]
        [ SerializeField ] float m_RadiusRelative = 0.5f ;
        [ SerializeField ] bool m_LimitToHalfRound = true ;
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

        // Border
        [ SerializeField ] private BorderWidthSrc m_BorderWidthSource = BorderWidthSrc.RelativeToRadius ;
        [ Range ( 0 , 0.5f ) ] [ SerializeField ] float m_BorderWidthObjectRel = 0.3f ;
        [ Range ( 0 , 1f ) ] [ SerializeField ] float m_BorderWidthRadiusRel = 1f ;
        [ Range ( 0 , 50 ) ]
        [ SerializeField ] float m_BorderWidth = 10 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_RefractivePower = 0.5f ;
        [ SerializeField ] BorderProfile m_BorderProfile = BorderProfile.Bulge ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_LiftUp = 0f ;

        // Reflexion
        [ SerializeField ] private bool m_DrawReflection = true ;
        [ Range ( 0 , 2 ) ]
        [ SerializeField ] float m_ReflexionSharpness = 0.6f ;
        [ Range ( 0 , 90 ) ]
        [ SerializeField ] float m_LightAngleVert = 9 ;
        [ SerializeField ] private bool m_CalcReflectionHoriz = true ;
        [ Range ( 0 , 180 ) ]
        [ SerializeField ] float m_LightAngleHoriz = 45f ;
        [ SerializeField ] Color m_ReflexionColor = StdReflexionColor ;

        // shadow
        [ SerializeField ] private bool m_ShowShadow = false ;
        [ Range ( 0 , 100 ) ]
        [ SerializeField ] int m_ShadowDarkening = 10 ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] float m_ShadowOffsetY = 1 ;

#pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
#pragma warning restore 649

        float blurSigma
        {
            get
            {
                if ( noBlur ) return 0.0f;
                return m_Sigma;
            }
        }

        bool hasBlur => !m_NoBlur && 0.0f < m_Sigma;

        public bool layerable
        {
            get => m_Layerable;
            set
            {
                if ( m_Layerable != value )
                {
                    m_Layerable = value;
                    SetVerticesDirty ();
                    SetMaterialDirty ();
                }
            }
        }

        public bool noBlur
        {
            get => m_NoBlur;
            set
            {
                if ( m_NoBlur != value )
                {
                    m_NoBlur = value;
                    SetVerticesDirty ();
                    SetMaterialDirty ();
                }
            }
        }
#if RudisApp
        public bool linearSampling
        {
            get => m_LinearSampling;
            set
            {
                if ( m_LinearSampling != value )
                {
                    m_LinearSampling = value;
                    //SetVerticesDirty () ;
                    SetMaterialDirty ();
                }
            }
        }
#else
        public bool linearSampling => true ;
#endif

        public float sigma
        {
            get => m_Sigma;
            set
            {
                if ( m_Sigma != value )
                {
                    m_Sigma = value;
                    SetMaterialDirty ();
                }
            }
        }

        public int step
        {
            get => m_Step;
            set
            {
                if ( m_Step != value )
                {
                    m_Step = value;
                    SetMaterialDirty ();
                }
            }
        }

        public int contrast
        {
            get => m_Contrast;
            set
            {
                if ( m_Contrast != value )
                {
                    m_Contrast = value;
                    SetMaterialDirty ();
                }
            }
        }

        public int brightness
        {
            get => m_Brightness;
            set
            {
                if ( m_Brightness != value )
                {
                    m_Brightness = value;
                    SetMaterialDirty ();
                }
            }
        }

        // round corners

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

        public bool hasRoundCorners
        {
            get
            {
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

        public float borderWidth
        {
            get => m_BorderWidth;
            set
            {
                m_BorderWidth = value;
                SetMaterialDirty ();
            }
        }

        public float refractivePower
        {
            get => m_RefractivePower;
            set
            {
                m_RefractivePower = value;
                SetMaterialDirty ();
            }
        }

        public bool hasBorder
        {
            get
            {
                switch ( m_BorderWidthSource )
                {
                    case BorderWidthSrc.RelativeToObjectSize: return m_BorderWidthObjectRel > 0f;
                    case BorderWidthSrc.RelativeToRadius: return hasRoundCorners && m_BorderWidthRadiusRel > 0f;
                    default: return m_BorderWidth > 0f;
                }
            }
        }

        public bool hasReflection => hasBorder && m_DrawReflection;

        public bool reflection
        {
            get => m_DrawReflection;
            set
            {
                m_DrawReflection = value;
                SetMaterialDirty ();
            }
        }

        public float rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value;
                transform.localRotation = Quaternion.Euler ( 0 , 0 , value );
                SetMaterialDirty ();
            }
        }

        private float borderWidthToDraw
        {
            get
            {
                switch ( m_BorderWidthSource )
                {
                    case BorderWidthSrc.RelativeToObjectSize: return m_BorderWidthObjectRel * minSizeLength;
                    case BorderWidthSrc.RelativeToRadius: return m_BorderWidthRadiusRel * getRadius ();
                    default: return m_BorderWidth;
                }
            }
        }

        public bool showShadow
        {
            get => m_ShowShadow;
            set
            {
                if ( value == m_ShowShadow ) return;
                m_ShowShadow = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }


        private Material m_MaterialGlassBlurLayerable = null ;
        private Material matGlassBlurLayerable
        {
            get
            {
                if ( null == m_MaterialGlassBlurLayerable )
                {
                    m_MaterialGlassBlurLayerable = Utils.createMaterial ( "Rudi/UI/GlassBlurLayerable" ); // GlassBlurLayerable GlassBlurConcealing
                    if ( null != m_MaterialGlassBlurLayerable && !CanvasUpdateRegistry.IsRebuildingGraphics () ) SetMaterialDirty ();
                }
                return m_MaterialGlassBlurLayerable;
            }
        }

        private Material m_MaterialGlassBlurConcealing = null ;
        private Material matGlassBlurConcealing
        {
            get
            {
                if ( null == m_MaterialGlassBlurConcealing )
                {
                    m_MaterialGlassBlurConcealing = Utils.createMaterial ( "Rudi/UI/GlassBlurConcealing" ); // GlassBlurLayerable GlassBlurConcealing
                    if ( null != m_MaterialGlassBlurConcealing && !CanvasUpdateRegistry.IsRebuildingGraphics () ) SetMaterialDirty ();
                }
                return m_MaterialGlassBlurConcealing;
            }
        }


        private Material m_MaterialGlassClearConcealing = null ;
        private Material matGlassClearConcealing
        {
            get
            {
                if ( null == m_MaterialGlassClearConcealing )
                {
                    m_MaterialGlassClearConcealing = Utils.createMaterial ( "Rudi/UI/GlassClearConcealing" ); // GlassClearConcealing GlassClearLayerable
                    if ( null != m_MaterialGlassClearConcealing && !CanvasUpdateRegistry.IsRebuildingGraphics () ) SetMaterialDirty ();
                }
                return m_MaterialGlassClearConcealing;
            }
        }

        private Material m_MaterialGlassClearLayerable = null ;
        private Material matGlassClearLayerable
        {
            get
            {
                if ( null == m_MaterialGlassClearLayerable )
                {
                    m_MaterialGlassClearLayerable = Utils.createMaterial ( "Rudi/UI/GlassClearLayerable" ); // GlassClearConcealing GlassClearLayerable
                    if ( null != m_MaterialGlassClearLayerable && !CanvasUpdateRegistry.IsRebuildingGraphics () ) SetMaterialDirty ();
                }
                return m_MaterialGlassClearLayerable;
            }
        }


        private Rect rect => rectTransform.rect;

        private float ppu => null != canvas ? canvas.scaleFactor : 1f;

        public override Material material
        {
            get
            {
                var NoBlur = m_NoBlur || 0.0f >= m_Sigma ;
                if ( m_Layerable ) return NoBlur ? matGlassClearLayerable : matGlassBlurLayerable;
                return NoBlur ? matGlassClearConcealing : matGlassBlurConcealing;
            }
        }

        protected override void UpdateMaterial ()
        {
            base.UpdateMaterial ();
            updateMaterial ( materialForRendering );
        }

        private Vector2 visibleSize => GetPixelAdjustedRect ().size;

        private float minSizeLength
        {
            get
            {
                var s = visibleSize ;
                return Mathf.Min ( s.x , s.y );
            }
        }

        private float getRelativeRadius ( float t )
        {
            return minSizeLength * t;
        }

        float getRadius ()
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

        private void switchBorderProfile ( Material mat , BorderProfile p )
        {
            foreach ( var pr in ( BorderProfile [ ] ) Enum.GetValues ( typeof ( BorderProfile ) ) )
            {
                mat.EnableKeyword ( pr.GetDescription () , pr == p );
            }
        }
        private static float getBySigma2 ( float sigma )
        {
            const float minus_log2e_half = -0.72134752044448170367996234050095f ; // ( -log₂ ( e ) / 2 ) = -0.72134752
            return minus_log2e_half / ( sigma * sigma + 0.00001f );
        }
        //private bool useLinearSampling => linearSampling ;
        public bool canShowShadow => hasBorder;
        public bool hasShadow
        {
            get
            {
                if ( !showShadow ) return false;
                if ( !canShowShadow ) return false;
                return true;
            }
        }

        private void updateMaterial ( Material mat )
        {
            var HasRoundCorners = hasRoundCorners ;
            var HasBorder = hasBorder ;
            var HasReflection = hasReflection;
            var HasRotation = 0 != m_Rotation && ( HasBorder || HasReflection ) ;
            var HasShadow = hasShadow ;

            mat.EnableKeyword ( "USE_LINEAR_SAMPLING" , linearSampling );
            //mat.EnableKeyword ( "ROUND_CORNERS"   , HasRoundCorners ) ;
            mat.EnableKeyword ( "DRAW_BORDER" , HasBorder );
            mat.EnableKeyword ( "DRAW_REFLECTION" , HasReflection );
            mat.EnableKeyword ( "CALC_REFLECTION_HORIZONTAL" , HasReflection && m_CalcReflectionHoriz );
            mat.EnableKeyword ( "ROTATION" , HasRotation ); // HasRotation
            mat.EnableKeyword ( "DROP_SHADOW" , HasShadow );

            switchBorderProfile ( mat , m_BorderProfile );

            var Ppu = ppu ;
            var Sigma = blurSigma ;
            mat.SetFloat ( "_Sigma" , Sigma * Ppu );
            mat.SetFloat ( "_Step" , m_Step );
            var mul = m_Contrast * 0.01f ;
            var add = 0.5f - 0.5f * mul + m_Brightness * 0.01f ;
            mat.SetColor ( "_ColorMul" , ( color * mul ).GetWithAlpha ( 1 ) );
            mat.SetColor ( "_ColorAdd" , ( color * add ).GetWithAlpha ( 1 ) );
            mat.SetFloat ( "_Clamp" , m_Clamp );
            mat.SetFloat ( "_Compress" , m_Compress );
            var SigmaPixels = Sigma * Ppu ;
            var BySigma = getBySigma2 ( SigmaPixels ) ;
            var SigmaShadow = 1.0f ;
            var BySigmaShadow = getBySigma2 ( SigmaShadow ) ;
            var Sigma4 = new Vector4 ( SigmaPixels , BySigma , SigmaShadow , BySigmaShadow ) ;
            mat.SetVector ( "_Sigma4" , Sigma4 );

            // round corners

            var size = rect.size ;
            if ( !size.hasArea () ) return;
            var fBorderWidth = borderWidthToDraw * Ppu ;
            var Smoothing = Mathf.Max ( m_Smoothing , 0.0001f ) ;
            var SizePixel = size * Ppu ;
            //Vector4 rectangle = new Vector4 ( SizePixel.x , SizePixel.y , 1f / Smoothing , 0f ) ;
            var rectangle = new Vector4 ( SizePixel.x , SizePixel.y , 1f / Smoothing , m_Smoothing ) ; // new since sdf
            mat.SetVector ( "_Rectangle" , rectangle );

            // scale
            var scaleX = SizePixel.x ;
            var scaleY = SizePixel.y ;
            float offsetX = 0 ;
            float offsetY = 0 ;
            var scale = new Vector4 ( offsetX , offsetY , scaleX , scaleY ) ;
            mat.SetVector ( "_Scale" , scale );
            if ( HasRotation )
            {
                var sin = Mathf.Sin ( Mathf.Deg2Rad * m_Rotation ) ;
                var cos = Mathf.Cos ( Mathf.Deg2Rad * m_Rotation ) ;
                mat.SetVector ( "_Rotation" , new Vector4 ( cos , -sin , sin , cos ) );
            }
            if ( true || HasRoundCorners )
            {
                var radius = getRadius () ;
                var radiusPixels4 = Vector4.one * radius * Ppu ;
                var RadiusScale = new Vector4 ( m_RadiusScale1 , m_RadiusScale2 , m_RadiusScale3 , m_RadiusScale4  ) ;
                radiusPixels4 = Vector4.Scale ( radiusPixels4 , RadiusScale );
                mat.SetVector ( "_Radius" , radiusPixels4 );
                {
                    // new since sdf
                    var CenterX = new Vector4 ( - SizePixel.x * 0.5f , - SizePixel.y * 0.5f , SizePixel.x * 0.5f , SizePixel.y * 0.5f ) ;
                    //Log.i ( TAG , "CenterX = " + CenterX );
                    mat.SetVector ( "_CenterX" , CenterX );

                }
            }

            if ( HasBorder )
            {
                var borderFade = 1f / fBorderWidth ;

                var BorderWidth = new Vector4 ( fBorderWidth , borderFade , -Smoothing * 0.5f , fBorderWidth ) ;
                mat.SetVector ( "_BorderWidth" , BorderWidth );
                mat.SetFloat ( "_RefractivePower" , m_RefractivePower );
                mat.SetFloat ( "_Thickness" , m_LiftUp );
            }

            if ( HasReflection )
            {
                var DirX =   Mathf.Sin ( Mathf.Deg2Rad * m_LightAngleHoriz ) ;
                var DirY = - Mathf.Cos ( Mathf.Deg2Rad * m_LightAngleHoriz ) ;
                mat.SetVector ( "_ReflectionVars" , new Vector4 ( DirX , DirY , m_LightAngleVert / 90f , Mathf.Pow ( 10.0f , m_ReflexionSharpness ) ) );
                mat.SetColor ( "_ReflectionColor" , m_ReflexionColor.GetWithAlpha ( 1 ) );
            }

            if ( HasShadow )
            {
                mat.SetFloat ( "_ShadowDarkening" , 0.01f * m_ShadowDarkening );
                mat.SetFloat ( "_ShadowOffsetY" , m_ShadowOffsetY );
            }
        }

        protected override void OnRectTransformDimensionsChange ()
        {
            base.OnRectTransformDimensionsChange ();
            if ( hasRoundCorners || hasBorder ) SetMaterialDirty ();
        }

        private static readonly Vector2 [] uv_standard  = { Vector2.zero , Vector2.up , Vector2.one , Vector2.right } ;
        private Vector2 getUV ( int id ) => uv_standard [id];
        private Vector2 getUV ( int id , float wx , float wy , Rect rect )
        {
            var raw = getUV ( id ) ;
            //float du = duvX ( raw.x , rect ) * widening ;
            //float dv = duvY ( raw.y , rect ) * widening ;
            var du = duvX ( rect ) * wx ;
            var dv = duvY ( rect ) * wy ;
            if ( raw.x < 0.5f ) du = -du;
            if ( raw.y < 0.5f ) dv = -dv;
            //Log.i ( TAG , $"raw = { raw }, du = { du }, dv = { dv }" ) ;
            return new Vector2 ( raw.x + du , raw.y + dv );
        }

        private float duvX ( Rect rect ) => 1.0f / rect.width;

        private float duvY ( Rect rect ) => 1.0f / rect.height;

        protected override void OnPopulateMesh ( VertexHelper vh )
        {
            vh.Clear ();
            {
                var r = GetPixelAdjustedRect () ;
                Color32 color32 = color ;

                if ( hasShadow )
                {
                    var fBorderWidth = borderWidthToDraw ;
                    var wx = fBorderWidth * 0.375f ; // 3 / 8
                    var wy_hi = wx * ( 1f - m_ShadowOffsetY ) ;
                    var wy_lo = wx * 2f - wy_hi ;
                    var v = RectUtils.getVertices ( r , wx , wy_lo , wx , wy_hi ) ;
                    vh.AddVert ( v.getV ( 0 ) , color32 , v.getUV ( 0 ) );
                    vh.AddVert ( v.getV ( 1 ) , color32 , v.getUV ( 1 ) );
                    vh.AddVert ( v.getV ( 2 ) , color32 , v.getUV ( 2 ) );
                    vh.AddVert ( v.getV ( 3 ) , color32 , v.getUV ( 3 ) );
                }
                else
                {
                    var v = RectUtils.getVertices ( r ) ;
                    vh.AddVert ( v.getV ( 0 ) , color32 , v.getUV ( 0 ) );
                    vh.AddVert ( v.getV ( 1 ) , color32 , v.getUV ( 1 ) );
                    vh.AddVert ( v.getV ( 2 ) , color32 , v.getUV ( 2 ) );
                    vh.AddVert ( v.getV ( 3 ) , color32 , v.getUV ( 3 ) );
                }

                vh.AddTriangle ( 0 , 1 , 2 );
                vh.AddTriangle ( 2 , 3 , 0 );
            }
        }
        protected override void OnEnable ()
        {
            base.OnEnable ();
            m_Tracker.Add ( this , rectTransform , DrivenTransformProperties.Rotation );
        }
        protected override void OnDisable ()
        {
            m_Tracker.Clear ();
            base.OnDisable ();
        }
        protected override void OnDestroy ()
        {
            base.OnDestroy ();
            Utils.DestroyObjectAndZero ( ref m_MaterialGlassBlurLayerable );
            Utils.DestroyObjectAndZero ( ref m_MaterialGlassBlurConcealing );
            Utils.DestroyObjectAndZero ( ref m_MaterialGlassClearConcealing );
            Utils.DestroyObjectAndZero ( ref m_MaterialGlassClearLayerable );
        }
#if UNITY_EDITOR

        protected override void OnValidate ()
        {
            base.OnValidate ();
            transform.localRotation = Quaternion.Euler ( 0 , 0 , m_Rotation );
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor ( typeof ( GlassBackground ) )]
    [CanEditMultipleObjects]
    public class LiquidGlassEditorV03 : UnityEditor.UI.GraphicEditor
    {
        SerializedProperty m_Rotation  ;
        SerializedProperty m_LinearSampling  ;
        SerializedProperty m_Layerable  ;
        SerializedProperty m_NoBlur  ;
        SerializedProperty m_Sigma ;
        SerializedProperty m_Step ;
        SerializedProperty m_Contrast ;
        SerializedProperty m_Brightness ;
        SerializedProperty m_Clamp ;
        SerializedProperty m_Compress ;

        // round corners

        SerializedProperty m_RelativeRadius ;
        SerializedProperty m_Radius ;
        SerializedProperty m_RadiusRelative ;
        SerializedProperty m_LimitToHalfRound ;
        SerializedProperty m_RadiusScale1 ;
        SerializedProperty m_RadiusScale2 ;
        SerializedProperty m_RadiusScale3 ;
        SerializedProperty m_RadiusScale4 ;
        SerializedProperty m_Smoothing ;

        // border

        SerializedProperty m_BorderWidthSource ;
        SerializedProperty m_BorderWidthObjectRel ;
        SerializedProperty m_BorderWidthRadiusRel ;
        SerializedProperty m_BorderWidth ;
        SerializedProperty m_RefractivePower ;
        SerializedProperty m_BorderProfile ;
        SerializedProperty m_LiftUp ;

        // reflexion

        SerializedProperty m_DrawReflection ;
        SerializedProperty m_ReflexionSharpness ;
        SerializedProperty m_LightAngleVert ;
        SerializedProperty m_CalcReflectionHoriz ;
        SerializedProperty m_LightAngleHoriz ;
        SerializedProperty m_ReflexionColor ;

        //m_BorderWidth

        // shadow
        SerializedProperty m_ShowShadow ;
        SerializedProperty m_ShadowDarkening ;
        SerializedProperty m_ShadowOffsetY ;

        protected override void OnEnable ()
        {
            base.OnEnable ();
            m_Rotation = serializedObject.FindProperty ( "m_Rotation" );
            m_Layerable = serializedObject.FindProperty ( "m_Layerable" );
            m_NoBlur = serializedObject.FindProperty ( "m_NoBlur" );
            m_LinearSampling = serializedObject.FindProperty ( "m_LinearSampling" );
            m_Sigma = serializedObject.FindProperty ( "m_Sigma" );
            m_Step = serializedObject.FindProperty ( "m_Step" );
            m_Contrast = serializedObject.FindProperty ( "m_Contrast" );
            m_Brightness = serializedObject.FindProperty ( "m_Brightness" );
            m_Clamp = serializedObject.FindProperty ( "m_Clamp" );
            m_Compress = serializedObject.FindProperty ( "m_Compress" );

            m_RelativeRadius = serializedObject.FindProperty ( "m_RelativeRadius" );
            m_Radius = serializedObject.FindProperty ( "m_Radius" );
            m_RadiusRelative = serializedObject.FindProperty ( "m_RadiusRelative" );
            m_LimitToHalfRound = serializedObject.FindProperty ( "m_LimitToHalfRound" );
            m_RadiusScale1 = serializedObject.FindProperty ( "m_RadiusScale1" );
            m_RadiusScale2 = serializedObject.FindProperty ( "m_RadiusScale2" );
            m_RadiusScale3 = serializedObject.FindProperty ( "m_RadiusScale3" );
            m_RadiusScale4 = serializedObject.FindProperty ( "m_RadiusScale4" );
            m_Smoothing = serializedObject.FindProperty ( "m_Smoothing" );
            // border
            m_BorderWidthSource = serializedObject.FindProperty ( "m_BorderWidthSource" );
            m_BorderWidthObjectRel = serializedObject.FindProperty ( "m_BorderWidthObjectRel" );
            m_BorderWidthRadiusRel = serializedObject.FindProperty ( "m_BorderWidthRadiusRel" );
            m_BorderWidth = serializedObject.FindProperty ( "m_BorderWidth" );
            m_RefractivePower = serializedObject.FindProperty ( "m_RefractivePower" );
            m_BorderProfile = serializedObject.FindProperty ( "m_BorderProfile" );
            m_LiftUp = serializedObject.FindProperty ( "m_LiftUp" );

            // reflexion
            m_DrawReflection = serializedObject.FindProperty ( "m_DrawReflection" );
            m_ReflexionSharpness = serializedObject.FindProperty ( "m_ReflexionSharpness" );
            m_LightAngleVert = serializedObject.FindProperty ( "m_LightAngleVert" );
            m_CalcReflectionHoriz = serializedObject.FindProperty ( "m_CalcReflectionHoriz" );
            m_LightAngleHoriz = serializedObject.FindProperty ( "m_LightAngleHoriz" );
            m_ReflexionColor = serializedObject.FindProperty ( "m_ReflexionColor" );

            // shadow
            m_ShowShadow = serializedObject.FindProperty ( "m_ShowShadow" );
            m_ShadowDarkening = serializedObject.FindProperty ( "m_ShadowDarkening" );
            m_ShadowOffsetY = serializedObject.FindProperty ( "m_ShadowOffsetY" );
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            EditorGUILayout.PropertyField ( m_Rotation );
            EditorGUILayout.PropertyField ( m_Layerable );
            EditorGUILayout.PropertyField ( m_NoBlur );
            //if ( !m_NoBlur.boolValue )
            {
                EditorGUILayout.PropertyField ( m_Sigma );
                EditorGUILayout.PropertyField ( m_Step );
            }
            EditorGUILayout.PropertyField ( m_Contrast );
            EditorGUILayout.PropertyField ( m_Brightness );
            EditorGUILayout.PropertyField ( m_Clamp );
            EditorGUILayout.PropertyField ( m_Compress );
            EditorGUILayout.PropertyField ( m_Color );
#if RudisApp
            EditorGUILayout.PropertyField ( m_LinearSampling );
#endif
            // round corners
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

            if ( IsRound )
            {
                EditorGUILayout.Space ();
                EditorGUILayout.PropertyField ( m_RadiusScale1 );
                EditorGUILayout.PropertyField ( m_RadiusScale2 );
                EditorGUILayout.PropertyField ( m_RadiusScale3 );
                EditorGUILayout.PropertyField ( m_RadiusScale4 );
                EditorGUILayout.Space ();
                EditorGUILayout.PropertyField ( m_Smoothing );
            }

            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ( "Refraction" , EditorStyles.boldLabel );
            var myObject = target as GlassBackground ;


            EditorGUILayout.PropertyField ( m_BorderWidthSource );
            //if ( !m_AsRadius.boolValue )
            if ( m_BorderWidthSource.intValue == ( int ) GlassBackground.BorderWidthSrc.RelativeToObjectSize )
            {
                EditorGUILayout.PropertyField ( m_BorderWidthObjectRel );
            }
            else if ( m_BorderWidthSource.intValue == ( int ) GlassBackground.BorderWidthSrc.RelativeToRadius )
            {
                EditorGUILayout.PropertyField ( m_BorderWidthRadiusRel );
            }
            else
            {
                EditorGUILayout.PropertyField ( m_BorderWidth );
            }


            if ( myObject.hasBorder || myObject.hasRoundCorners )
            {
                EditorGUILayout.PropertyField ( m_BorderProfile );
            }

            if ( myObject.hasBorder )
            {
                EditorGUILayout.PropertyField ( m_RefractivePower );
                if ( m_BorderProfile.intValue == ( int ) GlassBackground.BorderProfile.Round )
                {
                    EditorGUILayout.PropertyField ( m_LiftUp );
                }

                EditorGUILayout.Space ();
                EditorGUILayout.LabelField ( "Reflexion" , EditorStyles.boldLabel );
                EditorGUILayout.PropertyField ( m_DrawReflection );
                if ( m_DrawReflection.boolValue )
                {
                    EditorGUILayout.PropertyField ( m_ReflexionSharpness );
                    EditorGUILayout.PropertyField ( m_LightAngleVert );
                    EditorGUILayout.PropertyField ( m_CalcReflectionHoriz );
                    if ( m_CalcReflectionHoriz.boolValue )
                    {
                        EditorGUILayout.PropertyField ( m_LightAngleHoriz );
                    }
                    EditorGUILayout.PropertyField ( m_ReflexionColor );
                }
            }

            if ( myObject.canShowShadow )
            {
                EditorGUILayout.Space ();
                EditorGUILayout.PropertyField ( m_ShowShadow );
                if ( m_ShowShadow.boolValue )
                {
                    EditorGUILayout.PropertyField ( m_ShadowDarkening );
                    EditorGUILayout.PropertyField ( m_ShadowOffsetY );
                }
            }


            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ( "Raycast Controls" , EditorStyles.boldLabel );

            RaycastControlsGUI ();
            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif
}
