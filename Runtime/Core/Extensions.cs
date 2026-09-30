using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Rudi.Core;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Rudi.Extensions
{
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
            var num = gameObject.transform.childCount ;
            var Result = new GameObject [ num ] ;
            for ( var i = 0 ; i < num ; i++ )
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

            var transform = gameObject.transform ;
            var childCount = transform.childCount;

            // C++-Blick: Wir laufen rückwärts von der höchsten ID zu 0.
            // Das verhindert jegliche Index-Verschiebung und Endlosschleifen!
            for ( var i = childCount - 1 ; i >= 0 ; i-- )
            {
                var child = transform.GetChild ( i ) ;
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
                    if ( Application.isPlaying ) UnityEngine.Object.Destroy ( child.gameObject );
                    else UnityEngine.Object.DestroyImmediate ( child.gameObject );
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
            if ( Application.isPlaying ) UnityEngine.Object.Destroy ( gameObject );
            else UnityEngine.Object.DestroyImmediate ( gameObject );
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
        public static bool RemoveComponent<T> ( this GameObject obj ) where T : UnityEngine.Component
        {
            var component = obj.GetComponent < T > () ;

            if ( component != null )
            {
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate ( component , true );
#else
                GameObject.Destroy ( component ) ;
#endif
                return true;
            }
            return false;
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
                Core.Log.i ( TAG , "listCount is zero" );
                return null;
            }

            var Result = list [ listCount - 1 ] ;

            for ( var i = 0 ; i < list.Count ; ++i )
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
            var Child = new GameObject ( name , components ) ;
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
            var arr = mono.GetComponentsInChildren < T > ( includeInactive ) ;
            foreach ( var obj in arr )
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
                for ( var i = 0 ; i < list.Count ; ++i )
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
        public static float minVal ( this Vector3 v ) => Mathf.Min ( v.x , Mathf.Min ( v.y , v.z ) ) ;
        public static float maxVal ( this Vector3 v ) => Mathf.Max ( v.x , Mathf.Max ( v.y , v.z ) ) ;
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
        public static Vector2 PivotToCenter ( this RectTransform rt ) => rt.PivotTo ( Vec2Half );
        public static Vector2 PivotToLL ( this RectTransform rt ) => rt.PivotTo ( Vector2.zero );
        public static Vector2 PivotToLR ( this RectTransform rt ) => rt.PivotTo ( Vector2.right );
        public static Vector2 PivotToTR ( this RectTransform rt ) => rt.PivotTo ( Vector2.one );
        public static Vector2 PivotToTL ( this RectTransform rt ) => rt.PivotTo ( Vector2.up );
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
            var apos = new Vector2 ( apos_x , apos_y ) ;
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
        */

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

            var rt = transform.GetComponent<RectTransform> ();
            if ( null == rt )
            {
                //Log.i ( TAG , "no RectTransform!" ) ;
                return;
            }
            //Log.i ( TAG , "SetHeight () - getting sizeDelta" );
            var Size = rt.sizeDelta;
            //Log.i ( TAG , "SetHeight () - setting sizeDelta" );
            rt.sizeDelta = new Vector2 ( Size.x , height );
            //Log.i ( TAG , "SetHeight () - finished" );
        }
        public static void SetWidth ( this Transform transform , float width )
        {
            var rt = transform.GetComponent<RectTransform> ();
            var Size = rt.sizeDelta;
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
            var width = tex.width ;
            var height = tex.height ;
            var numPixels = width * height ;
            var pixels = tex.GetPixelData < Color32 > ( 0 ) ;
            if ( resetalpha )
            {
                for ( var i = 0 ; i < numPixels ; i++ )
                {
                    var c = pixels [i] ;
                    pixels [i] = new Color32 ( c.r , c.g , c.b , 255 );
                }
            }
            var CornerWidth = Mathf.CeilToInt ( radius ) ;
            float fCornerWidth = CornerWidth ;
            // Corner ul
            {
                var StartX = 0 ;
                var StartY = height - CornerWidth ;
                var CX = radius ;
                var CY = height - 1f - radius ;
            }

        }
        private static void roundCorner ( NativeArray<Color32> pixels , int texwidth , int startX , int startY , int cornerwidth , float radius , float cx , float cy )
        {
            for ( var iy = 0 ; iy < cornerwidth ; iy++ )
            {
                var indexLine = ( iy + startY ) * texwidth ;
                var disty = iy - cy ;
                for ( var ix = 0 ; ix < cornerwidth ; ix++ )
                {
                    var index = indexLine + ix + startX ;
                    var distx = ix - cx ;
                    var dist = Mathf.Sqrt ( disty * disty + distx * distx ) ;
                    var alpha = Mathf.Clamp01 ( 1f - ( dist - radius ) ) ;
                    var ialpha = ( int ) ( alpha * 255 ) ;
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
        public static bool Resize ( this RenderTexture rt , Vector2Int size )
        {
            if ( rt.IsCreated () && rt.GetSize () == size ) return true ;
            rt.Release () ;
            rt.width = size.x ;
            rt.height = size.y ;
            return rt.Create () ;
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
        public static void addRect ( this VertexHelper vh , Rect rect , Color32 col ) => vh.addRect ( rect , col , uv_standard );
        public static void addRect ( this VertexHelper vh , Rect rect , Color32 col , Vector2 [ ] idx )
        {
            var StartIndex = vh.currentVertCount ;
            var v = new Vector4 ( rect.xMin , rect.yMin , rect.xMax , rect.yMax ) ;
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
            var type = en.GetType () ;

            var memInfo = type.GetMember ( en.ToString() ) ;
            if ( memInfo != null && memInfo.Length > 0 )
            {
                var attrs = memInfo[0].GetCustomAttributes ( typeof ( DescriptionAttribute ) , false ) ;
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
                result = default;
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
            var index = list.BinarySearch(item);
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
            var tex = Utils.LoadTexture ( filepath );
            image.texture = tex;
            image.color = null != tex ? Color.white : Color.black;
            if ( null != tex )
            {
                image.color = Color.white;
                var AspectXtoY = ( float ) tex.width / tex.height;
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
                Core.Log.e ( TAG , "texture load failed" );
                image.color = Color.black;
            }
            return null != tex;
        }
        public static bool LoadImageResize_Y ( this RawImage image , string filepath )
        {
            //Log.i ( TAG , "LoadImageResize_Y ()"  ) ;
            if ( image == null )
            {
                Core.Log.e ( TAG , "image is null!" );
                return false;
            }
            var tex = Utils.LoadTexture ( filepath );
            image.texture = tex;
            image.color = null != tex ? Color.white : Color.black;
            if ( null != tex )
            {
                image.color = Color.white;
                var AspectYtoX = ( float ) tex.height / tex.width;
                var size = image.GetComponent<RectTransform> ().sizeDelta;
                var sizey = size.x * AspectYtoX;
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
            if ( null == comp ) comp = myComp.GetComponentsInChildren<T> ( includeInactive );
            return Utils.hasData ( comp ) ? comp : null;
        }
    }

}
