using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Rudi.Core
{
    public static class Paths
    {
        private static string m_TempFolderPath = null ;
        private static string m_DataFolderPath = null ;

        // standard paths
        public static string DataFolderPath => m_DataFolderPath ??= GetFolderPathFromFolderName ( "Data" );
        public static string TempFolderPath => m_TempFolderPath ??= GetCleanFolderPathFromFolderPath ( DataFolderPath , "Temp" );

        // methods

        public static string GetFolderPathFromFolderName ( string folder , bool create = true ) => GetFolderPathFromFolderPath ( Application.persistentDataPath , folder , create );
        public static string GetFolderPathFromFolderPath ( string path , string folder , bool create = true )
        {
            var Result = Path.Combine ( path , folder ) ;
            if ( create ) Directory.CreateDirectory ( Result );
            return Result;
        }

        public static string GetCleanFolderPathFromFolderPath ( string path , string folder , bool create = true )
        {
            var Result = Path.Combine ( path , folder ) ;
            if ( create ) Directory.CreateDirectory ( Result );
            Utils.emptyFolder ( Result );
            return Result;
        }

        private static string CreateDirectory ( string path ) => Directory.CreateDirectory ( path ).FullName;
        private static string CreateCleanDirectory ( string path ) => Utils.emptyFolder ( CreateDirectory ( path ) );
        public static bool IsSubfolder ( string parentPath , string childPath )
        {
            var parent = new DirectoryInfo ( parentPath ) ;
            var child  = new DirectoryInfo ( childPath  ) ;
            return child.Parent.FullName == parent.FullName;
        }
    }
}
