using UnityEngine ;
using UnityEngine.Events ;

// =========================================================================================
// ScreenControl
//
// Developed by Rudi Weinacker under patient assistance of Google-AI
//
// Centralized static manager for screen orientation and orientation change events in Unity.
// Enables temporary changes in orientation as well as glitch-free change notifications.
//
// BACKGROUND & INNER WORKINGS:
// Unity's built-in callbacks (such as OnRectTransformDimensionsChange or UI canvas events) 
// often suffer from race conditions or 1-frame glitches during orientation transitions.
// ScreenControl hooks into Canvas.willRenderCanvases to sample the physical screen 
// orientation deterministically immediately before the UI rendering pass.
//
// Polling runs strictly on demand:
// - It polls when listeners are registered AND either AutoRotation is active or the screen 
//   has not yet reached the forced target orientation.
// - It completely detaches from Canvas.willRenderCanvases when idle to maintain zero overhead.
//
// KEY FEATURE: intendedOrientation VS Screen.orientation:
// On mobile devices, Unity's Screen.orientation always reflects the current physical 
// device orientation and NEVER returns ScreenOrientation.AutoRotation. 
// ScreenControl solves this: ScreenControl.intendedOrientation reliably preserves and 
// returns ScreenOrientation.AutoRotation if it was previously set by the user or script.
// This allows caller views to restore the true intended mode rather than being locked 
// to whatever physical orientation the device happened to be in when caching the state.
//
// USAGE GUIDELINES & CONVENTIONS:
// 1. Strict Exclusive Access (MANDATORY):
//    NEVER assign Screen.orientation directly anywhere in the project! All orientation 
//    changes must go exclusively through ScreenControl.orientation.
//    Reason: ScreenControl dynamically activates/deactivates the polling loop based on 
//    the intended state. Direct writes to Screen.orientation bypass this state tracking, 
//    leaving the polling loop asleep and preventing change notifications from firing.
// 2. Project Player Settings:
//    Keep a fixed default orientation (e.g., Portrait) in Unity's Player Settings.
// 3. Enabling Auto-Rotation:
//    Do not rely on Player Settings flags for auto-rotation. If your project starts in 
//    or allows free rotation, initialize it explicitly in code at startup:
//        ScreenControl.orientation = ScreenOrientation.AutoRotation;
// 4. Listener Lifecycle:
//    Always pair AddListener and RemoveListener symmetrically (typically in OnEnable 
//    and OnDisable). Any mismatch will log an error (Log.e) during development.
//
// EXAMPLE: Temporarily forcing an orientation and restoring the previous state:
//    // Cache the previous intended orientation (accurately preserves AutoRotation if active!)
//    ScreenOrientation previous = ScreenControl.intendedOrientation;
//
//    // Force target view orientation (exclusively via ScreenControl!)
//    ScreenControl.orientation = ScreenOrientation.Portrait;
//
//    // ... view active ...
//
//    // Restore previous state cleanly (switches back to AutoRotation if it was active before)
//    ScreenControl.orientation = previous;
// =========================================================================================

namespace Rudi.Core
{
    public static class ScreenControl
    {
        private static readonly string TAG = "Rudis ScreenOrientationNotifier" ;

        private static ScreenOrientation m_LastScreenOrientation = ScreenOrientation.Unknown ;
        private static ScreenOrientation m_IntendedScreenOrientation = ScreenOrientation.Unknown ;
        public static bool isAutoRotate => ScreenOrientation.AutoRotation == m_IntendedScreenOrientation ;
        private static bool isOrientationValid ( ScreenOrientation o ) => o != ScreenOrientation.Unknown ;

        // stack functionality
        private static readonly System.Collections.Generic.Stack < ScreenOrientation > m_OrientationStack = new () ;

        public static void SetTemporary ( ScreenOrientation newOrientation )
        {
            m_OrientationStack.Push ( intendedOrientation ) ;
            orientation = newOrientation ;
        }

        public static void Restore ()
        {
            if ( m_OrientationStack.Count == 0 )
            {
                Log.e ( TAG , "Restore called on an empty orientation stack!" ) ;
                return ;
            }
            orientation = m_OrientationStack.Pop () ;
        }

        public static ScreenOrientation orientation
        {
            get => Screen.orientation ;
            set
            {
                if ( !isOrientationValid ( value ) )
                {
                    Log.e ( TAG , "Not a valid orientation: " + value ) ;
                    return ;
                }
                if ( Utils.UpdateProperty ( ref m_IntendedScreenOrientation , value ) )
                {
                    Screen.orientation = value ;
                    checkStartPolling () ;
                }
            }
        }

        public static ScreenOrientation intendedOrientation
        {
            get
            {
                if ( m_IntendedScreenOrientation == ScreenOrientation.Unknown )
                {
                    // no auto rotation in project settings supported!
                    // use auto rotation only through this class!
                    m_IntendedScreenOrientation = Screen.orientation ;
                }
                return m_IntendedScreenOrientation ;
            }
        }

        private static UnityEvent m_ScreenOrientationChangeEvent = new () ;
        private static int m_NumListeners = 0 ;
        private static bool m_IsPolling = false ;
        public static void AddListener ( UnityAction call )
        {
            m_ScreenOrientationChangeEvent.AddListener ( call ) ;
            m_NumListeners++ ;
            checkStartPolling () ;
        }

        public static void RemoveListener ( UnityAction call )
        {
            m_ScreenOrientationChangeEvent.RemoveListener ( call ) ;
            m_NumListeners-- ;
            if ( m_NumListeners <= 0 ) stopPolling () ;
            if ( m_NumListeners < 0 )
            {
                Log.e ( TAG , "Num listeners error" ) ;
            }
        }

        private static bool shouldPoll => ( m_NumListeners > 0 ) && ( m_LastScreenOrientation != m_IntendedScreenOrientation ) ;
        private static void checkStartPolling ()
        {
            if ( shouldPoll ) startPolling () ;
        }

        private static void startPolling ()
        {
            if ( m_IsPolling ) return ;
            Canvas.willRenderCanvases += OnWillRenderCanvases ;
            //Log.i ( TAG , "polling started" ) ;
            m_IsPolling = true ;
        }

        private static void stopPolling ()
        {
            if ( !m_IsPolling ) return ;
            Canvas.willRenderCanvases -= OnWillRenderCanvases ;
            //Log.i ( TAG , "polling stopped" ) ;
            m_IsPolling = false ;
        }

        private static void OnWillRenderCanvases ()
        {
            if ( Utils.UpdateProperty ( ref m_LastScreenOrientation , Screen.orientation ) )
            {
                m_ScreenOrientationChangeEvent.Invoke () ;
            }

            if ( !shouldPoll ) stopPolling () ;
        }

        public static bool isLandScape ( ScreenOrientation o )
        {
            switch ( o )
            {
                case ScreenOrientation.LandscapeLeft  :
                case ScreenOrientation.LandscapeRight : return true ;
                default: return false ;
            }
        }

        public static ScreenOrientation lastOrientation
        {
            get
            {
                if ( m_LastScreenOrientation == ScreenOrientation.Unknown )
                {
                    m_LastScreenOrientation = Screen.orientation ;
                }
                return m_LastScreenOrientation ;
            }
        }

        public static bool isLandScape () => isLandScape ( lastOrientation ) ;
    }
}
