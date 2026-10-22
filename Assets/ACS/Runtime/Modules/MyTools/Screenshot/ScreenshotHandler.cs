#if ACS_NATIVE_GALLERY
using ACS.Animations;
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

    private Coroutine captureRoutine, popupRoutine;
    private AnimationHandle popupMove, popupScale;
    private Texture2D ownedScreenshot;
    private int requestVersion;

    private void OnEnable()
    {
        if (popup != null) popup.gameObject.SetActive(false);
    }

    private void OnDisable() => StopScreenshot();

    private void StopScreenshot()
    {
        ++requestVersion;
        if (captureRoutine != null) StopCoroutine(captureRoutine);
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        captureRoutine = popupRoutine = null;
        popupMove?.Cancel(); popupScale?.Cancel();
        if (capturedPopup != null) capturedPopup.StopManaged();
        if (popup != null) popup.gameObject.SetActive(false);
        if (popupThumbnail != null && popupThumbnail.texture == ownedScreenshot) popupThumbnail.texture = null;
        if (ownedScreenshot != null) Destroy(ownedScreenshot);
        ownedScreenshot = null;
    }

    public void TakeScreenshot()
    {
        StopScreenshot();
        if (!isActiveAndEnabled) return;
        captureRoutine = StartCoroutine(CaptureLayeredScreenshot(requestVersion));
        AudioPlayer.Instance?.PlayOneShot(AudioType.Button);
    }

    private IEnumerator CaptureLayeredScreenshot(int version)
    {
        yield return new WaitForEndOfFrame();

        if (version != requestVersion || !isActiveAndEnabled) yield break;
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
        ownedScreenshot = screenShot;
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
            if (this == null || version != requestVersion || !isActiveAndEnabled) return;
            if (check) popupRoutine = StartCoroutine(ShowPopup(screenShot, version));
            else Debug.LogError("Failed to save screenshot to gallery: " + path);
        });
#else
        popupRoutine = StartCoroutine(ShowPopup(screenShot, version));
#endif

        captureRoutine = null;
        Debug.Log("Screenshot saved to: " + filepath);
    }

    IEnumerator ShowPopup(Texture2D screenshot, int version)
    {
        if (version != requestVersion || popup == null) yield break;
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
        popupMove = popup.AnimateAnchorPos(new Vector2(-250f, 0), 1f).SetOwner(this);
        popupScale = popup.AnimateScale(Vector3.one * 1.5f, 1f).SetOwner(this);

        // Show the captured popup
        if (capturedPopup != null) capturedPopup.Show();
        yield return new WaitForSeconds(3f);
        if (version != requestVersion || !isActiveAndEnabled) yield break;
        if (capturedPopup != null) capturedPopup.Hide();
        if (popupThumbnail != null)
            popupThumbnail.gameObject.SetActive(false);

        // Hide the popup after showing
        popup.gameObject.SetActive(false);
        popupRoutine = null;
    }
}
#endif
