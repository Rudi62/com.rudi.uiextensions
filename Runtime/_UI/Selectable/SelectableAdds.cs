using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rudi.UI
{
    [RequireComponent ( typeof ( Selectable ) )]
    [ExecuteAlways]
    [AddComponentMenu ( "Rudi/UI/Selectable Adds" )]

    public class SelectableAdds : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler

    {
        private static readonly string TAG = "Rudis SelectableAdds" ;
        public enum ShadowDrive
        {
            [ Description ( "Full Drive"        ) ] FullDrive,
            [ Description ( "Interactable Only" ) ] InteractableOnly,
            None,
        }

        [ SerializeField ] private bool m_Interactable = true ;

        [ SerializeField ] private ShadowDrive m_ShadowDrive = ShadowDrive.FullDrive ;

        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_ShadowNormal = 1f ;
        [ Range ( 0 , 2 ) ]
        [ SerializeField ] private float m_ShadowHighlighted = 1.3f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_ShadowPressed = 0.2f ;
        [ Range ( 0 , 2 ) ]
        [ SerializeField ] private float m_ShadowSelected = 1.3f ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_ShadowDisabled = 0.0f ;

        [ SerializeField ] private Graphic [] m_AdditionalTargets ;

        public class Distances
        {
            public float ShadowNormal      ;
            public float ShadowHighlighted ;
            public float ShadowPressed     ;
            public float ShadowSelected    ;
            public float ShadowDisabled    ;
        }

        public Distances distances
        {
            get
            {
                var Result = new Distances () ;
                Result.ShadowNormal = m_ShadowNormal;
                Result.ShadowHighlighted = m_ShadowHighlighted;
                Result.ShadowPressed = m_ShadowPressed;
                Result.ShadowSelected = m_ShadowSelected;
                Result.ShadowDisabled = m_ShadowDisabled;
                return Result;
            }
            set
            {
                m_ShadowNormal = value.ShadowNormal;
                m_ShadowHighlighted = value.ShadowHighlighted;
                m_ShadowPressed = value.ShadowPressed;
                m_ShadowSelected = value.ShadowSelected;
                m_ShadowDisabled = value.ShadowDisabled;
                refresh ();
            }
        }

        private Selectable m_Selectable = null ;
        private Selectable selectable
        {
            get
            {
                if ( null == m_Selectable )
                {
                    m_Selectable = GetComponent<Selectable> ();
                }
                return m_Selectable;
            }
        }

        private ButtonV4 m_ButtonV4 = null ;
        private ButtonV4 buttonV4
        {
            get
            {
                if ( null == m_ButtonV4 )
                {
                    m_ButtonV4 = GetComponent<ButtonV4> ();
                }
                return m_ButtonV4;
            }
        }
        private ButtonV3 m_ButtonV3 = null ;
        private ButtonV3 buttonV3
        {
            get
            {
                if ( null == m_ButtonV3 )
                {
                    m_ButtonV3 = GetComponent<ButtonV3> ();
                }
                return m_ButtonV3;
            }
        }

        bool driveSelectable => null == buttonV4;
        bool driveOwnShadow => null == buttonV3;
        private static bool isEnabled ( ISoftShadow sh )
        {
            if ( null == sh ) return false;
            if ( !sh.isEnabled ) return false;
            return true;
        }

        private ISoftShadow m_ActiveShadow = null ;
        private ISoftShadow activeShadow
        {
            get
            {
                if ( null == m_ActiveShadow )
                {
                    if ( null == selectable ) return null ;
                    if ( selectable.transition != Selectable.Transition.ColorTint ) return null ;
                    if ( null == selectable.targetGraphic ) return null ;
                    m_ActiveShadow = selectable.targetGraphic.GetComponent < SoftShadow > () ;
                    if ( null == m_ActiveShadow ) m_ActiveShadow = selectable.targetGraphic.GetComponent < ISoftShadow > () ;
                    if ( isEnabled ( m_ActiveShadow ) ) m_ActiveShadow.omitIfZeroDistance = true ;
                }
                return m_ActiveShadow;
            }
        }

        private bool hasAdditionalTargets
        {
            get
            {
                if ( null == m_AdditionalTargets ) return false;
                return m_AdditionalTargets.Length > 0;
            }
        }

        private float animationDuration
        {
            get
            {
                if ( null == selectable ) return 0;
                if ( selectable.transition != Selectable.Transition.ColorTint ) return 0;
                return selectable.colors.fadeDuration;
            }
        }

        private void OnCanvasGroupChanged () => OnSetProperty ();

        public bool interactable
        {
            get => m_Interactable;
            set
            {
                m_Interactable = value;
                if ( driveSelectable )
                {
                    selectable.interactable = value;
                }
                //else
                if ( driveOwnShadow )
                {
                    currentSelectionState = calcCurrentSelectionState ();
#if UNITY_EDITOR
                    if ( !Application.isPlaying )
                    {
                        DoStateTransition ( currentSelectionState , true );
                    }
                    else
#endif
                        DoStateTransition ( currentSelectionState , false );
                }
            }
        }
        public bool IsInteractable () => m_Interactable && selectable.IsInteractable ();

        // copy of variables of selectables
        // it's a shame that they are protected :-(
        public bool isPointerInside { get; private set; }
        public bool isPointerDown { get; private set; }
        public bool hasSelection { get; private set; }

        public enum SelectionState
        {
            Normal,
            Highlighted,
            Pressed,
            Selected,
            Disabled,
        }

        public SelectionState currentSelectionState { get; private set; }
        private SelectionState calcCurrentSelectionState ()
        {
            if ( !IsInteractable () ) return SelectionState.Disabled;
            if ( isPointerDown ) return SelectionState.Pressed;
            if ( hasSelection ) return SelectionState.Selected;
            if ( isPointerInside ) return SelectionState.Highlighted;
            return SelectionState.Normal;
        }

        private float GetShadowMultiplier ( SelectionState state )
        {
            switch ( m_ShadowDrive )
            {
                case ShadowDrive.FullDrive:
                {
                    switch ( state )
                    {
                        case SelectionState.Normal: return m_ShadowNormal;
                        case SelectionState.Highlighted: return m_ShadowHighlighted;
                        case SelectionState.Pressed: return m_ShadowPressed;
                        case SelectionState.Selected: return m_ShadowSelected;
                        case SelectionState.Disabled: return m_ShadowDisabled;
                        default: return 1f;
                    }
                }
                case ShadowDrive.InteractableOnly: return SelectionState.Disabled == state ? 0 : 1;
                case ShadowDrive.None: return 1f;
                default: return 1f;
            }
        }

        private Color GetColor ( SelectionState state )
        {
            if ( null == selectable ) return Color.white;
            var colors = selectable.colors ;
            switch ( state )
            {
                case SelectionState.Normal: return colors.normalColor;
                case SelectionState.Highlighted: return colors.highlightedColor;
                case SelectionState.Pressed: return colors.pressedColor;
                case SelectionState.Selected: return colors.selectedColor;
                case SelectionState.Disabled: return colors.disabledColor;
                default: return Color.white;
            }
        }

        private void OnSetProperty ( bool dontcare = false )
        {
            currentSelectionState = calcCurrentSelectionState ();
            DoStateTransition ( currentSelectionState , false );
        }

        protected virtual void DoStateTransition ( SelectionState state , bool instant )
        {
            //Log.i ( TAG , gameObject , "DoStateTransition ()" );
            var multiplier = GetShadowMultiplier ( state ) ;
            var duration = instant ? 0f : animationDuration ;
            if ( isEnabled ( activeShadow ) ) activeShadow.CrossFadeElevationMultiplier ( multiplier , duration );
            if ( hasAdditionalTargets )
            {
                var color = GetColor ( state ) ;
                foreach ( var obj in m_AdditionalTargets )
                {
                    if ( null == obj ) continue;
                    obj.CrossFadeColor ( color , duration , true , true );
                    var shadow = obj.GetComponent < SoftShadow > () ;
                    if ( isEnabled ( shadow ) && m_ShadowDrive != ShadowDrive.None )
                    {
                        shadow.omitIfZeroDistance = true;
                        shadow.CrossFadeElevationMultiplier ( multiplier , duration );
                    }
                }
            }
        }

        public void refresh ()
        {
            DoStateTransition ( calcCurrentSelectionState () , true );
        }

#if UNITY_EDITOR
        private void OnValidate ()
        {
            interactable = m_Interactable;
            if ( isActiveAndEnabled )
            {
                currentSelectionState = calcCurrentSelectionState ();
                DoStateTransition ( currentSelectionState , true );
            }
        }
#endif
        public void OnSelect ( BaseEventData eventData ) => OnSetProperty ( hasSelection = true );
        public void OnDeselect ( BaseEventData eventData ) => OnSetProperty ( hasSelection = false );
        public void OnPointerDown ( PointerEventData eventData ) => OnSetProperty ( isPointerDown = true );
        public void OnPointerUp ( PointerEventData eventData ) => OnSetProperty ( isPointerDown = false );
        public void OnPointerEnter ( PointerEventData eventData ) => OnSetProperty ( isPointerInside = true );
        public void OnPointerExit ( PointerEventData eventData ) => OnSetProperty ( isPointerInside = false );
    }

#if UNITY_EDITOR
    [CustomEditor ( typeof ( SelectableAdds ) )]
    [CanEditMultipleObjects]
    public class SelectableAddsEditor : Editor
    {
        SerializedProperty m_Interactable      ;
        SerializedProperty m_ShadowDrive       ;
        SerializedProperty m_ShadowNormal      ;
        SerializedProperty m_ShadowHighlighted ;
        SerializedProperty m_ShadowPressed     ;
        SerializedProperty m_ShadowSelected    ;
        SerializedProperty m_ShadowDisabled    ;
        SerializedProperty m_AdditionalTargets ;

        protected void OnEnable ()
        {
            m_Interactable = serializedObject.FindProperty ( "m_Interactable" );
            m_ShadowDrive = serializedObject.FindProperty ( "m_ShadowDrive" );
            m_ShadowNormal = serializedObject.FindProperty ( "m_ShadowNormal" );
            m_ShadowHighlighted = serializedObject.FindProperty ( "m_ShadowHighlighted" );
            m_ShadowPressed = serializedObject.FindProperty ( "m_ShadowPressed" );
            m_ShadowSelected = serializedObject.FindProperty ( "m_ShadowSelected" );
            m_ShadowDisabled = serializedObject.FindProperty ( "m_ShadowDisabled" );
            m_AdditionalTargets = serializedObject.FindProperty ( "m_AdditionalTargets" );
        }
        public override void OnInspectorGUI ()
        {
            base.CreateInspectorGUI ();
            serializedObject.Update ();

            EditorGUILayout.PropertyField ( m_Interactable );
            EditorGUILayout.PropertyField ( m_ShadowDrive );
            if ( m_ShadowDrive.intValue == ( int ) SelectableAdds.ShadowDrive.FullDrive )
            {
                EditorGUILayout.PropertyField ( m_ShadowNormal );
                EditorGUILayout.PropertyField ( m_ShadowHighlighted );
                EditorGUILayout.PropertyField ( m_ShadowPressed );
                EditorGUILayout.PropertyField ( m_ShadowSelected );
                EditorGUILayout.PropertyField ( m_ShadowDisabled );
            }
            EditorGUILayout.PropertyField ( m_AdditionalTargets );

            //EditorGUILayout.Space ();
            //EditorGUILayout.LabelField ( "Elements" , EditorStyles.boldLabel );
            //EditorGUILayout.PropertyField ( m_HeaderElement );
            //EditorGUILayout.PropertyField ( m_TextElement );
            //if ( GUILayout.Button ( "Start Help Cycle" ) )
            //{
            //    ( target as HelpBubble ).InternalStartHelpCycle ();
            //}

            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif

}
