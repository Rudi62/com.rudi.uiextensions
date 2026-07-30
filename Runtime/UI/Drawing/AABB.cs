using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rudi.Drawing
{
	public class AABB
	{
		Vector2 m_Min ;
		Vector2 m_Max ;
		public bool valid => m_Max.x >= m_Min.x && m_Max.y >= m_Min.y ;
		public AABB () => makeInvalid () ;
		public AABB ( Vector2 p ) => set ( p ) ;
		public AABB ( Vector2 [] points ) => set ( points ) ;
		public void makeInvalid ()
		{
			m_Min = Vector2.positiveInfinity ;
			m_Max = Vector2.negativeInfinity ;
		}
		public void set ( Vector2 p )
		{
			m_Min = p ;
			m_Max = p ;
		}
		public void add ( Vector2 p )
		{
			m_Min = Vector2.Min ( m_Min , p ) ;
			m_Max = Vector2.Max ( m_Max , p ) ;
		}
		public void add ( Vector2 [] points )
		{
			foreach ( Vector2 p in points ) add ( p ) ;
		}
		public void set ( Vector2 [ ] points )
		{
			int NumPoints = points.Length ;
			if ( NumPoints < 1 )
			{
				makeInvalid () ;
				m_Min = Vector2.positiveInfinity ;
				m_Max = Vector2.negativeInfinity ;
			}
			else
			{
				set ( points [ 0 ] ) ;
				for ( int i = 1 ; i < NumPoints ; i++ )
				{
					add ( points [i] ) ;
				}
			}
		}

		public Vector2 midPoint => 0.5f * ( m_Min + m_Max ) ;
		public Vector2 size => m_Max - m_Min ;

		public static implicit operator Rect ( AABB aabb ) => Rect.MinMaxRect ( aabb.m_Min.x , aabb.m_Min.y , aabb.m_Max.x , aabb.m_Max.y ) ;
	}
}