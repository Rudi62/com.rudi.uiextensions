using System.Collections;
using Rudi.Core;
using Rudi.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rudi.UI
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent ( typeof ( InputField ) )]
    public class InputFieldEnhancer : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
    {
        private static readonly string TAG = "Rudis InputFieldEnhancer" ;
        private static Color m_DefaultFrameColor = new Color32 ( 168 , 206 , 255 , 192 ) ;

        // https://discussions.unity.com/t/inputfield-s-inside-a-scrollview/649471/6
        [ SerializeField ] private bool  m_DrawFrameOnEdit = true ;
        [ Range ( 0 , 10 ) ]
        [ SerializeField ] private float m_FrameWidth      = 5 ;
        [ Range ( 0 , 10 ) ]
        [ SerializeField ] private float m_Gap             = 0 ;
        [ SerializeField ] private bool  m_RoundCorners    = true ;

        private ScrollRect m_ScrollRect = null ;
        private InputField m_InputField = null ;
        private bool m_isDragging = false ;
        private bool m_isEditing = false ;
        private RawImageV2 m_FrameObject = null ;
        private RawImageV2 frameObject
        {
            get
            {
                if ( null == m_FrameObject )
                {
                    m_FrameObject = createFrameObject ();
                    m_FrameObject.drawTextureOnly = true;
                }
                return m_FrameObject;
            }
        }

        private RawImageV2 createFrameObject ()
        {
            var obj = new GameObject ( "Frame" , typeof ( RawImageV2 ) ) ;
            obj.transform.SetParent ( transform , false );
            var ri = obj.GetComponent < RawImageV2 > () ;
            return ri;
        }

        private void destroyFrameObject ()
        {
            if ( null == m_FrameObject ) return;
            Utils.DestroyObject ( m_FrameObject.gameObject );
            m_FrameObject = null;
        }

        private ScrollRect scrollRect => this.getComponentInParentIfZero ( ref m_ScrollRect );

        private InputField inputField => this.getComponentIfZero ( ref m_InputField );

        float getRadius ()
        {
            if ( null == inputField ) return 0;
            if ( null == inputField.targetGraphic ) return 0;
            var ri = inputField.targetGraphic.GetComponent < RawImageV2 > () ;
            if ( null == ri ) return 0;
            return ri.getRadiusToDraw ();
        }

        Vector4 getRadii ()
        {
            if ( null == inputField ) return Vector4.one;
            if ( null == inputField.targetGraphic ) return Vector4.one;
            var ri = inputField.targetGraphic.GetComponent < RawImageV2 > () ;
            if ( null == ri ) return Vector4.one;
            return ri.radii;
        }

        private void initFrameObject ()
        {
            if ( m_DrawFrameOnEdit )
            {
                if ( null != m_FrameObject )
                {
                    setupFrame ( m_FrameObject );
                    m_FrameObject.drawTextureOnly = !m_isEditing;
                }
            }
            else
            {
                destroyFrameObject ();
            }
        }

        private bool setupFrame ( RawImageV2 ri )
        {
            if ( null == ri ) return false;
            var rt = ri.rectTransform ;
            var Offset = m_FrameWidth + m_Gap ;
            //Log.i ( TAG , "Offset = " + Offset );
            rt.stretch ( -Offset );
            //m_Frame.color = m_ColorFrame ;
            ri.color = inputField.selectionColor.GetWithAlpha ( 0.6f );
            ri.drarBorder = true;
            ri.borderWidth = m_FrameWidth;
            ri.borderColor = Color.white;
            ri.fillColor = Color.white.GetWithAlpha ( 0f );
            ri.raycastTarget = false;
            ri.transform.localScale = Vector3.one;
            if ( m_RoundCorners )
            {
                var Radius = getRadius () + Offset ;
                ri.radius = Radius;
                ri.radii = getRadii ();
            }
            return true;
        }

        bool m_Started = false ;
        private void Start ()
        {
            m_Started = true;
            OnEnable ();
        }

        private void deselect ()
        {
            if ( gameObject == EventSystem.current.currentSelectedGameObject )
            {
                EventSystem.current.SetSelectedGameObject ( null );
            }
        }

        private void OnEnable ()
        {
            if ( !m_Started ) return;
            inputField.DeactivateInputField ();
            inputField.onEndEdit.AddListener ( OnInputEnded );
            initFrameObject ();
        }

        private void showFrame ()
        {
            if ( !m_DrawFrameOnEdit ) return;
            if ( !setupFrame ( frameObject ) ) return;
            frameObject.drawTextureOnly = false;
        }

        private void hideFrame ()
        {
            if ( null == m_FrameObject ) return;
            m_FrameObject.drawTextureOnly = true;
        }

        private void OnDisable ()
        {
            inputField.onEndEdit.RemoveListener ( OnInputEnded );
        }

        private void OnInputEnded ( string v )
        {
            m_isEditing = false;
            hideFrame ();
        }

        private void sendMessageToScrollRect ( string methodName , object value )
        {
            if ( null != scrollRect )
            {
                scrollRect.SendMessage ( methodName , value );
            }
        }

        private static bool isHorizontal ( Vector2 v ) => Mathf.Abs ( v.x ) > Mathf.Abs ( v.y );

        public void OnBeginDrag ( PointerEventData data )
        {
            if ( null == scrollRect ) return;
            // ignore horizontal drags
            if ( m_isEditing && isHorizontal ( data.delta ) ) return;
            m_isDragging = true;
            inputField.DeactivateInputField ();
            inputField.enabled = false;
            sendMessageToScrollRect ( "OnBeginDrag" , data );
        }

        public void OnEndDrag ( PointerEventData data )
        {
            if ( !m_isDragging ) return;
            if ( null == m_ScrollRect ) return;
            m_isDragging = false;
            sendMessageToScrollRect ( "OnEndDrag" , data );
            deselect ();
            inputField.enabled = true;
        }

        public void OnDrag ( PointerEventData data )
        {
            if ( !m_isDragging ) return;
            if ( null == m_ScrollRect ) return;
            sendMessageToScrollRect ( "OnDrag" , data );
        }

        public void OnPointerClick ( PointerEventData data )
        {
            if ( !m_isDragging && !data.dragging && m_InputField.interactable )
            {
                m_InputField.ActivateInputField ();
                m_isEditing = true;
                showFrame ();
            }
        }

#if UNITY_EDITOR
        private void OnValidate ()
        {
            //if ( Application.isPlaying ) return ;
            initFrameObject ();
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor ( typeof ( InputFieldEnhancer ) )]
    [CanEditMultipleObjects]
    public class InputFieldEnhancerEditor : Editor
    {
        SerializedProperty m_DrawFrameOnEdit ;
        SerializedProperty m_FrameWidth      ;
        SerializedProperty m_Gap             ;
        SerializedProperty m_RoundCorners    ;

        protected virtual void OnEnable ()
        {
            m_DrawFrameOnEdit = serializedObject.FindProperty ( "m_DrawFrameOnEdit" );
            m_FrameWidth = serializedObject.FindProperty ( "m_FrameWidth" );
            m_Gap = serializedObject.FindProperty ( "m_Gap" );
            m_RoundCorners = serializedObject.FindProperty ( "m_RoundCorners" );
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            EditorGUILayout.PropertyField ( m_DrawFrameOnEdit );
            if ( m_DrawFrameOnEdit.boolValue )
            {
                EditorGUILayout.PropertyField ( m_FrameWidth );
                EditorGUILayout.PropertyField ( m_Gap );
                EditorGUILayout.PropertyField ( m_RoundCorners );
            }
            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif
}
