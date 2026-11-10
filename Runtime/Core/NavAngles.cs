using Rudi.Core.Utilities;
using UnityEngine;

namespace Rudi.Core
{
    [ System.Serializable ]
    public struct NavAngles
    {
        public static readonly NavAngles zero = default ;

        public float yaw   ; //   0° .. 360° (Heading / Azimut: 0° = N, 90° = O, 180° = S, 270° = W)
        public float pitch ; // -90° .. +90° (Negativ = Blick nach unten, Positiv = Blick nach oben)
        public float roll  ; // -90° .. +90° (Positiv = Rechtsneigung, Negativ = Linksneigung)

        public NavAngles ( float yaw , float pitch , float roll )
        {
            this.yaw   = yaw   ;
            this.pitch = pitch ;
            this.roll  = roll  ;
        }

        public NavAngles ( Vector3 unityEuler )
        {
            this = Quaternion.Euler ( unityEuler );
        }

        public static implicit operator NavAngles ( Quaternion q )
        {
            Vector3 earthRight   = q * Vector3.right ;
            Vector3 earthForward = q * Vector3.forward ;

            // 1. Yaw (0° bis 360°, exakt wie getHeadingOutOfPhoneToEarth in Northing)
            float yawRad = Mathf.Atan2 ( -earthRight.z , earthRight.x ) ;
            float yaw    = ( yawRad * Mathf.Rad2Deg + 360f ) % 360f ;

            // 2. Pitch (Negativ = Blick nach unten, Positiv = Blick nach oben)
            float pitch = Mathf.Asin ( Mathf.Clamp ( earthForward.y , -1f , 1f ) ) * Mathf.Rad2Deg ;

            // 3. Roll (Positiv = Rechtsneigung / rechte Kante unten, Negativ = Linksneigung)
            float roll  = Mathf.Asin ( Mathf.Clamp ( -earthRight.y , -1f , 1f ) ) * Mathf.Rad2Deg ;

            return new NavAngles ( yaw , pitch , roll );
        }

        //public static implicit operator NavAngles ( Vector3 euler ) => new NavAngles ( euler ) ;
        public static implicit operator Quaternion ( NavAngles angles ) => Quaternion.Euler ( 0f , angles.yaw , -angles.roll ) * Quaternion.Euler ( -angles.pitch , 0f , 0f );
        public Vector3 toUnityEuler () => ( ( Quaternion ) this ).eulerAngles;
        public override string ToString () => $"Yaw: {yaw:0.0}°, Pitch: {pitch:0.0}°, Roll: {roll:0.0}°";
    }


    //[System.Serializable]
    //public struct NavAngles
    //{
    //    public static readonly NavAngles zero = default ;

    //    public float yaw   ; // 0° .. 360° (Heading / Azimut)
    //    public float pitch ; // -180° .. +180°
    //    public float roll  ; // -180° .. +180°

    //    public NavAngles ( float yaw , float pitch , float roll )
    //    {
    //        this.yaw   = yaw   ;
    //        this.pitch = pitch ;
    //        this.roll  = roll  ;
    //    }

    //    public NavAngles ( Vector3 unityEuler )
    //    {
    //        this = Quaternion.Euler ( unityEuler );
    //    }
    //    public static implicit operator NavAngles ( Quaternion q )
    //    {
    //        Vector3 earthRight   = q * Vector3.right ;
    //        Vector3 earthForward = q * Vector3.forward ;

    //        // 1. Yaw (0° bis 360°, exakt wie getHeadingOutOfPhoneToEarth in Northing)
    //        float yawRad = Mathf.Atan2 ( -earthRight.z , earthRight.x ) ;
    //        float yaw    = ( yawRad * Mathf.Rad2Deg + 360f ) % 360f ;

    //        // 2. Pitch (Negativ = Blick nach unten, Positiv = Blick nach oben)
    //        float pitch = Mathf.Asin ( Mathf.Clamp ( earthForward.y , -1f , 1f ) ) * Mathf.Rad2Deg ;

    //        // 3. Roll (Positiv = rechte Kante oben / Linksneigung, Negativ = Rechtsneigung)
    //        float roll  = Mathf.Asin ( Mathf.Clamp ( earthRight.y , -1f , 1f ) ) * Mathf.Rad2Deg ;

    //        return new NavAngles ( yaw , pitch , roll );
    //    }
    //    //public static implicit operator NavAngles ( Vector3 euler ) => new NavAngles ( euler ) ;
    //    public static implicit operator Quaternion ( NavAngles angles ) => Quaternion.Euler ( 0f , angles.yaw , angles.roll ) * Quaternion.Euler ( -angles.pitch , 0f , 0f ) ;
    //    public Vector3 toUnityEuler () => ( ( Quaternion ) this ) . eulerAngles ;
    //    public override string ToString () => $"Yaw: {yaw:0.0}°, Pitch: {pitch:0.0}°, Roll: {roll:0.0}°";
}

