using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    [ExecuteAlways]
    [AddComponentMenu ( "Rudi/UI/Raw Image BG" , 12 )]

    public class RawImageBG : RawImage
    {
        private static readonly string TAG = "Rudis RawImageBG" ;

        [ SerializeField ] public bool m_Draw = false ;

        public bool draw
        {
            get => m_Draw;
            set
            {
                if ( m_Draw != value )
                {
                    m_Draw = value;
                    SetVerticesDirty ();
                    SetMaterialDirty ();
                }
            }
        }
        public override Texture mainTexture => m_Draw ? s_WhiteTexture : null;
        public void setDraw ( bool draw ) => this.draw = draw;
        public void setColor ( Color color ) => this.color = color;
    }
#if UNITY_EDITOR
    [CustomEditor ( typeof ( RawImageBG ) )]
    [CanEditMultipleObjects]
    public class RawImageBGEditor : UnityEditor.UI.GraphicEditor
    {
        SerializedProperty m_Draw ;
        protected override void OnEnable ()
        {
            base.OnEnable ();
            m_Draw = serializedObject.FindProperty ( "m_Draw" );
        }
        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            EditorGUILayout.PropertyField ( m_Draw );
            EditorGUILayout.PropertyField ( m_Color );
            RaycastControlsGUI ();
            MaskableControlsGUI ();
            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif

}
