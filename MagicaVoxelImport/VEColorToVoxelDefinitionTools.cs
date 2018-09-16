using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VoxelPlay;
using UnityEngine;
using UnityEditor;

namespace VE.Importers
{
    

    public class VEColorToVoxelDefinitionTools : EditorWindow 
    {
        private VEColor32ToVoxelDefinitionMap map;
        private ModelDefinition model;

        [MenuItem("Assets/Create/Voxel Engines/Apply CTV Map", false, 1000)]
        public static void ShowWindow()
        {
            var window = GetWindow<VEColorToVoxelDefinitionTools>("Apply Color to VoxDef Map", true);
            window.minSize = new Vector2(300, 120);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Color To Voxel Map", "Choose the map."), GUILayout.Width(160));
            map = (VEColor32ToVoxelDefinitionMap)EditorGUILayout.ObjectField(
                "", 
                map, 
                typeof(VEColor32ToVoxelDefinitionMap), 
                true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Model Definition", "Choose the mdef."), GUILayout.Width(160));
            model = (ModelDefinition)EditorGUILayout.ObjectField(
                "",
                model,
                typeof(ModelDefinition),
                true
                );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            GUI.enabled = model && map;
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Apply", GUILayout.Width(160)))
            {
                Apply();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = false;
            EditorGUILayout.EndHorizontal();
        }

        private void Apply()
        {
            if(model == null || map == null)
            {
                Debug.Log("hmmm... model or map was null");
                return;
            }

            var bits = model.bits;
            for(int i=0; i<bits.Length; ++i)
            {
                VEColor32ToVoxelDefinitionMap.VoxDefReplace def;
                if(map.Lookup(bits[i].color, out def))
                {
                    bits[i].voxelDefinition = def.voxDef;
                    if (def.replaceColorsWithWhite)
                    {
                        bits[i].color = Misc.color32White;
                    }
                }
            }
            model.bits = bits;

            //overwrite model
            //var path = AssetDatabase.GetAssetPath(model);
            //AssetDatabase.CreateAsset(model, path);
            //AssetDatabase.SaveAssets();
            //EditorUtility.FocusProjectWindow();
            //Selection.activeObject = model; // errors

            EditorUtility.SetDirty(model);
            AssetDatabase.SaveAssets();
        }
    }
}
