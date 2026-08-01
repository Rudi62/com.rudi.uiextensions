using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    [ AddComponentMenu ( "Rudi/UI/Speech Bubble" ) ]

    public class SpeechBubble : RawImage
    {
        private static readonly string TAG = "Rudis SpeechBubble" ;
        // 0xFFC931 derstandard.at
        public enum Side
        {
            None,
            Left,
            Bottom,
            Right,
            Top
        }

        // Tail
        [ SerializeField ] private Side m_TailSide = Side.Top ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_TailPos = 0 ;
        [ SerializeField ] private bool m_TipFollowsBase = true ;
        [ Range ( 0 , 1 ) ]
        [ SerializeField ] private float m_TailTip = 0 ;
        [ Range ( 0 , 20 ) ]
        [ SerializeField ] private float m_TailWidth = 15 ;
        [ Range ( 0 , 50 ) ]
        [ SerializeField ] private float m_TailLength = 30 ;

        // Body
        [ Range ( 0 , 25 ) ]
        [ SerializeField ] private float m_CornerRadius = 12 ;
        [ SerializeField ] bool m_DrawBorder = false ;
        [ Range ( 0 , 20 ) ]
        [ SerializeField ] float m_BorderWidth = 0 ;
        [ SerializeField ] Color m_BorderColor = new Color32 ( 50 , 50 , 50 , 255 ) ;
        [ SerializeField ] Color m_FillColor = Color.white ;
        [ SerializeField ] bool m_FadeBorder = false ;

        // testing
        //[ SerializeField ] bool m_NewApproach = false ;

        private static readonly Vector2 [] uv  = { Vector2.zero , Vector2.up , Vector2.one , Vector2.right } ;
        private static readonly float MinSize = 10 ;

        public Side tailSide
        {
            get => m_TailSide;
            set
            {
                if ( value == m_TailSide ) return;
                m_TailSide = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        public float tailPos
        {
            get => m_TailPos;
            set
            {
                if ( value == m_TailPos ) return;
                m_TailPos = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        public bool tipFollowsBase
        {
            get => m_TipFollowsBase;
            set
            {
                if ( value == m_TipFollowsBase ) return;
                m_TipFollowsBase = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        public float tailTip
        {
            get => m_TailTip;
            set
            {
                if ( value == m_TailTip ) return;
                m_TailTip = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        public float tailWidth
        {
            get => m_TailWidth;
            set
            {
                if ( value == m_TailWidth ) return;
                m_TailWidth = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        public float tailLength
        {
            get => m_TailLength;
            set
            {
                if ( value == m_TailLength ) return;
                m_TailLength = value;
                SetVerticesDirty ();
                SetMaterialDirty ();
            }
        }

        private float shrinkBy ( Rect rect )
        {
            switch ( m_TailSide )
            {
                case Side.None: return 0;
                case Side.Left:
                case Side.Right: return Mathf.Min ( m_TailLength , Mathf.Max ( rect.width - MinSize , 0f ) );
                case Side.Top:
                case Side.Bottom: return Mathf.Min ( m_TailLength , Mathf.Max ( rect.height - MinSize , 0f ) );
                default: return 0f;
            }
        }

        private Rect shrinkLeft ( Rect rect , float v ) => new Rect ( rect.x + v , rect.y , rect.width - v , rect.height );
        private Rect shrinkRight ( Rect rect , float v ) => new Rect ( rect.x , rect.y , rect.width - v , rect.height );
        private Rect shrinkBottom ( Rect rect , float v ) => new Rect ( rect.x , rect.y + v , rect.width , rect.height - v );
        private Rect shrinkTop ( Rect rect , float v ) => new Rect ( rect.x , rect.y , rect.width , rect.height - v );

        private (Rect rect, float length) getShrinked ( Rect rect )
        {
            var l = shrinkBy ( rect ) ;
            switch ( m_TailSide )
            {
                case Side.None: return (rect, 0f);
                case Side.Left: return (shrinkLeft ( rect , l ), l);
                case Side.Right: return (shrinkRight ( rect , l ), l);
                case Side.Top: return (shrinkTop ( rect , l ), l);
                case Side.Bottom: return (shrinkBottom ( rect , l ), l);
                default: return (rect, 0f);
            }
        }

        private static readonly Vector2 [] TriangleZero = { Vector2.zero , Vector2.zero , Vector2.zero } ;
        private Vector2 [] m_Triangle = new Vector2 [ 3 ] ;
        private Vector2 getTailTip ( Rect s )
        {
            //float tip = m_TipFollowsBase ? m_TailPos : m_TailTip ; // m_TailTip
            var tip = m_TailPos ; // m_TailTip
            switch ( m_TailSide )
            {
                case Side.Left: return new Vector2 ( s.xMin , Mathf.Lerp ( s.yMin , s.yMax , tip ) );
                case Side.Right: return new Vector2 ( s.xMax , Mathf.Lerp ( s.yMin , s.yMax , tip ) );
                case Side.Bottom: return new Vector2 ( Mathf.Lerp ( s.xMin , s.xMax , tip ) , s.yMin );
                case Side.Top: return new Vector2 ( Mathf.Lerp ( s.xMin , s.xMax , tip ) , s.yMax );
                default: return Vector2.zero;
            }
        }

        private (Vector2 p1, Vector2 p2) getTriangleBase ( Rect s )
        {
            switch ( m_TailSide )
            {
                case Side.Left:
                {
                    var margin = s.height - m_TailWidth ;
                    var ymin = s.yMin + m_TailPos * margin ;
                    var ymax = ymin + m_TailWidth ;
                    var x = s.xMin ;
                    return (new Vector2 ( x , ymin ), new Vector2 ( x , ymax ));
                }
                case Side.Right:
                {
                    var margin = s.height - m_TailWidth ;
                    var ymin = s.yMin + m_TailPos * margin ;
                    var ymax = ymin + m_TailWidth ;
                    var x = s.xMax ;
                    return (new Vector2 ( x , ymin ), new Vector2 ( x , ymax ));
                }
                case Side.Bottom:
                {
                    var margin = s.width - m_TailWidth ;
                    var xmin = s.xMin + m_TailPos * margin ;
                    var xmax = xmin + m_TailWidth ;
                    var y = s.yMin ;
                    return (new Vector2 ( xmin , y ), new Vector2 ( xmax , y ));
                }
                case Side.Top:
                {
                    var margin = s.width - m_TailWidth ;
                    var xmin = s.xMin + m_TailPos * margin ;
                    var xmax = xmin + m_TailWidth ;
                    var y = s.yMax ;
                    return (new Vector2 ( xmin , y ), new Vector2 ( xmax , y ));
                }
                default: return (Vector2.zero, Vector2.zero);
            }
        }

        private static readonly Vector2 half2 = Vector2.one * 0.5f ;
        private static Vector4 getMinMaxOfTriangle ( Vector2 [ ] triangle )
        {
            var Min = Vector2.Min ( triangle [ 0 ] , triangle [ 1 ] ) ;
            var Max = Vector2.Max ( triangle [ 0 ] , triangle [ 1 ] ) ;
            Min = Vector2.Min ( Min , triangle [2] );
            Max = Vector2.Max ( Max , triangle [2] );
            return new Vector4 ( Min.x , Min.y , Max.x , Max.y );
        }

        private Vector2 [ ] getTriangle ( Rect rectImage , Rect rectBody )
        {
            var Spot = getTailTip ( rectImage ) ;
            var Base = getTriangleBase ( rectBody ) ;
            var zero = new Vector2 ( rectImage.xMin , rectImage.yMin ) ;
            switch ( m_TailSide )
            {
                case Side.None:
                {
                    m_Triangle [0] = zero;
                    m_Triangle [1] = zero;
                    m_Triangle [2] = zero;
                    break;
                }
                case Side.Left:
                {
                    m_Triangle [0] = Base.p1;
                    m_Triangle [1] = Spot;
                    m_Triangle [2] = Base.p2;
                    break;
                }
                case Side.Right:
                {
                    m_Triangle [0] = Base.p2;
                    m_Triangle [1] = Spot;
                    m_Triangle [2] = Base.p1;
                    break;
                }
                case Side.Bottom:
                {
                    m_Triangle [0] = Base.p2;
                    m_Triangle [1] = Spot;
                    m_Triangle [2] = Base.p1;
                    break;
                }
                case Side.Top:
                {
                    m_Triangle [0] = Base.p1;
                    m_Triangle [1] = Spot;
                    m_Triangle [2] = Base.p2;
                    break;
                }
                default:
                {
                    m_Triangle [0] = zero;
                    m_Triangle [1] = zero;
                    m_Triangle [2] = zero;
                    break;
                }
            }
            return m_Triangle;
        }

        private Vector2 [ ] getTriangle ()
        {
            var (rectImage, rectBody) = rectangles;
            return getTriangle ( rectImage , rectBody );
        }

        private Vector4 getTailRect () => getMinMaxOfTriangle ( getTriangle () );
        private Vector4 getTailRect ( Rect rectImage , Rect rectBody ) => getMinMaxOfTriangle ( getTriangle ( rectImage , rectBody ) );

        private void lengthen_by ( Vector2 [ ] triangle , float v )
        {
            var dir1 = triangle [ 0 ] - triangle [ 1 ] ;
            if ( dir1.sqrMagnitude > 0f )
            {
                var length = dir1.magnitude ;
                dir1.Normalize ();
                triangle [0] = triangle [1] + dir1 * ( length + v );
            }
            var dir2 = triangle [ 2 ] - triangle [ 1 ] ;
            if ( dir2.sqrMagnitude > 0f )
            {
                var length = dir2.magnitude ;
                dir2.Normalize ();
                triangle [2] = triangle [1] + dir2 * ( length + v );
            }
        }

        private void lengthen_to ( Vector2 [ ] triangle , float v )
        {
            var dir1 = triangle [ 0 ] - triangle [ 1 ] ;
            if ( dir1.sqrMagnitude > 0f )
            {
                //float length = dir1.magnitude ;
                dir1.Normalize ();
                triangle [0] = triangle [1] + dir1 * v;
            }
            var dir2 = triangle [ 2 ] - triangle [ 1 ] ;
            if ( dir2.sqrMagnitude > 0f )
            {
                dir2.Normalize ();
                triangle [2] = triangle [1] + dir2 * v;
            }
        }

        private float getMaxLength ( Vector2 [ ] triangle )
        {
            var d1 = Vector2.Distance ( triangle [ 0 ] , triangle [ 1 ] ) ;
            var d2 = Vector2.Distance ( triangle [ 2 ] , triangle [ 1 ] ) ;
            return Mathf.Max ( d1 , d2 );
        }

        private void getVertices ( VertexHelper vh )
        {
            var (rectImage, rectBody) = rectangles;
            {
                Color32 color32 = color ;

                if ( true )
                {
                    var Mapper = new VertexUVMapper ( rectBody ) ;
                    Mapper.addRect ( vh , rectImage , color32 );
                    //int nextIndex = Mapper.addRect ( vh , color32 ) ;
                    //nextIndex = Mapper.addRect ( vh , getTailRect () , color32 , nextIndex );
                }
                //else
                //{
                //    var tr = getTriangle ( rectImage , rectBody ) ;
                //    var v = rectBody.getMinMaxV4 () ;
                //    vh.AddVert ( new Vector3 ( v.x , v.y ) , color32 , uv [0] );
                //    vh.AddVert ( new Vector3 ( v.x , v.w ) , color32 , uv [1] );
                //    vh.AddVert ( new Vector3 ( v.z , v.w ) , color32 , uv [2] );
                //    vh.AddVert ( new Vector3 ( v.z , v.y ) , color32 , uv [3] );

                //    var uvTail = new Vector2 ( 0.5f , 0 ) ;
                //    if ( rectBody.height > rectBody.width ) uvTail = new Vector2 ( 0 , 0.5f );

                //    vh.AddVert ( tr [0] , color32 , uvTail );
                //    vh.AddVert ( tr [1] , color32 , uvTail );
                //    vh.AddVert ( tr [2] , color32 , uvTail );
                //    vh.AddTriangle ( 0 , 1 , 2 );
                //    vh.AddTriangle ( 2 , 3 , 0 );
                //    vh.AddTriangle ( 4 , 5 , 6 );
                //}

            }
        }

        private static Vector2 getRadii ( float edge_min , float edge_length , float tail_width , float tail_pos , float byRad )
        {
            var edge_max = edge_min + edge_length ;
            var margin = edge_length - tail_width ;
            var vmin = edge_min + tail_pos * margin ;
            var vmax = vmin + tail_width ;
            var lo = Mathf.Clamp01 ( ( vmin - edge_min ) * byRad ) ;
            var hi = Mathf.Clamp01 ( ( edge_max - vmax ) * byRad ) ;
            return new Vector2 ( lo , hi );
        }

        private bool isTailOnSide ()
        {
            switch ( m_TailSide )
            {
                case Side.Left:
                case Side.Right: return true;
                default: return false;
            }
        }

        private Vector4 getRadii ()
        {
            if ( true ) return Vector4.one;
            if ( m_CornerRadius > 0 )
            {
                var byRad = 1f / m_CornerRadius ;
                var rect = rectTransform.rect ;
                var l = isTailOnSide () ? new Vector2 ( rect.yMin , rect.height ) : new Vector2 ( rect.xMin , rect.width ) ;
                var r = getRadii ( l.x , l.y , m_TailWidth , m_TailPos , byRad ) ;
                switch ( m_TailSide )
                {
                    case Side.Left: return new Vector4 ( r.x , r.y , 1f , 1f );
                    case Side.Right: return new Vector4 ( 1f , 1f , r.y , r.x );
                    case Side.Bottom: return new Vector4 ( r.x , 1f , 1f , r.y );
                    case Side.Top: return new Vector4 ( 1f , r.x , r.y , 1f );
                }
            }
            return Vector4.one;
        }

        protected override void OnPopulateMesh ( VertexHelper vh )
        {
            vh.Clear ();
            if ( mainTexture != null )
            {
                getVertices ( vh );
            }
        }

        private (Rect rectImage, Rect rectBody) rectangles
        {
            get
            {
                var rectImage = rectTransform.rect ;
                var rectBody = getShrinked ( rectImage ) . rect ;
                return (rectImage, rectBody);
            }
        }

        private Rect getRectBody () => getShrinked ( GetPixelAdjustedRect () ).rect;
        private Vector2 getBodySize () => getRectBody ().size;

        //public override Material material => m_NewApproach ? materialSpeechBubble : matRoundedCorner;
        public override Material material => materialSpeechBubble;
        protected override void UpdateMaterial ()
        {
            base.UpdateMaterial ();
            if ( true )
            {
                updateMaterial ( materialForRendering );
            }
            //else
            //{
            //    matRoundedCorner.radius = m_CornerRadius;
            //    matRoundedCorner.radii = getRadii ();
            //    matRoundedCorner.drawBorder = m_DrawBorder;
            //    matRoundedCorner.fadeBorder = m_FadeBorder;
            //    matRoundedCorner.borderWidth = m_BorderWidth;
            //    matRoundedCorner.colorBorder = m_BorderColor;
            //    matRoundedCorner.colorFill = m_FillColor;
            //    if ( null != canvas ) matRoundedCorner.setup ( getBodySize () , canvas.scaleFactor );
            //}
        }
        private bool hasBorder => m_DrawBorder && m_BorderWidth > 0f;
        private bool hasRoundCorners => m_CornerRadius > 0;
        private bool hasShadow => false;
        private bool hasEventuallyShadow => false;
        private float ppu => null != canvas ? canvas.scaleFactor : 1f;

        static float getRelativeRadius ( Vector2 size , float rel )
        {
            return Mathf.Min ( size.x , size.y ) * rel;
        }

        static float getLowestValue ( Vector2 v ) => Mathf.Min ( v.x , v.y );

        float getRadiusToDraw ()
        {
            var half = getRelativeRadius ( getBodySize () , 0.5f ) ;
            return Mathf.Min ( half , m_CornerRadius );
        }

        private void updateMaterial ( Material mat )
        {
            const bool UseOptimizedRect = true ;
            var optimizeRect = UseOptimizedRect && !hasBorder &&  hasRoundCorners  ;
            var Round = hasRoundCorners ;
            var HasShadow = hasShadow ;
            mat.EnableKeyword ( "MAP_TEX_ALPHA" , false );
            mat.EnableKeyword ( "MAP_TEX_COLOR_WITH_TEX_ALPHA" , false );
            mat.EnableKeyword ( "ROUND_CORNERS" , Round );
            mat.EnableKeyword ( "PIXEL_CORRECT" , false );
            mat.EnableKeyword ( "MAP_CORNER_ALPHA" , false );
            mat.EnableKeyword ( "DRAW_BORDER" , hasBorder );
            mat.EnableKeyword ( "DRAW_BORDER_FADED" , hasBorder && m_FadeBorder );
            mat.EnableKeyword ( "DROP_SHADOW" , HasShadow );

            var (rectImage, rectBody) = rectangles;

            //Rect rectBody = getRectBody () ;
            var BodySize = rectBody.size ;
            //Vector2 size = rect.size ;
            if ( !BodySize.hasArea () ) return; // size = Vector2.one;
            var Ppu = ppu ;
            //log ( $"ppu = { Ppu }" ) ;
            var fBorderWidth = m_BorderWidth * Ppu ;
            //if ( m_PixelCorrect && !m_FadeBorder ) fBorderWidth = Mathf.Round ( fBorderWidth );
            //float Smoothing = m_Smoothing ;
            var m_Smoothing = 1f ;
            var HalfSmoothing = m_Smoothing * 0.5f ;
            var InvSmoothing = 1f /  m_Smoothing  ; // 1.0f / Mathf.Max ( m_Smoothing , 0.0001f ) ;
            //Vector2 HalfSmoothing2 = Vector2.one * HalfSmoothing;
            //Vector2 Smoothing2 = Vector2.one * m_Smoothing;
            var BodySizeInPixels = BodySize * Ppu ;
            var DrawingRectSize_unrotated = BodySizeInPixels ;
            if ( optimizeRect )
            {
                DrawingRectSize_unrotated = Vector2.Max ( DrawingRectSize_unrotated - Vector2.one * m_Smoothing * 1.0f , Vector2.zero );
            }
            var HalfSizeInPixels = Mathf.Min ( BodySizeInPixels.x , BodySizeInPixels.y ) * 0.5f ;
            var Smoothing = optimizeRect ? m_Smoothing : HalfSmoothing ;
            var rectangle = new Vector4 ( HalfSizeInPixels , - Smoothing * InvSmoothing , - InvSmoothing , HalfSmoothing ) ;

            //Log.i ( TAG , "HalfSize = " + HalfSize ) ;

            mat.SetVector ( "_Rectangle" , rectangle );
            {
                // scale
                var offset = 0f ;
                //if ( isSwapped ) offset = offset.getSwapped ();
                var scaleX = BodySizeInPixels.x ;
                var scaleY = BodySizeInPixels.y ;
                var offsetX = offset * scaleX ;
                var offsetY = offset * scaleY ;
                var scale = new Vector4 ( offsetX , offsetY , scaleX , scaleY ) ;
                mat.SetVector ( "_Scale" , scale );
            }

            if ( Round || hasBorder || HasShadow )
            {
                var radius = getRadiusToDraw () ;
                var radiusPixels = Mathf.Min ( radius * Ppu , HalfSizeInPixels ) ;
                var radiusPixels4 = Vector4.one * radius * Ppu ;
                //Vector4 RadiusScale = new Vector4 ( m_RadiusScale1 , m_RadiusScale2 , m_RadiusScale3 , m_RadiusScale4  ) ;
                var RadiusScale = getRadii () ;

                radiusPixels4 = Vector4.Scale ( radiusPixels4 , RadiusScale );
                //if ( m_PixelCorrect ) radiusPixels4 = ( radiusPixels4 * 2f ).getRounded () * 0.5f;

                // new since using sdf
                //Vector4 rectangle = new Vector4 ( SizePixel.x , SizePixel.y , 1f / Smoothing , m_Smoothing * 0.5f ) ;
                var CenterX = new Vector4 ( - BodySizeInPixels.x * 0.5f , - BodySizeInPixels.y * 0.5f , DrawingRectSize_unrotated.x * 0.5f , DrawingRectSize_unrotated.y * 0.5f ) ;
                var ZeroOffset = 0.0f ;
                if ( optimizeRect )
                {
                    radiusPixels4 = Vector4.Max ( radiusPixels4 - Vector4.one * HalfSmoothing , Vector4.zero );
                    ZeroOffset = HalfSmoothing;
                }
                if ( Round && !hasBorder && !hasEventuallyShadow )
                {
                    if ( radiusPixels4.x == 0.0f && radiusPixels4.w == 0.0f )
                    {
                        // top round
                        CenterX.y = -ZeroOffset; // offset y -> 0
                        CenterX.w = DrawingRectSize_unrotated.y;
                    }
                    else if ( radiusPixels4.y == 0.0f && radiusPixels4.z == 0.0f )
                    {
                        // bottom round
                        CenterX.y = ZeroOffset - BodySizeInPixels.y; // offset y -> full
                        CenterX.w = DrawingRectSize_unrotated.y;
                    }
                    else if ( radiusPixels4.x == 0.0f && radiusPixels4.y == 0.0f )
                    {
                        // right round
                        CenterX.x = -ZeroOffset; // offset x -> 0
                        CenterX.z = DrawingRectSize_unrotated.x;
                    }
                    else if ( radiusPixels4.z == 0.0f && radiusPixels4.w == 0.0f )
                    {
                        // left round
                        CenterX.x = ZeroOffset - BodySizeInPixels.x; // offset x -> full
                        CenterX.z = DrawingRectSize_unrotated.x;
                    }
                }

                //mat.SetVector ( "_Radius" , radiusPixels4 );
                mat.SetFloat ( "_Radius" , radiusPixels );
                mat.SetVector ( "_CenterX" , CenterX );
                //mat.SetVector ( "_CenterY" , CenterY ) ;
            }

            if ( hasBorder )
            {
                //float borderFade = m_FadeBorder ? 1f / fBorderWidth : 1f ;
                //float BorderInside = m_FadeBorder ? -fBorderWidth : -fBorderWidth - Smoothing ;
                //float BorderOutside = m_FadeBorder ? 0.0f : -fBorderWidth + Smoothing ;
                //Vector4 BorderWidth = new Vector4 ( fBorderWidth , BorderInside , BorderOutside , 0f ) ;

                var Width = m_FadeBorder ? fBorderWidth : m_Smoothing ;
                var ByWidth = 1.0f / Mathf.Max ( Width , 0.00001f ) ;
                var BorderLo = m_FadeBorder ? -fBorderWidth : -fBorderWidth - HalfSmoothing ;
                var BorderLoByWidth = BorderLo * ByWidth ;
                //Vector4 BorderWidth = new Vector4 ( BorderLoByWidth , BorderLo , Width , ByWidth ) ;
                var BorderWidth = new Vector4 ( BorderLoByWidth , BorderLo , fBorderWidth , ByWidth ) ;
                mat.SetVector ( "_BorderWidth" , BorderWidth );
                mat.SetColor ( "_BorderColor" , m_BorderColor );
                mat.SetColor ( "_FillColor" , m_FillColor );
            }

            {
                // triangle
                var Mapper = new VertexUVMapper ( rectBody ) ;

                var tr = getTriangle ( rectImage , rectBody ) ;
                var MaxLength = getMaxLength ( tr ) ;
                var lengthenBy = Mathf.Max ( fBorderWidth , m_CornerRadius , 1f ) ;
                lengthen_to ( tr , MaxLength + lengthenBy );
                var p0 = Mapper.getUV ( tr [ 0 ] ) * BodySizeInPixels ;
                var p1 = Mapper.getUV ( tr [ 1 ] ) * BodySizeInPixels ;
                var p2 = Mapper.getUV ( tr [ 2 ] ) * BodySizeInPixels ;

                var d1 = p0 - p1 ;
                var d2 = p2 - p1 ;

                var dot_d1 = Vector2.Dot ( d1 , d1 ) ;
                var dot_d1_d2 = Vector2.Dot ( d1 , d2 ) ;

                var _Triangle02 = new Vector4 ( d1.x , d1.y , d2.x , d2.y ) ;
                var _Triangle1  = new Vector4 ( p1.x , p1.y , 1f / dot_d1 , dot_d1_d2 ) ;
                mat.SetVector ( "_Triangle02" , _Triangle02 );
                mat.SetVector ( "_Triangle1" , _Triangle1 );
            }
        }

        //private MaterialRoundedCorner m_MaterialRoundedCorner = null ;
        //private MaterialRoundedCorner matRoundedCorner
        //{
        //    get
        //    {
        //        if ( null == m_MaterialRoundedCorner )
        //        {
        //            m_MaterialRoundedCorner = new MaterialRoundedCorner ();
        //            m_MaterialRoundedCorner.radius = m_CornerRadius;
        //        }
        //        return m_MaterialRoundedCorner;
        //    }
        //}

        private static readonly string materialName = "Rudi/UI/SpeechBubble" ;

        private Material m_MaterialSpeechBubble = null ;
        private Material materialSpeechBubble
        {
            get
            {
                if ( null == m_MaterialSpeechBubble )
                {
                    m_MaterialSpeechBubble = Utils.createMaterial ( materialName );
                    if ( null != m_MaterialSpeechBubble && !CanvasUpdateRegistry.IsRebuildingGraphics () ) SetMaterialDirty ();

                }
                return m_MaterialSpeechBubble;
            }
        }

        protected override void OnRectTransformDimensionsChange ()
        {
            base.OnRectTransformDimensionsChange ();
            SetMaterialDirty ();
        }

        protected override void OnDestroy ()
        {
            Utils.DestroyObjectAndZero ( ref m_MaterialSpeechBubble );

            base.OnDestroy ();
        }
    }
#if UNITY_EDITOR
    [CustomEditor ( typeof ( SpeechBubble ) )]
    [CanEditMultipleObjects]
    public class SpeechBubbleEditor : UnityEditor.UI.GraphicEditor
    {

        //[ SerializeField ] private bool m_TipFollowsBase = true ;
        //[ SerializeField ] private float m_TailTip = 0 ;


        SerializedProperty m_TailSide       ;
        SerializedProperty m_TailPos        ;
        SerializedProperty m_TipFollowsBase ;
        SerializedProperty m_TailTip        ;
        SerializedProperty m_TailWidth      ;
        SerializedProperty m_TailLength     ;

        SerializedProperty m_CornerRadius ;
        SerializedProperty m_DrawBorder   ;
        SerializedProperty m_BorderWidth  ;
        SerializedProperty m_BorderColor  ;
        SerializedProperty m_FillColor    ;
        SerializedProperty m_FadeBorder   ;

        //SerializedProperty m_NewApproach ;


        protected override void OnEnable ()
        {
            base.OnEnable ();
            m_TailSide = serializedObject.FindProperty ( "m_TailSide" );
            m_TailPos = serializedObject.FindProperty ( "m_TailPos" );
            m_TipFollowsBase = serializedObject.FindProperty ( "m_TipFollowsBase" );
            m_TailTip = serializedObject.FindProperty ( "m_TailTip" );
            m_TailWidth = serializedObject.FindProperty ( "m_TailWidth" );
            m_TailLength = serializedObject.FindProperty ( "m_TailLength" );

            m_CornerRadius = serializedObject.FindProperty ( "m_CornerRadius" );
            m_DrawBorder = serializedObject.FindProperty ( "m_DrawBorder" );
            m_BorderWidth = serializedObject.FindProperty ( "m_BorderWidth" );
            m_BorderColor = serializedObject.FindProperty ( "m_BorderColor" );
            m_FillColor = serializedObject.FindProperty ( "m_FillColor" );
            m_FadeBorder = serializedObject.FindProperty ( "m_FadeBorder" );

            // testing
            //m_NewApproach = serializedObject.FindProperty ( "m_NewApproach" );
        }
        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            EditorGUILayout.PropertyField ( m_Color );

            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ( "Tail" , EditorStyles.boldLabel );
            EditorGUILayout.PropertyField ( m_TailSide );
            EditorGUILayout.PropertyField ( m_TailPos );
            //EditorGUILayout.PropertyField ( m_TipFollowsBase ) ;
            if ( !m_TipFollowsBase.boolValue )
            {
                EditorGUILayout.PropertyField ( m_TailTip );
            }
            EditorGUILayout.PropertyField ( m_TailWidth );
            EditorGUILayout.PropertyField ( m_TailLength );

            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ( "Body" , EditorStyles.boldLabel );
            EditorGUILayout.PropertyField ( m_CornerRadius );
            EditorGUILayout.PropertyField ( m_DrawBorder );
            if ( m_DrawBorder.boolValue )
            {
                EditorGUILayout.PropertyField ( m_BorderWidth );
                EditorGUILayout.PropertyField ( m_BorderColor );
                EditorGUILayout.PropertyField ( m_FillColor );
                EditorGUILayout.PropertyField ( m_FadeBorder );
            }

            RaycastControlsGUI ();
            MaskableControlsGUI ();
#if RudisApp
            {
                EditorGUILayout.Space ();
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField ( "Testing" , EditorStyles.boldLabel );
                //EditorGUILayout.PropertyField ( m_NewApproach );
                EditorGUI.indentLevel--;
            }
#endif
            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif

}
