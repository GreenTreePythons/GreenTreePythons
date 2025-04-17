using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "ShipSkinPartsColorInformation", menuName = "ScriptableObject/ShipSkinPartsColorInformation")]
public class ShipSkinPartsColorInformation : ScriptableObject
{
    [SerializeField] ShipSkinColorInfo[] m_Colors;

    public Color GetShipPartsColor(ShipSkinType skinType, ShipPartsColor color) 
        => GetColorInfo(skinType, color).GetColor();

    public Color GetShipPartsColorWithAlpha(ShipSkinType skinType, ShipPartsColor color) 
    {
        var colorInfo = GetColorInfo(skinType, color).GetColor();
        Color colorValue = new Color()
        {
            r = colorInfo.r,
            g = colorInfo.g,
            b = colorInfo.b,
            a = 255
        };
        return colorValue;
    }

    public ShipSkinColorInfo GetColorInfo(ShipSkinType skinType, ShipPartsColor color) 
        => m_Colors.Where(c => c.GetShipSkinType() == skinType).Single(c => c.GetShipPartsColor() == color);
}

[System.Serializable]
public class ShipSkinColorInfo
{
    [SerializeField] ShipSkinType m_ShipSkinType;
    [SerializeField] ShipPartsColor m_PartsColor;
    [ColorUsage(false, true)]
    [SerializeField] Color m_ColorValue;
    [SerializeField] string m_ColorPropertyName;

    public ShipSkinType GetShipSkinType() => m_ShipSkinType;
    public Color GetColor() => m_ColorValue;
    public ShipPartsColor GetShipPartsColor() => m_PartsColor;
    public string GetColorPropertyName() => m_ColorPropertyName;
}