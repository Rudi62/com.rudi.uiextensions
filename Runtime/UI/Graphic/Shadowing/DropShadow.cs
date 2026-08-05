
using UnityEngine;
using UnityEditor;
using Rudi.Extensions;
using Rudi.Core;

namespace Rudi.UI
{
    [System.Serializable]
    public class DropShadow
    {
        private static readonly string TAG = "Rudis DropShadow" ;
        // https://discussions.unity.com/t/a-simple-way-to-access-the-class-from-the-property-drawer/900422/8
        public enum MessageReason
        {
            ValuesChanged,
            ObjectChanged,
        }

        private static readonly Properties m_DefaultProperties = new Properties () ;
        public static Properties defaultProperties => m_DefaultProperties;
        // defaults

        public static readonly float DefaultElevation = 3.0f ;
        public static readonly bool  DefaultUseLocalProperties = false ;
        [ SerializeField ] private bool m_ShowShadow = false ;
        [ ReadOnly ]
        [ SerializeField ] private bool m_CanSink = false ;
        //[ Range ( 0 , 30 ) ]
        [ SerializeField ] private float m_Elevation = DefaultElevation ;
        [ ReadOnly ]
        [ SerializeField ] private float m_ShadowDistance = DefaultElevation ;
        [ SerializeField ] private bool m_UseLocalProperties = DefaultUseLocalProperties ;
        [ SerializeField ] private Properties m_LocalProperties = new () ;

        public bool useLocalProperties => m_UseLocalProperties;

        public bool canSink
        {
            get => m_CanSink;
            set => m_CanSink = value;
        }

        public float elevation
        {
            get
            {
                manageRenewals ();
                return m_Elevation;
            }
            set
            {
                manageRenewals ();
                if ( value == m_Elevation ) return;
                m_Elevation = value;
                m_ShadowObject?.shadowChanged ();
            }
        }

        public float distance
        {
            get
            {
                manageRenewals ();
                return Mathf.Abs ( multipliedElevation );
            }

            //set
            //{
            //    if ( value == m_ShadowDistance ) return;
            //    m_ShadowDistance = value;
            //    m_ShadowObject?.shadowChanged ();
            //}
        }

        private bool m_RenewalsChecked = false ;
        public void manageRenewals ()
        {
            if ( m_RenewalsChecked )
            {
                m_ShadowDistance = m_Elevation;
            }
            else
            {
                m_Elevation = m_ShadowDistance;
                m_RenewalsChecked = true;
            }
        }

        private float m_ElevationMultiplier = 1 ;
        public float elevationMultiplier
        {
            get => m_ElevationMultiplier;
            set
            {
                if ( m_ElevationMultiplier == value ) return;
                m_ElevationMultiplier = value;
                m_ShadowObject?.shadowChanged ();
                //Log.i ( TAG , "m_DistanceMultiplier = " + m_DistanceMultiplier ) ;
            }
        }

        public bool showShadow
        {
            get => m_ShowShadow;
            set
            {
                if ( value == m_ShowShadow ) return;
                m_ShowShadow = value;
                m_ShadowObject?.shadowChanged ();
            }
        }
        private bool m_IsListening = false ;
        public void releasePropertiesObject ()
        {
            if ( null != m_PropertiesObject )
            {
                if ( m_IsListening )
                {
                    m_PropertiesObject.removeListener ( m_ShadowObject );
                    m_IsListening = false;
                }
            }
            m_PropertiesObject = null;
        }

        bool m_IsEnabled = false ;

        void startListening ()
        {
            if ( m_IsListening ) return;
            if ( null == m_PropertiesObject ) return;
            if ( null == m_ShadowObject ) return;
            //Log.i ( TAG , m_MonoObject.gameObject , "Add listener" ) ;
            m_PropertiesObject.addListener ( m_ShadowObject );
            m_IsListening = true;
        }

        void stopListening ()
        {
            if ( !m_IsListening ) return;
            m_IsListening = false;
            if ( null == m_PropertiesObject ) return;
            if ( null == m_ShadowObject ) return;
            m_PropertiesObject.removeListener ( m_ShadowObject );
        }

        bool shouldListen ()
        {
            if ( !m_IsEnabled ) return false;
            if ( !showShadow ) return false;
            if ( m_UseLocalProperties ) return false;
            return null != m_PropertiesObject;
        }

        void checkListener ()
        {
            var ShouldListen = shouldListen () ;
            if ( m_IsListening == ShouldListen ) return;
            if ( ShouldListen ) startListening ();
            else stopListening ();
        }

        public GameObject gameObjct => null != m_MonoObject ? m_MonoObject.gameObject : null;
        public bool hasObjects ()
        {
            if ( null == m_ShadowObject ) return false;
            if ( null == m_MonoObject ) return false;
            return true;
        }
        private void checkObjects ()
        {
            if ( null == m_MonoObject )
            {
                Log.e ( TAG , gameObjct , "Mono object not assigned!" );
            }
            if ( null == m_ShadowObject )
            {
                Log.e ( TAG , gameObjct , "Shadow object not assigned!" );
            }
        }

        public void OnEnable ()
        {
            checkObjects ();
            m_IsEnabled = true;
            setPropertiesObject ();
#if UNITY_EDITOR
            if ( null != m_ShadowObject ) ShadowProperties.addStatic ( m_ShadowObject );
#endif
            //checkListener () ;
        }

        public void OnDisable ()
        {
            if ( !m_IsEnabled ) return;
            checkObjects ();
            stopListening ();
            m_IsEnabled = false;
#if UNITY_EDITOR
            if ( null != m_ShadowObject ) ShadowProperties.removeStatic ( m_ShadowObject );
#endif
            //checkListener () ;
        }

        public void OnDestroy ()
        {
            m_ShadowObject = null;
            m_MonoObject = null;
        }

        public void setPropertiesObject ( ShadowProperties prop )
        {
            if ( prop != m_PropertiesObject )
            {
                //Log.i ( TAG , "new ShadowProperties object" ) ;
                releasePropertiesObject ();
                m_PropertiesObject = prop;
                if ( null != prop )
                {
                    m_Properties = prop.shadowProperties;
                    checkListener ();
                }
            }
        }

        private ShadowProperties m_PropertiesObject = null ;
        private MonoBehaviour m_MonoObject = null ;
        private Properties m_Properties = defaultProperties ;
        public Properties properties => m_Properties;
        public void setPropertiesObject ()
        {
            //bool didChange = false ;
            var OldProperties = m_Properties ;
            m_Properties = null;
            if ( m_UseLocalProperties )
            {
                m_Properties = m_LocalProperties;
                releasePropertiesObject ();
            }
            else if ( null != m_MonoObject )
            {
                if ( null == m_PropertiesObject )
                {
                    var prop = m_MonoObject.getFirstActiveEnabledParent < ShadowProperties > () ;
                    if ( null == prop )
                    {
                        prop = Utils.GetRootObject<ShadowProperties> ();
                    }
                    setPropertiesObject ( prop );
                }
                else
                {
                    m_Properties = m_PropertiesObject.shadowProperties;
                    checkListener ();
                }
            }
            if ( null == m_Properties )
            {
                releasePropertiesObject ();
                m_Properties = defaultProperties;
            }
            if ( m_Properties != OldProperties && m_IsEnabled )
            {
                m_ShadowObject?.shadowChanged ();
            }
        }

        public void resetPropertiesObject ()
        {
            stopListening ();
            releasePropertiesObject ();
            setPropertiesObject ();
        }

        public void resetProperties ()
        {
            m_ShadowDistance = DefaultElevation;
            m_UseLocalProperties = DefaultUseLocalProperties;
            m_Properties.reset ();
            updated ();
        }

        public void updated ()
        {
            manageRenewals ();
            if ( m_UseLocalProperties ) m_LocalProperties.updated ();
            checkListener ();
            if ( !m_IsEnabled ) return;
            setPropertiesObject ();
        }

        public void propertiesObjectChanged ()
        {
            if ( !m_UseLocalProperties ) setPropertiesObject ();
        }

        private ISoftShadow m_ShadowObject = null ;
        public void setObject ( ISoftShadow obj )
        {
            if ( null == obj )
            {
                Log.i ( TAG , "Assigning null object" );
                return;
            }
            if ( null != m_ShadowObject && obj != m_ShadowObject )
            {
                Log.e ( TAG , "reassign object!" );
            }
            if ( m_ShadowObject == obj )
            {
                return;
            }

            m_ShadowObject = obj;
            m_MonoObject = obj.getMonoObject ();
            checkListener ();
        }

        public DropShadow ( ISoftShadow obj )
        {
            m_ShadowObject = obj;
            m_MonoObject = obj.getMonoObject ();
        }

        public DropShadow ( bool initialShow )
        {
            m_ShowShadow = initialShow;
        }

        public static float MyGammaToLinearSpace ( float gamma ) => Mathf.Pow ( gamma , 2.2f ) ;
        public static float UnityGammaToLinearSpace ( float gamma ) => Mathf.GammaToLinearSpace ( gamma ) ;
        public static float GammaToLinearSpace ( float gamma ) => UnityGammaToLinearSpace ( gamma ) ;
        public static float AlphaGammaToLinearSpace      ( float alpha_gamma ) => 1f - GammaToLinearSpace      ( 1f - alpha_gamma ) ;
        public static float MyAlphaGammaToLinearSpace    ( float alpha_gamma ) => 1f - MyGammaToLinearSpace    ( 1f - alpha_gamma ) ;
        public static float UnityAlphaGammaToLinearSpace ( float alpha_gamma ) => 1f - UnityGammaToLinearSpace ( 1f - alpha_gamma ) ;

        public static float getAdaptedShadowAlpha ( float alpha_gamma )
        {
            if ( QualitySettings.activeColorSpace != ColorSpace.Linear ) return alpha_gamma ;
            return AlphaGammaToLinearSpace ( alpha_gamma ) ;
        }

        public DropShadow () { }
        public float multipliedElevation => elevation * m_ElevationMultiplier;
        public float diffusivity => properties.diffusivity;
        public float enlargeWithDistance => properties.enlargeWithDistance;
        public float darkening => properties.darkening;
        public float brighteningWithBlur => properties.brighteningWithBlur;
        public bool linearSampling => properties.linearSampling;
        public Vector2 dirHorizontal => properties.dirHorizontal;
        public float sinAngleVertical => properties.sinAngleVertical;
        public float cosAngleVertical => properties.cosAngleVertical;
        public float byCosAngleVertical => properties.byCosAngleVertical;
        public float angleHorizontal => properties.angleHorizontal;
        public float angleVertical => properties.angleVertical;
        public float shadowDistance => distance * byCosAngleVertical * 2.0f;
        public Vector2 shadowOffset => dirHorizontal * ( shadowDistance * 1.4142f * sinAngleVertical );
        public Vector3 lightDir => properties.lightDir;
        public Vector4 lightDir4 => properties.lightDir4;
        public float sigma => shadowDistance * diffusivity * 0.1f;
        //public float shadowAlpha => darkening * Mathf.Exp ( -0.1f * sigma * brighteningWithBlur );
        public float shadowAlpha => getAdaptedShadowAlpha ( darkening * Mathf.Exp ( -0.1f * sigma * brighteningWithBlur ) ) ;

        public Rect getWidened ( Rect rect , float sigma_factor = 2f )
        {
            if ( multipliedElevation <= 0f ) return rect;
            var widening = sigma * sigma_factor ;
            var offset = shadowOffset ;
            var xMinOffset = Mathf.Max ( widening - offset.x , 0f ) ;
            var yMinOffset = Mathf.Max ( widening - offset.y , 0f ) ;
            var xMaxOffset = Mathf.Max ( widening + offset.x , 0f ) ;
            var yMaxOffset = Mathf.Max ( widening + offset.y , 0f ) ;
            var Result = rect ;
            Result.xMin -= xMinOffset;
            Result.yMin -= yMinOffset;
            Result.xMax += xMaxOffset;
            Result.yMax += yMaxOffset;
            //Log.i ( TAG , $"widened by ( { xMinOffset } , { yMinOffset } , { xMaxOffset } , { yMaxOffset } )" ) ;
            return Result;
        }

        // ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // class Properties

        [System.Serializable]
        public class Properties
        {
            private static readonly string TAG = "Rudis DropShadow.Properties" ;

            public const float DefaultDiffusivity         = 5 ;
            public const float DefaultEnlargeWithDistance = 0.4f ;
            public const int   DefaultDarkening           = 28 ;
            public const int   DefaultBrighteningWithBlur = 30 ; // 30 10
            public const float DefaultAngleHorizontal     = 45 ;
            public const float DefaultAngleVertical       = 22.5f ;
            public const bool  DefaultLinearSampling      = true ;

            [ Range ( 0 , 10 ) ]
            [ SerializeField ] public float m_Diffusivity         = DefaultDiffusivity ;
            [ Range ( 0 , 2 ) ]
            [ SerializeField ] public float m_EnlargeWithDistance = DefaultEnlargeWithDistance ;
            [ Range ( 0 , 100 ) ]
            [ SerializeField ] public int   m_Darkening           = DefaultDarkening ;
            [ Range ( 0 , 100 ) ]
            [ SerializeField ] public int   m_BrighteningWithBlur = DefaultBrighteningWithBlur ;
            [ Range ( 0 , 360 ) ]
            [ SerializeField ] public float m_AngleHorizontal     = DefaultAngleHorizontal ;
            [ Range ( 0 , 60 ) ]
            [ SerializeField ] public float m_AngleVertical       = DefaultAngleVertical ;
            [ Tooltip ( "Uses fast gauss calculations using GPU bilinear sampler to calculate two pixels in one sample step. Switching on or off shouldn't make any visible difference. Recommented to leave on!" )]
            [ SerializeField ] public bool  m_LinearSampling      = DefaultLinearSampling ;

            private Vector2 m_DirHorizontal = Vector2.zero ;
            private Vector3 m_LightDir = Vector3.zero ;
            private float m_SinAngleVertical = 0 ;
            private float m_CosAngleVertical = 0 ;
            private float m_ByCosAngleVertical = 0 ;
            public float diffusivity => m_Diffusivity;
            public float enlargeWithDistance => m_EnlargeWithDistance;
            public float darkening => m_Darkening * 0.01f;
            public float brighteningWithBlur => m_BrighteningWithBlur * 0.01f;
            public bool linearSampling => m_LinearSampling;
            public Vector2 dirHorizontal => m_DirHorizontal;
            public float sinAngleVertical => m_SinAngleVertical;
            public float cosAngleVertical => m_CosAngleVertical;
            public float byCosAngleVertical => m_ByCosAngleVertical;
            public float angleHorizontal => m_AngleHorizontal;
            public float angleVertical => m_AngleVertical;
            public Vector3 lightDir => m_LightDir;
            public Vector4 lightDir4 => new Vector4 ( m_DirHorizontal.x , m_DirHorizontal.y , m_SinAngleVertical , m_CosAngleVertical );
            public void updated ()
            {
                m_DirHorizontal = new Vector2 ( Mathf.Cos ( m_AngleHorizontal * Mathf.Deg2Rad ) , -Mathf.Sin ( m_AngleHorizontal * Mathf.Deg2Rad ) );
                m_SinAngleVertical = Mathf.Sin ( m_AngleVertical * Mathf.Deg2Rad );
                m_CosAngleVertical = Mathf.Cos ( m_AngleVertical * Mathf.Deg2Rad );
                m_ByCosAngleVertical = 0 != m_CosAngleVertical ? 1f / m_CosAngleVertical : 0f;
                m_LightDir = new Vector3 ( m_DirHorizontal.x * m_SinAngleVertical , m_DirHorizontal.y * m_SinAngleVertical , m_CosAngleVertical );
                //Log.i ( TAG , $"m_LightDir = ( { m_LightDir } )" ) ;
            }
            public Properties ()
            {
                updated ();
            }

            public void reset ()
            {
                m_Diffusivity = DefaultDiffusivity;
                m_EnlargeWithDistance = DefaultEnlargeWithDistance;
                m_Darkening = DefaultDarkening;
                m_BrighteningWithBlur = DefaultBrighteningWithBlur;
                m_AngleHorizontal = DefaultAngleHorizontal;
                m_AngleVertical = DefaultAngleVertical;
                m_LinearSampling = DefaultLinearSampling;
                updated ();
            }
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer ( typeof ( DropShadow.Properties ) )]
    public class DropShadowPropertiesPropertyDrawer : SimplePropertyDrawer
    {
        private SerializedProperty m_Diffusivity         ;
        private SerializedProperty m_EnlargeWithDistance ;
        private SerializedProperty m_Darkening           ;
        private SerializedProperty m_BrighteningWithBlur ;
        private SerializedProperty m_AngleHorizontal     ;
        private SerializedProperty m_AngleVertical       ;
        private SerializedProperty m_LinearSampling      ;
        protected override void SetProperties ()
        {
            m_Diffusivity = findProperty ( "m_Diffusivity        " );
            m_EnlargeWithDistance = findProperty ( "m_EnlargeWithDistance" );
            m_Darkening = findProperty ( "m_Darkening          " );
            m_BrighteningWithBlur = findProperty ( "m_BrighteningWithBlur" );
            m_AngleHorizontal = findProperty ( "m_AngleHorizontal    " );
            m_AngleVertical = findProperty ( "m_AngleVertical      " );
            m_LinearSampling = findProperty ( "m_LinearSampling     " );
        }
        protected override void PassDrawer ()
        {
            addField ( m_Diffusivity );
            addField ( m_EnlargeWithDistance );
            addField ( m_Darkening );
            addField ( m_BrighteningWithBlur );
            addField ( m_AngleHorizontal );
            addField ( m_AngleVertical );
            addField ( m_LinearSampling );
        }
    }

    [CustomPropertyDrawer ( typeof ( DropShadow ) )]
    public class DropShadowPropertyDrawer : SimplePropertyDrawer
    {
        private static readonly string TAG = "Rudis DropShadowPropertyDrawer" ;

        private SerializedProperty m_ShowShadow         ;
        private SerializedProperty m_CanSink            ;
        private SerializedProperty m_Elevation          ;
        private SerializedProperty m_ShadowDistance     ;
        private SerializedProperty m_UseLocalProperties ;
        private SerializedProperty m_LocalProperties    ;

        protected override void SetProperties ()
        {
            m_ShowShadow = findProperty ( "m_ShowShadow        " );
            m_CanSink = findProperty ( "m_CanSink           " );
            m_Elevation = findProperty ( "m_Elevation         " );
            m_ShadowDistance = findProperty ( "m_ShadowDistance    " );
            m_UseLocalProperties = findProperty ( "m_UseLocalProperties" );
            m_LocalProperties = findProperty ( "m_LocalProperties   " );
        }

        protected override void PassDrawer ()
        {
            addField ( m_ShowShadow );
            if ( m_ShowShadow.boolValue )
            {
                incIntend ();
                //addField ( m_CanSink        ) ;
                var canSink = m_CanSink.boolValue ;
                var MinVal = canSink ? -20f : 0f ;
                addSlider ( m_Elevation , MinVal , 30f );
                //addField ( m_ShadowDistance ) ;
                addField ( m_UseLocalProperties );
                if ( m_UseLocalProperties.boolValue )
                {
                    incIntend ();
                    addField ( m_LocalProperties );
                    decIntend (); // how much I miss c++ ...
                }
                decIntend (); // how much I miss c++ ...
            }
        }
    }
#endif
}
