using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.RMath;
using Unity.Collections;
using UnityEngine;

namespace Rudi.UI
{
    public class ThreeDots : RawImageSupplier
    {
        private static readonly string TAG = "Rudis ThreeDots" ;

        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_PointSize = 0.5f ;

        private static Texture2D [] m_Textures = new Texture2D [ NumCachedTextures ] ;
        protected override Texture2D [ ] textureArray => m_Textures;

        //private static float dist ( Vector2 center , Vector2 p ) => ( p - center ) . magnitude ;

        protected override void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ); }


        protected override bool calcTextureContent ( Texture2D tex )
        {
            log ( "calcTextureContent ()" );
            var iwidth  = tex.width ;
            var iheight = tex.height ;
            var halfWidth  = 0.5f * ( iwidth  - 1 ) ;
            var halfHeight = 0.5f * ( iheight - 1 ) ;
            var radius = m_PointSize * halfHeight / 3f ;
            var dot_0 = new Vector2 ( 0 , halfHeight * 2f / 3f ) ;
            var dot_1 = new Vector2 ( 0 , 0 ) ;
            var dot_2 = new Vector2 ( 0 , -halfHeight * 2f / 3f ) ;
            var Pixels = tex.GetPixelData < Color32 > ( 0 ) ;
            var SymmLine = halfHeight / 3f;

            for ( var iy = 0 ; iy < iheight ; iy++ )
            {
                var y =    iy - halfHeight ;
                if ( y > SymmLine ) y -= SymmLine * 2f;
                else if ( y < -SymmLine ) y += SymmLine * 2f;

                for ( var ix = 0 ; ix < iwidth ; ix++ )
                {
                    var x =    ix - halfWidth ;
                    var p = new Vector2 ( x , y ) ;
                    var Dist = Vector2.Distance ( dot_1 , p ) - radius ;
                    //float alpha = Mathf.SmoothStep ( 1 , 0 , Dist ) ;
                    //float alpha = RMath.SmoothStep ( 0.5f , -0.5f , Dist ) ;
                    //Pixels [ iy * iwidth + ix ] = new Color ( 1 , 1 , 1 , alpha ) ;
                    Pixels [iy * iwidth + ix] = RMathUtils.SmoothAlphaWhiteFromDist ( Dist );
                }
            }
            tex.Apply ();
            return true;
        }
    }
}
