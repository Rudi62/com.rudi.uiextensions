using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rudi.UI
{
    [AddComponentMenu ( "Rudi/UI/Button V4" )]

    public class ButtonV4 : Button
    {
        private static readonly string TAG = "Rudis ButtonV4" ;

        [ Header ( "Background Object" ) ]
        [ SerializeField ] private RawImageBG m_RawImageBG ;
        [ SerializeField ] private bool m_HighlightBackgroundObject = false ;
        [ SerializeField ] private Color m_ColorBackground = new Color32 ( 230 , 160 , 4 , 255 ) ;

        [ Header ( "Long Press" ) ]
        [ SerializeField ] private bool m_EnableLongPress = false ;
        [ Tooltip ( "Hold duration in seconds" ) ]
        [ Range ( 0.3f , 5f ) ]
        [ SerializeField ] private float m_HoldDuration = 0.5f ;
        [ SerializeField ] private bool m_PerformClickAfterLongPress = false ;
        [ SerializeField ] private UnityEvent m_OnLongPress ;

        private SelectionState m_LastState = ( SelectionState ) ( -1 ) ;

        private Graphic m_CachedTargetGraphic = null ;

        private ISoftShadow m_SoftShadow = null ;
        private ISoftShadow activeShadow => m_SoftShadow;
        private RawImageBG m_RawImageBG_toUse = null ;
        private void checkTargetGraphic ()
        {
            if ( targetGraphic != m_CachedTargetGraphic )
            {
                m_CachedTargetGraphic = targetGraphic;
                //m_SoftShadow = null != m_CachedTargetGraphic ? m_CachedTargetGraphic.GetComponent < SoftShadow > () : null ;
                m_SoftShadow = null;
                if ( null != m_CachedTargetGraphic )
                {
                    m_SoftShadow = m_CachedTargetGraphic.GetComponent<SoftShadow> ();
                    if ( null == m_SoftShadow ) m_SoftShadow = m_CachedTargetGraphic.GetComponent<ISoftShadow> ();
                }
                m_RawImageBG_toUse = m_RawImageBG;
                if ( null == m_RawImageBG_toUse )
                {
                    m_RawImageBG_toUse = m_CachedTargetGraphic as RawImageBG;
                }
                if ( null == m_RawImageBG_toUse )
                {
                    m_RawImageBG_toUse = GetComponent<RawImageBG> ();
                }
            }
        }

        private RawImageBG rawImageBG => m_RawImageBG_toUse;

        private float m_CurrentShadowMultiplier = 0f ;

        private bool m_SuppressClick = false ;
        private bool m_IsPointerInside = false ;
        private bool m_SelectableInteractable ;
        private SelectableAdds m_SelectableAdds = null ;
        private SelectableAdds selectableAdds
        {
            get
            {
                if ( null == m_SelectableAdds )
                {
                    m_SelectableAdds = GetComponent<SelectableAdds> ();
                    if ( null != m_SelectableAdds )
                    {
                        m_SelectableInteractable = interactable;
                        m_SelectableAdds.interactable = interactable;
                    }
                }
                return m_SelectableAdds;
            }
        }

        private bool shouldAnimate ()
        {
            if ( !gameObject.activeInHierarchy ) return false;
            if ( m_EnableLongPress ) return true;
            if ( colors.fadeDuration > 0f ) return true;
            return false;
        }

        private void doTransition ()
        {
            if ( m_HighlightBackgroundObject ) // && !IsPressed () && !m_IsPointerInside )
            {
                if ( null != rawImageBG )
                {
                    rawImageBG.setColor ( m_ColorBackground );
                    rawImageBG.setDraw ( IsPressed () );
                }
            }
        }

        protected override void DoStateTransition ( SelectionState state , bool instant )
        {
            doTransition ();
            if ( null != selectableAdds )
            {
                if ( m_SelectableInteractable != interactable )
                {
                    m_SelectableInteractable = interactable;
                    selectableAdds.interactable = interactable;
                }
            }
            base.DoStateTransition ( state , instant );
        }

        public override void OnPointerClick ( PointerEventData eventData )
        {
            var performClick = !m_SuppressClick ;
            m_SuppressClick = false;
            if ( performClick ) base.OnPointerClick ( eventData );
        }

        public override void OnPointerEnter ( PointerEventData eventData )
        {
            m_IsPointerInside = true;
            base.OnPointerEnter ( eventData );
        }

        public override void OnPointerExit ( PointerEventData eventData )
        {
            m_IsPointerInside = false;
            base.OnPointerExit ( eventData );
        }

        protected override void Start ()
        {
            checkTargetGraphic ();
            base.Start ();
        }
    }
#if UNITY_EDITOR
    [CustomEditor ( typeof ( ButtonV4 ) )]
    [CanEditMultipleObjects]
    public class ButtonV4Editor : UnityEditor.UI.ButtonEditor
    {
        SerializedProperty m_RawImageBG                 ;
        SerializedProperty m_HighlightBackgroundObject  ;
        SerializedProperty m_ColorBackground            ;

        SerializedProperty m_EnableLongPress            ;
        SerializedProperty m_HoldDuration               ;
        SerializedProperty m_PerformClickAfterLongPress ;
        SerializedProperty m_OnLongPress                ;

        //float NeededSize ;

        protected override void OnEnable ()
        {
            base.OnEnable ();

            m_RawImageBG = serializedObject.FindProperty ( "m_RawImageBG" );
            m_HighlightBackgroundObject = serializedObject.FindProperty ( "m_HighlightBackgroundObject" );
            m_ColorBackground = serializedObject.FindProperty ( "m_ColorBackground" );

            m_EnableLongPress = serializedObject.FindProperty ( "m_EnableLongPress" );
            m_HoldDuration = serializedObject.FindProperty ( "m_HoldDuration" );
            m_PerformClickAfterLongPress = serializedObject.FindProperty ( "m_PerformClickAfterLongPress" );
            m_OnLongPress = serializedObject.FindProperty ( "m_OnLongPress" );

            //NeededSize = EditorStyles.label.CalcSize ( new GUIContent ( m_PerformClickAfterLongPress.displayName ) ).x + 15 ;
        }
        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI ();
            EditorGUILayout.Space ();

            serializedObject.Update ();
            EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize ( new GUIContent ( m_PerformClickAfterLongPress.displayName ) ).x + 15;

            EditorGUILayout.PropertyField ( m_RawImageBG );
            EditorGUILayout.PropertyField ( m_HighlightBackgroundObject );
            EditorGUILayout.PropertyField ( m_ColorBackground );

            EditorGUILayout.PropertyField ( m_EnableLongPress );
            if ( m_EnableLongPress.boolValue )
            {
                EditorGUILayout.PropertyField ( m_HoldDuration );
                EditorGUILayout.PropertyField ( m_PerformClickAfterLongPress );
                EditorGUILayout.PropertyField ( m_OnLongPress );
            }
            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif
}
