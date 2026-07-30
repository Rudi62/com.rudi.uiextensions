using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.RMath;
using Rudi.UI;
using Unity.Collections;
using UnityEngine;

namespace Rudi.Drawing
{
    public class Reset_Icon : RawImageSupplier
	{
		private static readonly string TAG = "Rudis DefaultIcon" ;

		[ Range ( 0 , 5 ) ]
		[ SerializeField ] private float m_LineWidth = 2f;
		[ Range ( 0 , 1 ) ]
		[ SerializeField ] private float m_Scale = 1f ;

		protected override void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ); }

        private static Texture2D [] m_Textures = new Texture2D [ NumCachedTextures ] ;
        protected override Texture2D [] textureArray => m_Textures ;


        private Arc m_Arc = null ;
		private Arc arc
		{
			get
			{
				if ( null == m_Arc )
				{
					m_Arc = new Arc ( Vector2.zero , 0.5f * m_Scale , 180f , 330f ); // 315
				}
				return m_Arc;
			}
		}

		private LineList m_LineList = null ;
		private LineList lineList
		{
			get
			{
				if ( null == m_LineList )
				{
					m_LineList = calcLineList ();
				}
				return m_LineList;
			}
		}

		private LineList calcLineList ()
		{
			Vector2 [] points = new Vector2 [ 4 ] ;

			Vector2 ArcEndPoint = m_Arc.endPoint ;

			//float ArrowLength = m_Scale * 0.5f * ( 1.0f - 0.5f * Mathf.Sqrt ( 2f ) ) ;

			float ArrowLength = ( m_Scale * 0.5f - ArcEndPoint.y ) * 1f ;

			points [0] = ArcEndPoint;
			points [1] = ArcEndPoint + ArrowLength * Vector2.right ;
			points [2] = ArcEndPoint;
			points [3] = ArcEndPoint + ArrowLength * Vector2.up ;


			return LineList.FromPointList_AsList ( points );
		}


		protected override bool calcTextureContent ( Texture2D tex )
		{
			log ( "calcTextureContent ()" );
			int iwidth  = tex.width ;
			int iheight = tex.height ;
			float halfWidth  = 0.5f * ( iwidth  - 1 ) ;
			float halfHeight = 0.5f * ( iheight - 1 ) ;
			float MinWidth = Mathf.Min ( halfWidth , halfHeight ) ;
			float lineWidth = m_LineWidth * gv.ppu ;
			float radius = 0.5f * lineWidth ;
			float UsableWidth = MinWidth - radius ;
			float ScaleR = MinWidth / UsableWidth ;
			float Scale = 0.5f / UsableWidth ;
			NativeArray < Color32 > Pixels = tex.GetPixelData < Color32 > ( 0 ) ;

			for ( int iy = 0 ; iy < iheight ; iy++ )
			{
				float y = Scale * ( ( float ) iy - halfHeight ) ;
				for ( int ix = 0 ; ix < iwidth ; ix++ )
				{
					float x = Scale * ( ( float ) ix - halfWidth ) ;
					Vector2 p = new Vector2 ( x , y ) ;

					var np_Arc = arc.NearestPoint ( p ) ;

					var np_Lines = lineList.NearestPoint ( p ) ;

					float nearest = Mathf.Min ( np_Arc.Dist2 , np_Lines.Dist2 ) ;
					float dist = Mathf.Sqrt ( nearest ) / Scale - radius ;

                    //float alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01((mindist / Scale - radius))) ;
                    ////alpha = 1f;
                    //Pixels [iy * iwidth + ix] = new Color ( 1 , 1 , 1 , alpha );
                    Pixels [ iy * iwidth + ix ] = RMathUtils.SmoothAlphaWhiteFromDist ( dist ) ;
                }
            }
			tex.Apply ();
			return true;
		}

#if UNITY_EDITOR
		protected new void OnValidate ()
		{
			base.OnValidate ();
			if ( Application.isPlaying ) return;
			m_LineList = null; // enforce recalculation
			m_Arc = null;
		}

#endif
	}
}
