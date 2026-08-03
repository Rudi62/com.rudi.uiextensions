// author: Rudi Weinacker
// with friendly and patient support of Google-AI

using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Rudi.Core
{
    public abstract class BackgroundWork : CustomYieldInstruction
    {
        private static readonly string TAG = "Rudis BackgroundWork" ;

        public delegate void COMPLETED ( BackgroundWork obj );

        private int m_IsDoneState = 0 ; // Google says no volatile... ; 0 = busy, 1 = done
        private int m_ProgressBits = 0 ; // Google says no volatile...

        private volatile bool m_Aborted = false ;
        private volatile bool m_Success ;

        private COMPLETED m_Completed = null ;
        public COMPLETED completed { set => m_Completed = value; }

        public bool aborted => m_Aborted; // reliable in main thread and worker thread
        public bool success => m_Success;

        public bool isDone => m_IsDoneState == 1;
        public bool busy => m_IsDoneState == 0;
        public override bool keepWaiting => m_IsDoneState == 0; // CustomYieldInstruction-api

        public float progress => BitConverter.Int32BitsToSingle ( m_ProgressBits ); // simple cast, no execution overhead
        protected void SetProgress ( float value )
        {
            var bits = BitConverter.SingleToInt32Bits ( value ) ; // simple cast, no execution overhead
            Interlocked.Exchange ( ref m_ProgressBits , bits );
        }
        private void internal_start ()
        {
            OnPrepare ();
            ThreadPool.QueueUserWorkItem ( _ => internal_work () ); // faster than new Thread
        }

        private void internal_work ()
        {
            try
            {
                m_Success = work ();
            }
            catch ( Exception ex )
            {
                Log.e ( TAG , $"Exception in worker-thread: {ex.Message}\n{ex.StackTrace}" );
                m_Success = false;
            }
            finally
            {
                Interlocked.Exchange ( ref m_IsDoneState , 1 );
            }
        }
        protected abstract bool work (); // should be executed in background thread
        protected virtual void OnCleanup () { } // should be executed in main thread
        protected virtual void OnPrepare () { } // should be executed in main thread
        private void invoke ()
        {
            OnCleanup ();
            m_Completed?.Invoke ( this );
        }

        public void start () // main thread only
        {
            addInQueue ( this );
        }

        public void abort () => m_Aborted = true;

        // ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        // loop

        private static readonly Queue < BackgroundWork > m_Queue = new () ; // no need for ConcurrentQueue
        private static BackgroundWork m_CurrentWork = null ;
        private static BackgroundWork m_FinishedWork = null ;
        private static bool m_AnyRunning = false ;
        public static bool any_running => m_AnyRunning;
        private static void addInQueue ( BackgroundWork work )
        {
            // try immediate start
            if ( null == m_CurrentWork )
            {
                m_CurrentWork = work;
                work.internal_start ();
            }
            else
            {
                m_Queue.Enqueue ( work );
            }
            startLoop ();
        }

        private static void startLoop ()
        {
            if ( !m_AnyRunning )
            {
                //Log.i ( TAG , "starting loop" );
                Canvas.willRenderCanvases += loop;
                m_AnyRunning = true;
            }
        }

        private static void stopLoop ()
        {
            if ( m_AnyRunning )
            {
                //Log.i ( TAG , "stopping loop" );
                Canvas.willRenderCanvases -= loop;
                m_AnyRunning = false;
            }
        }

        private static void loop ()
        {
            if ( null != m_CurrentWork )
            {
                if ( m_CurrentWork.busy ) return;
                else
                {
                    m_FinishedWork = m_CurrentWork; // start of next object, then finish this one
                    m_CurrentWork = null;
                }
            }
            // m_CurrentWork is null
            if ( m_Queue.TryDequeue ( out m_CurrentWork ) ) m_CurrentWork.internal_start ();
            else stopLoop ();
            if ( null != m_FinishedWork )
            {
                m_FinishedWork.invoke ();
                m_FinishedWork = null;
            }
        }
    }
}
