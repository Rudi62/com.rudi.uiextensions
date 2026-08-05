using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Rudi.Extensions;
using Rudi.Core;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

// https://stackoverflow.com/questions/29354519/unity-5-clean-way-to-manage-dynamically-created-gameobjects

namespace Rudi.UI
{
    // https://tutorials.eu/unity-attributes-15-tipps-for-the-perfect-inspector-experience/
    [AddComponentMenu ( "Rudi/UI/Soft Shadow" )]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent ( typeof ( Graphic ) )]

    public class SoftShadow : CanvasElement, ISoftShadow
    {
        private static readonly string TAG = "Rudis SoftShadow";

        [ SerializeField ] private DropShadow m_DropShadow = new () ;
        [ Space ( 10 ) ]
        [ Tooltip ( "Switching shadow off if distance is set to zero to prevent artefacts at the borders" ) ]
        [ SerializeField ] private bool m_OmitIfZeroDistance = false ;
        [ SerializeField ] WatchTransformInEditor m_Watcher = new () ;

        private void log ( string msg ) { log ( TAG , msg ); }
        private void log1 ( string msg ) { log1 ( TAG , msg ); }

        public float distance => m_DropShadow.distance;

        public float elevation
        {
            get => m_DropShadow.elevation;
            set
            {
                m_DropShadow.elevation = value;
                rebuildShadowGraphic ();
            }
        }


        public bool showShadow
        {
            get => m_DropShadow.showShadow;
            set
            {
                if ( value == m_DropShadow.showShadow ) return;
                m_DropShadow.showShadow = value;
                if ( null != shadowReciever ) shadowReciever.shadowEnabled = shadowEnabled;
                if ( !m_ShadowCalculated && value ) rebuildShadowGraphic ();
            }
        }

        public float elevationMultiplier
        {
            get => shadowProperties.elevationMultiplier;
            set
            {
                if ( shadowProperties.elevationMultiplier == value ) return;
                shadowProperties.elevationMultiplier = value;
            }
        }

        private TweenRunnerFloat m_DistanceTweener ;
        private TweenRunnerFloat distanceTweener
        {
            get
            {
                if ( null == m_DistanceTweener )
                {
                    m_DistanceTweener = new ( this , () => elevationMultiplier );
                    m_DistanceTweener.onValueChanged.AddListener ( ( val ) => elevationMultiplier = val );
                }
                return m_DistanceTweener;
            }
        }

        public void CrossFadeElevationMultiplier ( float dest , float duration ) => distanceTweener.tween ( dest , duration );

        public bool omitIfZeroDistance
        {
            get => m_OmitIfZeroDistance;
            set
            {
                m_OmitIfZeroDistance = value;
                rebuildShadowGraphic ();
            }
        }

        public void shadowChanged ()
        {
            rebuildShadowGraphic ();
        }


        private bool m_ShadowCalculated = false ;
        private bool m_OnDestroy        = false ;
        private bool m_ShadowRecieverDestroyed = false ;
        private ShadowReciever m_ShadowReciever; // ShadowReciever SoftShadowReciever
        private ShadowReciever shadowReciever
        {
            get
            {
                if ( null == m_ShadowReciever )
                {
                    if ( m_OnDestroy ) return null; // m_Started m_Awakened
                    if ( m_ShadowRecieverDestroyed ) return null; // m_Started m_Awakened
                    m_ShadowReciever = ShadowReciever.Create ( this , renderTexture );
                    m_ShadowReciever.RegisterUpdatePosition ();
                }
                return m_ShadowReciever;
            }
        }

        // private data
        private bool m_Started = false;
        private bool m_Awakened = false;

        private RenderTexture m_RenderTexture ;
        private RenderTexture renderTexture
        {
            get
            {
                if ( null == m_RenderTexture )
                {
                    if ( m_OnDestroy ) return null ;
                    m_RenderTexture = new RenderTexture ( 10 , 10 , 0 , RenderTextureFormat.ARGB32 ) ;
                    //m_RenderTexture.Create ();
                    m_RenderTexture.filterMode = FilterMode.Bilinear ; // Bilinear is needed for linear sampling method
                    m_RenderTexture.wrapMode   = TextureWrapMode.Clamp ; // Schützt die Schattenränder vor Artefakten
                    m_RenderTexture.hideFlags  = HideFlags.HideAndDontSave ; // DontSave HideAndDontSave
                }
                return m_RenderTexture ;
            }
        }

        private DropShadow shadowProperties => m_DropShadow ;

        //private void log ( string msg ) { log ( TAG , msg ) ; }
        private RectTransform m_RectTransform = null ;
        public RectTransform rectTransform
        {
            get
            {
                if ( null == m_RectTransform )
                {
                    m_RectTransform = graphic.rectTransform;
                }
                return m_RectTransform;
            }
        }

        private RectTransform parentRC
        {
            get
            {
                var res = rectTransform.parent as RectTransform ;
                if ( null == res )
                {
                    log ( "no parent" );
                }
                return res;
            }
        }

        private float multipliedElevation => shadowProperties.multipliedElevation;
        private float diffusivity => shadowProperties.diffusivity;
        private float enlargeWithDistance => shadowProperties.enlargeWithDistance;
        private float brighteningWithBlur => shadowProperties.brighteningWithBlur;
        private float angleHorizontal => shadowProperties.angleHorizontal;
        private float angleVertical => shadowProperties.angleVertical;
        private bool linearSampling => shadowProperties.linearSampling;
        private float darkening => shadowProperties.darkening;

        private float sigma => shadowDistance * diffusivity * 0.1f;
        private float pixel_sigma => sigma * ppu;
        private float destAlpha => darkening * Mathf.Exp ( -0.1f * sigma * brighteningWithBlur );
        public Color shadowColor => new Color ( 0 , 0 , 0 , destAlpha );
        private int border => Mathf.CeilToInt ( pixel_sigma * 3.0f );
        private Vector2 shadowDir => shadowProperties.dirHorizontal;
        private float shadowDistance => multipliedElevation * shadowProperties.byCosAngleVertical;
        private Vector2 shadowOffset => shadowDir * ( shadowDistance * 1.4142f * shadowProperties.sinAngleVertical ); // 1.4142f

        //private Vector2 myCenter => rectTransform.pivot + rectTransform.PivotToCenter () ;
        //private Vector2 myCenter => rectTransform.rect.center + rectTransform.PivotToCenter ()  ;

        private Vector2 myCenter => ( Vector2 ) rectTransform.localPosition + rectTransform.PivotToCenter () - ( null != parentRC ? parentRC.PivotToCenter () : Vector2.zero );
        public Vector2 shadowCenter => myCenter + shadowOffset;
        private Vector2 mySize => rectTransform.rect.size * transform.localScale;
        private float shadowEnlarge => enlargeWithDistance * shadowDistance + 2 * border / ppu;
        public Vector2 shadowSize => mySize + ( Vector2 ) transform.localScale * shadowEnlarge;
        //public bool shadowEnabled => ! ( m_OmitIfZeroDistance && multipliedDistance == 0.0f ) && isActiveAndEnabled && m_ShadowCalculated ;
        public bool shadowEnabled => showShadow && !( m_OmitIfZeroDistance && multipliedElevation == 0.0f ) && gameObject.activeInHierarchy && enabled;

        public void pleaseLogYourRect ()
        {
            log ( "my rect = " + rectTransform.rect );
        }
        private bool ErrorReturn ( string msg )
        {
            log ( TAG , msg );
            return false;
        }
        bool ISoftShadow.isEnabled => showShadow && enabled ;

        private bool isValid
        {
            get
            {
                if ( null == this ) return ErrorReturn ( "Object Destroyed" );
                if ( null == gameObject ) return ErrorReturn ( "Object Destroyed" );
                if ( !m_Started ) return ErrorReturn ( "not started" );
                if ( null == graphic ) return ErrorReturn ( "graphic is zero" );
                if ( null == shadowReciever ) return ErrorReturn ( "shadowReciever is zero" );
                if ( null == matBlur ) return ErrorReturn ( "matBlur is zero" );
                return true;
            }
        }

        //private float ppu => canvas != null ? canvas.scaleFactor : 1f ;

        public void clear ()
        {
            if ( null != m_RenderTexture )
            {
                m_RenderTexture.Release ();
                Utils.DestroyObject ( m_RenderTexture );
                m_RenderTexture = null;
            }
            m_ShadowCalculated = false;
        }

        const float MinimalSigma = 0.01f ;

        private bool errorReturn ( string msg )
        {
            log ( msg );
            return false;
        }

        private bool CalcShadow ()
        {
            if ( null == graphic ) return errorReturn ( "graphic is null" );
            if ( !hasPPU ) return errorReturn ( "no ppu" );
            var rt = GraphicRenderTools.drawGraphicToTexture ( renderTexture , graphic , border ) ;
            if ( rt == null ) return errorReturn ( "couldn't draw texture" ); ;

            //log ( "CalcShadow () - step 3" ) ;
            if ( pixel_sigma > MinimalSigma ) rt = getBlurred ( rt , pixel_sigma , linearSampling );
            else
            {
                log ( "sigma is zero" );
            }
            log ( "pixel_sigma = " + pixel_sigma );
            return true;
        }

        private CanvasCallbacks m_Callbacks = null ;
        private CanvasCallbacks callbacks => m_Callbacks ??= new CanvasCallbacks ( OnCallback , this );
        private void OnCallback ()
        {
            if ( hasPPU )
            {
                callbacks.Stop ();
                rebuildShadowGraphic ();
            }
        }

        public void rebuildShadowGraphic ()
        {
            if ( isValid )
            {
                log ( "rebuilding all" );
                //rebuildAll ();
                rebuildGraphic ();
                // ****************************************************************************************************************************************************
                //if ( null != m_ShadowReciever ) m_ShadowReciever.RegisterUpdatePosition () ;
                updateShadowRecieverPosition ();
            }
        }
        protected override void OnRebuild ( CanvasUpdate executing )
        {
            //switch ( executing )
            //{
            //    case CanvasUpdate.Prelayout:
            //    {
            //        shadowReciever?.UpdatePosition ();
            //        break;
            //    }
            //}
        }
        protected override void OnLayoutComplete () => updateShadowRecieverPosition ( true );
        protected override void OnVerticesDirty () => updateShadowRecieverPosition ();

        private void updateShadowRecieverPosition ( bool immediate = false )
        {
            if ( null == shadowReciever ) return;
            if ( immediate ) m_ShadowReciever.UpdatePosition ();
            else m_ShadowReciever.RegisterUpdatePosition ();
        }

        protected override void OnGraphicUpdateComplete ()
        {
            log ( "GraphicUpdateComplete ()" );
            if ( m_OnDestroy ) return;
            if ( !isActiveAndEnabled ) return;
            if ( hasPPU ) m_ShadowCalculated = CalcShadow ();
            //else callbacks.StartRepeating ( 5 );
            log ( "m_ShadowCalculated = " + m_ShadowCalculated );
            pleaseLogYourRect ();
        }

        private void OnTransformParentChanged ()
        {
            //shadowReciever.AdjustSiblingIndex () ;
            updateShadowRecieverPosition ();
            //ReassignEnvironmentVariables () ;
        }

#if UNITY_EDITOR
        protected override void OnValidate ()
        {
            base.OnValidate ();
            log ( "OnValidate ()" );
            m_DropShadow.setObject ( this );
            m_DropShadow.updated ();
            EditorApplication.delayCall += () => // causes problems
            {
                if ( m_OnDestroy ) return;
                if ( null == this ) return;

                //Log.i ( TAG , "OnValidate () - " + this );
                m_Watcher.OnValidate ( this , OnPositionChanged , OnParentChanged , OnIndexChanged );
                // https://discussions.unity.com/t/sendmessage-cannot-be-called-during-awake-checkconsistency-or-onvalidate-can-we-suppress/705805/20
                if ( null != shadowReciever )
                {
                    //if ( !Application.isPlaying ) shadowReciever.AdjustSiblingIndex ();
                    shadowReciever.shadowEnabled = shadowEnabled;
                }
                if ( !m_Started ) return;
                rebuildShadowGraphic ();

            };
        }
        [ContextMenu ( "Reset Shadow Properties" )]
        public void ResetShadowProperties ()
        {
            m_DropShadow.resetProperties ();
        }
#endif
        private const int maxNumRepeats = 5 ;

        private bool checkPPU ()
        {
            if ( hasPPU ) return true;
            callbacks.StartRepeating ( maxNumRepeats );
            return false;
        }

        protected override void OnEnable ()
        {
            base.OnEnable ();
            if ( !m_Started ) return;
            if ( !m_DropShadow.hasObjects () )
            {
                Log.i ( TAG , gameObject , "DropShadow doesn't have objects - e!" );
            }
            m_DropShadow.OnEnable ();
            if ( !m_OnDestroy )
            {
                m_ShadowRecieverDestroyed = false;
            }
            if ( !isActiveAndEnabled ) return;
            log ( "OnEnable ()" );
            if ( checkPPU () )
            {
                rebuildShadowGraphic ();
            }
        }
        private void unregisterCallback () => m_Callbacks?.requestStop ();

        protected override void OnDisable ()
        {
            unregisterCallback () ;
            if ( null != shadowReciever ) shadowReciever.shadowEnabled = false;
            base.OnDisable ();
            m_DropShadow.OnDisable ();
        }

        protected override void OnDestroy ()
        {
            log ( "OnDestroy ()" );
            m_OnDestroy = true;
            unregisterCallback () ;
            clear ();
            //ShadowProperties.ShadowObjectsChanged () ;
            m_DistanceTweener?.OnDestroy ();
            base.OnDestroy ();
        }

        private void Awake ()
        {
            callbackOnVerticesDirty = true;
            m_Awakened = true;
        }

        protected override void Start ()
        {
            base.Start ();
            log ( "Start ()" );
            m_DropShadow.setObject ( this );
            m_Started = true;
            m_OnDestroy = false;
            OnEnable ();

        }

        public MonoBehaviour getMonoObject () => this;
        public void propertiesObjectChanged ( DropShadow.MessageReason reason )
        {
            // todo implement!
            if ( DropShadow.MessageReason.ObjectChanged == reason )
            {
                m_DropShadow.resetPropertiesObject ();
            }
            shadowChanged ();
        }

        void OnPositionChanged ( Vector3 position )
        {
            log ( TAG , "Position changed" );
            updateShadowRecieverPosition ();
        }

        void OnIndexChanged ( int index )
        {
            log ( TAG , "index = " + index );
            //shadowReciever.AdjustSiblingIndex () ;
            updateShadowRecieverPosition ();

        }

        void OnParentChanged ( GameObject parent )
        {
            log ( TAG , "parent = " + parent );
            //shadowReciever.AdjustSiblingIndex () ;
            updateShadowRecieverPosition ();
        }

        public void OnShadowRecieverDestroyed ()
        {
            log ( "OnShadowObjectDestroyed ()" );
            //m_OnDestroy = true ;
            m_ShadowRecieverDestroyed = true;
            m_ShadowReciever = null;
        }

        private RenderTexture nullReturn ( string msg ) { log ( msg ); return null; }
        private RenderTexture nullReturn1 ( string msg ) { log1 ( msg ); return null; }
        private T objReturn<T> ( string msg , T t ) { log ( msg ); return t; }
        private T objReturn1<T> ( string msg , T t ) { log1 ( msg ); return t; }

        private static Material m_MaterialBlur = null ;
        private static Material matBlur
        {
            get
            {
                if ( null == m_MaterialBlur )
                {
                    m_MaterialBlur = Utils.createMaterial ( "Rudi/post/gauss_filter" );
                }

                return m_MaterialBlur;
            }
        }

        private RenderTexture getBlurred ( RenderTexture tex , float sigma , bool linear_sampling )
        {
            //Log.i ( TAG , "getBlurred()" ) ;
            if ( null == matBlur ) return objReturn1 ( "matBlur is zero" , tex );
            var tmp = RenderTexture.GetTemporary ( tex.width , tex.height , 0 , RenderTextureFormat.ARGB32 ) ;
            if ( null == tmp ) return nullReturn1 ( "failed getting render texture" );
            tmp.filterMode = FilterMode.Bilinear ;
            tmp.wrapMode = TextureWrapMode.Clamp ; // Zwingend erforderlich für saubere Ränder beim Blit!
            matBlur.SetFloat ( "_Sigma" , sigma );
            var pass = linear_sampling ? 2 : 0 ;
            log ( "sigma = " + sigma );
            //var previous = RenderTexture.active ; // Google-AI means, this is not necessary
            Graphics.Blit ( tex , tmp , matBlur , pass ); // horizontal
            Graphics.Blit ( tmp , tex , matBlur , pass + 1 ); // vertical
            //RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary ( tmp );
            return tex;
        }
    }
}
