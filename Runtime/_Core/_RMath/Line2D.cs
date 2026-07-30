using UnityEngine;

namespace Rudi.RMath
{
    public class Line2D
    {
        public readonly Vector2 p1 ;
        public readonly Vector2 p2 ;
        public readonly Vector2 dir ;
        public readonly float LDir2 ;
        public readonly float ByLDir2 ;
        //public Vector2 p2 => p1 + dir ;
        public float getDistance2 ( Vector2 p ) => nearestPoint ( p ).dist2;
        public (Vector2 nearest, float dist2, bool outside) nearestPoint ( Vector2 p )
        {
            if ( !valid ) return (p1, ( p - p1 ).sqrMagnitude, true);
            var diff = p - p1 ;
            var t = Vector2.Dot ( diff , dir ) * ByLDir2 ;
            var t_clamped = Mathf.Clamp01 ( t ) ;
            var outside =  t != t_clamped  ;
            var PNear = p1 + dir * t_clamped ;
            var Dist2 = ( PNear - p ) . sqrMagnitude ;
            return (PNear, Dist2, outside);
        }
        public Line2D ( Vector2 p1 , Vector2 p2 )
        {
            this.p1 = p1;
            this.p2 = p2;
            dir = p2 - p1;
            LDir2 = dir.sqrMagnitude;
            ByLDir2 = LDir2 > 0f ? 1.0f / LDir2 : 0.0f;
        }
        public (bool crosses, float x) crossesXAxis ()
        {
            if ( dir.y == 0.0f ) return (false, p1.x);
            var crosses =  p1.y >= 0  ==  p2.y < 0  ;
            float x = 0 ;
            if ( crosses )
            {
                var rel = p1.y / dir.y ;
                x = p1.x - rel * dir.x;
            }
            return (crosses, x);
        }
        public static Line2D operator + ( Line2D line , Vector2 p ) => new Line2D ( line.p1 + p , line.p2 + p );
        public static Line2D operator - ( Line2D line , Vector2 p ) => new Line2D ( line.p1 - p , line.p2 - p );
        public bool valid => LDir2 > 0f;
    }
}
