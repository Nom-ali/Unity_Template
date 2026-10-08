using UnityEngine;
using UnityEditor;
public static class LabelStyles
{
    public static GUIStyle GetHeaderStyle(int fontSize = 14)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = fontSize
        };

        style.normal.textColor = Color.yellow;
        return style;
    }

    public static GUIStyle GetLabelStyle(int fontSize = 13)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Normal,
            fontSize = fontSize
        };

        style.normal.textColor = new Color(1f, 1f, 0.65f); // light yellow
        return style;
    }
}
