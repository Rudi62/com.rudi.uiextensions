using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Rudi.UI
{

    // ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // class ShadowProperties

    [AddComponentMenu ( "Rudi/UI/Soft Shadow Properties" )]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public class ShadowProperties : MonoBehaviour
    {
        private static readonly string TAG = "Rudis ShadowProperties" ;

        [ SerializeField ] private DropShadow.Properties m_ShadowProperties = new () ;

        [System.Serializable] public class CShadowEvent : UnityEvent<DropShadow.MessageReason> { }
        [ SerializeField ] private CShadowEvent m_OnPropertiesChanged = new () ;

        public DropShadow.Properties shadowProperties => m_ShadowProperties;

#if UNITY_EDITOR
        private static CShadowEvent m_StaticShadowObjects = new () ;

        public static void addStatic ( ISoftShadow obj )
        {
            if ( null == obj ) return;
            UnityAction < DropShadow.MessageReason > call = obj.propertiesObjectChanged ;
            m_StaticShadowObjects.AddListener ( obj.propertiesObjectChanged );
        }

        public static void removeStatic ( ISoftShadow obj )
        {
            if ( null == obj ) return;
            UnityAction < DropShadow.MessageReason > call = obj.propertiesObjectChanged ;
            m_StaticShadowObjects.RemoveListener ( obj.propertiesObjectChanged );
        }
#endif

        public void addListener ( ISoftShadow obj )
        {
            if ( null == obj ) return;
            UnityAction < DropShadow.MessageReason > call = obj.propertiesObjectChanged ;
            m_OnPropertiesChanged.AddListener ( obj.propertiesObjectChanged );
            //Log.i ( TAG , "adding listener: " + obj );
            //Log.i ( TAG , "Num Listeners: " + m_OnPropertiesChanged.GetPersistentEventCount () ) ;
            if ( m_Started ) call.Invoke ( DropShadow.MessageReason.ValuesChanged );
        }

        public void removeListener ( ISoftShadow obj )
        {
            //Log.i ( TAG , "removing listener: " + obj );
            if ( null == obj ) return;
            UnityAction < DropShadow.MessageReason > call = obj.propertiesObjectChanged ;
            m_OnPropertiesChanged.RemoveListener ( call );
            //Log.i ( TAG , "Num Listeners: " + m_OnPropertiesChanged.GetPersistentEventCount () ) ;
        }

        private bool m_Started = false ;

        public void RefreshShadows ()
        {
            m_OnPropertiesChanged.Invoke ( DropShadow.MessageReason.ValuesChanged );

        }

        private void ValuesChanged ()
        {
            //CopyVariables () ;
            RefreshShadows ();
        }
        private void OnEnable ()
        {
#if UNITY_EDITOR
            m_StaticShadowObjects.Invoke ( DropShadow.MessageReason.ObjectChanged );
#endif
        }

        private void OnDisable ()
        {
            m_OnPropertiesChanged.Invoke ( DropShadow.MessageReason.ObjectChanged );
        }

        private void Start ()
        {
            m_Started = true;
        }
#if UNITY_EDITOR

        private void OnValidate ()
        {
            m_ShadowProperties.updated ();
            if ( enabled ) RefreshShadows ();
        }

        private void Reset ()
        {
            EditorUtility.SetDirty ( this );
            OnValidate ();
        }

        [ContextMenu ( "Reset Shadow Properties" )]
        public void ResetShadowProperties ()
        {
            shadowProperties.reset ();
            OnValidate ();
        }

        /* switch off menus

        [ContextMenu ( "Switch on all Logs" )]
        private void SwitchOnAllLogs ()
        {
            var Objects = FindObjectsOfType<SoftShadow> ( true ) ;
            foreach ( var ss in Objects )
            {
                ss.enableLog ( true );
                EditorUtility.SetDirty ( ss );

            }
            EditorUtility.SetDirty ( this );
        }


        [ContextMenu ( "Switch off all Logs" )]
        private void SwitchOffAllLogs ()
        {
            var Objects = FindObjectsOfType<SoftShadow> ( true );
            foreach ( var ss in Objects )
            {
                ss.enableLog ( false );
                EditorUtility.SetDirty ( ss );
            }
            EditorUtility.SetDirty ( this );
        }

        [ContextMenu ( "Delete All Shadows" )]
        public void DeleteAllShadows ()
        {
            //if ( !CheckInstance () ) return;
            Log.i ( TAG , "Deleting all shadows" );
            var Objects = FindObjectsOfType<SoftShadow> ( true );
            foreach ( var ss in Objects )
            {
                ss.clear ();
            }
        }
        // */
#endif
    }
}
