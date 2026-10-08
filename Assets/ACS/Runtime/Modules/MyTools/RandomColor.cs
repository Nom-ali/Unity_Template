
using UnityEngine;
using UnityEngine.UI;

public class RandomColor : MonoBehaviour
{
    [SerializeField] private bool Self = true;
    [SerializeField] private bool LightColor = false;
    [SerializeField] private bool ApplyToChildren = false;

    [MyBox.ConditionalField(nameof(Self), true)]
    [SerializeField] private GameObject TargetObject;

    [SerializeField, Range(0, 255)] private int AlphaValue = 150;


    private void Start()
    {
        // Get Target
        TargetObject = Self == true ? gameObject : TargetObject;

        // GEt Random Color
        Color randomColor = GenerateRandomColor();
        
        //Apply Color
        SetColor(randomColor);
        SetChildrenColor(randomColor);
    }

    /// <summary>
    /// Apply Color
    /// </summary>
    /// <param name="randomColor">Color</param>
    void SetColor(Color randomColor)
    {
        if (TargetObject.TryGetComponent(out Image image))
            image.color = randomColor;
        else if (TargetObject.TryGetComponent(out SpriteRenderer spriteRenderer))
            spriteRenderer.color = randomColor;
        else if (TargetObject.TryGetComponent(out MeshRenderer meshRenderer))
        {
            foreach (var material in meshRenderer.materials)
                material.color = randomColor;
        }
    }

    /// <summary>
    /// Apply color tot he child found
    /// </summary>
    /// <param name="color">Color</param>
    private void SetChildrenColor(Color color)
    {
        if (ApplyToChildren)
        {
            Image[] children_Image = TargetObject.GetComponentsInChildren<Image>();
            if (children_Image != null & children_Image.Length > 0)
            {
                foreach (var chld in children_Image)
                {
                    chld.color = color;
                }
                return;
            }
                
            SpriteRenderer[] children_SpriteRender = TargetObject.GetComponentsInChildren<SpriteRenderer>();
            if (children_SpriteRender != null & children_SpriteRender.Length > 0)
            {
                foreach (var chld in children_SpriteRender)
                {
                    chld.color = color;
                }
                return;
            }
                
            MeshRenderer[] children_MeshRender = TargetObject.GetComponentsInChildren<MeshRenderer>();
            if (children_SpriteRender != null & children_SpriteRender.Length > 0)
            {
                foreach (var chld in children_MeshRender)
                {
                    foreach (var material in chld.materials)
                        material.color = color;
                }
                return;
            }
        }
    }

    // gerenrate rando color
    Color GenerateRandomColor()
    {
        Color color = Color.white;
        if (LightColor)
        {
            int radomIndex = Random.Range(0, 6);
            color = radomIndex switch
            {
                0 => new(Random.Range(0f, 1f), 1, 0, AlphaValue / 255f),
                1 => new(1, Random.Range(0f, 1f), 0, AlphaValue / 255f),
                2 => new(1, 0, Random.Range(0f, 1f), AlphaValue / 255f),
                3 => new(Random.Range(0f, 1f), 0, 1, AlphaValue / 255f),
                4 => new(0, Random.Range(0f, 1f), 1, AlphaValue / 255f),
                5 => new(0, 1, Random.Range(0f, 1f), AlphaValue / 255f),
                _ => Color.white,
            };
        }
        else
            color = new(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), AlphaValue / 255f);
        
        Debug.Log("Random Color: " + color);
        return color;
    }
}





