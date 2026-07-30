using System.Collections;
using System.Collections.Generic;
using Rudi.Core;
using Rudi.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Rudi.UI
{
    public static class GraphicRenderTools
    {
        private static readonly string TAG = "Rudis GraphicRenderTools" ;

        private static void log ( string msg )
        {
            //Log.i ( TAG , msg );
        }

        static private Matrix4x4 m_ModelMatrix = Matrix4x4.identity ;
        static private Matrix4x4 getModelMatrix ( Vector2 mul , Vector2 add )
        {
            m_ModelMatrix.m00 = mul.x;
            m_ModelMatrix.m11 = mul.y;
            m_ModelMatrix.m03 = add.x;
            m_ModelMatrix.m13 = add.y;
            return m_ModelMatrix;
        }
        private static float getPPU ( Graphic graphic )
        {
            var canvas = graphic.canvas ;
            if ( null == canvas ) return 1f;
            return canvas.scaleFactor;
        }
        private static RenderTexture nullReturn ( string msg ) { log ( msg ); return null; }
        private static bool falseReturn ( string msg ) { log ( msg ); return false; }
        public static RenderTexture drawUIVerticesToTexture ( RenderTexture renderTexture , Graphic graphic , int borderPixels )
        {
            var ppu = getPPU ( graphic ) ;
            var upp = 1f / ppu ;

            var rect = graphic.rectTransform.rect ;

            var VBorderPixels = Vector2Int.one * borderPixels ;
            var DstSizePixels = Vector2Int.RoundToInt ( rect.size * ppu ) + VBorderPixels * 2 ;

            if ( !DstSizePixels.hasArea () ) return nullReturn ( "no area" );
            if ( !renderTexture.Resize ( DstSizePixels ) ) return nullReturn ( "resize failed" );


            var VDstSize = ( Vector2 ) DstSizePixels * upp ;
            var VBorder  = ( Vector2 ) VBorderPixels * upp ;

            var Mul = Vector2.one / VDstSize ;
            var Add = ( VBorder - rect.min ) * Mul ;

            var ModelMatrix = getModelMatrix ( Mul , Add ) ;

            if ( !renderMesh ( renderTexture , graphic , ModelMatrix ) ) return null;
            return renderTexture;
        }

        private static bool renderMesh ( RenderTexture renderTexture , Graphic graphic , Matrix4x4 modelmatrix )
        {
            var renderer = graphic.canvasRenderer ;
            if ( null == renderer ) return falseReturn ( "no CanvasRenderer" );
            var mesh = renderer.GetMesh () ;
            if ( null == mesh ) return falseReturn ( "no Mesh" );

            var vertices  = mesh.vertices  ;
            var indices   = mesh.triangles ;
            var colors    = mesh.colors32  ;
            var texcoords = mesh.uv        ;

            // start rendering

            var previous = RenderTexture.active ;
            RenderTexture.active = renderTexture;

            graphic.material.mainTexture = graphic.mainTexture;
            if ( !graphic.material.SetPass ( 0 ) ) return falseReturn ( "pass failed" );

            GL.Clear ( false , true , Color.clear ); // fill transparent
            GL.Color ( Color.white );
            GL.PushMatrix ();
            GL.LoadOrtho ();
            GL.modelview = modelmatrix;
            GL.Begin ( GL.TRIANGLES );

            foreach ( var index in indices )
            {
                GL.Color ( colors [index] );
                GL.TexCoord ( texcoords [index] );
                GL.Vertex ( vertices [index] );
            }

            GL.End ();
            GL.PopMatrix ();

            graphic.material.mainTexture = null;
            RenderTexture.active = previous;
            return true;
        }

        public static RenderTexture drawUIVerticesToTexture ( RenderTexture renderTexture , Graphic graphic , Vector2 scale , Vector2 offset )
        {
            var rect = graphic.rectTransform.rect ;

            var bySize = Vector2.one / rect.size ;

            var Add = offset - rect.min * bySize ;
            var Mul = scale * bySize ;

            var ModelMatrix = getModelMatrix ( Mul , Add ) ;

            if ( !renderMesh ( renderTexture , graphic , ModelMatrix ) ) return null;

            return renderTexture;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // experimental stuff

        private static void logVertices ( Mesh mesh )
        {
            if ( null == mesh )
            {
                log ( "mesh is zero" );
                return;
            }

            var vertices  = mesh.vertices  ;
            if ( null == vertices )
            {
                log ( "vertices is zero" );
                return;
            }

            foreach ( var v in vertices )
            {
                log ( "v = " + v );
            }
        }

        private static readonly Vector4 w1 = new Vector4 ( 0 , 0 , 0 , 1 ) ;

        private static void logVertices ( Mesh mesh , Matrix4x4 model , Matrix4x4 proj )
        {
            if ( null == mesh )
            {
                log ( "mesh is zero" );
                return;
            }

            var vertices  = mesh.vertices  ;
            if ( null == vertices )
            {
                log ( "vertices is zero" );
                return;
            }

            foreach ( var v in vertices )
            {
                var v0 = w1 + ( Vector4 ) v ;
                var v1 = model * v0 ;
                var v2 = proj * v1 ;
                log ( $"v0: { v0 }, v1: { v1 }, v2: { v2 }" );
            }
        }

        private static Mesh m_Quad = null ;
        private static Mesh quad
        {
            get
            {
                if ( null == m_Quad )
                {
                    m_Quad = createQuad ();
                }
                return m_Quad;
            }
        }

        private static Mesh createQuad ()
        {
            // https://github.com/nothke/unity-utils/blob/master/Runtime/RTUtils.cs
            var mesh = new Mesh () ;

            float width  = 1 ;
            float height = 1 ;

            var vertices = new Vector3 [ 4 ]
            {
                new Vector3 (     0 ,      0 , 0 ) ,
                new Vector3 ( width ,      0 , 0 ) ,
                new Vector3 (     0 , height , 0 ) ,
                new Vector3 ( width , height , 0 )
            } ;
            mesh.vertices = vertices;

            var tris = new int [ 6 ]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };
            mesh.triangles = tris;

            var uv = new Vector2 [ 4 ]
            {
                new Vector2 ( 0 , 0 ) ,
                new Vector2 ( 1 , 0 ) ,
                new Vector2 ( 0 , 1 ) ,
                new Vector2 ( 1 , 1 )
            } ;
            mesh.uv = uv;

            var colors = new Color32 [ 4 ]
            {
                Color.white ,
                Color.white ,
                Color.white ,
                Color.white
            } ;
            mesh.colors32 = colors;

            return mesh;
        }
        static private readonly Matrix4x4 ortho = Matrix4x4.Ortho ( 0 , 1 , 0 , 1 , -100 , 100 ) ;

        private static RenderTexture drawUIVerticesToTexture ( Graphic graphic , Material material , Mesh mesh , float ppu , Vector2 scale , Vector2 offset , Vector2Int dstSize )
        {
            if ( null == material ) return null;
            var rect = graphic.rectTransform.rect ;
            //int borderPixels = 0 ;
            var OffsetReducer = 1/16f ;

            if ( !dstSize.hasArea () ) return null;
            var bySize = Vector2.one / rect.size ;
            var Add = offset * OffsetReducer - rect.min * bySize ;
            var Mul = scale * bySize ;

            //log ( "Mul = " + Mul ) ;
            //log ( "Add = " + Add ) ;

            var ModelMatrix = getModelMatrix ( Mul , Add ) ;

            var renderTexture = RenderTexture.GetTemporary ( dstSize.x , dstSize.y , 0 , RenderTextureFormat.ARGB32 );
            var previous = RenderTexture.active ;
            RenderTexture.active = renderTexture;
            material.mainTexture = graphic.mainTexture;
            if ( !material.SetPass ( 0 ) )
            {
                Log.i ( TAG , "drawToTexture() - pass failed" );
                return null;
            }


            GL.Color ( graphic.color );
            //GL.Color ( Color.white ) ;

            GL.PushMatrix ();

            //Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix ( ortho , true ) ;
            var projectionMatrix = ortho ;

            if ( false && Camera.current != null )
            {
                var cm = Camera.current.worldToCameraMatrix ;
                var ic = cm.inverse ;
                log ( "adding camera matrix:\n" + cm + "\n\n" + ic );
                log ( "prev proj:\n" + projectionMatrix );
                projectionMatrix *= ic;
                log ( "now proj:\n" + projectionMatrix );
            }


            //mesh = quad;
            //ModelMatrix = Matrix4x4.identity;


            //GL.LoadOrtho ();
            //log ( "ortho = \n" + ortho ) ;
            GL.LoadProjectionMatrix ( projectionMatrix );
            //GL.LoadProjectionMatrix ( ortho );
            //GL.LoadProjectionMatrix ( Matrix4x4.identity );


            //GL.LoadIdentity () ;
            //GL.modelview = Matrix4x4.identity;
            GL.modelview = ModelMatrix;


            //GL.MultMatrix ( ModelMatrix );
            //GL.MultMatrix ( Matrix4x4.identity );

            //GL.LoadPixelMatrix ();

            GL.Clear ( false , true , Color.clear ); // fill transparent

            var UseDrawMesh = false ;
            //UseDrawMesh = true;


            if ( UseDrawMesh )
            {
                // https://gist.github.com/nothke/e5214489f5690bffa86e2db1563e6fc9
                // https://github.com/nothke/unity-utils/blob/master/Runtime/RTUtils.cs

                //logVertices ( mesh , ModelMatrix , projectionMatrix );
                GL.invertCulling = true;

                //Graphics.DrawMeshNow ( mesh , ModelMatrix );
                Graphics.DrawMeshNow ( mesh , Matrix4x4.identity );

                GL.invertCulling = false;
            }
            else
            {
                // get mesh data

                var vertices  = mesh.vertices  ;
                var indices   = mesh.triangles ;
                var colors    = mesh.colors32  ;
                var texcoords = mesh.uv        ;


                // start rendering

                GL.Begin ( GL.TRIANGLES );

                foreach ( var index in indices )
                {
                    GL.Color ( colors [index] );
                    GL.TexCoord ( texcoords [index] );
                    GL.Vertex ( vertices [index] );
                }

                GL.End ();
            }


            GL.PopMatrix ();

            //material.color = Color.white ;
            material.mainTexture = null;

            RenderTexture.active = previous;
            return renderTexture;
        }

    }
}
