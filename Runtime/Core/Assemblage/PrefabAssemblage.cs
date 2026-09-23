
using System;
using System.Linq;
using UnityEngine;

namespace Rudi.Core
{
    public class PrefabAssemblage : MonoBehaviour
    {
        private static readonly string TAG = "Rudis PrefabAssemblage" ;

        private static PrefabAssemblage m_PrefabAssemblage = null ;
        private static int m_FrameToUnload = -1 ;

        [ SerializeField ] private GameObject [] m_Prefabs = Array.Empty < GameObject > () ;

        private static PrefabAssemblage prefabAssemblage
        {
            get
            {
                if ( null == m_PrefabAssemblage )
                {
                    m_PrefabAssemblage = Resources.Load<PrefabAssemblage> ( "PrefabAssemblagePrefab" );
                }
                return m_PrefabAssemblage;
            }
        }

        public static T findPrefab<T> ( string name ) where T : Component
        {
            if ( prefabAssemblage == null ) return null;

            // Die NULL-Prüfung für das Array kann hier sicher entfallen!
            var directComp = prefabAssemblage.m_Prefabs
                .Where ( go => go != null && go.name == name )
                .Select ( go => go.GetComponent < T > () )
                .FirstOrDefault ( comp => comp != null ) ;

            EnsureTimedUnload ( 2 );

            return directComp;
        }

        public static void EnsureTimedUnload ( int numFrames = 2 )
        {
            // Wenn der Unloader noch nicht läuft, registrieren wir ihn
            if ( m_FrameToUnload < 0 )
            {
                Canvas.willRenderCanvases += MyUnloader;
            }

            // Berechnet den Ziel-Frame im Voraus (und schiebt ihn bei Mehrfachaufrufen nach hinten)
            m_FrameToUnload = Mathf.Max ( m_FrameToUnload , Time.frameCount + numFrames );
        }

        private static void MyUnloader ()
        {
            // Sobald wir den Ziel-Frame erreicht (oder überschritten) haben
            //Log.i ( TAG , "MyUnloader ()" ) ;
            if ( Time.frameCount >= m_FrameToUnload )
            {
                Canvas.willRenderCanvases -= MyUnloader;
                m_FrameToUnload = -1; // Bereit für den nächsten Zyklus

                if ( m_PrefabAssemblage != null )
                {
                    //Log.i ( TAG , "unloading ..." ) ;
                    m_PrefabAssemblage = null;
                    Resources.UnloadUnusedAssets ();
                }
            }
        }
    }
}
