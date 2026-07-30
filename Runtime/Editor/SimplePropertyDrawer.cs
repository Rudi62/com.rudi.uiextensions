using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Rudi.Core
{
    public abstract class SimplePropertyDrawer : PropertyDrawer
    {
        // makes it easy to create simple property drawers
        // use:
        //
        // #if UNITY_EDITOR
        //    [ CustomPropertyDrawer ( typeof ( MyClass ) ) ]
        //    public class MyClassPropertyDrawer : SimplePropertyDrawer
        //    {
        //        private SerializedProperty m_Prop1 ;
        //        private SerializedProperty m_Prop2 ;
        //        private SerializedProperty m_Prop3 ;
        //        private SerializedProperty m_Prop4 ;
        //        private SerializedProperty m_Prop5 ;
        //
        //        private bool m_MyFoldOut = true ; // opened at begin
        //
        //        protected override void CacheProperties ()
        //        {
        //            m_Prop1 = findProperty ( "m_Prop1" ) ; // whitespaces are trimmed away at the end
        //            m_Prop2 = findProperty ( "m_Prop2" ) ;
        //            m_Prop3 = findProperty ( "m_Prop3" ) ;
        //            m_Prop4 = findProperty ( "m_Prop4" ) ;
        //            m_Prop5 = findProperty ( "m_Prop5" ) ;
        //        }
        //        protected override void PassDrawer ()
        //        {
        //            addLabel ( label ) ; // label is the label of the property - optional
        //            addField ( m_Prop1 ) ;
        //            if ( addFoldout ( "My Folder" , ref m_MyFoldOut ) )
        //            {
        //                incIntend () ;
        //                addField ( m_Prop2 ) ;
        //                decIntend () ;
        //            }
        //            addField ( m_Prop3 ) ;
        //            if ( m_Prop3.boolValue )
        //            {
        //                incIntend () ;
        //                addField ( m_Prop4 ) ;
        //                addField ( m_Prop5 , "Prop Name" ) ;
        //                decIntend () ;
        //            }
        //        }
        //    }
        // #endif

        // https://catlikecoding.com/unity/tutorials/editor/custom-data/
        // https://nosuchstudio.medium.com/learn-unity-editor-scripting-property-drawers-part-2-6fe6097f1586

        private int m_NumElements ;
        private float m_CurrentTotalHeight ;
        private float m_NextElementPosY;
        private int m_InitialIndent ;
        private bool m_Cached = false ;
        private string m_Name ;
        private SerializedProperty m_Property ;
        private Rect? m_Position = null ;

        protected string label => m_Name;

        protected SerializedProperty findProperty ( string name ) => m_Property.FindPropertyRelative ( name.TrimEnd () );

        private Rect? nextRect ( Rect? position , float height )
        {
            m_NumElements++;
            Rect? Result = null ;
            if ( null != position )
            {
                var pos = position.Value ;
                Result = new Rect ( pos.min.x , pos.min.y + m_NextElementPosY , pos.size.x , height );
            }
            m_CurrentTotalHeight = m_NextElementPosY + height;
            m_NextElementPosY = m_CurrentTotalHeight + EditorGUIUtility.standardVerticalSpacing;
            //return getRect ( position , m_Line++ ) ;
            return Result;
        }

        protected Rect? nextRect ( float height ) => nextRect ( m_Position , height );
        protected Rect? nextRect () => nextRect ( EditorGUIUtility.singleLineHeight );
        private static float GetPropertyHeight ( SerializedProperty prop , string label = null )
        {
            if ( string.IsNullOrEmpty ( label ) ) return EditorGUI.GetPropertyHeight ( prop , true );
            return EditorGUI.GetPropertyHeight ( prop , new GUIContent ( label ) , true );
        }

        private static bool AddPropertyField ( Rect rect , SerializedProperty prop , string label , bool readOnly )
        {
            var GUIenabled = GUI.enabled ;
            bool result ;
            if ( readOnly ) GUI.enabled = false;
            if ( string.IsNullOrEmpty ( label ) ) result = EditorGUI.PropertyField ( rect , prop );
            else result = EditorGUI.PropertyField ( rect , prop , new GUIContent ( label ) );
            if ( readOnly ) GUI.enabled = GUIenabled;
            return result;
        }

        private static bool AddSliderField ( Rect rect , SerializedProperty prop , float min_val , float max_val , string label , bool readOnly )
        {
            var GUIenabled = GUI.enabled ;
            if ( readOnly ) GUI.enabled = false;
            if ( string.IsNullOrEmpty ( label ) ) EditorGUI.Slider ( rect , prop , min_val , max_val );
            else EditorGUI.Slider ( rect , prop , min_val , max_val , new GUIContent ( label ) );
            if ( readOnly ) GUI.enabled = GUIenabled;
            return false;
        }

        private bool InternalAddField ( SerializedProperty prop , string label , bool readOnly )
        {
            // https://discussions.unity.com/t/how-to-make-a-readonly-property-in-inspector/75448
            var height = GetPropertyHeight ( prop , label ) ;
            var rect = nextRect ( height ) ;
            if ( null == rect ) return false;
            return AddPropertyField ( rect.Value , prop , label , readOnly );
        }
        private bool InternalAddSlider ( SerializedProperty prop , float min_val , float max_val , string label , bool readOnly )
        {
            // https://discussions.unity.com/t/how-to-make-a-readonly-property-in-inspector/75448
            var height = GetPropertyHeight ( prop , label ) ;
            var rect = nextRect ( height ) ;
            if ( null == rect ) return false;
            return AddSliderField ( rect.Value , prop , min_val , max_val , label , readOnly );
        }

        private void InternalAddToggleValueField ( SerializedProperty prop1 , SerializedProperty prop2 , string label , bool show_always )
        {
            var height1 = GetPropertyHeight ( prop1 ) ;
            var height2 = GetPropertyHeight ( prop2 ) ;
            var rect = nextRect ( Mathf.Max ( height1 , height2 ) ) ;
            if ( null == rect ) return;

            if ( null == label ) label = prop2.displayName;
            var rectContent = EditorGUI.PrefixLabel ( rect.Value , new GUIContent ( label ) ) ;
            rectContent.xMin -= EditorGUIUtility.standardVerticalSpacing * 2f;
            var FirstElementWidth = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing ;
            var rect1 = rectContent ;
            rect1.width = FirstElementWidth;
            var rect2 = rectContent ;
            rect2.xMin += FirstElementWidth;
            var currrlabelWidth = EditorGUIUtility.labelWidth ;
            EditorGUIUtility.labelWidth = EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField ( rect1 , prop1 , null );
            if ( show_always )
            {
                var GUIenabled = GUI.enabled ;
                GUI.enabled = prop1.boolValue;
                EditorGUI.PropertyField ( rect2 , prop2 , null );
                GUI.enabled = GUIenabled;
            }
            else
            {
                if ( prop1.boolValue ) EditorGUI.PropertyField ( rect2 , prop2 , null );
            }
            EditorGUIUtility.labelWidth = currrlabelWidth;
        }

        protected void addToggleValueField ( SerializedProperty prop_toggle , SerializedProperty prop_value , bool show_always = false ) => InternalAddToggleValueField ( prop_toggle , prop_value , null , show_always );
        protected void addToggleValueField ( SerializedProperty prop_toggle , SerializedProperty prop_value , string label , bool show_always = false ) => InternalAddToggleValueField ( prop_toggle , prop_value , label , show_always );
        protected bool addSlider ( SerializedProperty prop , float min_val , float max_val , bool readOnly = false ) => InternalAddSlider ( prop , min_val , max_val , null , readOnly );
        protected bool addField ( SerializedProperty prop , bool readOnly = false ) => InternalAddField ( prop , null , readOnly );
        protected bool addField ( SerializedProperty prop , string label , bool readOnly = false ) => InternalAddField ( prop , label , readOnly );
        protected void addLabel ( string label )
        {
            var rect = nextRect () ;
            if ( null == rect ) return;
            EditorGUI.LabelField ( rect.Value , label );
        }

        protected bool addFoldout ( string label , ref bool fold_out )
        {
            var rect = nextRect () ;
            if ( null == rect ) return fold_out;
            return fold_out = EditorGUI.Foldout ( rect.Value , fold_out , label );
        }

        private void InternalCacheProperties ( SerializedProperty property )
        {
            // https://gamedev.stackexchange.com/questions/122301/how-can-i-create-a-custom-propertydrawer-for-my-point-struct
            if ( m_Cached ) return;
            m_Property = property;
            m_Name = property.displayName;
            SetProperties ();
            m_Property = null;
            m_Cached = true;
        }

        protected void initGUI ( SerializedProperty property , Rect? position = null )
        {
            InternalCacheProperties ( property );
            m_NumElements = 0;
            m_CurrentTotalHeight = 0f;
            m_NextElementPosY = 0f;
            m_Position = position;
            if ( null != position ) m_InitialIndent = EditorGUI.indentLevel;
        }

        protected void incIntend ()
        {
            if ( null != m_Position ) EditorGUI.indentLevel++;
        }

        protected void decIntend ()
        {
            if ( null != m_Position ) EditorGUI.indentLevel--;
        }

        protected int numElements => m_NumElements;

        protected void resume ()
        {
            m_Property = null;
            if ( null != m_Position ) EditorGUI.indentLevel = m_InitialIndent;
        }

        public override sealed float GetPropertyHeight ( SerializedProperty property , GUIContent label )
        {
            initGUI ( property );
            PassDrawer ();
            //float Result = GetPropertyHeight ( m_Line ) ;
            var Result = m_CurrentTotalHeight ;
            resume ();
            return Result;
        }

        public override sealed void OnGUI ( Rect position , SerializedProperty property , GUIContent label )
        {
            initGUI ( property , position );
            EditorGUI.BeginProperty ( position , label , property );
            PassDrawer ();
            EditorGUI.EndProperty ();
            PropertyFinished ();
            resume ();
        }

        protected static float singleLineOffset => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // api
        protected abstract void SetProperties ();
        protected abstract void PassDrawer ();
        protected virtual void PropertyFinished () { }
    }
}
#endif
