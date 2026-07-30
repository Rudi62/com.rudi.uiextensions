using Rudi.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    [ DisallowMultipleComponent ]
    [ AddComponentMenu ( "Rudi/UI/Raw Image V2 Auto Web Loader" ) ]
    [ RequireComponent ( typeof ( RawImageV2 ) ) ]
    public class RawImageV2AutoWebLoader : MonoBehaviour
    {
        [ TextArea ]
        [ SerializeField ] private string m_Url ;

        // https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRF6FXT_Z473RH_54iWCvPZxaqw7bh4jeb9g_Ox7byBxw&s=10

        RawImageV2 m_Image = null ;
        RawImageV2 image => this.getComponentIfZero ( ref m_Image ) ;

        private void loadImage ()
        {
            if ( string.IsNullOrEmpty ( m_Url ) ) return ;
            if ( null == image ) return ;
            image.loadFromWeb ( m_Url ) ;
        }

        void Start ()
        {
            loadImage () ;
        }
    }
}
