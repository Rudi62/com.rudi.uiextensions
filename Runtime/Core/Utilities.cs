using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.Core.Utilities
{
    public static class Cyclic
    {
        public static float GetDiff ( float v1 , float v2 , float cycle )
        {
            var half = cycle * 0.5f;
            var Diff = v2 - v1;
            while ( Diff > half ) Diff -= cycle;
            while ( Diff < -half ) Diff += cycle;
            return Diff;
        }
        public static float GetNearest ( float fix , float var , float cycle )
        {
            return fix + GetDiff ( fix , var , cycle );
        }
        public static double GetDiff ( double v1 , double v2 , double cycle )
        {
            var half = cycle * 0.5;
            var Diff = v2 - v1;
            while ( Diff > half ) Diff -= cycle;
            while ( Diff < -half ) Diff += cycle;
            return Diff;
        }
        public static double GetNearest ( double fix , double var , double cycle )
        {
            return fix + GetDiff ( fix , var , cycle );
        }
        public static float GetAngleDiff ( float v1 , float v2 )
        {
            return GetDiff ( v1 , v2 , 360.0f );
        }
        public static float GetNearestAngle ( float fix , float var )
        {
            return GetNearest ( fix , var , 360.0f );
        }

        public static float GetWithin ( float v , float cycle )
        {
            while ( v < 0.0f ) v += cycle;
            while ( v >= cycle ) v -= cycle;
            return v;
        }
        public static float GetWithin ( float v , float min , float max )
        {
            var size = max - min;
            while ( v < min ) v += size;
            while ( v >= max ) v -= size;
            return v;
        }
        public static float GetWithin_m180_180 ( float v )
        {
            return GetWithin ( v , -180.0f , 180.0f );
        }

        public static float GetWithin360 ( float v )
        {
            return GetWithin ( v , 360.0f );
        }
        public static double GetWithin ( double v , double cycle )
        {
            while ( v < 0.0 ) v += cycle;
            while ( v >= cycle ) v -= cycle;
            return v;
        }
        public static double GetWithin ( double v , double min , double max )
        {
            var size = max - min;
            while ( v < min ) v += size;
            while ( v >= max ) v -= size;
            return v;
        }
        public static double GetWithin_m180_180 ( double v )
        {
            return GetWithin ( v , -180.0 , 180.0 );
        }

        public static int GetOffset ( float v , float cycle )
        {
            var n = 0;
            while ( cycle * n + v < 0.0f ) ++n;
            while ( cycle * n + v >= cycle ) --n;
            return n;
        }
    }

    public static class Angle
    {
        public static float getPlusMinus ( float angle ) => ( angle + 540f ) % 360f - 180f ;
    }

    public class Interpol
    {
        // input  : 0 ... 1
        // output : 0 ... 1
        public static double To_0_1 ( double minv , double maxv , double v ) { return ( v - minv ) / ( maxv - minv ); }
        public static double ToPiHalf ( double v ) { return Math.PI * 0.5 * v; }
        public static double ToPi ( double v ) { return Math.PI * v; }
        public static double CosQuarter ( double v ) { return 1.0 - Math.Cos ( ToPiHalf ( v ) ); }
        public static double CosHalf ( double v ) { return 0.5 - 0.5 * Math.Cos ( ToPi ( v ) ); }
        public static double Zero05_Cos ( double v ) { return ZeroT_Cos ( v , 0.5 ); }
        public static double Zero06_Cos ( double v ) { return ZeroT_Cos ( v , 0.6 ); }
        public static double Zero07_Cos ( double v ) { return ZeroT_Cos ( v , 0.7 ); }
        public static double ZeroT_Cos ( double v , double t )
        {
            if ( v < t ) return 0.0;
            return CosHalf ( To_0_1 ( t , 1.0 , v ) );
        }
    }
    public class Interpol2D
    {
        public static float BiCubic ( short [ ] inp , float tx , float ty ) // inp [ 16 ] , 4  rows, 4 columns
        {
            var fx0 = ( ( 2.0f - tx ) * tx - 1.0f ) * tx;        // -1
            var fx1 = ( 3.0f * tx - 5.0f ) * tx * tx + 2.0f;     //  0
            var fx2 = ( ( 4.0f - 3.0f * tx ) * tx + 1.0f ) * tx; // +1
            var fx3 = ( tx - 1.0f ) * tx * tx;                   // +2

            var fy0 = ( ( 2.0f - ty ) * ty - 1.0f ) * ty;        // -1
            var fy1 = ( 3.0f * ty - 5.0f ) * ty * ty + 2.0f;     //  0
            var fy2 = ( ( 4.0f - 3.0f * ty ) * ty + 1.0f ) * ty; // +1
            var fy3 = ( ty - 1.0f ) * ty * ty;                   // +2

            var l0 = inp [ 0 ] * fx0 + inp [ 1 ] * fx1 + inp [ 2 ] * fx2 + inp [ 3 ] * fx3;
            var l1 = inp [ 4 ] * fx0 + inp [ 5 ] * fx1 + inp [ 6 ] * fx2 + inp [ 7 ] * fx3;
            var l2 = inp [ 8 ] * fx0 + inp [ 9 ] * fx1 + inp [ 10 ] * fx2 + inp [ 11 ] * fx3;
            var l3 = inp [ 12 ] * fx0 + inp [ 13 ] * fx1 + inp [ 14 ] * fx2 + inp [ 15 ] * fx3;

            var res = ( l0 * fy0 + l1 * fy1 + l2 * fy2 + l3 * fy3 ) * 0.25f;
            return res;
        }
    }

    public class TimedValue
    {
        private static double Lerp ( double v1 , double v2 , double t ) => v1 + t * ( v2 - v1 );
        private double StartVal = 0;
        private double DestVal = 0;
        private double m_CurrentLerp = 0;
        private double m_CurrentValue = 0;
        private double m_DestTime = 3.0;
        public double m_TimeSum = 0.0;
        private double GetLerpOfTimeLinear ()
        {
            var LinearLerp = m_TimeSum / m_DestTime;

            return LinearLerp;
        }
        private double GetLerpOfTime ()
        {
            var LerpTimeLinear = GetLerpOfTimeLinear ();
            var LerpTime = Interpol.Zero06_Cos ( LerpTimeLinear );
            return LerpTime;
        }

        public TimedValue ( double startval , double destval , double desttime )
        {
            StartVal = startval;
            DestVal = destval;
            m_DestTime = desttime;
            m_TimeSum = 0.0;
            m_CurrentValue = startval;
        }
        public void Restart ()
        {
            m_TimeSum = 0.0;
            m_CurrentValue = StartVal;
        }
        public void Update ( double timediff )
        {
            m_TimeSum += timediff;
            if ( m_TimeSum < m_DestTime )
            {
                m_CurrentLerp = GetLerpOfTime ();
                m_CurrentValue = Lerp ( StartVal , DestVal , m_CurrentLerp );
            }
            else
            {
                m_CurrentLerp = 1.0;
                m_CurrentValue = DestVal;
            }
        }
        public void SetDestTime ( double val ) { m_DestTime = val; }
        public void SetValues ( double startval , double destval )
        {
            StartVal = startval;
            DestVal = destval;
        }
        public double Value { get => m_CurrentValue; }
        public double GetValue ( double startval , double destval )
        {
            return Lerp ( startval , destval , m_CurrentLerp );
        }
    }

    public class Smooth
    {
        private static readonly string TAG = "Rudis Smooth";

        private int m_SmoothVal;
        private double m_SmoothAlpha;
        private double m_Value = 0;
        //private int m_Counter = 0 ;
        //private readonly int m_IntroLength = 100 ;
        private double m_TimeSum = 0.0;
        private readonly double m_TimeToWorkFully = 3.0;

        public Smooth ()
        {
            SmoothStrength = 0;
        }

        public int SmoothStrength
        {
            get => m_SmoothVal;
            set => SetSmoothStrength ( value );
        }

        public static double Lerp ( double v1 , double v2 , double t ) => v1 + t * ( v2 - v1 );

        public static double CalcAlpha ( double v ) => 1.0 / Math.Pow ( 2.0 , 0.5 * v );
        private double GetLerpOfTime ()
        {
            var LinearLerp = m_TimeSum / m_TimeToWorkFully;
            return LinearLerp;
        }
        public void ResetTimer () { m_TimeSum = 0.0; }
        private static double GetTimedAlpha ( double alpha , double dt )
        {
            return 1.0 - Math.Pow ( 1.0 - alpha , dt );
        }
        private void SetSmoothStrength ( int v )
        {
            if ( v < 0 ) v = 0;
            m_SmoothVal = v;
            m_SmoothAlpha = CalcAlpha ( v );
        }
        public double GetSmoothed ( double val , double timediff = 1.0 )
        {
            m_TimeSum += timediff;
            var SmoothAlpha = m_SmoothAlpha;
            if ( m_TimeSum < m_TimeToWorkFully )
            {
                var LerpTimeLinear = GetLerpOfTime ();
                var LerpTime = Interpol.Zero06_Cos ( LerpTimeLinear );
                //DB.AddScreenText ( "Lerp" , LerpTime ) ;
                SmoothAlpha = CalcAlpha ( Lerp ( 0.001 , m_SmoothVal , LerpTime ) );
            }
            var Alpha = GetTimedAlpha ( SmoothAlpha , timediff );
            //Log.i ( TAG , $"Alpha = {Alpha} , SmoothAlpha = {SmoothAlpha}" );

            //DB.AddScreenText ( "AlphaT" , Alpha );
            m_Value = m_Value + ( val - m_Value ) * Alpha;
            return m_Value;
        }
        public float GetSmoothed ( float val , float timediff = 1.0f )
        {
            return ( float ) GetSmoothed ( val , ( double ) timediff );
        }
        public double GetValue () { return m_Value; }
        public void SetValue ( double v ) { m_Value = v; }
    }

    public class SmoothCyclic
    {
        private static readonly string TAG = "Rudis SmoothCyclic";

        private Smooth m_Smooth = new Smooth ();
        private readonly double m_Cycle;

        public SmoothCyclic ( double cycle = 360 )
        {
            m_Cycle = cycle;
        }
        public void ResetTimer ()
        {
            m_Smooth.ResetTimer ();
        }

        public int SmoothStrength
        {
            get => m_Smooth.SmoothStrength;
            set => m_Smooth.SmoothStrength = value;
        }

        public double GetSmoothed ( double val , double timediff = 1.0 )
        {
            var NearVal = Cyclic.GetNearest ( m_Smooth.GetValue () , val , m_Cycle );
            var NewVal = Cyclic.GetWithin ( m_Smooth.GetSmoothed ( NearVal , timediff ) , m_Cycle );
            m_Smooth.SetValue ( NewVal );
            //Log.i ( TAG , $"val = {val} , NewVal = {NewVal} , timediff = {timediff}" ) ;
            return NewVal;
        }

        public float GetSmoothed ( float val , float timediff = 1.0f )
        {
            return ( float ) GetSmoothed ( val , ( double ) timediff );
        }
        public double GetValue () { return m_Smooth.GetValue (); }
        public void SetValue ( double v ) { m_Smooth.SetValue ( v ); }
        public double value
        {
            get => GetValue ();
            set => SetValue ( value );
        }
    }

    class BuildingUpMean
    {
        private readonly int m_MaxNumIterations;
        private int m_NumIterations = -100;
        private double m_Value = 0;
        public BuildingUpMean ( int maxnumiterations )
        {
            m_MaxNumIterations = maxnumiterations;
        }
        public bool IsFinished () { return m_MaxNumIterations == m_NumIterations; }
        public bool HasData () { return m_NumIterations > 0; }
        public double Update ( double v )
        {
            if ( IsFinished () ) return m_Value;
            m_NumIterations++;
            if ( m_NumIterations > 0 )
            {
                m_Value += ( v - m_Value ) / m_NumIterations;
            }
            else
            {
                m_Value = v;
            }
            return m_Value;
        }
        public float Update ( float v ) { return ( float ) Update ( ( double ) v ); }
        public double GetValue () { return m_Value; }
        public float GetValueF () { return ( float ) GetValue (); }
    }

    class BuildingUpMeanCyclic
    {
        private readonly double m_Cycle;
        private readonly int m_MaxNumIterations;
        private int m_NumIterations = 0;
        private double m_Value = 0;
        public BuildingUpMeanCyclic ( int maxnumiterations , double cycle = 360 )
        {
            m_MaxNumIterations = maxnumiterations;
            m_Cycle = cycle;
        }
        public bool IsFinished () { return m_MaxNumIterations == m_NumIterations; }
        public bool HasData () { return m_NumIterations > 0; }
        public double Update ( double v )
        {
            if ( IsFinished () ) return m_Value;
            m_NumIterations++;
            var NewVal = Cyclic.GetNearest ( m_Value , v , m_Cycle );
            m_Value = Cyclic.GetWithin ( ( NewVal - m_Value ) / m_NumIterations + m_Value , m_Cycle );
            return m_Value;
        }
        public float Update ( float v ) { return ( float ) Update ( ( double ) v ); }
        public double GetValue () { return m_Value; }
        public float GetValueF () { return ( float ) GetValue (); }
    }

    public class DB
    {
        public static Text ScreenTextObject = null;

        private static void SetScreenText ( string str ) { if ( null != ScreenTextObject ) ScreenTextObject.text = str; }
        private static void AddToScreenText ( string str ) { if ( null != ScreenTextObject ) ScreenTextObject.text += str; }

        public static void ClearScreenText () => SetScreenText ( "" );
        public static void AddScreenText9 ( string label , double v ) => AddToScreenText ( GetFormated9 ( label , v ) );
        public static void AddScreenText ( string label , double v ) => AddToScreenText ( GetFormated ( label , v ) );
        public static void AddScreenText ( string label , float v ) => AddToScreenText ( GetFormated ( label , v ) );
        public static void AddScreenText ( string label , int v ) => AddToScreenText ( GetFormated ( label , v ) );
        public static void AddScreenText ( string label , Vector2 v ) => AddToScreenText ( GetFormated ( label , v ) );
        public static void AddScreenText ( string label , Vector3 v ) => AddToScreenText ( GetFormated ( label , v ) );
        public static void AddScreenText ( string label , Ray r ) => AddToScreenText ( GetFormated ( label , r ) );
        public static void AddScreenText ( string line ) => AddToScreenText ( line + '\n' );

        //public static void Log ( string message ) => Debug.Log ( "UnityRudi : " + message ) ;
        //public static void Log9 ( string label , double v  ) => Log ( GetFormated9 ( label , v ) ) ;
        //public static void Log ( string label , double v  ) => Log ( GetFormated ( label , v ) ) ;
        //public static void Log ( string label , float v   ) => Log ( GetFormated ( label , v ) ) ;
        //public static void Log ( string label , int v     ) => Log ( GetFormated ( label , v ) ) ;
        //public static void Log ( string label , Vector3 v ) => Log ( GetFormated ( label , v ) ) ;

        private static string GetFormated9 ( string label , double v )
        {
            return string.Format ( "{0,-5}: {1,5:F9}\n" , label , v );
        }
        private static string GetFormated ( string label , double v )
        {
            return string.Format ( "{0,-5}: {1,5:F3}\n" , label , v );
        }
        private static string GetFormated ( string label , float v )
        {
            return string.Format ( "{0,-5}: {1,5:F3}\n" , label , v );
        }
        private static string GetFormated ( string label , int v )
        {
            return string.Format ( "{0,-5}: {1}\n" , label , v );
        }
        private static string GetFormated ( string label , Vector2 v )
        {
            return string.Format ( "{0,-5}: ( {1,5:F3} , {2,5:F3} )\n" , label , v.x , v.y );
        }
        private static string GetFormated ( string label , Vector3 v )
        {
            return string.Format ( "{0,-5}: ( {1,5:F3} , {2,5:F3} , {3,5:F3} )\n" , label , v.x , v.y , v.z );
        }
        private static string GetFormated ( string label , Ray r )
        {
            return label + '\n' + GetFormated ( "Orig" , r.origin ) + GetFormated ( "Dir" , r.direction );
        }
    }
}
