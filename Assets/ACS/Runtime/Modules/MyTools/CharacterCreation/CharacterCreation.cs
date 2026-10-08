using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ACS.SaveManager;
using TMPro;

public class CharacterCreation : MonoBehaviour
{

    [Header("Display Character Info")]
    public TextMeshProUGUI NameText;
    public Image ProfileImage;

    [Header("Character Creation")]
    public Button CreationBtn;
    public AnimationBase CreationPanel;
    [Space]
    public Transform iconButtonContainer; // Content of the ScrollView
    public GameObject iconButtonPrefab;   // A prefab with Image + Button
    public TMP_InputField nameInputField;
    public TextMeshProUGUI warningText;
    public TMP_Dropdown genderDropdown;
    public Button randomNameButton;
    public Button submitButton;

    public List<Sprite> profileIcons;

    private Gender selectedGender = Gender.Male;
    private int selectedIconIndex = -1;
    private List<Button> iconButtons = new List<Button>();

    void Start()
    {
        GetSavedData();

        CreationBtn.onClick.AddListener(() =>
        {
            CreationSetup();
        });
    }

    void GetSavedData()
    {
        if(!SaveManager.Prefs.GetBool(CharacterInfo.Default, false))
        {
            CreationSetup();
        }

        string name = SaveManager.Prefs.GetString(CharacterInfo.Name, "No_Name");
        int iconIndex =  SaveManager.Prefs.GetInt(CharacterInfo.ProfileIcon, 0);

        NameText.text = name;
        ProfileImage.sprite = profileIcons[iconIndex];
    }

    void CreationSetup()
    {
        CreationPanel.Show();
        CreationBtn.enabled = false;
        PopulateIconButtons();
        SetRandomName();

        genderDropdown.onValueChanged.AddListener(OnGenderChanged);
        randomNameButton.onClick.AddListener(SetRandomName);
        submitButton.onClick.AddListener(SubmitCharacter);
    }

    void OnGenderChanged(int index)
    {
        selectedGender = (Gender)index;
        SetRandomName();
    }

    void PopulateIconButtons()
    {
        for (int i = 0; i < profileIcons.Count; i++)
        {
            int index = i;
            GameObject buttonObj = Instantiate(iconButtonPrefab, iconButtonContainer);
            Image iconImage = buttonObj.transform.GetChild(0).GetComponent<Image>();
            iconImage.sprite = profileIcons[i];

            Button btn = buttonObj.GetComponent<Button>();
            btn.onClick.AddListener(() => SelectProfileIcon(index));
            iconButtons.Add(btn);
        }
    }

    void SelectProfileIcon(int index)
    {
        selectedIconIndex = index;
        if(ProfileImage) ProfileImage.sprite = profileIcons[index];

        for (int i = 0; i < iconButtons.Count; i++)
        {
            bool isSelected = i == selectedIconIndex;
            iconButtons[i].GetComponent<Image>().color = isSelected ? Color.green : Color.white;
        }

        Debug.Log("Selected Icon Index: " + index);
    }

    void SetRandomName()
    {
        string randomName = "Player";

        switch (selectedGender)
        {
            case Gender.Male:
                if (CharacterInfo.MaleNames.Count > 0)
                    randomName = CharacterInfo.MaleNames[Random.Range(0, CharacterInfo.MaleNames.Count)];
                break;

            case Gender.Female:
                if (CharacterInfo.FemaleNames.Count > 0)
                    randomName = CharacterInfo.FemaleNames[Random.Range(0, CharacterInfo.FemaleNames.Count)];
                break;

            case Gender.Other:
                List<string> combined = new List<string>(CharacterInfo.MaleNames);
                combined.AddRange(CharacterInfo.FemaleNames);
                if (combined.Count > 0)
                    randomName = combined[Random.Range(0, combined.Count)];
                break;
        }

        nameInputField.text = randomName;
    }

    void SubmitCharacter()
    {
        string characterName = nameInputField.text;
        if (string.IsNullOrEmpty(characterName) || selectedIconIndex == -1)
        {
            warningText.text = "Please select a name and profile icon.";
            return;
        }

        warningText.text = "";
        SaveManager.Prefs.SetInt( CharacterInfo.Gender, (int)selectedGender);
        SaveManager.Prefs.SetString(CharacterInfo.Name, characterName);
        SaveManager.Prefs.SetInt(CharacterInfo.ProfileIcon, selectedIconIndex);
        SaveManager.Prefs.SetBool(CharacterInfo.Default, true);

        CreationPanel.Hide();
        CreationBtn.enabled = true;
        GetSavedData();

        Debug.Log("Character saved: " + characterName);
    }
}

public static class CharacterInfo
{
    public static string Default = "Default";

    public static string Gender = "CharacterGender";
    public static string Name = "CharacterName";
    public static string ProfileIcon = "IconIndex";

    public static List<string> FemaleNames = new List<string>
    {
        "Alina", "Linda", "Emily", "Sophie", "Emma", "Olivia", "Isabella", "Mia", "Ella", "Grace",
        "Ava", "Charlotte", "Amelia", "Lily", "Hannah", "Abigail", "Chloe", "Zoe", "Nora", "Layla",
        "Victoria", "Scarlett", "Leah", "Riley", "Savannah", "Madison", "Claire", "Audrey", "Lucy", "Anna",
        "Natalie", "Evelyn", "Brooklyn", "Hazel", "Camila", "Penelope", "Ruby", "Ivy", "Naomi", "Aria",
        "Aaliyah", "Gabriella", "Bella", "Eliana", "Kylie", "Alice", "Sarah", "Luna", "Stella", "Hailey"
    };

    public static List<string> MaleNames = new List<string>
    {
        "Liam", "Noah", "Ethan", "James", "Lucas", "Mason", "Logan", "Jackson", "Aiden", "Elijah",
        "Alexander", "Benjamin", "Jacob", "Michael", "William", "Henry", "Daniel", "Matthew", "David", "Joseph",
        "Owen", "Samuel", "Sebastian", "Jack", "Jayden", "Luke", "Caleb", "Nathan", "Isaac", "Levi",
        "John", "Julian", "Anthony", "Grayson", "Andrew", "Thomas", "Joshua", "Gabriel", "Ryan", "Lincoln",
        "Ezra", "Hunter", "Adam", "Charles", "Christian", "Elias", "Jason", "Connor", "Miles", "Zachary"
    };

}

public enum Gender
{
    Male,
    Female,
    Other
}




