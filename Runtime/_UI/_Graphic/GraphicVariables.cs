using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Rudi.UI
{
    public class GraphicVariables
    {
        private static readonly string TAG = "Rudis GraphicVariables" ;

        private MonoBehaviour m_MonoBehaviour ;
        private Graphic m_Graphic = null ;
        private Canvas m_Canvas = null ;
        private RectTransform m_RectTransform = null ;
        public delegate void GOTPPU ();

        private GOTPPU m_gotPPU = null ;

        public GraphicVariables ( MonoBehaviour mb )
        {
            //Log.i ( TAG , "new GraphicVariables object" ) ;
            m_MonoBehaviour = mb;
        }

        private const int maxNumRepeats = 5 ;

        private CanvasCallbacks m_Callbacks = null ;
        private CanvasCallbacks callbacks => m_Callbacks ??= new CanvasCallbacks ( OnCallback , m_MonoBehaviour );
        private void OnCallback ()
        {
            if ( hasPPU )
            {
                m_Callbacks.Stop ();
                m_gotPPU?.Invoke ();
            }
        }

        public void waitForPPU ( GOTPPU callback )
        {
            if ( hasPPU ) callback?.Invoke ();
            else
            {
                //Log.i ( TAG , "waiting for ppu" ) ;
                m_gotPPU = callback;
                if ( m_MonoBehaviour.isActiveAndEnabled ) callbacks.StartRepeating ( maxNumRepeats );
            }
        }

        public void reset ()
        {
            m_Graphic = null;
            m_Canvas = null;
            m_RectTransform = null;
            m_PPU = null;
        }

        public Graphic graphic
        {
            get
            {
                if ( null == m_Graphic )
                {
                    m_Graphic = m_MonoBehaviour.GetComponent<Graphic> ();
                }
                return m_Graphic;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if ( null == m_RectTransform )
                {
                    if ( null != graphic ) m_RectTransform = graphic.rectTransform;
                    else m_RectTransform = m_MonoBehaviour.GetComponent<RectTransform> ();
                }
                return m_RectTransform;
            }
        }

        public static readonly Vector2 Vec2Half = Vector2.one * 0.5f ;

        static Vector2 PivotTo ( RectTransform rt , Vector2 rel_pos ) => rt != null ? ( rel_pos - rt.pivot ) * rt.rect.size : Vector2.zero;
        public static Vector2 PivotToCenter ( RectTransform rt ) => PivotTo ( rt , Vec2Half );
        public static Vector2 PivotToLL ( RectTransform rt ) => PivotTo ( rt , Vector2.zero );
        public static Vector2 PivotToUR ( RectTransform rt ) => PivotTo ( rt , Vector2.one );

        public Vector2 pivotToLL => PivotToLL ( rectTransform );
        public Vector2 ll_screen => ( Vector2 ) rectTransform.position + PivotToLL ( rectTransform ) * ppu;
        public Vector2 rectCenter_screen => ( Vector2 ) rectTransform.position + PivotToCenter ( rectTransform ) * ppu;
        //public Vector2 ll => ( Vector2 ) rectTransform.localPosition + PivotToLL ( rectTransform ) - PivotToCenter ( ( RectTransform ) rectTransform.parent ) ;
        public int rectWidthPixels => Mathf.RoundToInt ( rectTransform.rect.width * ppu );
        public int rectHeightPixels => Mathf.RoundToInt ( rectTransform.rect.height * ppu );
        public Vector2Int rectSizePixels => Vector2Int.RoundToInt ( rectTransform.rect.size * ppu ); // RoundToInt CeilToInt FloorToInt

        private static Vector3 [] m_Corners = new Vector3[4] ;

        private static (Vector3 min, Vector3 max) getMinMax ( Vector3 [ ] data )
        {
            var min = data[0] ;
            var max = data[0] ;
            var num = data.Length ;
            for ( var i = 1 ; i < num ; i++ )
            {
                min = Vector3.Min ( min , data [i] );
                max = Vector3.Max ( max , data [i] );
            }
            return (min, max);
        }
        public Vector2Int rectSizePixels2
        {
            get
            {
                rectTransform.GetWorldCorners ( m_Corners );
                var min = Vector2Int.RoundToInt ( m_Corners [ 0 ] ) ;
                var max = Vector2Int.RoundToInt ( m_Corners [ 2 ] ) ;
                return max - min;
            }
        }
        public Vector2 GetRectSize ( Vector2Int pixelSize ) => ( Vector2 ) pixelSize * upp;
        public Vector2 pixelAlignedRectSize => ( Vector2 ) rectSizePixels * upp; //  new Vector2 ( rectWidthPixels / ppu , rectHeightPixels / ppu ) ;
        public Vector2 PixelAlignedOffset_ll
        {
            get
            {
                var ll = ll_screen ;
                var snapped = new Vector2 ( Mathf.Round ( ll.x ) , Mathf.Round ( ll.y ) ) ;
                return ( snapped - ll ) * upp;
            }
        }

        private Canvas findCanvas ()
        {
            Canvas Result = null ;
            var list = ListPool < Canvas > . Get();
            m_MonoBehaviour.GetComponentsInParent ( false , list );
            if ( list.Count > 0 )
            {
                // Find the first active and enabled canvas.
                for ( var i = 0 ; i < list.Count ; ++i )
                {
                    if ( !list [i].isActiveAndEnabled ) continue;
                    if ( !list [i].isRootCanvas ) continue;
                    Result = list [i];
                    break;
                }
            }
            ListPool<Canvas>.Release ( list );
            return Result;
        }

        public Canvas canvas
        {
            get
            {
                if ( null == m_Canvas )
                {
                    if ( null != graphic ) m_Canvas = graphic.canvas;
                    else
                    {
                        m_Canvas = findCanvas ();
                    }
                }
                return m_Canvas;
            }
        }

        private float? m_PPU = null ;
        public float ppu
        {
            get
            {
#if UNITY_EDITOR
                return getActualPPU () ?? 1.0f;
#else
				if ( null == m_PPU )
				{
					m_PPU = getActualPPU ();
				}
				return m_PPU ?? 1.0f ;
#endif
            }
        }

        private float m_UPP = 1 ;
        public float upp
        {
            get
            {
#if UNITY_EDITOR
                return 1f / ppu;
#else

				if ( 1.0f == m_UPP )
				{
					m_UPP = 1f / ppu ;
					//Log.i ( TAG , "m_UPP = " + m_UPP ) ;
				}
				return m_UPP ;
#endif
            }
        }

        public bool hasPPU
        {
            get
            {
                if ( null == canvas ) return false;
                return canvas.scaleFactor != 1.0f;
            }
        }

        private float? getActualPPU ()
        {
            if ( null == canvas ) return null;
            if ( canvas.scaleFactor == 1.0f ) return null;
            return canvas.scaleFactor;
        }
        public void OnDisable ()
        {
            m_Callbacks?.Stop ();
        }
    }
}
