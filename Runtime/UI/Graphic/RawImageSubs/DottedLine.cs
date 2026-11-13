using Rudi.Core;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rudi.UI
{
    [ AddComponentMenu ( "Rudi/UI/Dotted Line" ) ]
    public class DottedLine : RawImage
    {
        public enum Style
        {
            Dotted,
            Dashed,
            DashedRound,
        }

        public enum Origin
        {
            [ InspectorName ( "0 to 1" ) ] UV00 ,
            [ InspectorName ( "1 to 0" ) ] UV11 ,
        }

        [ SerializeField ] private Style  m_Style           = Style.Dotted ;
        [ SerializeField ] private Origin m_Origin          = Origin.UV00 ;
        [ SerializeField ] private bool   m_RelativePeriod  = true ;
        [ Min ( 0.5f ) ]
        [ Tooltip ( "Distance from one dot/dash start to the next (in Canvas units)" ) ]
        [ SerializeField ] private float  m_Period          = 20f ;
        [ Min ( 0.1f ) ]
        [ Tooltip ( "Distance from one dot/dash start to the next (multiples of scaled dot size / line thickness)" ) ]
        [ SerializeField ] private float  m_PeriodRelative  = 2f ;
        [ Range ( 0f , 1f ) ]
        [ SerializeField ] private float  m_DashRatio       = 0.5f ;
        [ Range ( 0f , 1f ) ]
        [ SerializeField ] private float  m_Scale           = 0.85f ;
        [ Tooltip ( "If enabled, incomplete cut-off dots/dashes at the far end are hidden" ) ]
        [ SerializeField ] private bool   m_HidePartialEnd  = false ;
        [ SerializeField ] private Color  m_BackgroundColor = Color.clear ;
        [ Range ( 0.5f , 3f ) ]
        [ SerializeField ] private float  m_Smoothing       = 1f ;

        private static readonly string materialName = "Rudi/UI/DottedLine" ;

        private static readonly int s_ID_BgColor       = Shader.PropertyToID ( "_BgColor"       ) ;
        private static readonly int s_ID_UVToPixels    = Shader.PropertyToID ( "_UVToPixels"    ) ;
        private static readonly int s_ID_StripParams   = Shader.PropertyToID ( "_StripParams"   ) ;
        private static readonly int s_ID_PatternParams = Shader.PropertyToID ( "_PatternParams" ) ;
        private static readonly int s_ID_FadeParams    = Shader.PropertyToID ( "_FadeParams"    ) ;

        public Style style
        {
            get => m_Style;
            set { if ( m_Style == value ) return; m_Style = value; SetMaterialDirty (); }
        }

        public Origin origin
        {
            get => m_Origin;
            set { if ( m_Origin == value ) return; m_Origin = value; SetMaterialDirty (); }
        }

        public bool relativePeriod
        {
            get => m_RelativePeriod;
            set { if ( m_RelativePeriod == value ) return; m_RelativePeriod = value; SetMaterialDirty (); }
        }

        public float period
        {
            get => m_Period;
            set
            {
                float v = Mathf.Max ( 0.5f , value ) ;
                m_RelativePeriod = false;
                if ( m_Period == v ) return;
                m_Period = v;
                SetMaterialDirty ();
            }
        }

        public float periodRelative
        {
            get => m_PeriodRelative;
            set
            {
                float v = Mathf.Max ( 0.1f , value ) ;
                m_RelativePeriod = true;
                if ( m_PeriodRelative == v ) return;
                m_PeriodRelative = v;
                SetMaterialDirty ();
            }
        }

        public float dashRatio
        {
            get => m_DashRatio;
            set
            {
                float v = Mathf.Clamp01 ( value ) ;
                if ( m_DashRatio == v ) return;
                m_DashRatio = v;
                SetMaterialDirty ();
            }
        }

        public float scale
        {
            get => m_Scale;
            set
            {
                float v = Mathf.Clamp01 ( value ) ;
                if ( m_Scale == v ) return;
                m_Scale = v;
                SetMaterialDirty ();
            }
        }

        public bool hidePartialEnd
        {
            get => m_HidePartialEnd;
            set { if ( m_HidePartialEnd == value ) return; m_HidePartialEnd = value; SetMaterialDirty (); }
        }

        public Color backgroundColor
        {
            get => m_BackgroundColor;
            set { if ( m_BackgroundColor.Equals ( value ) ) return; m_BackgroundColor = value; SetMaterialDirty (); }
        }

        public float smoothing
        {
            get => m_Smoothing;
            set
            {
                float v = Mathf.Clamp ( value , 0.5f , 3f ) ;
                if ( m_Smoothing == v ) return;
                m_Smoothing = v;
                SetMaterialDirty ();
            }
        }

        private float ppu => null != canvas ? canvas.scaleFactor : 1f;

        private Material m_MaterialDottedLine = null ;
        private Material materialDottedLine
        {
            get
            {
                if ( null == m_MaterialDottedLine )
                {
                    m_MaterialDottedLine = Utils.createMaterial ( materialName );
                    if ( null != m_MaterialDottedLine && !CanvasUpdateRegistry.IsRebuildingGraphics () )
                    {
                        SetMaterialDirty ();
                    }
                }
                return m_MaterialDottedLine;
            }
        }

        public override Material material => materialDottedLine;

        protected override void OnPopulateMesh ( VertexHelper vh )
        {
            vh.Clear ();
            Rect r = rectTransform.rect ;
            if ( r.width <= 0f || r.height <= 0f ) return;

            Color32 c = color ;
            vh.AddVert ( new Vector3 ( r.xMin , r.yMin ) , c , new Vector2 ( 0f , 0f ) );
            vh.AddVert ( new Vector3 ( r.xMin , r.yMax ) , c , new Vector2 ( 0f , 1f ) );
            vh.AddVert ( new Vector3 ( r.xMax , r.yMax ) , c , new Vector2 ( 1f , 1f ) );
            vh.AddVert ( new Vector3 ( r.xMax , r.yMin ) , c , new Vector2 ( 1f , 0f ) );

            vh.AddTriangle ( 0 , 1 , 2 );
            vh.AddTriangle ( 2 , 3 , 0 );
        }

        protected override void UpdateMaterial ()
        {
            base.UpdateMaterial ();
            if ( null != materialForRendering )
            {
                updateMaterial ( materialForRendering );
            }
        }

        private void updateMaterial ( Material mat )
        {
            Rect r = rectTransform.rect ;
            if ( r.width <= 0f || r.height <= 0f ) return;

            float Ppu      = ppu ;
            float widthPx  = r.width  * Ppu ;
            float heightPx = r.height * Ppu ;
            bool  vertical = heightPx > widthPx ;
            float longPx   = vertical ? heightPx : widthPx ;
            float shortPx  = vertical ? widthPx  : heightPx ;

            float smooth    = Mathf.Max ( m_Smoothing , 0.001f ) ;
            float invSmooth = 1f / smooth ;

            float clampedScale    = Mathf.Clamp01 ( m_Scale ) ;
            float thicknessPx     = shortPx * clampedScale ;
            float halfThicknessPx = thicknessPx * 0.5f ;
            float marginPx        = ( shortPx - thicknessPx ) * 0.5f ;
            float activeLenPx     = Mathf.Max ( longPx - 2f * marginPx , 0f ) ;
            float activeHalfLenPx = activeLenPx * 0.5f ;

            // UV -> Pixel-Abbildung inkl. Richtung (UV00 vs. UV11) und Hoch-/Querformat
            bool    reverse     = ( m_Origin == Origin.UV11 ) ;
            float   signedLong  = reverse ? -longPx : longPx ;
            float   offsetStart = reverse ? ( longPx - marginPx ) : -marginPx ;
            float   offsetShort = -0.5f * shortPx ;
            Vector4 uvToPixels  = vertical
                ? new Vector4 ( 0f , signedLong , shortPx , 0f )
                : new Vector4 ( signedLong , 0f , 0f , shortPx ) ;

            // Abstand von Punkt-/Dash-Anfang zum nächsten Anfang (relativ zur skalierten Punktgröße)
            float periodPx = m_RelativePeriod
                ? m_PeriodRelative * thicknessPx
                : m_Period * Ppu ;
            periodPx = Mathf.Max ( periodPx , 0.001f );

            float invPeriodPx  = 1f / periodPx ;
            float halfDashPx   = -1000f ;
            float cornerRadPx  = 0f ;
            float maxCell      = 1e6f ;
            float fgSubFade    = 1f ;
            float stripSubFade = Mathf.Clamp01 ( thicknessPx * invSmooth ) ;

            if ( thicknessPx <= 0.0001f || activeLenPx <= 0.0001f )
            {
                activeHalfLenPx = -1000f;
                halfThicknessPx = -1000f;
                invPeriodPx = 0f;
                maxCell = 0f;
                fgSubFade = 0f;
                stripSubFade = 0f;
            }
            else if ( m_Style == Style.Dotted )
            {
                float dashLenPx = thicknessPx ;
                halfDashPx = halfThicknessPx;
                cornerRadPx = halfThicknessPx;
                fgSubFade = stripSubFade;

                if ( m_HidePartialEnd )
                {
                    float maxFit = Mathf.Floor ( ( activeLenPx - dashLenPx + 0.01f ) * invPeriodPx ) ;
                    if ( maxFit < 0f )
                    {
                        halfDashPx = -1000f;
                        invPeriodPx = 0f;
                        maxCell = 0f;
                        fgSubFade = 0f;
                    }
                    else
                    {
                        maxCell = maxFit;
                    }
                }
            }
            else // Style.Dashed & Style.DashedRound
            {
                if ( m_DashRatio <= 0.0001f )
                {
                    halfDashPx = -1000f;
                    invPeriodPx = 0f;
                    maxCell = 0f;
                    fgSubFade = 0f;
                }
                else if ( m_DashRatio >= 0.9999f )
                {
                    halfDashPx = activeHalfLenPx;
                    cornerRadPx = ( m_Style == Style.DashedRound ) ? Mathf.Min ( halfDashPx , halfThicknessPx ) : 0f;
                    invPeriodPx = 0f;
                    maxCell = 0f;
                    fgSubFade = stripSubFade;
                }
                else
                {
                    float dashLenPx = periodPx * m_DashRatio ;
                    halfDashPx = dashLenPx * 0.5f;
                    cornerRadPx = ( m_Style == Style.DashedRound ) ? Mathf.Min ( halfDashPx , halfThicknessPx ) : 0f;
                    fgSubFade = Mathf.Clamp01 ( Mathf.Min ( dashLenPx , thicknessPx ) * invSmooth );

                    if ( m_HidePartialEnd )
                    {
                        float maxFit = Mathf.Floor ( ( activeLenPx - dashLenPx + 0.01f ) * invPeriodPx ) ;
                        if ( maxFit < 0f )
                        {
                            halfDashPx = -1000f;
                            invPeriodPx = 0f;
                            maxCell = 0f;
                            fgSubFade = 0f;
                        }
                        else
                        {
                            maxCell = maxFit;
                        }
                    }
                }
            }

            mat.SetColor ( s_ID_BgColor , m_BackgroundColor );
            mat.SetVector ( s_ID_UVToPixels , uvToPixels );
            mat.SetVector ( s_ID_StripParams , new Vector4 ( activeHalfLenPx , halfThicknessPx , offsetStart , offsetShort ) );
            mat.SetVector ( s_ID_PatternParams , new Vector4 ( periodPx , invPeriodPx , halfDashPx , cornerRadPx ) );
            mat.SetVector ( s_ID_FadeParams , new Vector4 ( -invSmooth , maxCell , fgSubFade , stripSubFade ) );
        }

        protected override void OnRectTransformDimensionsChange ()
        {
            base.OnRectTransformDimensionsChange ();
            SetMaterialDirty ();
        }

        protected override void OnDestroy ()
        {
            Utils.DestroyObjectAndZero ( ref m_MaterialDottedLine );
            base.OnDestroy ();
        }

#if UNITY_EDITOR
        protected override void OnValidate ()
        {
            base.OnValidate ();
            SetMaterialDirty ();
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor ( typeof ( DottedLine ) )]
    [CanEditMultipleObjects]
    public class DottedLineEditor : UnityEditor.UI.GraphicEditor
    {
        private static readonly GUIContent s_ForegroundLabel = new ( "Foreground Color" ) ;

        SerializedProperty m_Style           ;
        SerializedProperty m_Origin          ;
        SerializedProperty m_RelativePeriod  ;
        SerializedProperty m_Period          ;
        SerializedProperty m_PeriodRelative  ;
        SerializedProperty m_DashRatio       ;
        SerializedProperty m_Scale           ;
        SerializedProperty m_HidePartialEnd  ;
        SerializedProperty m_BackgroundColor ;
        SerializedProperty m_Smoothing       ;

        protected override void OnEnable ()
        {
            base.OnEnable ();
            m_Style           = serializedObject.FindProperty ( "m_Style"           );
            m_Origin          = serializedObject.FindProperty ( "m_Origin"          );
            m_RelativePeriod  = serializedObject.FindProperty ( "m_RelativePeriod"  );
            m_Period          = serializedObject.FindProperty ( "m_Period"          );
            m_PeriodRelative  = serializedObject.FindProperty ( "m_PeriodRelative"  );
            m_DashRatio       = serializedObject.FindProperty ( "m_DashRatio"       );
            m_Scale           = serializedObject.FindProperty ( "m_Scale"           );
            m_HidePartialEnd  = serializedObject.FindProperty ( "m_HidePartialEnd"  );
            m_BackgroundColor = serializedObject.FindProperty ( "m_BackgroundColor" );
            m_Smoothing       = serializedObject.FindProperty ( "m_Smoothing"       );
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();

            EditorGUILayout.LabelField ( "Colors" , EditorStyles.boldLabel );
            EditorGUILayout.PropertyField ( m_Color , s_ForegroundLabel );
            EditorGUILayout.PropertyField ( m_BackgroundColor );

            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ( "Pattern" , EditorStyles.boldLabel );
            EditorGUILayout.PropertyField ( m_Style );
            EditorGUILayout.PropertyField ( m_Origin );
            EditorGUILayout.PropertyField ( m_RelativePeriod );

            if ( !m_RelativePeriod.boolValue )
            {
                EditorGUILayout.PropertyField ( m_Period );
            }
            else
            {
                EditorGUILayout.PropertyField ( m_PeriodRelative );
            }

            if ( m_Style.enumValueIndex != ( int ) DottedLine.Style.Dotted )
            {
                EditorGUILayout.PropertyField ( m_DashRatio );
            }

            EditorGUILayout.PropertyField ( m_Scale );
            EditorGUILayout.PropertyField ( m_HidePartialEnd );

            EditorGUILayout.Space ();
            EditorGUILayout.PropertyField ( m_Smoothing );

            RaycastControlsGUI ();
            MaskableControlsGUI ();
            serializedObject.ApplyModifiedProperties ();
        }
    }
#endif
}
