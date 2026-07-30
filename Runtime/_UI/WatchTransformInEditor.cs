
using System;
using System.Collections;
using UnityEngine;

namespace Rudi.UI
{
    [ExecuteInEditMode]
    [Serializable]
    public class WatchTransformInEditor
    {
        // https://forum.unity.com/threads/transform-position-change-callback.746360/

        private static readonly string TAG = "Rudis WatchTransformInEditor";

        public bool       m_Watch = true ;
        public float      m_RefreshRate = 0.1f ;
        public Vector3    m_Position = Vector3.zero ;
        public GameObject m_Parent   = null ;
        public int        m_Index    = -1 ;

        public Action < Vector3    > onPositionChanged ;
        public Action < GameObject > onParentChanged   ;
        public Action < int        > onIndexChanged    ;

        WaitForSecondsRealtime delay ;

        IEnumerator tick ;

        public void OnValidate (
            MonoBehaviour mono ,
            Action<Vector3> onPositionChanged ,
            Action<GameObject> onParentChanged ,
            Action<int> onIndexChanged )
        {
            if ( null == mono ) return;
#if UNITY_EDITOR
            if ( !m_Watch || !mono.gameObject.activeInHierarchy ) return;

            this.onPositionChanged = onPositionChanged;
            this.onParentChanged = onParentChanged;
            this.onIndexChanged = onIndexChanged;

            delay = new WaitForSecondsRealtime ( m_RefreshRate );
            m_Position = mono.transform.localPosition;
            m_Index = mono.transform.GetSiblingIndex ();
            m_Parent = GetParent ( mono.transform );
            if ( tick == null )
            {
                tick = Tick ( mono.transform );
                mono.StartCoroutine ( Tick ( mono.transform ) );
            }
#endif
        }


        private static GameObject GetParent ( Transform transform )
        {
            if ( null == transform ) return null;
            if ( null == transform.parent ) return null;
            return transform.parent.gameObject;
        }

#if UNITY_EDITOR
        IEnumerator Tick ( Transform transform )
        {
            while ( m_Watch && transform.gameObject.activeInHierarchy )
            {
                var position = transform.localPosition ;
                if ( m_Position != position )
                {
                    m_Position = position;
                    onPositionChanged?.Invoke ( position );
                }

                var parent = GetParent ( transform ) ;
                if ( parent != m_Parent )
                {
                    m_Parent = parent;
                    onParentChanged?.Invoke ( parent );
                }


                var index = transform.GetSiblingIndex () ;
                if ( index != m_Index )
                {
                    m_Index = index;
                    onIndexChanged?.Invoke ( index );
                }

                yield return delay;
            }
            tick = null;
        }
#endif
    }
}
