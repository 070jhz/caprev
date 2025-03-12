using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

/// <summary>
/// UI system for selecting materials in VR for building components
/// </summary>
public class MaterialSelectionUI : MonoBehaviour
{
    [Header("References")]
    public Transform materialButtonContainer;
    public GameObject materialButtonPrefab;
    public GameObject layerButtonPrefab;
    public Text componentNameText;
    public Text componentTypeText;
    
    [Header("Categories")]
    public string[] materialCategories;
    public Toggle[] categoryToggles;
    
    [Header("Layer UI")]
    public GameObject layerSelectionPanel;
    public Transform layerButtonContainer;
    public Button addLayerButton;
    public Button removeLayerButton;
    
    private BuildingComponent selectedComponent;
    private List<BuildingPhysicsMaterial> availableMaterials = new List<BuildingPhysicsMaterial>();
    private List<string> activeCategories = new List<string>();
    private int selectedLayerIndex = -1;
    
    void Start()
    {
        LoadAllMaterials();
        SetupCategoryToggles();
        HideUI();
    }
    
    /// <summary>
    /// Loads all material assets from the Resources folder
    /// </summary>
    void LoadAllMaterials()
    {
        // Find all material scriptable objects
        BuildingPhysicsMaterial[] materials = Resources.LoadAll<BuildingPhysicsMaterial>("BuildingMaterials");
        availableMaterials.AddRange(materials);
        
        Debug.Log($"Loaded {availableMaterials.Count} building materials");
    }
    
    /// <summary>
    /// Sets up category filter toggles
    /// </summary>
    void SetupCategoryToggles()
    {
        if (categoryToggles == null || categoryToggles.Length == 0)
            return;
            
        // Extract unique categories from materials
        HashSet<string> categories = new HashSet<string>();
        foreach (var material in availableMaterials)
        {
            categories.Add(material.category);
        }
        
        // Add "All" category
        categories.Add("All");
        
        // Create toggle for each category if needed
        if (categoryToggles.Length < categories.Count)
        {
            Debug.LogWarning("Not enough category toggles for all material categories");
        }
        
        // Setup available toggles
        int toggleIndex = 0;
        foreach (string category in categories)
        {
            if (toggleIndex >= categoryToggles.Length)
                break;
                
            Toggle toggle = categoryToggles[toggleIndex];
            Text toggleText = toggle.GetComponentInChildren<Text>();
            if (toggleText != null)
            {
                toggleText.text = category;
            }
            
            // Set initial state and hook up event
            toggle.isOn = category == "All";
            string toggleCategory = category; // Capture in closure
            
            toggle.onValueChanged.AddListener((isOn) => {
                OnCategoryToggleChanged(toggleCategory, isOn);
            });
            
            toggleIndex++;
        }
        
        // Initially all categories are active (via the "All" toggle)
        activeCategories.Add("All");
    }
    
    /// <summary>
    /// Shows the material selection UI for a specific component
    /// </summary>
    public void ShowForComponent(BuildingComponent component)
    {
        selectedComponent = component;
        gameObject.SetActive(true);
        
        // Update component info
        if (componentNameText != null)
        {
            componentNameText.text = component.name;
        }
        
        if (componentTypeText != null)
        {
            componentTypeText.text = component.ifcType;
        }
        
        // Show appropriate UI based on multi-layer or single-layer
        if (component.isMultiLayer)
        {
            ShowLayerSelectionUI();
        }
        else
        {
            ShowMaterialSelectionUI();
        }
    }
    
    /// <summary>
    /// Shows the material selection UI for a simple, single-layer component
    /// </summary>
    private void ShowMaterialSelectionUI()
    {
        // Hide layer UI if visible
        if (layerSelectionPanel != null)
        {
            layerSelectionPanel.SetActive(false);
        }
        
        // Clear existing buttons
        foreach (Transform child in materialButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Filter materials based on component type
        List<BuildingPhysicsMaterial> appropriateMaterials = FilterMaterialsForComponent(selectedComponent);
        
        // Create material buttons
        foreach (var material in appropriateMaterials)
        {
            GameObject buttonObj = Instantiate(materialButtonPrefab, materialButtonContainer);
            MaterialButton button = buttonObj.GetComponent<MaterialButton>();
            
            if (button != null)
            {
                button.Initialize(material, () => ApplyMaterial(material));
                
                // Highlight current material if it matches
                if (selectedComponent.currentMaterial == material)
                {
                    button.SetSelected(true);
                }
            }
        }
    }
    
    /// <summary>
    /// Shows the layer selection UI for a multi-layer component
    /// </summary>
    private void ShowLayerSelectionUI()
    {
        if (layerSelectionPanel != null)
        {
            layerSelectionPanel.SetActive(true);
        }
        
        // Clear existing layer buttons
        foreach (Transform child in layerButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons for each layer
        for (int i = 0; i < selectedComponent.materialLayers.Count; i++)
        {
            var layer = selectedComponent.materialLayers[i];
            GameObject buttonObj = Instantiate(layerButtonPrefab, layerButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            Text buttonText = button.GetComponentInChildren<Text>();
            
            if (buttonText != null)
            {
                buttonText.text = $"Layer {i+1}: {layer.name}";
                if (layer.material != null)
                {
                    buttonText.text += $"\n{layer.material.materialName}";
                }
            }
            
            int layerIndex = i; // Capture for closure
            button.onClick.AddListener(() => SelectLayer(layerIndex));
        }
        
        // Set button interactability
        if (addLayerButton != null)
        {
            addLayerButton.interactable = selectedComponent.materialLayers.Count < 5; // Limit to 5 layers
        }
        
        if (removeLayerButton != null)
        {
            removeLayerButton.interactable = selectedComponent.materialLayers.Count > 1; // Can't remove last layer
        }
        
        // No layer selected initially
        selectedLayerIndex = -1;
    }
    
    /// <summary>
    /// Selects a layer and shows materials for it
    /// </summary>
    public void SelectLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= selectedComponent.materialLayers.Count)
            return;
            
        selectedLayerIndex = layerIndex;
        
        // Clear existing material buttons
        foreach (Transform child in materialButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Filter materials based on layer position
        List<BuildingPhysicsMaterial> appropriateMaterials = FilterMaterialsForLayer(
            selectedComponent.materialLayers[layerIndex].layerOrder);
        
        // Create material buttons
        foreach (var material in appropriateMaterials)
        {
            GameObject buttonObj = Instantiate(materialButtonPrefab, materialButtonContainer);
            MaterialButton button = buttonObj.GetComponent<MaterialButton>();
            
            if (button != null)
            {
                button.Initialize(material, () => ApplyLayerMaterial(selectedLayerIndex, material));
                
                // Highlight current material if it matches
                if (selectedComponent.materialLayers[layerIndex].material == material)
                {
                    button.SetSelected(true);
                }
            }
        }
    }
    
    /// <summary>
    /// Adds a new layer to the component
    /// </summary>
    public void AddLayer()
    {
        if (selectedComponent == null || !selectedComponent.isMultiLayer)
            return;
            
        // Create a new layer
        BuildingComponent.MaterialLayer newLayer = new BuildingComponent.MaterialLayer
        {
            name = $"Layer {selectedComponent.materialLayers.Count + 1}",
            thickness = 0.05f,
            layerOrder = selectedComponent.materialLayers.Count,
            material = null
        };
        
        selectedComponent.materialLayers.Add(newLayer);
        
        // Refresh UI
        ShowLayerSelectionUI();
    }
    
    /// <summary>
    /// Removes the currently selected layer
    /// </summary>
    public void RemoveLayer()
    {
        if (selectedComponent == null || !selectedComponent.isMultiLayer ||
            selectedLayerIndex < 0 || selectedLayerIndex >= selectedComponent.materialLayers.Count)
            return;
            
        // Don't remove the last layer
        if (selectedComponent.materialLayers.Count <= 1)
            return;
            
        // Remove layer
        selectedComponent.materialLayers.RemoveAt(selectedLayerIndex);
        
        // Refresh UI
        ShowLayerSelectionUI();
    }
    
    /// <summary>
    /// Filters materials based on the component type
    /// </summary>
    private List<BuildingPhysicsMaterial> FilterMaterialsForComponent(BuildingComponent component)
    {
        List<BuildingPhysicsMaterial> result = new List<BuildingPhysicsMaterial>();
        
        // If active categories contains "All", include all materials
        bool includeAll = activeCategories.Contains("All");
        
        // Determine suitable categories based on IFC type
        string[] allowedCategories = GetAllowedCategoriesForType(component.ifcType);
        
        foreach (var material in availableMaterials)
        {
            // Filter by category toggle selection
            bool categoryMatch = includeAll || activeCategories.Contains(material.category);
            
            // Filter by compatibility with component type
            bool typeMatch = Array.IndexOf(allowedCategories, material.category) >= 0;
            
            if (categoryMatch && typeMatch)
            {
                result.Add(material);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Filters materials based on their suitability for a specific layer position
    /// </summary>
    private List<BuildingPhysicsMaterial> FilterMaterialsForLayer(int layerOrder)
    {
        List<BuildingPhysicsMaterial> result = new List<BuildingPhysicsMaterial>();
        
        // If active categories contains "All", include all materials
        bool includeAll = activeCategories.Contains("All");
        
        foreach (var material in availableMaterials)
        {
            // Filter by category toggle selection
            bool categoryMatch = includeAll || activeCategories.Contains(material.category);
            
            // Different filtering based on layer position
            bool layerMatch = true;
            
            // Outside layer (layerOrder = 0) should be weather-resistant
            if (layerOrder == 0)
            {
                layerMatch = material.category != "Insulation" || material.waterVaporResistance > 100;
            }
            // Middle layers good for insulation
            else if (layerOrder > 0 && layerOrder < selectedComponent.materialLayers.Count - 1)
            {
                layerMatch = true; // All materials can be used in middle layers
            }
            // Inside layer (last layer) should be suitable for interior
            else if (layerOrder == selectedComponent.materialLayers.Count - 1)
            {
                layerMatch = material.category != "Membrane" && material.category != "Roof";
            }
            
            if (categoryMatch && layerMatch)
            {
                result.Add(material);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Returns allowed material categories based on IFC element type
    /// </summary>
    private string[] GetAllowedCategoriesForType(string ifcType)
    {
        if (ifcType.Contains("IfcWall"))
        {
            return new string[] { "Wall", "Insulation", "Cladding", "Brick", "Concrete", "General" };
        }
        else if (ifcType.Contains("IfcWindow"))
        {
            return new string[] { "Window", "Glass", "Frame", "General" };
        }
        else if (ifcType.Contains("IfcSlab"))
        {
            return new string[] { "Floor", "Insulation", "Concrete", "General" };
        }
        else if (ifcType.Contains("IfcRoof"))
        {
            return new string[] { "Roof", "Insulation", "Membrane", "General" };
        }
        else if (ifcType.Contains("IfcDoor"))
        {
            return new string[] { "Door", "Wood", "Metal", "General" };
        }
        else if (ifcType.Contains("IfcColumn") || ifcType.Contains("IfcBeam"))
        {
            return new string[] { "Concrete", "Metal", "General" };
        }
        else
        {
            return new string[] { "General" };
        }
    }
    
    /// <summary>
    /// Applies a material to a single-layer component
    /// </summary>
    private void ApplyMaterial(BuildingPhysicsMaterial material)
    {
        if (selectedComponent != null)
        {
            selectedComponent.ChangeMaterial(material);
            
            // Refresh the UI to show the selected material
            ShowMaterialSelectionUI();
        }
    }
    
    /// <summary>
    /// Applies a material to a specific layer in a multi-layer component
    /// </summary>
    private void ApplyLayerMaterial(int layerIndex, BuildingPhysicsMaterial material)
    {
        if (selectedComponent != null && 
            layerIndex >= 0 && 
            layerIndex < selectedComponent.materialLayers.Count)
        {
            selectedComponent.ChangeLayerMaterial(layerIndex, material);
            
            // Refresh the UI to show the selected material
            SelectLayer(layerIndex);
        }
    }
    
    /// <summary>
    /// Handles category toggle changes
    /// </summary>
    private void OnCategoryToggleChanged(string category, bool isOn)
    {
        if (category == "All" && isOn)
        {
            // When "All" is selected, clear other categories
            activeCategories.Clear();
            activeCategories.Add("All");
            
            // Update other toggles
            foreach (var toggle in categoryToggles)
            {
                Text toggleText = toggle.GetComponentInChildren<Text>();
                if (toggleText != null && toggleText.text != "All")
                {
                    toggle.isOn = false;
                }
            }
        }
        else if (category == "All" && !isOn)
        {
            // Don't allow deselecting "All" without selecting something else
            if (activeCategories.Count <= 1)
            {
                // Re-enable the toggle
                foreach (var toggle in categoryToggles)
                {
                    Text toggleText = toggle.GetComponentInChildren<Text>();
                    if (toggleText != null && toggleText.text == "All")
                    {
                        toggle.isOn = true;
                        return;
                    }
                }
            }
            else
            {
                activeCategories.Remove("All");
            }
        }
        else if (isOn)
        {
            // Remove "All" if another category is selected
            activeCategories.Remove("All");
            
            // Add the new category
            if (!activeCategories.Contains(category))
            {
                activeCategories.Add(category);
            }
            
            // Update "All" toggle
            foreach (var toggle in categoryToggles)
            {
                Text toggleText = toggle.GetComponentInChildren<Text>();
                if (toggleText != null && toggleText.text == "All")
                {
                    toggle.isOn = false;
                }
            }
        }
        else
        {
            // Remove category when toggled off
            activeCategories.Remove(category);
            
            // If no categories selected, select "All"
            if (activeCategories.Count == 0)
            {
                activeCategories.Add("All");
                
                foreach (var toggle in categoryToggles)
                {
                    Text toggleText = toggle.GetComponentInChildren<Text>();
                    if (toggleText != null && toggleText.text == "All")
                    {
                        toggle.isOn = true;
                    }
                }
            }
        }
        
        // Refresh material list
        if (selectedComponent != null)
        {
            if (selectedComponent.isMultiLayer && selectedLayerIndex >= 0)
            {
                SelectLayer(selectedLayerIndex);
            }
            else
            {
                ShowMaterialSelectionUI();
            }
        }
    }
    
    /// <summary>
    /// Converts a component to multi-layer
    /// </summary>
    public void ConvertToMultiLayer()
    {
        if (selectedComponent == null || selectedComponent.isMultiLayer)
            return;
            
        // Convert to multi-layer
        selectedComponent.isMultiLayer = true;
        
        // Create initial layer with current material
        BuildingComponent.MaterialLayer initialLayer = new BuildingComponent.MaterialLayer
        {
            name = "Layer 1",
            material = selectedComponent.currentMaterial,
            thickness = selectedComponent.componentThickness,
            layerOrder = 0
        };
        
        selectedComponent.materialLayers.Clear();
        selectedComponent.materialLayers.Add(initialLayer);
        
        // Show layer UI
        ShowLayerSelectionUI();
    }
    
    /// <summary>
    /// Converts a multi-layer component to single-layer
    /// </summary>
    public void ConvertToSingleLayer()
    {
        if (selectedComponent == null || !selectedComponent.isMultiLayer)
            return;
            
        // Get the primary material (exterior layer)
        BuildingPhysicsMaterial primaryMaterial = null;
        float totalThickness = 0;
        
        foreach (var layer in selectedComponent.materialLayers)
        {
            if (layer.layerOrder == 0 && layer.material != null)
            {
                primaryMaterial = layer.material;
            }
            
            totalThickness += layer.thickness;
        }
        
        // Convert to single-layer
        selectedComponent.isMultiLayer = false;
        selectedComponent.currentMaterial = primaryMaterial;
        selectedComponent.componentThickness = totalThickness;
        
        // Show material selection UI
        ShowMaterialSelectionUI();
    }
    
    /// <summary>
    /// Hides the material selection UI
    /// </summary>
    public void HideUI()
    {
        selectedComponent = null;
        selectedLayerIndex = -1;
        gameObject.SetActive(false);
    }
}