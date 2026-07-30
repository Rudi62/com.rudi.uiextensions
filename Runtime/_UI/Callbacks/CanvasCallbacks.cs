using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    public class CanvasCallbacks
    {
        private static readonly string TAG = "Rudis CanvasCallbacks" ;

        // use within monobehavior:

        //private CanvasCallbacks m_Callbacks = null ;
        //private CanvasCallbacks callbacks => m_Callbacks ??= new CanvasCallbacks ( OnCanvasCallback , this , CanvasCallbacks.Timing.Early );
        //private void OnCanvasCallback ()
        //{
        //    Log.i ( TAG , "OnCanvasCallback ()" );
        //}

        private const bool RunInEditMode = true ;

        public enum Timing
        {
            Early,
            Late
        }

        // https://csharpindepth.com/Articles/Events

        private Canvas.WillRenderCanvases m_InternalCallback = null ;
        private Canvas.WillRenderCanvases m_ExternalCallback ;
        private int m_Counter ;
        private MonoBehaviour m_Mono = null ;
        private delegate void ToCall ( Canvas.WillRenderCanvases callback );
        private readonly ToCall m_AddCallback ;
        private readonly ToCall m_RemoveCallback ;
        public bool isRegistered => null != m_InternalCallback;

#if UNITY_EDITOR
        //private bool canStartCallbacks => Application.isPlaying ? null == m_InternalCallback : RunInEditMode ? null == m_InternalCallback : false ;
        private bool canStartCallbacks
        {
            get
            {
                if ( RunInEditMode )
                {
                    return null == m_InternalCallback;
                }
                else
                {
                    if ( !Application.isPlaying ) return false;
                    return null == m_InternalCallback;
                }
            }
        }
#else
        private bool canStartCallbacks => null == m_InternalCallback ;
#endif

        public CanvasCallbacks ( Canvas.WillRenderCanvases callback , MonoBehaviour mono , Timing timing = Timing.Early )
        {
            m_ExternalCallback = callback;
            m_Mono = mono;
            if ( null != m_Mono )
            {
                m_Mono.destroyCancellationToken.Register ( Stop );
            }
            if ( timing == Timing.Early )
            {
                m_AddCallback = addCallbackEarly;
                m_RemoveCallback = removeCallbackEarly;
            }
            else
            {
                m_AddCallback = addCallbackLate;
                m_RemoveCallback = removeCallbackLate;
            }
        }

        public void setCallback ( Canvas.WillRenderCanvases callback )
        {
            if ( isRegistered ) return;
            m_ExternalCallback = callback;
        }

        public void StartRepeating ( int numRepeats )
        {
            if ( !canStartCallbacks ) return;
            if ( null == m_ExternalCallback ) return;
            m_Counter = numRepeats;
            addCallback ( CallbackFunctionRepeat );
        }

        public void StartRepeating ()
        {
            if ( !canStartCallbacks ) return;
            if ( null == m_ExternalCallback ) return;
            addCallback ( m_ExternalCallback );
        }

        public void StartSingle ( int numFramesDelay = 1 )
        {
            if ( !canStartCallbacks ) return;
            if ( null == m_ExternalCallback ) return;
            m_Counter = numFramesDelay;
            addCallback ( CallbackFunctionSingle );
        }

        public void StartAsap () => StartSingle ( 0 );

        private static void addCallbackEarly ( Canvas.WillRenderCanvases callback ) => Canvas.preWillRenderCanvases += callback;
        private static void addCallbackLate ( Canvas.WillRenderCanvases callback ) => Canvas.willRenderCanvases += callback;
        private static void removeCallbackEarly ( Canvas.WillRenderCanvases callback ) => Canvas.preWillRenderCanvases -= callback;
        private static void removeCallbackLate ( Canvas.WillRenderCanvases callback ) => Canvas.willRenderCanvases -= callback;
        private void addCallback ( Canvas.WillRenderCanvases callback ) => m_AddCallback ( m_InternalCallback = callback );

        public void Stop ()
        {
            if ( isRegistered )
            {
                m_RemoveCallback ( m_InternalCallback );
                m_InternalCallback = null;
            }
        }

        public int counter => m_Counter;

        public void requestCallback ( Canvas.WillRenderCanvases callback , int numRepeats = 1 )
        {
            if ( !canStartCallbacks ) return;
            if ( null == callback ) return;
            m_ExternalCallback = callback;
            m_Counter = numRepeats;
            addCallback ( CallbackFunctionRepeat );
        }

        private void CallbackFunctionRepeat ()
        {
            if ( --m_Counter < 0 ) Stop ();
            m_ExternalCallback ();
        }

        private void CallbackFunctionSingle ()
        {
            if ( --m_Counter < 0 )
            {
                Stop ();
                m_ExternalCallback ();
            }
        }
    }
}
