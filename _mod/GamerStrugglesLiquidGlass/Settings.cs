using UnityEngine;

namespace GamerStrugglesLiquidGlass;

public static class Settings
{
    const string Prefix = "lg_";

    public static bool GodMode;
    public static bool InfiniteHp;
    public static bool AutoHeal;
    public static bool OneHitKill;
    public static bool InfiniteStamina;
    public static bool InfiniteDash;
    public static bool AutoCollect;

    public static bool EspEnabled;
    public static bool EspEnemies = true;
    public static bool EspItems = true;
    public static bool EspHealth = true;
    public static bool EspNames = true;
    public static bool EspTracers;
    public static bool EspLivingOnly = true;

    public static bool PauseGame;
    public static bool PauseOnOpen = true;
    public static bool LightTheme;
    public static bool ReducedMotion;

    public static float AutoHealRate = 10f;
    public static float DamageMultiplier = 1f;
    public static float SpeedMultiplier = 1f;
    public static float JumpMultiplier = 1f;
    public static float ExpMultiplier = 1f;
    public static float EspRange = 40f;
    public static float EspOpacity = 0.9f;
    public static float EspFontSize = 12f;
    public static float GameSpeed = 1f;
    public static float GlassOpacity = 0.92f;

    public static bool Dirty;

    public static void Touch() => Dirty = true;

    public static void Load()
    {
        try
        {
            GodMode = GetBool(nameof(GodMode));
            InfiniteHp = GetBool(nameof(InfiniteHp));
            AutoHeal = GetBool(nameof(AutoHeal));
            OneHitKill = GetBool(nameof(OneHitKill));
            InfiniteStamina = GetBool(nameof(InfiniteStamina));
            InfiniteDash = GetBool(nameof(InfiniteDash));
            AutoCollect = GetBool(nameof(AutoCollect));

            EspEnabled = GetBool(nameof(EspEnabled));
            EspEnemies = GetBool(nameof(EspEnemies), true);
            EspItems = GetBool(nameof(EspItems), true);
            EspHealth = GetBool(nameof(EspHealth), true);
            EspNames = GetBool(nameof(EspNames), true);
            EspTracers = GetBool(nameof(EspTracers));
            EspLivingOnly = GetBool(nameof(EspLivingOnly), true);

            PauseGame = GetBool(nameof(PauseGame));
            PauseOnOpen = GetBool(nameof(PauseOnOpen), true);
            LightTheme = GetBool(nameof(LightTheme));
            ReducedMotion = GetBool(nameof(ReducedMotion));

            AutoHealRate = GetFloat(nameof(AutoHealRate), 10f);
            DamageMultiplier = GetFloat(nameof(DamageMultiplier), 1f);
            SpeedMultiplier = GetFloat(nameof(SpeedMultiplier), 1f);
            JumpMultiplier = GetFloat(nameof(JumpMultiplier), 1f);
            ExpMultiplier = GetFloat(nameof(ExpMultiplier), 1f);
            EspRange = GetFloat(nameof(EspRange), 40f);
            EspOpacity = GetFloat(nameof(EspOpacity), 0.9f);
            EspFontSize = GetFloat(nameof(EspFontSize), 12f);
            GameSpeed = GetFloat(nameof(GameSpeed), 1f);
            GlassOpacity = GetFloat(nameof(GlassOpacity), 0.92f);
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogWarning("Could not load settings: " + ex.Message);
        }

        Clamp();
        Dirty = false;
    }

    public static void Save()
    {
        try
        {
            Clamp();
            SetBool(nameof(GodMode), GodMode);
            SetBool(nameof(InfiniteHp), InfiniteHp);
            SetBool(nameof(AutoHeal), AutoHeal);
            SetBool(nameof(OneHitKill), OneHitKill);
            SetBool(nameof(InfiniteStamina), InfiniteStamina);
            SetBool(nameof(InfiniteDash), InfiniteDash);
            SetBool(nameof(AutoCollect), AutoCollect);

            SetBool(nameof(EspEnabled), EspEnabled);
            SetBool(nameof(EspEnemies), EspEnemies);
            SetBool(nameof(EspItems), EspItems);
            SetBool(nameof(EspHealth), EspHealth);
            SetBool(nameof(EspNames), EspNames);
            SetBool(nameof(EspTracers), EspTracers);
            SetBool(nameof(EspLivingOnly), EspLivingOnly);

            SetBool(nameof(PauseGame), PauseGame);
            SetBool(nameof(PauseOnOpen), PauseOnOpen);
            SetBool(nameof(LightTheme), LightTheme);
            SetBool(nameof(ReducedMotion), ReducedMotion);

            SetFloat(nameof(AutoHealRate), AutoHealRate);
            SetFloat(nameof(DamageMultiplier), DamageMultiplier);
            SetFloat(nameof(SpeedMultiplier), SpeedMultiplier);
            SetFloat(nameof(JumpMultiplier), JumpMultiplier);
            SetFloat(nameof(ExpMultiplier), ExpMultiplier);
            SetFloat(nameof(EspRange), EspRange);
            SetFloat(nameof(EspOpacity), EspOpacity);
            SetFloat(nameof(EspFontSize), EspFontSize);
            SetFloat(nameof(GameSpeed), GameSpeed);
            SetFloat(nameof(GlassOpacity), GlassOpacity);

            PlayerPrefs.Save();
        }
        catch (System.Exception ex)
        {
            Plugin.Log?.LogWarning("Could not save settings: " + ex.Message);
        }

        Dirty = false;
    }

    public static void Reset()
    {
        GodMode = false;
        InfiniteHp = false;
        AutoHeal = false;
        OneHitKill = false;
        InfiniteStamina = false;
        InfiniteDash = false;
        AutoCollect = false;

        EspEnabled = false;
        EspEnemies = true;
        EspItems = true;
        EspHealth = true;
        EspNames = true;
        EspTracers = false;
        EspLivingOnly = true;

        PauseGame = false;
        PauseOnOpen = true;
        LightTheme = false;
        ReducedMotion = false;

        AutoHealRate = 10f;
        DamageMultiplier = 1f;
        SpeedMultiplier = 1f;
        JumpMultiplier = 1f;
        ExpMultiplier = 1f;
        EspRange = 40f;
        EspOpacity = 0.9f;
        EspFontSize = 12f;
        GameSpeed = 1f;
        GlassOpacity = 0.92f;

        Save();
    }

    static void Clamp()
    {
        AutoHealRate = Mathf.Clamp(AutoHealRate, 1f, 200f);
        DamageMultiplier = Mathf.Clamp(DamageMultiplier, 1f, 25f);
        SpeedMultiplier = Mathf.Clamp(SpeedMultiplier, 0.5f, 5f);
        JumpMultiplier = Mathf.Clamp(JumpMultiplier, 0.5f, 3f);
        ExpMultiplier = Mathf.Clamp(ExpMultiplier, 1f, 10f);
        EspRange = Mathf.Clamp(EspRange, 5f, 100f);
        EspOpacity = Mathf.Clamp(EspOpacity, 0.2f, 1f);
        EspFontSize = Mathf.Clamp(EspFontSize, 9f, 18f);
        GameSpeed = Mathf.Clamp(GameSpeed, 0.1f, 3f);
        GlassOpacity = Mathf.Clamp(GlassOpacity, 0.5f, 1f);
    }

    static bool GetBool(string key, bool fallback = false)
        => PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) != 0;

    static void SetBool(string key, bool value)
        => PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);

    static float GetFloat(string key, float fallback)
        => PlayerPrefs.GetFloat(Prefix + key, fallback);

    static void SetFloat(string key, float value)
        => PlayerPrefs.SetFloat(Prefix + key, value);
}
