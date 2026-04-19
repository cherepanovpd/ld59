// Path: Assets/_Project/Scripts/Editor/CreateHumanPrefab.cs

using UnityEditor;
using UnityEngine;
using Project.Human;

namespace Project.Editor
{
    /// <summary>
    /// Editor utility to create a HumanCharacter prefab with all required components.
    /// Run this from the menu: Tools/Create Human Prefab
    /// </summary>
    public static class CreateHumanPrefab
    {
        [MenuItem("Tools/Create Human Prefab")]
        public static void Execute()
        {
            // Ensure the target directory exists
            const string prefabsPath = "Assets/_Project/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsPath))
            {
                Debug.LogError($"Prefabs directory not found: {prefabsPath}");
                return;
            }

            // Create a new GameObject
            GameObject humanGo = new GameObject("HumanCharacter");
            
            // Add required components
            HumanCharacter humanCharacter = humanGo.AddComponent<HumanCharacter>();
            HumanVisual humanVisual = humanGo.AddComponent<HumanVisual>();
            HumanMovement humanMovement = humanGo.AddComponent<HumanMovement>();
            HumanState humanState = humanGo.AddComponent<HumanState>();
            
            // Add SpriteRenderer (required by HumanVisual)
            SpriteRenderer spriteRenderer = humanGo.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = humanGo.AddComponent<SpriteRenderer>();
            
            // Create child objects for body, head, hair
            GameObject bodyChild = new GameObject("Body");
            bodyChild.transform.SetParent(humanGo.transform);
            bodyChild.transform.localPosition = Vector3.zero;
            SpriteRenderer bodyRenderer = bodyChild.AddComponent<SpriteRenderer>();
            bodyRenderer.sortingOrder = 0;
            
            GameObject headChild = new GameObject("Head");
            headChild.transform.SetParent(humanGo.transform);
            headChild.transform.localPosition = new Vector3(0, 0.5f, 0); // offset
            SpriteRenderer headRenderer = headChild.AddComponent<SpriteRenderer>();
            headRenderer.sortingOrder = 1;
            
            GameObject hairChild = new GameObject("Hair");
            hairChild.transform.SetParent(humanGo.transform);
            hairChild.transform.localPosition = new Vector3(0, 0.6f, 0); // offset
            SpriteRenderer hairRenderer = hairChild.AddComponent<SpriteRenderer>();
            hairRenderer.sortingOrder = 2;
            
            // Assign references in HumanVisual
            SerializedObject visualSo = new SerializedObject(humanVisual);
            visualSo.FindProperty("_bodyRenderer").objectReferenceValue = bodyRenderer;
            visualSo.FindProperty("_headRenderer").objectReferenceValue = headRenderer;
            visualSo.FindProperty("_hairRenderer").objectReferenceValue = hairRenderer;
            visualSo.ApplyModifiedProperties();
            
            // Assign component references in HumanCharacter
            SerializedObject charSo = new SerializedObject(humanCharacter);
            charSo.FindProperty("_visual").objectReferenceValue = humanVisual;
            charSo.FindProperty("_movement").objectReferenceValue = humanMovement;
            charSo.FindProperty("_state").objectReferenceValue = humanState;
            charSo.ApplyModifiedProperties();
            
            // Save as prefab
            string prefabPath = $"{prefabsPath}/HumanCharacter.prefab";
            PrefabUtility.SaveAsPrefabAsset(humanGo, prefabPath);
            Debug.Log($"Created prefab at {prefabPath}");
            
            // Destroy the temporary GameObject
            Object.DestroyImmediate(humanGo);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("HumanCharacter prefab created successfully. Please assign configuration assets in the Inspector.");
        }
    }
}