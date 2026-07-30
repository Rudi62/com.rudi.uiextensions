using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rudi.Drawing
{
	public class Path
	{
		enum ElementType
		{
			MoveTo,
			LineTo,
			CubicTo,
		}
		struct Element
		{
			public readonly ElementType type ;
			public readonly Vector2 p ;
			public Element ( ElementType type , Vector2 p ) { this.type = type; this.p = p; }
		}
		private readonly Element [] m_Elements ;
		private readonly bool m_bClosed ;
		private readonly Vector2 m_Size ;
		public bool closed => m_bClosed;
		Path ( Element [ ] elements , bool closed , Vector2 size )
		{
			m_Elements = elements;
			m_bClosed = closed;
			m_Size = size;
		}
		public class Builder
		{
			private List < Element > m_Elements ;
			private bool m_bClosed = false ;
			private Vector2 m_Size = Vector2.zero ;
			private Vector2 m_Current = Vector2.zero ;
			private bool m_bStarted = false ;
			public Builder ( Vector2 startpoint , bool closed )
			{
				m_Elements = new ();
				m_Elements.Add ( new Element ( ElementType.MoveTo , startpoint ) );
			}
			public void startPath ( Vector2 startpoint )
			{
				m_Current = Vector2.zero;
				ElementType type = ElementType.MoveTo ;
				addElement ( type , startpoint );
				m_bStarted = true;
			}
			private void addElement ( ElementType type , Vector2 p )
			{
				m_Elements.Add ( new Element ( ElementType.CubicTo , p ) );
				m_Current += p;
				m_Size = Vector2.Max ( m_Size , m_Current );
			}
			public void addLine ( Vector2 p ) => addElement ( ElementType.LineTo , p );
			public void addCubic ( Vector2 p1 , Vector2 p2 , Vector2 p3 )
			{
				addElement ( ElementType.CubicTo , p1 );
				addElement ( ElementType.CubicTo , p2 );
				addElement ( ElementType.CubicTo , p3 );
			}
			public Path toPath ()
			{
				if ( m_bClosed && m_Current.sqrMagnitude > 0.001f ) addLine ( -m_Current );
				return new Path ( m_Elements.ToArray () , m_bClosed , m_Size );
			}
		}
	}
}
