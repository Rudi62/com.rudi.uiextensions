using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.Drawing;
using Rudi.RMath;
using Unity.Collections;
using UnityEngine;

namespace Rudi.UI
{
    public class Stats_Icon : RawImageSupplier
    {
        private static readonly string TAG = "Rudis Stats_Icon" ;

        [ Range ( 0 , 5 ) ]
        [ SerializeField ] private float m_LineWidth = 2f ;
        [ Range ( 0 , 5 ) ]
        [ SerializeField ] private float m_CornerRadius = 1f ;
        [ Range ( 1 , 6 ) ]
        [ SerializeField ] private float m_Bar1 = 1.5f ;
        [ Range ( 1 , 6 ) ]
        [ SerializeField ] private float m_Bar2 = 3.5f ;
        [ Range ( 1 , 6 ) ]
        [ SerializeField ] private float m_Bar3 = 2.5f ;
        [ Range ( 1 , 6 ) ]
        [ SerializeField ] private float m_Bar4 = 5.5f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_Scale = 1f ;

        protected override void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ); }

        private static Texture2D [] m_Textures = new Texture2D [ NumCachedTextures ] ;
        protected override Texture2D [ ] textureArray => m_Textures;


        private Arc m_Arc = null ;
        private Arc createArc ()
        {
            //m_Arc = new Arc ( Vector2.zero , 0.5f * m_Scale , 180f , 330f ); // 315
            var radius = m_CornerRadius * m_Scale ;
            return new Arc ( Vector2.one * radius , radius , 180f , 90f ); // 315
        }
        private Arc arc => Utils.createIfZero ( ref m_Arc , createArc );

        private LineList m_LineList = null ;
        private LineList lineList => Utils.createIfZero ( ref m_LineList , calcLineList );

        private Vector2 m_Shift ;
        private float m_Scale2 ;

        private LineList calcLineList ()
        {
            var points = new Vector2 [ 12 ] ;

            var ArcStartPoint = m_Arc.startPoint ;
            var ArcEndPoint   = m_Arc.endPoint   ;

            //float ArrowLength = m_Scale * 0.5f * ( 1.0f - 0.5f * Mathf.Sqrt ( 2f ) ) ;

            //float ArrowLength = ( m_Scale * 0.5f - ArcEndPoint.y ) * 1f ;
            var z = 0 ;
            points [z++] = ArcStartPoint;
            points [z++] = Vector2.up * 8f;
            points [z++] = ArcEndPoint;
            points [z++] = Vector2.right * 9f;

            //float L1 = 1.5f ;
            //float L2 = 3.5f ;
            //float L3 = 2.5f ;
            //float L4 = 5.5f ;

            // 4 bars
            points [z++] = new Vector2 ( 2f , 2f );
            points [z++] = new Vector2 ( 2f , 2f + m_Bar1 );
            points [z++] = new Vector2 ( 4f , 2f );
            points [z++] = new Vector2 ( 4f , 2f + m_Bar2 );
            points [z++] = new Vector2 ( 6f , 2f );
            points [z++] = new Vector2 ( 6f , 2f + m_Bar3 );
            points [z++] = new Vector2 ( 8f , 2f );
            points [z++] = new Vector2 ( 8f , 2f + m_Bar4 );

            var Result = LineList.FromPointList_AsList ( points ) ;
            m_Shift = Result.shiftToMidPoint ();
            //Result.scaleToMatch ( 1f );
            m_Scale2 = Result.scaleToMatch ( m_Scale );
            //Log.i ( TAG , "m_Scale2 = " + m_Scale2 ) ;
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
            var ScaleR = MinWidth / UsableWidth ;
            var Scale = 0.5f / UsableWidth ;
            var Pixels = tex.GetPixelData < Color32 > ( 0 ) ;

            for ( var iy = 0 ; iy < iheight ; iy++ )
            {
                var y = Scale * (    iy - halfHeight ) ;
                for ( var ix = 0 ; ix < iwidth ; ix++ )
                {
                    var x = Scale * (    ix - halfWidth ) ;
                    var p = new Vector2 ( x , y ) ;

                    var np_Arc = arc.NearestPoint ( p , m_Shift , m_Scale2 ) ;

                    var np_Lines = lineList.NearestPoint ( p ) ;

                    var nearest = Mathf.Min ( np_Arc.Dist2 , np_Lines.Dist2 ) ;
                    var dist = Mathf.Sqrt ( nearest ) / Scale - radius ;

                    //float alpha = 1f - Mathf.SmoothStep ( 0 , 1 , Mathf.Clamp01 ( ( mindist / Scale - radius ) ) ) ;
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
            m_Arc = null;
        }
#endif

    }
}
