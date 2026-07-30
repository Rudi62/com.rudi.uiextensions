using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Rudi.Drawing;
using Rudi.Core;
using Rudi.RMath;

namespace Rudi.UI
{
    public class Cancel_Icon : RawImageSupplier
    {
        private static readonly string TAG = "Rudis Cancel_Icon" ;

        [ Range ( 0 , 5 ) ]
        [ SerializeField ] private float m_LineWidth = 2f;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_Scale = 1f ;
        protected override void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ); }
        private static Texture2D [] m_Textures = new Texture2D [ NumCachedTextures ] ;
        protected override Texture2D [ ] textureArray => m_Textures;

        private LineList m_LineList = null ;
        private LineList lineList => Utils.createIfZero ( ref m_LineList , calcLineList );

        private LineList calcLineList ()
        {
            var points = new Vector2 [ 4 ] ;
            points [0] = new Vector2 ( -1 , 1 );
            points [1] = new Vector2 ( 1 , -1 );
            points [2] = new Vector2 ( -1 , -1 );
            points [3] = new Vector2 ( 1 , 1 );

            var Result = LineList.FromPointList_AsList ( points ) ;
            Result.scale ( m_Scale / 2f );
            return Result;
        }

        protected override bool calcTextureContent ( Texture2D tex )
        {
            log ( "calcTextureContent ()" );
            var iwidth  = tex.width ;
            var iheight = tex.height ;
            var halfWidth  = 0.5f * ( iwidth  - 1 ) ;
            var halfHeight = 0.5f * ( iheight - 1 ) ;
            var MinWidth = Mathf.Min ( halfWidth , halfHeight ) ;
            var lineWidth = m_LineWidth * gv.ppu ;
            var radius = 0.5f * lineWidth ;
            var UsableWidth = MinWidth - radius ;
            var Scale = 0.5f / UsableWidth ;
            var Pixels = tex.GetPixelData < Color32 > ( 0 ) ;

            for ( var iy = 0 ; iy < iheight ; iy++ )
            {
                var y = Scale * (    iy - halfHeight ) ;
                for ( var ix = 0 ; ix < iwidth ; ix++ )
                {
                    var x = Scale * (    ix - halfWidth ) ;
                    var p = new Vector2 ( x , y ) ;

                    var np_Lines = lineList.NearestPoint ( p ) ;

                    var dist = Mathf.Sqrt ( np_Lines.Dist2 ) / Scale - radius;

                    //float alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01((mindist / Scale - radius))) ;
                    ////alpha = 1f;
                    //Pixels [iy * iwidth + ix] = new Color ( 1 , 1 , 1 , alpha );
                    Pixels [iy * iwidth + ix] = RMathUtils.SmoothAlphaWhiteFromDist ( dist );
                }
            }
            tex.Apply ();
            return true;
        }

#if UNITY_EDITOR
        protected new void OnValidate ()
        {
            base.OnValidate ();
            //if ( Application.isPlaying ) return;
            m_LineList = null; // enforce recalculation
        }
#endif
    }
}
