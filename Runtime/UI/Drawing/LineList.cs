using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rudi.RMath;

namespace Rudi.Drawing
{
    public class LineList : IDrawingElement
	{

		private Line2D [] m_Lines ;

		public LineList ( List < Line2D > lines ) => m_Lines = lines.ToArray () ;
		public LineList ( Line2D [] lines ) => m_Lines = lines ;

		public Rect rect => aabb ;

		public Vector2 midPoint => aabb.midPoint ;

		public static LineList FromPointList_AsStrip ( List < Vector2 > points )
		{
			List < Line2D > lines = new () ;
			if ( points.Count > 1 )
			{
				var it = points.GetEnumerator () ;
				Vector2 OldPoint = next ( it ) ;
				while ( it.MoveNext () )
				{
					Vector2 NextPoint = it.Current ;
					lines.Add ( new Line2D ( OldPoint , NextPoint ) ) ;
					OldPoint = NextPoint ;
				}
			}
			return new LineList ( lines ) ;
		}
		public static LineList FromPointList_AsStrip ( Vector2 [] points )
		{
			List < Line2D > lines = new () ;
			if ( points.Length > 1 )
			{
				IEnumerator it = points.GetEnumerator () ;
				it.MoveNext () ;
				Vector2 OldPoint = ( Vector2 ) it.Current ;
				while ( it.MoveNext () )
				{
					Vector2 NextPoint = ( Vector2 ) it.Current ;
					lines.Add ( new Line2D ( OldPoint , NextPoint ) ) ;
					OldPoint = NextPoint ;
				}
			}
			return new LineList ( lines ) ;
		}
		private static Vector2 next ( List < Vector2 > . Enumerator it ) => it.MoveNext () ? it.Current : Vector2.zero ;
		public static LineList FromPointList_AsList ( List < Vector2 > points )
		{
			List < Line2D > lines = new () ;
			int NumLines = points.Count / 2 ;
			var it = points.GetEnumerator () ;
			for ( int i = 0 ; i < NumLines ; i++ )
			{
				lines.Add ( new Line2D ( next ( it ) , next ( it ) ) ) ;
			}
			return new LineList ( lines ) ;
		}

		public static LineList FromPointList_AsList ( Vector2 [] points )
		{
			List < Line2D > lines = new () ;
			int NumLines = points.Length / 2 ;
			int index = 0 ;
			for ( int i = 0 ; i < NumLines ; i++ )
			{
				lines.Add ( new Line2D ( points [ index++ ] , points [ index++ ] ) ) ;
			}
			return new LineList ( lines ) ;
		}

		public override ( Vector2 NearestPoint , float Dist2 ) NearestPoint ( Vector2 p )
		{
			Vector2 ResultPoint = Vector2.zero ;
			float ResultDist2 = float.MaxValue ;
			for ( int i = 0 ; i < m_Lines.Length ; i++ )
			{
				var np = m_Lines[i].nearestPoint ( p ) ;
				if ( np.dist2 < ResultDist2 )
				{
					ResultPoint = np.nearest ;
					ResultDist2 = np.dist2 ;
				}
			}
			return ( ResultPoint , ResultDist2 ) ;
		}

		public override void scale ( float scale )
		{
			for ( int i = 0 ; i < m_Lines.Length ; i++ )
			{
				Vector2 p1 = m_Lines[i].p1 ;
				Vector2 p2 = m_Lines[i].p2 ;
				m_Lines [i] = new Line2D ( p1 * scale , p2 * scale ) ;
			}
		}

		public override void translate ( Vector2 shift )
		{
			for ( int i = 0 ; i < m_Lines.Length ; i++ )
			{
				Vector2 p1 = m_Lines[i].p1 ;
				Vector2 p2 = m_Lines[i].p2 ;
				m_Lines [i] = new Line2D ( p1 + shift , p2 + shift ) ;
			}
		}

		public override AABB aabb
		{
			get
			{
				AABB Result = new () ;
				foreach ( Line2D line in m_Lines )
				{
					Result.add ( line.p1 );
					Result.add ( line.p2 );
				}
				return Result;
			}
		}
	}
}