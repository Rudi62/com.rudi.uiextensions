
// last change:
// 03.03.2022 12:17
// by: Rudi Weinacker

using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Rudi.Core.DateTimeUtils;
using System.Text;
//using UnityEditor.SearchService;
using UnityEngine.SceneManagement;
using UnityEngine.Pool;
using Unity.Collections;
using System.Reflection;
using System.ComponentModel;
using Rudi.Extensions;
//using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
//public static class MathR
//{
//	public const double Deg2Rad = Math.PI / 180.0 ;
//}

namespace Rudi.Core
{
    /*
    public static class GameObjectExtensions
    {
        private static readonly string TAG = "Rudis GameObjectExtensions" ;

        public static void SetParent ( this GameObject gameObject , GameObject parent )
        {
            if ( null == parent ) return;
            gameObject.transform.SetParent ( parent.transform , false );
        }

        public static int GetNumChildren ( this GameObject gameObject )
        {
            return gameObject.transform.childCount;
        }

        public static GameObject [ ] getChildren ( this GameObject gameObject )
        {
            int num = gameObject.transform.childCount ;
            GameObject [] Result = new GameObject [ num ] ;
            for ( int i = 0 ; i < num ; i++ )
            {
                Result [i] = gameObject.transform.GetChild ( i ).gameObject;
            }
            return Result;
        }

        public static void DestroyChildren ( this GameObject gameObject )
        {
            //Transform transform = gameObject.transform ;
            //while ( 0 < transform.childCount )
            //{
            //    Transform child = transform.GetChild ( 0 ) ;
            //    //Log.i ( "DestroyChildren" , "child = " + child ) ;
            //    Utils.DestroyObject ( child.gameObject );
            //}
            if ( gameObject == null ) return; // Early Return Schutz

            Transform transform = gameObject.transform ;
            int childCount = transform.childCount;

            // C++-Blick: Wir laufen rückwärts von der höchsten ID zu 0.
            // Das verhindert jegliche Index-Verschiebung und Endlosschleifen!
            for ( int i = childCount - 1 ; i >= 0 ; i-- )
            {
                Transform child = transform.GetChild ( i ) ;
                Utils.DestroyObject ( child.gameObject );
            }
        }

        public static void DestroyActiveChildren ( this GameObject gameObject )
        {
            foreach ( Transform child in gameObject.transform )
            {
                if ( child.gameObject.activeSelf )
                {
                    //GameObject.Destroy ( child.gameObject );

#if UNITY_EDITOR
                    if ( Application.isPlaying ) GameObject.Destroy ( child.gameObject );
                    else GameObject.DestroyImmediate ( child.gameObject );
#else
				GameObject.Destroy ( child.gameObject );
#endif
                }
            }
        }

        public static void DestroyComplete ( this GameObject gameObject )
        {
            //GameObject.Destroy ( gameObject );

#if UNITY_EDITOR
            if ( Application.isPlaying ) GameObject.Destroy ( gameObject );
            else GameObject.DestroyImmediate ( gameObject );
#else
				GameObject.Destroy ( gameObject );
#endif
        }
        //        public static bool RemoveComponent<Component> ( this GameObject obj )
        //        {
        //            Component component = obj.GetComponent < Component > () ;

        //            if ( component != null )
        //            {
        //#if UNITY_EDITOR
        //                GameObject.DestroyImmediate ( component as UnityEngine.Object , true );
        //#else
        //                GameObject.Destroy ( component as UnityEngine.Object );
        //#endif
        //                return true ;
        //            }
        //            return false ;
        //        }
        public static bool RemoveComponent < T > ( this GameObject obj ) where T : UnityEngine.Component
        {
            T component = obj.GetComponent < T > () ;

            if ( component != null )
            {
#if UNITY_EDITOR
                GameObject.DestroyImmediate ( component , true ) ;
#else
                GameObject.Destroy ( component ) ;
#endif
                return true ;
            }
            return false ;
        }

        private static bool isRootCanvas ( Canvas canvas )
        {
            if ( !canvas.isActiveAndEnabled ) return false;
            if ( canvas.isRootCanvas ) return true;
            if ( canvas.overrideSorting ) return true;
            return false;
        }

        public static Canvas getRootCanvas ( this GameObject obj )
        {
            // code is borrowed from Dropdown.cs, in method Show()
            var list = ListPool < Canvas > . Get () ;
            obj.GetComponentsInParent ( false , list );
            var listCount = list.Count ;
            if ( listCount == 0 )
            {
                Log_Old.i ( TAG , "listCount is zero" );
                return null;
            }

            Canvas Result = list [ listCount - 1 ] ;

            for ( int i = 0 ; i < list.Count ; ++i )
            {
                if ( isRootCanvas ( list [i] ) )
                {
                    Result = list [i];
                    break;
                }
            }
            ListPool<Canvas>.Release ( list );
            return Result;
        }

        public static GameObject createChild ( this GameObject obj , string name , params Type [ ] components )
        {
            GameObject Child = new GameObject ( name , components ) ;
            Child.transform.SetParent ( obj.transform , false );
            return Child;
        }
    }

    public static class MonoBehaviorExtension
    {
        //public static T getComponentIfZero < T > ( this MonoBehaviour mono , ref T comp ) where T : UnityEngine.Component
        //{
        //    if ( null == comp ) comp = mono.GetComponent<T> ();
        //    return comp;
        //}

        public static T [ ] GetComponentsInOnlyChildren<T> ( this MonoBehaviour mono , bool includeInactive = false ) where T : MonoBehaviour
        {
            List < T > Result = new () ;
            T [] arr = mono.GetComponentsInChildren < T > ( includeInactive ) ;
            foreach ( T obj in arr )
            {
                if ( obj.transform.parent.gameObject == mono.gameObject )
                {
                    if ( !includeInactive && !obj.enabled ) continue;
                    Result.Add ( obj );
                }
            }
            return Result.ToArray ();
        }
        public static GameObject GetParent ( this MonoBehaviour mono )
        {
            return mono.transform.parent.gameObject;
        }
        public static T GetComponentInParentOnly<T> ( this MonoBehaviour mono ) where T : MonoBehaviour
        {
            return mono.GetParent ().GetComponent<T> ();
        }
        public static void SetEnabled ( this MonoBehaviour mono , bool en )
        {
            mono.enabled = en;
        }
        public static T getFirstActiveEnabledParent<T> ( this MonoBehaviour mono ) where T : MonoBehaviour
        {
            // this code is extracted and generalized from Graphic.cs
            var list = ListPool < T > . Get () ;
            T Result = null ;
            mono.gameObject.GetComponentsInParent ( false , list );
            if ( list.Count > 0 )
            {
                // Find the first active and enabled object.
                for ( int i = 0 ; i < list.Count ; ++i )
                {
                    if ( list [i].isActiveAndEnabled )
                    {
                        Result = list [i];
                        break;
                    }
                }
            }
            ListPool<T>.Release ( list );
            return Result;
        }
    }

    public static class CanvasExtension
    {
        public static float getPPU ( this Canvas canvas )
        {
            if ( null == canvas ) return 1f;
            return canvas.scaleFactor;
        }
        public static bool hasPPU ( this Canvas canvas )
        {
            if ( null == canvas ) return false;
            return 1f != canvas.scaleFactor;
        }
    }

    public static class ButtonExtension
    {
        public static void SetInteractable ( this Button button , bool interactable )
        {
            button.interactable = interactable;
        }
    }
    public static class TextExtension
    {
        public static void SetText ( this Text obj , string text )
        {
            obj.text = text;
        }
    }

    public static class ColorExtension
    {
        public static Color GetWithAlpha ( this Color c , float alpha ) => new ( c.r , c.g , c.b , alpha );
    }

    public static class Vector3Extensions
    {
        public static Vector3 GetHorizontal ( this Vector3 v ) => new Vector3 ( v.x , 0 , v.z );
    }
    public static class Vector4Extensions
    {
        public static Vector4 getRounded ( this Vector4 v ) => new Vector4 ( Mathf.Round ( v.x ) , Mathf.Round ( v.y ) , Mathf.Round ( v.z ) , Mathf.Round ( v.w ) );
    }

    public static class Vector2IntExtensions
    {
        public static bool hasArea ( this Vector2Int size ) => size.x > 0 && size.y > 0;
        public static Vector2Int getSwapped ( this Vector2Int v ) => new Vector2Int ( v.y , v.x );
    }
    public static class Vector2Extensions
    {
        public static bool hasArea ( this Vector2 size ) => size.x > 0 && size.y > 0;
        public static Vector2 getSwapped ( this Vector2 v ) => new Vector2 ( v.y , v.x );
    }

    public static class RectExtension
    {
        public static Rect getRectRightToRight ( this Rect r , float width ) => new Rect ( r.xMax , r.y , width , r.height );
        public static Rect getRectLeftToLeft ( this Rect r , float width ) => new Rect ( r.x - width , r.y , width , r.height );
        public static Rect getRectAboveTop ( this Rect r , float height ) => new Rect ( r.x , r.yMax , r.width , height );
        public static Rect getRectBelowBottom ( this Rect r , float height ) => new Rect ( r.x , r.y - height , r.width , height );
        public static Vector4 getMinMaxV4 ( this Rect r ) => new Vector4 ( r.xMin , r.yMin , r.xMax , r.yMax );
    }

    public static class RectTransformExtension
    {
        public static readonly Vector2 Vec2Half = Vector2.one * 0.5f ;

        public static Vector2 PivotTo ( this RectTransform rt , Vector2 rel_pos ) => ( rel_pos - rt.pivot ) * rt.rect.size;
        public static Vector2 PivotToCenter ( this RectTransform rt ) => PivotTo ( rt , Vec2Half );
        public static Vector2 PivotToLL ( this RectTransform rt ) => PivotTo ( rt , Vector2.zero );
        public static Vector2 PivotToLR ( this RectTransform rt ) => PivotTo ( rt , Vector2.right );
        public static Vector2 PivotToTR ( this RectTransform rt ) => PivotTo ( rt , Vector2.one );
        public static Vector2 PivotToTL ( this RectTransform rt ) => PivotTo ( rt , Vector2.up );
        public static void UpdateSize ( this RectTransform rt , Vector2 size )
        {
            if ( size != rt.sizeDelta ) rt.sizeDelta = size;
        }
        public static void UpdatePosition ( this RectTransform rt , Vector2 position )
        {
            if ( rt.anchoredPosition != position ) rt.anchoredPosition = position;
        }

        public static void SetHeight ( this RectTransform rt , float height )
        {
            var sd = rt.sizeDelta ;
            rt.sizeDelta = new Vector2 ( sd.x , height );
        }

        public static void SetFull ( this RectTransform rt )
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            //rt.sizeDelta = Vector2.zero;
            //rt.pivot = Vector2.one * 0.5f;
            //rt.anchoredPosition = Vector2.zero ;
            rt.pivot = Vec2Half;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        public static void SetZero ( this RectTransform rt )
        {
            rt.pivot = Vec2Half;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
        public static void SetTop ( this RectTransform rt , float height )
        {
            rt.pivot = new Vector2 ( 0.5f , 1f );
            rt.anchorMin = Vector2.up;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2 ( 0 , height );
        }
        public static void SetBottom ( this RectTransform rt , float height )
        {
            rt.pivot = new Vector2 ( 0.5f , 1f );
            rt.pivot = new Vector2 ( 0.5f , 0f );
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.right;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2 ( 0 , height );
        }
        public static void SetFull ( this RectTransform rt , Vector4 margins )
        {
            rt.pivot = Vec2Half;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2 ( margins.x , margins.y );
            rt.offsetMax = new Vector2 ( -margins.z , -margins.w );
        }

        public static void SetFull ( this RectTransform rt , float to_left , float to_right , float to_top , float to_bottom )
        {
            rt.pivot = Vec2Half;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2 ( to_left , to_bottom );
            rt.offsetMax = new Vector2 ( -to_right , -to_top );
        }

        public static void setLeftCenter ( this RectTransform rt , float posx , float posy , float sizex , float sizey )
        {
            rt.anchorMin = new Vector2 ( 0 , 0.5f );
            rt.anchorMax = new Vector2 ( 0 , 0.5f );
            rt.pivot = new Vector2 ( 0 , 0.5f );
            rt.anchoredPosition = new Vector2 ( posx , posy );
            rt.sizeDelta = new Vector2 ( sizex , sizey );
        }

        private static void setStd ( RectTransform rt , float apos_x , float apos_y , float pos_x , float pos_y , float width , float height )
        {
            Vector2 apos = new Vector2 ( apos_x , apos_y ) ;
            rt.anchorMin = apos;
            rt.anchorMax = apos;
            rt.pivot = apos;
            rt.anchoredPosition = new Vector2 ( pos_x , pos_y );
            rt.sizeDelta = new Vector2 ( width , height );
        }

        private static void setStretchedY ( RectTransform rt , float apos_x , float pos_x , float top , float width , float bottom )
        {
            rt.anchorMin = new Vector2 ( apos_x , 0 );
            rt.anchorMax = new Vector2 ( apos_x , 1 );
            rt.pivot = new Vector2 ( apos_x , 0.5f );
            rt.anchoredPosition = new Vector2 ( pos_x , 0 );
            rt.sizeDelta = new Vector2 ( width , 0 );
            rt.offsetMin = new Vector2 ( rt.offsetMin.x , bottom );
            rt.offsetMax = new Vector2 ( rt.offsetMax.x , -top );
        }

        private static void setStretchedX ( RectTransform rt , float apos_y , float left , float pos_y , float right , float height )
        {
            rt.anchorMin = new Vector2 ( 0 , apos_y );
            rt.anchorMax = new Vector2 ( 1 , apos_y );
            rt.pivot = new Vector2 ( 0.5f , apos_y );
            rt.anchoredPosition = new Vector2 ( 0 , pos_y );
            rt.sizeDelta = new Vector2 ( 0 , height );
            rt.offsetMin = new Vector2 ( left , rt.offsetMin.y );
            rt.offsetMax = new Vector2 ( -right , rt.offsetMax.y );
        }

        private static void setStretched ( RectTransform rt , float left , float top , float right , float bottom )
        {
            rt.anchorMin = new Vector2 ( 0 , 0 );
            rt.anchorMax = new Vector2 ( 1 , 1 );
            rt.pivot = new Vector2 ( 0.5f , 0.5f );
            rt.sizeDelta = Vector2.zero; // necessary..? found in DefaultControls.cs
            rt.offsetMin = new Vector2 ( left , bottom );
            rt.offsetMax = new Vector2 ( -right , -top );
        }

        public static void top_left ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 0 , 1 , pos_x , pos_y , width , height );
        public static void top_center ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 0.5f , 1 , pos_x , pos_y , width , height );
        public static void top_right ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 1f , 1 , pos_x , pos_y , width , height );
        public static void middle_left ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 0 , 0.5f , pos_x , pos_y , width , height );
        public static void middle_center ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 0.5f , 0.5f , pos_x , pos_y , width , height );
        public static void middle_right ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 1 , 0.5f , pos_x , pos_y , width , height );
        public static void bottom_left ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 0 , 0 , pos_x , pos_y , width , height );
        public static void bottom_center ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 0.5f , 0 , pos_x , pos_y , width , height );
        public static void bottom_right ( this RectTransform rt , float pos_x , float pos_y , float width , float height ) => setStd ( rt , 1 , 0 , pos_x , pos_y , width , height );
        public static void stretch_left ( this RectTransform rt , float pos_x , float top , float width , float bottom ) => setStretchedY ( rt , 0 , pos_x , top , width , bottom );
        public static void stretch_center ( this RectTransform rt , float pos_x , float top , float width , float bottom ) => setStretchedY ( rt , 0.5f , pos_x , top , width , bottom );
        public static void stretch_right ( this RectTransform rt , float pos_x , float top , float width , float bottom ) => setStretchedY ( rt , 1 , pos_x , top , width , bottom );
        public static void top_stretch ( this RectTransform rt , float left , float pos_y , float right , float height ) => setStretchedX ( rt , 1 , left , pos_y , right , height );
        public static void middle_stretch ( this RectTransform rt , float left , float pos_y , float right , float height ) => setStretchedX ( rt , 0.5f , left , pos_y , right , height );
        public static void bottom_stretch ( this RectTransform rt , float left , float pos_y , float right , float height ) => setStretchedX ( rt , 0 , left , pos_y , right , height );
        public static void stretch ( this RectTransform rt , float left , float top , float right , float bottom ) => setStretched ( rt , left , top , right , bottom );
        public static void stretch ( this RectTransform rt , float offset ) => setStretched ( rt , offset , offset , offset , offset );

        /*
            top_left       ( float pos_x , float pos_y , float width , float height )
            top_center     ( float pos_x , float pos_y , float width , float height )
            top_right      ( float pos_x , float pos_y , float width , float height )
            middle_left    ( float pos_x , float pos_y , float width , float height )
            middle_center  ( float pos_x , float pos_y , float width , float height )
            middle_right   ( float pos_x , float pos_y , float width , float height )
            bottom_left    ( float pos_x , float pos_y , float width , float height )
            bottom_center  ( float pos_x , float pos_y , float width , float height )
            bottom_right   ( float pos_x , float pos_y , float width , float height )

            stretch_left   ( float pos_x , float top   , float width , float bottom )
            stretch_center ( float pos_x , float top   , float width , float bottom )
            stretch_right  ( float pos_x , float top   , float width , float bottom )

            top_stretch    ( float left  , float pos_y , float right , float height )
            middle_stretch ( float left  , float pos_y , float right , float height )
            bottom_stretch ( float left  , float pos_y , float right , float height )

            stretch        ( float left  , float top   , float right , float bottom )

    }


    public static class TransformExtension
    {
        private static readonly string TAG = "Rudis TransformExtension";

        public static void Reset ( this Transform transform )
        {
            transform.localPosition = Vector3.zero;
            //transform.localRotation = Quaternion.Euler ( Vector3.zero );
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        public static void MakePosition ( this Transform transform , Vector3 pos )
        {
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            transform.localPosition = pos;
        }
        public static void MakePosX ( this Transform transform , float pos_x )
        {
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = new Vector3 ( pos_x , 0 , 0 );
        }
        public static void MakePosY ( this Transform transform , float pos_y )
        {
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = new Vector3 ( 0 , pos_y , 0 );
        }
        public static void MakePosZ ( this Transform transform , float pos_z )
        {
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = new Vector3 ( 0 , 0 , pos_z );
        }

        public static void MakeRotX ( this Transform transform , float rot_x )
        {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.Euler ( new Vector3 ( rot_x , 0 , 0 ) );
        }
        public static void MakeRotY ( this Transform transform , float rot_y )
        {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.Euler ( new Vector3 ( 0 , rot_y , 0 ) );
        }
        public static void MakeRotZ ( this Transform transform , float rot_z )
        {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.Euler ( new Vector3 ( 0 , 0 , rot_z ) );
        }
        public static void MakeWorldRotX ( this Transform transform , float rot_x )
        {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.Euler ( new Vector3 ( rot_x , 0 , 0 ) );
        }
        public static void MakeWorldRotY ( this Transform transform , float rot_y )
        {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.Euler ( new Vector3 ( 0 , rot_y , 0 ) );
        }
        public static void MakeWorldRotZ ( this Transform transform , float rot_z )
        {
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.Euler ( new Vector3 ( 0 , 0 , rot_z ) );
        }
        public static void SetRotX ( this Transform transform , float rot_x )
        {
            transform.localRotation = Quaternion.Euler ( new Vector3 ( rot_x , 0 , 0 ) );
        }
        public static void SetRotY ( this Transform transform , float rot_y )
        {
            transform.localRotation = Quaternion.Euler ( new Vector3 ( 0 , rot_y , 0 ) );
        }
        public static void SetRotZ ( this Transform transform , float rot_z )
        {
            transform.localRotation = Quaternion.Euler ( new Vector3 ( 0 , 0 , rot_z ) );
        }

        public static void SetHeight ( this Transform transform , float height )
        {
            //Log.i ( TAG , "SetHeight ()" );

            RectTransform rt = transform.GetComponent<RectTransform> ();
            if ( null == rt )
            {
                //Log.i ( TAG , "no RectTransform!" ) ;
                return;
            }
            //Log.i ( TAG , "SetHeight () - getting sizeDelta" );
            Vector2 Size = rt.sizeDelta;
            //Log.i ( TAG , "SetHeight () - setting sizeDelta" );
            rt.sizeDelta = new Vector2 ( Size.x , height );
            //Log.i ( TAG , "SetHeight () - finished" );
        }
        public static void SetWidth ( this Transform transform , float width )
        {
            RectTransform rt = transform.GetComponent<RectTransform> ();
            Vector2 Size = rt.sizeDelta;
            rt.sizeDelta = new Vector2 ( width , Size.y );
        }
        public static void MakeScale ( this Transform transform , float scale )
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3 ( scale , scale , scale );
        }
        public static void SetScale ( this Transform transform , float scale )
        {
            transform.localScale = new Vector3 ( scale , scale , scale );
        }
    }
    public static class Texture2DExtension
    {
        public static void RoundCorners ( this Texture2D tex , float radius , bool resetalpha = false )
        {
            if ( !tex.isReadable ) return;
            if ( tex.format != TextureFormat.RGBA32 ) return;
            int width = tex.width ;
            int height = tex.height ;
            int numPixels = width * height ;
            NativeArray < Color32 > pixels = tex.GetPixelData < Color32 > ( 0 ) ;
            if ( resetalpha )
            {
                for ( int i = 0 ; i < numPixels ; i++ )
                {
                    var c = pixels [i] ;
                    pixels [i] = new Color32 ( c.r , c.g , c.b , 255 );
                }
            }
            int CornerWidth = Mathf.CeilToInt ( radius ) ;
            float fCornerWidth = CornerWidth ;
            // Corner ul
            {
                int StartX = 0 ;
                int StartY = height - CornerWidth ;
                float CX = radius ;
                float CY = height - 1f - radius ;
            }

        }
        private static void roundCorner ( NativeArray<Color32> pixels , int texwidth , int startX , int startY , int cornerwidth , float radius , float cx , float cy )
        {
            for ( int iy = 0 ; iy < cornerwidth ; iy++ )
            {
                int indexLine = ( iy + startY ) * texwidth ;
                float disty = iy - cy ;
                for ( int ix = 0 ; ix < cornerwidth ; ix++ )
                {
                    int index = indexLine + ix + startX ;
                    float distx = ix - cx ;
                    float dist = Mathf.Sqrt ( disty * disty + distx * distx ) ;
                    float alpha = Mathf.Clamp01 ( 1f - ( dist - radius ) ) ;
                    int ialpha = ( int ) ( alpha * 255 ) ;
                }
            }
        }
    }

    public static class TextureExtension
    {
        public static float GetAspectRatio ( this Texture tex )
        {
            return ( float ) tex.width / tex.height;
        }
        public static Vector2Int GetSize ( this Texture tex )
        {
            return new Vector2Int ( tex.width , tex.height );
        }
    }

    public static class RenderTextureExtension
    {
        //public static Vector2Int GetSize ( this RenderTexture rt )
        //{
        //	return new Vector2Int ( rt.width , rt.height ) ;
        //}
        public static bool Resize ( this RenderTexture rt , Vector2Int size )
        {
            if ( rt.GetSize () == size ) return true;
            rt.Release ();
            rt.width = size.x;
            rt.height = size.y;
            return rt.Create ();
        }
    }

    public static class ResolutionExtension
    {
        public static int NumPixels ( this Resolution r ) => r.width * r.height;
    }

    public static class MaterialExtension
    {
        private static readonly string TAG = "Rudis MaterialExtension" ;

        public static void EnableKeyword ( this Material mat , string keyword , bool enable )
        {
            if ( enable )
            {
                mat.EnableKeyword ( keyword );
                //Log.i ( TAG , "enabling " + keyword ) ;
            }
            else mat.DisableKeyword ( keyword );
        }
    }

    public static class VertexHelperExtension
    {
        private static readonly Vector2 [] uv_standard = { Vector2.zero , Vector2.up , Vector2.one , Vector2.right } ;

        public static void AddQuad ( this VertexHelper vh , int startIndex )
        {
            vh.AddTriangle ( startIndex , startIndex + 1 , startIndex + 2 );
            vh.AddTriangle ( startIndex , startIndex + 2 , startIndex + 3 );
        }
        public static void addRect ( this VertexHelper vh , Rect rect , Color32 col ) => addRect ( vh , rect , col , uv_standard );
        public static void addRect ( this VertexHelper vh , Rect rect , Color32 col , Vector2 [ ] idx )
        {
            int StartIndex = vh.currentVertCount ;
            Vector4 v = new Vector4 ( rect.xMin , rect.yMin , rect.xMax , rect.yMax ) ;
            vh.AddVert ( new Vector3 ( v.x , v.y ) , col , idx [0] );
            vh.AddVert ( new Vector3 ( v.x , v.w ) , col , idx [1] );
            vh.AddVert ( new Vector3 ( v.z , v.w ) , col , idx [2] );
            vh.AddVert ( new Vector3 ( v.z , v.y ) , col , idx [3] );
            vh.AddQuad ( StartIndex );
        }
    }

    public static class EnumExtension
    {
        public static string GetDescription ( this Enum en )
        {
            // https://stackoverflow.com/questions/1415140/can-my-enums-have-friendly-names
            Type type = en.GetType () ;

            MemberInfo [] memInfo = type.GetMember ( en.ToString() ) ;
            if ( memInfo != null && memInfo.Length > 0 )
            {
                object [] attrs = memInfo[0].GetCustomAttributes ( typeof ( DescriptionAttribute ) , false ) ;
                if ( attrs != null && attrs.Length > 0 )
                    return ( ( DescriptionAttribute ) attrs [0] ).Description;
            }
            return en.ToString ();
        }
    }

    public static class StackExt
    {
        // https://stackoverflow.com/questions/20864974/is-it-possible-to-peek-below-the-surface-of-a-stack
        public static T Peek2nd<T> ( this Stack<T> stack ) => stack.Skip ( 1 ).First ();

        public static bool TryPeek2nd<T> ( this Stack<T> stack , out T result )
        {
            if ( stack.Count < 2 )
            {
                result = default ( T );
                return false;
            }
            result = stack.Peek2nd ();
            return true;
        }
    }
    public static class ListExt
    {
        // https://stackoverflow.com/questions/12172162/how-to-insert-item-into-list-in-order
        public static void AddSorted<T> ( this List<T> list , T item ) where T : IComparable<T>
        {
            if ( list.Count == 0 )
            {
                list.Add ( item );
                return;
            }
            if ( list [list.Count - 1].CompareTo ( item ) <= 0 )
            {
                list.Add ( item );
                return;
            }
            if ( list [0].CompareTo ( item ) >= 0 )
            {
                list.Insert ( 0 , item );
                return;
            }
            int index = list.BinarySearch(item);
            if ( index < 0 )
                index = ~index;
            list.Insert ( index , item );
        }
    }
    public static class RawImageExtension
    {
        private static readonly string TAG = "Rudis RawImageExtension";
        public static bool LoadImageScaled ( this RawImage image , string filepath )
        {
            //Log.i ( TAG , "LoadImageScaled() - filepath = " + filepath );
            Texture2D tex = Utils.LoadTexture ( filepath );
            image.texture = tex;
            image.color = ( null != tex ) ? Color.white : Color.black;
            if ( null != tex )
            {
                image.color = Color.white;
                float AspectXtoY = ( float ) tex.width / tex.height;
                if ( AspectXtoY > 1.0f )
                {
                    image.transform.localScale = new Vector3 ( 1.0f , 1.0f / AspectXtoY , 1.0f );
                }
                else
                {
                    image.transform.localScale = new Vector3 ( AspectXtoY , 1.0f , 1.0f );
                }
                //image.transform.localScale 
            }
            else
            {
                Log_Old.e ( TAG , "texture load failed" );
                image.color = Color.black;
            }
            return null != tex;
        }
        public static bool LoadImageResize_Y ( this RawImage image , string filepath )
        {
            //Log.i ( TAG , "LoadImageResize_Y ()"  ) ;
            if ( image == null )
            {
                Log_Old.e ( TAG , "image is null!" );
                return false;
            }
            Texture2D tex = Utils.LoadTexture ( filepath );
            image.texture = tex;
            image.color = ( null != tex ) ? Color.white : Color.black;
            if ( null != tex )
            {
                image.color = Color.white;
                float AspectYtoX = ( float ) tex.height / tex.width;
                Vector2 size = image.GetComponent<RectTransform> ().sizeDelta;
                float sizey = size.x * AspectYtoX;
                size.y = sizey;
                image.GetComponent<RectTransform> ().sizeDelta = size;
            }
            else
            {
                image.color = Color.black;
            }
            return null != tex;
        }

        public static void SetTexture ( this RawImage image , Texture texture )
        {
            if ( null != image ) image.texture = texture;
        }
    }

    public static class ComponentExtension
    {
        public static T getComponentIfZero<T> ( this UnityEngine.Component myComp , ref T comp ) //where T : UnityEngine.Component
        {
            if ( null == comp ) comp = myComp.GetComponent<T> ();
            return comp;
        }
        public static T getComponentInParentIfZero<T> ( this UnityEngine.Component myComp , ref T comp ) //where T : UnityEngine.Component
        {
            if ( null == comp ) comp = myComp.GetComponentInParent<T> ();
            return comp;
        }
        public static T getComponentInChildrenIfZero<T> ( this UnityEngine.Component myComp , ref T comp )// where T : UnityEngine.Component
        {
            if ( null == comp ) comp = myComp.GetComponentInChildren<T> ();
            return comp;
        }
        public static T [ ] getComponentsInChildrenIfZero<T> ( this UnityEngine.Component myComp , ref T [ ] comp , bool includeInactive = false ) //where T : UnityEngine.Component
        {
            if ( null == comp ) comp = myComp.GetComponentsInChildren<T> ( includeInactive ) ;
            return Utils.hasData ( comp ) ? comp : null;
        }
    }
    
    public static class Log
    {
        //private static void i ( string tag , string msg ) { }
        //private static void e ( string tag , string msg ) { }
        //private static void w ( string tag , string msg ) { }

        //public static void i ( string tag , string msg ) { }
        //public static void e ( string tag , string msg ) { }
        //public static void w ( string tag , string msg ) { }

        private static string objInfo ( GameObject obj )
        {
            if ( null == obj ) return string.Empty ;
            if ( null != obj.transform.parent )
            {
                return $" - { obj.name } id: {obj.GetInstanceID ()} parent: { obj.transform.parent.gameObject.name }";
            }
            else
            {
                return $" - { obj.name } id: {obj.GetInstanceID ()}";
            }
        }

        private const string LOG_SEPARATOR_DASH  = " - " ;
        private const string LOG_SEPARATOR_COLON = " : " ;
        private const string LOG_FROM            = " from " ;
        private const string LOG_SPACE           = " " ;

#if !RudisApp
        [ System.Diagnostics.Conditional ( "UNITY_EDITOR" ) ]
#endif
        public static void i ( string tag , string msg )
        {
            Debug.Log ( string.Concat ( Time.frameCount.ToString () , LOG_SPACE , tag , LOG_SEPARATOR_COLON , msg ) );
        }

#if !RudisApp
        [System.Diagnostics.Conditional ( "UNITY_EDITOR" ) ]
#endif
        public static void i ( string tag , GameObject obj , string msg )
        {
            Debug.Log ( string.Concat ( Time.frameCount.ToString () , LOG_SPACE , tag , LOG_SEPARATOR_COLON , msg , objInfo ( obj ) ) );
        }

        //public static void i ( string tag , string msg ) { Debug.Log ( $"{ Time.frameCount } { tag } : { msg }" ); }
        ////public static void i ( string tag , GameObject obj , string msg ) { Debug.Log ( tag + " : " + msg + " - " + obj.name + " from " + obj.transform.parent.gameObject.name ); }
        //public static void i ( string tag , GameObject obj , string msg ) { Debug.Log ( $"{ Time.frameCount } { tag } : { msg }{ objInfo ( obj ) }" ); }
        public static void e ( string tag , string msg ) { UnityEngine.Debug.LogError ( tag + " : " + msg ); }
        public static void e ( string tag , GameObject obj , string msg ) { Debug.LogError ( $"{ Time.frameCount } { tag } : { msg }{ objInfo ( obj ) }" ); }



        [ System.Diagnostics.Conditional ( "UNITY_EDITOR" ) ]
        public static void u ( string tag , string msg ) { Debug.Log ( $"{ Time.frameCount } { tag } : { msg }" ); }

        [System.Diagnostics.Conditional ( "UNITY_EDITOR" )]
        public static void u ( string tag , GameObject obj , string msg ) { Debug.Log ( $"{ Time.frameCount } { tag } : { msg } - { obj.name } from { obj.transform.parent.gameObject.name }" ); }

        [System.Diagnostics.Conditional ( "UNITY_EDITOR" )]
        public static void er ( string tag , string msg ) { Debug.LogWarning ( tag + " : " + msg ); }

        public static void w ( string tag , string msg ) { Debug.LogWarning ( tag + " : " + msg ); }
    }

    // */

    sealed class Ref<T>
    {
        // https://stackoverflow.com/questions/2980463/how-do-i-assign-by-reference-to-a-class-field-in-c
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        public Ref ( Func<T> getter , Action<T> setter )
        {
            this.getter = getter;
            this.setter = setter;
        }
        public T Value { get { return getter (); } set { setter ( value ); } }
    }

    sealed class Wrapper<T> where T : class
    {
        public T Value { get; set; }
    }

    public sealed class Holder<T> where T : class
    {
        public T Value = null ;
        public void destroy () => Value = null;
        public Holder () { }
        public Holder ( T val ) => Value = val;
        public bool hasObject () => null != Value;
    }

    public static class Holder
    {
        public static T get<T> ( Holder<T> obj ) where T : class
        {
            return obj?.Value ?? null;
        }
        public static void set<T> ( Holder<T> obj , T val ) where T : class
        {
            if ( null != obj ) obj.Value = val;
        }
    }

    public static class Utils
    {
        private static readonly string TAG = "RudisUtils";

        static public void DestroyObject ( UnityEngine.Object obj )
        {
            if ( obj == null ) return;

#if UNITY_EDITOR
            if ( !Application.isPlaying )
            {
                UnityEngine.Object.DestroyImmediate ( obj );
                return;
            }
#endif
            if ( obj is GameObject go )
            {
                go.transform.SetParent ( null );
            }
            else if ( obj is MonoBehaviour mb && mb.gameObject != null )
            {
                mb.gameObject.transform.SetParent ( null );
                obj = mb.gameObject;
            }

            UnityEngine.Object.Destroy ( obj );
        }

        static public void DestroyComponent<T> ( T comp ) where T : UnityEngine.Component
        {
            if ( comp == null ) return;

#if UNITY_EDITOR
            if ( !Application.isPlaying ) UnityEngine.Object.DestroyImmediate ( comp );
            else
#endif
            {
                UnityEngine.Object.Destroy ( comp );
            }
        }


        static public void DestroyObjectAndZero<T> ( ref T obj ) where T : UnityEngine.Object
        {
            DestroyObject ( obj );
            obj = null;
        }

        static public void DestroyComponentAndZero<T> ( ref T comp ) where T : UnityEngine.Component
        {
            DestroyComponent ( comp );
            comp = null;
        }

        static public T loadPrefab<T> ( string name ) where T : MonoBehaviour
        {
            var FullName = "ModuleResources/" + name ;
            var Result = Resources.Load < T > ( FullName ) ;
            if ( null == Result ) Log.e ( TAG , "couldn't load prefab: " + FullName );
            return Result;
        }

        public static T loadPrefabIfZero<T> ( ref T prefab , string name ) where T : MonoBehaviour
        {
            if ( null == prefab )
            {
                prefab = loadPrefab<T> ( name );
            }
            return prefab;
        }

        static public T instantiate<T> ( string name ) where T : MonoBehaviour
        {
            var prefab = loadPrefab < T > ( name ) ;
            return null != prefab ? UnityEngine.Object.Instantiate ( prefab ) : null;
        }

        static public T instantiateFull<T> ( T prefab , Transform parent = null ) where T : MonoBehaviour
        {
            if ( null == prefab ) return null;
            var rootCanvas = findRootCanvas () ;
            if ( !rootCanvas ) return nullReturn<T> ( "couldn't find root canvas" );
            var Result = UnityEngine.Object.Instantiate ( prefab ) ;
            if ( null == Result ) return null;
            var objRect = Result.transform as RectTransform ;
            if ( null == objRect )
            {
                DestroyObject ( Result );
                return null;
            }
            if ( null == parent ) parent = rootCanvas.transform;
            objRect.SetParent ( parent , false );
            objRect.SetFull ();
            return Result;
        }

        static public T instantiateFull<T> ( string name , Transform parent = null ) where T : MonoBehaviour
        {
            return instantiateFull ( loadPrefab<T> ( name ) , parent );
        }

        public static T createIfZero<T> ( ref T obj , Func<T> create )
        {
            if ( null == obj ) obj = create ();
            return obj;
        }

        /// <summary>
        /// Destroy the specified object immediately, unless not in the editor, in which case the regular Destroy is used instead.
        /// </summary>

        static public void DestroyImmediate ( UnityEngine.Object obj )
        {
            if ( obj != null )
            {
                if ( Application.isEditor ) UnityEngine.Object.DestroyImmediate ( obj );
                else UnityEngine.Object.Destroy ( obj );
            }
        }


        public static bool isEditorAboutToPlay ()
        {
#if UNITY_EDITOR
            if ( Application.isPlaying ) return false;
            if ( EditorApplication.isPlayingOrWillChangePlaymode ) return true;
#endif
            return false;
        }

        private static bool isInPrefabStage ( MonoBehaviour mono )
        {
#if UNITY_EDITOR
            var stage = PrefabStageUtility.GetPrefabStage ( mono.gameObject ) ;
            return null != stage;
#else
            return false ;
#endif
        }

        private static bool IsPartOfPrefabAsset ( MonoBehaviour mono )
        {
#if UNITY_EDITOR
            return PrefabUtility.IsPartOfPrefabAsset ( mono );
#else
            return false ;
#endif
        }

        public static bool hasData<T> ( T [ ] arr )
        {
            if ( null == arr ) return false;
            return arr.Length > 0;
        }

        public static bool hasData<T> ( List<T> list )
        {
            if ( null == list ) return false;
            return list.Count > 0;
        }

        public static int getNumElements<T> ( T [ ] arr )
        {
            if ( null == arr ) return 0;
            return arr.Length;
        }
        public static int getNumElements<T> ( List<T> list )
        {
            if ( null == list ) return 0;
            return list.Count;
        }


        public static Material createMaterial ( string shaderName )
        {
            //Log.i ( TAG , "trying to find shader: " + shaderName ) ;
            var shader = Shader.Find ( shaderName ) ;
            if ( null == shader )
            {
                Log.i ( TAG , "Couldn't find shader: " + shaderName );
                return null;
            }
            //Log.i ( TAG , "found shader: " + shaderName );
            var mat = new Material ( shader ) ;
            mat.hideFlags = HideFlags.HideAndDontSave;
            return mat;
        }
        public static Material createMaterial ( Shader shader )
        {
            if ( null == shader )
            {
                Log.i ( TAG , "shader is zero" );
                return null;
            }
            //Log.i ( TAG , "found shader: " + shader.name );
            var mat = new Material ( shader ) ;
            mat.hideFlags = HideFlags.HideAndDontSave;
            return mat;
        }

        public static T GetRootObject<T> ( bool includeinactive = false ) where T : Behaviour
        {
            var scene = SceneManager.GetActiveScene () ;
            if ( null == scene )
            {
                Log.i ( TAG , "scene is zero" );
                return null;
            }

            if ( !scene.isLoaded )
            {
                //Log.i ( TAG , "scene not loaded" ) ;
                return null;
            }

            T Result = null ;
            var list = ListPool < GameObject > . Get () ;
            scene.GetRootGameObjects ( list );
            if ( list.Count > 0 )
            {
                for ( var i = 0 ; i < list.Count ; ++i )
                {
                    var sp = list [ i ].GetComponent < T > () ;
                    if ( null == sp ) continue;
                    if ( !includeinactive && !sp.isActiveAndEnabled ) continue;
                    Result = sp;
                    break;
                }
            }
            ListPool<GameObject>.Release ( list );
            return Result;
        }
        private static bool falseReturn ( string msg )
        {
            Log.i ( TAG , msg );
            return false;
        }

        private static void voidReturn ( string msg )
        {
            Log.i ( TAG , msg );
        }

        private static T nullReturn<T> ( string msg ) where T : class
        {
            Log.i ( TAG , msg );
            return null;
        }

        public static Canvas findRootCanvas ()
        {
            var Objects = UnityEngine.Object.FindObjectsByType < Canvas > ( FindObjectsInactive.Exclude , FindObjectsSortMode.None ) ;
            if ( null == Objects ) return nullReturn<Canvas> ( "no canvas objects found" );
            foreach ( var cv in Objects )
            {
                if ( !cv.isActiveAndEnabled ) continue;
                if ( !cv.isRootCanvas ) continue;
                return cv;
            }
            return null;
        }

        private static T [ ] FindObjectsByType<T> ( FindObjectsInactive exclude , FindObjectsSortMode none )
        {
            throw new NotImplementedException ();
        }

        public static bool isPlaying ()
        {
#if UNITY_EDITOR
            return Application.isPlaying;
#else
            return true ;
#endif
        }
        //public static long GpsNanosToUtcMillis ( long gps_nanos )
        //{
        //	const long OFFSET_SEC_UNIX_TO_GPS = 315964800L;
        //	const long OFFSET_MILLIS_UNIX_TO_GPS = OFFSET_SEC_UNIX_TO_GPS * 1000L ;
        //	const long NANOS_IN_MILLI = 1000000L ;
        //	const long LEAP_SECONDS = 18L ;
        //	const long LEAP_MILLIS = LEAP_SECONDS * 1000L ;
        //	long UtcMillis = gps_nanos / NANOS_IN_MILLI + OFFSET_MILLIS_UNIX_TO_GPS - LEAP_MILLIS ;
        //	return UtcMillis;
        //}
        //public static long getFileTimeMillis ( string filepath ) { return new DateTimeOffset ( File.GetLastWriteTimeUtc ( filepath ) ).ToUnixTimeMilliseconds (); }

        //public static ulong KnutHash ( string s )
        //{
        //	// https://stackoverflow.com/questions/9545619/a-fast-hash-function-for-string-in-c-sharp
        //	ulong hashedValue = 3074457345618258791ul ;
        //	foreach ( char c in s )
        //	{
        //		hashedValue += c;
        //		hashedValue *= 3074457345618258799ul;
        //	}
        //	return hashedValue ;
        //}
        public static ulong KnutHash ( ReadOnlySpan<char> s )
        {
            // https://stackoverflow.com/questions/9545619/a-fast-hash-function-for-string-in-c-sharp
            var hashedValue = 3074457345618258791ul;
            foreach ( var c in s )
            {
                hashedValue += c;
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        public static ulong KnutHashInHyphens ( ReadOnlySpan<char> s )
        {
            var h1 = s.IndexOf ( '\"' ) ;
            if ( h1 < 0 ) return 0;
            var s1 = s [ ( h1 + 1 ).. ] ;
            var h2 = s1.IndexOf ( '\"' ) ;
            if ( h2 < 0 ) return 0UL;
            return KnutHash ( s1.Slice ( 0 , h2 ) );
        }
        public static (ulong hash, int next_index) KnutHashToNextHyphen ( ReadOnlySpan<char> s )
        {
            //int h = s.IndexOf ( '\"' ) ;
            //if ( h < 0 ) return ( ~0UL , 0 ) ;
            //return ( KnutHash ( s.Slice ( 0 , h -1 ) ) , h + 1 ) ;
            var hashedValue = 3074457345618258791ul;
            var backslash = false;
            for ( var i = 0 ; i < s.Length ; i++ )
            {
                var c = s [ i ];
                if ( !backslash && c == '\"' ) return (hashedValue, i + 1);
                backslash = c == '\\';
                hashedValue += c;
                hashedValue *= 3074457345618258799ul;
            }
            return (0, 0);
        }

        public static ulong KnutHash ( string s )
        {
            return KnutHash ( s.AsSpan () );
        }

        public static bool isNum ( char c )
        {
            if ( c < '0' ) return false;
            if ( c > '9' ) return false;
            return true;
        }

        public static int readTrailingInt ( ReadOnlySpan<char> s , int notfoundval , bool skipfileextension = false )
        {
            var index = s.Length ;
            var Result = 0 ;
            var Factor = 1 ;
            var found = false ;
            if ( skipfileextension )
            {
                while ( --index >= 0 )
                {
                    var c = s [ index ] ;
                    if ( c == '.' ) break;
                }
            }
            while ( --index >= 0 )
            {
                var c = s [ index ] ;
                if ( !isNum ( c ) ) break;
                Result += Factor * ( c - '0' );
                Factor *= 10;
                found = true;
            }
            return found ? Result : notfoundval;
        }

        public static int readTrailingInt ( string s , int notfoundval , bool skipfileextension = false )
        {
            return readTrailingInt ( s.AsSpan () , notfoundval , skipfileextension );
        }

        public static bool hasTrailingInt ( ReadOnlySpan<char> s , bool skipfileextension = false )
        {
            var index = s.Length ;
            if ( skipfileextension )
            {
                while ( --index >= 0 )
                {
                    var c = s [ index ] ;
                    if ( c == '.' ) break;
                }
            }
            if ( index < 1 ) return false;
            return isNum ( s [index - 1] );
        }

        public static bool hasTrailingInt ( string s , bool skipfileextension = false )
        {
            return hasTrailingInt ( s.AsSpan () , skipfileextension );
        }


        public static ulong Get64BitHash ( string s )
        {
            // https://stackoverflow.com/questions/31464894/better-64-bit-byte-array-hash
            const ulong p = 1099511628211UL;
            var hash = 14695981039346656037UL;
            foreach ( var c in s )
            {
                hash ^= c;
                hash *= p;
            }
            return hash;
        }

        public static void deleteFile ( string filepath )
        {
            if ( File.Exists ( filepath ) )
            {
                try
                {
                    File.Delete ( filepath );
                }
                catch ( Exception )
                {
                    return;
                }
            }
        }
        public static string GetFilenameWithPostfix ( string filepath , string postfix )
        {
            // https://stackoverflow.com/questions/17184333/change-file-name-of-image-path-in-c-sharp
            var dir = Path.GetDirectoryName ( filepath ) ;
            var name = Path.GetFileNameWithoutExtension ( filepath ) ;
            var ext = Path.GetExtension ( filepath ) ;
            return Path.Combine ( dir , name + postfix + ext );
        }
        public static string GetFilenameWithPostfix ( string filepath , char postfix )
        {
            // https://stackoverflow.com/questions/17184333/change-file-name-of-image-path-in-c-sharp
            var dir = Path.GetDirectoryName ( filepath );
            var name = Path.GetFileNameWithoutExtension ( filepath );
            var ext = Path.GetExtension ( filepath );
            return Path.Combine ( dir , name + postfix + ext );
        }
        public static string GetFilenameWithPrefix ( string filepath , string prefix )
        {
            // https://stackoverflow.com/questions/17184333/change-file-name-of-image-path-in-c-sharp
            var dir = Path.GetDirectoryName ( filepath );
            var name = Path.GetFileNameWithoutExtension ( filepath );
            var ext = Path.GetExtension ( filepath );
            return Path.Combine ( dir , prefix + name + ext );
        }
        public static string GetFilePathWithPrefix ( string filepath , char prefix )
        {
            // https://stackoverflow.com/questions/17184333/change-file-name-of-image-path-in-c-sharp
            var dir = Path.GetDirectoryName ( filepath );
            var name = Path.GetFileNameWithoutExtension ( filepath );
            var ext = Path.GetExtension ( filepath );
            return Path.Combine ( dir , prefix + name + ext );
        }

        public static string GetFilePathWithExtension ( string filepath , string ext )
        {
            var dir = Path.GetDirectoryName ( filepath ) ;
            var name = Path.GetFileNameWithoutExtension ( filepath ) ;
            if ( ext.StartsWith ( '.' ) )
            {
                return Path.Combine ( dir , name + ext );
            }
            else
            {
                return Path.Combine ( dir , name + '.' + ext );
            }
        }

        public static string GetDescription<T1> ( T1 value )
        {
            // https://stackoverflow.com/questions/424366/string-representation-of-an-enum
            var descriptionAttribute = ( DescriptionAttribute ) value.GetType ()
                .GetField ( value.ToString () )
                .GetCustomAttributes ( false )
                .Where ( a => a is DescriptionAttribute )
                .FirstOrDefault ();
            return descriptionAttribute != null ? descriptionAttribute.Description : value.ToString ();

        }

        //public static void emptyLogFolder () => emptyFolder ( getLogFolder () ) ;
        public static string ensureEmptyFolder ( string folderPath ) => emptyFolder ( Directory.CreateDirectory ( folderPath ).FullName );
        public static string emptyFolder ( string folderPath , bool includeSubDirs = true )
        {
            // https://stackoverflow.com/questions/1288718/how-to-delete-all-files-and-folders-in-a-directory
            if ( !Directory.Exists ( folderPath ) ) return folderPath;
            foreach ( var file in Directory.GetFiles ( folderPath ) )
            {
                File.Delete ( file );
            }
            if ( includeSubDirs )
            {
                foreach ( var dir in Directory.GetDirectories ( folderPath ) )
                {
                    deleteFolder ( dir );
                }
            }
            return folderPath;
        }

        public static void deleteFolder ( string folderPath )
        {
            if ( !Directory.Exists ( folderPath ) ) return;
            emptyFolder ( folderPath );
            Directory.Delete ( folderPath );
        }

        public static string getFolderName ( string folderPath )
        {
            // Path.GetDirectoryName ( folder ) doesn't do that! Instead it returns the full pate of parent folder!!
            // https://stackoverflow.com/questions/5229292/get-folder-name-from-full-folder-path
            //             m_ProjectFolder = Path.GetDirectoryName ( filepath ) ;

            if ( isFile ( folderPath ) ) return getFolderName ( Path.GetDirectoryName ( folderPath ) );
            return new DirectoryInfo ( folderPath ).Name;
        }

        private static bool isDirectory ( string path ) => Directory.Exists ( path );
        private static bool isFile ( string path ) => File.Exists ( path );
        public static bool isDirectoryEmpty ( string path )
        {
            if ( !isDirectory ( path ) ) return false;
            if ( hasData ( Directory.GetFiles ( path ) ) ) return false;
            if ( hasData ( Directory.GetDirectories ( path ) ) ) return false;
            return true;
        }

        //private void deleteFile ( string filepath )
        //{
        //    if ( File.Exists ( filepath ) ) File.Delete ( filepath ) ;
        //}

        public static bool hasExtension ( string path , string ext )
        {
            var extension = Path.GetExtension ( path ) ;
            if ( string.IsNullOrEmpty ( extension ) ) return false;
            if ( ext.StartsWith ( '.' ) )
            {
                return ext.Equals ( extension , StringComparison.InvariantCultureIgnoreCase );
            }
            else
            {
                return ext.Equals ( extension [1..] , StringComparison.InvariantCultureIgnoreCase );
            }
        }

        private static bool CreateEmptyFolder ( string path , bool preserveSubFolders = false )
        {
            try
            {
                if ( isFile ( path ) ) return false;
                if ( isDirectory ( path ) )
                {
                    emptyFolder ( path , !preserveSubFolders );
                    return true;
                }
                Directory.CreateDirectory ( path );
                return true;
            }
            catch ( Exception )
            {
                Log.e ( TAG , "Couldn't create directory " + path );
                return false;
            }
        }

        public static void Sort ( FileInfo [ ] fis )
        {
            // https://stackoverflow.com/questions/1199006/how-to-sort-an-array-of-fileinfo
            Array.Sort ( fis , ( f1 , f2 ) => f1.Name.CompareTo ( f2.Name ) );
        }

        public static int MoveDirectoryContent ( string SrcFolder , string DstFolder , bool includeSubDirs = true ) // , bool sort = false 
        {
            var NumFilesMoved = 0 ;
            if ( !isDirectory ( SrcFolder ) ) return 0;
            if ( !CreateEmptyFolder ( DstFolder , !includeSubDirs ) ) return 0;
            DirectoryInfo di = new ( SrcFolder ) ;
            var fis = di.GetFiles () ;
            foreach ( var fi in fis )
            {
                var DstPath = Path.Combine ( DstFolder , fi.Name ) ;
                //Log.i ( TAG , "Moving " + fi.Name ) ;
                if ( File.Exists ( DstPath ) ) File.Delete ( DstPath );
                fi.MoveTo ( DstPath );
            }
            if ( includeSubDirs )
            {
                foreach ( var dir in Directory.GetDirectories ( SrcFolder ) )
                {
                    var FolderName = getFolderName ( dir ) ;
                    var DstPath = Path.Combine ( DstFolder , FolderName ) ;
                    NumFilesMoved += MoveDirectoryContent ( dir , DstPath );
                }
            }
            return NumFilesMoved;
        }

        public static void CopyDirectory ( string sourceDir , string destinationDir , bool recursive )
        {
            // https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
            // Get information about the source directory
            var dir = new DirectoryInfo ( sourceDir ) ;

            // Check if the source directory exists
            if ( !dir.Exists )
                throw new DirectoryNotFoundException ( $"Source directory not found: { dir.FullName }" );

            // Cache directories before we start copying
            var dirs = dir.GetDirectories () ;

            // Create the destination directory
            Directory.CreateDirectory ( destinationDir );

            // Get the files in the source directory and copy to the destination directory
            foreach ( var file in dir.GetFiles () )
            {
                var targetFilePath = Path.Combine ( destinationDir , file.Name ) ;
                file.CopyTo ( targetFilePath );
            }

            // If recursive and copying subdirectories, recursively call this method
            if ( recursive )
            {
                foreach ( var subDir in dirs )
                {
                    var newDestinationDir = Path.Combine ( destinationDir , subDir.Name ) ;
                    CopyDirectory ( subDir.FullName , newDestinationDir , true );
                }
            }
        }

        // classes
        public static bool doesClassExist ( string @namespace , string @class )
        {
            // https://stackoverflow.com/questions/8499593/c-sharp-how-to-check-if-namespace-class-or-method-exists-in-c
            var FullName = string.Empty ;
            if ( string.IsNullOrEmpty ( @namespace ) )
            {
                FullName = @class;
            }
            else
            {
                FullName = string.Format ( "{0}.{1}" , @namespace , @class );
            }
            var myClassType = Type.GetType (FullName ) ;
            return null != myClassType;
        }

        // FrameRates
        public enum FrameRates
        {
            [ Description ( "15 fps" ) ] fps_15 = 15,
            [ Description ( "20 fps" ) ] fps_20 = 20,
            [ Description ( "30 fps" ) ] fps_30 = 30,
            [ Description ( "60 fps" ) ] fps_60 = 60,
            [ Description ( "90 fps" ) ] fps_90 = 90,
            [ Description ( "120 fps" ) ] fps_120 = 120,
            [ Description ( "max fps" ) ] fps_max = 999,
        }
        public static void setFrameRate ( FrameRates framerate ) => setFramerateInFPS ( getFPS ( framerate ) );

#if !UNITY_EDITOR && UNITY_ANDROID
        private static Stack < int > m_UsedFrameRates = new () ;

        [RuntimeInitializeOnLoadMethod ( RuntimeInitializeLoadType.BeforeSceneLoad )]
        private static void recieveNativeFrameRate ()
        {
            //m_NativeFrameRate = ( int ) Screen.currentResolution.refreshRateRatio.value;
            //Log.i ( TAG , $"native framerate = { m_NativeFrameRate } hz" ) ;
            m_UsedFrameRates.Clear () ;
            m_NativeFrameRate = 0 ;
        }
#endif

        private static int m_NativeFrameRate = 0 ;

        public static void pushFrameRate ( FrameRates framerate )
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            int pushing = Application.targetFrameRate ;
            //Log.i ( TAG , $"pushing { pushing }, setting { framerate }" ) ;
            m_UsedFrameRates.Push ( pushing ) ;
            setFrameRate ( framerate ) ;
#endif
        }
        public static void popFrameRate ()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if ( m_UsedFrameRates.Count < 1 ) return ;
            int popped = m_UsedFrameRates.Pop () ;
            //Log.i ( TAG , $"popping { popped } hz" ) ;
            setFramerateInFPS ( popped ) ;
#endif
        }

        //private static int getrate ( RefreshRate rate )

        public static int nativeFramerate // => m_NativeFrameRate ;
        {
            get
            {
                if ( 0 == m_NativeFrameRate )
                {
                    var curres = Screen.currentResolution ;
                    var resolutions = Screen.resolutions ;
                    var MaxFrameRate = 0 ;
                    // Print the resolutions
                    foreach ( var res in resolutions )
                    {
                        if ( res.width != curres.width ) continue;
                        if ( res.height != curres.height ) continue;
                        var FrameRate = ( int ) res.refreshRateRatio.value ;
                        if ( FrameRate > MaxFrameRate ) MaxFrameRate = FrameRate;
                        MaxFrameRate = Mathf.Max ( MaxFrameRate , ( int ) res.refreshRateRatio.value );
                    }
                    m_NativeFrameRate = MaxFrameRate;
                    Log.i ( TAG , "MaxFrameRate = " + MaxFrameRate );
                }
                return m_NativeFrameRate;
            }
        }
        //private static int getNativeFrameRate ()
        //{
        //    int fr = ( int ) Screen.currentResolution.refreshRateRatio.value ;
        //    Log.i ( TAG , $"native framerate = { fr } hz" ) ;
        //    return fr ;
        //}
        //private static int getMaxFrameRate ()
        //{

        //}
        private static int getFPS ( FrameRates framerate )
        {
            if ( FrameRates.fps_max == framerate ) return nativeFramerate;
            return ( int ) framerate;
        }
        private static void setFramerateInFPS ( int fps )
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            //Log.i ( TAG , $"setting { fps } fps" ) ;
            Application.targetFrameRate = fps ;
#endif
        }

        // time
        public static long currentUtcTimeMillis ()
        {
            return LocalDateTimeToUtcTimeMillis ( DateTime.Now );
        }
        public static DateTime TimeMillisToDateTime ( long millis )
        {
            return DateTimeOffset.FromUnixTimeMilliseconds ( millis ).DateTime.ToLocalTime ();
        }
        public static long LocalDateTimeToUtcTimeMillis ( DateTime time )
        {
            return new DateTimeOffset ( time.ToUniversalTime () ).ToUnixTimeMilliseconds ();
        }
        public static void removeSuperfluidAudioListeners ()
        {
            var aL = UnityEngine.Object.FindObjectsOfType<AudioListener> ();
            for ( var i = 0 ; i < aL.Length ; i++ )
            {
                //Ignore the first AudioListener in the array 
                if ( i == 0 )
                    continue;

                //Destroy 
                DestroyObject ( aL [i] );
            }
        }
        public static void destroyAllAudioListeners ()
        {
            var aL = UnityEngine.Object.FindObjectsOfType<AudioListener> ();
            for ( var i = 0 ; i < aL.Length ; i++ )
            {
                //Destroy 
                DestroyObject ( aL [i] );
            }
        }

        //https://answers.unity.com/questions/7776/how-to-make-an-gameobject-invisible-and-disappeare.html
        public static void hide ( GameObject obj )
        {
            if ( null == obj ) return;
            obj.transform.localScale = new Vector3 ( 0 , 1 , 1 );
        }

        public static void moveOut ( GameObject obj )
        {
            if ( null == obj ) return;
            obj.transform.localPosition = new Vector3 ( -5000f , 0 , 0 );
        }


        public static void show ( GameObject obj )
        {
            if ( null == obj ) return;
            obj.transform.localScale = Vector3.one;
        }
        public static string getTimeDurationString ( double dseconds )
        {
            return getTimeDurationString ( Convert.ToInt64 ( dseconds ) );
        }


        public static string getTimeDurationString2 ( long seconds )
        {
            var Result = "";
            var bShowSeconds = true;
            if ( seconds >= 60 )
            {
                var minutes = seconds / 60;
                seconds %= 60;
                if ( minutes >= 60 )
                {
                    var hours = minutes / 60;
                    minutes %= 60;
                    Result = "" + hours + "h ";
                    bShowSeconds = false;
                }
                Result += "" + minutes + "m ";
            }
            if ( bShowSeconds ) Result += "" + seconds + "s";
            return Result;
        }

        static char getChar ( long v ) { return ( char ) ( v + '0' ); }
        public static string getTimeDurationString ( long seconds )
        {
            StringBuilder sb = new ( 32 ) ;
            var bShowSeconds = true;
            if ( seconds >= 60 )
            {
                var minutes = seconds / 60;
                seconds %= 60;
                if ( seconds == 0 ) bShowSeconds = false;
                if ( minutes >= 60 )
                {
                    var hours = minutes / 60;
                    minutes %= 60;

                    if ( hours >= 24 )
                    {
                        var days = hours / 24;
                        hours %= 24;
                        sb.Append ( days );
                        sb.Append ( 'd' );
                        sb.Append ( ' ' );
                    }
                    if ( hours != 0L )
                    {
                        sb.Append ( hours );
                        sb.Append ( 'h' );
                        sb.Append ( ' ' );
                    }
                    //Result = "" + hours + "h ";
                    bShowSeconds = false;
                }
                //Result += "" + minutes + "m ";
                if ( minutes != 0L )
                {
                    sb.Append ( minutes );
                    sb.Append ( 'm' );
                    if ( bShowSeconds ) sb.Append ( ' ' );
                }
            }
            if ( bShowSeconds )
            {
                //Result += "" + seconds + "s";
                sb.Append ( seconds );
                sb.Append ( 's' );
            }
            return sb.ToString ();
        }
        public static Texture2D LoadTexture ( string filePath )
        {
            Texture2D tex = null;
            byte [] fileData;

            if ( File.Exists ( filePath ) )
            {
                fileData = File.ReadAllBytes ( filePath );
                tex = new Texture2D ( 2 , 2 );
                tex.hideFlags = HideFlags.HideAndDontSave;
                if ( !tex.LoadImage ( fileData ) )  // this will auto-resize the texture dimensions.
                {
                    Log.e ( TAG , "couldn't load image" );
                    DestroyObject ( tex );
                    tex = null;
                }
            }
            else
            {
                Log.e ( TAG , "no picture found" );
            }
            return tex;
        }

    }
}
