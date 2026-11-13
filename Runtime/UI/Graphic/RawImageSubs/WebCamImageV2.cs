using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.Events;
using Rudi.Core;
using Rudi.UI;

namespace Rudi
{
    [ AddComponentMenu ( "Rudi/UI/WebCam Image V2" , 12 ) ]

	public class WebCamImageV2 : RawImageV2
	{
		private static readonly string TAG = "Rudis WebCamImageV2" ;

		[ SerializeField ] private bool m_Play = false ;


        public class WebcamStartedEvent : UnityEvent < bool > {}
        [ SerializeField ] private WebcamStartedEvent m_WebcamStarted = new () ;

        public WebcamStartedEvent camStarted => m_WebcamStarted ;
        private const float OrientationWatchDuration = 1.0f ;
        private Coroutine m_AdjustCoroutine      = null ;
        private Coroutine m_WatchCoroutine       = null ;
        private float     m_WatchUntilTime       = 0f ;
        private int       m_AppliedVideoAngle    = -1 ;
        private bool      m_AppliedVideoMirrored = false ;

        public enum Quality
        {
            SD  ,
            HD  ,
            FHD ,
            Max ,
        }

		public override Texture mainTexture => texture ;

		private WebCamTexture m_WebCamTexture = null ;
		public WebCamTexture webCamTexture
		{
			get
			{
				if ( null == m_WebCamTexture )
				{
					m_WebCamTexture = new WebCamTexture () ;
				}
				return m_WebCamTexture ;
			}
			set
			{
				if ( null != m_WebCamTexture )
				{
                    Utils.DestroyObject ( m_WebCamTexture ) ;
					m_WebCamTexture = null ;
				}
				m_WebCamTexture = value ;
			}
		}

		public bool play
		{
			get => m_Play ;
			set
			{
				m_Play = value ;
				if ( m_Play ) startPlaying () ;
				else stopPlaying () ;
			}
		}

        public bool isPlaying => ( null != webCamTexture && webCamTexture.isPlaying ) ;


        public void setup ( string cameraName , int width , int height )
		{
            bool wasPlaying = isPlaying ;
            if ( wasPlaying ) stopCoroutinesAndCam () ;
            webCamTexture.deviceName = cameraName ;
            webCamTexture.requestedWidth = width ;
            webCamTexture.requestedHeight = height ;
            if ( wasPlaying ) startPlaying () ;
        }

        IEnumerator adjust_Coroutine ()
		{
			do { yield return Utils.WaitForEndOfFrame ; } while ( !webCamTexture.didUpdateThisFrame ) ;
            m_AdjustCoroutine = null ;
            if ( !isPlaying ) yield break ;

            updateAppearanceIfNeeded ( force: true ) ;
            m_WebcamStarted.Invoke ( true ) ;

            // Fängt gleichzeitigen Orientierungswechsel beim Session-Start ab:
            startWatchingOrientation ();
        }

        private void startWatchingOrientation ()
        {
            if ( !isActiveAndEnabled || !isPlaying ) return;
            m_WatchUntilTime = Time.unscaledTime + OrientationWatchDuration;
            if ( null == m_WatchCoroutine )
            {
                m_WatchCoroutine = StartCoroutine ( watchOrientation_Coroutine () );
            }
        }
        private IEnumerator watchOrientation_Coroutine ()
        {
            updateAppearanceIfNeeded () ;

            while ( isPlaying && Time.unscaledTime < m_WatchUntilTime )
            {
                yield return null ;
                if ( null != m_WebCamTexture && m_WebCamTexture.didUpdateThisFrame )
                {
                    updateAppearanceIfNeeded () ;
                }
            }

            m_WatchCoroutine = null ;
        }
        private bool updateAppearanceIfNeeded ( bool force = false )
        {
            if ( null == m_WebCamTexture ) return false ;

            int  angle    = m_WebCamTexture.videoRotationAngle ;
            bool mirrored = m_WebCamTexture.videoVerticallyMirrored ;

            if ( !force && angle == m_AppliedVideoAngle && mirrored == m_AppliedVideoMirrored )
            {
                return false ;
            }

            m_AppliedVideoAngle = angle ;
            m_AppliedVideoMirrored = mirrored ;

            m_WebCamTexture.filterMode = FilterMode.Bilinear ;
            rotation = -( angle / 90 ) ;
            mirrorV = mirrored ;
            SetMaterialDirty () ;
            SetVerticesDirty () ;
            return true ;
        }
        private void adjustAppearance () => updateAppearanceIfNeeded ( force: true ) ;

        public void readjust () => updateAppearanceIfNeeded ( force: true ) ;

        private void stopCoroutines ()
        {
            if ( null != m_AdjustCoroutine )
            {
                StopCoroutine ( m_AdjustCoroutine );
                m_AdjustCoroutine = null;
            }
            if ( null != m_WatchCoroutine )
            {
                StopCoroutine ( m_WatchCoroutine );
                m_WatchCoroutine = null;
            }
        }

        //      private void adjustAppearance ()
        //{
        //	webCamTexture.filterMode = FilterMode.Bilinear ;
        //	rotation = - ( webCamTexture.videoRotationAngle / 90 ) ;
        //	mirrorV = webCamTexture.videoVerticallyMirrored ;
        //          SetMaterialDirty () ;
        //	SetVerticesDirty () ;
        //      }

        //public void readjust ()
        //{
        //    adjustAppearance () ;
        //}
        private void stopCoroutinesAndCam ()
        {
            stopCoroutines ();
            if ( null != m_WebCamTexture && m_WebCamTexture.isPlaying )
            {
                m_WebCamTexture.Stop () ;
            }
        }

        private void startPlaying ()
		{
			if ( null != webCamTexture && !webCamTexture.isPlaying )
			{
                stopCoroutines ();
                m_AppliedVideoAngle = -1;
                m_WebCamTexture.Play ();
                texture = m_WebCamTexture;
                m_AdjustCoroutine = StartCoroutine ( adjust_Coroutine () );
                //            //Log.i ( TAG , "start play" );
                //            webCamTexture.Play () ;
                //            //Log.i ( TAG , "back from play" );
                //            StartCoroutine ( adjust_Coroutine () ) ;
                //texture = webCamTexture ;
            }
        }

        private static ( int width , int height ) getResolution ( Quality quality )
        {
            switch ( quality )
            {
                case Quality.SD  : return (  640 ,  480 ) ;
                case Quality.HD  : return ( 1280 ,  720 ) ;
                case Quality.FHD : return ( 1920 , 1080 ) ;
                default : return ( 640 , 480 ) ;
            }
        }

        public void startPlaying ( Quality quality )
        {
            if ( !DeviceEnvironment.IsInEditor )
            {
                if ( isPlaying ) stopCoroutinesAndCam () ;

                if ( quality == Quality.Max )
                {
                    var id = Webcams.bestWebCam ;
                    webCamTexture.deviceName      = id.name   ;
                    webCamTexture.requestedWidth  = id.width  ;
                    webCamTexture.requestedHeight = id.height ;
                }
                else
                {
                    var res = getResolution ( quality ) ;
                    webCamTexture.requestedWidth  = res.width  ;
                    webCamTexture.requestedHeight = res.height ;
                }

            }
            startPlaying () ;
        }

        public void startPlaying ( Webcams.Identifier identifier )
        {
            if ( isPlaying ) stopCoroutinesAndCam () ;
            //if ( webCamTexture.isPlaying ) webCamTexture.Stop ();
            webCamTexture.deviceName      = identifier.name   ;
            webCamTexture.requestedWidth  = identifier.width  ;
            webCamTexture.requestedHeight = identifier.height ;
            Log.i ( TAG , "webcam resolution = " + identifier.getResolution () ) ;
            startPlaying () ;
        }

        private void stopPlaying ()
		{
            stopCoroutines ();
            m_AppliedVideoAngle = -1;
            if ( null != m_WebCamTexture && webCamTexture.isPlaying )
			{
                m_WebCamTexture.Stop () ;
				SetMaterialDirty () ;
				SetVerticesDirty () ;
			}
			texture = null ;
		}

        private void OnOrientationChanged ()
        {
            if ( isPlaying )
            {
                startWatchingOrientation () ;
                //readjust () ;
            }
        }


        protected override void OnEnable ()
		{
            ScreenControl.AddListener ( OnOrientationChanged ) ;
            if ( play ) startPlaying () ;
			base.OnEnable () ;
		}

		protected override void OnDisable ()
		{
            ScreenControl.RemoveListener ( OnOrientationChanged ) ;
            stopPlaying () ;
			base.OnDisable () ;
		}
		protected override void OnDestroy ()
		{
			if ( null != m_WebCamTexture )
			{
                Utils.DestroyObject ( m_WebCamTexture ) ;
				m_WebCamTexture = null ;
			}
			base.OnDestroy () ;
		}

        protected override void Start ()
        {
            base.Start () ;
            limitToHalfRound = true ;
        }

#if UNITY_EDITOR
        protected override void OnValidate ()
		{
			play = m_Play ;
			base.OnValidate () ;
		}
#endif

	}

#if UNITY_EDITOR
	[ CustomEditor ( typeof ( WebCamImageV2 ) ) ]
	public class WebCamImageV2Editor : UnityEditor.UI.GraphicEditor
	{
		SerializedProperty m_Play ;
		//SerializedProperty m_PreserveAspectRatio ;
		SerializedProperty m_FitMode ;
		SerializedProperty m_Radius ;
		SerializedProperty m_CropX ;
		SerializedProperty m_CropY ;

		protected override void OnEnable ()
		{
			base.OnEnable () ;
			m_Play = serializedObject.FindProperty ( "m_Play" ) ;
            //m_PreserveAspectRatio = serializedObject.FindProperty ( "m_PreserveAspectRatio" ) ;
            m_FitMode = serializedObject.FindProperty ( "m_FitMode" );
            m_Radius = serializedObject.FindProperty ( "m_Radius" ) ;
			m_CropX = serializedObject.FindProperty ( "m_CropX" ) ;
			m_CropY = serializedObject.FindProperty ( "m_CropY" ) ;
		}
		public override void OnInspectorGUI ()
		{
			serializedObject.Update () ;
			EditorGUILayout.PropertyField ( m_Play  ) ;
			EditorGUILayout.PropertyField ( m_Color ) ;
			//EditorGUILayout.PropertyField ( m_PreserveAspectRatio ) ;
			EditorGUILayout.PropertyField ( m_FitMode ) ;
			EditorGUILayout.PropertyField ( m_Radius ) ;
			EditorGUILayout.PropertyField ( m_CropX ) ;
			EditorGUILayout.PropertyField ( m_CropY ) ;
			RaycastControlsGUI () ;
			MaskableControlsGUI () ;
			serializedObject.ApplyModifiedProperties () ;
		}
	}
#endif
}
