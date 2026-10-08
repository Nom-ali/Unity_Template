using UnityEngine;

public class PopupManager : MonoBehaviour
{
    [SerializeField] private FloatingPopup Prefab;
    [SerializeField] private RectTransform ParentRect;

    private void Start()
    {
        InvokeRepeating(nameof(Testing), 1, 0.7f);
    }

    void Testing()
    {
        ShowPopup(Random.Range(0, 10001).ToString());
    }

    public void ShowPopup(string message, float duration)
    {
        FloatingPopup popup = Instantiate(Prefab, ParentRect);
        popup.SetupMessage(message, duration);
    }

    public void ShowPopup(string message)
    {
        FloatingPopup popup = Instantiate(Prefab, ParentRect);
        popup.SetupMessage(message);
    }
}




