using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    [DisallowMultipleComponent]
    [RequireComponent ( typeof ( RectTransform ) )]
    [ExecuteAlways]
    [AddComponentMenu ( "Rudi/UI/Safe Area" )]
    public class UiSafeArea : MonoBehaviour
    {
        private static readonly string TAG = "Rudis UiSafeArea" ;

        [ Header ( "Coverage" ) ]
        [ SerializeField ] private bool m_Symmetric = false ;
        [ SerializeField ] private float m_SubtractFromTopMargin = 0f ;

        private const int maxNumRepeats = 5 ;

        private CanvasCallbacks m_Callbacks = null ;
        private CanvasCallbacks callbacks => m_Callbacks ??= new CanvasCallbacks ( OnCallback , this );
        private void OnCallback ()
        {
            if ( TryCoverArea () ) callbacks.Stop ();
        }

        private void logFull ( string msg ) => Log.i ( TAG , msg + " - " + gameObject.name + " from " + transform.parent.gameObject.name );

        private Canvas m_Canvas = null ;
        private Canvas canvas
        {
            get
            {
                if ( null == m_Canvas )
                {
                    m_Canvas = GetComponentInParent<Canvas> ();
                    if ( null == m_Canvas )
                    {
                        //Log.i ( TAG , gameObject , "Couldn't find canvas" );
                    }
                }
                return m_Canvas;
            }
        }

        private RectTransform m_RectTransform ;
        private RectTransform rectTransform
        {
            get
            {
                if ( null == m_RectTransform )
                {
                    m_RectTransform = GetComponent<RectTransform> ();
                }
                return m_RectTransform;
            }
        }

        private static float? m_PPU = null ;
        private float ppu
        {
            get
            {
                if ( null == m_PPU )
                {
                    if ( null == canvas ) return 1f;
                    var f = canvas.scaleFactor ;
                    if ( f == 1f ) return 1f;
                    m_PPU = canvas.scaleFactor;
                }
                return m_PPU.Value;
            }
        }

        private void OnTransformParentChanged ()
        {
            clearCache ();
        }

        private void clearCache ()
        {
            m_Canvas = null;
            m_PPU = null;
            m_Margins = null;
        }

        private static Vector4? m_Margins = null ;
        public Vector4 safeMargins
        {
            get
            {
                if ( null == m_Margins )
                {
                    if ( 1f == ppu ) return Vector4.zero;
                    var sa = Screen.safeArea ;
                    var Top    = ( Screen.height - sa.y - sa.height ) / ppu ;
                    var Right  = ( Screen.width - sa.x - sa.width ) / ppu ;
                    var Left   = sa.x / ppu ;
                    var Bottom = sa.y / ppu ;
                    m_Margins = new Vector4 ( Left , Bottom , Right , Top );
                }
                return m_Margins.Value;
            }
        }

        public float top
        {
            get
            {
                var Top    = Mathf.Max ( safeMargins.w - m_SubtractFromTopMargin , 0f ) ;
                var Bottom = safeMargins.y ;
                return m_Symmetric ? Mathf.Max ( Top , Bottom ) : Top;
            }
        }

        public float bottom
        {
            get
            {
                var Top    = Mathf.Max ( safeMargins.w - m_SubtractFromTopMargin , 0f ) ;
                var Bottom = safeMargins.y ;
                return m_Symmetric ? Mathf.Max ( Top , Bottom ) : Bottom;
            }
        }

        private void setMarginsToRectTransform ( Vector4 margins )
        {
            var Left   = margins.x ;
            var Bottom = margins.y ;
            var Right  = margins.z ;
            var Top    = Mathf.Max ( margins.w - m_SubtractFromTopMargin , 0f ) ;
            if ( m_Symmetric )
            {
                Top = Mathf.Max ( Top , Bottom ); Bottom = Top;
                Left = Mathf.Max ( Left , Right ); Right = Left;
            }
            rectTransform.offsetMin = new Vector2 ( Left , Bottom );
            rectTransform.offsetMax = new Vector2 ( -Right , -Top );
        }

        private bool TryCoverArea ()
        {
            if ( null == canvas ) return false;
            if ( 1f == canvas.scaleFactor ) return false;
            if ( Mathf.Abs ( canvas.pixelRect.width - Screen.width ) > 0.1f ) return false;

            setMarginsToRectTransform ( safeMargins );
            return true;
        }

        DrivenRectTransformTracker m_Tracker ;
        private bool isTracking = false ;
        private void startTracker ()
        {
            m_Tracker.Clear ();
            m_Tracker.Add ( this , rectTransform , DrivenTransformProperties.All );
        }

        private void OnEnable ()
        {
            if ( !m_bStarted ) return;
            startTracker ();
            recalc ();
        }

        private void OnDisable ()
        {
            m_Tracker.Clear ();
            m_Callbacks?.Stop ();
            clearCache ();
            rectTransform.SetFull ();
        }

        public void cover ()
        {
            if ( isActiveAndEnabled ) callbacks.StartRepeating ( maxNumRepeats );
        }

        public void recalc ()
        {
            clearCache ();
            cover ();
        }

        bool m_bStarted = false ;
        private void Start ()
        {
            //logFull ( "Start ()" );
            rectTransform.SetFull ();
            rectTransform.localScale = Vector3.one;
            m_bStarted = true;
            OnEnable ();
        }


#if UNITY_EDITOR
        [ContextMenu ( "Recalculate" )]
        private void SwitchOffAllLogs ()
        {
            OnDeviceChanged ();
            EditorUtility.SetDirty ( this );
        }

        private void OnDeviceChanged ()
        {
            recalc ();
        }

        protected virtual void OnValidate ()
        {
            //if ( EditorApplication.isPlayingOrWillChangePlaymode ) return ;
            recalc ();
        }
#endif
    }
}
