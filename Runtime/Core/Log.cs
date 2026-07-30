using UnityEngine;

namespace Rudi.Core
{
    public static class Log
    {
        private static string objInfo ( GameObject obj )
        {
            if ( null == obj ) return string.Empty ;
            if ( null != obj.transform.parent )
            {
                return $" - { obj.name } id: {obj.GetInstanceID ()} parent: { obj.transform.parent.gameObject.name }" ;
            }
            else
            {
                return $" - { obj.name } id: {obj.GetInstanceID ()}" ;
            }
        }

#if !RudisApp
        [ System.Diagnostics.Conditional ( "UNITY_EDITOR" ) ]
#endif
        public static void i ( string tag , string msg ) { Debug.Log ( $"{ Time.frameCount } { tag } : { msg }" ); }

#if !RudisApp
        [System.Diagnostics.Conditional ( "UNITY_EDITOR" ) ]
#endif
        public static void i ( string tag , GameObject obj , string msg ) { Debug.Log ( $"{ Time.frameCount } { tag } : { msg }{ objInfo ( obj ) }" ); }

        public static void e ( string tag , string msg ) { UnityEngine.Debug.LogError ( tag + " : " + msg ); }
        public static void e ( string tag , GameObject obj , string msg ) { Debug.LogError ( $"{ Time.frameCount } { tag } : { msg }{ objInfo ( obj ) }" ); }



        [System.Diagnostics.Conditional ( "UNITY_EDITOR" )]
        public static void u ( string tag , string msg ) { Debug.Log ( $"{ Time.frameCount } { tag } : { msg }" ); }

        [System.Diagnostics.Conditional ( "UNITY_EDITOR" )]
        public static void u ( string tag , GameObject obj , string msg ) { Debug.Log ( $"{ Time.frameCount } { tag } : { msg } - { obj.name } from { obj.transform.parent.gameObject.name }" ); }

        [System.Diagnostics.Conditional ( "UNITY_EDITOR" )]
        public static void er ( string tag , string msg ) { Debug.LogWarning ( tag + " : " + msg ); }

        public static void w ( string tag , string msg ) { Debug.LogWarning ( tag + " : " + msg ); }
    }
}
