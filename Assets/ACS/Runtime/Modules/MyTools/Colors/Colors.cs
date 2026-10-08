using UnityEngine;

[CreateAssetMenu(fileName = "Colors", menuName = "Scriptable Objects/Colors")]
public class Colors : ScriptableObject
{

    [SerializeField] private ColorsData[] colorsDatas;

    public ColorsData GetColorFromList(int index)
    {
        if (index < 0 || index >= colorsDatas.Length)
        {
            Debug.LogError("Index out of range");
            return default;
        }
        return colorsDatas[index];
    }

    public Color GetColorFromList(ColorType colorType)
    {
        foreach (var colorData in colorsDatas)
        {
            if (colorData.colorType == colorType)
            {
                return colorData.color;
            }
        }
        return Color.white;
    }

    public static Color GetColor(ColorType colorType)
    {
        return colorType switch
        {
            ColorType.Red => Color.red,
            ColorType.Yellow => Color.yellow,
            ColorType.Blue => Color.blue,
            ColorType.Green => Color.green,
            ColorType.Purple => GetColorFromHex("#A020F0"),
            ColorType.Pink => new Color(252f / 255f, 15f / 255f, 192f / 255f), // Adjusted values
            ColorType.Orange => new Color(0.9568628f, 0.4392157f, 0.2470588f), // Adjusted values
            ColorType.Gray => Color.gray, // Adjusted values
            ColorType.LightBlue => new Color(0.372549f, 1f, 0.8862746f),
            ColorType.Parrot => new Color(0.7098039f, 1f, 0.2509804f),
            _ => Color.white,
        }; ;
    }

    public static Color GetColorFromHex(string hex)
    {
        Color color = Color.white;
        ColorUtility.TryParseHtmlString(hex, out color);
        return color;
    }
}

[System.Serializable]
public enum ColorType
{
    None, 
    Red, 
    Blue, 
    Pink, 
    Gray, 
    Green, 
    Black,
    White,
    Yellow, 
    Purple, 
    Orange, 
    Parrot, 
    LightBlue, 
    PurplePink, 

}

[System.Serializable]
public struct ColorsData
{
    public ColorType colorType;
    public Color color;
    public Sprite sprite;
}




