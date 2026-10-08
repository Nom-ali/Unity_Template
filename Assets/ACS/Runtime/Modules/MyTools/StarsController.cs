using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StarsController : MonoBehaviour
{
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private float delay = 0f;
    [SerializeField] private int numberOfRotations = 1;
    [SerializeField] private Vector3 targetRotation = new Vector3(0, 0, 360); 
    
    private Vector3 initialScale = Vector3.zero;
    private Vector3 targetScale = Vector3.one;

    private Image image => GetComponent<Image>();
    
    private void Start()
    {
        if(image)
            image.enabled = false;

        StartCoroutine(AnimateStar());
    }

    IEnumerator AnimateStar()
    {
        targetScale = transform.localScale;
        transform.localScale = initialScale;
        yield return new WaitForSecondsRealtime(delay);
        image.enabled = true;

        float elapsedTime = 0f;
        Quaternion initialRotation = transform.rotation;
        Quaternion targetFinalRotation = Quaternion.Euler(targetRotation);
        float totalRotationDegrees = numberOfRotations * 360f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Scale interpolation
            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);

            // Rotation interpolation
            float currentRotationDegrees = Mathf.Lerp(0, totalRotationDegrees, t);
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, currentRotationDegrees);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        transform.rotation = initialRotation * targetFinalRotation * Quaternion.Euler(0, 0, totalRotationDegrees);
    }
}




