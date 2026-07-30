using UnityEngine;

namespace Rudi.Core
{
    public static class RectUtils
    {

        public class SMinMax2
        {
            private float [] data = new float [ 4 ] ;
            public float xMin { get => data [0]; set => data [0] = value; }
            public float yMin { get => data [1]; set => data [1] = value; }
            public float xMax { get => data [2]; set => data [2] = value; }
            public float yMax { get => data [3]; set => data [3] = value; }
            public float this [int i]
            {
                get => data [i];
                set => data [i] = value;
            }

            public SMinMax2 getPositive ()
            {
                var result = new SMinMax2 () ;
                result.xMin = Mathf.Max ( xMin , 0.0f );
                result.yMin = Mathf.Max ( yMin , 0.0f );
                result.xMax = Mathf.Max ( xMax , 0.0f );
                result.yMax = Mathf.Max ( yMax , 0.0f );
                return result;
            }

            public SMinMax2 () { }

            public SMinMax2 ( float xMin , float yMin , float xMax , float yMax )
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }

            public SMinMax2 ( Rect rect )
            {
                xMin = rect.xMin;
                yMin = rect.yMin;
                xMax = rect.xMax;
                yMax = rect.yMax;
            }

            public static SMinMax2 operator * ( SMinMax2 a , float b )
            {
                return new SMinMax2 ( a.xMin * b , a.yMin * b , a.xMax * b , a.yMax * b );
            }

            public static readonly SMinMax2 zero = new SMinMax2 ( 0 , 0 , 0 , 0 ) ;
            public static readonly SMinMax2 one  = new SMinMax2 ( 1 , 1 , 1 , 1 ) ;
            public static readonly SMinMax2 uv   = new SMinMax2 ( 0 , 0 , 1 , 1 ) ;
        }

        public class RectVertices
        {
            SMinMax2 v  = new SMinMax2 () ;
            SMinMax2 uv = new SMinMax2 () ;
            public RectVertices ( SMinMax2 v , SMinMax2 uv )
            {
                this.v = v;
                this.uv = uv;
            }
            public RectVertices ( Rect rect , SMinMax2 uv )
            {
                v = new SMinMax2 ( rect );
                this.uv = uv;
            }
            //   1---2
            //   |   |
            //   0---3
            private readonly static int [] X = { 0 , 0 , 2 , 2 } ; // xMin xMin xMax xMax
            private readonly static int [] Y = { 1 , 3 , 3 , 1 } ; // yMin yMax yMax yMin
            public Vector3 getV ( int i ) => new Vector3 ( v [X [i]] , v [Y [i]] );
            public Vector4 getUV ( int i ) => new Vector2 ( uv [X [i]] , uv [Y [i]] );
        }

        public static SMinMax2 getWidenedMinMax ( Rect rect , SMinMax2 w )
        {
            var result = new SMinMax2 () ;
            result.xMin = rect.xMin - w.xMin;
            result.yMin = rect.yMin - w.yMin;
            result.xMax = rect.xMax + w.xMax;
            result.yMax = rect.yMax + w.yMax;
            return result;
        }

        public static SMinMax2 getWidenedUV ( Rect rect_01 , SMinMax2 w )
        {
            var result = new SMinMax2 () ;
            var dx = 1.0f / rect_01.width  ;
            var dy = 1.0f / rect_01.height ;
            result.xMin = -w.xMin * dx;
            result.yMin = -w.yMin * dy;
            result.xMax = 1f + w.xMax * dx;
            result.yMax = 1f + w.yMax * dy;
            return result;
        }

        public static RectVertices getVertices ( Rect rect_01 ) => new RectVertices ( rect_01 , SMinMax2.uv );

        public static RectVertices getVertices ( Rect rect_01 , SMinMax2 w )
        {
            w = w.getPositive ();
            //w *= 0.5f;
            var v  = getWidenedMinMax ( rect_01 , w ) ;
            var uv = getWidenedUV     ( rect_01 , w ) ;
            return new RectVertices ( v , uv );
        }

        public static RectVertices getVertices ( Rect rect_01 , float w_lo_x , float w_lo_y , float w_hi_x , float w_hi_y )
        {
            return getVertices ( rect_01 , new SMinMax2 ( w_lo_x , w_lo_y , w_hi_x , w_hi_y ) );
        }
    }
}
