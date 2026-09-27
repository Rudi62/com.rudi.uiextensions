using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rudi.RMath
{
    public static class SmoothStep
    {
        public enum Mode
        {
            Linear,
            Smooth1,
            Smooth2,
            EaseInExp,
            EaseOutExp,
            EaseIn4,
            EaseOut4,
        }
        private static float v0_impl ( float x ) => x;
        private static float v1_impl ( float x ) => ( 3.0f - 2.0f * x ) * x * x;
        private static float v2_impl ( float x ) => ( ( 6.0f * x - 15.0f ) * x + 10.0f ) * x * x * x;
        private static float ease_in2_impl ( float x ) => x * x;

        private static float ease_out2_impl ( float x ) => ( 2.0f - x ) * x;
        private static float ease_out3_impl ( float x ) => ( ( x - 3f ) * x + 3f ) * x; // x^3 - 3 x^2 + 3x

        //private static float ease_out4_impl ( float x ) => ( ( ( 4f - x ) * x - 6f ) * x + 4f ) * x ; // -x^4 + 4x^3 - 6 x^2 + 4x 
        private static float ease_out4_impl ( float x ) => ( ( 3f * x - 8f ) * x + 6f ) * x * x; // 3 x^4 - 8 x^3 + 6 x^2
        private static float ease_in4_impl ( float x ) => ( -3f * x + 4f ) * x * x * x; // -3 x^4 + 4 x^3 

        private static float ease_out5_impl ( float x ) => ( ( ( ( x - 5f ) * x + 10f ) * x - 10f ) * x + 5f ) * x; //  x (x (x ((x - 5) x + 10) - 10) + 5)

        private static float ease_in_pow_impl ( float x , float p ) => Mathf.Pow ( x , p );
        private static float ease_out_pow_impl ( float x , float p ) => 1 - Mathf.Pow ( 1 - x , p );
        private static float ease_in_exp_impl ( float x , float p )
        {
            var q = Mathf.Pow ( 10 , p ) ;
            return ( Mathf.Pow ( q , x ) - 1 ) / ( q - 1 );
        }
        private static float ease_out_exp_impl ( float x , float p ) => 1 - ease_in_exp_impl ( 1 - x , p );

        private static float cos_impl ( float x ) => ( 1.0f - Mathf.Cos ( x * Mathf.PI ) ) * 0.5f;
        private static float algebraicSigmoid_impl ( float x , float k ) => ( k * x ) / ( 1f + ( k - 1f ) * Mathf.Abs ( x ) ) ;

        public static float step ( Mode mode , float x , float p = 1.2f )
        {
            x = Mathf.Clamp01 ( x );
            switch ( mode )
            {
                case Mode.Linear: return x;
                case Mode.Smooth1: return v1_impl ( x );
                case Mode.Smooth2: return v2_impl ( x );
                case Mode.EaseInExp: return ease_in_exp_impl ( x , p );
                case Mode.EaseOutExp: return ease_out_exp_impl ( x , p );
                case Mode.EaseIn4: return ease_in4_impl ( x );
                case Mode.EaseOut4: return ease_out4_impl ( x );
                default: return x;
            }
        }

        public static float v0 ( float x ) => v0_impl ( Mathf.Clamp01 ( x ) );
        public static float v1 ( float x ) => v1_impl ( Mathf.Clamp01 ( x ) );
        public static float v2 ( float x ) => v2_impl ( Mathf.Clamp01 ( x ) );
        public static float ease_in_pow ( float x , float p ) => ease_in_pow_impl ( Mathf.Clamp01 ( x ) , p );
        public static float ease_out_pow ( float x , float p ) => ease_out_pow_impl ( Mathf.Clamp01 ( x ) , p );
        public static float ease_in_exp ( float x , float p ) => ease_in_exp_impl ( Mathf.Clamp01 ( x ) , p );
        public static float ease_out_exp ( float x , float p ) => ease_out_exp_impl ( Mathf.Clamp01 ( x ) , p );
        public static float ease_in2 ( float x ) => ease_in2_impl ( Mathf.Clamp01 ( x ) );
        public static float ease_out2 ( float x ) => ease_out2_impl ( Mathf.Clamp01 ( x ) );
        public static float ease_out3 ( float x ) => ease_out3_impl ( Mathf.Clamp01 ( x ) );
        public static float ease_out4 ( float x ) => ease_out4_impl ( Mathf.Clamp01 ( x ) );
        public static float ease_out5 ( float x ) => ease_out5_impl ( Mathf.Clamp01 ( x ) );
        public static float cos ( float x ) => cos_impl ( Mathf.Clamp01 ( x ) );
        public static float algebraicSigmoid ( float x , float k ) => algebraicSigmoid_impl ( Mathf.Clamp ( x , -1f , 1f ) , k ) ;
    }
}
