using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Rudi.Drawing;
using UnityEditor;
using Rudi.Core;
using Rudi.RMath;

namespace Rudi.UI
{
    public class Arrow_Icon : RawImageSupplier
    {
        private static readonly string TAG = "Rudis Arrow_Icon" ;

        public enum Direction
        {
            Left,
            Right,
            Up,
            Down,
            X,
        }

        [ SerializeField ] private Direction m_Direction = Direction.Left ;
        [ Range ( 0 , 10 ) ]
        [ SerializeField ] private float m_LineWidth = 2f ;
        [ SerializeField ] private bool m_DrawCenterLine = false ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_ArrowSize = 0.5f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_Scale = 1f ;

        private void setDirty ()
        {
            m_LineList = null;
            setTextureDirty ();
        }

        public Direction direction
        {
            get => m_Direction;
            set => setVal ( ref m_Direction , value );
        }

        public float lineWidth
        {
            get => m_LineWidth;
            set => setVal ( ref m_LineWidth , value );
        }

        public float scale
        {
            get => m_Scale;
            set => setVal ( ref m_Scale , value );
        }

        private void setVal<T> ( ref T var , T val )
        {
            if ( !var.Equals ( val ) )
            {
                var = val;
                setDirty ();
            }
        }

        protected override void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ); }

        private static Texture2D [] m_Textures = new Texture2D [ NumCachedTextures ] ;
        protected override Texture2D [ ] textureArray => m_Textures;

        private LineList m_LineList = null ;
        private LineList lineList => Utils.createIfZero ( ref m_LineList , calcLineList );

        static readonly Vector2 [] PointArray =
        {

            // 4  3  2
            // 5  0  1
            // 6  7  8

            new Vector2 (  0 ,  0 ) , // 0
            new Vector2 (  1 ,  0 ) , // 1
            new Vector2 (  1 ,  1 ) , // 2
            new Vector2 (  0 ,  1 ) , // 3
            new Vector2 ( -1 ,  1 ) , // 4
            new Vector2 ( -1 ,  0 ) , // 5
            new Vector2 ( -1 , -1 ) , // 6
            new Vector2 (  0 , -1 ) , // 7
            new Vector2 (  1 , -1 ) , // 8

        } ;

        static readonly int [] indices_Arrow_Left  = { 2 , 8 , 1 } ;
        static readonly int [] indices_Arrow_Right = { 4 , 6 , 5 } ;
        static readonly int [] indices_Arrow_Up    = { 6 , 8 , 7 } ;
        static readonly int [] indices_Arrow_Down  = { 2 , 4 , 3 } ;
        static readonly int [] indices_Arrow_X     = { 4 , 8 , 6 , 2 } ;
        static int [ ] getArrowIndices ( Direction dir )
        {
            switch ( dir )
            {
                case Direction.Left: return indices_Arrow_Left;
                case Direction.Right: return indices_Arrow_Right;
                case Direction.Up: return indices_Arrow_Up;
                case Direction.Down: return indices_Arrow_Down;
                case Direction.X: return indices_Arrow_X;
                default: return null;
            }
        }

        private int getArraySize ()
        {
            if ( Direction.X == m_Direction ) return 4;
            return m_DrawCenterLine ? 6 : 4;
        }

        private float getOverallScale ()
        {
            if ( Direction.X == m_Direction ) return m_Scale * 0.5f;
            return m_DrawCenterLine ? m_Scale : m_Scale * 0.5f;
        }

        private float getArrowScale ()
        {
            if ( Direction.X == m_Direction ) return 1f;
            return m_DrawCenterLine ? m_ArrowSize * 0.5f : 1f;
        }

        private LineList calcLineList ()
        {
            var NumPoints = getArraySize () ;

            var points = new Vector2 [ NumPoints ] ;

            var indArrow     = getArrowIndices ( m_Direction ) ;
            var ArrowScale = getArrowScale () ;
            var Scale      = getOverallScale () ;

            if ( Direction.X == m_Direction )
            {
                points [0] = PointArray [4];
                points [1] = PointArray [8];
                points [2] = PointArray [6];
                points [3] = PointArray [2];
            }
            else
            {
                points [0] = PointArray [0];
                points [1] = PointArray [indArrow [0]] * ArrowScale;
                points [2] = PointArray [0];
                points [3] = PointArray [indArrow [1]] * ArrowScale;
                if ( m_DrawCenterLine )
                {
                    points [4] = PointArray [0];
                    points [5] = PointArray [indArrow [2]];
                }
            }


            var Result = LineList.FromPointList_AsList ( points ) ;
            Result.scale ( Scale );
            Result.shiftToMidPoint ();
            return Result;
        }

        private LineList calcLineList1 ()
        {
            var points = new Vector2 [ 4 ] ;
            points [0] = new Vector2 ( -1 , 1 );
            points [1] = new Vector2 ( 1 , -1 );
            points [2] = new Vector2 ( -1 , -1 );
            points [3] = new Vector2 ( 1 , 1 );

            switch ( m_Direction )
            {
                case Direction.Left: points [0] = Vector2.zero; points [2] = Vector2.zero; break;
                case Direction.Right: points [1] = Vector2.zero; points [3] = Vector2.zero; break;
                case Direction.Up: points [0] = Vector2.zero; points [3] = Vector2.zero; break;
                case Direction.Down: points [1] = Vector2.zero; points [2] = Vector2.zero; break;
            }

            var Result = LineList.FromPointList_AsList ( points ) ;
            Result.scale ( m_Scale / 2f );
            Result.shiftToMidPoint ();
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

                    var dist = Mathf.Sqrt ( np_Lines.Dist2 ) / Scale - radius ;

                    //float alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01((mindist / Scale - radius))) ;
                    //float alpha = dist >= 0f ? 0f : 1f ;
                    //Pixels [iy * iwidth + ix] = new Color ( 1 , 1 , 1 , alpha );
                    //dist = Mathf.Sign ( dist ) * 2 ;
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
            if ( Application.isPlaying ) return;
            m_LineList = null; // enforce recalculation
        }
#endif
    }
}
