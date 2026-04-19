// Path: Assets/_Project/Scripts/Editor/CreateHumanConfigAssets.cs

using UnityEditor;
using UnityEngine;
using Project.Human.Config;

namespace Project.Editor
{
    /// <summary>
    /// Editor utility to create the required ScriptableObject assets for the human character system.
    /// Run this from the menu: Tools/Create Human Config Assets
    /// </summary>
    public static class CreateHumanConfigAssets
    {
        [MenuItem("Tools/Create Human Config Assets")]
        public static void Execute()
        {
            // Ensure the target directory exists
            const string configsPath = "Assets/_Project/Configs";
            if (!AssetDatabase.IsValidFolder(configsPath))
            {
                Debug.LogError($"Configs directory not found: {configsPath}");
                return;
            }

            // 1. Create BodyColorConfig
            BodyColorConfig bodyColorConfig = ScriptableObject.CreateInstance<BodyColorConfig>();
            AssetDatabase.CreateAsset(bodyColorConfig, $"{configsPath}/BodyColorConfig.asset");
            Debug.Log($"Created {configsPath}/BodyColorConfig.asset");

            // 2. Create HeadColorConfig
            HeadColorConfig headColorConfig = ScriptableObject.CreateInstance<HeadColorConfig>();
            AssetDatabase.CreateAsset(headColorConfig, $"{configsPath}/HeadColorConfig.asset");
            Debug.Log($"Created {configsPath}/HeadColorConfig.asset");

            // 3. Create HairColorConfig
            HairColorConfig hairColorConfig = ScriptableObject.CreateInstance<HairColorConfig>();
            AssetDatabase.CreateAsset(hairColorConfig, $"{configsPath}/HairColorConfig.asset");
            Debug.Log($"Created {configsPath}/HairColorConfig.asset");

            // 4. Create HumanConfig
            HumanConfig humanConfig = ScriptableObject.CreateInstance<HumanConfig>();
            // Assign references to color configs
            humanConfig.SetPrivateField("_bodyColorConfig", bodyColorConfig);
            humanConfig.SetPrivateField("_headColorConfig", headColorConfig);
            humanConfig.SetPrivateField("_hairColorConfig", hairColorConfig);
            // Set default prefabs/sprites (none for now, user must assign later)
            AssetDatabase.CreateAsset(humanConfig, $"{configsPath}/HumanConfig.asset");
            Debug.Log($"Created {configsPath}/HumanConfig.asset");

            // Save all assets
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("All human config assets created successfully. Please assign prefabs and sprites in the Inspector.");
        }

        // Helper method to set private fields via reflection (since we cannot directly assign)
        private static void SetPrivateField<T>(this object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
            else
                Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}");
        }
    }
}