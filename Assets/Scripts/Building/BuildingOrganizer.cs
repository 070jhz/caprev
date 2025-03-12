using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

/// <summary>
/// Responsible for organizing and setting up building elements from IFC data in Unity.
/// This class processes the extracted IFC metadata and applies it to scene objects.
/// </summary>
public class BuildingOrganizer : MonoBehaviour
{
    [Header("IFC Data")]
    public TextAsset buildingMetadata;
    public Transform buildingRoot;
    
    [Header("Material Assignment")]
    public bool autoAssignMaterials = true;
    public string physicsMaterialsPath = "BuildingMaterials";
    
    [Header("Organization")]
    public bool createHierarchy = true;
    public bool addMissingColliders = true;
    
    // Runtime references
    private BuildingData data;
    private Dictionary<string, BuildingPhysicsMaterial> availableMaterials = new Dictionary<string, BuildingPhysicsMaterial>();
    
    void Start()
    {
        if (buildingMetadata != null)
        {
            LoadMaterialLibrary();
            ApplyMetadataToComponents();
        }
        else
        {
            Debug.LogError("Building metadata not assigned. Please assign the JSON file extracted from IFC.");
        }
    }
    
    /// <summary>
    /// Loads all available physics materials from Resources folder
    /// </summary>
    private void LoadMaterialLibrary()
    {
        BuildingPhysicsMaterial[] materials = Resources.LoadAll<BuildingPhysicsMaterial>(physicsMaterialsPath);
        foreach (var material in materials)
        {
            availableMaterials[material.materialName.ToLower()] = material;
        }
        
        Debug.Log($"Loaded {availableMaterials.Count} building physics materials");
    }
    
    /// <summary>
    /// Processes the building metadata and applies it to scene objects
    /// </summary>
    private void ApplyMetadataToComponents()
    {
        try
        {
            // Parse metadata
            data = JsonConvert.DeserializeObject<BuildingData>(buildingMetadata.text);
            Debug.Log($"Loaded building data with {data.components.Count} components, {data.spaces.Count} spaces, and {data.building_storeys.Count} storeys");
            
            // Create organizational hierarchy if requested
            if (createHierarchy)
            {
                CreateBuildingHierarchy();
            }
            
            // Add BuildingComponent to objects
            AddBuildingComponentsToElements();
            
            // Assign materials if requested
            if (autoAssignMaterials)
            {
                AssignDefaultMaterials();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error processing building metadata: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Creates a hierarchical structure in the scene matching the IFC spatial structure
    /// </summary>
    private void CreateBuildingHierarchy()
    {
        if (buildingRoot == null)
        {
            GameObject root = new GameObject("Building");
            buildingRoot = root.transform;
        }
        
        // Create GameObject for each storey
        foreach (var storeyEntry in data.building_storeys)
        {
            string storeyId = storeyEntry.Key;
            StoreyData storeyData = storeyEntry.Value;
            
            // Create or find storey GameObject
            GameObject storeyObject = GameObject.Find(storeyData.name);
            if (storeyObject == null)
            {
                storeyObject = new GameObject(storeyData.name);
                storeyObject.transform.SetParent(buildingRoot);
                
                // Set position based on elevation
                storeyObject.transform.position = new Vector3(0, storeyData.elevation, 0);
            }
            
            // Add custom component to store GlobalId
            StoreyIdentifier identifier = storeyObject.AddComponent<StoreyIdentifier>();
            identifier.globalId = storeyId;
            
            // Create GameObject for each space in this storey
            foreach (string spaceId in storeyData.contained_spaces)
            {
                if (data.spaces.TryGetValue(spaceId, out SpaceData spaceData))
                {
                    // Create or find space GameObject
                    string spaceName = string.IsNullOrEmpty(spaceData.long_name) ? spaceData.name : spaceData.long_name;
                    GameObject spaceObject = GameObject.Find(spaceName);
                    if (spaceObject == null)
                    {
                        spaceObject = new GameObject(spaceName);
                        spaceObject.transform.SetParent(storeyObject.transform);
                    }
                    
                    // Add custom component to store GlobalId
                    SpaceIdentifier spaceIdentifier = spaceObject.AddComponent<SpaceIdentifier>();
                    spaceIdentifier.globalId = spaceId;
                }
            }
        }
    }
    
    /// <summary>
    /// Adds BuildingComponent components to objects in the scene based on their IFC GlobalId
    /// </summary>
    private void AddBuildingComponentsToElements()
    {
        // Find all objects in the scene with IFC GlobalIds
        Dictionary<string, GameObject> elementMap = FindAllIfcElements();
        int componentsAdded = 0;
        
        foreach (var entry in data.components)
        {
            string globalId = entry.Key;
            ComponentData componentData = entry.Value;
            
            // Skip elements that don't match an object in the scene
            if (!elementMap.TryGetValue(globalId, out GameObject elementObject))
            {
                continue;
            }
            
            // Add BuildingComponent if not already present
            BuildingComponent buildingComponent = elementObject.GetComponent<BuildingComponent>();
            if (buildingComponent == null)
            {
                buildingComponent = elementObject.AddComponent<BuildingComponent>();
                componentsAdded++;
            }
            
            // Set the component properties
            buildingComponent.globalId = globalId;
            buildingComponent.elementName = componentData.name;
            buildingComponent.ifcType = componentData.type;
            buildingComponent.storeyId = componentData.storey_id;
            buildingComponent.spaceId = componentData.space_id;
            
            // Copy properties
            if (componentData.properties != null)
            {
                foreach (var prop in componentData.properties)
                {
                    buildingComponent.SetProperty(prop.Key, prop.Value);
                }
            }
            
            // Handle material layers if present
            if (componentData.materials != null && componentData.materials.Count > 0)
            {
                ProcessMaterialLayers(buildingComponent, componentData.materials);
            }
            
            // Reorganize in hierarchy if requested
            if (createHierarchy)
            {
                OrganizeInHierarchy(elementObject, componentData);
            }
            
            // Add collider if needed
            if (addMissingColliders && elementObject.GetComponent<Collider>() == null)
            {
                AddAppropriateCollider(elementObject, componentData.type);
            }
        }
        
        Debug.Log($"Added BuildingComponent to {componentsAdded} objects");
    }
    
    /// <summary>
    /// Processes material layers for a building component
    /// </summary>
    private void ProcessMaterialLayers(BuildingComponent component, List<MaterialData> materials)
    {
        // Clear existing layers
        component.materialLayers.Clear();
        
        // Sort materials by layer index if available
        materials.Sort((a, b) => (a.layer_index ?? 0).CompareTo(b.layer_index ?? 0));
        
        // Check if this should be a multi-layer component
        component.isMultiLayer = materials.Count > 1;
        
        // Calculate total thickness
        float totalThickness = 0;
        foreach (var materialData in materials)
        {
            totalThickness += materialData.thickness;
        }
        
        // Set component thickness
        component.componentThickness = totalThickness > 0 ? totalThickness : 0.1f;
        
        if (component.isMultiLayer)
        {
            // Add all layers
            int layerIndex = 0;
            foreach (var materialData in materials)
            {
                BuildingComponent.MaterialLayer layer = new BuildingComponent.MaterialLayer
                {
                    name = materialData.name ?? $"Layer {layerIndex+1}",
                    thickness = materialData.thickness,
                    layerOrder = materialData.layer_index ?? layerIndex,
                    material = FindMaterialByName(materialData.name)
                };
                
                component.materialLayers.Add(layer);
                layerIndex++;
            }
        }
        else if (materials.Count == 1)
        {
            // Single material component
            component.currentMaterial = FindMaterialByName(materials[0].name);
        }
    }
    
    /// <summary>
    /// Finds all objects in the scene with IFC GlobalIds
    /// </summary>
    private Dictionary<string, GameObject> FindAllIfcElements()
    {
        Dictionary<string, GameObject> elementMap = new Dictionary<string, GameObject>();
        
        // Check all objects in the scene with a MeshRenderer
        foreach (MeshRenderer renderer in GameObject.FindObjectsOfType<MeshRenderer>())
        {
            GameObject obj = renderer.gameObject;
            string globalId = ExtractGlobalIdFromObject(obj);
            
            if (!string.IsNullOrEmpty(globalId))
            {
                elementMap[globalId] = obj;
            }
        }
        
        Debug.Log($"Found {elementMap.Count} elements with IFC GlobalIds in the scene");
        return elementMap;
    }
    
    /// <summary>
    /// Extracts GlobalId from a GameObject using various methods
    /// </summary>
    private string ExtractGlobalIdFromObject(GameObject obj)
    {
        // Method 1: Check custom properties directly on the GameObject
        if (obj.TryGetComponent<MonoBehaviour>(out var monoBehaviour))
        {
            // Try direct property
            System.Reflection.PropertyInfo globalIdProperty = monoBehaviour.GetType().GetProperty("GlobalId");
            if (globalIdProperty != null)
            {
                string id = globalIdProperty.GetValue(monoBehaviour) as string;
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }
            
            // Try field
            System.Reflection.FieldInfo globalIdField = monoBehaviour.GetType().GetField("GlobalId");
            if (globalIdField != null)
            {
                string id = globalIdField.GetValue(monoBehaviour) as string;
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }
        }
        
        // Method 2: Check for GlobalId in the name (like "ElementName [GlobalId]")
        string name = obj.name;
        int startBracket = name.IndexOf('[');
        int endBracket = name.IndexOf(']');
        
        if (startBracket >= 0 && endBracket > startBracket)
        {
            return name.Substring(startBracket + 1, endBracket - startBracket - 1);
        }
        
        // Method 3: Check for GlobalId_Name pattern
        int underscore = name.IndexOf('_');
        if (underscore > 0 && underscore < name.Length - 1)
        {
            string potentialId = name.Substring(0, underscore);
            // IFC GlobalIds are typically 22 characters
            if (potentialId.Length == 22 && !potentialId.Contains(" "))
            {
                return potentialId;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Assigns default physics materials based on IFC type
    /// </summary>
    private void AssignDefaultMaterials()
    {
        BuildingComponent[] components = GameObject.FindObjectsOfType<BuildingComponent>();
        int materialsAssigned = 0;
        
        foreach (var component in components)
        {
            bool wasAssigned = false;
            
            // Skip if already has material
            if (component.isMultiLayer)
            {
                bool allLayersHaveMaterials = true;
                foreach (var layer in component.materialLayers)
                {
                    if (layer.material == null)
                    {
                        allLayersHaveMaterials = false;
                        break;
                    }
                }
                
                if (allLayersHaveMaterials)
                {
                    continue; // Skip if all layers already have materials
                }
            }
            else if (component.currentMaterial != null)
            {
                continue; // Skip if single-layer already has material
            }
            
            // Assign materials based on IFC type
            if (component.isMultiLayer)
            {
                // Multi-layer approach
                for (int i = 0; i < component.materialLayers.Count; i++)
                {
                    var layer = component.materialLayers[i];
                    if (layer.material == null)
                    {
                        // Find material by name first
                        layer.material = FindMaterialByName(layer.name);
                        
                        // If not found, assign default for this layer position
                        if (layer.material == null)
                        {
                            layer.material = GetDefaultMaterialForLayerInType(i, component.materialLayers.Count, component.ifcType);
                            if (layer.material != null)
                            {
                                wasAssigned = true;
                            }
                        }
                        else
                        {
                            wasAssigned = true;
                        }
                    }
                }
            }
            else
            {
                // Single material
                component.currentMaterial = GetDefaultMaterialForType(component.ifcType);
                if (component.currentMaterial != null)
                {
                    wasAssigned = true;
                }
            }
            
            if (wasAssigned)
            {
                // Update visuals
                component.UpdateVisuals();
                materialsAssigned++;
            }
        }
        
        Debug.Log($"Assigned default materials to {materialsAssigned} components");
    }
    
    /// <summary>
    /// Finds a physics material by name with various fallbacks
    /// </summary>
    private BuildingPhysicsMaterial FindMaterialByName(string materialName)
    {
        if (string.IsNullOrEmpty(materialName))
            return null;
            
        string lowerName = materialName.ToLower();
        
        // Try exact match
        if (availableMaterials.TryGetValue(lowerName, out BuildingPhysicsMaterial material))
        {
            return material;
        }
        
        // Try common abbreviations and variants
        Dictionary<string, string> commonVariants = new Dictionary<string, string>
        {
            {"concrete", "concrete"},
            {"conc", "concrete"},
            {"brick", "brick"},
            {"masonry", "brick"},
            {"glass", "glass"},
            {"glazing", "glass"},
            {"window", "glass"},
            {"steel", "steel"},
            {"metal", "steel"},
            {"aluminum", "aluminum"},
            {"aluminium", "aluminum"},
            {"timber", "wood"},
            {"wood", "wood"},
            {"insulation", "glasswool"},
            {"insul", "glasswool"},
            {"gypsum", "gypsum"},
            {"plaster", "gypsum"},
            {"drywall", "gypsum"},
            {"tile", "ceramictile"},
            {"ceramic", "ceramictile"},
            {"stone", "stone"},
            {"marble", "stone"},
            {"granite", "stone"},
            {"roof", "roof"}
        };
        
        foreach (var variant in commonVariants)
        {
            if (lowerName.Contains(variant.Key))
            {
                // Try to find material with standardized name
                if (availableMaterials.TryGetValue(variant.Value, out material))
                {
                    return material;
                }
                
                // Try to find any material in that category
                foreach (var entry in availableMaterials)
                {
                    if (entry.Value.category.ToLower() == variant.Value)
                    {
                        return entry.Value;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Returns a default material based on IFC type
    /// </summary>
    private BuildingPhysicsMaterial GetDefaultMaterialForType(string ifcType)
    {
        string lowerType = ifcType.ToLower();
        
        if (lowerType.Contains("ifcwall"))
        {
            return FindMaterialByCategory("Wall") ?? FindMaterialByCategory("Concrete");
        }
        else if (lowerType.Contains("ifcwindow"))
        {
            return FindMaterialByCategory("Glass");
        }
        else if (lowerType.Contains("ifcslab") && lowerType.Contains("floor"))
        {
            return FindMaterialByCategory("Floor") ?? FindMaterialByCategory("Concrete");
        }
        else if (lowerType.Contains("ifcslab") && lowerType.Contains("roof"))
        {
            return FindMaterialByCategory("Roof");
        }
        else if (lowerType.Contains("ifcslab"))
        {
            return FindMaterialByCategory("Concrete");
        }
        else if (lowerType.Contains("ifcdoor"))
        {
            return FindMaterialByCategory("Door") ?? FindMaterialByCategory("Wood");
        }
        else if (lowerType.Contains("ifccolumn") || lowerType.Contains("ifcbeam"))
        {
            return FindMaterialByCategory("Concrete") ?? FindMaterialByCategory("Metal");
        }
        else if (lowerType.Contains("ifcroof"))
        {
            return FindMaterialByCategory("Roof");
        }
        else if (lowerType.Contains("ifcstair"))
        {
            return FindMaterialByCategory("Concrete");
        }
        else if (lowerType.Contains("ifcrailing"))
        {
            return FindMaterialByCategory("Metal");
        }
        else if (lowerType.Contains("ifcfurnishingelement"))
        {
            return FindMaterialByCategory("Wood");
        }
        
        // Default fallback
        foreach (var material in availableMaterials.Values)
        {
            if (material.category == "General")
            {
                return material;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Returns a default material for a specific layer in a multi-layer component
    /// </summary>
    private BuildingPhysicsMaterial GetDefaultMaterialForLayerInType(int layerIndex, int totalLayers, string ifcType)
    {
        string lowerType = ifcType.ToLower();
        
        // Exterior wall layers
        if (lowerType.Contains("ifcwall"))
        {
            if (layerIndex == 0) // Exterior layer
            {
                return FindMaterialByCategory("Wall") ?? FindMaterialByCategory("Brick");
            }
            else if (layerIndex == totalLayers - 1) // Interior layer
            {
                return FindMaterialByCategory("Gypsum");
            }
            else // Middle layers
            {
                return FindMaterialByCategory("Insulation");
            }
        }
        // Roof layers
        else if (lowerType.Contains("ifcroof") || (lowerType.Contains("ifcslab") && lowerType.Contains("roof")))
        {
            if (layerIndex == 0) // Exterior layer
            {
                return FindMaterialByCategory("Roof");
            }
            else if (layerIndex == totalLayers - 1) // Interior layer
            {
                return FindMaterialByCategory("Gypsum");
            }
            else // Middle layers
            {
                return FindMaterialByCategory("Insulation");
            }
        }
        // Floor layers
        else if (lowerType.Contains("ifcslab") && lowerType.Contains("floor"))
        {
            if (layerIndex == 0) // Top layer
            {
                return FindMaterialByCategory("Floor");
            }
            else if (layerIndex == totalLayers - 1) // Bottom layer
            {
                return FindMaterialByCategory("Concrete");
            }
            else // Middle layers
            {
                return FindMaterialByCategory("Insulation");
            }
        }
        
        // Default case
        return GetDefaultMaterialForType(ifcType);
    }
    
    /// <summary>
    /// Finds a material by category
    /// </summary>
    private BuildingPhysicsMaterial FindMaterialByCategory(string category)
    {
        foreach (var material in availableMaterials.Values)
        {
            if (material.category.ToLower() == category.ToLower())
            {
                return material;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Adds the appropriate collider type based on element type
    /// </summary>
    private void AddAppropriateCollider(GameObject obj, string ifcType)
    {
        // Check if the object has a mesh
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }
        
        string lowerType = ifcType.ToLower();
        
        // Use mesh collider for most building elements
        if (lowerType.Contains("ifcwall") || 
            lowerType.Contains("ifcslab") || 
            lowerType.Contains("ifcroof") ||
            lowerType.Contains("ifcstair"))
        {
            if (obj.GetComponent<MeshCollider>() == null)
            {
                MeshCollider collider = obj.AddComponent<MeshCollider>();
                
                // Make stairs non-convex for proper stepping
                if (lowerType.Contains("ifcstair"))
                {
                    collider.convex = false;
                }
            }
        }
        // Use box collider for simpler elements
        else if (lowerType.Contains("ifccolumn") || 
                lowerType.Contains("ifcbeam") ||
                lowerType.Contains("ifcfurnishingelement") ||
                lowerType.Contains("ifcdoor") ||
                lowerType.Contains("ifcwindow"))
        {
            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<BoxCollider>();
            }
        }
    }
    
    /// <summary>
    /// Organizes an element in the building hierarchy
    /// </summary>
    private void OrganizeInHierarchy(GameObject elementObject, ComponentData componentData)
    {
        // Check if it should be under a storey or space
        if (!string.IsNullOrEmpty(componentData.space_id))
        {
            // Try to find space GameObject
            SpaceIdentifier[] spaces = FindObjectsOfType<SpaceIdentifier>();
            foreach (var space in spaces)
            {
                if (space.globalId == componentData.space_id)
                {
                    elementObject.transform.SetParent(space.transform);
                    return;
                }
            }
        }
        
        if (!string.IsNullOrEmpty(componentData.storey_id))
        {
            // Try to find storey GameObject
            StoreyIdentifier[] storeys = FindObjectsOfType<StoreyIdentifier>();
            foreach (var storey in storeys)
            {
                if (storey.globalId == componentData.storey_id)
                {
                    elementObject.transform.SetParent(storey.transform);
                    return;
                }
            }
        }
        
        // If no specific parent found, put under building root
        if (buildingRoot != null)
        {
            elementObject.transform.SetParent(buildingRoot);
        }
    }
    
    // Helper classes for hierarchy organization
    public class StoreyIdentifier : MonoBehaviour
    {
        public string globalId;
    }
    
    public class SpaceIdentifier : MonoBehaviour
    {
        public string globalId;
    }
    
    // Data classes for JSON deserialization
    [System.Serializable]
    public class BuildingData
    {
        public Dictionary<string, ProjectInfo> project_info = new Dictionary<string, ProjectInfo>();
        public Dictionary<string, StoreyData> building_storeys = new Dictionary<string, StoreyData>();
        public Dictionary<string, SpaceData> spaces = new Dictionary<string, SpaceData>();
        public Dictionary<string, ComponentData> components = new Dictionary<string, ComponentData>();
        public Dictionary<string, MaterialInfo> materials = new Dictionary<string, MaterialInfo>();
    }
    
    [System.Serializable]
    public class ProjectInfo
    {
        public string name;
        public string description;
        public string global_id;
    }
    
    [System.Serializable]
    public class StoreyData
    {
        public string name;
        public float elevation;
        public string global_id;
        public List<string> contained_spaces = new List<string>();
        public Dictionary<string, string> properties = new Dictionary<string, string>();
    }
    
    [System.Serializable]
    public class SpaceData
    {
        public string name;
        public string long_name;
        public string global_id;
        public string storey_id;
        public Dictionary<string, string> properties = new Dictionary<string, string>();
        public List<string> contained_elements = new List<string>();
        public List<BoundaryData> boundaries = new List<BoundaryData>();
    }
    
    [System.Serializable]
    public class BoundaryData
    {
        public string element_id;
        public string connection_type;
        public string internal_external;
    }
    
    [System.Serializable]
    public class ComponentData
    {
        public string name;
        public string type;
        public string global_id;
        public string storey_id;
        public string space_id;
        public Dictionary<string, string> properties = new Dictionary<string, string>();
        public List<MaterialData> materials = new List<MaterialData>();
        public Dictionary<string, object> geometry = new Dictionary<string, object>();
    }
    
    [System.Serializable]
    public class MaterialData
    {
        public string name;
        public string type;
        public float thickness;
        public int? layer_index;
    }
    
    [System.Serializable]
    public class MaterialInfo
    {
        public string name;
        public string global_id;
        public Dictionary<string, PropertyData> properties = new Dictionary<string, PropertyData>();
    }
    
    [System.Serializable]
    public class PropertyData
    {
        public object value;
        public string type;
    }
}