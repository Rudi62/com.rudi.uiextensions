using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rudi.RMath
{
    public static class RMathUtils
    {
        private static readonly string TAG = "Rudis RMath";

        public static float Sqr ( float v ) => v * v;
        public static double Sqr ( double v ) => v * v;
        public static int Sqr ( int v ) => v * v;
        public static uint Sqr ( uint v ) => v * v;
        public static float GetSol_2_OfSqrEq ( float a , float b , float c , float false_val = 0 )
        {
            var d = b * b - 4 * a * c ;
            if ( d < 0 ) return false_val;
            return ( -b + Mathf.Sqrt ( d ) ) / ( 2 * a );
        }
        public struct FixedPoint_uint
        {
            public readonly uint RawVal;
            private const int shift = 24;
            public FixedPoint_uint ( uint val )
            {
                RawVal = val << shift;
            }
            public static implicit operator uint ( FixedPoint_uint fp ) => fp.RawVal >> shift;
            public static implicit operator FixedPoint_uint ( uint v ) => new FixedPoint_uint ( v );
        }
        public static float LinStep ( float lo , float hi , float val ) => Mathf.Clamp01 ( ( val - lo ) / ( hi - lo ) );
        public static float LinStep1 ( float lo , float val ) => Mathf.Clamp01 ( val - lo );
        public static float LinStepMinus1 ( float lo , float val ) => Mathf.Clamp01 ( lo - val );
        public static float LinStepLoByWidth ( float lo , float bywidth , float val ) => Mathf.Clamp01 ( ( val - lo ) * bywidth );
        public static float SmoothStep ( float x ) => x * x * ( 3f - 2f * x );
        public static float SmootherStep ( float x ) => ( ( 6f * x - 15f ) * x + 10f ) * x * x * x;
        public static float SmoothStep ( float lo , float hi , float val ) => SmoothStep ( LinStep ( lo , hi , val ) );
        public static float SmoothStep1 ( float lo , float val ) => SmoothStep ( LinStep1 ( lo , val ) );
        public static float SmoothStepMinus1 ( float lo , float val ) => SmoothStep ( LinStepMinus1 ( lo , val ) );
        public static float SmoothStepLoByWidth ( float lo , float bywidth , float val ) => SmoothStep ( LinStepLoByWidth ( lo , bywidth , val ) );
        public static float SmootherStep ( float lo , float hi , float val ) => SmootherStep ( LinStep ( lo , hi , val ) );
        public static float SmoothAlphaFromDist ( float dist ) => SmoothStepMinus1 ( 0.5f , dist );
        //{
        //    return SmoothStepLoByWidth ( 0.5f , -1.0f , dist ) ;
        //}
        public static Color SmoothAlphaWhiteFromDist ( float dist ) => new Color ( 1 , 1 , 1 , SmoothAlphaFromDist ( dist ) );
    }
}
