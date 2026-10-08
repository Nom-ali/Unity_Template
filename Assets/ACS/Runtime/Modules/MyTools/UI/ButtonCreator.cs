using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonCreator
{
    [Header("Assign in Inspector")]
    public GameObject buttonPrefab;      // Prefab with a Button + Image child
    public Transform parent;             // Parent to hold the buttons (e.g., a GridLayoutGroup)
    public List<Sprite> spriteList;      // Your list of sprites

    [Header("Unlock Settings")]
    public int defaultUnlockedIndex = 5; // The index that's unlocked by default

    private HashSet<int> sessionUnlocked = new HashSet<int>(); // Tracks session unlocks

  
    protected void CreateButtons()
    {
        for (int i = 0; i < spriteList.Count; i++)
        {
            int buttonIndex = i; // For closure

            // Instantiate button and set as child of parent
            GameObject btnObj = MonoBehaviour.Instantiate(buttonPrefab, parent);

            // Assume the sprite goes on the first child Image component
            Image childImage = btnObj.transform.GetChild(0).GetComponent<Image>();
            childImage.sprite = spriteList[i];

            Button btn = btnObj.GetComponent<Button>();

            // Check if button is unlocked (default or already unlocked this session)
            bool isUnlocked = (buttonIndex < defaultUnlockedIndex) || sessionUnlocked.Contains(buttonIndex);

            if (isUnlocked)
            {
                btn.interactable = true;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnButtonClicked(buttonIndex));
            }
            else
            {
                btn.interactable = true; // Keep interactable to allow unlock

                // Setup unlock logic
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (buttonIndex % 2 == 0)
                    {
                        // Even index: unlock with ad
                        ShowAd(() => UnlockButton(buttonIndex, btn));
                    }
                    else
                    {
                        // Odd index: unlock with coins
                        TryPurchaseWithCoins(() => UnlockButton(buttonIndex, btn));
                    }
                });
            }
        }
    }

    void OnButtonClicked(int index)
    {
        Debug.Log($"Button {index} clicked!");
        // Your logic for what happens when the button is used
    }

    void UnlockButton(int index, Button btn)
    {
        sessionUnlocked.Add(index);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnButtonClicked(index));
        // Optionally, visually indicate unlocked (e.g., remove a lock adsIcon)
        Debug.Log($"Button {index} unlocked for this session!");
    }

    void ShowAd(System.Action onComplete)
    {
        Debug.Log("Show rewarded ad here...");
        // Simulate ad success
        onComplete?.Invoke();
    }

    void TryPurchaseWithCoins(System.Action onComplete)
    {
        int coins = 100; // Replace with your coin system
        int price = 20;
        if (coins >= price)
        {
            // Deduct coins
            // coins -= price; // Implement your own coin logic
            onComplete?.Invoke();
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }
}




