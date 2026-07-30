using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.RMath;
using Rudi.UI;
using Unity.Collections;
using UnityEngine;

namespace Rudi.Drawing
{
    public class Power_Icon : RawImageSupplier
	{
		private static readonly string TAG = "Rudis Power_Icon" ;

		[ Range ( 0 , 5 ) ]
		[ SerializeField ] private float m_LineWidth = 2f ;
		[ Range ( 0 , 90 ) ]
		[ SerializeField ] private float m_Gap = 60f ;
		[ Range ( 0 , 1 ) ]
		[ SerializeField ] private float m_LineStart = 0.2f ;
		[ Range ( 1 , 1.5f ) ]
		[ SerializeField ] private float m_LineEnd = 1.0f ;
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
                    float AngleStart = 90f + m_Gap * 0.5f ;
                    float AngleEnd   = 90f - m_Gap * 0.5f ;
					m_Arc = new Arc ( Vector2.zero , 0.5f * m_Scale , AngleStart , 360f - m_Gap ) ;
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
        private float m_Scale2 ;

        private LineList calcLineList ()
		{
			Vector2 [] points = new Vector2 [ 2 ] ;
            points [0] = Vector2.up * m_LineStart * 0.5f * m_Scale ;
			points [1] = Vector2.up * m_LineEnd   * 0.5f * m_Scale ;

            var Result =  LineList.FromPointList_AsList ( points );
            m_Scale2 = 1.0f / m_LineEnd ;
            Result.scale ( m_Scale2 ) ;
            return Result ;
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

					var np_Arc = arc.NearestPoint ( p , Vector2.zero , m_Scale2 ) ;

					var np_Lines = lineList.NearestPoint ( p ) ;

					float nearest = Mathf.Min ( np_Arc.Dist2 , np_Lines.Dist2 ) ;
					float dist = Mathf.Sqrt ( nearest ) / Scale - radius ;

					//float alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01((mindist / Scale - radius))) ;
					////alpha = 1f;
					//Pixels [iy * iwidth + ix] = new Color ( 1 , 1 , 1 , alpha );
                    Pixels [iy * iwidth + ix] = RMathUtils.SmoothAlphaWhiteFromDist ( dist );
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
