using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using Rudi.Extensions;

namespace Rudi.UI
{
	[ExecuteAlways]
    [RequireComponent ( typeof ( RectTransform ) )]
    [AddComponentMenu ( "Rudi/UI/Switch" )]

    public class Switch : Selectable, IPointerClickHandler, IToggle, IResizedReciever
    {
        private static readonly string TAG = "Rudis Switch" ;
        private static readonly Color DefaultColorKnobOn  = new Color32 ( 14 , 213 , 14 , 255 ) ;
        private static readonly Color DefaultColorKnobOff = Color.gray ;
        private static readonly Color DefaultColorSlotOn  = Color.white ;
        private static readonly Color DefaultColorSlotOff = new Color32 ( 216 , 216 , 216 , 255 ) ;

        //private static readonly Color DefaultColorKnobOn  = Lindsay.getColor ( Lindsay.EColors.White ) ;
        //private static readonly Color DefaultColorKnobOff = Lindsay.getColor ( Lindsay.EColors.White ) ;
        //private static readonly Color DefaultColorSlotOn  = Lindsay.getColor ( Lindsay.EColors.Blue ) ;
        //private static readonly Color DefaultColorSlotOff = Lindsay.getColor ( Lindsay.EColors.LighterGrey ) ;

        [ Header ( "State" ) ]
        [ SerializeField ] private bool m_IsOn = true ;
        [ Range ( -10 , 10 ) ]
        [ SerializeField ] private int m_KnobSize = -1 ;

        [ Header ( "Colors" ) ]
        [ SerializeField ] private bool m_DriveKnobColor = true ;
        [ SerializeField ] private Color m_KnobColorOn  = DefaultColorKnobOn  ;
        [ SerializeField ] private Color m_KnobColorOff = DefaultColorKnobOff ;
        [ Space ]
        [ SerializeField ] private bool m_DriveSlotColor = true ;
        [ SerializeField ] private Color m_SlotColorOn  = DefaultColorSlotOn  ;
        [ SerializeField ] private Color m_SlotColorOff = DefaultColorSlotOff ;

        [ Header ( "Components" ) ]
        [ SerializeField ] private Graphic m_Slot ;
        [ SerializeField ] private Graphic m_Knob ;

        [Serializable]
        public class SwitchEvent : UnityEvent<bool> { }
        [ Space ]
        [ SerializeField ] private SwitchEvent m_OnValueChanged = new SwitchEvent () ;

        public SwitchEvent onValueChanged => m_OnValueChanged;

        public int knobSize
        {
            get => m_KnobSize;
            set
            {
                if ( value == m_KnobSize ) return;
                m_KnobSize = value;
                setRectTransforms ();
            }
        }

        public Color colorKnobOn
        {
            get => m_KnobColorOn;
            set
            {
                m_KnobColorOn = value;
                if ( m_DriveKnobColor && null != m_Knob ) m_Knob.color = Color.Lerp ( m_KnobColorOff , m_KnobColorOn , m_CurrentSwitchState );
            }
        }

        public Color colorKnobOff
        {
            get => m_KnobColorOff;
            set
            {
                m_KnobColorOff = value;
                if ( m_DriveKnobColor && null != m_Knob ) m_Knob.color = Color.Lerp ( m_KnobColorOff , m_KnobColorOn , m_CurrentSwitchState );
            }
        }

        public Color colorSlotOn
        {
            get => m_SlotColorOn;
            set
            {
                m_SlotColorOn = value;
                if ( m_DriveSlotColor && null != m_Slot ) m_Slot.color = Color.Lerp ( m_SlotColorOff , m_SlotColorOn , m_CurrentSwitchState );
            }
        }

        public Color colorSlotOff
        {
            get => m_SlotColorOff;
            set
            {
                m_SlotColorOff = value;
                if ( m_DriveSlotColor && null != m_Slot ) m_Slot.color = Color.Lerp ( m_SlotColorOff , m_SlotColorOn , m_CurrentSwitchState );
            }
        }

        //private float m_CurrentValue = 0 ;

        private OnOffTweenRunner m_OnOffTweenRunner = null ;
        private OnOffTweenRunner onOffTweener
        {
            get
            {
                if ( null == m_OnOffTweenRunner )
                {
                    m_OnOffTweenRunner = new OnOffTweenRunner ( this , () => m_CurrentSwitchState );
                    m_OnOffTweenRunner.onValueChanged.AddListener ( SetSwitchState );
                }
                return m_OnOffTweenRunner;
            }
        }

        bool selfSlot => m_Slot == null;

        private RectTransform m_RectTransform = null ;

        private RectTransform rectTransform
        {
            get
            {
                if ( null == m_RectTransform )
                {
                    m_RectTransform = GetComponent<RectTransform> ();
                }
                return m_RectTransform;
            }
        }

        private RectTransform rectSlot => null != m_Slot ? m_Slot.rectTransform : null;
        private RectTransform rectKnob => null != m_Knob ? m_Knob.rectTransform : null;
        private RectTransform rectSwitch => null != rectSlot ? rectSlot.parent as RectTransform : null;

        public bool isOn
        {
            get => m_IsOn;
            set => Set ( value );
        }

        public void SetIsOnWithoutNotify ( bool value )
        {
            Set ( value , false );
        }

        private void Set ( bool value , bool sendCallback = true )
        {
            if ( m_IsOn == value ) return;
            InternalSet ( value , sendCallback );
        }

        private void InternalSet ( bool value , bool sendCallback = true , bool instant = false )
        {
            m_IsOn = value;
            PlayEffect ( instant );
            if ( sendCallback )
            {
                m_OnValueChanged.Invoke ( m_IsOn );
            }
        }

        public bool setElements ( GameObject slot , GameObject knob )
        {
            m_Slot = slot.GetComponent<Graphic> ();
            m_Knob = knob.GetComponent<Graphic> ();
            targetGraphic = m_Knob;
            var Result =  null != m_Slot  &&  null != m_Knob  ;
            if ( Result )
            {
#if UNITY_EDITOR
                startTracker ();
#endif
                setRectTransforms ();
                InternalSet ( m_IsOn , false , true );
            }

            return Result;
        }

        private void PlayEffect ( bool instant = false )
        {
#if UNITY_EDITOR
            if ( !Application.isPlaying )
            {
                SetSwitchState ( m_IsOn ? 1f : 0f );
            }
            else
#endif
            {
                onOffTweener.tween ( m_IsOn , instant ? 0 : colors.fadeDuration );
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate ()
        {
            base.OnValidate ();
            //if ( Application.isPlaying ) return ;
            setRectTransforms ();
            InternalSet ( m_IsOn , false , true );
        }
#endif

        private void InternalToggle ()
        {
            if ( !IsActive () || !IsInteractable () ) return;

            isOn = !isOn;
        }

        private float m_CurrentSwitchState ;

        private static readonly Vector2 half2 = Vector2.one * 0.5f ;

        private void setRectTransforms ()
        {
            if ( selfSlot ) return;
            if ( null == m_Slot ) return;
            if ( null == m_Knob ) return;
            var relKnobSize = Mathf.Exp ( m_KnobSize * 0.1f * Mathf.Log ( 5f ) ) ;

            var s = rectSwitch.rect.size ;
            //Log.i ( TAG , "size = " + s ) ;

            var SwitchHeight = rectSwitch.rect.height ;
            var SwitchWidth  = rectSwitch.rect.width ;
            var Size = Mathf.Min ( SwitchWidth , SwitchHeight ) ;
            var rtKnob = rectKnob ;
            rtKnob.anchorMin = half2;
            rtKnob.anchorMax = half2;
            rtKnob.pivot = half2;
            if ( relKnobSize > 1f )
            {
                // shrink Slot
                var SlotSize = Size / relKnobSize ;
                var SizeDiff = Size - SlotSize ;
                var SlotWidth = SwitchWidth - SizeDiff ;
                var SlotHeight = SwitchHeight - SizeDiff ;
                rectSlot.anchorMin = half2;
                rectSlot.anchorMax = half2;
                rectSlot.pivot = half2;
                rectSlot.sizeDelta = new Vector2 ( SlotWidth , SlotHeight );
                //set knob
                rtKnob.sizeDelta = new Vector2 ( Size , Size );
            }
            else
            {
                // expand Slot
                rectSlot.SetFull ();
                //set knob
                var KnobSize = Size * relKnobSize ;
                rtKnob.sizeDelta = new Vector2 ( KnobSize , KnobSize );
            }
            rectSlot.localScale = Vector3.one;
            rtKnob.localScale = Vector3.one;
        }

        private void SetSwitchState ( float v )
        {
            m_CurrentSwitchState = v;
            if ( null == m_Knob ) return;
            if ( m_DriveKnobColor && null != m_Knob ) m_Knob.color = Color.Lerp ( m_KnobColorOff , m_KnobColorOn , v );
            if ( m_DriveSlotColor && null != m_Slot ) m_Slot.color = Color.Lerp ( m_SlotColorOff , m_SlotColorOn , v );

            var rtKnob = rectKnob ;
            var rtSwitch = rectSwitch ;
            if ( null == rtSwitch ) return;
            var SwitchHeight = rtSwitch.rect.height ;
            var SwitchWidth  = rtSwitch.rect.width ;
            var distance = Mathf.Abs ( SwitchWidth - SwitchHeight ) * 0.5f ;

            rtKnob.anchorMin = half2;
            rtKnob.anchorMax = half2;
            rtKnob.pivot = half2;
            rtKnob.anchoredPosition = new Vector2 ( Mathf.Lerp ( -distance , distance , v ) , 0 );
        }

        DrivenRectTransformTracker m_Tracker ;

        private void startTracker ()
        {
            m_Tracker.Clear ();
            if ( null != m_Slot ) m_Tracker.Add ( this , rectSlot , DrivenTransformProperties.All );
            if ( null != m_Knob ) m_Tracker.Add ( this , rectKnob , DrivenTransformProperties.All );
        }

        protected override void OnEnable ()
        {
            base.OnEnable ();
            startTracker ();
        }

        protected override void OnDisable ()
        {
            m_Tracker.Clear ();
            base.OnDisable ();
        }
        private bool m_bStarted = false ;
        protected override void Start ()
        {
            base.Start ();
            setRectTransforms ();
            InternalSet ( m_IsOn , false , true );
            m_bStarted = true;
        }

        public void OnResized ()
        {
            setRectTransforms ();
            SetSwitchState ( m_CurrentSwitchState );
        }

        public void OnPointerClick ( PointerEventData eventData )
        {
            if ( eventData.button != PointerEventData.InputButton.Left ) return;
            //Log.i ( TAG , "OnPointerClick ()" ) ;
            InternalToggle ();
        }
    }
}
