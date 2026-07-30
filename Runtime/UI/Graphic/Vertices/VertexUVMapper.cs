using Rudi.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    public class VertexUVMapper
    {
        private static readonly string TAG = "Rudis VertexUVMapper" ;

        private readonly Vector2 m_Mul ;
        private readonly Vector2 m_Add ;
        private readonly bool m_SwapXY ;

        private Vector4 m_RectMinMax ;

        private static (Vector2 mul, Vector2 add) getAffineTransformation ( Vector2 x1 , Vector2 x2 , Vector2 y1 , Vector2 y2 )
        {
            // x1 * mul + add = y1 (I)
            // x2 * mul + add = y2 (II)
            var mul = ( y2 - y1 ) / ( x2 - x1 ) ; // (II) - (I)
            var add = y1 - mul * x1 ;             // (I)
            return (mul, add);
        }

        private static Vector2 getSwappedXY ( Vector2 v ) => new Vector2 ( v.y , v.x );
        private static Vector2 getSwappedXY ( Vector2 v , bool swap ) => swap ? getSwappedXY ( v ) : v;

        private const uint m_uIndices = 0x12323010 ; // storage: xMin yMin xMin yMax xMax yMax xMax yMin
        private static int id_x ( int id ) => ( int ) ( m_uIndices >> ( id & 3 ) * 8 & 3 );
        private static int id_y ( int id ) => ( int ) ( m_uIndices >> ( id & 3 ) * 8 + 4 & 3 );
        private Vector2 getCorner ( int id ) => new Vector2 ( m_RectMinMax [id_x ( id )] , m_RectMinMax [id_y ( id )] );

        // public methods

        public VertexUVMapper ( Rect rect , bool swap_xy = false ) : this ( rect , Vector2.zero , Vector2.one , swap_xy ) { }
        public VertexUVMapper ( Rect rect , Vector2 uv_0 , Vector2 uv_2 , bool swap_xy = false )
        {
            m_SwapXY = swap_xy;
            setRect ( rect );

            var rMin = getSwappedXY ( rect.min , swap_xy ) ;
            var rMax = getSwappedXY ( rect.max , swap_xy ) ;

            var t = getAffineTransformation ( rMin , rMax , uv_0 , uv_2 ) ;
            m_Mul = t.mul;
            m_Add = t.add;
        }

        public void setRect ( Rect rect ) => m_RectMinMax = rect.getMinMaxV4 ();
        public void setRect ( Vector4 minmax ) => m_RectMinMax = minmax;

        public Vector2 getUV ( Vector2 position ) => getSwappedXY ( position , m_SwapXY ) * m_Mul + m_Add;

        public UIVertex getVertex ( int index , Color32 color ) => getVertex ( getCorner ( index ) , color );

        public UIVertex getVertex ( Vector2 position , Color32 color )
        {
            var vertex = UIVertex.simpleVert ;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = getUV ( position );
            //Log.i ( TAG , $"uv = { vertex.uv0 }" ) ;
            return vertex;
        }

        public void addVertex ( VertexHelper vh , Vector2 position , Color32 color ) => vh.AddVert ( getVertex ( position , color ) );
        public void addVertex ( VertexHelper vh , int index , Color32 color ) => vh.AddVert ( getVertex ( index , color ) );
        public int addRect ( VertexHelper vh , Color32 color , int start_index = 0 )
        {
            addVertex ( vh , 0 , color );
            addVertex ( vh , 1 , color );
            addVertex ( vh , 2 , color );
            addVertex ( vh , 3 , color );
            vh.AddTriangle ( start_index + 0 , start_index + 1 , start_index + 2 );
            vh.AddTriangle ( start_index + 2 , start_index + 3 , start_index + 0 );
            return start_index + 4;
        }
        public int addRect ( VertexHelper vh , Rect rect , Color32 color , int start_index = 0 )
        {
            setRect ( rect );
            return addRect ( vh , color , start_index );
        }
        public int addRect ( VertexHelper vh , Vector4 minmax , Color32 color , int start_index = 0 )
        {
            setRect ( minmax );
            return addRect ( vh , color , start_index );
        }
    }
}
