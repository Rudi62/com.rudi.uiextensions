using Rudi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    [ RequireComponent ( typeof ( CanvasScaler ) ) ]
    public class CanvasAdaptor : MonoBehaviour
    {
        private Orientation m_LastOrientation = Orientation.Unknown ;
        private enum Orientation
        {
            Unknown   ,
            Portrait  ,
            Landscape ,
        }

        private Orientation orientation => Screen.width > Screen.height ? Orientation.Landscape : Orientation.Portrait ;

        private static float GetMatch ( Orientation o ) => o == Orientation.Landscape ? 1f : 0f ;

        private void checkOrientation ()
        {
            if ( Utils.UpdateProperty ( ref m_LastOrientation , orientation ) )
            {
                GetComponent < CanvasScaler > () . matchWidthOrHeight = GetMatch ( m_LastOrientation ) ;
            }
        }

        private void Awake ()
        {
            CanvasScaler Scaler = GetComponent < CanvasScaler > () ;
            Scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize ;
            Scaler.referenceResolution = new Vector2 ( 400 , 400 ) ;
        }

        private void OnEnable () => checkOrientation () ;
        private void OnRectTransformDimensionsChange () => checkOrientation () ;
    }
}
