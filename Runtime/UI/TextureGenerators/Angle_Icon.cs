using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.Drawing;
using Rudi.RMath;
using Unity.Collections;
using UnityEngine;

namespace Rudi.UI
{
    public class Angle_Icon : RawImageSupplier
    {
        private static readonly string TAG = "Rudis Angle_Icon" ;

        [ Range ( 0 , 5 ) ]
        [ SerializeField ] private float m_LineWidth = 2f ;
        [ Range ( 0 , 90 ) ]
        [ SerializeField ] private float m_Angle = 45f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_Radius = 0.62f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_ArcWidth = 0.5f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_Scale = 1f ;

        protected override void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ); }

        private static Texture2D [] m_Textures = new Texture2D [ NumCachedTextures ] ;
        protected override Texture2D [ ] textureArray => m_Textures;


        private Arc m_Arc = null ;
        private Arc arc
        {
            get
            {
                if ( null == m_Arc )
                {
                    m_Arc = new Arc ( Vector2.zero , m_Radius * m_Scale , 0f , m_Angle );
                }
                return m_Arc;
            }
        }

        private LineList m_LineList = null ;
        private LineList lineList
        {
            get
            {
                if ( null == m_LineList )
                {
                    m_LineList = calcLineList ();
                }
                return m_LineList;
            }
        }
        private Vector2 m_Shift ;
        private float m_Scale2 ;

        private LineList calcLineList ()
        {
            var points = new Vector2 [ 4 ] ;
            points [0] = Vector2.zero;
            points [1] = Vector2.right * m_Scale;
            points [2] = Vector2.zero;
            points [3] = new Vector2 ( Mathf.Cos ( Mathf.Deg2Rad * m_Angle ) , Mathf.Sin ( Mathf.Deg2Rad * m_Angle ) ) * m_Scale;

            var Result =  LineList.FromPointList_AsList ( points ) ;
            m_Shift = Result.shiftToMidPoint ();
            return Result;
        }


        protected override bool calcTextureContent ( Texture2D tex )
        {
            log ( "calcTextureContent ()" );
            //Log.i ( TAG , "gv.hasPPU = " + gv.hasPPU ) ;
            //Log.i ( TAG , "gv.ppu = " + gv.ppu ) ;
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
            //float Smooth = 0.5f;

            for ( var iy = 0 ; iy < iheight ; iy++ )
            {
                var y = Scale * (    iy - halfHeight ) ;
                for ( var ix = 0 ; ix < iwidth ; ix++ )
                {
                    var x = Scale * (    ix - halfWidth ) ;
                    var p = new Vector2 ( x , y ) ;

                    var np_Arc = arc.NearestPoint ( p , m_Shift , 1.0f ) ;

                    var np_Lines = lineList.NearestPoint ( p ) ;

                    var DistArc   = Mathf.Sqrt ( np_Arc.Dist2 ) / Scale - radius * m_ArcWidth ;
                    var DistLines = Mathf.Sqrt ( np_Lines.Dist2 ) / Scale - radius ;
                    var mindist   = Mathf.Min ( DistArc , DistLines ) ;

                    Pixels [iy * iwidth + ix] = RMathUtils.SmoothAlphaWhiteFromDist ( mindist );
                }
            }
            tex.Apply ();
            return true;
        }

#if UNITY_EDITOR
        protected new void OnValidate ()
        {
            base.OnValidate ();
            if ( Application.isPlaying ) return;
            m_LineList = null; // enforce recalculation
            m_Arc = null;
        }

#endif
    }
}
