using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rudi.Drawing
{
	public class Arc : IDrawingElement
	{
		private Vector2 m_Center = Vector2.zero ;
		private float m_Radius = 0 ;
		private float m_AngleStart = 0 ;
		private float m_AngleArc = 0 ;
		private Vector2? m_StartPoint = null ;
		private Vector2? m_EndPoint = null ;
		public Arc () { }
		public Arc ( Vector2 center , float radius , float angleStart , float angleArc ) => setup ( center , radius , angleStart , angleArc ) ;
		public void setup ( Vector2 center , float radius , float angleStart , float angleArc )
		{
			m_Center     = center ;
			m_Radius     = radius ;
			m_AngleStart = angleStart ;
			m_AngleArc   = angleArc ;
			ValuesChanged () ;
		}
		private void ValuesChanged ()
		{
			m_StartPoint = null ;
			m_EndPoint = null ;
		}
		public Vector2 center
		{
			get => m_Center;
			set
			{
				m_Center = value ;
				ValuesChanged () ;
			}
		}
		public float radius
		{
			get => m_Radius ;
			set
			{
				m_Radius = value ;
				ValuesChanged ();
			}
		}
		public float angleStart
		{
			get => m_AngleStart ;
			set
			{
				m_AngleStart = value ;
				ValuesChanged () ;
			}
		}
		public float angleEnd => Mathf.Repeat ( m_AngleStart + m_AngleArc , 360f ) ;
		public float angleArc
		{
			get => m_AngleArc ;
			set
			{
				m_AngleArc = value;
				ValuesChanged ();
			}
		}

		public Vector2 startPoint
		{
			get
			{
				if ( null == m_StartPoint )
				{
					m_StartPoint = getCirclePoint ( angleStart ) ;
				}
				return m_StartPoint.Value ;
			}
		}

		public Vector2 endPoint
		{
			get
			{
				if ( null == m_EndPoint )
				{
					m_EndPoint = getCirclePoint ( angleEnd ) ;
				}
				return m_EndPoint.Value ;
			}
		}

		public static Vector2 getUnitCirclePointFramAngle ( float angle ) => new Vector2 ( Mathf.Cos ( angle * Mathf.Deg2Rad ) , Mathf.Sin ( angle * Mathf.Deg2Rad ) ) ;
		private Vector2 getCirclePoint ( float angle ) => center + radius * getUnitCirclePointFramAngle ( angle ) ;

		private static float getRelAngle ( Vector2 p ) => Mathf.Atan2 ( p.y , p.x ) * Mathf.Rad2Deg ;
		//https://github.com/Unity-Technologies/UnityCsReference/blob/e740821767d2290238ea7954457333f06e952bad/Runtime/Export/Math/Mathf.cs
		private static float DeltaAngle360 ( float current , float target ) => Mathf.Repeat ( target - current , 360f ) ;

		public float getAngle ( Vector2 p ) => getRelAngle ( p - center ) ;

		public bool isAngleWithin ( Vector2 p ) => isAngleWithin ( getAngle ( p ) ) ;

		public bool isAngleWithin ( float a )
		{
			return DeltaAngle360 ( m_AngleStart , a ) <= angleArc ;
		}

		public override ( Vector2 NearestPoint , float Dist2 ) NearestPoint ( Vector2 p )
		{
			if ( p.Equals ( center ) ) return ( startPoint , radius * radius ) ;
			float angle = getAngle ( p ) ;
			if ( isAngleWithin ( angle ) )
			{
				Vector2 np = getCirclePoint ( angle ) ;
				float dist2 = ( p - np ).sqrMagnitude ;
				return ( np , dist2 ) ;
			}
			else
			{
				Vector2 sp = startPoint ;
				float dist2_sp = ( p - sp ).sqrMagnitude ;
				Vector2 ep = endPoint ;
				float dist2_ep = ( p - ep ).sqrMagnitude ;
				if ( dist2_sp < dist2_ep ) return ( sp , dist2_sp ) ;
				return (ep, dist2_ep ) ;
			}
		}
		public override ( Vector2 NearestPoint , float Dist2 ) NearestPoint ( Vector2 p , Vector2 shift , float scale )
		{
            // transform geometry
            Vector2 center     = ( this.center     + shift ) * scale ;
            Vector2 startPoint = ( this.startPoint + shift ) * scale ;
            Vector2 endPoint   = ( this.endPoint   + shift ) * scale ;
            float radius = this.radius * scale ;
            // calculate distance
            if ( p.Equals ( center ) ) return ( startPoint , radius * radius ) ;
			float angle = getRelAngle ( p - center) ;
			if ( isAngleWithin ( angle ) )
			{
				Vector2 np = center + radius * getUnitCirclePointFramAngle ( angle ) ;
				float dist2 = ( p - np ).sqrMagnitude ;
				return ( np , dist2 ) ;
			}
			else
			{
				Vector2 sp = startPoint ;
				float dist2_sp = ( p - sp ).sqrMagnitude ;
				Vector2 ep = endPoint ;
				float dist2_ep = ( p - ep ).sqrMagnitude ;
				if ( dist2_sp < dist2_ep ) return ( sp , dist2_sp ) ;
				return (ep, dist2_ep ) ;
			}
		}

		public override void translate ( Vector2 shift ) => center += shift ;

		public override void scale ( float scale )
		{
			center *= scale ;
			radius *= scale ;
		}

		public override AABB aabb
		{
			get
			{
				AABB Result = new AABB ( startPoint ) ;
				Result.add ( endPoint );
				if ( isAngleWithin ( 0 ) ) Result.add ( radius * Vector2.right );
				if ( isAngleWithin ( 90 ) ) Result.add ( radius * Vector2.up );
				if ( isAngleWithin ( 180 ) ) Result.add ( radius * Vector2.left );
				if ( isAngleWithin ( 270 ) ) Result.add ( radius * Vector2.down );
				return Result;
			}
		}

		public Rect rect => aabb ;
		public Vector2 midPoint => aabb.midPoint ;
	}
}
