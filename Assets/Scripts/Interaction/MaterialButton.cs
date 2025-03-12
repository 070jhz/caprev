using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// UI component for a material selection button
/// </summary>
public class MaterialButton : MonoBehaviour
{
    [Header("UI Elements")]
    public Image thumbnailImage;
    public Text materialNameText;
    public Text categoryText;
    public Text propertyText;
    public Image selectionIndicator;
    
    private BuildingPhysicsMaterial material;
    private Action onClickCallback;
    
    /// <summary>
    /// Initializes the button with a material and callback
    /// </summary>
    public void Initialize(BuildingPhysicsMaterial materialData, Action callback)
    {
        material = materialData;
        onClickCallback = callback;
        
        // Set visual elements
        if (materialNameText != null)
        {
            materialNameText.text = material.materialName;
        }
        
        if (categoryText != null)
        {
            categoryText.text = material.category;
        }
        
        if (thumbnailImage != null)
        {
            if (material.thumbnail != null)
            {
                thumbnailImage.sprite = Sprite.Create(
                    material.thumbnail, 
                    new Rect(0, 0, material.thumbnail.width, material.thumbnail.height), 
                    Vector2.zero
                );
                thumbnailImage.color = Color.white;
            }
            else
            {
                // No thumbnail, use a color block instead
                thumbnailImage.sprite = null;
                thumbnailImage.color = material.materialColor;
            }
        }
        
        if (propertyText != null)
        {
            // Show U-value for a standard thickness (10cm)
            float uValue = material.GetUValue(0.1f);
            propertyText.text = $"U-Value: {uValue:F2} W/m²K\n" +
                               $"λ: {material.thermalConductivity:F3} W/mK";
        }
        
        // Initially not selected
        if (selectionIndicator != null)
        {
            selectionIndicator.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Sets the selected state of the button
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.gameObject.SetActive(selected);
        }
    }
    
    /// <summary>
    /// Called when the button is clicked
    /// </summary>
    public void OnButtonClick()
    {
        onClickCallback?.Invoke();
    }
}