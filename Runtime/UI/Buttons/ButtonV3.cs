using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using Rudi.RMath;

namespace Rudi.UI
{
    [AddComponentMenu ( "Rudi/UI/Button V3" )]

    public class ButtonV3 : Button
    {
        private static readonly string TAG = "Rudis ButtonV3" ;

        [ Header ( "Shadow Multipliers to Animate" ) ]
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_ShadowNormal = 1f ;
        [ Range ( 0 , 2 ) ]
        [ SerializeField ] private float m_ShadowHighlighted = 1.3f ;
        [ Range ( -1 , 1 ) ]
        [ SerializeField] private float m_ShadowPressed = 0.2f ;
        [ Range ( 0 , 2 ) ]
        [ SerializeField ] private float m_ShadowSelected = 1.3f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_ShadowDisabled = 0.0f ;

        [ Header ( "Background Object" ) ]
        [ SerializeField ] private RawImageBG m_RawImageBG ;
        [ SerializeField ] private bool m_HighlightBackgroundObject = false ;
        [ SerializeField ] private Color m_ColorBackground = new Color32 ( 230 , 160 , 4 , 255 ) ;

        [ Header ( "Additional Shadow Object" ) ]
        [ SerializeField ] private GameObject m_AdditionalShadowObject ;
        [ SerializeField ] private bool m_AnimateAdditionalObject = false ;
        [ SerializeField ] private bool m_AnimateAllChildShadows = false ;

        [ Header ( "Long Press" ) ]
        [ SerializeField ] private bool m_EnableLongPress = false ;
        [ Tooltip ( "Hold duration in seconds" ) ]
        [ Range ( 0.3f , 5f ) ]
        [ SerializeField ] private float m_HoldDuration = 0.5f ;
        [ SerializeField ] private bool m_PerformClickAfterLongPress = false ;
        [ SerializeField ] private UnityEvent m_OnLongPress = new () ;

        public UnityEvent onLongPress => m_OnLongPress;

        public bool enableLongPress
        {
            get => m_EnableLongPress;
            set => m_EnableLongPress = value;
        }

        private bool highlightBackgroundObject
        {
            get
            {
                if ( null == rawImageBG ) return false;
                return m_HighlightBackgroundObject;
            }
        }

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
                //m_SoftShadow = null != m_CachedTargetGraphic ? m_CachedTargetGraphic.GetComponent < ISoftShadow > () : null ;
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

        ISoftShadow [] m_ShadowsToDrive ;
        ISoftShadow [ ] shadowsToDrive
        {
            get
            {
                if ( null == m_ShadowsToDrive )
                {
                    m_ShadowsToDrive = GetComponentsInChildren<ISoftShadow> ();
                }
                return m_ShadowsToDrive;
            }
        }

        private ISoftShadow m_IAdditionalShadowObject = null ;

        private ISoftShadow additionalShadowObject
        {
            get
            {
                if ( null == m_IAdditionalShadowObject )
                {
                    if ( null == m_AdditionalShadowObject ) return null;
                    m_IAdditionalShadowObject = m_AdditionalShadowObject.GetComponent<ISoftShadow> ();
                }
                return m_IAdditionalShadowObject;
            }
        }
        private RawImageBG rawImageBG => m_RawImageBG_toUse;

        //private float m_CurrentShadowMultiplier = 0f ;

        private bool m_SuppressClick = false ;
        private bool m_IsPointerInside = false ;

        private bool shouldAnimate ()
        {
            if ( !gameObject.activeInHierarchy ) return false;
            if ( m_EnableLongPress ) return true;
            if ( colors.fadeDuration > 0f ) return true;
            return false;
        }

        private static bool isEnabled ( ISoftShadow sh )
        {
            if ( null == sh ) return false ;
            if ( !sh.isEnabled ) return false ;
            return true ;
        }

        private void SetShadows ( float shadowMultiplier )
        {
            if ( isEnabled ( activeShadow ) )
            {
                activeShadow.omitIfZeroDistance = true;
                activeShadow.elevationMultiplier = shadowMultiplier;
            }
            if ( m_AnimateAdditionalObject && null != m_AdditionalShadowObject && isEnabled ( additionalShadowObject ) )
            {
                additionalShadowObject.omitIfZeroDistance = true;
                additionalShadowObject.elevationMultiplier = shadowMultiplier;
            }
            if ( m_AnimateAllChildShadows && null != shadowsToDrive )
            {
                foreach ( var shadow in shadowsToDrive )
                {
                    if ( !shadow.isEnabled ) continue ;
                    shadow.elevationMultiplier = shadowMultiplier;
                }
            }
        }

        private static void startShadowAnimaton ( ISoftShadow obj , float shadowDest , float duration )
        {
            if ( !isEnabled ( obj ) ) return ;
            obj.omitIfZeroDistance = true ;
            obj.CrossFadeElevationMultiplier ( shadowDest , duration ) ;
        }

        private void doTransition ( SelectionState state , bool instant )
        {
            if ( state == m_LastState ) return;
            m_LastState = state;
            if ( highlightBackgroundObject ) // && !IsPressed () && !m_IsPointerInside )
            {
                rawImageBG.setColor ( m_ColorBackground );
                rawImageBG.setDraw ( IsPressed () );
            }
            if ( state == SelectionState.Pressed ) m_SuppressClick = false;
            var shadowMultiplier = GetShadowMultiplier ( state ) ;
            if ( instant || !shouldAnimate () )
            {
                SetShadows ( shadowMultiplier );
                if ( null != m_AdditionalShadowObject && !m_AnimateAdditionalObject && isEnabled ( additionalShadowObject ) )
                {
                    additionalShadowObject.omitIfZeroDistance = true;
                    additionalShadowObject.elevationMultiplier = state == SelectionState.Disabled ? 0 : 1;
                }
                //m_CurrentShadowMultiplier = shadowMultiplier;
            }
            else
            {
                // use their own tweeners instead of my coroutine
                startShadowAnimaton ( activeShadow , shadowMultiplier , colors.fadeDuration ) ;
                if ( m_AnimateAdditionalObject ) startShadowAnimaton ( additionalShadowObject , shadowMultiplier , colors.fadeDuration ) ;
                if ( m_EnableLongPress && IsPressed () && m_OnLongPress != null && m_IsPointerInside )
                {
                    StopAnimation ();
                    m_Animation = Animation ( colors.fadeDuration , shadowMultiplier );
                    StartCoroutine ( m_Animation );
                }
            }
        }

        public override void OnPointerClick ( PointerEventData eventData )
        {
            var performClick = !m_SuppressClick ;
            m_SuppressClick = false;
            if ( performClick ) base.OnPointerClick ( eventData );
        }

        protected override void DoStateTransition ( SelectionState state , bool instant )
        {
            checkTargetGraphic ();
            doTransition ( state , instant );
            base.DoStateTransition ( state , instant );
        }

        private void StopAnimation ()
        {
            if ( null != m_Animation )
            {
                //Log.i ( TAG , "stopping animation" ) ;
                StopCoroutine ( m_Animation );
                m_Animation = null;
            }
        }

        // this no longer animates anything, but is for long press only, so maybe consider renaming...
        private IEnumerator m_Animation = null ;
        private IEnumerator Animation ( float duration , float shadowDest )
        {
            float pos = 0 ;
            // shadow animation now in their own tweeners
            //var shadowStart = m_CurrentShadowMultiplier ;
            //if ( null != activeShadow ) activeShadow.omitIfZeroDistance = true;
            //if ( null != additionalShadowObject ) additionalShadowObject.omitIfZeroDistance = true;
            //while ( pos < duration )
            //{
            //    var percent = SmoothStep.ease_out4 ( pos / duration );
            //    var ShadowValue = Mathf.Lerp ( shadowStart , shadowDest , percent ) ;
            //    m_CurrentShadowMultiplier = ShadowValue;
            //    SetShadows ( ShadowValue );
            //    yield return null;
            //    pos += Time.unscaledDeltaTime;
            //}
            //SetShadows ( shadowDest );

            // handle long press
            yield return null;
            pos += Time.unscaledDeltaTime;

            if ( m_EnableLongPress && IsPressed () && m_OnLongPress != null && m_IsPointerInside )
            {
                while ( pos < m_HoldDuration && IsPressed () && m_IsPointerInside )
                {
                    yield return null;
                    pos += Time.unscaledDeltaTime;
                }
                if ( IsPressed () && m_IsPointerInside )
                {
                    m_SuppressClick = !m_PerformClickAfterLongPress;
                    m_OnLongPress.Invoke ();
                }
            }
            m_Animation = null;
        }

        private float GetShadowMultiplier ( SelectionState state )
        {
            switch ( state )
            {
                case SelectionState.Normal: return m_ShadowNormal;
                case SelectionState.Highlighted: return m_ShadowHighlighted;
                case SelectionState.Pressed: return m_ShadowPressed;
                case SelectionState.Selected: return m_ShadowSelected;
                case SelectionState.Disabled: return m_ShadowDisabled;
                default: return 0f;
            }
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

        private void OnTransformChildrenChanged ()
        {
            m_ShadowsToDrive = null;
        }


#if UNITY_EDITOR
        protected override void OnValidate ()
        {
            base.OnValidate ();
            if ( null != m_AdditionalShadowObject )
            {
                if ( null == m_AdditionalShadowObject.GetComponent<ISoftShadow> () )
                {
                    Debug.LogError ( "object must have ISoftShadow-interface!" );
                    m_AdditionalShadowObject = null;
                }
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor ( typeof ( ButtonV3 ) )]
    [CanEditMultipleObjects]
    public class ButtonV3Editor : UnityEditor.UI.ButtonEditor
    {
        SerializedProperty m_ShadowNormal               ;
        SerializedProperty m_ShadowHighlighted          ;
        SerializedProperty m_ShadowPressed              ;
        SerializedProperty m_ShadowSelected             ;
        SerializedProperty m_ShadowDisabled             ;

        SerializedProperty m_RawImageBG                 ;
        SerializedProperty m_HighlightBackgroundObject  ;
        SerializedProperty m_ColorBackground            ;

        SerializedProperty m_AdditionalShadowObject     ;
        SerializedProperty m_AnimateAdditionalObject    ;
        SerializedProperty m_AnimateAllChildShadows     ;

        SerializedProperty m_EnableLongPress            ;
        SerializedProperty m_HoldDuration               ;
        SerializedProperty m_PerformClickAfterLongPress ;
        SerializedProperty m_OnLongPress                ;

        //float NeededSize ;

        protected override void OnEnable ()
        {
            base.OnEnable ();
            m_ShadowNormal = serializedObject.FindProperty ( "m_ShadowNormal" );
            m_ShadowHighlighted = serializedObject.FindProperty ( "m_ShadowHighlighted" );
            m_ShadowPressed = serializedObject.FindProperty ( "m_ShadowPressed" );
            m_ShadowSelected = serializedObject.FindProperty ( "m_ShadowSelected" );
            m_ShadowDisabled = serializedObject.FindProperty ( "m_ShadowDisabled" );

            m_RawImageBG = serializedObject.FindProperty ( "m_RawImageBG" );
            m_HighlightBackgroundObject = serializedObject.FindProperty ( "m_HighlightBackgroundObject" );
            m_ColorBackground = serializedObject.FindProperty ( "m_ColorBackground" );

            m_AdditionalShadowObject = serializedObject.FindProperty ( "m_AdditionalShadowObject" );
            m_AnimateAdditionalObject = serializedObject.FindProperty ( "m_AnimateAdditionalObject" );
            m_AnimateAllChildShadows = serializedObject.FindProperty ( "m_AnimateAllChildShadows" );

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
            EditorGUILayout.PropertyField ( m_ShadowNormal );
            EditorGUILayout.PropertyField ( m_ShadowHighlighted );
            EditorGUILayout.PropertyField ( m_ShadowPressed );
            EditorGUILayout.PropertyField ( m_ShadowSelected );
            EditorGUILayout.PropertyField ( m_ShadowDisabled );

            EditorGUILayout.PropertyField ( m_RawImageBG );
            EditorGUILayout.PropertyField ( m_HighlightBackgroundObject );
            EditorGUILayout.PropertyField ( m_ColorBackground );

            EditorGUILayout.PropertyField ( m_AdditionalShadowObject );
            EditorGUILayout.PropertyField ( m_AnimateAdditionalObject );
            EditorGUILayout.PropertyField ( m_AnimateAllChildShadows );

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
