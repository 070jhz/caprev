using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Handles communication between building components and external simulation systems.
/// This manages the exchange of physical properties and simulation results.
/// </summary>
public class BuildingSimulationManager : MonoBehaviour
{
    [Header("Connection Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 8080;
    public bool autoConnect = true;
    
    [Header("Simulation")]
    public float outsideTemperature = 10.0f;
    public float outsideHumidity = 60.0f;
    public float windSpeed = 2.0f;
    public float simulationTimeScale = 1.0f;
    
    [Header("References")]
    public BuildingSimulationClient client;
    
    // Cached component references
    private Dictionary<string, BuildingComponent> componentRegistry = new Dictionary<string, BuildingComponent>();
    
    void Start()
    {
        // Initialize client if needed
        if (client == null)
        {
            client = GetComponent<BuildingSimulationClient>();
            
            // If still null, create a dummy client
            if (client == null && autoConnect)
            {
                client = gameObject.AddComponent<BuildingSimulationClient>();
            }
        }
        
        if (client != null && autoConnect)
        {
            client.serverIP = serverIP;
            client.serverPort = serverPort;
            
            if (autoConnect)
            {
                client.Connect();
            }
        }
        
        // Register all building components
        RegisterAllComponents();
    }
    
    /// <summary>
    /// Registers all building components in the scene for simulation
    /// </summary>
    private void RegisterAllComponents()
    {
        BuildingComponent[] components = FindObjectsOfType<BuildingComponent>();
        foreach (var component in components)
        {
            if (!string.IsNullOrEmpty(component.globalId))
            {
                componentRegistry[component.globalId] = component;
            }
        }
        
        Debug.Log($"BuildingSimulationManager registered {componentRegistry.Count} components");
    }
    
    /// <summary>
    /// Called when a component's material has changed
    /// </summary>
    public void OnComponentMaterialChanged(BuildingComponent component, Dictionary<string, object> materialData = null)
    {
        if (client == null || !client.connected)
            return;
            
        // Generate material data if not provided
        if (materialData == null)
        {
            materialData = new Dictionary<string, object>();
            
            if (!component.isMultiLayer && component.currentMaterial != null)
            {
                materialData["materialName"] = component.currentMaterial.materialName;
                materialData["thermalConductivity"] = component.currentMaterial.thermalConductivity;
                materialData["density"] = component.currentMaterial.density;
                materialData["specificHeat"] = component.currentMaterial.specificHeatCapacity;
                materialData["uValue"] = component.GetUValue();
                materialData["thickness"] = component.componentThickness;
            }
            else if (component.isMultiLayer)
            {
                List<Dictionary<string, object>> layers = new List<Dictionary<string, object>>();
                
                foreach (var layer in component.materialLayers)
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
                materialData["uValue"] = component.GetUValue();
                materialData["totalThickness"] = component.GetTotalThickness();
            }
        }
        
        // Create message to send to simulation
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "type", "COMPONENT_UPDATE" },
            { "componentId", component.globalId },
            { "data", materialData }
        };
        
        // Send message
        if (client != null && client.connected)
        {
            client.SendNetworkMessage(JsonConvert.SerializeObject(message));
        }
        
        Debug.Log($"Sent material update for component {component.name} ({component.globalId})");
    }
    
    /// <summary>
    /// Broadcasts the current environment state to the simulation
    /// </summary>
    public void BroadcastEnvironmentState(Dictionary<string, object> stateData)
    {
        if (client == null || !client.connected)
            return;
            
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "type", "ENVIRONMENT_UPDATE" },
            { "data", stateData }
        };
        
        client.SendNetworkMessage(JsonConvert.SerializeObject(message));
    }
    
    /// <summary>
    /// Broadcasts the state of a specific zone to the simulation
    /// </summary>
    public void BroadcastZoneState(string zoneId, Dictionary<string, object> zoneData)
    {
        if (client == null || !client.connected)
            return;
            
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "type", "ZONE_UPDATE" },
            { "zoneId", zoneId },
            { "data", zoneData }
        };
        
        client.SendNetworkMessage(JsonConvert.SerializeObject(message));
    }
    
    /// <summary>
    /// Updates a component's state with data from the simulation
    /// </summary>
    public void UpdateComponentState(string componentId, Dictionary<string, object> stateData)
    {
        if (!componentRegistry.TryGetValue(componentId, out BuildingComponent component))
            return;
            
        // Update component properties
        foreach (var entry in stateData)
        {
            component.SetProperty(entry.Key, entry.Value.ToString());
        }
        
        // Update temperatures if provided
        if (stateData.TryGetValue("surfaceTemperature", out object surfaceTempObj))
        {
            if (surfaceTempObj is float surfaceTemp)
            {
                component.surfaceTemperature = surfaceTemp;
            }
            else if (float.TryParse(surfaceTempObj.ToString(), out float parsedTemp))
            {
                component.surfaceTemperature = parsedTemp;
            }
        }
        
        if (stateData.TryGetValue("innerTemperature", out object innerTempObj))
        {
            if (innerTempObj is float innerTemp)
            {
                component.innerTemperature = innerTemp;
            }
            else if (float.TryParse(innerTempObj.ToString(), out float parsedTemp))
            {
                component.innerTemperature = parsedTemp;
            }
        }
    }
}