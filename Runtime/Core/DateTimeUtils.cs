using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Rudi.Core
{
    public class DateTimeUtils
    {
        private static readonly string TAG = "RudisDateTimeUtils";


        //                                             1    6  9  12 15 18
        //                                             |    |  |  |  |  |
        private static readonly string sTimeFormat = "_yyyy_MM_dd_HH_mm_ss";

        public static DateTime getNow ( bool utc ) => utc ? DateTime.UtcNow : DateTime.Now;
        public static string getString ( DateTime datetime ) => datetime.ToString ( sTimeFormat );
        public static string getNowString ( bool utc ) => getString ( getNow ( utc ) );

        private static int val ( ReadOnlySpan<char> arr , int index , int numDigits )
        {
            var result = 0 ;
            for ( var i = 0 ; i < numDigits ; i++ )
            {
                result = result * 10 + arr [index + i] - '0';
            }
            return result;
        }
        public static DateTime getDateTimeFrom_String ( string s , int start_of_time_string , bool utc )
        {
            var kind = utc ? DateTimeKind.Utc : DateTimeKind.Local ;
            if ( s.Length < 20 + start_of_time_string ) return DateTime.FromFileTimeUtc ( 0 );
            var arr = s.AsSpan ( start_of_time_string , 20 ) ;

            //  1    6  9  12 15 18
            //  |    |  |  |  |  |
            // _yyyy_MM_dd_HH_mm_ss

            var year  = val ( arr ,  1 , 4 ) ;
            var month = val ( arr ,  6 , 2 ) ;
            var day   = val ( arr ,  9 , 2 ) ;
            var hour  = val ( arr , 12 , 2 ) ;
            var min   = val ( arr , 15 , 2 ) ;
            var sec   = val ( arr , 18 , 2 ) ;

            var datetime = new DateTime ( year , month , day , hour , min , sec , kind ) ;
            return datetime;
        }
        public static long GpsNanosToUtcMillis ( long gps_nanos )
        {
            const long OFFSET_SEC_UNIX_TO_GPS = 315964800L ;
            const long OFFSET_MILLIS_UNIX_TO_GPS = OFFSET_SEC_UNIX_TO_GPS * 1000L ;
            const long NANOS_IN_MILLI = 1000000L ;
            const long LEAP_SECONDS = 18L ;
            const long LEAP_MILLIS = LEAP_SECONDS * 1000L ;
            var UtcMillis = gps_nanos / NANOS_IN_MILLI + OFFSET_MILLIS_UNIX_TO_GPS - LEAP_MILLIS ;
            return UtcMillis;
        }
        //    public static long getFileTimeMillis ( string filepath )
        //    {
        //#if UNITY_EDITOR
        //		long ResultUnity = new DateTimeOffset ( File.GetLastWriteTimeUtc ( filepath ) ).ToUnixTimeMilliseconds ();
        //        return ResultUnity;
        //#else
        //        long ResultJava = PluginWrapper.Plugin.getFileTime ( filepath ) ;
        //        //Log.i ( TAG , $"getFileTimeMillis ( { filepath } )" );
        //        //Log.i ( TAG , "ResultUnity = " + ResultUnity ) ;
        //        //Log.i ( TAG , "ResultJava = " + ResultJava ) ;
        //        return ResultJava ;
        //#endif
        //        //return new DateTimeOffset ( File.GetLastWriteTimeUtc ( filepath ) ).ToUnixTimeMilliseconds ();
        //    }

    }
}
