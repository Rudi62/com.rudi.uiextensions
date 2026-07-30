using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    [ExecuteAlways]
    public class HueTexture : MonoBehaviour
    {
        private static readonly string TAG = "Rudis HueTexture" ;

        private const int width  =  2 ;
        private const int height = 32 ;

        public enum TextureToApply
        {
            None,
            Hue,
            Sat,
            Val,
        }

        [ SerializeField ] private TextureToApply m_TextureToApply = TextureToApply.None ;

        private static Texture2D m_TextureHue = null ;
        private static Texture2D textureHue
        {
            get
            {
                if ( null == m_TextureHue )
                {
                    m_TextureHue = calcTextureHue ();
                    if ( null != m_TextureHue )
                    {
                        DontDestroyOnLoad ( m_TextureHue );
                    }
                }
                return m_TextureHue;
            }
        }
        private static Texture2D m_TextureSat = null ;
        private static Texture2D textureSat
        {
            get
            {
                if ( null == m_TextureSat )
                {
                    m_TextureSat = calcTextureSat ();
                    if ( null != m_TextureSat )
                    {
                        DontDestroyOnLoad ( m_TextureSat );
                        updateSat ();
                    }
                }
                return m_TextureSat;
            }
        }

        private static Texture2D m_TextureVal = null ;
        private static Texture2D textureVal
        {
            get
            {
                if ( null == m_TextureVal )
                {
                    m_TextureVal = calcTextureVal ();
                    if ( null != m_TextureVal )
                    {
                        DontDestroyOnLoad ( m_TextureVal );
                        updateVal ();
                    }
                }
                return m_TextureVal;
            }
        }

        private static Texture2D calcTextureHue ()
        {
            // https://de.wikipedia.org/wiki/Normalverteilung
            var Result = new Texture2D ( width , height , TextureFormat.RGBA32 , false ) ;
            var pixels = Result.GetPixelData < Color32 > ( 0 ) ;
            var by = 1f / ( height - 1 ) ;
            for ( var iy = 0 ; iy < height ; iy++ )
            {
                var fy = iy * by ;
                var col = Color.HSVToRGB ( fy , 1 , 1 ) ;
                for ( var ix = 0 ; ix < width ; ix++ )
                {
                    pixels [iy * width + ix] = col;
                }
            }
            Result.Apply ();
            Result.hideFlags = HideFlags.HideAndDontSave;
            Result.filterMode = FilterMode.Bilinear;
            Result.wrapMode = TextureWrapMode.Clamp;
            return Result;
        }

        private static Texture2D calcTextureSat ()
        {
            // https://de.wikipedia.org/wiki/Normalverteilung
            var Result  = new Texture2D ( width , height , TextureFormat.RGBA32 , false ) ;
            Result.hideFlags = HideFlags.HideAndDontSave;
            Result.filterMode = FilterMode.Bilinear;
            Result.wrapMode = TextureWrapMode.Clamp;
            return Result;
        }

        private static Texture2D calcTextureVal ()
        {
            // https://de.wikipedia.org/wiki/Normalverteilung
            var Result = new Texture2D ( width , height , TextureFormat.RGBA32 , false ) ;
            Result.hideFlags = HideFlags.HideAndDontSave;
            Result.filterMode = FilterMode.Bilinear;
            Result.wrapMode = TextureWrapMode.Clamp;
            return Result;
        }

        private static void updateSat ()
        {
            var tex = m_TextureSat ;
            if ( null == tex ) return;
            var pixels = tex.GetPixelData < Color32 > ( 0 ) ;
            var by = 1f / ( height - 1 ) ;
            for ( var iy = 0 ; iy < height ; iy++ )
            {
                var fy = iy * by ;
                var col = ColorTools.HSLToRGB ( hue , fy , val ) ;
                for ( var ix = 0 ; ix < width ; ix++ )
                {
                    pixels [iy * width + ix] = col;
                }
            }
            tex.Apply ();
        }

        private static void updateVal ()
        {
            var tex = m_TextureVal ;
            if ( null == tex ) return;
            var pixels = tex.GetPixelData < Color32 > ( 0 ) ;
            var by = 1f / ( height - 1 ) ;
            for ( var iy = 0 ; iy < height ; iy++ )
            {
                var fy = iy * by ;
                var col = ColorTools.HSLToRGB ( hue , sat , fy ) ;
                for ( var ix = 0 ; ix < width ; ix++ )
                {
                    pixels [iy * width + ix] = col;
                }
            }
            tex.Apply ();
        }

        private static float m_Hue = 0 ;
        private static float m_Sat = 0 ;
        private static float m_Val = 1 ;

        public static float hue
        {
            get => m_Hue;
            set
            {
                if ( m_Hue == value ) return;
                m_Hue = value;
                updateSat ();
                updateVal ();
            }
        }

        public static float sat
        {
            get => m_Sat;
            set
            {
                if ( m_Sat == value ) return;
                m_Sat = value;
                updateVal ();
            }
        }

        public static float val
        {
            get => m_Val;
            set
            {
                if ( m_Val == value ) return;
                m_Val = value;
                updateSat ();
            }
        }

        private RawImage m_Image = null ;
        private RawImage image
        {
            get
            {
                if ( null == m_Image )
                {
                    m_Image = GetComponent<RawImage> ();
                }
                return m_Image;
            }
        }

        private Texture2D toApply ()
        {
            switch ( m_TextureToApply )
            {
                case TextureToApply.Hue: return textureHue;
                case TextureToApply.Sat: return textureSat;
                case TextureToApply.Val: return textureVal;
                default: return null;
            }
        }

        private void Start ()
        {
            if ( null == image ) return;
            image.texture = toApply ();
        }
    }
}
