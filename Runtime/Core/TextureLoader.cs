
// author: Rudi Weinacker
// Hardened with help of Google AI

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Rudi.Core
{
    // Inheriting from CustomYieldInstruction allows this class to be used with 'yield return'
    public class TextureLoader
    {
        private static readonly string TAG = "Rudis TextureLoader";

        public delegate void FINISHED ( Texture2D result );
        private UnityWebRequest m_UnityWebRequest = null ;
        private UnityWebRequestAsyncOperation m_UnityWebRequestAsyncOperation = null ;
        public bool isLoading => !m_UnityWebRequestAsyncOperation?.isDone ?? false;
        public float progress => m_UnityWebRequestAsyncOperation?.progress ?? 0;

        private FINISHED m_Callback = null ;

        public static string getUrlFromFilePath ( string filepath ) => new System.Uri ( System.IO.Path.GetFullPath ( filepath ) , UriKind.Absolute ) . AbsoluteUri ;
        public static TextureLoader Load ( string url , FINISHED callback ) => new TextureLoader ( url , callback );
        public static TextureLoader LoadFile ( string filepath , FINISHED callback ) => new TextureLoader ( getUrlFromFilePath ( filepath ) , callback );

        public TextureLoader ( string url , FINISHED callback )
        {
            if ( null == callback ) return;
            m_Callback = callback;
            m_UnityWebRequest = UnityWebRequestTexture.GetTexture ( url );
            m_UnityWebRequestAsyncOperation = m_UnityWebRequest.SendWebRequest ();
            m_UnityWebRequestAsyncOperation.completed += finished;
        }

        public void abort ()
        {
            if ( null == m_UnityWebRequest ) return;
            //Log.i ( TAG , $"abort () - url = { m_UnityWebRequest.url }" ) ;
            // Order matters: Clear the callback first to signal 'finished()' that this is an intentional abort
            m_Callback = null;
            m_UnityWebRequestAsyncOperation = null;

            // Triggers 'finished()' synchronously on the Main Thread immediately
            m_UnityWebRequest?.Abort ();
        }

        private void finished ( AsyncOperation obj )
        {
            // Safe cast using defensive programming, even though Unity guarantees this type here
            var asyncOp = obj as UnityWebRequestAsyncOperation;
            if ( asyncOp == null ) return;

            var currentRequest = asyncOp.webRequest;
            Texture2D tex = null;

            try
            {
                if ( currentRequest != null )
                {
                    if ( currentRequest.result == UnityWebRequest.Result.Success )
                    {
                        tex = DownloadHandlerTexture.GetContent ( currentRequest );
                    }
                    else
                    {
                        Log.i ( TAG , $"{currentRequest.error} {currentRequest.url}" );
                    }
                }
            }
            catch ( Exception e )
            {
                Utils.DestroyObjectAndZero ( ref tex );
            }
            finally
            {
                currentRequest?.Dispose ();

                // Safe to null directly since the entire abort chain executes synchronously on the Main Thread
                m_UnityWebRequest = null;
                m_UnityWebRequestAsyncOperation = null;

                if ( null != m_Callback )
                {
                    m_Callback.Invoke ( tex );
                }
                else
                {
                    // Cleans up the downloaded texture if 'abort()' cleared the callback beforehand
                    Utils.DestroyObject ( tex );
                }
            }
        }
    }
}
