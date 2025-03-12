using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Represents a building component with physical properties and materials.
/// Attached to each building element imported from IFC.
/// </summary>
public class BuildingComponent : MonoBehaviour
{
    [Header("IFC Information")]
    public string globalId;
    public string ifcType;
    public string elementName;
    public string storeyId;
    public string spaceId;
    
    [Header("Physical Structure")]
    public bool isMultiLayer = false;
    public float componentThickness = 0.1f;
    public BuildingPhysicsMaterial currentMaterial;
    public List<MaterialLayer> materialLayers = new List<MaterialLayer>();
    
    [Header("Runtime State")]
    public float surfaceTemperature = 20.0f;
    public float innerTemperature = 20.0f;
    public float moistureContent = 0.0f;
    public Dictionary<string, string> properties = new Dictionary<string, string>();
    
    [Header("Visualization")]
    public Material defaultMaterial;
    public Material highlightMaterial;
    public bool isHighlighted = false;
    
    // Events
    public event Action<BuildingComponent> OnMaterialChanged;
    
    // Cached renderer
    private Renderer componentRenderer;
    private Material originalMaterial;
    
    // Cached calculation results
    private float cachedUValue = 0;
    private bool needsRecalculation = true;
    
    [Serializable]
    public class MaterialLayer
    {
        public string name;
        public BuildingPhysicsMaterial material;
        public float thickness = 0.1f;
        public int layerOrder = 0;  // 0 = exterior, higher = more interior
        
        // Calculate thermal resistance for this layer
        public float GetThermalResistance()
        {
            if (material == null)
                return 0.1f; // Default low resistance
                
            return material.GetThermalResistance(thickness);
        }
    }
    
    void Awake()
    {
        componentRenderer = GetComponent<Renderer>();
        if (componentRenderer != null)
        {
            originalMaterial = componentRenderer.material;
        }
    }
    
    void Start()
    {
        UpdateVisuals();
    }
    
    /// <summary>
    /// Changes the material of a single-layer component
    /// </summary>
    public void ChangeMaterial(BuildingPhysicsMaterial newMaterial)
    {
        currentMaterial = newMaterial;
        needsRecalculation = true;
        
        if (!isMultiLayer)
        {
            // Single material component
            if (componentRenderer != null && newMaterial != null && newMaterial.renderMaterial != null)
            {
                originalMaterial = newMaterial.renderMaterial;
                UpdateVisuals();
            }
        }
        
        // Notify listeners about the material change
        OnMaterialChanged?.Invoke(this);
        
        // Broadcast to simulation
        BroadcastMaterialChange();
    }
    
    /// <summary>
    /// Changes the material of a specific layer in a multi-layer component
    /// </summary>
    public void ChangeLayerMaterial(int layerIndex, BuildingPhysicsMaterial newMaterial)
    {
        if (!isMultiLayer || layerIndex < 0 || layerIndex >= materialLayers.Count)
            return;
            
        materialLayers[layerIndex].material = newMaterial;
        needsRecalculation = true;
        
        // Update visuals for multi-layer
        UpdateVisuals();
        
        // Notify listeners about the material change
        OnMaterialChanged?.Invoke(this);
        
        // Broadcast to simulation
        BroadcastMaterialChange();
    }
    
    /// <summary>
    /// Updates the visual appearance of the component
    /// </summary>
    public void UpdateVisuals()
    {
        if (componentRenderer == null)
            return;
            
        if (isHighlighted)
        {
            componentRenderer.material = highlightMaterial;
            return;
        }
        
        if (!isMultiLayer && currentMaterial != null && currentMaterial.renderMaterial != null)
        {
            componentRenderer.material = currentMaterial.renderMaterial;
        }
        else if (isMultiLayer && materialLayers.Count > 0)
        {
            // For multi-layer, visualize the outermost layer
            var outerLayer = materialLayers[0];
            foreach (var layer in materialLayers)
            {
                if (layer.layerOrder < outerLayer.layerOrder)
                    outerLayer = layer;
            }
            
            if (outerLayer.material != null && outerLayer.material.renderMaterial != null)
            {
                componentRenderer.material = outerLayer.material.renderMaterial;
            }
        }
        else
        {
            componentRenderer.material = originalMaterial;
        }
    }
    
    /// <summary>
    /// Sets the highlight state of the component
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (componentRenderer == null) return;
        
        isHighlighted = highlight;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Calculates the total U-value (thermal transmittance) of the component
    /// </summary>
    public float GetUValue()
    {
        if (!needsRecalculation)
            return cachedUValue;
            
        if (!isMultiLayer)
        {
            // Single material
            if (currentMaterial != null)
            {
                cachedUValue = currentMaterial.GetUValue(componentThickness);
            }
            else
            {
                cachedUValue = 1.0f; // Default for unknown material
            }
        }
        else
        {
            // Calculate combined thermal resistance for all layers
            float totalResistance = 0;
            
            foreach (var layer in materialLayers)
            {
                if (layer.material != null)
                {
                    totalResistance += layer.material.GetThermalResistance(layer.thickness);
                }
            }
            
            cachedUValue = totalResistance > 0 ? 1.0f / totalResistance : 1.0f;
        }
        
        needsRecalculation = false;
        return cachedUValue;
    }
    
    /// <summary>
    /// Gets the total thickness of the component
    /// </summary>
    public float GetTotalThickness()
    {
        if (!isMultiLayer)
            return componentThickness;
            
        float totalThickness = 0;
        foreach (var layer in materialLayers)
        {
            totalThickness += layer.thickness;
        }
        
        return totalThickness > 0 ? totalThickness : componentThickness;
    }
    
    /// <summary>
    /// Calculates the surface area of the component for heat transfer calculations
    /// </summary>
    public float GetSurfaceArea()
    {
        // Try to calculate from mesh
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // Get the mesh data
            Vector3[] vertices = meshFilter.sharedMesh.vertices;
            int[] triangles = meshFilter.sharedMesh.triangles;
            
            // Calculate total surface area
            float area = 0f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Get triangle vertices
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];
                
                // Calculate area of this triangle
                Vector3 side1 = v2 - v1;
                Vector3 side2 = v3 - v1;
                Vector3 cross = Vector3.Cross(side1, side2);
                area += cross.magnitude * 0.5f;
            }
            
            // Convert from local to world scale
            area *= transform.lossyScale.x * transform.lossyScale.z;
            return area;
        }
        
        // Fallback: approximate from bounds
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            // Simplify to largest face area
            float xy = bounds.size.x * bounds.size.y;
            float xz = bounds.size.x * bounds.size.z;
            float yz = bounds.size.y * bounds.size.z;
            return Mathf.Max(xy, xz, yz);
        }
        
        return 1.0f; // Default value
    }
    
    /// <summary>
    /// Gets a property value from the component
    /// </summary>
    public string GetProperty(string propertyName, string defaultValue = "")
    {
        if (properties.TryGetValue(propertyName, out string value))
        {
            return value;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Sets a property value on the component
    /// </summary>
    public void SetProperty(string propertyName, string value)
    {
        properties[propertyName] = value;
    }
    
    /// <summary>
    /// Updates temperature based on surrounding conditions
    /// </summary>
    public void UpdateTemperature(float outsideTemp, float insideTemp, float timeStep)
    {
        // Simple model: surface temperature moves toward equilibrium between inside and outside
        float resistance = 1.0f / GetUValue();
        float conductivity = 1.0f / Mathf.Max(resistance, 0.01f);
        
        // Weight based on thermal properties
        surfaceTemperature = Mathf.Lerp(
            surfaceTemperature,
            (outsideTemp + insideTemp) / 2.0f,
            conductivity * timeStep * 0.1f
        );
        
        // Interior temperature changes more slowly
        innerTemperature = Mathf.Lerp(
            innerTemperature,
            (outsideTemp + insideTemp * 3.0f) / 4.0f, // Weighted toward inside
            (conductivity * 0.05f) * timeStep
        );
    }
    
    /// <summary>
    /// Broadcasts material changes to the simulation system
    /// </summary>
    private void BroadcastMaterialChange()
    {
        // Find the building simulation manager
        BuildingSimulationManager simManager = FindObjectOfType<BuildingSimulationManager>();
        if (simManager == null)
            return;
            
        // Create material data to send
        Dictionary<string, object> materialData = new Dictionary<string, object>();
        
        if (!isMultiLayer && currentMaterial != null)
        {
            materialData["materialName"] = currentMaterial.materialName;
            materialData["thermalConductivity"] = currentMaterial.thermalConductivity;
            materialData["density"] = currentMaterial.density;
            materialData["specificHeat"] = currentMaterial.specificHeatCapacity;
            materialData["uValue"] = GetUValue();
            materialData["thickness"] = componentThickness;
        }
        else if (isMultiLayer)
        {
            List<Dictionary<string, object>> layers = new List<Dictionary<string, object>>();
            
            foreach (var layer in materialLayers)
            {
                if (layer.material != null)
                {
                    Dictionary<string, object> layerData = new Dictionary<string, object>
                    {
                        {"materialName", layer.material.materialName},
                        {"thermalConductivity", layer.material.thermalConductivity},
                        {"density", layer.material.density},
                        {"specificHeat", layer.material.specificHeatCapacity},
                        {"thickness", layer.thickness},
                        {"layerOrder", layer.layerOrder}
                    };
                    
                    layers.Add(layerData);
                }
            }
            
            materialData["layers"] = layers;
            materialData["uValue"] = GetUValue();
            materialData["totalThickness"] = GetTotalThickness();
        }
        
        // Send to simulation manager
        simManager.OnComponentMaterialChanged(this, materialData);
    }
}