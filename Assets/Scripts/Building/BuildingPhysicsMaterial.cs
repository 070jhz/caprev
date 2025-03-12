using UnityEngine;

/// <summary>
/// ScriptableObject that defines the physical properties of a building material.
/// This includes thermal, moisture, and economic properties that will affect simulation.
/// </summary>
[CreateAssetMenu(fileName = "PhysicsMaterial", menuName = "Building/Physics Material")]
public class BuildingPhysicsMaterial : ScriptableObject
{
    [Header("Identification")]
    public string materialName = "New Material";
    public string category = "Generic"; // Wall, insulation, window, etc.
    
    [Header("Visual Properties")]
    public Material renderMaterial;
    public Texture2D thumbnail;
    public Color materialColor = Color.white;
    
    [Header("Thermal Properties")]
    [Tooltip("W/m·K - Lower is better insulation")]
    [Range(0.01f, 50f)]
    public float thermalConductivity = 1.0f;
    
    [Tooltip("J/kg·K - Higher means more thermal mass")]
    [Range(100f, 5000f)]
    public float specificHeatCapacity = 1000.0f;
    
    [Tooltip("kg/m³ - Density of material")]
    [Range(10f, 8000f)]
    public float density = 1000.0f;
    
    [Header("Moisture Properties")]
    [Tooltip("μ-value - Higher means more water resistant")]
    [Range(1f, 10000f)]
    public float waterVaporResistance = 10.0f;
    
    [Tooltip("% - Maximum moisture content by mass")]
    [Range(0f, 100f)]
    public float maxMoistureContent = 20.0f;
    
    [Header("Economic & Environmental Properties")]
    [Tooltip("Cost per square meter in currency units")]
    public float costPerSquareMeter = 50.0f;
    
    [Tooltip("kg CO2 per kg of material")]
    public float embodiedCarbonPerKg = 1.0f;
    
    [Tooltip("Years - Expected lifespan of material")]
    public float expectedLifespan = 30.0f;

    /// <summary>
    /// Calculates thermal resistance (R-value) for a given thickness
    /// </summary>
    /// <param name="thickness">Material thickness in meters</param>
    /// <returns>R-value in m²·K/W</returns>
    public float GetThermalResistance(float thickness)
    {
        // R = thickness(m) / conductivity(W/m·K)
        return thickness / Mathf.Max(thermalConductivity, 0.001f);
    }
    
    /// <summary>
    /// Calculates thermal transmittance (U-value) for a given thickness
    /// </summary>
    /// <param name="thickness">Material thickness in meters</param>
    /// <returns>U-value in W/m²·K</returns>
    public float GetUValue(float thickness)
    {
        // U = 1 / R
        return 1.0f / GetThermalResistance(thickness);
    }
    
    /// <summary>
    /// Calculates thermal mass for a given thickness
    /// </summary>
    /// <param name="thickness">Material thickness in meters</param>
    /// <param name="area">Surface area in square meters</param>
    /// <returns>Thermal mass in J/K</returns>
    public float GetThermalMass(float thickness, float area)
    {
        // Thermal mass = volume * density * specific heat capacity
        return area * thickness * density * specificHeatCapacity;
    }
    
    /// <summary>
    /// Calculates the total cost for a given area
    /// </summary>
    /// <param name="area">Surface area in square meters</param>
    /// <returns>Cost in currency units</returns>
    public float GetTotalCost(float area)
    {
        return area * costPerSquareMeter;
    }
    
    /// <summary>
    /// Calculates embodied carbon for given thickness and area
    /// </summary>
    /// <param name="thickness">Material thickness in meters</param>
    /// <param name="area">Surface area in square meters</param>
    /// <returns>Embodied carbon in kg CO2</returns>
    public float GetEmbodiedCarbon(float thickness, float area)
    {
        // Volume * density = mass, then multiply by carbon per kg
        return area * thickness * density * embodiedCarbonPerKg;
    }
}