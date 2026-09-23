using System.Collections;
using System.Collections.Generic;
using Rudi.Extensions;
using UnityEngine;

namespace Rudi.Core
{
    public static class Webcams
    {
        private static readonly string TAG = "Rudis Webcams" ;
        public class Identifier
        {
            public string name = string.Empty ;
            public int width = 0 ;
            public int height = 0 ;
            public Identifier () { }
            public Identifier ( string name , int width , int height )
            {
                this.name = name;
                this.width = width;
                this.height = height;
            }
        }

        public static void queryCams ()
        {
            var devices = WebCamTexture.devices ;
            if ( null != devices )
            {
                foreach ( var device in devices )
                {
                    Log.i ( TAG , "device: " + device.name );
                    Log.i ( TAG , "kind: " + device.kind );
                    queryResolutions ( device );
                }
            }
        }

        public static void log ( Resolution r )
        {
            Log.i ( TAG , "resolution = " + r );
        }

        public static void queryResolutions ( WebCamDevice device )
        {
            var res = device.availableResolutions ;
            if ( null != res )
            {
                Log.i ( TAG , "Num resolutions: " + res.Length );
                foreach ( var r in res )
                {
                    Log.i ( TAG , $"resolutiion: { r }" );
                }
            }
            else
            {
                Log.i ( TAG , "No resolutions" );
            }
        }

        private static Identifier m_BestWebCam = null ;
        public static Identifier bestWebCam
        {
            get
            {
                if ( null == m_BestWebCam )
                {
#if UNITY_EDITOR
                    var devices = WebCamTexture.devices ;
                    if ( null == devices || devices.Length == 0 ) return null; // no devices
                    m_BestWebCam = new Identifier ( devices [0].name , 0 , 0 );
#else
                    m_BestWebCam = getBestWebCam () ;
#endif
                }
                return m_BestWebCam;
            }
        }

        public static int score ( WebCamKind kind )
        {
            switch ( kind ) // the more focal length, the better
            {
                case WebCamKind.UltraWideAngle: return 1; // worst choice
                case WebCamKind.WideAngle: return 2; // better
                case WebCamKind.Telephoto: return 3; // best
                default: return 0;
            }
        }

        private static bool isBetter ( WebCamKind newKind , WebCamKind oldKind )
        {
            return score ( newKind ) > score ( oldKind );
        }

        private static Identifier getBestWebCam () // Identifier WebCamTexture
        {
            //queryCams () ;
            WebCamDevice? bestDevice = null ;
            Resolution? bestResolution = null ;
            var bestKind = WebCamKind.UltraWideAngle ;
            var bestNumPixels = 0 ;
            var devices = WebCamTexture.devices ;
            if ( !Utils.hasData ( devices ) ) return null; // no devices
            foreach ( var device in devices )
            {
                if ( device.isFrontFacing ) continue; // no front camera

                if ( isBetter ( device.kind , bestKind ) ) // looking for high focal length
                {
                    bestKind = device.kind;
                    bestNumPixels = 0;
                }
                if ( isBetter ( bestKind , device.kind ) ) continue; // old kind is better than actual

                var res = device.availableResolutions ;
                if ( !Utils.hasData ( res ) ) continue;

                foreach ( var r in res )
                {
                    if ( r.NumPixels () > bestNumPixels )
                    {
                        bestDevice = device;
                        bestResolution = r;
                        bestNumPixels = r.NumPixels ();
                    }
                }
            }
            if ( null == bestDevice ) return null;
            log ( bestResolution.Value );
            return new Identifier ( bestDevice.Value.name , bestResolution.Value.width , bestResolution.Value.height );
        }

        public static WebCamTexture getBestWebCamTexture ()
        {
#if UNITY_EDITOR
            //WebCamDevice [] devices = WebCamTexture.devices ;
            //if ( null == devices || devices.Length == 0 ) return null ; // no devices
            //return new WebCamTexture ( devices[0].name ) ;
            return new WebCamTexture ();
#else
            var id = bestWebCam ;
            return new WebCamTexture ( id.name , id.width , id.height ) ;
#endif
        }
    }
}
