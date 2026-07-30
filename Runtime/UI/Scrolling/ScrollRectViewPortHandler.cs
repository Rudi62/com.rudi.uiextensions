using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    [RequireComponent ( typeof ( RawImageV2 ) )]
    public class ScrollRectViewPortHandler : MonoBehaviour
    {
        private static readonly string TAG = "Rudis ScrollRectViewPortHandler" ;

        private static readonly Vector4 m_RadiiFull = new Vector4 ( 1 , 1 , 1 , 1 ) ;
        private static readonly Vector4 m_RadiiHalf = new Vector4 ( 1 , 1 , 0 , 0 ) ;

        private ScrollRect m_ScrollRect = null ;
        private ScrollRect scrollRect => m_ScrollRect = null != m_ScrollRect ? m_ScrollRect : GetComponentInParent<ScrollRect> ();
        private Scrollbar scrollbar => null != scrollRect ? scrollRect.verticalScrollbar : null;
        bool hasScrollBar => null != scrollbar ? scrollbar.gameObject.activeSelf : false;

        private RawImageV2 m_RawImageV2 ;
        private RawImageV2 maskImageV2 => m_RawImageV2 = null != m_RawImageV2 ? m_RawImageV2 : GetComponent<RawImageV2> ();

        private CanvasCallbacks m_Callbacks = null ;
        private CanvasCallbacks callbacks => m_Callbacks ??= new CanvasCallbacks ( OnLateEnable , this );
        private void OnLateEnable () => maskImageV2.radii = hasScrollBar ? m_RadiiHalf : m_RadiiFull;

        private void OnEnable () => callbacks.StartSingle ( 1 );
        private void OnDisable () => callbacks.Stop ();
    }
}
