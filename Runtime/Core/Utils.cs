
// last change:
// 03.03.2022 12:17
// by: Rudi Weinacker

using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Rudi.Core.DateTimeUtils;
using System.Text;
//using UnityEditor.SearchService;
using UnityEngine.SceneManagement;
using UnityEngine.Pool;
using Unity.Collections;
using System.Reflection;
using System.ComponentModel;
using Rudi.Extensions;
//using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Rudi.Core
{

    sealed class Ref<T>
    {
        // https://stackoverflow.com/questions/2980463/how-do-i-assign-by-reference-to-a-class-field-in-c
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        public Ref ( Func<T> getter , Action<T> setter )
        {
            this.getter = getter;
            this.setter = setter;
        }
        public T Value { get { return getter (); } set { setter ( value ); } }
    }

    sealed class Wrapper<T> where T : class
    {
        public T Value { get; set; }
    }

    public sealed class Holder<T> where T : class
    {
        public T Value = null ;
        public void destroy () => Value = null;
        public Holder () { }
        public Holder ( T val ) => Value = val;
        public bool hasObject () => null != Value;
    }

    public static class Holder
    {
        public static T get<T> ( Holder<T> obj ) where T : class
        {
            return obj?.Value ?? null;
        }
        public static void set<T> ( Holder<T> obj , T val ) where T : class
        {
            if ( null != obj ) obj.Value = val;
        }
    }

    public static class Utils
    {
        private static readonly string TAG = "RudisUtils";

        private static readonly WaitForEndOfFrame s_WaitForEndOfFrame = new WaitForEndOfFrame () ;
        public static WaitForEndOfFrame WaitForEndOfFrame => s_WaitForEndOfFrame ;
        static public void DestroyObject ( UnityEngine.Object obj )
        {
            if ( obj == null ) return;

#if UNITY_EDITOR
            if ( !Application.isPlaying )
            {
                UnityEngine.Object.DestroyImmediate ( obj );
                return;
            }
#endif
            if ( obj is GameObject go )
            {
                go.transform.SetParent ( null );
            }
            else if ( obj is MonoBehaviour mb && mb.gameObject != null )
            {
                mb.gameObject.transform.SetParent ( null );
                obj = mb.gameObject;
            }

            UnityEngine.Object.Destroy ( obj );
        }

        static public void DestroyComponent<T> ( T comp ) where T : UnityEngine.Component
        {
            if ( comp == null ) return;

#if UNITY_EDITOR
            if ( !Application.isPlaying ) UnityEngine.Object.DestroyImmediate ( comp );
            else
#endif
            {
                UnityEngine.Object.Destroy ( comp );
            }
        }


        static public void DestroyObjectAndZero<T> ( ref T obj ) where T : UnityEngine.Object
        {
            DestroyObject ( obj );
            obj = null;
        }

        static public void DestroyComponentAndZero<T> ( ref T comp ) where T : UnityEngine.Component
        {
            DestroyComponent ( comp );
            comp = null;
        }

        static public T loadPrefab<T> ( string name ) where T : MonoBehaviour
        {
            var FullName = name ;
            var Result = Resources.Load < T > ( FullName ) ;
            if ( null == Result ) Log.e ( TAG , "couldn't load prefab: " + FullName );
            return Result;
        }

        public static T loadPrefabIfZero<T> ( ref T prefab , string name ) where T : MonoBehaviour
        {
            if ( null == prefab )
            {
                prefab = loadPrefab<T> ( name );
            }
            return prefab;
        }

        static public T instantiate<T> ( string name ) where T : MonoBehaviour
        {
            var prefab = loadPrefab < T > ( name ) ;
            return null != prefab ? UnityEngine.Object.Instantiate ( prefab ) : null;
        }

        static public T instantiateFull<T> ( T prefab , Transform parent = null ) where T : MonoBehaviour
        {
            if ( null == prefab ) return null;
            var rootCanvas = findRootCanvas () ;
            if ( !rootCanvas ) return nullReturn<T> ( "couldn't find root canvas" );
            var Result = UnityEngine.Object.Instantiate ( prefab ) ;
            if ( null == Result ) return null;
            var objRect = Result.transform as RectTransform ;
            if ( null == objRect )
            {
                DestroyObject ( Result );
                return null;
            }
            if ( null == parent ) parent = rootCanvas.transform;
            objRect.SetParent ( parent , false );
            objRect.SetFull ();
            return Result;
        }

        static public T instantiateFull<T> ( string name , Transform parent = null ) where T : MonoBehaviour
        {
            return instantiateFull ( loadPrefab<T> ( name ) , parent );
        }

        public static T createIfZero<T> ( ref T obj , Func<T> create )
        {
            if ( null == obj ) obj = create ();
            return obj;
        }

        /// <summary>
        /// Destroy the specified object immediately, unless not in the editor, in which case the regular Destroy is used instead.
        /// </summary>

        static public void DestroyImmediate ( UnityEngine.Object obj )
        {
            if ( obj != null )
            {
                if ( Application.isEditor ) UnityEngine.Object.DestroyImmediate ( obj );
                else UnityEngine.Object.Destroy ( obj );
            }
        }


        public static bool isEditorAboutToPlay ()
        {
#if UNITY_EDITOR
            if ( Application.isPlaying ) return false;
            if ( EditorApplication.isPlayingOrWillChangePlaymode ) return true;
#endif
            return false;
        }

        private static bool isInPrefabStage ( MonoBehaviour mono )
        {
#if UNITY_EDITOR
            var stage = PrefabStageUtility.GetPrefabStage ( mono.gameObject ) ;
            return null != stage;
#else
            return false ;
#endif
        }

        private static bool IsPartOfPrefabAsset ( MonoBehaviour mono )
        {
#if UNITY_EDITOR
            return PrefabUtility.IsPartOfPrefabAsset ( mono );
#else
            return false ;
#endif
        }

        public static bool hasData<T> ( T [ ] arr )
        {
            if ( null == arr ) return false;
            return arr.Length > 0;
        }

        public static bool hasData<T> ( List<T> list )
        {
            if ( null == list ) return false;
            return list.Count > 0;
        }

        public static int getNumElements<T> ( T [ ] arr )
        {
            if ( null == arr ) return 0;
            return arr.Length;
        }
        public static int getNumElements<T> ( List<T> list )
        {
            if ( null == list ) return 0;
            return list.Count;
        }


        public static Material createMaterial ( string shaderName )
        {
            //Log.i ( TAG , "trying to find shader: " + shaderName ) ;
            var shader = Shader.Find ( shaderName ) ;
            if ( null == shader )
            {
                Log.i ( TAG , "Couldn't find shader: " + shaderName );
                return null;
            }
            //Log.i ( TAG , "found shader: " + shaderName );
            var mat = new Material ( shader ) ;
            mat.hideFlags = HideFlags.HideAndDontSave;
            return mat;
        }
        public static Material createMaterial ( Shader shader )
        {
            if ( null == shader )
            {
                Log.i ( TAG , "shader is zero" );
                return null;
            }
            //Log.i ( TAG , "found shader: " + shader.name );
            var mat = new Material ( shader ) ;
            mat.hideFlags = HideFlags.HideAndDontSave;
            return mat;
        }

        public static T GetRootObject<T> ( bool includeinactive = false ) where T : Behaviour
        {
            var scene = SceneManager.GetActiveScene () ;
            if ( null == scene )
            {
                Log.i ( TAG , "scene is zero" );
                return null;
            }

            if ( !scene.isLoaded )
            {
                //Log.i ( TAG , "scene not loaded" ) ;
                return null;
            }

            T Result = null ;
            var list = ListPool < GameObject > . Get () ;
            scene.GetRootGameObjects ( list );
            if ( list.Count > 0 )
            {
                for ( var i = 0 ; i < list.Count ; ++i )
                {
                    var sp = list [ i ].GetComponent < T > () ;
                    if ( null == sp ) continue;
                    if ( !includeinactive && !sp.isActiveAndEnabled ) continue;
                    Result = sp;
                    break;
                }
            }
            ListPool<GameObject>.Release ( list );
            return Result;
        }
        private static bool falseReturn ( string msg )
        {
            Log.i ( TAG , msg );
            return false;
        }

        private static void voidReturn ( string msg )
        {
            Log.i ( TAG , msg );
        }

        private static T nullReturn<T> ( string msg ) where T : class
        {
            Log.i ( TAG , msg );
            return null;
        }

        public static Canvas findRootCanvas ()
        {
            var Objects = UnityEngine.Object.FindObjectsByType < Canvas > ( FindObjectsInactive.Exclude , FindObjectsSortMode.None ) ;
            if ( null == Objects ) return nullReturn<Canvas> ( "no canvas objects found" );
            foreach ( var cv in Objects )
            {
                if ( !cv.isActiveAndEnabled ) continue;
                if ( !cv.isRootCanvas ) continue;
                return cv;
            }
            return null;
        }

        private static T [ ] FindObjectsByType<T> ( FindObjectsInactive exclude , FindObjectsSortMode none )
        {
            throw new NotImplementedException ();
        }

        public static bool isPlaying ()
        {
#if UNITY_EDITOR
            return Application.isPlaying;
#else
            return true ;
#endif
        }
        //public static long GpsNanosToUtcMillis ( long gps_nanos )
        //{
        //	const long OFFSET_SEC_UNIX_TO_GPS = 315964800L;
        //	const long OFFSET_MILLIS_UNIX_TO_GPS = OFFSET_SEC_UNIX_TO_GPS * 1000L ;
        //	const long NANOS_IN_MILLI = 1000000L ;
        //	const long LEAP_SECONDS = 18L ;
        //	const long LEAP_MILLIS = LEAP_SECONDS * 1000L ;
        //	long UtcMillis = gps_nanos / NANOS_IN_MILLI + OFFSET_MILLIS_UNIX_TO_GPS - LEAP_MILLIS ;
        //	return UtcMillis;
        //}
        //public static long getFileTimeMillis ( string filepath ) { return new DateTimeOffset ( File.GetLastWriteTimeUtc ( filepath ) ).ToUnixTimeMilliseconds (); }

        //public static ulong KnutHash ( string s )
        //{
        //	// https://stackoverflow.com/questions/9545619/a-fast-hash-function-for-string-in-c-sharp
        //	ulong hashedValue = 3074457345618258791ul ;
        //	foreach ( char c in s )
        //	{
        //		hashedValue += c;
        //		hashedValue *= 3074457345618258799ul;
        //	}
        //	return hashedValue ;
        //}
        public static ulong KnutHash ( ReadOnlySpan<char> s )
        {
            // https://stackoverflow.com/questions/9545619/a-fast-hash-function-for-string-in-c-sharp
            var hashedValue = 3074457345618258791ul;
            foreach ( var c in s )
            {
                hashedValue += c;
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        public static ulong KnutHashInHyphens ( ReadOnlySpan<char> s )
        {
            var h1 = s.IndexOf ( '\"' ) ;
            if ( h1 < 0 ) return 0;
            var s1 = s [ ( h1 + 1 ).. ] ;
            var h2 = s1.IndexOf ( '\"' ) ;
            if ( h2 < 0 ) return 0UL;
            return KnutHash ( s1.Slice ( 0 , h2 ) );
        }
        public static (ulong hash, int next_index) KnutHashToNextHyphen ( ReadOnlySpan<char> s )
        {
            //int h = s.IndexOf ( '\"' ) ;
            //if ( h < 0 ) return ( ~0UL , 0 ) ;
            //return ( KnutHash ( s.Slice ( 0 , h -1 ) ) , h + 1 ) ;
            var hashedValue = 3074457345618258791ul;
            var backslash = false;
            for ( var i = 0 ; i < s.Length ; i++ )
            {
                var c = s [ i ];
                if ( !backslash && c == '\"' ) return (hashedValue, i + 1);
                backslash = c == '\\';
                hashedValue += c;
                hashedValue *= 3074457345618258799ul;
            }
            return (0, 0);
        }

        public static ulong KnutHash ( string s )
        {
            return KnutHash ( s.AsSpan () );
        }

        public static bool isNum ( char c )
        {
            if ( c < '0' ) return false;
            if ( c > '9' ) return false;
            return true;
        }

        public static int readTrailingInt ( ReadOnlySpan<char> s , int notfoundval , bool skipfileextension = false )
        {
            var index = s.Length ;
            var Result = 0 ;
            var Factor = 1 ;
            var found = false ;
            if ( skipfileextension )
            {
                while ( --index >= 0 )
                {
                    var c = s [ index ] ;
                    if ( c == '.' ) break;
                }
            }
            while ( --index >= 0 )
            {
                var c = s [ index ] ;
                if ( !isNum ( c ) ) break;
                Result += Factor * ( c - '0' );
                Factor *= 10;
                found = true;
            }
            return found ? Result : notfoundval;
        }

        public static int readTrailingInt ( string s , int notfoundval , bool skipfileextension = false )
        {
            return readTrailingInt ( s.AsSpan () , notfoundval , skipfileextension );
        }

        public static bool hasTrailingInt ( ReadOnlySpan<char> s , bool skipfileextension = false )
        {
            var index = s.Length ;
            if ( skipfileextension )
            {
                while ( --index >= 0 )
                {
                    var c = s [ index ] ;
                    if ( c == '.' ) break;
                }
            }
            if ( index < 1 ) return false;
            return isNum ( s [index - 1] );
        }

        public static bool hasTrailingInt ( string s , bool skipfileextension = false )
        {
            return hasTrailingInt ( s.AsSpan () , skipfileextension );
        }


        public static ulong Get64BitHash ( string s )
        {
            // https://stackoverflow.com/questions/31464894/better-64-bit-byte-array-hash
            const ulong p = 1099511628211UL;
            var hash = 14695981039346656037UL;
            foreach ( var c in s )
            {
                hash ^= c;
                hash *= p;
            }
            return hash;
        }

        public static void deleteFile ( string filepath )
        {
            if ( File.Exists ( filepath ) )
            {
                try
                {
                    File.Delete ( filepath );
                }
                catch ( Exception )
                {
                    return;
                }
            }
        }
        public static string GetFilenameWithPostfix ( string filepath , string postfix )
        {
            // https://stackoverflow.com/questions/17184333/change-file-name-of-image-path-in-c-sharp
            var dir = Path.GetDirectoryName ( filepath ) ;
            var name = Path.GetFileNameWithoutExtension ( filepath ) ;
            var ext = Path.GetExtension ( filepath ) ;
            return Path.Combine ( dir , name + postfix + ext );
        }
        public static string GetFilenameWithPostfix ( string filepath , char postfix )
        {
            // https://stackoverflow.com/questions/17184333/change-file-name-of-image-path-in-c-sharp
            var dir = Path.GetDirectoryName ( filepath );
            var name = Path.GetFileNameWithoutExtension ( filepath );
            var ext = Path.GetExtension ( filepath );
            return Path.Combine ( dir , name + postfix + ext );
        }
        public static string GetFilenameWithPrefix ( string filepath , string prefix )
        {
            // https://stackoverflow.com/questions/17184333/change-file-name-of-image-path-in-c-sharp
            var dir = Path.GetDirectoryName ( filepath );
            var name = Path.GetFileNameWithoutExtension ( filepath );
            var ext = Path.GetExtension ( filepath );
            return Path.Combine ( dir , prefix + name + ext );
        }
        public static string GetFilePathWithPrefix ( string filepath , char prefix )
        {
            // https://stackoverflow.com/questions/17184333/change-file-name-of-image-path-in-c-sharp
            var dir = Path.GetDirectoryName ( filepath );
            var name = Path.GetFileNameWithoutExtension ( filepath );
            var ext = Path.GetExtension ( filepath );
            return Path.Combine ( dir , prefix + name + ext );
        }

        public static string GetFilePathWithExtension ( string filepath , string ext )
        {
            var dir = Path.GetDirectoryName ( filepath ) ;
            var name = Path.GetFileNameWithoutExtension ( filepath ) ;
            if ( ext.StartsWith ( '.' ) )
            {
                return Path.Combine ( dir , name + ext );
            }
            else
            {
                return Path.Combine ( dir , name + '.' + ext );
            }
        }

        public static string GetDescription<T1> ( T1 value )
        {
            // https://stackoverflow.com/questions/424366/string-representation-of-an-enum
            var descriptionAttribute = ( DescriptionAttribute ) value.GetType ()
                .GetField ( value.ToString () )
                .GetCustomAttributes ( false )
                .Where ( a => a is DescriptionAttribute )
                .FirstOrDefault ();
            return descriptionAttribute != null ? descriptionAttribute.Description : value.ToString ();

        }

        //public static void emptyLogFolder () => emptyFolder ( getLogFolder () ) ;
        public static string ensureEmptyFolder ( string folderPath ) => emptyFolder ( Directory.CreateDirectory ( folderPath ).FullName );
        public static string emptyFolder ( string folderPath , bool includeSubDirs = true )
        {
            // https://stackoverflow.com/questions/1288718/how-to-delete-all-files-and-folders-in-a-directory
            if ( !Directory.Exists ( folderPath ) ) return folderPath;
            foreach ( var file in Directory.GetFiles ( folderPath ) )
            {
                File.Delete ( file );
            }
            if ( includeSubDirs )
            {
                foreach ( var dir in Directory.GetDirectories ( folderPath ) )
                {
                    deleteFolder ( dir );
                }
            }
            return folderPath;
        }

        public static void deleteFolder ( string folderPath )
        {
            if ( !Directory.Exists ( folderPath ) ) return;
            emptyFolder ( folderPath );
            Directory.Delete ( folderPath );
        }

        public static string getFolderName ( string folderPath )
        {
            // Path.GetDirectoryName ( folder ) doesn't do that! Instead it returns the full pate of parent folder!!
            // https://stackoverflow.com/questions/5229292/get-folder-name-from-full-folder-path
            //             m_ProjectFolder = Path.GetDirectoryName ( filepath ) ;

            if ( isFile ( folderPath ) ) return getFolderName ( Path.GetDirectoryName ( folderPath ) );
            return new DirectoryInfo ( folderPath ).Name;
        }

        private static bool isDirectory ( string path ) => Directory.Exists ( path );
        private static bool isFile ( string path ) => File.Exists ( path );
        public static bool isDirectoryEmpty ( string path )
        {
            if ( !isDirectory ( path ) ) return false;
            if ( hasData ( Directory.GetFiles ( path ) ) ) return false;
            if ( hasData ( Directory.GetDirectories ( path ) ) ) return false;
            return true;
        }

        //private void deleteFile ( string filepath )
        //{
        //    if ( File.Exists ( filepath ) ) File.Delete ( filepath ) ;
        //}

        public static bool hasExtension ( string path , string ext )
        {
            var extension = Path.GetExtension ( path ) ;
            if ( string.IsNullOrEmpty ( extension ) ) return false;
            if ( ext.StartsWith ( '.' ) )
            {
                return ext.Equals ( extension , StringComparison.InvariantCultureIgnoreCase );
            }
            else
            {
                return ext.Equals ( extension [1..] , StringComparison.InvariantCultureIgnoreCase );
            }
        }

        private static bool CreateEmptyFolder ( string path , bool preserveSubFolders = false )
        {
            try
            {
                if ( isFile ( path ) ) return false;
                if ( isDirectory ( path ) )
                {
                    emptyFolder ( path , !preserveSubFolders );
                    return true;
                }
                Directory.CreateDirectory ( path );
                return true;
            }
            catch ( Exception )
            {
                Log.e ( TAG , "Couldn't create directory " + path );
                return false;
            }
        }

        public static void Sort ( FileInfo [ ] fis )
        {
            // https://stackoverflow.com/questions/1199006/how-to-sort-an-array-of-fileinfo
            Array.Sort ( fis , ( f1 , f2 ) => f1.Name.CompareTo ( f2.Name ) );
        }

        public static int MoveDirectoryContent ( string SrcFolder , string DstFolder , bool includeSubDirs = true ) // , bool sort = false 
        {
            var NumFilesMoved = 0 ;
            if ( !isDirectory ( SrcFolder ) ) return 0;
            if ( !CreateEmptyFolder ( DstFolder , !includeSubDirs ) ) return 0;
            DirectoryInfo di = new ( SrcFolder ) ;
            var fis = di.GetFiles () ;
            foreach ( var fi in fis )
            {
                var DstPath = Path.Combine ( DstFolder , fi.Name ) ;
                //Log.i ( TAG , "Moving " + fi.Name ) ;
                if ( File.Exists ( DstPath ) ) File.Delete ( DstPath );
                fi.MoveTo ( DstPath );
            }
            if ( includeSubDirs )
            {
                foreach ( var dir in Directory.GetDirectories ( SrcFolder ) )
                {
                    var FolderName = getFolderName ( dir ) ;
                    var DstPath = Path.Combine ( DstFolder , FolderName ) ;
                    NumFilesMoved += MoveDirectoryContent ( dir , DstPath );
                }
            }
            return NumFilesMoved;
        }

        public static void CopyDirectory ( string sourceDir , string destinationDir , bool recursive )
        {
            // https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
            // Get information about the source directory
            var dir = new DirectoryInfo ( sourceDir ) ;

            // Check if the source directory exists
            if ( !dir.Exists )
                throw new DirectoryNotFoundException ( $"Source directory not found: { dir.FullName }" );

            // Cache directories before we start copying
            var dirs = dir.GetDirectories () ;

            // Create the destination directory
            Directory.CreateDirectory ( destinationDir );

            // Get the files in the source directory and copy to the destination directory
            foreach ( var file in dir.GetFiles () )
            {
                var targetFilePath = Path.Combine ( destinationDir , file.Name ) ;
                file.CopyTo ( targetFilePath );
            }

            // If recursive and copying subdirectories, recursively call this method
            if ( recursive )
            {
                foreach ( var subDir in dirs )
                {
                    var newDestinationDir = Path.Combine ( destinationDir , subDir.Name ) ;
                    CopyDirectory ( subDir.FullName , newDestinationDir , true );
                }
            }
        }

        // classes
        public static bool doesClassExist ( string @namespace , string @class )
        {
            // https://stackoverflow.com/questions/8499593/c-sharp-how-to-check-if-namespace-class-or-method-exists-in-c
            var FullName = string.Empty ;
            if ( string.IsNullOrEmpty ( @namespace ) )
            {
                FullName = @class;
            }
            else
            {
                FullName = string.Format ( "{0}.{1}" , @namespace , @class );
            }
            var myClassType = Type.GetType (FullName ) ;
            return null != myClassType;
        }

        // FrameRates
        public enum FrameRates
        {
            [ Description ( "15 fps" ) ] fps_15 = 15,
            [ Description ( "20 fps" ) ] fps_20 = 20,
            [ Description ( "30 fps" ) ] fps_30 = 30,
            [ Description ( "60 fps" ) ] fps_60 = 60,
            [ Description ( "90 fps" ) ] fps_90 = 90,
            [ Description ( "120 fps" ) ] fps_120 = 120,
            [ Description ( "max fps" ) ] fps_max = 999,
        }
        public static void setFrameRate ( FrameRates framerate ) => setFramerateInFPS ( getFPS ( framerate ) );

#if !UNITY_EDITOR && UNITY_ANDROID
        private static Stack < int > m_UsedFrameRates = new () ;

        [RuntimeInitializeOnLoadMethod ( RuntimeInitializeLoadType.BeforeSceneLoad )]
        private static void recieveNativeFrameRate ()
        {
            //m_NativeFrameRate = ( int ) Screen.currentResolution.refreshRateRatio.value;
            //Log.i ( TAG , $"native framerate = { m_NativeFrameRate } hz" ) ;
            m_UsedFrameRates.Clear () ;
            m_NativeFrameRate = 0 ;
        }
#endif

        private static int m_NativeFrameRate = 0 ;

        public static void pushFrameRate ( FrameRates framerate )
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            int pushing = Application.targetFrameRate ;
            //Log.i ( TAG , $"pushing { pushing }, setting { framerate }" ) ;
            m_UsedFrameRates.Push ( pushing ) ;
            setFrameRate ( framerate ) ;
#endif
        }
        public static void popFrameRate ()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if ( m_UsedFrameRates.Count < 1 ) return ;
            int popped = m_UsedFrameRates.Pop () ;
            //Log.i ( TAG , $"popping { popped } hz" ) ;
            setFramerateInFPS ( popped ) ;
#endif
        }

        //private static int getrate ( RefreshRate rate )

        public static int nativeFramerate // => m_NativeFrameRate ;
        {
            get
            {
                if ( 0 == m_NativeFrameRate )
                {
                    var curres = Screen.currentResolution ;
                    var resolutions = Screen.resolutions ;
                    var MaxFrameRate = 0 ;
                    // Print the resolutions
                    foreach ( var res in resolutions )
                    {
                        if ( res.width != curres.width ) continue;
                        if ( res.height != curres.height ) continue;
                        var FrameRate = ( int ) res.refreshRateRatio.value ;
                        if ( FrameRate > MaxFrameRate ) MaxFrameRate = FrameRate;
                        MaxFrameRate = Mathf.Max ( MaxFrameRate , ( int ) res.refreshRateRatio.value );
                    }
                    m_NativeFrameRate = MaxFrameRate;
                    Log.i ( TAG , "MaxFrameRate = " + MaxFrameRate );
                }
                return m_NativeFrameRate;
            }
        }
        //private static int getNativeFrameRate ()
        //{
        //    int fr = ( int ) Screen.currentResolution.refreshRateRatio.value ;
        //    Log.i ( TAG , $"native framerate = { fr } hz" ) ;
        //    return fr ;
        //}
        //private static int getMaxFrameRate ()
        //{

        //}
        private static int getFPS ( FrameRates framerate )
        {
            if ( FrameRates.fps_max == framerate ) return nativeFramerate;
            return ( int ) framerate;
        }
        private static void setFramerateInFPS ( int fps )
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            //Log.i ( TAG , $"setting { fps } fps" ) ;
            Application.targetFrameRate = fps ;
#endif
        }

        // time
        public static long currentUtcTimeMillis ()
        {
            return LocalDateTimeToUtcTimeMillis ( DateTime.Now );
        }
        public static DateTime TimeMillisToDateTime ( long millis )
        {
            return DateTimeOffset.FromUnixTimeMilliseconds ( millis ).DateTime.ToLocalTime ();
        }
        public static long LocalDateTimeToUtcTimeMillis ( DateTime time )
        {
            return new DateTimeOffset ( time.ToUniversalTime () ).ToUnixTimeMilliseconds ();
        }
        public static void removeSuperfluidAudioListeners ()
        {
            var aL = UnityEngine.Object.FindObjectsOfType<AudioListener> ();
            for ( var i = 0 ; i < aL.Length ; i++ )
            {
                //Ignore the first AudioListener in the array 
                if ( i == 0 )
                    continue;

                //Destroy 
                DestroyObject ( aL [i] );
            }
        }
        public static void destroyAllAudioListeners ()
        {
            var aL = UnityEngine.Object.FindObjectsOfType<AudioListener> ();
            for ( var i = 0 ; i < aL.Length ; i++ )
            {
                //Destroy 
                DestroyObject ( aL [i] );
            }
        }

        //https://answers.unity.com/questions/7776/how-to-make-an-gameobject-invisible-and-disappeare.html
        public static void hide ( GameObject obj )
        {
            if ( null == obj ) return;
            obj.transform.localScale = new Vector3 ( 0 , 1 , 1 );
        }

        public static void moveOut ( GameObject obj )
        {
            if ( null == obj ) return;
            obj.transform.localPosition = new Vector3 ( -5000f , 0 , 0 );
        }


        public static void show ( GameObject obj )
        {
            if ( null == obj ) return;
            obj.transform.localScale = Vector3.one;
        }
        public static string getTimeDurationString ( double dseconds )
        {
            return getTimeDurationString ( Convert.ToInt64 ( dseconds ) );
        }


        public static string getTimeDurationString2 ( long seconds )
        {
            var Result = "";
            var bShowSeconds = true;
            if ( seconds >= 60 )
            {
                var minutes = seconds / 60;
                seconds %= 60;
                if ( minutes >= 60 )
                {
                    var hours = minutes / 60;
                    minutes %= 60;
                    Result = "" + hours + "h ";
                    bShowSeconds = false;
                }
                Result += "" + minutes + "m ";
            }
            if ( bShowSeconds ) Result += "" + seconds + "s";
            return Result;
        }

        static char getChar ( long v ) { return ( char ) ( v + '0' ); }
        public static string getTimeDurationString ( long seconds )
        {
            StringBuilder sb = new ( 32 ) ;
            var bShowSeconds = true;
            if ( seconds >= 60 )
            {
                var minutes = seconds / 60;
                seconds %= 60;
                if ( seconds == 0 ) bShowSeconds = false;
                if ( minutes >= 60 )
                {
                    var hours = minutes / 60;
                    minutes %= 60;

                    if ( hours >= 24 )
                    {
                        var days = hours / 24;
                        hours %= 24;
                        sb.Append ( days );
                        sb.Append ( 'd' );
                        sb.Append ( ' ' );
                    }
                    if ( hours != 0L )
                    {
                        sb.Append ( hours );
                        sb.Append ( 'h' );
                        sb.Append ( ' ' );
                    }
                    //Result = "" + hours + "h ";
                    bShowSeconds = false;
                }
                //Result += "" + minutes + "m ";
                if ( minutes != 0L )
                {
                    sb.Append ( minutes );
                    sb.Append ( 'm' );
                    if ( bShowSeconds ) sb.Append ( ' ' );
                }
            }
            if ( bShowSeconds )
            {
                //Result += "" + seconds + "s";
                sb.Append ( seconds );
                sb.Append ( 's' );
            }
            return sb.ToString ();
        }
        public static Texture2D LoadTexture ( string filePath )
        {
            Texture2D tex = null;
            byte [] fileData;

            if ( File.Exists ( filePath ) )
            {
                fileData = File.ReadAllBytes ( filePath );
                tex = new Texture2D ( 2 , 2 );
                tex.hideFlags = HideFlags.HideAndDontSave;
                if ( !tex.LoadImage ( fileData ) )  // this will auto-resize the texture dimensions.
                {
                    Log.e ( TAG , "couldn't load image" );
                    DestroyObject ( tex );
                    tex = null;
                }
            }
            else
            {
                Log.e ( TAG , "no picture found" );
            }
            return tex;
        }

    }
}
