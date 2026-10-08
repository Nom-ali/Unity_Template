using UnityEngine;

public class SharedVariables
{
    public static string Character = "Character";

    public static string Tutorial_1 = "Tutorial_1";
    public static string Tutorial_2 = "Tutorial_2";
    public static string DefaultSetting = "DefaultSetting";

    public static string Skin = "Skin_";
    public static string SkinMale = "SkinMale_";
    public static string SkinFemale = "SkinFemale_";
    public static string SelectedSkin = "SelectedSkin_";

    public static string FreeMode = "FreeMode";
    public static string SelectedCaseTypeID = "ModeIndex";
    public static string NightMode = "NightMode";

    public static string Level_ = "Level_";
    public static string RepeatingLevels = "RepeatingLevels";
    public static string UnlockedLevels = "UnlockedLevels";
    public static string CurrentLevelNo = "CurrentLevelNo";
    public static string RandomLevel = "RandomLevel";
    public static string GameCompleted = "GameCompleted";
    public static string LastPlayedLevel = "LastPlayedLevel";
    public static string BoltPopup = "BoltPopup";
    public static string Hammerpopup = "Hammerpopup";

    public static string Coins = "Coins";
    public static string HammerAmount = "HammerAmount";

    public static string Music = "Music";
    public static string MusicVolume = "MusicVolume";
    public static string Sound = "Sound";
    public static string SoundVolume = "SoundVolume";
    public static string Vibration = "Vibration";
    
    public static string RemoveAds = "RemoveAds";

    public static string WarningPopup = "WarningPopup";

    public static string SelectedPlayer = "SelectedPlayer_";
    public static string Player_Female = "Player_Female";

    public static string Splash = "1_Splash";
    public static string MainMenu = "2_MainMenu";
    public static string Gameplay = "3_Gameplay";

    public static string Revive = "Revive";
    public static string ReviveChances = "ReviveChances_";

    public static string NearestSpawnPoint = "NearestSpawnPoint_";
    
    public static string Unlocked= "Unlocked_";
    public static string Bundle = "Bundle_";

    internal static readonly string AnimalPopup = "AnimalPopup";
    internal static readonly string BannerTextureKey = "BannerTexture";

    internal static int NextScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
    internal static int PreviousScene => UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;

    /***************************************************************************************************************************************
                                                                Links
    //**************************************************************************************************************************************/
    public static string IOSRateUsLink = "https://apps.apple.com/us/app/royal-tower-smash-puzzle-game/id6808675421";
    public static string AndroidRateUsLink = "https://play.google.com/store/apps/details?id=com.zenith.royal.tower.smash.puzzle.physics.game";
    
    public static string AndroidMoreGamesLink = "https://play.google.com/store/apps/developer?id=Zenith+Games+Studio+LLC";
    public static string IOSMoreGamesLink = "https://apps.apple.com/us/developer/haachi-games-studio/id1825315156";
    
    public static string AndroidPrivacyPolicyLink = "https://zenithgamesstudio.com/privacy-policy/";
    public static string IOSPrivacyPolicyLink = "https://haachitech.com/game-privacy-policy";

    public enum Tags
    {
        None, Screw, Body,
        Player,
        DropPoint,
    }


}







