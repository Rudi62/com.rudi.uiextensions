using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEditor;
using Rudi.Extensions;
using Rudi.Core;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Rudi.UI
{
    [ExecuteAlways]
    public class ShadowReciever : RawImage, ILayoutSelfController
    {
        private static readonly string TAG = "Rudis ShadowReciever";

        private static readonly Vector2 Vec2Half = new Vector2 ( 0.5f , 0.5f );

        private SoftShadow SoftShadowObject = null;

        private void log ( string msg ) => SoftShadowObject?.log ( TAG , msg );

        //private Vector2 ShadowPos ;
        //private Vector2 ShadowSize ;
        public override Texture mainTexture => m_ShadowEnabled ? texture : null;
        public override Color color
        {
            get => null != SoftShadowObject ? SoftShadowObject.shadowColor : Color.clear;
            set
            {
                if ( null != SoftShadowObject ) base.color = SoftShadowObject.shadowColor;
            }
        }

        public override bool Raycast ( Vector2 sp , Camera eventCamera )
        {
            return false;
        }

        private bool m_ShadowEnabled = true ;
        public bool shadowEnabled
        {
            get => m_ShadowEnabled;
            set
            {
                m_ShadowEnabled = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        private RectTransform m_RectTransform;
        private RectTransform myRectTransform
        {
            get
            {
                if ( null == m_RectTransform )
                {
                    if ( null == SoftShadowObject ) return null;
                    m_RectTransform = GetComponent<RectTransform> ();
                    if ( null == m_RectTransform )
                    {
                        m_RectTransform = gameObject.AddComponent<RectTransform> ();
                    }
                    m_RectTransform.anchorMin = Vec2Half;
                    m_RectTransform.anchorMax = Vec2Half;
                    m_RectTransform.pivot = Vec2Half;
                    m_RectTransform.sizeDelta = Vector2.zero;
                    transform.localScale = Vector3.one;
                }
                return m_RectTransform;
            }
        }

#if UNITY_EDITOR
        private static bool isInPrefabStage ( GameObject obj )
        {
            var stage = PrefabStageUtility.GetPrefabStage ( obj );
            return null != stage;
        }

        private static bool isInPrefabStage ( MonoBehaviour mono )
        {
            var stage = PrefabStageUtility.GetPrefabStage ( mono.gameObject ) ;
            return null != stage;
        }
        private static GameObject GetParent ( Transform transform )
        {
            if ( null == transform ) return null;
            if ( null == transform.parent ) return null;
            return transform.parent.gameObject;
        }
#endif

        public void AdjustSiblingIndex ()
        {
            if ( null == SoftShadowObject ) return;
            if ( !m_Started ) return;
            try
            {
                if ( SoftShadowObject.transform.parent != transform.parent )
                {
                    //Log.i ( TAG , "adjusting parent" );
                    if ( null == SoftShadowObject.transform.parent ) return;
                    if ( null == SoftShadowObject.transform.parent.gameObject ) return;
                    //if ( SoftShadowObject.transform.parent.parent == null ) return;
                    //if ( isInPrefabStage ( SoftShadowObject.transform.parent.gameObject ) ) return ;
                    //if ( isInPrefabStage ( gameObject ) ) return ;
                    //if ( PrefabStageUtility.GetPrefabStage ( SoftShadowObject.transform.parent.gameObject ).IsPartOfPrefabContents ( SoftShadowObject.transform.parent.gameObject ) ) return;
#if UNITY_EDITOR
                    if ( PrefabUtility.IsPartOfPrefabAsset ( SoftShadowObject ) ) return;
#endif

                    transform.SetParent ( SoftShadowObject.transform.parent , false );
                }
                var myIndex = transform.GetSiblingIndex ();
                var ShadowIndex = SoftShadowObject.transform.GetSiblingIndex ();
                if ( myIndex == ShadowIndex - 1 ) return;
                //Log.i ( TAG , "adjusting sibling index" ) ;
                if ( myIndex < ShadowIndex ) transform.SetSiblingIndex ( ShadowIndex - 1 );
                else transform.SetSiblingIndex ( ShadowIndex );
                //RegisterUpdatePosition ();
            }
            catch ( Exception e )
            {
                transform.SetParent ( null );
                log ( "couldn't set parent" );
            }
        }

        public void RegisterUpdatePosition ()
        {
            if ( null == SoftShadowObject ) return;
            registerCallback ();
        }

        private CanvasCallbacks m_Callbacks = null ;
        private CanvasCallbacks callbacks => m_Callbacks ??= new CanvasCallbacks ( OnCanvasCallback , this , CanvasCallbacks.Timing.Early );
        private void OnCanvasCallback () => UpdatePosition ();
        private void registerCallback ()
        {
            if ( !m_Started ) return;
            callbacks.StartAsap ();
        }

        private void unregisterCallback () => m_Callbacks?.requestStop ();

        public void UpdatePosition ( bool performparenting = true )
        {
            if ( !m_Started ) return;
            if ( null == SoftShadowObject )
            {
                shadowEnabled = false;
                return;
            }
            AdjustSiblingIndex ();
            shadowEnabled = SoftShadowObject.shadowEnabled;
            if ( null == rectTransform ) return;
            if ( !enabled ) return;
            rectTransform.UpdatePosition ( SoftShadowObject.shadowCenter );
            rectTransform.UpdateSize ( SoftShadowObject.shadowSize );
        }

        private bool m_Started = false ;
        protected override void Start ()
        {
            log ( "Start ()" );
            base.Start ();
            m_Started = true;
            if ( null == SoftShadowObject )
            {
                Utils.DestroyObject ( gameObject );
            }
            else
            {
                UpdatePosition ();
                callbacks.StartSingle ( 2 );
            }
        }

        protected override void OnEnable ()
        {
            log ( "OnEnable ()" );
            base.OnEnable ();
            if ( null != SoftShadowObject )
            {
                shadowEnabled = SoftShadowObject.shadowEnabled;
                RegisterUpdatePosition ();
            }
        }

        protected override void OnDisable ()
        {
            //if ( CanvasUpdateRegistry.IsRebuildingLayout () ) return;
            unregisterCallback ();
            shadowEnabled = false;
            log ( "OnDisable ()" );
            base.OnDisable ();
        }

        protected override void OnDestroy ()
        {
            unregisterCallback ();
            if ( null != SoftShadowObject )
            {
                SoftShadowObject.OnShadowRecieverDestroyed ();
            }
            SoftShadowObject = null;
            base.OnDestroy ();
        }

        public static ShadowReciever Create ( SoftShadow shadow , Texture texture )
        {
            //Log.i ( TAG , "Create (), texture = " + texture );
            var obj = new GameObject ( "Shadow " + shadow ) ;


            // layout
            obj.AddComponent<LayoutElement> ().ignoreLayout = true;

            // ShadowReciever
            var rec = obj.AddComponent < ShadowReciever > () ;
            rec.SoftShadowObject = shadow;
            rec.texture = texture;
            rec.raycastTarget = false;
            rec.color = Color.black;

            // hide flags
            //obj.hideFlags = HideFlags.DontSave; // for debug purposes only!
            var hideFlags = HideFlags.HideAndDontSave ; // HideAndDontSave DontSave
            rec.hideFlags = hideFlags;
            obj.hideFlags = hideFlags;

            //rec.myRectTransform.UpdatePosition ( shadow.shadowCenter );
            //rec.myRectTransform.UpdateSize ( shadow.shadowSize );

            return rec;
        }

        void ILayoutController.SetLayoutHorizontal ()
        {
            UpdatePosition ();
            //RegisterUpdatePosition ();
        }

        void ILayoutController.SetLayoutVertical ()
        {
            //UpdatePosition ();
            //RegisterUpdatePosition ();
        }
    }
}
