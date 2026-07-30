using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Collections.Generic;
using Rudi.Extensions;
using Rudi.UI;

namespace Rudi
{
    ///*
    public static class DefaultControls
    {
#if UNITY_EDITOR

        // default variables - as is in Unity
        private const float  kWidth         = 160f ;
        private const float  kThickHeight   =  30f ;
        private const float  kThinHeight    =  20f ;
        private const float  kDefaultBorderWidth   = 1.1f ;
        //private const float  kLindsayHeight =  28f ;
        private static readonly Vector2 s_ThickElementSize       = new Vector2 ( kWidth , kThickHeight   ) ;
        private static readonly Vector2 s_ThinElementSize        = new Vector2 ( kWidth , kThinHeight    ) ;
        //private static readonly Vector2 s_LindsayElementSize   = new Vector2 ( kWidth , kLindsayHeight ) ;
        private static readonly Vector2 s_ImageElementSize       = new Vector2 ( 100f , 100f ) ;
        private static readonly Color   s_DefaultSelectableColor = new Color32 ( 255 , 255 , 255 , 255 ) ;
        private static readonly Color   s_PanelColor             = new Color32 ( 255 , 255 , 255 , 100 ) ;
        private static readonly Color   s_TextColor              = new Color32 (  50 ,  50 ,  50 , 255 ) ;
        private static readonly Color   s_DefaultBorderColor     = new Color32 (  220 ,  220 ,  223 , 255 ) ;
        // helper functions
        static bool hasSelectionCanvas () => Selection.activeGameObject && Selection.activeGameObject.GetComponentInParent < Canvas > () ;

        private static GameObject CreateUIElementRoot ( string name , Vector2 size , params Type [] components )
        {
            GameObject go = new GameObject ( name , components ) ;
            go.GetComponent < RectTransform > () . sizeDelta = size ;
            return go ;
        }

        private static GameObject CreateUIObject ( string name , GameObject parent , params Type [] components )
        {
            GameObject go = new GameObject ( name , components ) ;
            go.transform.SetParent ( parent.transform , false ) ;
            return go ;
        }

        private static void SetParentAndAlign ( GameObject child , GameObject parent )
        {
            if ( parent == null ) return ;
            child.transform.SetParent ( parent.transform , false ) ;
            SetLayerRecursively ( child , parent.layer ) ;
        }

        private static void SetLayerRecursively ( GameObject go , int layer )
        {
            go.layer = layer;
            Transform t = go.transform;
            for ( int i = 0 ; i < t.childCount ; i++ )
                SetLayerRecursively ( t.GetChild ( i ).gameObject , layer ) ;
        }

        private static Color getGrey ( byte v ) => new Color32 ( v , v , v , 255 ) ;
        private static Color getGrey ( byte v , byte a ) => new Color32 ( v , v , v , a ) ;
        private static Color getGrey ( float v ) => new Color ( v , v , v ) ;
        private static Color getGrey ( float v , float a ) => new Color ( v , v , v , a ) ;
        private static readonly Color MenuSelectedColor = new Color32 ( 230 , 158 , 5 , 255 ) ;
        private static void SetDefaultColorTransitionValues ( Selectable sel )
        {
            ColorBlock colors = sel.colors;
            // as in Unities DefaultControls.cs - but because of a bug, they aren't activated
            //colors.highlightedColor = new Color ( 0.882f , 0.882f , 0.882f ) ; // 225
            //colors.pressedColor     = new Color ( 0.698f , 0.698f , 0.698f ) ; // 178
            //colors.disabledColor    = new Color ( 0.521f , 0.521f , 0.521f ) ; // 133

            // standard unity colors as in selectable ctor
            //colors.normalColor      = Color.white ;
            //colors.highlightedColor = getGrey ( 245 ) ;
            //colors.pressedColor     = getGrey ( 200 ) ;
            //colors.selectedColor    = getGrey ( 245 ) ;
            //colors.disabledColor    = getGrey ( 200, 128 ) ;

            // we want our colors lighter because of focusing to shadow
            colors.normalColor      = getGrey ( 255 ) ;
            colors.highlightedColor = getGrey ( 255 ) ;
            colors.pressedColor     = getGrey ( 245 ) ;
            colors.selectedColor    = getGrey ( 255 ) ;
            colors.disabledColor    = getGrey ( 200, 128 ) ;

            sel.colors = colors ; // this line is missing in Unities DefaultControls.cs
        }

        private static void SetDefaultColorTransitionValues ( GameObject go )
        {
            SetDefaultColorTransitionValues ( go.GetComponent < Selectable > () ) ;
        }

        private static void SetDefaultMenuColorTransitionValues ( Selectable sel )
        {
            ColorBlock colors = sel.colors;
            colors.normalColor      = getGrey ( 255 , 0 ) ;
            colors.highlightedColor = getGrey ( 255 , 0 ) ;
            colors.pressedColor     = MenuSelectedColor ;
            colors.selectedColor    = getGrey ( 255 , 0 ) ;
            colors.disabledColor    = getGrey ( 200, 128 ) ;

            sel.colors = colors ; // this line is missing in Unities DefaultControls.cs
        }
        private static void SetDefaultMenuColorTransitionValues ( GameObject go )
        {
            SetDefaultMenuColorTransitionValues ( go.GetComponent<Selectable> () );
        }

        private static void setDefaultShadowDistances ( SelectableAdds sel )
        {
            var db = sel.distances ;
            db.ShadowNormal      = 1.0f ;
            db.ShadowPressed     = 0.2f ;
            db.ShadowSelected    = 1.0f ;
            db.ShadowHighlighted = 1.0f ;
            db.ShadowDisabled    = 0.0f ;
            sel.distances = db ;
        }

        private static void setDefaultShadowDistances ( GameObject go )
        {
            setDefaultShadowDistances ( go.GetComponent < SelectableAdds > () ) ;
        }

        private static Text SetDefaultTextValues ( GameObject go , string text , TextAnchor alignment , int fontsize = 0 )
        {
            // Set text values we want across UI elements in default controls.
            // Don't set values which are the same as the default values for the Text component,
            // since there's no point in that, and it's good to keep them as consistent as possible.
            Text lbl = go.GetComponent < Text > () ;
            //if ( null == lbl ) return null ;
            lbl.color     = s_TextColor;
            lbl.text      = text ;
            lbl.alignment = alignment ;
            if ( fontsize > 0 ) lbl.fontSize = fontsize ;
            return lbl ;
        }
        private static Text SetDefaultTextValues ( GameObject go , TextAnchor alignment )
        {
            return SetDefaultTextValues ( go , string.Empty , alignment ) ;
        }

        private static RawImageV2 setupBackground ( GameObject go , Color color , bool halfround , float elevation = 0.0f )
        {
            RawImageV2 ri = go.GetComponent < RawImageV2 > () ;
            ri.color = color ;
            if ( halfround ) ri.halfRound = true ;
            if ( elevation > 0.0f )
            {
                ri.showShadow = true ;
                ri.elevation = elevation ;
            }
            return ri ;
        }

        private static RawImageV2 setupBackground ( GameObject go , bool halfround , float shadowdistance = 0.0f )
        {
            return setupBackground ( go , Color.white , halfround , shadowdistance ) ;
        }
        private static RawImageV2 setupBackground ( GameObject go , byte grey , bool halfround , float shadowdistance = 0.0f )
        {
            return setupBackground ( go , getGrey ( grey ) , halfround , shadowdistance ) ;
        }

        public static readonly Vector2 Vec2Half = Vector2.one * 0.5f ;

        public static void SetFull ( GameObject go )
        {
            RectTransform rt = go.GetComponent < RectTransform > () ;
            rt.pivot = Vec2Half ;
            rt.anchorMin = Vector2.zero ;
            rt.anchorMax = Vector2.one ;
            rt.offsetMin = Vector2.zero ;
            rt.offsetMax = Vector2.zero ;
            rt.sizeDelta = Vector2.zero ;
            rt.pivot = Vector2.one * 0.5f ;
            rt.anchoredPosition = Vector2.zero ;
        }

        public static RectTransform SetFull ( GameObject go , float to_left , float to_right , float to_top , float to_bottom )
        {
            RectTransform rt = go.GetComponent < RectTransform > () ;
            rt.pivot = Vec2Half;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2 ( to_left , to_bottom ) ;
            rt.offsetMax = new Vector2 ( -to_right , -to_top ) ;
            return rt ;
        }

        public static void SetLeftCenter ( GameObject go , float posx , float posy , float sizex , float sizey )
        {
            RectTransform rt = go.GetComponent < RectTransform > () ;
            rt.anchorMin = new Vector2 ( 0 , 0.5f ) ;
            rt.anchorMax = new Vector2 ( 0 , 0.5f ) ;
            rt.pivot     = new Vector2 ( 0 , 0.5f ) ;
            rt.anchoredPosition = new Vector2 ( posx , posy ) ;
            rt.sizeDelta        = new Vector2 ( sizex , sizey ) ;
        }

        public static void SetLeftCenter ( GameObject go , float sizex , float sizey )
        {
            SetLeftCenter ( go , 0 , 0 , sizex , sizey ) ;
        }

        public static void SetRightCenter ( GameObject go , float posx , float posy , float sizex , float sizey )
        {
            RectTransform rt = go.GetComponent < RectTransform > () ;
            rt.anchorMin = new Vector2 ( 1 , 0.5f ) ;
            rt.anchorMax = new Vector2 ( 1 , 0.5f ) ;
            rt.pivot     = new Vector2 ( 1 , 0.5f ) ;
            rt.anchoredPosition = new Vector2 ( posx , posy ) ;
            rt.sizeDelta        = new Vector2 ( sizex , sizey ) ;
        }
        public static void SetRightCenter ( GameObject go , float sizex , float sizey )
        {
            SetRightCenter ( go , 0 , 0 , sizex , sizey ) ;
        }

        public static void setRight ( GameObject go , float sizex )
        {
            RectTransform rt = go.GetComponent < RectTransform > () ;
            rt.anchorMin = Vector2.right ;
            rt.anchorMax = Vector2.one ;
            rt.pivot     = Vector2.one ;
            rt.sizeDelta = new Vector2 ( sizex , 0 ) ;
        }
        public static void setTop ( GameObject go , float sizey )
        {
            RectTransform rt = go.GetComponent < RectTransform > () ;
            rt.anchorMin = Vector2.up ;
            rt.anchorMax = Vector2.one ;
            rt.pivot     = new Vector2 ( 0.5f , 1 ) ;
            rt.sizeDelta = new Vector2 ( 0 , sizey ) ;
        }

        private static GameObject getParent ( GameObject go )
        {
            if ( null == go ) return null ;
            if ( null == go.transform.parent ) return null ;
            return go.transform.parent.gameObject ;
        }

        const int Priority = 8 ;

        // menu lines
        const string MenuLineUI = "GameObject/UI (Canvas)/Rudis UI Extensions/" ;

        const string NameSwitch          = "Switch"            ;
        const string NameSwitchShadowed  = "Switch (Shadowed)" ;
        const string NameSwitchLindsay   = "Switch (Lindsay)"  ;
        const string NameSwitchLindsayNL = "Switch (Lindsay No Label)"  ;
        const string NameSwitchLindsayL  = "Switch (Lindsay Label)"  ;
        const string NameShadowedButton  = "Button (Shadowed)" ;
        const string NameRawImageV2      = "RawImageV2"        ;
        const string NameGlass           = "Glass Background"  ;
        const string NameGlassShadowed   = "Glass Background (Shadowed)" ;
        const string NameSafeArea        = "Safe Area"         ;
        const string NameDropDown        = "Dropdown"          ;
        const string NameInputField      = "InputField"        ;
        const string NamScrollbar        = "Scrollbar"         ;

        const int DropdownSlot = 2 ;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // menu

        private static void CreateControl ( MenuCommand menuCommand , GameObject go )
        {
            GameObjectUtility.SetParentAndAlign ( go , menuCommand.context as GameObject ) ;
            Undo.RegisterCreatedObjectUndo ( go , "Create " + go.name ) ;
            Selection.activeObject = go ;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // menu controls

        // Safe Area
        [ MenuItem ( MenuLineUI + NameSafeArea , true , Priority ) ]
        static bool ValidateSafeArea ()
        {
            GameObject go = Selection.activeGameObject ;
            if ( null == go ) return false;
            Canvas canvas = go.GetComponent < Canvas > () ;
            if ( null == canvas ) return false;
            return canvas.isRootCanvas;
        }

        [ MenuItem ( MenuLineUI + NameSafeArea , false , Priority ) ]
        static void MenuCreateSafeArea ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateSafeArea () ) ;


        // RawImageV2
        [ MenuItem ( MenuLineUI + NameRawImageV2 , true  , Priority + 0 ) ] static bool ValiateRawImageV2 () => hasSelectionCanvas () ;
        [ MenuItem ( MenuLineUI + NameRawImageV2 , false , Priority + 0 ) ] static void MenuRawImageV2 ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateRawImageV2 () ) ;

        // switch
        [ MenuItem ( MenuLineUI + NameSwitch , true  , Priority + 1 ) ] static bool ValiateSwitch () => hasSelectionCanvas () ;
        [ MenuItem ( MenuLineUI + NameSwitch , false , Priority + 1 ) ] static void MenuSwitch ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateSwitch () ) ;

        // switch shadowed
        [ MenuItem ( MenuLineUI + NameSwitchShadowed , true  , Priority + 2 ) ] static bool ValiateSwitchSh () => hasSelectionCanvas () ;
        [ MenuItem ( MenuLineUI + NameSwitchShadowed , false , Priority + 2 ) ] static void MenuSwitchSh ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateSwitch ( true ) ) ;

        //// switch Lindsay
        //[ MenuItem ( MenuLineUI + NameSwitchLindsayL , true , Priority + 3 ) ] static bool ValiateSwitchLindsay () => hasSelectionCanvas () ;
        //[ MenuItem ( MenuLineUI + NameSwitchLindsayL , false , Priority + 3 ) ] static void MenuSwitchLindsay ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateSwitchLindsay ( true ) ) ;

        //// switch Lindsay
        //[ MenuItem ( MenuLineUI + NameSwitchLindsayNL , true  , Priority + 4 ) ] static bool ValiateSwitchLindsayNL () => hasSelectionCanvas () ;
        //[ MenuItem ( MenuLineUI + NameSwitchLindsayNL , false , Priority + 4 ) ] static void MenuSwitchLindsayNL ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateSwitchLindsay ( false ) ) ;

        // shadowed button
        [ MenuItem ( MenuLineUI + NameShadowedButton , true  , Priority + 5 ) ] static bool ValiateShadowedButton () => hasSelectionCanvas () ;
        [ MenuItem ( MenuLineUI + NameShadowedButton , false , Priority + 5 ) ] static void MenuShadowedButton ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateShadowedButton () ) ;

        // InputField
        [ MenuItem ( MenuLineUI + NameInputField , true  , Priority + 6 ) ] static bool ValiateInputField () => hasSelectionCanvas () ;
        [ MenuItem ( MenuLineUI + NameInputField , false , Priority + 6 ) ] static void MenuInputField ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateInputField () ) ;

        // Dropdown
        [ MenuItem ( MenuLineUI + NameDropDown , true  , Priority + 7 ) ] static bool ValiateDropDown () => hasSelectionCanvas () ;
        [ MenuItem ( MenuLineUI + NameDropDown , false , Priority + 7 ) ] static void MenuDropDown ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateDropdown () ) ;

        // glass
        [ MenuItem ( MenuLineUI + NameGlass , true  , Priority + 8 ) ] static bool ValiateGlass () => hasSelectionCanvas () ;
        [ MenuItem ( MenuLineUI + NameGlass , false , Priority + 8 ) ] static void MenuCreateGlass ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateGlass () );

        // glass
        [ MenuItem ( MenuLineUI + NameGlassShadowed , true  , Priority + 9 ) ] static bool ValiateGlassSh () => hasSelectionCanvas () ;
        [ MenuItem ( MenuLineUI + NameGlassShadowed , false , Priority + 9 ) ] static void MenuCreateGlassSh ( MenuCommand menuCommand ) => CreateControl ( menuCommand , CreateGlass ( true ) ) ;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // menu controls

        // Scrollbar
        static GameObject CreateScrollbar ()
        {
            GameObject scrollbarRoot = CreateUIElementRoot ( NameDropDown , s_ThinElementSize  , typeof ( Scrollbar ) , typeof ( SelectableAdds ) , typeof ( RawImageV2 ) ) ;
            GameObject sliderArea    = CreateUIObject ( "Sliding Area" , scrollbarRoot , typeof ( RectTransform ) ) ;
            GameObject handle        = CreateUIObject ( "Handle" , sliderArea , typeof ( RawImageV2 ) ) ;

            var ri = setupBackground ( scrollbarRoot , 214 , true ) ;
            ri.radii = new Vector4 ( 0 , 0 , 1 , 1 ) ;
            ri.enabled = false ;

            SetFull ( sliderArea , 12 , 12 , 12 , 12 ) ;
            var handleRect = SetFull ( handle , -9 , -9 , -9 , -9 ) ;

            var handleImage = setupBackground ( handle , 230 , true , 2 ) ;

            Scrollbar scrollbar = scrollbarRoot.GetComponent < Scrollbar > () ;
            scrollbar.handleRect = handleRect ;
            scrollbar.targetGraphic = handleImage ;
            SetDefaultColorTransitionValues ( scrollbar ) ;

            //var sel = scrollbarRoot.GetComponent < SelectableAdds > () ;
            setDefaultShadowDistances ( scrollbarRoot.GetComponent<SelectableAdds> () ) ;

            return scrollbarRoot ;
        }

        // InputField
        static GameObject CreateInputField ()
        {
            GameObject root = CreateUIElementRoot ( NameInputField , s_ThickElementSize , typeof ( InputFieldEnhancer ) , typeof ( RawImageV2 ) ) ;

            GameObject childPlaceholder = CreateUIObject ( "Placeholder" , root , typeof ( Text ) ) ;
            GameObject childText        = CreateUIObject ( "Text"        , root , typeof ( Text ) ) ;

            InputField inputField = root.GetComponent < InputField > () ;
            SetDefaultColorTransitionValues ( inputField ) ;

            var ri = root.GetComponent < RawImageV2 > () ;
            ri.radius = 7 ;
            //ri.drarBorder = true ;
            ri.borderColor = s_DefaultBorderColor ;
            ri.borderWidth = kDefaultBorderWidth ;

            Text text = SetDefaultTextValues ( childText , string.Empty , TextAnchor.MiddleLeft ) ;
            text.supportRichText = false;

            Text placeholder = SetDefaultTextValues ( childPlaceholder , "Enter text..." , TextAnchor.MiddleLeft ) ;
            placeholder.fontStyle = FontStyle.Italic;

            // Make placeholder color half as opaque as normal text color.
            Color placeholderColor = text.color ;
            placeholderColor.a *= 0.5f ;
            placeholder.color = placeholderColor ;

            childText        . GetComponent < RectTransform > ().stretch ( 10 , 6 , 10 , 6 ) ;
            childPlaceholder . GetComponent < RectTransform > ().stretch ( 10 , 6 , 10 , 6 ) ;

            inputField.textComponent = text ;
            inputField.placeholder   = placeholder ;

            return root ;
        }

        // Dropdown
        static GameObject CreateDropdown ()
        {
            GameObject root = CreateUIElementRoot ( NameDropDown , s_ThickElementSize , typeof ( RawImageV2 ) , typeof ( Dropdown ) , typeof ( SelectableAdds ) ) ;

            GameObject label          = CreateUIObject ( "Label"           , root     , typeof ( Text  ) ) ;
            GameObject arrow          = CreateUIObject ( "Arrow"           , root     , typeof ( Arrow_Icon ) , typeof ( SoftShadow ) ) ;
            GameObject template       = CreateUIObject ( "Template"        , root     , typeof ( ScrollRect ) , typeof ( RawImageV2 ) ) ;
            GameObject viewport       = CreateUIObject ( "Viewport"        , template , typeof ( RawImageV2 ) , typeof ( Mask ) , typeof ( ScrollRectViewPortHandler ) ) ;
            GameObject content        = CreateUIObject ( "Content"         , viewport , typeof ( RectTransform ) ) ;
            GameObject item           = CreateUIObject ( "Item"            , content  , typeof ( Toggle     ) ) ;
            GameObject itemBackground = CreateUIObject ( "Item Background" , item     , typeof ( RawImageV2 ) ) ;
            GameObject itemCheckmark  = CreateUIObject ( "Item Checkmark"  , item     , typeof ( OK_Icon    ) ) ;
            GameObject itemLabel      = CreateUIObject ( "Item Label"      , item     , typeof ( Text       ) ) ;

            // Sub controls.

            GameObject scrollbar = CreateScrollbar () ;
            scrollbar.name = "Scrollbar";
            SetParentAndAlign ( scrollbar , template ) ;
            var scrollbarScrollbar = scrollbar.GetComponent < Scrollbar > () ;
            scrollbarScrollbar.SetDirection ( Scrollbar.Direction.BottomToTop , true ) ;
            setRight ( scrollbar , 24 ) ;

            // Setup item UI components.
            var itemBackgroundImage = setupBackground ( itemBackground , 255 , false ) ;
            var itemLabelText = SetDefaultTextValues ( itemLabel , string.Empty , TextAnchor.MiddleLeft ) ;
            var ok_icon = itemCheckmark.GetComponent < OK_Icon > () ; //.slot = DropdownSlot;
            ok_icon.lineWidth = 1.5f   ; // 1.2f
            ok_icon.scale     = 0.65f ;
            itemCheckmark.GetComponent < Graphic > () . color = s_TextColor ;

            SetDefaultMenuColorTransitionValues ( item ) ;
            Toggle itemToggle = item.GetComponent < Toggle > () ;
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.graphic = itemCheckmark.GetComponent < Graphic > () ;
            itemToggle.isOn = true;

            // Setup template UI components.
            var templateImage = setupBackground ( template , 255 , false , 8 ) ;
            templateImage.radius = 12 ;

            ScrollRect templateScrollRect = template.GetComponent < ScrollRect > () ;
            templateScrollRect.content  = content.GetComponent < RectTransform > () ;
            templateScrollRect.viewport = viewport.GetComponent < RectTransform > () ;
            templateScrollRect.horizontal = false ;
            templateScrollRect.movementType = ScrollRect.MovementType.Clamped ;
            templateScrollRect.verticalScrollbar = scrollbarScrollbar ;
            templateScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport ;
            templateScrollRect.verticalScrollbarSpacing = 0 ;

            Mask scrollRectMask = viewport.GetComponent < Mask > () ;
            scrollRectMask.showMaskGraphic = false ;

            var viewportImage = viewport.GetComponent < RawImageV2 > () ;
            viewportImage.radius = 12 ;
            //viewportImage.radii = new Vector4 ( 1 , 1 , 0 , 0 ) ;
            viewportImage.color = Color.white ;

            // Setup dropdown UI components.
            Text labelText = SetDefaultTextValues ( label , TextAnchor.MiddleLeft ) ;

            var arrowImage = arrow.GetComponent < Arrow_Icon > () ;
            arrowImage.direction = Arrow_Icon.Direction.Down ;
            arrowImage.lineWidth = 1.3f ;
            arrowImage.scale     = 0.5f ;
            arrow.GetComponent < Graphic > () . color = s_TextColor ;
            arrow.GetComponent < SoftShadow > () . elevation  = 1.0f ;
            arrow.GetComponent < SoftShadow > () . showShadow = true ;
            //arrow.GetComponent < Arrow_Icon > () . direction = Arrow_Icon.Direction.Down ;

            var backgroundImage = setupBackground ( root , 255 , true , 3 ) ;

            Dropdown dropdown = root.GetComponent<Dropdown>();
            dropdown.targetGraphic = backgroundImage;
            SetDefaultColorTransitionValues ( dropdown );
            dropdown.template = template.GetComponent<RectTransform> ();
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText ;

            // Setting default Item list.
            itemLabelText.text = "Option A";
            dropdown.options.Add ( new Dropdown.OptionData { text = "Option A" } );
            dropdown.options.Add ( new Dropdown.OptionData { text = "Option B" } );
            dropdown.options.Add ( new Dropdown.OptionData { text = "Option C" } );
            dropdown.RefreshShownValue () ;

            // Set up RectTransforms.
            SetFull ( label , 13 , 25 , 2 , 2 ) ;
            SetRightCenter ( arrow , -5 , 0 , 20 , 20 ) ;

            RectTransform templateRT = template.GetComponent < RectTransform > () ;
            templateRT.anchorMin        = new Vector2 ( 0 , 0 ) ;
            templateRT.anchorMax        = new Vector2 ( 1 , 0 ) ;
            templateRT.pivot            = new Vector2 ( 0.5f , 1 ) ;
            templateRT.anchoredPosition = new Vector2 ( 0 , 2 ) ;
            templateRT.sizeDelta        = new Vector2 ( 0 , 150 ) ;

            RectTransform viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = new Vector2 ( 0 , 0 ) ;
            viewportRT.anchorMax = new Vector2 ( 1 , 1 ) ;
            viewportRT.sizeDelta = new Vector2 ( -24 , 0 ) ;
            viewportRT.pivot     = new Vector2 ( 0 , 1 ) ;

            setTop ( content , 40 ) ;

            RectTransform itemRT = item.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2 ( 0 , 0.5f );
            itemRT.anchorMax = new Vector2 ( 1 , 0.5f );
            itemRT.sizeDelta = new Vector2 ( 0 , 32 );

            SetFull ( itemBackground ) ;
            SetLeftCenter ( itemCheckmark , 1.7f , 0 , 17 , 17 ) ;
            SetFull ( itemLabel , 20 , 10 , 2 , 1 ) ;

            template.SetActive ( false ) ;

            arrow.GetComponent < Arrow_Icon > () . slot = DropdownSlot ;
            itemCheckmark.GetComponent < OK_Icon > () . slot = DropdownSlot ;
            return root ;
        }

        // Safe Area
        static GameObject CreateSafeArea () => CreateUIElementRoot ( NameSafeArea , s_ImageElementSize , typeof ( UiSafeArea ) ) ;

        // Glass
        static GameObject CreateGlass ( bool shadowed = false )
        {
            GameObject root = CreateUIElementRoot ( NameGlass , s_ImageElementSize , typeof ( GlassBackground ) ) ;
            if ( shadowed )
            {
                var gl = root.GetComponent < GlassBackground > () ;
                gl.halfRound  = true ;
                gl.showShadow = true ;
            }
            return root ;
        }

        // RawImageV2
        static GameObject CreateRawImageV2 () => CreateUIElementRoot ( NameRawImageV2 , s_ImageElementSize , typeof ( RawImageV2 ) );

        // switch
        static GameObject CreateSwitch ( bool shadowed = false )
        {
            //  Lindsay:    Unity:
            //  53 x 32     56.5 x 34.1
            GameObject root = CreateUIElementRoot ( NameSwitch , s_ThickElementSize , typeof ( Switch ) , typeof ( SelectableAdds ) , typeof ( RawImageBG ) ) ;

            // slot box / knob
            var SlotBoxKnob = root.createChild ( "Box Slot Knob" , typeof ( RectSizeNotifier ) ) ;
            SetLeftCenter ( SlotBoxKnob , 50 , 28 ) ;

            // slot
            var Slot = SlotBoxKnob.createChild ( "Slot" , typeof ( RawImageV2 ) ) ;
            var Knob = Slot.createChild ( "Knob" , typeof ( RawImageV2 ) ) ;
            setupBackground ( Slot , true ) ;
            setupBackground ( Knob , true , shadowed ? 2 : 0 ) ;

            // label
            var Label = root.createChild ( "Label" , typeof ( Text ) ) ;
            SetFull ( Label , 60 , 5 , 2 , 1 ) ;
            SetDefaultTextValues ( Label , "Switch" , TextAnchor.MiddleLeft ) ;

            // set elements
            Switch sw = root.GetComponent < Switch > () ;
            sw.setElements ( Slot , Knob ) ;
            if ( shadowed ) sw.knobSize = 3 ;
            SetDefaultColorTransitionValues ( root ) ;
            setDefaultShadowDistances ( root ) ;

            return root ;
        }

        //// switch
        //static GameObject CreateSwitchLindsay ( bool has_label = true )
        //{
        //    //  Lindsay:    Unity:
        //    //  53 x 32     50 x 28
        //    Vector2 ElementSize = has_label ? s_ThickElementSize : new Vector2 ( 50 , 28 ) ;
        //    GameObject root = null ;
        //    GameObject SlotParent = null ;
        //    if ( has_label )
        //    {
        //        root = CreateUIElementRoot ( NameSwitch , s_ThickElementSize , typeof ( Switch ) , typeof ( AutoColorSwitch ) , typeof ( SelectableAdds ) ) ;
        //        // slot box / knob
        //        var SlotBoxKnob = root.createChild ( "Box Slot Knob" , typeof ( RectSizeNotifier ) ) ;
        //        SetLeftCenter ( SlotBoxKnob , 50 , 28 ) ;
        //        SlotParent = SlotBoxKnob ;
        //    }
        //    else
        //    {
        //        root = CreateUIElementRoot ( NameSwitchLindsay , new Vector2 ( 50 , 28 ) , typeof ( Switch ) , typeof ( AutoColorSwitch ) , typeof ( RectSizeNotifier ) , typeof ( SelectableAdds ) );
        //        SlotParent = root ;
        //    }

        //    // slot
        //    var Slot = SlotParent.createChild ( "Slot" , typeof ( RawImageV2 ) ) ;
        //    var SlotRi = Slot.GetComponent < RawImageV2 > () ;
        //    SlotRi.drarBorder = true ;
        //    SlotRi.borderWidth = 1 ;
        //    SlotRi.borderFade = true ;
        //    SlotRi.borderColor = getGrey ( 0.5f ) ;
        //    var Knob = Slot.createChild ( "Knob" , typeof ( RawImageV2 ) ) ;
        //    setupBackground ( Slot , true ) ;
        //    setupBackground ( Knob , true , 1 ) ;

        //    if ( has_label )
        //    {
        //        // label
        //        var Label = root.createChild ( "Label" , typeof ( Text ) ) ;
        //        SetFull ( Label , 60 , 5 , 2 , 1 );
        //        SetDefaultTextValues ( Label , "Switch" , TextAnchor.MiddleLeft , 16 ) ;
        //    }

        //    // set elements
        //    Switch sw = root.GetComponent < Switch > () ;
        //    sw.setElements ( Slot , Knob ) ;
        //    SetDefaultColorTransitionValues ( root ) ;

        //    return root ;
        //}

        // shadowed button
        static GameObject CreateShadowedButton ()
        {
            GameObject go = CreateUIElementRoot ( NameShadowedButton , s_ThickElementSize , typeof ( RawImageV2 ) , typeof ( ButtonV3 ) ) ;

            ButtonV3 bt = go.GetComponent < ButtonV3 > () ;
            SetDefaultColorTransitionValues ( bt ) ;
            setupBackground ( go , true , 3 ) ;

            // text child
            var child = go.createChild ( "Text" , typeof ( Text ) ) ;
            SetFull ( child ) ;
            SetDefaultTextValues ( child , "Button" , TextAnchor.MiddleCenter ) ;

            return go ;
        }

#endif
    }
    // */
}
