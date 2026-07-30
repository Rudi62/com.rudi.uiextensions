using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rudi.Drawing
{
	public abstract class IDrawingElement
	{
		public abstract ( Vector2 NearestPoint , float Dist2 ) NearestPoint ( Vector2 p ) ;
		public virtual ( Vector2 NearestPoint , float Dist2 ) NearestPoint ( Vector2 p , Vector2 shift , float scale )
        {
            return NearestPoint ( p );
        }
		public abstract void translate ( Vector2 shift ) ;
		public abstract void scale ( float scale ) ;
		public Vector2 shiftToMidPoint ()
        {
            Vector2 toShift = - aabb.midPoint ;
            translate ( - aabb.midPoint ) ;
            return toShift ;
        }
		public  void scaleToMatch ( Vector2 size )
		{
			AABB aabb = this.aabb ;
			Vector2 MySize = aabb.size ;
			Vector2 Scale2 = size / MySize ;
			float scale = Mathf.Min ( Scale2.x , Scale2.y ) ;
			this.scale ( scale ) ;
		}
		public float scaleToMatch ( float size )
		{
			AABB aabb = this.aabb ;
			Vector2 MySize = aabb.size ;
			float scale = Mathf.Min ( size / MySize.x , size / MySize.y ) ;
			this.scale ( scale ) ;
            return scale ;
		}
		public abstract AABB aabb { get; }
	}
}
