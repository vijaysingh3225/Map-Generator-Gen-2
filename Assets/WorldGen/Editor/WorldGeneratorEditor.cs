using System.IO;
using UnityEditor;
using UnityEngine;
using WorldGen.Core;
using WorldGen.Debug;

namespace WorldGen.Editor
{
    [CustomEditor(typeof(WorldGenerator))]
    public sealed class WorldGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var gen = (WorldGenerator)target;

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(gen.settings == null))
            {
                if (GUILayout.Button("Generate", GUILayout.Height(40)))
                {
                    gen.Generate();
                    EditorUtility.SetDirty(gen);
                }
            }

            EditorGUILayout.Space(6);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(gen.LastOutputPath)))
            {
                if (GUILayout.Button("Open Output Folder"))
                {
                    var path = gen.LastOutputPath;
                    if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    {
                        EditorUtility.RevealInFinder(path);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("WorldGen", $"Output folder not found:\n{path}", "OK");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(gen.LastOutputPath))
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Last Output Path", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(gen.LastOutputPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            if (gen.settings != null)
            {
                var resolved = DebugPaths.ResolveRunOutputPath(gen.settings);
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Next Run Output (preview)", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(resolved, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        [MenuItem("Assets/Create/WorldGen/WorldGen Settings", priority = 10)]
        public static void CreateSettingsAsset()
        {
            var asset = ScriptableObject.CreateInstance<WorldGenSettings>();

            var folder = GetSelectedFolderPath();
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "WorldGenSettings.asset"));

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        private static string GetSelectedFolderPath()
        {
            var path = "Assets";
            if (Selection.activeObject == null) return path;

            var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrWhiteSpace(selectedPath)) return path;

            if (Directory.Exists(selectedPath)) return selectedPath;

            var dir = Path.GetDirectoryName(selectedPath);
            return string.IsNullOrWhiteSpace(dir) ? "Assets" : dir;
        }
    }
}


