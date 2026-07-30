using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// provides mainly the two callbacks:
//   public abstract void LayoutComplete () ;
//   public abstract void GraphicUpdateComplete () ;
//
// useful if you want to react to graphic change.


namespace Rudi.UI
{
    [ExecuteAlways]
    [RequireComponent ( typeof ( Graphic ) )]
    public abstract class CanvasElement : MonoBehaviour, ICanvasElement
    {
        private static readonly string TAG = "Rudis CanvasElement";
        [ SerializeField ] private bool m_Log = false ;
        protected bool callbackOnVerticesDirty { get; set; }
        private void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ); }
        private void log1 ( string msg ) { Log.i ( TAG , msg + ' ' + this ); }
        public void log ( string tag , string msg ) { if ( m_Log ) Log.i ( tag , msg + ' ' + this ); }
        public void log1 ( string tag , string msg ) { Log.i ( tag , msg + ' ' + this ); }

#if UNITY_EDITOR
        public void enableLog ( bool b )
        {
            m_Log = b;
            EditorUtility.SetDirty ( this );
        }

        [ContextMenu ( "Switch on all Logs" )]
        private void SwitchOnAllLogs ()
        {
            var Objects = FindObjectsOfType < CanvasElement > ( true ) ;
            foreach ( var ss in Objects )
            {
                ss.m_Log = true;
            }
            EditorUtility.SetDirty ( this );
        }


        [ContextMenu ( "Switch off all Logs" )]
        private void SwitchOffAllLogs ()
        {
            var Objects = FindObjectsOfType < CanvasElement > ( true ) ;
            foreach ( var ss in Objects )
            {
                ss.m_Log = false;
            }
            EditorUtility.SetDirty ( this );
        }
#endif


        private Graphic m_Graphic = null ;
        protected Graphic graphic
        {
            get
            {
                if ( null == m_Graphic )
                {
                    m_Graphic = GetComponent<Graphic> ();
                }
                return m_Graphic;
            }
        }

        protected Canvas canvas => graphic != null ? graphic.canvas : null;
        protected float ppu => canvas.getPPU ();
        protected bool hasPPU => canvas.hasPPU ();

        protected MaskableGraphic maskableGraphic => graphic as MaskableGraphic;

        private void InternalOnVerticesDirty ()
        {
            log ( "InternalOnVerticesDirty ()" );
            RegisterCanvasGraphicUpdate ();
            if ( callbackOnVerticesDirty ) OnVerticesDirty ();
        }

        private void InternalOnLayoutDirty ()
        {
            log ( "InternalOnLayoutDirty ()" );
            RegisterCanvasLayoutUpdate ();
        }

        //private static bool ObjectValidForUpdate ( ICanvasElement element )
        //{
        //    // copied from CanvasUpdateRegistry.cs
        //    var valid = element != null;

        //    var isUnityObject = element is Object;
        //    if ( isUnityObject )
        //        valid = ( element as Object ) != null; //Here we make use of the overloaded UnityEngine.Object == null, that checks if the native object is alive.

        //    return valid;
        //}
        private bool m_CanvasElementForGraphicRebuild_Registered = false ;
        private bool m_CanvasElementForLayoutRebuild_Registered  = false ;
        private void DisableCanvasUpdate ()
        {
            m_CanvasElementForGraphicRebuild_Registered = false;
            m_CanvasElementForLayoutRebuild_Registered = false;
            CanvasUpdateRegistry.DisableCanvasElementForRebuild ( this );
        }

        private void RegisterCanvasGraphicUpdate ()
        {
            //if ( !isActiveAndEnabled)
            //         {
            //             log ( "RegisterCanvasGraphicUpdate () - isActiveAndEnabled is false" ) ;
            //             return ;
            //         }

            log ( "RegisterCanvasGraphicUpdate ()" );

            if ( !m_CanvasElementForGraphicRebuild_Registered )
            {
                m_CanvasElementForGraphicRebuild_Registered = CanvasUpdateRegistry.TryRegisterCanvasElementForGraphicRebuild ( this );
            }
        }

        private void RegisterCanvasLayoutUpdate ()
        {

            if ( !m_CanvasElementForLayoutRebuild_Registered )
            {
                m_CanvasElementForLayoutRebuild_Registered = CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild ( this );
            }
        }

        private void UnregisterCanvasUpdate ()
        {
            m_CanvasElementForGraphicRebuild_Registered = false;
            m_CanvasElementForLayoutRebuild_Registered = false;
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild ( this );
        }

        public void rebuildGraphic ( bool materialToo = false )
        {
            graphic.SetVerticesDirty ();
            if ( materialToo ) graphic.SetMaterialDirty ();
        }

        public void rebuildLayout () => graphic.SetLayoutDirty ();
        public void rebuildAll ()
        {
            graphic.SetVerticesDirty ();
            graphic.SetMaterialDirty ();
            graphic.SetLayoutDirty ();
        }
        // implementing ICanvasElement
        public void LayoutComplete ()
        {
            m_CanvasElementForLayoutRebuild_Registered = false;
            OnLayoutComplete ();
        }

        public void GraphicUpdateComplete ()
        {
            m_CanvasElementForGraphicRebuild_Registered = false;
            OnGraphicUpdateComplete ();
        }

        public void Rebuild ( CanvasUpdate executing )
        {
            log ( $"Rebuild ( { executing } )" );
            OnRebuild ( executing );
        }
        public bool IsDestroyed () => this == null;

        // abstract methods
        protected abstract void OnLayoutComplete ();
        protected abstract void OnGraphicUpdateComplete ();
        protected abstract void OnRebuild ( CanvasUpdate executing );
        protected virtual void OnVerticesDirty () { }
        //protected virtual
        private void OnCullstateChanged ( bool isCulled )
        {
            if ( !isCulled )
            {
                RegisterCanvasGraphicUpdate ();
                RegisterCanvasLayoutUpdate ();
            }
        }

        private bool m_CullStateChangeRegistered ;
        protected void registerCullstateListener ( bool register )
        {
            if ( null == maskableGraphic ) return;
            if ( register )
            {
                if ( !m_CullStateChangeRegistered )
                {
                    m_CullStateChangeRegistered = true;
                    maskableGraphic.onCullStateChanged.AddListener ( OnCullstateChanged );
                }
            }
            else
            {
                if ( m_CullStateChangeRegistered )
                {
                    m_CullStateChangeRegistered = false;
                    maskableGraphic.onCullStateChanged.RemoveListener ( OnCullstateChanged );
                }
            }
        }

        private bool m_OnVerticesDirty_Registered = false ;
        protected void RegisterOnVerticesDirty ( bool register )
        {
            if ( register )
            {
                if ( !m_OnVerticesDirty_Registered )
                {
                    graphic.RegisterDirtyVerticesCallback ( InternalOnVerticesDirty );
                    graphic.RegisterDirtyLayoutCallback ( InternalOnLayoutDirty );
                    m_OnVerticesDirty_Registered = true;
                }
            }
            else
            {
                if ( m_OnVerticesDirty_Registered )
                {
                    graphic.UnregisterDirtyVerticesCallback ( InternalOnVerticesDirty );
                    graphic.UnregisterDirtyLayoutCallback ( InternalOnLayoutDirty );
                    m_OnVerticesDirty_Registered = false;
                }
            }
        }

        protected virtual void Start ()
        {
            log ( "Start ()" );
            //registerCullstateListener ( true );
            //RegisterOnVerticesDirty ( true );
        }

        protected virtual void OnEnable ()
        {
            log ( "OnEnable ()" );
            registerCullstateListener ( true );
            RegisterOnVerticesDirty ( true );
            RegisterCanvasGraphicUpdate ();
            RegisterCanvasLayoutUpdate ();

        }

        protected virtual void OnDisable ()
        {
            RegisterOnVerticesDirty ( false );
            registerCullstateListener ( false );
            DisableCanvasUpdate ();
        }

        protected virtual void OnDestroy ()
        {
            UnregisterCanvasUpdate ();
        }
#if UNITY_EDITOR
        protected virtual void OnValidate ()
        {
            if ( Application.isPlaying ) return;
            registerCullstateListener ( true );
            RegisterOnVerticesDirty ( true );
        }
#endif
    }
}
