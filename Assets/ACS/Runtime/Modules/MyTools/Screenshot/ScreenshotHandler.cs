#if ACS_DOTWEEN && ACS_NATIVE_GALLERY
using DG.Tweening;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ScreenshotHandler : MonoBehaviour
{
    public AnimationBase capturedPopup;
    public RawImage popupThumbnail; // Assign this in Inspector
    public RectTransform popup;

    [Header("Screenshot Settings")]
    public LayerMask screenshotLayers = ~0; // Set this in Inspector
    public bool transparentBackground = true;
    public Color backgroundColor = Color.white; // Used if transparentBackground is false

    private void OnEnable()
    {
        popup.gameObject.SetActive(false);
    }

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureLayeredScreenshot());
        AudioPlayer.instance?.PlayOneShot(AudioType.Button);
    }

    private IEnumerator CaptureLayeredScreenshot()
    {
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;

        // Create a temporary camera for the screenshot
        GameObject camObj = new GameObject("ScreenshotCamera");
        Camera tempCamera = camObj.AddComponent<Camera>();

        // Copy settings from main camera (optional: add fallback)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            tempCamera.CopyFrom(mainCam);
        }

        // Only render specified layers
        tempCamera.cullingMask = screenshotLayers;

        // Set up background and clear flags
        if (transparentBackground)
        {
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.backgroundColor = new Color(0, 0, 0, 0); // Fully transparent
        }
        else
        {
            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundColor.a = 1f; // Ensure solid
            tempCamera.backgroundColor = backgroundColor;
        }

        // Setup RenderTexture with alpha for transparency
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        tempCamera.targetTexture = rt;

        // Render and read pixels
        tempCamera.Render();
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // Clean up
        tempCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(camObj);

        // Save image (PNG preserves transparency)
        byte[] bytes = screenShot.EncodeToPNG();
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"DressUp_{timestamp}.png";
        string filepath = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllBytes(filepath, bytes);

#if UNITY_ANDROID
        NativeGallery.SaveImageToGallery(filepath, "DressUpGame", filename, (check, path) =>
        {
            if (check)
            {
                StartCoroutine(ShowPopup(screenShot));
            }
            else
                Debug.LogError("Failed to save screenshot to gallery: " + path);
        });
#else
        StartCoroutine(ShowPopup(screenShot));
#endif

        Debug.Log("Screenshot saved to: " + filepath);
    }

    IEnumerator ShowPopup(Texture2D screenshot)
    {
        // Show texture in popup thumbnail
        if (popupThumbnail != null)
        {
            popupThumbnail.texture = screenshot;
            popupThumbnail.gameObject.SetActive(true);
        }

        // Setup default popup
        popup.anchoredPosition = new Vector2(-1000f, 0f);
        popup.localScale = Vector3.one * 10f;

        // Show the popup
        popup.gameObject.SetActive(true);
        Handheld.Vibrate();

        // Animate the popup
#if ACS_DOTWEEN
        popup.DOAnchorPos(new Vector2(-250f, 0), 1f);
        popup.DOScale(Vector3.one * 1.5f, 1f);

#else
        popup.anchoredPosition = new Vector2(-250f, 0);
        popup.localScale = Vector3.one * 1.5f;
#endif
        // Show the captured popup
        capturedPopup.Show();
        yield return new WaitForSeconds(3f);
        capturedPopup.Hide();
        if (popupThumbnail != null)
            popupThumbnail.gameObject.SetActive(false);

        // Hide the popup after showing
        popup.gameObject.SetActive(false);
    }
}
#endif
