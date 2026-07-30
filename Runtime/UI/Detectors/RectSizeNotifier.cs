using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Rudi.UI
{
    [ExecuteAlways]
    [RequireComponent ( typeof ( RectTransform ) )]
    public class RectSizeNotifier : MonoBehaviour
    {
        private static readonly string TAG = "Rudis RectSizeNotifier" ;

        private IResizedReciever m_Reciever = null ;
        private IResizedReciever reciever
        {
            get
            {
                if ( null == m_Reciever )
                {
                    m_Reciever = GetComponentInParent<IResizedReciever> ();
                }
                return m_Reciever;
            }
        }

        private void OnRectTransformDimensionsChange ()
        {
            reciever?.OnResized ();
        }
    }
}
