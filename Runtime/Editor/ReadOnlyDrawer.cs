using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rudi.Core
{
    [Serializable]
    public struct ReadOnlyArray<T>
    {
        public T[] Values;

        // Allows direct access via m_Shaders[i]
        public T this [int index]
        {
            get => Values [index];
            set => Values [index] = value; // Fixed: Now correctly assigns the incoming value
        }

        // Provides direct access to the array length
        public int Length => Values?.Length ?? 0;

        // Returns the array as a high-performance ReadOnlySpan
        public ReadOnlySpan<T> AsSpan () => Values.AsSpan ();

        // Implizite Konvertierung, damit Sie im Code wie mit einem normalen Array arbeiten können
        public static implicit operator T [ ] ( ReadOnlyArray<T> readOnlyArray ) => readOnlyArray.Values;
    }

    // https://discussions.unity.com/t/how-to-make-a-readonly-property-in-inspector/75448/5
    public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer ( typeof ( ReadOnlyAttribute ) )]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight ( SerializedProperty property , GUIContent label )
        {
            return EditorGUI.GetPropertyHeight ( property , label , true );
        }

        public override void OnGUI ( Rect position , SerializedProperty property , GUIContent label )
        {
            var wasEnabled = GUI.enabled ;
            GUI.enabled = false;
            EditorGUI.PropertyField ( position , property , label , true );
            GUI.enabled = wasEnabled;
        }
    }
#endif
}
