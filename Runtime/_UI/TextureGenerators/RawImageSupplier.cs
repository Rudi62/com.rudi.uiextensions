using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    [RequireComponent ( typeof ( RawImage ) )]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]

    public abstract class RawImageSupplier : MonoBehaviour
    {
        private static readonly string TAG = "Rudis TextureGenerator" ;
        protected const int NumCachedTextures = 5 ;
        [ Range ( -1 , NumCachedTextures ) ]
        [ SerializeField ] private int m_Slot = -1 ;
        [ SerializeField ] private bool m_PixelFriendly = true ;
        [ ReadOnly ]
        [ SerializeField ] private Texture2D m_Texture = null ;
        [ ReadOnly ]
        [ SerializeField ] private string m_TextureSize = string.Empty ;
        [ SerializeField ] protected bool m_Log = false ;
        [ Space ]
        [ SerializeField ] private int m_UsedSlot = -1 ;

        protected virtual void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ); }

        private int m_TextureWidth ;
        private int m_TextureHeight ;
        private bool m_bOnDestroy = false ;

        private CanvasCallbacks m_Callbacks = null ;
        private CanvasCallbacks callbacks => m_Callbacks ??= new CanvasCallbacks ( CallbackFunction , this , CanvasCallbacks.Timing.Late );
        private void registerCallback () => callbacks.StartAsap (); // StartAsap StartSingle
        private void unregisterCallback () => m_Callbacks?.Stop ();

        private void CallbackFunction ()
        {
            log ( "CallbackFunction () 1" );
            if ( m_bOnDestroy ) return;
            //if ( !enabled ) return ;
            log ( "CallbackFunction () 2" );
            Redraw ();
        }

        public void setTextureDirty ()
        {
            registerCallback ();
        }

        protected abstract Texture2D [ ] textureArray { get; }

        protected int suggestedTextureWidth => m_TextureWidth = gv.rectWidthPixels;
        protected int suggestedTextureHeight => m_TextureHeight = gv.rectHeightPixels;

        bool hasPPU => gv.hasPPU;

        private GraphicVariables m_gv ;
        protected GraphicVariables gv => m_gv ??= new GraphicVariables ( this );

        private RawImage m_RawImage = null ;
        private RawImage rawImage => this.getComponentIfZero ( ref m_RawImage );

        private static string materialName => "Rudi/UI/PixelAlign";
        private static Material m_MaterialPixelAlign = null ;
        private static Material matPixelAlign
        {
            get
            {
                if ( null == m_MaterialPixelAlign )
                {
                    m_MaterialPixelAlign = Utils.createMaterial ( materialName );
                    //if ( null != m_MaterialPixelAlign && !CanvasUpdateRegistry.IsRebuildingGraphics () ) SetMaterialDirty ();
                    if ( null == m_MaterialPixelAlign ) return null;
#if UNITY_EDITOR
                    m_MaterialPixelAlign.EnableKeyword ( "UNITY_EDITOR" , true );
                    m_MaterialPixelAlign.SetVector ( "_MyScreenSize" , new Vector4 ( Screen.width , Screen.height , 0 , 0 ) );
#endif
                }
                return m_MaterialPixelAlign;
            }
        }

        private int getUsedSlot ()
        {
#if UNITY_EDITOR
            if ( PrefabUtility.IsPartOfPrefabAsset ( this ) ) return -1;
            if ( !Application.isPlaying ) return -1;
#endif
            if ( null == rawImage ) return -1;
            if ( null == rawImage.canvas ) return -1;
            if ( rawImage.canvas.scaleFactor == 1f ) return -1;
            return m_Slot;
        }

        public int slot //=> m_UsedSlot = getUsedSlot () ;
        {
            get => m_UsedSlot = getUsedSlot ();
            set
            {
                if ( value == m_Slot ) return;
                m_UsedSlot = value;
                m_Slot = value;
                setTextureDirty ();
            }
        }

        private bool isCurrentTextureValid ( Texture2D tex )
        {
            if ( null == tex ) return false;
            if ( slot >= 0 ) return true;
            if ( tex.width != suggestedTextureWidth ) return false;
            if ( tex.height != suggestedTextureHeight ) return false;
            return true;
        }

        private Texture2D currTex
        {
            get
            {
                if ( slot < 0 ) return m_Texture;
                else return textureArray [slot];
            }
            set
            {
                if ( slot < 0 ) m_Texture = value;
                else textureArray [slot] = value;
            }
        }

        private Texture2D createTexture ()
        {
            if ( suggestedTextureWidth < 1 || suggestedTextureHeight < 1 ) return null;
            var tex = new Texture2D ( suggestedTextureWidth , suggestedTextureHeight , TextureFormat.RGBA32 , false );
            tex.filterMode = FilterMode.Bilinear; // Point Bilinear Trilinear
            tex.wrapMode = TextureWrapMode.Mirror; // Clamp Mirror
            tex.hideFlags = HideFlags.HideAndDontSave;
            m_TextureSize = $"{suggestedTextureWidth} x {suggestedTextureHeight}";
            return tex;
        }

        private static void DestroyTexture ( ref Texture2D tex )
        {
            if ( null != tex ) Utils.DestroyObject ( tex );
            tex = null;
        }

        private void DestroyCurrentTexture ()
        {
            if ( null != currTex ) Utils.DestroyObject ( currTex );
            currTex = null;
            rawImage.texture = null;
            m_TextureSize = "-";
        }

        private void OnRectTransformDimensionsChange ()
        {
            if ( !enabled ) return;
            if ( !isCurrentTextureValid ( currTex ) ) initiateRedraw ();
        }

        private void Redraw ()
        {
            if ( slot < 0 )
            {
                if ( !isCurrentTextureValid ( currTex ) )
                {
                    DestroyCurrentTexture ();
                    currTex = createTexture ();
                }
                if ( isCurrentTextureValid ( currTex ) ) calcTextureContent ( currTex );
            }
            else
            {
                if ( null == currTex )
                {
                    currTex = createTexture ();
                    if ( isCurrentTextureValid ( currTex ) )
                    {
                        //if ( slot == 0 ) Log.i ( TAG , "calculating slot 0 - " + currTex.GetSize() ) ;
                        calcTextureContent ( currTex );
                    }
                }
            }

            rawImage.texture = currTex;
            rawImage.material = m_PixelFriendly ? matPixelAlign : null;
            rawImage.SetAllDirty ();
        }

        private void applyTexture ()
        {
            rawImage.texture = currTex;
            rawImage.SetAllDirty ();
        }

        private void initiateRedraw_ppu_set ()
        {
            if ( CanvasUpdateRegistry.IsRebuildingGraphics () ) setTextureDirty ();
            else Redraw ();
        }

        private void initiateRedraw ()
        {
            gv.waitForPPU ( initiateRedraw_ppu_set );
        }

        private void OnEnable ()
        {
            if ( m_Started ) initiateRedraw ();
        }

        private void OnDisable ()
        {
            m_gv?.OnDisable ();
            unregisterCallback ();
            m_RawImage = null;
            if ( slot < 0 ) DestroyCurrentTexture ();
        }
        private bool m_Started = false ;
        private void Start ()
        {
            m_Started = true;
            initiateRedraw ();
        }

        private void OnDestroy ()
        {
            m_bOnDestroy = true;
            DestroyTexture ( ref m_Texture );
        }

        protected abstract bool calcTextureContent ( Texture2D tex );

#if UNITY_EDITOR
        protected void OnValidate ()
        {
            if ( Application.isPlaying ) return;
            Redraw ();
        }

        [ContextMenu ( "Switch off all Logs" )]
        private void SwitchOffAllLogs ()
        {
            var Objects = FindObjectsOfType < RawImageSupplier > ( true ) ;
            foreach ( var ss in Objects )
            {
                ss.m_Log = false;
            }
            EditorUtility.SetDirty ( this );
        }
#endif
    }
}
