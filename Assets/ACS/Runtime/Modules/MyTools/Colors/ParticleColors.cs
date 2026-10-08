using UnityEngine;

public class ParticleColors : MonoBehaviour
{
    private ParticleSystem[] particleSystems;

    private void Start()
    {
        // Find all ParticleSystem components in the children of this GameObject
        particleSystems = GetComponentsInChildren<ParticleSystem>();
        
    }

    public void SetColor(Color color)
    {
        // Loop through each ParticleSystem and set its main module's start color
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.startColor = color;
            if(ps.gameObject.name.Equals("GlowTrail"))
                {
                    var temp = color;
                    temp.a = 0.1372549f;
                    main.startColor = temp;
                }
        }
    }
}




