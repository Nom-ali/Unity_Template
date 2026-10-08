using ACS.SaveManager;
using UnityEngine;
using UnityEngine.UI;

public class SimpleCharacterSelection : MonoBehaviour
{
    [SerializeField] public Button PlayBtn;
    [SerializeField] public Button[] charButtons;

    private void OnEnable()
    {
        PlayBtn.interactable = false; // Disable Play button initially
        AddListeners();
    }

    public void AddListeners()
    {
        for (int i = 0; i < charButtons.Length; i++)
        {
            Button btn  = charButtons[i];
            int index = i; // Capture the current index for the listener

            btn.onClick.RemoveAllListeners(); // Clear existing listeners
            btn.onClick.AddListener(() => 
            {
                SaveManager.Prefs.SetInt(SharedVariables.Character, index, true);
                PlayBtn.interactable = true;
                //PlayBtn.onClick.AddListener(ApplovinAD.Instance.ShowApplovin_Interstitial);
            });
        }
    }

    private void OnDisable()
    {
        foreach (Button btn in charButtons)
        {
            btn.onClick.RemoveAllListeners(); // Clear listeners when the object is disabled
        }   
    }
}




