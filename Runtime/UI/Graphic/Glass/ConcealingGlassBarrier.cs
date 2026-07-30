using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Rudi.Core;

namespace Rudi.UI
{
    [AddComponentMenu ( "Rudi/UI/Glass/Concealing Glass Barrier" )]

    public class ConcealingGlassBarrier : RawImage
    {
        public override Texture mainTexture => s_WhiteTexture;

        private Material m_MaterialClipAll = null ;
        private Material matClipAll
        {
            get
            {
                if ( null == m_MaterialClipAll )
                {
                    m_MaterialClipAll = Utils.createMaterial ( "Rudi/UI/ClipAll" );
                    if ( null != m_MaterialClipAll && !CanvasUpdateRegistry.IsRebuildingGraphics () ) SetMaterialDirty ();
                }
                return m_MaterialClipAll;
            }
        }

        public override Material material => matClipAll;
    }

#if UNITY_EDITOR
    [CustomEditor ( typeof ( ConcealingGlassBarrier ) )]
    [CanEditMultipleObjects]
    public class ClearGlassBarrierEditor : UnityEditor.UI.GraphicEditor
    {
        protected override void OnEnable ()
        {
            base.OnEnable ();
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif
}
