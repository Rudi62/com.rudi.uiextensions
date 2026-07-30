using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.RMath;
using Rudi.UI;
using Unity.Collections;
using UnityEngine;

namespace Rudi
{
    public class ThreeDots : RawImageSupplier
	{
		private static readonly string TAG = "Rudis ThreeDots" ;

		[ Range ( 0 , 1 ) ]
		[ SerializeField ] private float m_PointSize = 0.5f ;

        private static Texture2D [] m_Textures = new Texture2D [ NumCachedTextures ] ;
        protected override Texture2D [] textureArray => m_Textures;

        //private static float dist ( Vector2 center , Vector2 p ) => ( p - center ) . magnitude ;

		protected override void log ( string msg ) { if ( m_Log ) Log.i ( TAG , msg + ' ' + this ) ; }


		protected override bool calcTextureContent ( Texture2D tex )
		{
			log ( "calcTextureContent ()" );
			int iwidth  = tex.width ;
			int iheight = tex.height ;
			float halfWidth  = 0.5f * ( iwidth  - 1 ) ;
			float halfHeight = 0.5f * ( iheight - 1 ) ;
			float radius = m_PointSize * halfHeight / 3f ;
			Vector2 dot_0 = new Vector2 ( 0 , halfHeight * 2f / 3f ) ;
			Vector2 dot_1 = new Vector2 ( 0 , 0 ) ;
			Vector2 dot_2 = new Vector2 ( 0 , -halfHeight * 2f / 3f ) ;
			NativeArray < Color32 > Pixels = tex.GetPixelData < Color32 > ( 0 ) ;
            float SymmLine = halfHeight / 3f;

            for ( int iy = 0 ; iy < iheight ; iy++ )
			{
				float y = ( float ) iy - halfHeight ;
                if ( y > SymmLine ) y -= SymmLine * 2f ;
                else if ( y < -SymmLine ) y += SymmLine * 2f ;

                for ( int ix = 0 ; ix < iwidth ; ix++ )
				{
                    float x = ( float ) ix - halfWidth ;
					Vector2 p = new Vector2 ( x , y ) ;
					float Dist = Vector2.Distance ( dot_1 , p ) - radius ;
                    //float alpha = Mathf.SmoothStep ( 1 , 0 , Dist ) ;
                    //float alpha = RMath.SmoothStep ( 0.5f , -0.5f , Dist ) ;
                    //Pixels [ iy * iwidth + ix ] = new Color ( 1 , 1 , 1 , alpha ) ;
                    Pixels [ iy * iwidth + ix ] = RMathUtils.SmoothAlphaWhiteFromDist ( Dist ) ;
                }
            }
			tex.Apply () ;
			return true ;
		}
	}
}
