using System.Collections;
using System.Collections.Generic;
using Rudi.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rudi.Core
{
    public static class ColorTools
    {
        private static readonly string TAG = "Rudis ColorTools";

        // Colors in Geo-Quest:
        // Light  : ECEFF4 
        // Button : 3F87C2
        // GeoQuest Button: 5185BE

        // https://www.color-hex.com/color-palette/77667

        //public class ColorSet
        //{
        //    public int Hue       = 110 ;
        //    public int SatDark   =  55 ;
        //    public int ValDark   =  29 ;
        //    public int SatMed    =  50 ;
        //    public int ValMed    =  35 ;
        //    public int SatButton =  40 ;
        //    public int ValButton =  48 ;
        //    public int SatLight  =   5 ;
        //    public int ValLight  =  93 ;
        //}
        public static Color HSVToRGB ( int h , int s , int v ) => Color.HSVToRGB ( h / 360f , s / 100f , v / 100f );

        public static Color HSLToRGB ( int h , int s , int l ) => HSLToRGB ( h / 360f , s / 100f , l / 100f );

        public static (float h, float s, float l) RGBToHSL ( int r , int g , int b ) => RGBToHSL ( r / 255f , g / 255f , b / 255f );
        public static (float h, float s, float l) RGBToHSL ( Color32 c ) => RGBToHSL ( c.r , c.g , c.b );
        public static (float h, float s, float l) RGBToHSL ( uint c ) => RGBToHSL ( UInt_RGB_ToColor32 ( c ) );

        public static (float h, float s, float l) ColorToHSL ( Color c ) => RGBToHSL ( c );
        public static (float h, float s, float v) ColorToHSV ( Color c )
        {
            float h,s,v ;
            Color.RGBToHSV ( c , out h , out s , out v );
            return (h, s, v);
        }

        public static Color GetWithHue ( Color c , float hue )
        {
            float h,s,v ;
            Color.RGBToHSV ( c , out h , out s , out v );
            return Color.HSVToRGB ( hue , s , v ).GetWithAlpha ( c.a );
        }

        static float getDrilled ( float val , float drill )
        {
            if ( drill < 0f )
            {
                return val * ( 1 + drill );
            }
            else
            {
                return ( 1f - val ) * drill + val;
            }
        }

        public static Color getLightenedColor ( Color color , float saturating , float lightning )
        {
            var colHSL = RGBToHSL ( color ) ;
            var hue = colHSL.h ;
            var sat = colHSL.s ;
            var light = colHSL.l ;
            //float gradedSat   = ( 1f - sat ) * lightning * saturating + sat ;
            //float gradedLight = ( 1f - light ) * lightning + light ;
            var gradedSat = getDrilled ( sat , saturating ) ;
            var gradedLight = getDrilled ( light , lightning ) ;


            return HSLToRGB ( hue , gradedSat , gradedLight );
        }

        public static Color HSLToRGB ( float h , float s , float l )
        {
            // h s l are in range of [ 0 , 1 ]
            // https://stackoverflow.com/questions/2353211/hsl-to-rgb-color-conversion
            float r, g , b ;

            if ( s == 0.0f )
                r = g = b = l;

            else
            {
                // gelbfärbung
                //if ( h == 110 / 360f || true )
                //{
                //	h = Mathf.Clamp01 ( h - ( l * 0.05f ) ) ;
                //}
                // konvertierung
                var q = l < 0.5f ? l * ( 1.0f + s ) : l + s - l * s ;
                var p = 2.0f * l - q ;
                r = HueToRgb ( p , q , h + 1.0f / 3.0f );
                g = HueToRgb ( p , q , h );
                b = HueToRgb ( p , q , h - 1.0f / 3.0f );
            }

            return new Color ( r , g , b , 1f );
        }
        // Helper for HslToRgba
        private static float HueToRgb ( float p , float q , float t )
        {
            if ( t < 0.0f ) t += 1.0f;
            if ( t > 1.0f ) t -= 1.0f;
            if ( t < 1.0f / 6.0f ) return p + ( q - p ) * 6.0f * t;
            if ( t < 1.0f / 2.0f ) return q;
            if ( t < 2.0f / 3.0f ) return p + ( q - p ) * ( 2.0f / 3.0f - t ) * 6.0f;
            return p;
        }
        public static (float h, float s, float l) RGBToHSL ( float r , float g , float b )
        {
            var vmax = Mathf.Max ( r , g , b ) ;
            var vmin = Mathf.Min ( r , g , b ) ;
            var l = ( vmax + vmin ) * 0.5f ;
            if ( vmax == vmin ) return (0, 0, l);
            var d = vmax - vmin ;
            var s = l > 0.5f ? d / ( 2 - vmax - vmin ) : d / ( vmax + vmin ) ;
            float h = 0 ;
            if ( vmax == r ) h = ( g - b ) / d + ( g < b ? 6 : 0 );
            if ( vmax == g ) h = ( b - r ) / d + 2;
            if ( vmax == b ) h = ( r - g ) / d + 4;
            h /= 6.0f;
            return (h, s, l);
        }

        public static uint ToUINT_ARGB ( Color32 c )
        {
            uint result = c.a ;
            result = result << 8 | c.r;
            result = result << 8 | c.g;
            result = result << 8 | c.b;
            return result;
        }
        public static uint ToUINT_RGBA ( Color32 c )
        {
            uint result = c.r ;
            result = result << 8 | c.g;
            result = result << 8 | c.b;
            result = result << 8 | c.a;
            return result;
        }
        public static Color32 UInt_RGB_ToColor32 ( uint v )
        {
            var b = ( byte )  v  ;
            var g = ( byte ) ( v >> 8 ) ;
            var r = ( byte ) ( v >> 16 ) ;
            return new Color32 ( r , g , b , 255 );
        }
        public static Color32 UInt_ARGB_ToColor32 ( uint v )
        {
            var b = ( byte )  v  ;
            var g = ( byte ) ( v >> 8 ) ;
            var r = ( byte ) ( v >> 16 ) ;
            var a = ( byte ) ( v >> 24 ) ;
            return new Color32 ( r , g , b , a );
        }
        public static Color32 UInt_RGBA_ToColor32 ( uint v )
        {
            var a = ( byte )  v  ;
            var b = ( byte ) ( v >> 8 ) ;
            var g = ( byte ) ( v >> 16 ) ;
            var r = ( byte ) ( v >> 24 ) ;
            return new Color32 ( r , g , b , a );
        }
    }
}
