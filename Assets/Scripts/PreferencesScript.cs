﻿using UnityEngine;
using System;
using System.Collections;

public class PreferencesScript : Singleton<PreferencesScript> {

    private const string FIRST_USE = "FirstUse";
    private const string TUTORIAL_COMPLETED = "TutCompleted";

    private void Awake() {
        // If first use
        if (PlayerPrefs.GetString(FIRST_USE) == "") {
            PlayerPrefs.SetString(COLOR_MODE, ColorMode.LIGHT.ToString());
            PlayerPrefs.SetString(THEME_MODE, "DefaultTheme");

            PlayerPrefs.SetString(EMOJI_NAME + "0", "smilingEmoji");
            PlayerPrefs.SetString(EMOJI_NAME + "1", "angryEmoji");
            PlayerPrefs.SetString(EMOJI_NAME + "2", "fistBumpEmoji");
            PlayerPrefs.SetString(EMOJI_NAME + "3", "thinkingEmoji");
            
            PlayerPrefs.SetInt(TUTORIAL_COMPLETED, 0);

            PlayerPrefs.SetString(FIRST_USE, "IMDEADINSIDEPLSHELPME");

            PlayerPrefs.SetInt(PLAYER_LEVEL, 1);
            PlayerPrefs.Save();
        }

        expBarScript = FindObjectOfType<ExpBarScript>();

        // Color mode
        currentMode = (ColorMode) Enum.Parse(typeof(ColorMode), PlayerPrefs.GetString(COLOR_MODE));
        currentTheme = ColorThemes.GetTheme(PlayerPrefs.GetString(THEME_MODE));
        UpdateSignResourceStrgColors();

        // Player level
        playerLevel = PlayerPrefs.GetInt(PLAYER_LEVEL);
        playerExp = PlayerPrefs.GetInt(PLAYER_EXP);

        for (int i = 2; i <= maxPlayerLevel; i++)
            expNeededForLevel[i] = ExpNeededForLevel(i);
    }

    public bool IsTutorialCompleted() {
        return PlayerPrefs.GetInt(TUTORIAL_COMPLETED) == 1;
    }
    public void SetTutorialToCompleted() { PlayerPrefs.SetInt(TUTORIAL_COMPLETED, 1); }

    private IEnumerator ExecuteAfterSeconds(float seconds, Action action) {
        yield return new WaitForSeconds(seconds);

        action.Invoke();
    }

    // ____________________________________LEVELS____________________________________

    private const string PLAYER_LEVEL = "PlayerLevel";
    private const string PLAYER_EXP = "PlayerExp";

    private int playerLevel;
    public int PlayerLevel { get { return playerLevel; } }

    private int playerExp;
    public int PlayerEXP { get { return playerExp; } }

    private ExpBarScript expBarScript;

    /// <summary>
    /// Return whether player has levelled up. If they did it automatically levels the player up.
    /// </summary>
    public bool AddEXP(int exp) {
        // If reached max player level
        if (playerLevel >= maxPlayerLevel) return false;

        // Animation
        expBarScript.AddExpAnimation(exp);

        playerExp += exp;

        if (playerExp > expNeededForLevel[playerLevel + 1]) {
            LevelUp();
            return true;
        }

        // At this point we know that we haven't levelled up
        expBarScript.UpdateCurrExp(playerExp, ExpForNextLevel(), false);

        PlayerPrefs.SetInt(PLAYER_EXP, playerExp);
        return false;
    }

    /// <summary>
    /// First pulls the exp bar down the adds exp. If it wasn't pulled down it will push it up
    /// </summary>
    /// <param name="exp"></param>
    public void PullExpBarThenAdd(int exp) {
        if (expBarScript.IsPulledDown) {
            AddEXP(exp);
        } else { 
            expBarScript.PullDownExpBar(new DG.Tweening.TweenCallback(() => {
                StartCoroutine(ExecuteAfterSeconds(0.2f, new Action(() => {
                    AddEXP(exp);

                    StartCoroutine(ExecuteAfterSeconds(2f, new Action(() => {
                        expBarScript.PushUpExpBar();
                    })));
                })));
            }));
        }
    }

    /// <summary>
    /// Levels up the player. Updates the levels and the exp as well. Also writes to playerprefs.
    /// </summary>
    private void LevelUp() {
        if (playerLevel >= maxPlayerLevel) return;

        playerExp = playerExp - expNeededForLevel[playerLevel + 1];
        playerLevel++;

        expBarScript.UpdateLevelUpTexts(playerLevel, ExpForNextLevel(), playerExp);

        PlayerPrefs.SetInt(PLAYER_LEVEL, playerLevel);
        PlayerPrefs.SetInt(PLAYER_EXP, playerExp);
    }

    private const int maxPlayerLevel = 30;
    public int MaxPlayerLevel { get { return maxPlayerLevel; } }

    /// <summary>
    /// Stores the calculated expneeded
    /// </summary>
    private int[] expNeededForLevel = new int[maxPlayerLevel + 1];
    /// <summary>
    /// Returns -1 if the level you given is not between 2 and the maxLevel (both included)
    /// </summary>
    public int ExpForLevel(int level) {
        if (level < 2 || level > maxPlayerLevel) return -1;

        return expNeededForLevel[level];
    }
    /// <summary>
    /// Returns how much exp is needed alles zusammen for the player to level up
    /// </summary>
    public int ExpForNextLevel() {
        return expNeededForLevel[playerLevel + 1];
    }
    /// <summary>
    /// Returns how much exp is left for the player to collect to level up
    /// </summary>
    public int ExpLeftForNextLevel() {
        return expNeededForLevel[playerLevel + 1] - playerExp;
    }

    /// <summary>
    /// This is only used for the first calculation at the start
    /// </summary>
    private int ExpNeededForLevel(int level) {
        if (level <= 1) return 0;

        if (level <= 7) {
            return 1000 + (level - 2) * 200;
        } else if (level <= maxPlayerLevel) {
            return 1600 + (int) (Mathf.Pow(level, 3f) * 1.5f);
        }

        return 0;
    }


    // _________________________Emojis_______________________________________________

    /// <summary>
    /// There are 4 emojis which can be chosen so after this you need to put 0...3
    /// </summary>
    private const string EMOJI_NAME = "EmojiName";

    public readonly int EMOJI_COUNT = 4;

    public string[] GetEmojiNames() {
        string[] s = new string[EMOJI_COUNT];
        for (int i = 0; i < s.Length; i++)
            s[i] = PlayerPrefs.GetString(EMOJI_NAME + i);

        return s;
    }

    public Sprite[] GetEmojiSprites() {
        Sprite[] s = new Sprite[EMOJI_COUNT];
        for (int i = 0; i < s.Length; i++)
            s[i] = EmojiSprites.GetEmoji(PlayerPrefs.GetString(EMOJI_NAME + i));

        return s;
    }

    public string GetEmojiNameInSlot(int slot) {
        return PlayerPrefs.GetString(EMOJI_NAME + slot);
    }

    public Sprite GetEmojiSpriteInSlot(int slot) {
        return EmojiSprites.GetEmoji(PlayerPrefs.GetString(EMOJI_NAME + slot));
    }

    public void SetEmojiInSlotTo(int slot, string name) {
        PlayerPrefs.SetString(EMOJI_NAME + slot, name);
    }


    // ______________________Color mode variables_________________________________

    private const string COLOR_MODE = "ColorMode";
    private const string THEME_MODE = "ThemeMode";

    public ColorMode currentMode;

    /// <summary>
    /// How long tha changing animation should take
    /// </summary>
    private const float changeDuration = 0.5f;

    /// <summary>
    /// Delegate used for color changes
    /// </summary>
    public delegate void OnColorChange(ColorMode mode, float time);
    /// <summary>
    /// When we subscribe to this we can be sure that the color in SignResourceScript has already been changed
    /// </summary>
    public static OnColorChange ColorChangeEvent;

    // _______________________Which colors are chosen in colormode____________________________
    public ColorTheme currentTheme;

    /// <summary>
    /// Delegate used for theme changes
    /// </summary>
    public delegate void OnThemeChange(ColorTheme newTheme, float time);
    /// <summary>
    /// Subscribe to get notification when theme changes
    /// When we subscribe to this we can be sure that the color in SignResourceScript has already been changed
    /// </summary>
    public static OnThemeChange ThemeChangeEvent;



    public void ChangeToColorMode(ColorMode mode) {
        currentMode = mode;
        PlayerPrefs.SetString(COLOR_MODE, currentMode.ToString());

        UpdateSignResourceStrgColors(); // First update colors because some delegate listeners use it for simplicity
        ColorChangeEvent(mode, changeDuration); // Call delaegateategateggatagegatge
    }

    public void ChangeToColorTheme(ColorTheme newTheme, string nameOfTheme) {
        currentTheme = newTheme;
        PlayerPrefs.SetString(THEME_MODE, nameOfTheme + "Theme");

        UpdateSignResourceStrgColors(); // First update colors because some delegate listeners use it for simplicity
        ThemeChangeEvent(newTheme, changeDuration);// Call delaegateategateggasdasdsfeewedscxycasaatagegatge
    }
	
    private void UpdateSignResourceStrgColors() {
        SignResourceStorage.ChangeToColorMode(currentTheme.GetXColorOfMode(currentMode), currentTheme.GetOColorOfMode(currentMode));
    }


    public enum ColorMode {
        DARK, LIGHT
    }

    /// <summary>
    /// Which color theme is chosen in ColorMode
    /// </summary>
    public struct ColorTheme {
        public Color xColorLight;
        public Color oColorLight;

        public Color xColorDark;
        public Color oColorDark;

        public string themeName;

        public ColorTheme(Color xColorLight, Color oColorLight, Color xColorDark, Color oColorDark, string themeName) {
            this.xColorDark = xColorDark;
            this.oColorDark = oColorDark;
            this.xColorLight = xColorLight;
            this.oColorLight = oColorLight;
            this.themeName = themeName;
        }

        public Color GetXColorOfMode(ColorMode mode) {
            switch (mode) {
                case ColorMode.DARK: return xColorDark;
                case ColorMode.LIGHT: return xColorLight;
            }
            return Color.red;
        }

        public Color GetOColorOfMode(ColorMode mode) {
            switch (mode) {
                case ColorMode.DARK: return oColorDark;
                case ColorMode.LIGHT: return oColorLight;
            }
            return Color.blue;
        }
    }

}
