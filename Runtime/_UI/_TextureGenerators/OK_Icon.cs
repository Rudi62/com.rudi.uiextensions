using Rudi.Core;
using Rudi.Drawing;
using Rudi.RMath;
using Rudi.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Rudi
{
    public class OK_Icon : RawImageSupplier
	{
		private static readonly string TAG = "Rudis OK_Icon" ;

		[ Range ( 0 , 10 ) ]
		[ SerializeField ] private float m_LineWidth = 2f ;
		[ Range ( 0 , 1 ) ]
		[ SerializeField ] private float m_Scale = 1f ;
		protected override void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ) ; }

        public float lineWidth
        {
            get => m_LineWidth;
            set => setVal ( ref m_LineWidth , value );
        }

        public float scale
        {
            get => m_Scale;
            set => setVal ( ref m_Scale , value );
        }

        private void setVal < T > ( ref T var , T val )
        {
            if ( !var.Equals ( val ) )
            {
                var = val ;
                setDirty () ;
            }
        }
        private void setDirty ()
        {
            m_LineList = null ;
            setTextureDirty () ;
        }

        private static Texture2D [] m_Textures = new Texture2D [ NumCachedTextures ] ;
        protected override Texture2D [] textureArray => m_Textures ;

        private LineList m_LineList = null ;
        private LineList lineList => Utils.createIfZero ( ref m_LineList , calcLineList ) ;

		private LineList calcLineList ()
		{
            //       + ( 3 , 2 ) - p2
            // +       ( 0 , 1 ) - p0
            //   +     ( 1 , 0 ) - p1
			Vector2 [] points = new Vector2 [ 3 ] ;
			points [0] = Vector2.up    ; // ( 0 , 1 )
			points [1] = Vector2.right ; // ( 1 , 0 )
			points [2] = new Vector2        ( 3 , 2 ) ;

			var Result = LineList.FromPointList_AsStrip ( points );
			Result.shiftToMidPoint () ;
			//Result.scaleToMatch ( 1f );
			Result.scaleToMatch ( m_Scale ) ;
			//Result.translate ( new Vector2 ( -1.5f , -1f ) ) ;
			//Result.scale ( m_Scale / 3f );
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

					var np_Lines = lineList.NearestPoint ( p ) ;

					float dist = Mathf.Sqrt ( np_Lines.Dist2 ) / Scale - radius ;

                    //float alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.Clamp01((mindist / Scale - radius))) ;
                    ////alpha = 1f;
                    //Pixels [iy * iwidth + ix] = new Color ( 1 , 1 , 1 , alpha );
                    Pixels [ iy * iwidth + ix ] = RMathUtils.SmoothAlphaWhiteFromDist ( dist ) ;
                }
            }
			tex.Apply () ;
			return true ;
		}

#if UNITY_EDITOR
		protected new void OnValidate ()
		{
			base.OnValidate () ;
			if ( Application.isPlaying ) return ;
			m_LineList = null ; // enforce recalculation
		}

#endif
	}
}
