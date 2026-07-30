
//;/*
using UnityEngine;
using System; // Wichtig für Span

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

namespace Rudi.Core
{
    public class ShaderAssemblage : MonoBehaviour
    {
        private static readonly string TAG = "Rudis ShaderAssemblage" ;

#if UNITY_EDITOR
        [ SerializeField ] private DefaultAsset m_ShaderFolder ;

        // Statischer Konstruktor: Wird von Unity durch [InitializeOnLoad] automatisch aufgerufen
        static ShaderAssemblage ()
        {
            // Das Echtzeit-Netz während des Arbeitens
            EditorApplication.projectChanged += TriggerGlobalUpdate;

            // Das absolute Sicherheitsnetz beim Drücken auf Start
            EditorApplication.playModeStateChanged += ( state ) =>
            {
                if ( state == PlayModeStateChange.ExitingEditMode ) TriggerGlobalUpdate ();
            };
        }

        private static void TriggerGlobalUpdate ()
        {
            //Log.i ( TAG , "TriggerGlobalUpdate ()" ) ;
            // 1. Grobe Vorfilterung auf Festplattenebene (Sehr schnell)
            var guids = AssetDatabase.FindAssets ( "ShaderAssemblagePrefab t:Prefab" ) ;

            if ( guids.Length == 0 ) return;

            ShaderAssemblage resultPrefab = null ;
            var validCount = 0 ;

            // 2. Deine allokationsfreie Zählschleife
            for ( var i = 0 ; i < guids.Length ; i++ )
            {
                var path = AssetDatabase.GUIDToAssetPath ( guids [ i ] ) ;
                var comp = AssetDatabase.LoadAssetAtPath < ShaderAssemblage > ( path ) ;

                if ( comp != null )
                {
                    validCount++ ;
                    if ( resultPrefab == null ) resultPrefab = comp ;
                }
            }

            // 3. Auswertung des Ergebnisses komplett ohne Array-Ballast
            if ( validCount == 0 ) return ;

            if ( validCount > 1 )
            {
                Log.i ( TAG , $"Conflict: {validCount} 'ShaderAssemblagePrefab' assets with the 'ShaderAssemblage' component were found!\n" +
                                "Please ensure that the prefab is unique within the project." ) ;
            }

            // Das gefundene Ergebnis absolut sicher und ohne Umwege aktualisieren
            resultPrefab.AutomaticallyPopulateShaders ();
        }
#endif

        [ SerializeField , ReadOnly ] private ReadOnlyArray < Shader > m_Shaders  = new () { Values = Array.Empty < Shader > () } ;

#if UNITY_EDITOR
        // --- DIE INSTANT-AUTOMATISIERUNG BEI ORDNERWECHSEL ---
        private void OnValidate ()
        {
            EditorApplication.delayCall += () =>
            {
                // Verhindert Fehlermeldungen, während das Projekt kompiliert 
                // oder Unity im Hintergrund Assets importiert
                if ( EditorApplication.isCompiling || EditorApplication.isUpdating ) return;

                // Wir nutzen einfach die bestehende Logik!
                AutomaticallyPopulateShaders ();
            };
        }

        [ContextMenu ( "Read Shaders automatically" )] // Shader automatisch einlesen
        private void AutomaticallyPopulateShaders ()
        {
            if ( m_ShaderFolder == null )
            {
                if ( m_Shaders.Values.Length > 0 )
                {
                    Undo.RecordObject ( this , "Clear Shaders" );
                    m_Shaders.Values = Array.Empty<Shader> ();
                    EditorUtility.SetDirty ( this );
                }
                return;
            }

            var folderPath = AssetDatabase.GetAssetPath ( m_ShaderFolder ) ;

            if ( !AssetDatabase.IsValidFolder ( folderPath ) )
            {
                Log.i ( TAG , $"The assigned asset { folderPath } is not a valid folder!" );
                return;
            }

            var guids = AssetDatabase.FindAssets ( "t:Shader" , new [] { folderPath } ) ;

            var discoveredShaders = new Shader [ guids.Length ] ;
            var validCount = 0 ;

            for ( var i = 0 ; i < guids.Length ; i++ )
            {
                var assetPath = AssetDatabase.GUIDToAssetPath ( guids [ i ] ) ;
                var shader = AssetDatabase.LoadAssetAtPath < Shader > ( assetPath ) ;

                if ( shader != null )
                {
                    discoveredShaders [validCount] = shader;
                    validCount++;
                }
            }

            ReadOnlySpan < Shader > discoveredSpan = discoveredShaders.AsSpan ( 0 , validCount ) ;

            // Wir übergeben den Span direkt an die Vergleichsmethode
            if ( !AreShaderArraysEqual ( m_Shaders , discoveredSpan ) )
            {
                Undo.RecordObject ( this , "Automatically Populate Shaders" );

                // Nur bei echten Änderungen wird das finale Array erzeugt
                m_Shaders.Values = discoveredSpan.ToArray ();

                EditorUtility.SetDirty ( this );
                AssetDatabase.SaveAssetIfDirty ( this );
                Log.i ( TAG , $"Span-optimized update: {m_Shaders.Values.Length} shaders!" );
                //Debug.Log ( $"[{TAG}] Span-optimiert aktualisiert: {m_Shader.Length} Shader!" );
            }
        }

        // Die neue, hochmoderne Vergleichsmethode für dein Buch:
        private bool AreShaderArraysEqual ( Shader [ ] current , ReadOnlySpan<Shader> discovered )
        {
            if ( current.Length != discovered.Length ) return false;

            for ( var i = 0 ; i < current.Length ; i++ )
            {
                if ( current [i] != discovered [i] ) return false;
            }

            return true;
        }
#endif

    }
}
// */
