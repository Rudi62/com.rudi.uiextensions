using System.Collections;
using System.Collections.Generic;
using Rudi.RMath;
using Rudi.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Rudi.UI
{
    [System.Serializable]
    public class OnOffTweenRunner
    {
        private static readonly string TAG = "Rudis OnOffTweenRunner" ;

        public OnOffTweenRunner ( MonoBehaviour parent , GETCURRENTVALUE getCurrentValue )
        {
            m_CoroutineContainer = parent;
            m_GetCurrentValue = getCurrentValue;
        }

        private readonly MonoBehaviour m_CoroutineContainer ;
        public delegate float GETCURRENTVALUE ();
        private readonly GETCURRENTVALUE m_GetCurrentValue ;

        // onValueChanged callback
        public class TweenValueCallback : UnityEvent<float> { }
        private TweenValueCallback OnValueChanged = new () ;
        public TweenValueCallback onValueChanged => OnValueChanged;

        // finished callback
        public class TweenFinishedCallback : UnityEvent<bool> { }
        private TweenFinishedCallback OnFinished = new () ;
        public TweenFinishedCallback onFinished => OnFinished;

        private SmoothStep.Mode m_Mode = SmoothStep.Mode.Smooth1 ;
        public SmoothStep.Mode mode { get => m_Mode; set => m_Mode = value; }

        private float m_Power = 1.2f ;
        public float power { get => m_Power; set => m_Power = value; }

        //private bool m_DeactivateIfOff = false ;
        //public bool deactivateIfOff { get => m_DeactivateIfOff ; set => m_DeactivateIfOff = value ; }

        private bool m_Dest ;
        private Coroutine  m_RunningCoroutine = null ;
        private IEnumerator Start ( float start , float dest , float duration )
        {
            //Log.i ( TAG , m_CoroutineContainer.gameObject , $"in Coroutine ( {start} , {dest} , {duration} ) = " );
            var elapsedTime = 0.0f ;
            do
            {
                var perc = elapsedTime / duration ;
                var val = Mathf.Lerp ( start , dest , perc ) ;
                var step = SmoothStep.step ( mode , val , power ) ;
                onValueChanged.Invoke ( step );
                yield return Utils.WaitForEndOfFrame ;
                elapsedTime += Time.deltaTime ;
            } while ( elapsedTime < duration );
            onValueChanged.Invoke ( dest );
            OnFinished.Invoke ( m_Dest );
            m_RunningCoroutine = null;
            //Log.i ( TAG , m_CoroutineContainer.gameObject , "Coroutine - finished" );

        }

        public bool valid => null != m_CoroutineContainer;
        public bool isTweening => null != m_RunningCoroutine;

        private void StartTween ( float start , float dest , float duration )
        {
            //m_Tween = Start ( start , dest , duration );
            //m_CoroutineContainer.StartCoroutine ( m_Tween );

            // advice from Google-AI
            // StartCoroutine nimmt den IEnumerator an, gibt aber ein echtes Coroutine-Objekt zurück
            m_RunningCoroutine = m_CoroutineContainer.StartCoroutine ( Start ( start , dest , duration ) ) ;
        }

        public void tween ( bool dest , float duration )
        {
            if ( !valid ) return; // already destroyed or not set
            StopTween ();
            //if ( deactivateIfOff && dest ) m_CoroutineContainer.gameObject.SetActive ( true ) ;
            m_Dest = dest;
            float destVal = dest ? 1 : 0 ;
            //Log.i ( TAG , m_CoroutineContainer.gameObject , "duration = " + duration );
            if ( !m_CoroutineContainer.gameObject.activeInHierarchy || 0 >= duration )
            {
                //Log.i ( TAG , m_CoroutineContainer.gameObject , "m_CoroutineContainer.gameObject.activeInHierarchy = " + m_CoroutineContainer.gameObject.activeInHierarchy ) ;
                OnValueChanged.Invoke ( destVal );
                OnFinished.Invoke ( dest );
                return;
            }
            var StartVal = m_GetCurrentValue () ;
            m_Dest = dest;
            StartTween ( StartVal , destVal , duration );
        }

        public void StopTween ()
        {
            if ( m_RunningCoroutine != null )
            {
                m_CoroutineContainer.StopCoroutine ( m_RunningCoroutine );
                m_RunningCoroutine = null;
            }
        }
        public void OnDestroy ()
        {
            StopTween ();
            //OnFinished.RemoveAllListeners ();
            //OnValueChanged.RemoveAllListeners ();
        }
    }

    public abstract class TweenRunnerBase
    {
        protected TweenRunnerBase ( MonoBehaviour parent ) => m_CoroutineContainer = parent;

        protected readonly MonoBehaviour m_CoroutineContainer ;

        public class TweenFinishedCallback : UnityEvent { }
        protected TweenFinishedCallback OnFinished = new () ;
        public TweenFinishedCallback onFinished => OnFinished;

        public delegate float ON_STEP ( float perc );
        public ON_STEP onStep = null ;

        protected Coroutine m_RunningCoroutine = null ;
        protected abstract void performLerp ( float step );
        private IEnumerator Start ( float duration )
        {
            var elapsedTime = 0.0f ;
            do
            {
                var perc = elapsedTime / duration ;
                var step = onStep != null ? onStep ( perc ) : SmoothStep.step ( mode , perc , power ) ;
                performLerp ( step );
                yield return Utils.WaitForEndOfFrame ;
                elapsedTime += Time.deltaTime ;
            } while ( elapsedTime < duration );
            performLerp ( 1 );
            //finished () ;
            OnFinished.Invoke ();
            m_RunningCoroutine = null;
        }

        public bool valid => null != m_CoroutineContainer;
        public bool isTweening => null != m_RunningCoroutine;

        protected SmoothStep.Mode m_Mode = SmoothStep.Mode.Smooth1 ;
        public SmoothStep.Mode mode
        {
            get => m_Mode;
            set => m_Mode = value;
        }
        private float m_Power = 1.2f ;
        public float power { get => m_Power; set => m_Power = value; }

        protected void StartTween ( float duration )
        {
            //m_Tween = Start ( duration );
            //m_CoroutineContainer.StartCoroutine ( m_Tween );
            // advice from Google-AI
            // StartCoroutine nimmt den IEnumerator an, gibt aber ein echtes Coroutine-Objekt zurück
            m_RunningCoroutine = m_CoroutineContainer.StartCoroutine ( Start ( duration ) ) ;

        }

        public void StopTween ()
        {
            if ( m_RunningCoroutine != null )
            {
                m_CoroutineContainer.StopCoroutine ( m_RunningCoroutine );
                m_RunningCoroutine = null;
            }
        }
    }
    public abstract class TweenRunner<T> : TweenRunnerBase where T : struct
    {
        private static readonly string TAG = "Rudis TweenRunner" ;

        protected TweenRunner ( MonoBehaviour parent , GETCURRENTVALUE getCurrentValue ) : base ( parent ) => m_GetCurrentValue = getCurrentValue;
        public delegate T GETCURRENTVALUE ();
        protected readonly GETCURRENTVALUE m_GetCurrentValue ;
        public class TweenValueCallback : UnityEvent<T> { }
        protected TweenValueCallback OnValueChanged = new () ;
        public TweenValueCallback onValueChanged => OnValueChanged;

        private T m_StartVal ;
        private T m_DestVal ;
        protected abstract T lerp ( T startval , T destval , float p );
        protected override void performLerp ( float step )
        {
            var val = lerp ( m_StartVal , m_DestVal , step ) ;
            OnValueChanged.Invoke ( val );
        }

        public void tween ( T destVal , float duration )
        {
            if ( !valid ) return; // already destroyed or not set
            StopTween ();
            //Log.i ( TAG , m_CoroutineContainer.gameObject , "duration = " + duration );
            if ( !m_CoroutineContainer.gameObject.activeInHierarchy || 0 >= duration )
            {
                OnValueChanged.Invoke ( destVal );
                OnFinished.Invoke ();
                return;
            }
            m_StartVal = m_GetCurrentValue ();
            m_DestVal = destVal;
            StartTween ( duration );
        }

        public void OnDestroy ()
        {
            StopTween ();
            OnFinished.RemoveAllListeners ();
            OnValueChanged.RemoveAllListeners ();
        }
    }

    public class TweenRunnerFloat : TweenRunner<float>
    {
        public TweenRunnerFloat ( MonoBehaviour parent , GETCURRENTVALUE getCurrentValue ) : base ( parent , getCurrentValue ) { }
        protected override float lerp ( float startval , float destval , float p ) => Mathf.Lerp ( startval , destval , p );
    }
    public class TweenRunnerVector3 : TweenRunner<Vector3>
    {
        public TweenRunnerVector3 ( MonoBehaviour parent , GETCURRENTVALUE getCurrentValue ) : base ( parent , getCurrentValue ) { }
        protected override Vector3 lerp ( Vector3 startval , Vector3 destval , float p ) => Vector3.Lerp ( startval , destval , p );
    }
    public class TweenRunnerColor : TweenRunner<Color>
    {
        public TweenRunnerColor ( MonoBehaviour parent , GETCURRENTVALUE getCurrentValue ) : base ( parent , getCurrentValue ) { }
        protected override Color lerp ( Color startval , Color destval , float p ) => Color.Lerp ( startval , destval , p );
    }
}
