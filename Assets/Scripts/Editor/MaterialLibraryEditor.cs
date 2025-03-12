#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor window for managing building physics materials
/// </summary>
public class MaterialLibraryEditor : EditorWindow
{
    private Vector2 scrollPosition;
    private List<BuildingPhysicsMaterial> materials = new List<BuildingPhysicsMaterial>();
    private string searchString = "";
    private string[] categoryFilters = new string[] { "All", "Wall", "Insulation", "Glass", "Floor", "Roof", "Door", "Window", "Concrete", "Wood", "Metal", "General" };
    private int selectedCategoryIndex = 0;
    private bool showThermalProperties = true;
    private bool showMoistureProperties = true;
    private bool showEconomicProperties = true;
    
    [MenuItem("Building/Material Library")]
    public static void ShowWindow()
    {
        GetWindow<MaterialLibraryEditor>("Building Material Library");
    }
    
    void OnEnable()
    {
        LoadAllMaterials();
    }
    
    /// <summary>
    /// Loads all BuildingPhysicsMaterial assets in the project
    /// </summary>
    void LoadAllMaterials()
    {
        materials.Clear();
        string[] guids = AssetDatabase.FindAssets("t:BuildingPhysicsMaterial");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildingPhysicsMaterial material = AssetDatabase.LoadAssetAtPath<BuildingPhysicsMaterial>(path);
            if (material != null)
            {
                materials.Add(material);
            }
        }
    }
    
    void OnGUI()
    {
        GUILayout.Label("Building Physics Material Library", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        // Search bar
        searchString = EditorGUILayout.TextField("Search:", searchString);
        
        // Category filter
        EditorGUILayout.Space();
        selectedCategoryIndex = EditorGUILayout.Popup("Category:", selectedCategoryIndex, categoryFilters);
        
        EditorGUILayout.EndHorizontal();
        
        // Action buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Create New Material"))
        {
            CreateNewMaterial();
        }
        
        if (GUILayout.Button("Refresh"))
        {
            LoadAllMaterials();
        }
        
        if (GUILayout.Button("Import Standard Library"))
        {
            ImportStandardLibrary();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Property toggles
        EditorGUILayout.BeginHorizontal();
        showThermalProperties = EditorGUILayout.ToggleLeft("Thermal", showThermalProperties, GUILayout.Width(80));
        showMoistureProperties = EditorGUILayout.ToggleLeft("Moisture", showMoistureProperties, GUILayout.Width(80));
        showEconomicProperties = EditorGUILayout.ToggleLeft("Economic", showEconomicProperties, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // Display materials
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (var material in materials)
        {
            // Filter by search
            if (!string.IsNullOrEmpty(searchString) && 
                !material.materialName.ToLower().Contains(searchString.ToLower()) &&
                !material.category.ToLower().Contains(searchString.ToLower()))
            {
                continue;
            }
            
            // Filter by category
            if (selectedCategoryIndex > 0 && material.category != categoryFilters[selectedCategoryIndex])
            {
                continue;
            }
            
            EditorGUILayout.BeginVertical("box");
            
            // Header row
            EditorGUILayout.BeginHorizontal();
            
            // Left side - thumbnail
            EditorGUILayout.BeginVertical(GUILayout.Width(60));
            if (material.thumbnail != null)
            {
                GUILayout.Label(new GUIContent(material.thumbnail), GUILayout.Width(60), GUILayout.Height(60));
            }
            else
            {
                EditorGUI.DrawRect(
                    GUILayoutUtility.GetRect(60, 60), 
                    material.materialColor
                );
            }
            EditorGUILayout.EndVertical();
            
            // Right side - info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(material.materialName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Category: {material.category}");
            
            // Show thermal properties
            if (showThermalProperties)
            {
                EditorGUILayout.LabelField($"Thermal Conductivity: {material.thermalConductivity} W/m·K");
                EditorGUILayout.LabelField($"U-Value (10cm): {material.GetUValue(0.1f):F2} W/m²·K");
            }
            
            // Show moisture properties
            if (showMoistureProperties)
            {
                EditorGUILayout.LabelField($"Vapor Resistance Factor: {material.waterVaporResistance}");
            }
            
            // Show economic properties
            if (showEconomicProperties)
            {
                EditorGUILayout.LabelField($"Cost: {material.costPerSquareMeter}/m²");
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Bottom row - buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Edit"))
            {
                Selection.activeObject = material;
            }
            if (GUILayout.Button("Duplicate"))
            {
                DuplicateMaterial(material);
            }
            GUI.color = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Material", 
                    $"Are you sure you want to delete {material.materialName}?", 
                    "Delete", "Cancel"))
                {
                    DeleteMaterial(material);
                }
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// Creates a new building material asset
    /// </summary>
    void CreateNewMaterial()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Building Material",
            "New Building Material",
            "asset",
            "Create a new building physics material"
        );
        
        if (string.IsNullOrEmpty(path))
            return;
            
        BuildingPhysicsMaterial material = ScriptableObject.CreateInstance<BuildingPhysicsMaterial>();
        material.materialName = Path.GetFileNameWithoutExtension(path);
        material.category = "General";
        
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        
        materials.Add(material);
        Selection.activeObject = material;
    }
    
    /// <summary>
    /// Duplicates an existing material asset
    /// </summary>
    void DuplicateMaterial(BuildingPhysicsMaterial source)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Duplicate Building Material",
            source.materialName + " Copy",
            "asset",
            "Create a copy of this building material"
        );
        
        if (string.IsNullOrEmpty(path))
            return;
            
        BuildingPhysicsMaterial material = ScriptableObject.CreateInstance<BuildingPhysicsMaterial>();
        
        // Copy properties
        material.materialName = Path.GetFileNameWithoutExtension(path);
        material.category = source.category;
        material.renderMaterial = source.renderMaterial;
        material.thumbnail = source.thumbnail;
        material.materialColor = source.materialColor;
        material.thermalConductivity = source.thermalConductivity;
        material.specificHeatCapacity = source.specificHeatCapacity;
        material.density = source.density;
        material.waterVaporResistance = source.waterVaporResistance;
        material.maxMoistureContent = source.maxMoistureContent;
        material.costPerSquareMeter = source.costPerSquareMeter;
        material.embodiedCarbonPerKg = source.embodiedCarbonPerKg;
        material.expectedLifespan = source.expectedLifespan;
        
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        
        materials.Add(material);
        Selection.activeObject = material;
    }
    
    /// <summary>
    /// Deletes a material asset
    /// </summary>
    void DeleteMaterial(BuildingPhysicsMaterial material)
    {
        string path = AssetDatabase.GetAssetPath(material);
        if (string.IsNullOrEmpty(path))
            return;
            
        AssetDatabase.DeleteAsset(path);
        materials.Remove(material);
    }
    
    /// <summary>
    /// Creates a standard library of building materials
    /// </summary>
    void ImportStandardLibrary()
    {
        if (!EditorUtility.DisplayDialog("Import Standard Library", 
            "This will create a set of common building materials with standard physical properties. Continue?", 
            "Import", "Cancel"))
        {
            return;
        }
        
        // Create folder if it doesn't exist
        string folderPath = "Assets/BuildingModel/Materials/PhysicsMaterials";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parentFolder = Path.GetDirectoryName(folderPath);
            string folderName = Path.GetFileName(folderPath);
            
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                AssetDatabase.CreateFolder("Assets", "BuildingModel");
                AssetDatabase.CreateFolder("Assets/BuildingModel", "Materials");
            }
            else if (!AssetDatabase.IsValidFolder("Assets/BuildingModel/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/BuildingModel", "Materials");
            }
            
            AssetDatabase.CreateFolder("Assets/BuildingModel/Materials", "PhysicsMaterials");
        }
        
        // Create insulation materials
        CreateStandardMaterial(folderPath, "Glass Wool", "Insulation", 0.04f, 840f, 12f, 1.5f, Color.yellow);
        CreateStandardMaterial(folderPath, "Rock Wool", "Insulation", 0.037f, 1030f, 45f, 1.5f, new Color(0.7f, 0.4f, 0.3f));
        CreateStandardMaterial(folderPath, "EPS", "Insulation", 0.035f, 1500f, 60f, 3.0f, Color.white);
        CreateStandardMaterial(folderPath, "XPS", "Insulation", 0.03f, 1500f, 100f, 3.2f, new Color(0.8f, 0.8f, 1.0f));
        CreateStandardMaterial(folderPath, "Polyurethane Foam", "Insulation", 0.025f, 1400f, 35f, 3.0f, new Color(1.0f, 0.9f, 0.7f));
        
        // Create wall materials
        CreateStandardMaterial(folderPath, "Brick", "Wall", 0.77f, 840f, 1700f, 10f, new Color(0.7f, 0.3f, 0.2f));
        CreateStandardMaterial(folderPath, "Concrete", "Concrete", 1.7f, 880f, 2300f, 120f, new Color(0.7f, 0.7f, 0.7f));
        CreateStandardMaterial(folderPath, "Lightweight Concrete", "Concrete", 0.38f, 840f, 1200f, 60f, new Color(0.8f, 0.8f, 0.8f));
        CreateStandardMaterial(folderPath, "Wood Frame", "Wood", 0.13f, 1600f, 500f, 50f, new Color(0.6f, 0.4f, 0.2f));
        CreateStandardMaterial(folderPath, "Steel Frame", "Metal", 50f, 450f, 7800f, 10000f, new Color(0.6f, 0.6f, 0.6f));
        
        // Create glazing materials
        CreateGlazingMaterial(folderPath, "Single Glazing", "Glass", 5.8f, 840f, 2500f, 10000f, new Color(0.9f, 0.95f, 1.0f, 0.5f));
        CreateGlazingMaterial(folderPath, "Double Glazing", "Glass", 2.8f, 840f, 2500f, 10000f, new Color(0.9f, 0.95f, 1.0f, 0.5f));
        CreateGlazingMaterial(folderPath, "Triple Glazing", "Glass", 0.8f, 840f, 2500f, 10000f, new Color(0.9f, 0.95f, 1.0f, 0.5f));
        CreateGlazingMaterial(folderPath, "Low-E Double Glazing", "Glass", 1.4f, 840f, 2500f, 10000f, new Color(0.85f, 0.9f, 1.0f, 0.5f));
        
        // Create roofing materials
        CreateStandardMaterial(folderPath, "Slate", "Roof", 2.0f, 760f, 2700f, 1000f, new Color(0.2f, 0.2f, 0.25f));
        CreateStandardMaterial(folderPath, "Clay Tile", "Roof", 1.0f, 800f, 2000f, 30f, new Color(0.8f, 0.4f, 0.3f));
        CreateStandardMaterial(folderPath, "Metal Roofing", "Roof", 50f, 450f, 7800f, 1500f, new Color(0.7f, 0.7f, 0.75f));
        CreateStandardMaterial(folderPath, "Green Roof", "Roof", 0.7f, 1000f, 1000f, 10f, new Color(0.2f, 0.6f, 0.3f));
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        LoadAllMaterials();
    }
    
    /// <summary>
    /// Creates a standard building material asset
    /// </summary>
    private void CreateStandardMaterial(string folderPath, string name, string category, 
                                       float conductivity, float specificHeat, float density,
                                       float vaporResistance, Color color)
    {
        // Check if material already exists
        string assetPath = $"{folderPath}/{name.Replace(" ", "_")}.asset";
        if (File.Exists(assetPath))
            return;
            
        BuildingPhysicsMaterial material = ScriptableObject.CreateInstance<BuildingPhysicsMaterial>();
        material.materialName = name;
        material.category = category;
        material.thermalConductivity = conductivity;
        material.specificHeatCapacity = specificHeat;
        material.density = density;
        material.waterVaporResistance = vaporResistance;
        material.materialColor = color;
        
        // Set economic properties based on category
        if (category == "Insulation")
        {
            material.costPerSquareMeter = 20f + (1f / conductivity) * 5f;
            material.embodiedCarbonPerKg = 2.5f;
            material.expectedLifespan = 30f;
        }
        else if (category == "Wall" || category == "Concrete")
        {
            material.costPerSquareMeter = 50f + density / 50f;
            material.embodiedCarbonPerKg = 0.2f;
            material.expectedLifespan = 50f;
        }
        else if (category == "Glass")
        {
            material.costPerSquareMeter = 150f;
            material.embodiedCarbonPerKg = 1.2f;
            material.expectedLifespan = 25f;
        }
        else if (category == "Roof")
        {
            material.costPerSquareMeter = 80f;
            material.embodiedCarbonPerKg = 1.5f;
            material.expectedLifespan = 40f;
        }
        else
        {
            material.costPerSquareMeter = 40f;
            material.embodiedCarbonPerKg = 1.0f;
            material.expectedLifespan = 35f;
        }
        
        AssetDatabase.CreateAsset(material, assetPath);
    }
    
    /// <summary>
    /// Creates a glazing material with known U-value
    /// </summary>
    private void CreateGlazingMaterial(string folderPath, string name, string category, 
                                      float uValue, float specificHeat, float density,
                                      float vaporResistance, Color color)
    {
        // For glazing we work backwards from U-value to get equivalent conductivity for 4mm glass
        float thickness = 0.004f; // 4mm glass
        float conductivity = thickness * uValue;
        
        CreateStandardMaterial(folderPath, name, category, conductivity, specificHeat, density, vaporResistance, color);
    }
}
#endif