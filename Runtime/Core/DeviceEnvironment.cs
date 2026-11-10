using UnityEngine;

namespace Rudi.Core
{
    public static class DeviceEnvironment
    {
        // Deine bestehende Metal/Apple-Prüfung
        public const bool IsAppleMetalPlatform = 
#if UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        true;
#else
        false;
#endif

        // Speziell für Touch-Geräte vs. Desktop (falls macOS und iOS getrennt behandelt werden müssen)
        public const bool IsMobileApple = 
#if UNITY_IOS && !UNITY_EDITOR
        true;
#else
        false;
#endif

        public const bool IsDesktopApple = 
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        true;
#else
        false;
#endif

        public const bool IsInEditor = 
#if UNITY_EDITOR
        true;
#else
        false;
#endif

        public const bool IsMobile =
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR        
        true;
#else
        false;
#endif

        public const bool IL2CPP =
#if ENABLE_IL2CPP
        true ;
#else
        false ;
#endif

    }
}
