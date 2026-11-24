using System;
using System.Collections.Generic;
using BepInEx;
using GamerStruggles;
using GamerStruggles.DamageSystem;
using GamerStruggles.Player;
using GamerStruggles.StateMachine;
using UnityEngine;
using Object = UnityEngine.Object;
using PlayerBB = GamerStruggles.Player.PlayerBlackboard;
using PlayerInput = GamerStruggles.Player.PlayerInputHandler;
using BaseEnemy = GamerStruggles.StateMachine.BaseEnemyController;

namespace GamerStrugglesLiquidGlass;

public class ModMenu : MonoBehaviour
{
    public static ModMenu Instance { get; private set; }

    const float DW = 920f;
    const float DH = 600f;
    const float HeaderH = 64f;
    const float CW = 680f;
    const float CH = 46f;
    const float RH = 56f;
    const float SH = 64f;
    const float BH = 48f;
    const float TH = 40f;
    const float CG = 12f;
    const float CB = 14f;
    const float ViewH = 412f;

    readonly Dictionary<string, Texture2D> toggleCache = new();
    readonly List<Texture2D> styleTex = new();
    readonly List<EnemyTarget> enemies = new();
    readonly List<ItemTarget> items = new();

    bool open;
    int tab;
    float scale = 1f;
    float openAnim;
    Vector2 winOffset;
    Vector2 scroll;
    bool dragging;
    float lastContentH;
    float lastSliderUse = -10f;
    float nextSave;
    float nextPlayerScan;
    float nextCheat;
    float nextScan;
    float nextInputScan;
    float fpsTimer;
    int fpsFrames;
    float fps = 60f;

    bool stylesDirty = true;
    float stylesScale = -1f;
    bool stylesLight;
    Theme theme;
    Font font;

    GUIStyle sHeaderTitle;
    GUIStyle sIcon;
    GUIStyle sTabActive;
    GUIStyle sTabInactive;
    GUIStyle sTabIconActive;
    GUIStyle sTabIconInactive;
    GUIStyle sKey;
    GUIStyle sBadge;
    GUIStyle sClose;
    GUIStyle sPageTitle;
    GUIStyle sPageSub;
    GUIStyle sCardTitle;
    GUIStyle sCardSub;
    GUIStyle sLabel;
    GUIStyle sSub;
    GUIStyle sValue;
    GUIStyle sSmall;
    GUIStyle sTiny;
    GUIStyle sToast;
    GUIStyle sEsp;
    GUIStyle sEspShadow;

    GUIStyle btnPrimary;
    GUIStyle btnSecondary;
    GUIStyle btnDanger;
    GUIStyle rowStyle;
    GUIStyle cardStyle;
    GUIStyle windowStyle;
    GUIStyle sidebarStyle;
    GUIStyle pillStyle;
    GUIStyle iconStyle;
    GUIStyle iconTileStyle;
    GUIStyle tabActiveStyle;
    GUIStyle shadowStyle;
    GUIStyle sliderBar;
    GUIStyle sliderThumb;

    Texture2D whiteTex;
    Texture2D circleTex;
    Texture2D blobTex;
    Texture2D trackTex;

    PlayerBB player;
    PlayerHealth hp;
    PlayerStaminaController stamina;
    PlayerMovementController movement;
    PlayerJumpController jump;
    PlayerDashController dash;
    PlayerExperience exp;
    Transform playerTransform;
    Rigidbody2D playerRb;
    PlayerInput playerInput;

    float hpMaxUi = 100f;
    float hpCurUi = 100f;
    PlayerHealth syncedHp;
    float healAccum;

    float speedBase = float.NaN;
    PlayerBB speedPlayer;
    bool speedApplied;
    float jumpBase = float.NaN;
    PlayerBB jumpPlayer;
    bool jumpApplied;
    float pickupBase = -1f;
    PlayerBB pickupPlayer;
    bool godApplied;
    bool staminaForced;

    bool pausedByMenu;
    float savedTimeScale = 1f;
    bool timeApplied;
    bool inputDisabledByMenu;
    bool inputWasEnabled;
    bool cursorSaved;
    bool prevCursorVisible;
    CursorLockMode prevCursorLock;

    Vector3? savedPos;
    string toast;
    float toastUntil;
    Color toastColor = Color.white;

    static readonly HashSet<string> Logged = new();

    struct EnemyTarget
    {
        public BaseEnemy Ctrl;
        public string Label;
        public float Height;
        public float Distance;
    }

    struct ItemTarget
    {
        public Collectable Ctrl;
        public string Label;
        public float Height;
        public float Distance;
    }

    void Awake()
    {
        Instance = this;
        hideFlags = HideFlags.HideAndDontSave;
        Settings.Load();
        theme = MakeTheme();
        EnsureFont();
        MakeBaseTextures();
    }

    void Update()
    {
        try
        {
            float now = Time.unscaledTime;
            UpdateFps();

            if (Settings.ReducedMotion)
                openAnim = open ? 1f : 0f;
            else
                openAnim = Mathf.MoveTowards(openAnim, open ? 1f : 0f, Time.unscaledDeltaTime * 7f);

            UpdateMenuPause();

            if (now >= nextPlayerScan)
            {
                nextPlayerScan = now + (player == null ? 1f : 3f);
                RefreshPlayer();
            }

            if (player != null)
            {
                ApplyHealth();
                if (now >= nextCheat)
                {
                    nextCheat = now + 0.1f;
                    ApplyStats();
                }
            }

            ApplyTime();

            if (open)
            {
                try
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                catch { }

                SyncHealth(false);
            }

            if ((open || Settings.EspEnabled) && now >= nextScan)
            {
                nextScan = now + 0.35f;
                ScanTargets();
            }

            if (Settings.Dirty && now >= nextSave)
            {
                nextSave = now + 2f;
                Settings.Save();
            }

            if (toast != null && now >= toastUntil)
                toast = null;
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "Update");
        }
    }

    void OnGUI()
    {
        try
        {
            GUI.depth = -10000;
            GUI.color = Color.white;

            Event e = Event.current;
            if (e == null) return;

            UpdateScaleAndTheme();
            Rect wr = WinRect();

            HandleHotkeys(e);
            HandleWindowInput(e, wr);
            if (open) wr = WinRect();

            if (Settings.EspEnabled)
                DrawEsp();

            if (open || openAnim > 0.001f)
                DrawMenu(e, wr);
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "OnGUI");
        }
    }

    void OnApplicationQuit()
    {
        try { Settings.Save(); } catch { }
        try { RestoreGameState(); } catch { }
    }

    void OnDestroy()
    {
        try { Settings.Save(); } catch { }
        try { RestoreGameState(); } catch { }

        foreach (Texture2D tex in styleTex)
            if (tex != null) Destroy(tex);
        styleTex.Clear();

        foreach (Texture2D tex in toggleCache.Values)
            if (tex != null) Destroy(tex);
        toggleCache.Clear();

        if (whiteTex != null) Destroy(whiteTex);
        if (circleTex != null) Destroy(circleTex);
        if (blobTex != null) Destroy(blobTex);

        if (Instance == this)
            Instance = null;
    }

    public void RestoreGameState()
    {
        try
        {
            if (pausedByMenu)
            {
                Time.timeScale = savedTimeScale;
                pausedByMenu = false;
            }

            if (timeApplied)
            {
                Time.timeScale = 1f;
                timeApplied = false;
            }

            RestoreCheatEffects();
            SetGameInputBlocked(false);

            if (cursorSaved)
            {
                Cursor.visible = prevCursorVisible;
                Cursor.lockState = prevCursorLock;
                cursorSaved = false;
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "RestoreGameState");
        }
    }

    void RestoreCheatEffects()
    {
        try
        {
            if (speedApplied && movement != null && !float.IsNaN(speedBase))
                movement.movementSpeed = speedBase;
            if (jumpApplied && jump != null && !float.IsNaN(jumpBase))
                jump.jumpForce = jumpBase;
            if (pickupBase >= 0f && exp != null && pickupPlayer != null)
                exp.pickupRadius = pickupBase;
            if (godApplied && hp != null && hp.IsInvincible)
                hp.SetInvincible(false);
            if (staminaForced && stamina != null)
                stamina.infiniteStamina = false;
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "RestoreCheatEffects");
        }

        speedApplied = false;
        jumpApplied = false;
        speedBase = float.NaN;
        jumpBase = float.NaN;
        pickupBase = -1f;
        pickupPlayer = null;
        godApplied = false;
        staminaForced = false;
    }

    void UpdateMenuPause()
    {
        try
        {
            if (open && Settings.PauseOnOpen && !pausedByMenu)
            {
                savedTimeScale = Time.timeScale;
                pausedByMenu = true;
                Time.timeScale = 0f;
            }
            else if ((!open || !Settings.PauseOnOpen) && pausedByMenu)
            {
                Time.timeScale = savedTimeScale;
                pausedByMenu = false;
            }

            if (open)
                SetGameInputBlocked(true);
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "UpdateMenuPause");
        }
    }

    void UpdateFps()
    {
        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f)
        {
            fps = fpsFrames / Mathf.Max(0.0001f, fpsTimer);
            fpsFrames = 0;
            fpsTimer = 0f;
        }
    }

    bool IsReady()
    {
        try { return player != null && hp != null; }
        catch { return false; }
    }

    void RefreshPlayer()
    {
        try
        {
            PlayerBB p = null;
            try { p = Object.FindFirstObjectByType<PlayerBB>(); } catch { }

            if (p == null)
            {
                if (player != null)
                {
                    SetGameInputBlocked(false);
                    ForgetMultiplierBases();
                    player = null;
                    hp = null;
                    stamina = null;
                    movement = null;
                    jump = null;
                    dash = null;
                    exp = null;
                    playerTransform = null;
                    playerRb = null;
                    playerInput = null;
                    syncedHp = null;
                }
                return;
            }

            if (p != player)
            {
                SetGameInputBlocked(false);
                ForgetMultiplierBases();
                player = p;
                BindComponents();
                SyncHealth(true);
                nextPlayerScan = Time.unscaledTime + 0.5f;
                return;
            }

            if (hp == null || stamina == null || movement == null || jump == null || dash == null || exp == null)
            {
                BindComponents();
                if (hp == null)
                    nextPlayerScan = Time.unscaledTime + 0.5f;
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "RefreshPlayer");
            player = null;
            hp = null;
        }
    }

    void BindComponents()
    {
        try { hp = player.PlayerHealth; } catch { hp = null; }
        try { stamina = player.PlayerStamina; } catch { stamina = null; }
        try { movement = player.PlayerMovementController; } catch { movement = null; }
        try { jump = player.PlayerJumpController; } catch { jump = null; }
        try { dash = player.PlayerDashController; } catch { dash = null; }
        try { exp = player.PlayerExperience; } catch { exp = null; }
        try { playerTransform = player.PlayerTransform; } catch { playerTransform = null; }
        try { playerRb = player.PlayerRigidbody; } catch { playerRb = null; }
        try { playerInput = player.GetComponentInChildren<PlayerInput>(); } catch { playerInput = null; }
    }

    void SetGameInputBlocked(bool blocked)
    {
        try
        {
            if (blocked)
            {
                if (playerInput == null && Time.unscaledTime >= nextInputScan)
                {
                    nextInputScan = Time.unscaledTime + 1f;
                    playerInput = Object.FindFirstObjectByType<PlayerInput>();
                }
                if (playerInput == null)
                {
                    inputDisabledByMenu = false;
                    return;
                }
                if (!inputDisabledByMenu || playerInput.AreInputsEnabled())
                {
                    inputWasEnabled = playerInput.AreInputsEnabled();
                    playerInput.TogglePlayerInputs(false);
                    inputDisabledByMenu = true;
                }
            }
            else if (inputDisabledByMenu)
            {
                inputDisabledByMenu = false;
                if (playerInput != null)
                    playerInput.TogglePlayerInputs(inputWasEnabled);
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "SetGameInputBlocked");
        }
    }

    void ForgetMultiplierBases()
    {
        speedBase = float.NaN;
        speedPlayer = null;
        speedApplied = false;
        jumpBase = float.NaN;
        jumpPlayer = null;
        jumpApplied = false;
        pickupBase = -1f;
        pickupPlayer = null;
        godApplied = false;
        staminaForced = false;
    }

    void ApplyHealth()
    {
        PlayerHealth h = hp;
        if (h == null) return;

        try
        {
            if (Settings.InfiniteHp)
            {
                if (h.CurrentHP != h.MaxHP)
                {
                    h.CurrentHP = h.MaxHP;
                    h.SendUpdateEvent();
                }
            }
            else if (Settings.AutoHeal && h.CurrentHP < h.MaxHP)
            {
                healAccum += Settings.AutoHealRate * Time.unscaledDeltaTime;
                int amount = (int)healAccum;
                if (amount > 0)
                {
                    healAccum -= amount;
                    h.Heal(amount);
                }
            }
            else
            {
                healAccum = 0f;
            }

            if (Settings.GodMode)
            {
                if (!h.IsInvincible)
                    h.SetInvincible(true);
                godApplied = true;
            }
            else if (godApplied)
            {
                if (h.IsInvincible)
                    h.SetInvincible(false);
                godApplied = false;
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "ApplyHealth");
            hp = null;
        }
    }

    void ApplyStats()
    {
        if (player == null) return;

        try
        {
            if (stamina != null)
            {
                if (Settings.InfiniteStamina)
                {
                    if (!stamina.infiniteStamina)
                    {
                        stamina.infiniteStamina = true;
                        staminaForced = true;
                    }
                    if (stamina.stamina < stamina.maxStamina)
                        stamina.stamina = stamina.maxStamina;
                }
                else if (staminaForced)
                {
                    stamina.infiniteStamina = false;
                    staminaForced = false;
                }
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "Stamina");
            stamina = null;
        }

        try
        {
            if (dash != null && Settings.InfiniteDash)
            {
                if (!dash.dashingEnabled)
                    dash.EnableDashing(true);
                dash.dashUsed = false;
                dash.isOnCooldown = false;
                dash.cooldownTimeRemaining = 0f;
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "Dash");
            dash = null;
        }

        ApplySpeed();
        ApplyJump();
        ApplyPickup();
    }

    void ApplySpeed()
    {
        if (movement == null) return;

        try
        {
            if (Mathf.Abs(Settings.SpeedMultiplier - 1f) < 0.001f)
            {
                if (speedApplied && !float.IsNaN(speedBase))
                    movement.movementSpeed = speedBase;
                speedApplied = false;
                return;
            }

            if (!speedApplied || speedPlayer != player || float.IsNaN(speedBase))
            {
                speedBase = movement.movementSpeed;
                speedPlayer = player;
                speedApplied = true;
            }

            float target = speedBase * Settings.SpeedMultiplier;
            if (Mathf.Abs(movement.movementSpeed - target) > 0.001f)
                movement.movementSpeed = target;
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "Speed");
            movement = null;
        }
    }

    void ApplyJump()
    {
        if (jump == null) return;

        try
        {
            if (Mathf.Abs(Settings.JumpMultiplier - 1f) < 0.001f)
            {
                if (jumpApplied && !float.IsNaN(jumpBase))
                    jump.jumpForce = jumpBase;
                jumpApplied = false;
                return;
            }

            if (!jumpApplied || jumpPlayer != player || float.IsNaN(jumpBase))
            {
                jumpBase = jump.jumpForce;
                jumpPlayer = player;
                jumpApplied = true;
            }

            float target = jumpBase * Settings.JumpMultiplier;
            if (Mathf.Abs(jump.jumpForce - target) > 0.001f)
                jump.jumpForce = target;
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "Jump");
            jump = null;
        }
    }

    void ApplyPickup()
    {
        if (exp == null) return;

        try
        {
            if (Settings.AutoCollect)
            {
                if (pickupPlayer != player)
                {
                    pickupBase = exp.pickupRadius;
                    pickupPlayer = player;
                }

                float target = Mathf.Max(pickupBase, 30f);
                if (exp.pickupRadius < target)
                    exp.pickupRadius = target;
            }
            else if (pickupPlayer != null && pickupBase >= 0f)
            {
                exp.pickupRadius = pickupBase;
                pickupBase = -1f;
                pickupPlayer = null;
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "Pickup");
            exp = null;
        }
    }

    void ApplyTime()
    {
        if (pausedByMenu) return;

        try
        {
            bool controlled = Settings.PauseGame || Mathf.Abs(Settings.GameSpeed - 1f) > 0.001f;
            if (controlled)
            {
                float target = Settings.PauseGame ? 0f : Mathf.Clamp(Settings.GameSpeed, 0.1f, 3f);
                if (Mathf.Abs(Time.timeScale - target) > 0.001f)
                    Time.timeScale = target;
                timeApplied = true;
            }
            else if (timeApplied)
            {
                if (Mathf.Abs(Time.timeScale - 1f) > 0.001f)
                    Time.timeScale = 1f;
                timeApplied = false;
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "ApplyTime");
        }
    }

    void SyncHealth(bool force)
    {
        PlayerHealth h = hp;
        if (h == null) return;

        try
        {
            bool switched = syncedHp != h;
            if (switched)
            {
                hpMaxUi = Mathf.Max(1f, h.MaxHP);
                hpCurUi = Mathf.Clamp(h.CurrentHP, 1f, Mathf.Max(1f, h.MaxHP));
                syncedHp = h;
                return;
            }

            if (!force && Time.unscaledTime < lastSliderUse + 0.3f)
                return;

            hpMaxUi = Mathf.Max(1f, h.MaxHP);
            hpCurUi = Mathf.Clamp(h.CurrentHP, 1f, Mathf.Max(1f, h.MaxHP));
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "SyncHealth");
            hp = null;
        }
    }

    void ApplyMaxHp()
    {
        PlayerHealth h = hp;
        if (h == null) return;

        try
        {
            int max = Mathf.Clamp(Mathf.RoundToInt(hpMaxUi), 10, 9999);
            hpMaxUi = max;
            hpCurUi = Mathf.Min(hpCurUi, max);

            if (h.MaxHP != max)
                h.SetMaxHP(max);

            if (Settings.InfiniteHp || Settings.GodMode)
            {
                hpCurUi = max;
                if (h.CurrentHP != max)
                {
                    h.CurrentHP = max;
                    h.SendUpdateEvent();
                }
            }
            else if (h.CurrentHP > max)
            {
                hpCurUi = max;
                h.CurrentHP = max;
                h.SendUpdateEvent();
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "ApplyMaxHp");
            hp = null;
        }
    }

    void ApplyCurrentHp()
    {
        PlayerHealth h = hp;
        if (h == null) return;

        try
        {
            int current = Mathf.Clamp(Mathf.RoundToInt(hpCurUi), 1, Mathf.Max(1, h.MaxHP));
            hpCurUi = current;
            if (h.CurrentHP != current)
            {
                h.CurrentHP = current;
                h.SendUpdateEvent();
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "ApplyCurrentHp");
            hp = null;
        }
    }

    void ScanTargets()
    {
        enemies.Clear();
        items.Clear();

        if (!open && !Settings.EspEnabled) return;

        try
        {
            foreach (BaseEnemy e in Object.FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None))
            {
                if (e == null) continue;

                try
                {
                    if (e.gameObject == null || !e.gameObject.activeInHierarchy) continue;

                    EnemyHealth eh = e.Health;
                    if (eh == null) continue;
                    if (Settings.EspLivingOnly && eh.IsDead()) continue;

                    float distance = DistanceTo(e.transform.position);
                    if (player != null && distance > Settings.EspRange) continue;

                    enemies.Add(new EnemyTarget
                    {
                        Ctrl = e,
                        Label = CleanName(e.name),
                        Height = MeasureHeight(e.gameObject, 1.4f),
                        Distance = distance
                    });
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "ScanEnemies");
        }

        try
        {
            foreach (Collectable c in Object.FindObjectsByType<Collectable>(FindObjectsSortMode.None))
            {
                if (c == null) continue;

                try
                {
                    if (c.gameObject == null || !c.gameObject.activeInHierarchy || c.IsCollected) continue;

                    float distance = DistanceTo(c.transform.position);
                    if (player != null && distance > Settings.EspRange) continue;

                    items.Add(new ItemTarget
                    {
                        Ctrl = c,
                        Label = ItemLabel(c),
                        Height = MeasureHeight(c.gameObject, 0.55f),
                        Distance = distance
                    });
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "ScanItems");
        }
    }

    float DistanceTo(Vector3 world)
    {
        try
        {
            if (playerTransform != null)
                return Vector3.Distance(playerTransform.position, world);
        }
        catch { }
        return 0f;
    }

    static float MeasureHeight(GameObject go, float fallback)
    {
        try
        {
            SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                float h = sr.bounds.size.y;
                if (h > 0.05f && h < 50f)
                    return h;
            }
        }
        catch { }
        return fallback;
    }

    static string CleanName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        int clone = name.IndexOf("(Clone)", StringComparison.Ordinal);
        if (clone > 0) name = name.Substring(0, clone);
        return name.Replace('_', ' ').Trim();
    }

    static string ItemLabel(Collectable c)
    {
        try
        {
            ExperienceOrb orb = c.TryCast<ExperienceOrb>();
            if (orb != null)
                return orb.exp > 1 ? "EXP +" + orb.exp : "EXP";

            KeyItem key = c.TryCast<KeyItem>();
            if (key != null)
                return "ChĂ¬a khoĂ¡";
        }
        catch { }
        return "Váº­t pháº©m";
    }

    void DrawEsp()
    {
        if (!Settings.EspEnabled || sEsp == null) return;
        if (Event.current == null || Event.current.type != EventType.Repaint) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        GUI.depth = -9000;
        GUI.color = Color.white;

        float alpha = Mathf.Clamp01(Settings.EspOpacity);
        int fontSize = Mathf.Clamp(Mathf.RoundToInt(Settings.EspFontSize * scale), 8, 40);
        if (sEsp.fontSize != fontSize || sEspShadow.fontSize != fontSize)
        {
            sEsp.fontSize = fontSize;
            sEspShadow.fontSize = fontSize;
        }

        bool drawEnemies = Settings.EspEnemies;
        bool drawItems = Settings.EspItems;

        if (drawEnemies)
        {
            foreach (EnemyTarget t in enemies)
            {
                try
                {
                    if (t.Ctrl == null || t.Ctrl.gameObject == null) continue;

                    Vector3 world = t.Ctrl.transform.position;
                    Vector3 screen = cam.WorldToScreenPoint(world);
                    if (screen.z <= 0f || float.IsNaN(screen.x) || float.IsNaN(screen.y)) continue;

                    float sx = screen.x;
                    float sy = Screen.height - screen.y;
                    float ppu = cam.orthographic
                        ? Screen.height / Mathf.Max(0.01f, cam.orthographicSize * 2f)
                        : Screen.height / Mathf.Max(0.01f, 2f * screen.z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));

                    float bh = Mathf.Clamp(t.Height * ppu, 8f * scale, 260f * scale);
                    float bw = bh * 0.55f;
                    Rect box = new Rect(sx - bw * 0.5f, sy - bh * 0.5f, bw, bh);
                    if (!OnScreen(box)) continue;

                    EnemyHealth eh = t.Ctrl.Health;
                    bool dead = false;
                    int curHp = 0;
                    int maxHp = 0;
                    try
                    {
                        if (eh != null)
                        {
                            dead = eh.IsDead();
                            curHp = eh.CurrentHP;
                            maxHp = eh.MaxHP;
                        }
                    }
                    catch { }

                    Color col = dead ? new Color(0.62f, 0.64f, 0.7f) : new Color(1f, 0.34f, 0.44f);

                    if (Settings.EspTracers && player != null)
                        DrawLine(new Vector2(Screen.width * 0.5f, Screen.height), new Vector2(sx, sy), new Color(col.r, col.g, col.b, 0.20f * alpha), Mathf.Max(1f, scale));

                    DrawEspBox(box, col, alpha);

                    bool showHealth = Settings.EspHealth && maxHp > 0;
                    if (showHealth)
                        DrawHealthBar(box, curHp, maxHp, alpha);

                    if (Settings.EspNames)
                    {
                        string text = t.Label;
                        if (t.Distance > 0f)
                            text += "  Â·  " + Mathf.RoundToInt(t.Distance) + "m";
                        float nameY = box.y - fontSize - 6f * scale - (showHealth ? 9f * scale : 0f);
                        DrawEspText(new Vector2(box.x, nameY), text, alpha);
                    }
                }
                catch { }
            }
        }

        if (drawItems)
        {
            foreach (ItemTarget t in items)
            {
                try
                {
                    if (t.Ctrl == null || t.Ctrl.gameObject == null || t.Ctrl.IsCollected) continue;

                    Vector3 screen = cam.WorldToScreenPoint(t.Ctrl.transform.position);
                    if (screen.z <= 0f || float.IsNaN(screen.x) || float.IsNaN(screen.y)) continue;

                    float sx = screen.x;
                    float sy = Screen.height - screen.y;
                    if (sx < -30f || sx > Screen.width + 30f || sy < -30f || sy > Screen.height + 30f) continue;

                    Color col = new Color(0.30f, 0.86f, 1f);
                    if (Settings.EspTracers && player != null)
                        DrawLine(new Vector2(Screen.width * 0.5f, Screen.height), new Vector2(sx, sy), new Color(col.r, col.g, col.b, 0.16f * alpha), Mathf.Max(1f, scale));

                    float d = Mathf.Max(5f * scale, 5f);
                    Matrix4x4 old = GUI.matrix;
                    GUIUtility.RotateAroundPivot(45f, new Vector2(sx, sy));
                    GUI.color = new Color(col.r, col.g, col.b, 0.95f * alpha);
                    GUI.DrawTexture(new Rect(sx - d * 0.5f, sy - d * 0.5f, d, d), whiteTex);
                    GUI.matrix = old;
                    GUI.color = Color.white;

                    if (Settings.EspNames)
                    {
                        string text = t.Label;
                        if (t.Distance > 0f)
                            text += "  Â·  " + Mathf.RoundToInt(t.Distance) + "m";
                        DrawEspText(new Vector2(sx + 10f * scale, sy - 8f * scale), text, alpha);
                    }
                }
                catch { }
            }
        }

        GUI.color = Color.white;
    }

    static bool OnScreen(Rect r)
        => r.xMax > -20f && r.x < Screen.width + 20f && r.yMax > -20f && r.y < Screen.height + 20f;

    void DrawEspBox(Rect r, Color col, float alpha)
    {
        GUI.color = new Color(col.r, col.g, col.b, 0.06f * alpha);
        GUI.DrawTexture(r, whiteTex);

        float w = Mathf.Max(1f, 1.5f * scale);
        Color c = new Color(col.r, col.g, col.b, 0.9f * alpha);
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, w), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - w, r.width, w), whiteTex);
        GUI.DrawTexture(new Rect(r.x, r.y, w, r.height), whiteTex);
        GUI.DrawTexture(new Rect(r.xMax - w, r.y, w, r.height), whiteTex);
        GUI.color = Color.white;
    }

    void DrawHealthBar(Rect box, int cur, int max, float alpha)
    {
        float h = Mathf.Max(3f, 4f * scale);
        Rect bg = new Rect(box.x, box.y - h - 5f * scale, box.width, h);
        GUI.color = new Color(0f, 0f, 0f, 0.6f * alpha);
        GUI.DrawTexture(bg, whiteTex);

        float ratio = Mathf.Clamp01(cur / (float)Mathf.Max(1, max));
        Color fill = ratio > 0.5f ? new Color(0.28f, 0.86f, 0.48f)
                    : ratio > 0.25f ? new Color(1f, 0.72f, 0.26f)
                    : new Color(1f, 0.3f, 0.3f);
        GUI.color = new Color(fill.r, fill.g, fill.b, 0.95f * alpha);
        GUI.DrawTexture(new Rect(bg.x, bg.y, bg.width * ratio, bg.height), whiteTex);
        GUI.color = Color.white;
    }

    void DrawEspText(Vector2 pos, string text, float alpha)
    {
        float f = sEsp.fontSize;
        float x = Mathf.Clamp(pos.x, 4f, Screen.width - 240f * scale);
        float y = Mathf.Clamp(pos.y, 2f, Screen.height - f - 2f);
        Rect r = new Rect(x, y, 240f * scale, f + 6f * scale);

        GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.5f, 1f, alpha));
        GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, sEspShadow);
        GUI.Label(r, text, sEsp);
        GUI.color = Color.white;
    }

    void DrawLine(Vector2 a, Vector2 b, Color color, float width)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        if (len < 1f) return;

        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, a);
        GUI.color = color;
        GUI.DrawTexture(new Rect(a.x, a.y, len, width), whiteTex);
        GUI.matrix = old;
        GUI.color = Color.white;
    }

    void HandleHotkeys(Event e)
    {
        if (e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.F1 || e.keyCode == KeyCode.Insert)
        {
            ToggleMenu();
            e.Use();
            return;
        }

        if (open && e.keyCode == KeyCode.Escape)
        {
            CloseMenu();
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.F6)
        {
            ActSavePosition();
            e.Use();
            return;
        }

        if (e.keyCode == KeyCode.F7)
        {
            ActLoadPosition();
            e.Use();
        }
    }

    void ToggleMenu()
    {
        if (open) CloseMenu();
        else OpenMenu();
    }

    void OpenMenu()
    {
        open = true;
        scroll = Vector2.zero;
        lastContentH = 0f;
        dragging = false;

        try
        {
            if (!cursorSaved)
            {
                prevCursorVisible = Cursor.visible;
                prevCursorLock = Cursor.lockState;
                cursorSaved = true;
            }
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        catch { }

        if (Settings.PauseOnOpen && !pausedByMenu)
        {
            try
            {
                savedTimeScale = Time.timeScale;
                pausedByMenu = true;
                Time.timeScale = 0f;
            }
            catch { }
        }

        if (Settings.ReducedMotion)
            openAnim = 1f;

        SetGameInputBlocked(true);
        SyncHealth(true);
    }

    void CloseMenu()
    {
        open = false;
        dragging = false;

        SetGameInputBlocked(false);
        try { Settings.Save(); } catch { }

        if (pausedByMenu)
        {
            try { Time.timeScale = savedTimeScale; } catch { }
            pausedByMenu = false;
        }

        if (cursorSaved)
        {
            try
            {
                Cursor.visible = prevCursorVisible;
                Cursor.lockState = prevCursorLock;
            }
            catch { }
            cursorSaved = false;
        }
    }

    void HandleWindowInput(Event e, Rect wr)
    {
        if (!open) return;

        Rect close = CloseRect(wr);

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (close.Contains(e.mousePosition))
                return;

            if (new Rect(wr.x, wr.y, wr.width, S(HeaderH)).Contains(e.mousePosition))
            {
                dragging = true;
                e.Use();
                return;
            }

            if (openAnim > 0.97f && !wr.Contains(e.mousePosition))
            {
                CloseMenu();
                e.Use();
                return;
            }
        }

        if (dragging)
        {
            if (e.type == EventType.MouseDrag)
            {
                winOffset += e.delta / Mathf.Max(0.01f, scale);
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                dragging = false;
                e.Use();
            }
        }
    }

    Rect CloseRect(Rect wr)
        => new Rect(wr.x + wr.width - S(52), wr.y + S(16), S(32), S(32));

    Rect WinRect()
    {
        float w = S(DW);
        float h = S(DH);
        float x = (Screen.width - w) * 0.5f + winOffset.x * scale;
        float y = (Screen.height - h) * 0.5f + winOffset.y * scale;

        float maxX = Mathf.Max(8f, Screen.width - w - 8f);
        float maxY = Mathf.Max(8f, Screen.height - h - 8f);
        float cx = Mathf.Clamp(x, 8f, maxX);
        float cy = Mathf.Clamp(y, 8f, maxY);

        if (Mathf.Abs(cx - x) > 0.01f)
            winOffset.x += (cx - x) / Mathf.Max(0.01f, scale);
        if (Mathf.Abs(cy - y) > 0.01f)
            winOffset.y += (cy - y) / Mathf.Max(0.01f, scale);

        return new Rect(cx, cy, w, h);
    }

    void UpdateScaleAndTheme()
    {
        float target = Mathf.Clamp(
            Mathf.Min(Screen.height / 720f, Screen.width / 1250f),
            0.7f,
            2.2f);

        if (stylesScale < 0f ||
            Mathf.Abs(stylesScale - target) > 0.002f ||
            stylesLight != Settings.LightTheme ||
            stylesDirty)
        {
            scale = target;
            theme = MakeTheme();
            BuildStyles();
            stylesDirty = false;
        }
    }

    void DrawMenu(Event e, Rect wr)
    {
        GUI.depth = -10000;

        GUI.color = new Color(theme.Backdrop.r, theme.Backdrop.g, theme.Backdrop.b, theme.Backdrop.a * openAnim);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTex);
        GUI.color = Color.white;

        GUI.color = new Color(1f, 1f, 1f, 0.32f * openAnim);
        GUI.Box(new Rect(wr.x - S(8), wr.y + S(8), wr.width + S(16), wr.height + S(22)), GUIContent.none, shadowStyle);
        GUI.color = Color.white;

        GUI.color = new Color(1f, 1f, 1f, Settings.GlassOpacity * openAnim);
        GUI.Box(wr, GUIContent.none, windowStyle);
        GUI.color = Color.white;

        GUI.color = new Color(1f, 1f, 1f, 0.10f * openAnim);
        GUI.DrawTexture(new Rect(wr.x + S(20), wr.y + S(1), wr.width - S(40), Mathf.Max(1f, S(1))), whiteTex);
        GUI.color = Color.white;

        DrawBlobs(wr);

        if (!open || openAnim < 0.75f) return;

        DrawHeader(e, wr);
        DrawSidebar(e, wr);
        DrawContent(wr);
        DrawToast(wr);
    }

    void DrawBlobs(Rect wr)
    {
        GUI.BeginGroup(wr);
        try
        {
            GUI.color = new Color(theme.Blob1.r, theme.Blob1.g, theme.Blob1.b, theme.Blob1.a * openAnim);
            GUI.DrawTexture(new Rect(S(45), S(80), S(430), S(430)), blobTex);

            GUI.color = new Color(theme.Blob2.r, theme.Blob2.g, theme.Blob2.b, theme.Blob2.a * openAnim);
            GUI.DrawTexture(new Rect(wr.width - S(380), wr.height - S(340), S(340), S(340)), blobTex);
        }
        finally
        {
            GUI.EndGroup();
            GUI.color = Color.white;
        }
    }

    void DrawHeader(Event e, Rect wr)
    {
        Rect icon = new Rect(wr.x + S(20), wr.y + S(14), S(36), S(36));
        GUI.Box(icon, GUIContent.none, iconStyle);
        GUI.Label(icon, "GS", sIcon);

        GUI.Label(new Rect(wr.x + S(66), wr.y + S(10), S(320), S(22)), "Gamer Struggles", sHeaderTitle);
        GUI.Label(new Rect(wr.x + S(66), wr.y + S(30), S(320), S(16)), "LIQUID GLASS  Â·  v" + PluginInfo.Version, sTiny);

        bool ready = IsReady();
        Rect pill = new Rect(wr.x + wr.width - S(260), wr.y + S(18), S(168), S(28));
        GUI.Box(pill, GUIContent.none, pillStyle);
        DrawDot(new Vector2(pill.x + S(14), pill.center.y), S(4), ready ? theme.Green : theme.Amber);
        GUI.Label(new Rect(pill.x + S(26), pill.y, pill.width - S(34), pill.height), ready ? "Äang hoáº¡t Ä‘á»™ng" : "ChÆ°a vĂ o game", sBadge);

        Rect close = CloseRect(wr);
        if (close.Contains(e.mousePosition))
        {
            GUI.color = new Color(1f, 1f, 1f, 0.16f);
            GUI.Box(close, GUIContent.none, iconTileStyle);
            GUI.color = Color.white;
        }
        GUI.Label(close, "Ă—", sClose);
        if (GUI.Button(close, GUIContent.none, GUIStyle.none))
            CloseMenu();

        GUI.color = new Color(theme.CardBorder.r, theme.CardBorder.g, theme.CardBorder.b, theme.CardBorder.a);
        GUI.DrawTexture(new Rect(wr.x + S(16), wr.y + S(63), wr.width - S(32), Mathf.Max(1f, S(1))), whiteTex);
        GUI.color = Color.white;
    }

    void DrawSidebar(Event e, Rect wr)
    {
        Rect sb = new Rect(wr.x + S(10), wr.y + S(72), S(200), S(518));
        GUI.Box(sb, GUIContent.none, sidebarStyle);

        GUI.color = theme.CardBorder;
        GUI.DrawTexture(new Rect(sb.xMax + S(6), sb.y + S(12), Mathf.Max(1f, S(1)), sb.height - S(24)), whiteTex);
        GUI.color = Color.white;

        string[] names = { "NhĂ¢n váº­t", "Hiá»ƒn thá»‹", "Tháº¿ giá»›i", "CĂ i Ä‘áº·t" };
        string[] icons = { "NV", "HT", "TG", "CD" };
        Color[] accents = { theme.Pink, theme.Accent, theme.Green, theme.Amber };

        for (int i = 0; i < 4; i++)
        {
            Rect tr = new Rect(sb.x + S(14), sb.y + S(18) + S(52) * i, S(172), S(46));
            bool active = tab == i;
            bool hover = !active && tr.Contains(e.mousePosition);

            if (active)
            {
                GUI.color = new Color(accents[i].r, accents[i].g, accents[i].b, 0.95f);
                GUI.Box(tr, GUIContent.none, tabActiveStyle);
                GUI.color = Color.white;
            }
            else if (hover)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.06f);
                GUI.Box(tr, GUIContent.none, iconTileStyle);
                GUI.color = Color.white;
            }

            Rect ir = new Rect(tr.x + S(10), tr.y + S(9), S(28), S(28));
            GUI.color = active ? new Color(1f, 1f, 1f, 0.20f) : new Color(1f, 1f, 1f, 0.07f);
            GUI.Box(ir, GUIContent.none, iconTileStyle);
            GUI.color = Color.white;

            GUI.Label(ir, icons[i], active ? sTabIconActive : sTabIconInactive);
            GUI.Label(new Rect(tr.x + S(48), tr.y, tr.width - S(52), tr.height), names[i], active ? sTabActive : sTabInactive);

            if (GUI.Button(tr, GUIContent.none, GUIStyle.none))
            {
                if (tab != i)
                {
                    tab = i;
                    scroll = Vector2.zero;
                    lastContentH = 0f;
                }
            }
        }

        Rect foot = new Rect(sb.x + S(14), sb.yMax - S(76), sb.width - S(28), S(58));
        GUI.Box(foot, GUIContent.none, pillStyle);

        Rect key = new Rect(foot.x + S(12), foot.y + S(10), S(28), S(20));
        GUI.color = new Color(1f, 1f, 1f, 0.10f);
        GUI.Box(key, GUIContent.none, iconTileStyle);
        GUI.color = Color.white;
        GUI.Label(key, "F1", sKey);
        GUI.Label(new Rect(key.xMax + S(8), foot.y + S(7), foot.width - S(50), S(22)), "Má»Ÿ / Ä‘Ă³ng", sSmall);

        DrawDot(new Vector2(foot.x + S(16), foot.y + S(41)), S(3.5f), IsReady() ? theme.Green : theme.Amber);
        GUI.Label(new Rect(foot.x + S(26), foot.y + S(31), foot.width - S(34), S(18)), IsReady() ? "Äang hoáº¡t Ä‘á»™ng" : "ChÆ°a vĂ o game", sTiny);
    }

    void DrawContent(Rect wr)
    {
        Rect content = new Rect(wr.x + S(222), wr.y + S(64), S(688), S(528));
        DrawPageHeader(content);

        Rect viewport = new Rect(content.x, content.y + S(80), content.width, S(ViewH));
        float maxScroll = Mathf.Max(0f, lastContentH - viewport.height);
        scroll.y = Mathf.Clamp(scroll.y, 0f, maxScroll);

        Rect viewRect = new Rect(0f, 0f, viewport.width, Mathf.Max(lastContentH, viewport.height));
        scroll = GUI.BeginScrollView(viewport, scroll, viewRect, false, false, GUIStyle.none, GUIStyle.none);

        try
        {
            float y = 0f;
            if (!IsReady())
            {
                DrawBanner(y);
                y += 54f;
            }

            switch (tab)
            {
                case 0: DrawPlayerTab(y); break;
                case 1: DrawVisualsTab(y); break;
                case 2: DrawWorldTab(y); break;
                default: DrawSettingsTab(y); break;
            }

            lastContentH = S(GetTabHeight(tab, IsReady()));
        }
        finally
        {
            GUI.EndScrollView();
        }

        DrawScrollbar(viewport);
        DrawFooter(content);
    }

    float GetTabHeight(int which, bool ready)
    {
        float start = ready ? 0f : 54f;
        switch (which)
        {
            case 0: return start + 468f + CG + 228f + CG + 404f + CG;
            case 1: return start + 396f + CG + 308f + CG;
            case 2: return start + 148f + CG + 212f + CG + 228f + CG + 148f + CG;
            default: return start + 292f + CG + 180f + CG + 228f + CG;
        }
    }

    void DrawPageHeader(Rect content)
    {
        string[] titles = { "NhĂ¢n váº­t", "Hiá»ƒn thá»‹", "Tháº¿ giá»›i", "CĂ i Ä‘áº·t" };
        string[] subs =
        {
            "MĂ¡u, sĂ¡t thÆ°Æ¡ng vĂ  di chuyá»ƒn",
            "ESP vĂ  lá»›p phá»§ thĂ´ng tin",
            "Dá»‹ch chuyá»ƒn, EXP vĂ  thá»i gian",
            "Giao diá»‡n, phĂ­m táº¯t vĂ  thĂ´ng tin"
        };

        GUI.Label(new Rect(content.x, content.y + S(4), content.width - S(220), S(32)), titles[tab], sPageTitle);
        GUI.Label(new Rect(content.x, content.y + S(35), content.width - S(220), S(18)), subs[tab], sPageSub);

        Color accent = TabAccent(tab);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(content.x, content.y + S(57), S(30), Mathf.Max(2f, S(3))), whiteTex);
        GUI.color = Color.white;

        if (IsReady())
        {
            GUI.Label(new Rect(content.x + content.width - S(210), content.y + S(9), S(210), S(20)),
                "HP " + Mathf.RoundToInt(hpCurUi) + " / " + Mathf.RoundToInt(hpMaxUi), sValue);
        }
        else
        {
            GUI.Label(new Rect(content.x + content.width - S(210), content.y + S(9), S(210), S(20)),
                "ChÆ°a vĂ o game", sSmall);
        }
    }

    void DrawBanner(float y)
    {
        Rect r = new Rect(0f, S(y), S(CW), S(42));
        GUI.Box(r, GUIContent.none, cardStyle);

        GUI.color = theme.Amber;
        GUI.DrawTexture(new Rect(r.x + S(14), r.y + S(12), Mathf.Max(2f, S(3)), S(18)), whiteTex);
        GUI.color = Color.white;

        GUI.Label(new Rect(r.x + S(26), r.y, r.width - S(40), r.height),
            "ChÆ°a tĂ¬m tháº¥y nhĂ¢n váº­t â€” hĂ£y vĂ o game Ä‘á»ƒ dĂ¹ng chá»©c nÄƒng.", sSub);
    }

    void DrawScrollbar(Rect vp)
    {
        if (lastContentH <= vp.height + 1f) return;

        float trackH = vp.height;
        float thumbH = Mathf.Max(S(30), trackH * (vp.height / lastContentH));
        float max = Mathf.Max(1f, lastContentH - vp.height);
        float t = Mathf.Clamp01(scroll.y / max);
        float y = vp.y + (trackH - thumbH) * t;

        GUI.color = new Color(1f, 1f, 1f, 0.05f);
        GUI.DrawTexture(new Rect(vp.xMax - S(5), vp.y, Mathf.Max(2f, S(3)), trackH), whiteTex);

        GUI.color = new Color(1f, 1f, 1f, 0.22f);
        GUI.DrawTexture(new Rect(vp.xMax - S(5), y, Mathf.Max(2f, S(3)), thumbH), whiteTex);
        GUI.color = Color.white;
    }

    void DrawFooter(Rect content)
    {
        float y = content.y + content.height - S(28);
        GUI.color = theme.CardBorder;
        GUI.DrawTexture(new Rect(content.x, y - S(6), content.width, Mathf.Max(1f, S(1))), whiteTex);
        GUI.color = Color.white;

        GUI.Label(new Rect(content.x, y, content.width - S(120), S(20)),
            "F1 / Insert  Má»Ÿ menu     Â·     F6  LÆ°u vá»‹ trĂ­     Â·     F7  Quay láº¡i", sTiny);
        GUI.Label(new Rect(content.x + content.width - S(110), y, S(110), S(20)),
            Mathf.RoundToInt(fps) + " FPS", sValue);
    }

    void DrawPlayerTab(float y)
    {
        float h1 = CH + CB + RH + RH + SH + SH + RH + SH + BH;
        DrawCard(y, h1, "Sinh tá»“n", "MĂ¡u, báº¥t tá»­ vĂ  há»“i phá»¥c");
        float ry = y + CH;

        if (ToggleRow(ry, "Báº¥t tá»­", "KhĂ´ng nháº­n sĂ¡t thÆ°Æ¡ng", Settings.GodMode, theme.Pink))
        {
            Settings.GodMode = !Settings.GodMode;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "MĂ¡u vĂ´ háº¡n", "LuĂ´n giá»¯ thanh mĂ¡u Ä‘áº§y", Settings.InfiniteHp, theme.Pink))
        {
            Settings.InfiniteHp = !Settings.InfiniteHp;
            Settings.Touch();
        }
        ry += RH;

        LogHpRow(ry, theme.Pink);
        ry += SH;

        float oldCur = hpCurUi;
        SliderRow(ry, "MĂ¡u hiá»‡n táº¡i", "Chá»‰nh trá»±c tiáº¿p thanh mĂ¡u", ref hpCurUi, 1f, Mathf.Max(1f, hpMaxUi), v => Mathf.RoundToInt(v) + " HP", theme.Pink);
        if (Mathf.Abs(hpCurUi - oldCur) > 0.01f)
            ApplyCurrentHp();
        ry += SH;

        if (ToggleRow(ry, "Tá»± há»“i mĂ¡u", "Há»“i mĂ¡u theo thá»i gian", Settings.AutoHeal, theme.Green))
        {
            Settings.AutoHeal = !Settings.AutoHeal;
            Settings.Touch();
        }
        ry += RH;

        SliderRow(ry, "Tá»‘c Ä‘á»™ há»“i", "MĂ¡u má»—i giĂ¢y", ref Settings.AutoHealRate, 1f, 200f, v => Mathf.RoundToInt(v) + " HP/s", theme.Green);
        ry += SH;

        ButtonRow(ry,
            new[] { "Há»“i Ä‘áº§y mĂ¡u" },
            new[] { btnPrimary },
            new Action[] { ActHeal });

        y += h1 + CG;

        float h2 = CH + CB + RH + SH + BH;
        DrawCard(y, h2, "Chiáº¿n Ä‘áº¥u", "SĂ¡t thÆ°Æ¡ng gĂ¢y lĂªn káº» Ä‘á»‹ch");
        ry = y + CH;

        if (ToggleRow(ry, "Má»™t Ä‘Ă²n háº¡ gá»¥c", "Má»i Ä‘Ă²n Ä‘Ă¡nh Ä‘á»u káº¿t liá»…u", Settings.OneHitKill, theme.Red))
        {
            Settings.OneHitKill = !Settings.OneHitKill;
            Settings.Touch();
        }
        ry += RH;

        SliderRow(ry, "Há»‡ sá»‘ sĂ¡t thÆ°Æ¡ng", "NhĂ¢n sĂ¡t thÆ°Æ¡ng Ä‘áº§u ra", ref Settings.DamageMultiplier, 1f, 25f, v => "x" + v.ToString("0.0"), theme.Red);
        ry += SH;

        ButtonRow(ry,
            new[] { "Káº¿t liá»…u táº¥t cáº£ káº» Ä‘á»‹ch" },
            new[] { btnDanger },
            new Action[] { ActKillAll });

        y += h2 + CG;

        float h3 = CH + CB + SH + SH + RH + RH + RH + BH;
        DrawCard(y, h3, "Di chuyá»ƒn", "Tá»‘c Ä‘á»™, nháº£y vĂ  thá»ƒ lá»±c");
        ry = y + CH;

        SliderRow(ry, "Tá»‘c Ä‘á»™ di chuyá»ƒn", "Há»‡ sá»‘ tá»‘c cháº¡y", ref Settings.SpeedMultiplier, 0.5f, 5f, v => "x" + v.ToString("0.00"), theme.Green);
        ry += SH;

        SliderRow(ry, "Lá»±c nháº£y", "Há»‡ sá»‘ lá»±c nháº£y", ref Settings.JumpMultiplier, 0.5f, 3f, v => "x" + v.ToString("0.00"), theme.Green);
        ry += SH;

        if (ToggleRow(ry, "Thá»ƒ lá»±c vĂ´ háº¡n", "KhĂ´ng tá»‘n thá»ƒ lá»±c", Settings.InfiniteStamina, theme.Green))
        {
            Settings.InfiniteStamina = !Settings.InfiniteStamina;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "Dash vĂ´ háº¡n", "Bá» há»“i chiĂªu vĂ  giá»›i háº¡n dash", Settings.InfiniteDash, theme.Green))
        {
            Settings.InfiniteDash = !Settings.InfiniteDash;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "HĂºt váº­t pháº©m", "Tá»± Ä‘á»™ng nháº·t EXP gáº§n Ä‘Ă³", Settings.AutoCollect, theme.Accent))
        {
            Settings.AutoCollect = !Settings.AutoCollect;
            Settings.Touch();
        }
        ry += RH;

        ButtonRow(ry,
            new[] { "Má»Ÿ khoĂ¡ ká»¹ nÄƒng" },
            new[] { btnSecondary },
            new Action[] { ActUnlockAbilities });
    }

    void DrawVisualsTab(float y)
    {
        float h1 = CH + CB + RH * 6;
        DrawCard(y, h1, "ESP", "Lá»›p phá»§ thĂ´ng tin trong game");
        float ry = y + CH;

        if (ToggleRow(ry, "Báº­t ESP", "Báº­t hoáº·c táº¯t toĂ n bá»™ lá»›p phá»§", Settings.EspEnabled, theme.Accent))
        {
            Settings.EspEnabled = !Settings.EspEnabled;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "Káº» Ä‘á»‹ch", "Khung, tĂªn vĂ  khoáº£ng cĂ¡ch", Settings.EspEnemies, theme.Accent))
        {
            Settings.EspEnemies = !Settings.EspEnemies;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "Váº­t pháº©m", "EXP, chĂ¬a khoĂ¡ vĂ  váº­t pháº©m nháº·t", Settings.EspItems, theme.Green))
        {
            Settings.EspItems = !Settings.EspItems;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "Thanh mĂ¡u", "Hiá»ƒn thá»‹ mĂ¡u káº» Ä‘á»‹ch", Settings.EspHealth, theme.Red))
        {
            Settings.EspHealth = !Settings.EspHealth;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "TĂªn & khoáº£ng cĂ¡ch", "NhĂ£n thĂ´ng tin cáº¡nh má»¥c tiĂªu", Settings.EspNames, theme.Accent))
        {
            Settings.EspNames = !Settings.EspNames;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "ÄÆ°á»ng chá»‰ dáº«n", "Ná»‘i tĂ¢m mĂ n hĂ¬nh tá»›i má»¥c tiĂªu", Settings.EspTracers, theme.Accent))
        {
            Settings.EspTracers = !Settings.EspTracers;
            Settings.Touch();
        }

        y += h1 + CG;

        float h2 = CH + CB + SH + SH + SH + RH;
        DrawCard(y, h2, "Hiá»ƒn thá»‹", "Pháº¡m vi vĂ  giao diá»‡n ESP");
        ry = y + CH;

        SliderRow(ry, "Khoáº£ng cĂ¡ch quĂ©t", "BĂ¡n kĂ­nh quanh nhĂ¢n váº­t", ref Settings.EspRange, 5f, 100f, v => Mathf.RoundToInt(v) + " m", theme.Accent);
        ry += SH;

        SliderRow(ry, "Äá»™ má»", "Äá»™ Ä‘áº­m cá»§a lá»›p phá»§", ref Settings.EspOpacity, 0.2f, 1f, v => Mathf.RoundToInt(v * 100f) + "%", theme.Accent);
        ry += SH;

        SliderRow(ry, "Cá»¡ chá»¯", "KĂ­ch thÆ°á»›c nhĂ£n ESP", ref Settings.EspFontSize, 9f, 18f, v => Mathf.RoundToInt(v).ToString(), theme.Accent);
        ry += SH;

        if (ToggleRow(ry, "Chá»‰ má»¥c tiĂªu cĂ²n sá»‘ng", "áº¨n káº» Ä‘á»‹ch Ä‘Ă£ cháº¿t", Settings.EspLivingOnly, theme.Green))
        {
            Settings.EspLivingOnly = !Settings.EspLivingOnly;
            Settings.Touch();
        }
    }

    void DrawWorldTab(float y)
    {
        float h1 = CH + CB + BH + TH;
        DrawCard(y, h1, "Dá»‹ch chuyá»ƒn", "LÆ°u vĂ  quay láº¡i vá»‹ trĂ­");
        float ry = y + CH;

        ButtonRow(ry,
            new[] { "LÆ°u vá»‹ trĂ­", "Quay láº¡i" },
            new[] { btnPrimary, btnSecondary },
            new Action[] { ActSavePosition, ActLoadPosition });
        ry += BH;

        string posText = savedPos.HasValue
            ? Mathf.RoundToInt(savedPos.Value.x) + ", " + Mathf.RoundToInt(savedPos.Value.y)
            : "ChÆ°a lÆ°u";
        TextRow(ry, "Vá»‹ trĂ­ Ä‘Ă£ lÆ°u", posText, theme.Green);

        y += h1 + CG;

        float h2 = CH + CB + TH + SH + BH;
        DrawCard(y, h2, "Tiáº¿n trĂ¬nh", "Cáº¥p Ä‘á»™ vĂ  Ä‘iá»ƒm kinh nghiá»‡m");
        ry = y + CH;

        int level = 0;
        int expValue = 0;
        try
        {
            if (exp != null)
            {
                level = exp.GetLevel();
                expValue = exp.GetExp();
            }
        }
        catch { }
        TextRow(ry, "Cáº¥p hiá»‡n táº¡i", level + "  Â·  " + expValue + " EXP", theme.Accent);
        ry += TH;

        SliderRow(ry, "Há»‡ sá»‘ EXP", "NhĂ¢n EXP nháº­n Ä‘Æ°á»£c", ref Settings.ExpMultiplier, 1f, 10f, v => "x" + v.ToString("0.0"), theme.Accent);
        ry += SH;

        ButtonRow(ry,
            new[] { "+1.000 EXP", "Äáº·t EXP = 0" },
            new[] { btnPrimary, btnSecondary },
            new Action[] { ActAddExp, ActResetExp });

        y += h2 + CG;

        float h3 = CH + CB + RH + SH + BH;
        DrawCard(y, h3, "Thá»i gian", "Tá»‘c Ä‘á»™ vĂ  táº¡m dá»«ng");
        ry = y + CH;

        if (ToggleRow(ry, "Táº¡m dá»«ng game", "ÄĂ³ng bÄƒng má»i hoáº¡t Ä‘á»™ng", Settings.PauseGame, theme.Amber))
        {
            Settings.PauseGame = !Settings.PauseGame;
            Settings.Touch();
        }
        ry += RH;

        SliderRow(ry, "Tá»‘c Ä‘á»™ game", "Thá»i gian trong game", ref Settings.GameSpeed, 0.1f, 3f, v => "x" + v.ToString("0.00"), theme.Amber);
        ry += SH;

        ButtonRow(ry,
            new[] { "Äáº·t láº¡i tá»‘c Ä‘á»™" },
            new[] { btnSecondary },
            new Action[] { ActResetSpeed });

        y += h3 + CG;

        float h4 = CH + CB + TH + BH;
        DrawCard(y, h4, "Káº» Ä‘á»‹ch", "Theo dĂµi trong pháº¡m vi");
        ry = y + CH;

        TextRow(ry, "Äang theo dĂµi", enemies.Count + " káº» Ä‘á»‹ch  Â·  " + items.Count + " váº­t pháº©m", theme.Red);
        ry += TH;

        ButtonRow(ry,
            new[] { "Káº¿t liá»…u táº¥t cáº£" },
            new[] { btnDanger },
            new Action[] { ActKillAll });
    }

    void DrawSettingsTab(float y)
    {
        float h1 = CH + CB + RH + RH + RH + SH;
        DrawCard(y, h1, "Giao diá»‡n", "Giao diá»‡n vĂ  hĂ nh vi menu");
        float ry = y + CH;

        if (ToggleRow(ry, "Cháº¿ Ä‘á»™ sĂ¡ng", "Chuyá»ƒn sang tĂ´ng tráº¯ng", Settings.LightTheme, theme.Amber))
        {
            Settings.LightTheme = !Settings.LightTheme;
            Settings.Touch();
            stylesDirty = true;
        }
        ry += RH;

        if (ToggleRow(ry, "Giáº£m chuyá»ƒn Ä‘á»™ng", "Táº¯t hiá»‡u á»©ng má»Ÿ menu", Settings.ReducedMotion, theme.Amber))
        {
            Settings.ReducedMotion = !Settings.ReducedMotion;
            Settings.Touch();
        }
        ry += RH;

        if (ToggleRow(ry, "Táº¡m dá»«ng khi má»Ÿ menu", "NgÄƒn game nháº­n thao tĂ¡c chuá»™t", Settings.PauseOnOpen, theme.Green))
        {
            Settings.PauseOnOpen = !Settings.PauseOnOpen;
            Settings.Touch();
        }
        ry += RH;

        SliderRow(ry, "Äá»™ trong suá»‘t", "Äá»™ Ä‘áº­m cá»§a khung kĂ­nh", ref Settings.GlassOpacity, 0.5f, 1f, v => Mathf.RoundToInt(v * 100f) + "%", theme.Amber);
        y += h1 + CG;

        float h2 = CH + CB + TH * 3;
        DrawCard(y, h2, "PhĂ­m táº¯t", "Äiá»u khiá»ƒn nhanh");
        ry = y + CH;

        TextRow(ry, "F1 / Insert", "Má»Ÿ / Ä‘Ă³ng menu", theme.Amber);
        ry += TH;
        TextRow(ry, "F6", "LÆ°u vá»‹ trĂ­ hiá»‡n táº¡i", theme.Green);
        ry += TH;
        TextRow(ry, "F7", "Quay láº¡i vá»‹ trĂ­ Ä‘Ă£ lÆ°u", theme.Accent);

        y += h2 + CG;

        float h3 = CH + CB + TH * 3 + BH;
        DrawCard(y, h3, "ThĂ´ng tin", "Vá» báº£n mod");
        ry = y + CH;

        TextRow(ry, "PhiĂªn báº£n", "v" + PluginInfo.Version, theme.Accent);
        ry += TH;
        TextRow(ry, "Ná»n táº£ng", "BepInEx 6  Â·  IL2CPP", theme.Accent);
        ry += TH;
        TextRow(ry, "Game", "Gamer Struggles 0.2.4.1", theme.Accent);
        ry += TH;

        ButtonRow(ry,
            new[] { "KhĂ´i phá»¥c máº·c Ä‘á»‹nh" },
            new[] { btnDanger },
            new Action[] { ActResetDefaults });
    }

    void DrawCard(float y, float h, string title, string sub)
    {
        Rect r = new Rect(0f, S(y), S(CW), S(h));
        GUI.Box(r, GUIContent.none, cardStyle);

        GUI.Label(new Rect(r.x + S(16), r.y + S(10), r.width - S(32), S(22)), title, sCardTitle);
        if (!string.IsNullOrEmpty(sub))
            GUI.Label(new Rect(r.x + S(16), r.y + S(27), r.width - S(32), S(16)), sub, sCardSub);

        GUI.color = theme.CardBorder;
        GUI.DrawTexture(new Rect(r.x + S(16), r.y + S(43), r.width - S(32), Mathf.Max(1f, S(1))), whiteTex);
        GUI.color = Color.white;
    }

    bool ToggleRow(float y, string title, string sub, bool value, Color accent)
    {
        Rect row = new Rect(0f, S(y), S(CW), S(RH));
        bool clicked = GUI.Button(row, GUIContent.none, rowStyle);

        GUI.Label(new Rect(row.x + S(16), row.y + S(8), row.width - S(110), S(20)), title, sLabel);
        if (!string.IsNullOrEmpty(sub))
            GUI.Label(new Rect(row.x + S(16), row.y + S(27), row.width - S(110), S(16)), sub, sSub);

        Rect tr = new Rect(row.x + row.width - S(62), row.y + S(15), S(46), S(26));
        DrawToggle(tr, value, accent);
        return clicked;
    }

    void SliderRow(float y, string title, string sub, ref float value, float min, float max, Func<float, string> format, Color accent)
    {
        Rect row = new Rect(0f, S(y), S(CW), S(SH));
        GUI.Label(new Rect(row.x + S(16), row.y + S(7), row.width - S(210), S(20)), title, sLabel);
        if (!string.IsNullOrEmpty(sub))
            GUI.Label(new Rect(row.x + S(16), row.y + S(25), row.width - S(210), S(16)), sub, sSub);

        Rect sr = new Rect(row.x + S(16), row.y + S(40), row.width - S(32), S(20));
        float old = value;
        float nv = GUI.HorizontalSlider(sr, value, min, max, sliderBar, sliderThumb);
        if (Mathf.Abs(nv - old) > 0.0001f)
        {
            value = Mathf.Clamp(nv, min, max);
            lastSliderUse = Time.unscaledTime;
            Settings.Touch();
        }

        GUI.Label(new Rect(row.x + row.width - S(180), row.y + S(7), S(164), S(20)), format(value), sValue);
        DrawSliderVisual(sr, value, min, max, accent);
    }

    void LogHpRow(float y, Color accent)
    {
        Rect row = new Rect(0f, S(y), S(CW), S(SH));
        GUI.Label(new Rect(row.x + S(16), row.y + S(7), row.width - S(210), S(20)), "MĂ¡u tá»‘i Ä‘a", sLabel);
        GUI.Label(new Rect(row.x + S(16), row.y + S(25), row.width - S(210), S(16)), "10 â€“ 9.999 HP", sSub);

        float t = Mathf.Log(Mathf.Max(10f, hpMaxUi) / 10f) / Mathf.Log(999.9f);
        Rect sr = new Rect(row.x + S(16), row.y + S(40), row.width - S(32), S(20));
        float nt = GUI.HorizontalSlider(sr, t, 0f, 1f, sliderBar, sliderThumb);

        if (Mathf.Abs(nt - t) > 0.0005f)
        {
            hpMaxUi = Mathf.Clamp(Mathf.Round(10f * Mathf.Pow(999.9f, nt)), 10f, 9999f);
            lastSliderUse = Time.unscaledTime;
            ApplyMaxHp();
            t = Mathf.Log(Mathf.Max(10f, hpMaxUi) / 10f) / Mathf.Log(999.9f);
        }

        GUI.Label(new Rect(row.x + row.width - S(180), row.y + S(7), S(164), S(20)),
            Mathf.RoundToInt(hpMaxUi) + " HP", sValue);
        DrawSliderVisual(sr, t, 0f, 1f, accent);
    }

    void ButtonRow(float y, string[] labels, GUIStyle[] styles, Action[] actions)
    {
        float gap = S(8);
        float total = S(CW - 32);
        float width = (total - gap * (labels.Length - 1)) / Mathf.Max(1, labels.Length);

        for (int i = 0; i < labels.Length; i++)
        {
            Rect r = new Rect(S(16) + i * (width + gap), S(y + 6), width, S(36));
            if (GUI.Button(r, labels[i], styles[Mathf.Min(i, styles.Length - 1)]))
            {
                try { actions[Mathf.Min(i, actions.Length - 1)](); }
                catch (Exception ex)
                {
                    LogErrorOnce(ex, "ButtonAction");
                    Toast("Thao tĂ¡c tháº¥t báº¡i â€” xem log", theme.Red);
                }
            }
        }
    }

    void TextRow(float y, string left, string right, Color rightColor)
    {
        GUI.Label(new Rect(S(16), S(y + 10), S(CW - 32), S(20)), left, sLabel);
        if (!string.IsNullOrEmpty(right))
        {
            GUIStyle style = sValue;
            Color old = style.normal.textColor;
            style.normal.textColor = rightColor;
            GUI.Label(new Rect(S(CW - 240), S(y + 10), S(224), S(20)), right, style);
            style.normal.textColor = old;
        }
    }

    void DrawToggle(Rect r, bool on, Color accent)
    {
        Color off = theme.TrackOff;
        Texture2D track = GetToggleTex(
            Mathf.Max(2, Mathf.RoundToInt(r.width)),
            Mathf.Max(2, Mathf.RoundToInt(r.height)),
            on,
            on ? accent : off);

        GUI.color = Color.white;
        GUI.DrawTexture(r, track, ScaleMode.StretchToFill, true);

        int ks = Mathf.Max(6, Mathf.RoundToInt(r.height - S(4)));
        float knob = Mathf.Min(ks, r.height - S(4));
        float kx = on ? r.xMax - knob - S(2) : r.x + S(2);
        Rect kr = new Rect(kx, r.y + (r.height - knob) * 0.5f, knob, knob);

        GUI.color = new Color(0f, 0f, 0f, 0.25f);
        GUI.DrawTexture(new Rect(kr.x, kr.y + S(1), kr.width, kr.height), GetCircle(ks));
        GUI.color = Color.white;
        GUI.DrawTexture(kr, GetCircle(ks));
    }

    void DrawSliderVisual(Rect sr, float value, float min, float max, Color accent)
    {
        float h = Mathf.Max(2f, S(6));
        float trackY = sr.y + sr.height * 0.5f - h * 0.5f;
        float inset = S(8);
        Rect track = new Rect(sr.x + inset, trackY, Mathf.Max(2f, sr.width - inset * 2f), h);

        GUI.color = theme.TrackOff;
        GUI.DrawTexture(track, trackTex, ScaleMode.StretchToFill, true);

        float t = Mathf.InverseLerp(min, max, value);
        float knobX = Mathf.Lerp(track.x, track.xMax, t);

        GUI.color = accent;
        GUI.DrawTexture(new Rect(track.x + S(2), track.y, Mathf.Max(0f, knobX - track.x - S(2)), h), whiteTex);

        int ks = Mathf.Max(6, Mathf.RoundToInt(S(16)));
        float knob = ks;
        GUI.color = new Color(0f, 0f, 0f, 0.25f);
        GUI.DrawTexture(new Rect(knobX - knob * 0.5f, track.center.y - knob * 0.5f + S(1), knob, knob), GetCircle(ks));
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(knobX - knob * 0.5f, track.center.y - knob * 0.5f, knob, knob), GetCircle(ks));
        GUI.color = Color.white;
    }

    void DrawDot(Vector2 center, float radius, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), circleTex);
        GUI.color = Color.white;
    }

    void DrawToast(Rect wr)
    {
        if (toast == null || Time.unscaledTime >= toastUntil) return;

        float remaining = toastUntil - Time.unscaledTime;
        float fade = Mathf.Clamp01(remaining / 0.35f);
        float width = Mathf.Min(S(420), Mathf.Max(S(240), S(240) + toast.Length * S(5)));
        Rect r = new Rect(wr.center.x - width * 0.5f, wr.yMax - S(88), width, S(34));

        GUI.color = new Color(1f, 1f, 1f, fade * 0.95f);
        GUI.Box(r, GUIContent.none, pillStyle);
        GUI.color = new Color(toastColor.r, toastColor.g, toastColor.b, fade);
        GUI.DrawTexture(new Rect(r.x + S(10), r.y + S(9), Mathf.Max(2f, S(3)), S(16)), whiteTex);
        GUI.color = new Color(1f, 1f, 1f, fade);
        GUI.Label(r, toast, sToast);
        GUI.color = Color.white;
    }

    void Toast(string message, Color color)
    {
        toast = message;
        toastColor = color;
        toastUntil = Time.unscaledTime + 2.4f;
    }

    void ToastNoPlayer() => Toast("ChÆ°a tĂ¬m tháº¥y nhĂ¢n váº­t â€” hĂ£y vĂ o game", theme.Amber);

    void ActHeal()
    {
        if (!IsReady()) { ToastNoPlayer(); return; }
        hp.Heal(hp.MaxHP);
        Toast("ÄĂ£ há»“i Ä‘áº§y mĂ¡u", theme.Green);
    }

    void ActKillAll()
    {
        if (!IsReady()) { ToastNoPlayer(); return; }

        int killed = 0;
        foreach (EnemyTarget t in enemies)
        {
            try
            {
                if (t.Ctrl != null)
                {
                    EnemyHealth eh = t.Ctrl.Health;
                    if (eh != null && !eh.IsDead())
                    {
                        eh.Kill();
                        killed++;
                    }
                }
            }
            catch { }
        }

        Toast(killed > 0 ? "ÄĂ£ káº¿t liá»…u " + killed + " káº» Ä‘á»‹ch" : "KhĂ´ng cĂ³ káº» Ä‘á»‹ch trong pháº¡m vi",
            killed > 0 ? theme.Red : theme.Amber);
    }

    void ActUnlockAbilities()
    {
        if (!IsReady()) { ToastNoPlayer(); return; }

        try
        {
            if (jump != null) jump.EnableDoubleJumping();
            if (dash != null) dash.EnableDashing(true);
            PlayerChargeAttackController.EnableChargeAttack();
            Toast("ÄĂ£ má»Ÿ khoĂ¡ double jump, dash vĂ  charge attack", theme.Green);
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "UnlockAbilities");
            Toast("KhĂ´ng thá»ƒ má»Ÿ khoĂ¡ ká»¹ nÄƒng", theme.Red);
        }
    }

    void ActAddExp()
    {
        if (!IsReady() || exp == null) { ToastNoPlayer(); return; }

        try
        {
            exp.ReceiveExp(1000);
            Toast("ÄĂ£ thĂªm 1.000 EXP", theme.Accent);
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "AddExp");
            Toast("KhĂ´ng thá»ƒ thĂªm EXP", theme.Red);
        }
    }

    void ActResetExp()
    {
        if (!IsReady() || exp == null) { ToastNoPlayer(); return; }

        try
        {
            exp.SetExp(0);
            Toast("ÄĂ£ Ä‘áº·t EXP vá» 0", theme.Amber);
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "ResetExp");
            Toast("KhĂ´ng thá»ƒ Ä‘áº·t láº¡i EXP", theme.Red);
        }
    }

    void ActSavePosition()
    {
        if (!IsReady() || playerTransform == null) { ToastNoPlayer(); return; }

        try
        {
            savedPos = playerTransform.position;
            Toast("ÄĂ£ lÆ°u vá»‹ trĂ­", theme.Green);
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "SavePosition");
            Toast("KhĂ´ng thá»ƒ lÆ°u vá»‹ trĂ­", theme.Red);
        }
    }

    void ActLoadPosition()
    {
        if (!IsReady() || playerTransform == null) { ToastNoPlayer(); return; }
        if (!savedPos.HasValue) { Toast("ChÆ°a cĂ³ vá»‹ trĂ­ Ä‘Ă£ lÆ°u", theme.Amber); return; }

        try
        {
            Vector3 p = savedPos.Value;
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.position = new Vector2(p.x, p.y);
            }
            playerTransform.position = p;
            Toast("ÄĂ£ quay láº¡i vá»‹ trĂ­ Ä‘Ă£ lÆ°u", theme.Accent);
        }
        catch (Exception ex)
        {
            LogErrorOnce(ex, "LoadPosition");
            Toast("KhĂ´ng thá»ƒ dá»‹ch chuyá»ƒn", theme.Red);
        }
    }

    void ActResetSpeed()
    {
        Settings.GameSpeed = 1f;
        Settings.PauseGame = false;
        Settings.Touch();
        timeApplied = true;
        Toast("ÄĂ£ Ä‘áº·t láº¡i tá»‘c Ä‘á»™ game", theme.Green);
    }

    void ActResetDefaults()
    {
        RestoreCheatEffects();
        Settings.Reset();
        stylesDirty = true;
        Toast("ÄĂ£ khĂ´i phá»¥c cĂ i Ä‘áº·t máº·c Ä‘á»‹nh", theme.Green);
    }

    Color TabAccent(int which)
        => which == 0 ? theme.Pink : which == 1 ? theme.Accent : which == 2 ? theme.Green : theme.Amber;

    void EnsureFont()
    {
        if (font != null) return;

        try { font = Font.CreateDynamicFontFromOSFont("Segoe UI", 16); } catch { }
        if (font == null)
        {
            try { font = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch { }
        }
    }

    void MakeBaseTextures()
    {
        whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply(false, false);

        circleTex = MakeRounded(32, 32, 16f, Color.white, Color.white, Color.clear, 0f);
        blobTex = MakeBlob(128);
    }

    void BuildStyles()
    {
        EnsureFont();

        foreach (Texture2D tex in styleTex)
            if (tex != null) Destroy(tex);
        styleTex.Clear();

        int F(float v) => Mathf.RoundToInt(v * scale);

        sHeaderTitle = Text(F(15f), FontStyle.Bold, theme.Text);
        sIcon = Text(F(12f), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        sTabActive = Text(F(12.5f), FontStyle.Normal, Color.white);
        sTabInactive = Text(F(12.5f), FontStyle.Normal, theme.Dim);
        sTabIconActive = Text(F(9f), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        sTabIconInactive = Text(F(9f), FontStyle.Bold, theme.Dim, TextAnchor.MiddleCenter);
        sKey = Text(F(10f), FontStyle.Bold, theme.Text, TextAnchor.MiddleCenter);
        sBadge = Text(F(9.5f), FontStyle.Bold, theme.Text);
        sClose = Text(F(20f), FontStyle.Normal, theme.Text, TextAnchor.MiddleCenter);
        sPageTitle = Text(F(23f), FontStyle.Bold, theme.Text);
        sPageSub = Text(F(10f), FontStyle.Normal, theme.Dim);
        sCardTitle = Text(F(13.5f), FontStyle.Bold, theme.Text);
        sCardSub = Text(F(9f), FontStyle.Normal, theme.Dim);
        sLabel = Text(F(12.5f), FontStyle.Normal, theme.Text);
        sSub = Text(F(9.5f), FontStyle.Normal, theme.Dim);
        sValue = Text(F(11.5f), FontStyle.Bold, theme.Accent, TextAnchor.MiddleRight);
        sSmall = Text(F(9.5f), FontStyle.Normal, theme.Text);
        sTiny = Text(F(8.5f), FontStyle.Normal, theme.Dim);
        sToast = Text(F(11f), FontStyle.Bold, Settings.LightTheme ? theme.Text : Color.white, TextAnchor.MiddleCenter);
        sEsp = Text(F(Settings.EspFontSize), FontStyle.Bold, Color.white);
        sEspShadow = Text(F(Settings.EspFontSize), FontStyle.Bold, new Color(0f, 0f, 0f, 0.85f));

        btnPrimary = MakeButton(
            new Color(0.13f, 0.52f, 1f, 0.96f),
            new Color(0.02f, 0.36f, 0.94f, 0.96f),
            new Color(1f, 1f, 1f, 0.22f),
            Color.white,
            F(11.5f));

        btnSecondary = MakeButton(theme.BtnSecTop, theme.BtnSecBottom, theme.BtnSecBorder, theme.Text, F(11.5f));
        btnDanger = MakeButton(theme.BtnDangerTop, theme.BtnDangerBottom, theme.BtnDangerBorder, theme.Red, F(11.5f));

        rowStyle = new GUIStyle();
        int rowSize = Mathf.Max(32, Mathf.RoundToInt(96f * scale));
        int rowBorder = Mathf.Clamp(Mathf.RoundToInt(14f * scale), 1, rowSize / 2 - 1);
        rowStyle.normal.background = null;
        rowStyle.hover.background = StyleRounded(rowSize, rowSize, 14f * scale, theme.RowHover, theme.RowHover, Color.clear, 0f);
        rowStyle.active.background = StyleRounded(rowSize, rowSize, 14f * scale, theme.RowHover, theme.RowHover, Color.clear, 0f);
        rowStyle.border = new RectOffset(rowBorder, rowBorder, rowBorder, rowBorder);
        rowStyle.margin = new RectOffset(0, 0, 0, 0);
        rowStyle.padding = new RectOffset(0, 0, 0, 0);

        cardStyle = GlassStyle(theme.CardTop, theme.CardBottom, theme.CardBorder, 20f, 1f);
        windowStyle = GlassStyle(theme.WindowTop, theme.WindowBottom, theme.WindowBorder, 24f, 1.2f);
        sidebarStyle = GlassStyle(theme.SidebarTop, theme.SidebarBottom, theme.SidebarBorder, 18f, 1f);
        shadowStyle = GlassStyle(Color.black, Color.black, Color.clear, 24f, 0f);
        pillStyle = GlassStyle(theme.CardTop, theme.CardBottom, theme.CardBorder, 14f, 1f);
        iconTileStyle = GlassStyle(Color.white, Color.white, Color.clear, 9f, 0f);
        tabActiveStyle = GlassStyle(
            new Color(1f, 1f, 1f, 0.28f),
            new Color(1f, 1f, 1f, 0.12f),
            new Color(1f, 1f, 1f, 0.18f),
            14f,
            1f);
        iconStyle = GlassStyle(
            new Color(0.22f, 0.52f, 1f, 0.95f),
            new Color(0.55f, 0.28f, 1f, 0.95f),
            new Color(1f, 1f, 1f, 0.30f),
            11f,
            1f);

        sliderBar = new GUIStyle
        {
            fixedHeight = S(20),
            normal = { background = null }
        };
        sliderThumb = new GUIStyle
        {
            fixedWidth = S(16),
            fixedHeight = S(16),
            normal = { background = null }
        };

        int trackW = Mathf.Max(4, Mathf.RoundToInt(S(CW - 48f)));
        int trackH = Mathf.Max(2, Mathf.RoundToInt(S(6f)));
        trackTex = StyleRounded(trackW, trackH, trackH * 0.5f, Color.white, Color.white, Color.clear, 0f);

        stylesScale = scale;
        stylesLight = Settings.LightTheme;
    }

    GUIStyle Text(int size, FontStyle style, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        var st = new GUIStyle(GUI.skin.label)
        {
            font = font,
            fontSize = size,
            fontStyle = style,
            alignment = anchor,
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Clip,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        st.normal.textColor = color;
        return st;
    }

    GUIStyle MakeButton(Color top, Color bottom, Color border, Color text, int fontSize)
    {
        int size = Mathf.Max(32, Mathf.RoundToInt(96f * scale));
        float radius = 14f * scale;
        float bw = Mathf.Max(1f, scale);

        var st = new GUIStyle
        {
            font = font,
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            wordWrap = false,
            border = new RectOffset(
                Mathf.Max(1, Mathf.RoundToInt(radius)),
                Mathf.Max(1, Mathf.RoundToInt(radius)),
                Mathf.Max(1, Mathf.RoundToInt(radius)),
                Mathf.Max(1, Mathf.RoundToInt(radius))),
            padding = new RectOffset(8, 8, 4, 4),
            margin = new RectOffset(0, 0, 0, 0)
        };

        st.normal.background = StyleRounded(size, size, radius, top, bottom, border, bw);
        st.hover.background = StyleRounded(size, size, radius, Lighten(top, 0.07f), Lighten(bottom, 0.07f), border, bw);
        st.active.background = StyleRounded(size, size, radius, Darken(top, 0.06f), Darken(bottom, 0.06f), border, bw);
        st.focused.background = st.normal.background;

        st.normal.textColor = text;
        st.hover.textColor = text;
        st.active.textColor = text;
        st.focused.textColor = text;
        return st;
    }

    GUIStyle GlassStyle(Color top, Color bottom, Color border, float radius, float borderWidth)
    {
        int size = Mathf.Max(32, Mathf.RoundToInt(96f * scale));
        float r = Mathf.Clamp(radius * scale, 1f, size * 0.5f - 1f);
        float bw = borderWidth * scale;
        int b = Mathf.Clamp(Mathf.RoundToInt(r), 1, size / 2 - 1);

        var st = new GUIStyle
        {
            normal = { background = StyleRounded(size, size, r, top, bottom, border, bw) },
            border = new RectOffset(b, b, b, b),
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            stretchHeight = true,
            stretchWidth = true
        };
        return st;
    }

    Texture2D StyleRounded(int w, int h, float radius, Color top, Color bottom, Color border, float borderWidth)
    {
        Texture2D tex = MakeRounded(w, h, radius, top, bottom, border, borderWidth);
        styleTex.Add(tex);
        return tex;
    }

    Texture2D GetToggleTex(int w, int h, bool on, Color accent)
    {
        string key = "t" + w + "x" + h + (on ? "1" : "0") + ColorKey(accent);
        if (toggleCache.TryGetValue(key, out Texture2D cached) && cached != null)
            return cached;

        Color top = on ? Lighten(accent, 0.18f) : Lighten(theme.TrackOff, 0.04f);
        Color bottom = on ? Darken(accent, 0.08f) : theme.TrackOff;
        Color border = on ? new Color(1f, 1f, 1f, 0.24f) : new Color(1f, 1f, 1f, 0.10f);

        Texture2D tex = MakeRounded(w, h, h * 0.5f, top, bottom, border, Mathf.Max(1f, scale));
        tex.hideFlags = HideFlags.DontSave;
        toggleCache[key] = tex;
        return tex;
    }

    Texture2D GetCircle(int size)
    {
        size = Mathf.Max(4, size);
        string key = "c" + size;
        if (toggleCache.TryGetValue(key, out Texture2D cached) && cached != null)
            return cached;

        Texture2D tex = MakeRounded(size, size, size * 0.5f, Color.white, Color.white, Color.clear, 0f);
        toggleCache[key] = tex;
        return tex;
    }

    static string ColorKey(Color c)
        => ((int)(c.r * 255f)).ToString() + "," +
           ((int)(c.g * 255f)).ToString() + "," +
           ((int)(c.b * 255f)).ToString() + "," +
           ((int)(c.a * 255f)).ToString();

    static Color Lighten(Color c, float t)
        => new Color(Mathf.Lerp(c.r, 1f, t), Mathf.Lerp(c.g, 1f, t), Mathf.Lerp(c.b, 1f, t), c.a);

    static Color Darken(Color c, float t)
        => new Color(Mathf.Lerp(c.r, 0f, t), Mathf.Lerp(c.g, 0f, t), Mathf.Lerp(c.b, 0f, t), c.a);

    float S(float value) => value * scale;

    static Texture2D MakeRounded(int w, int h, float radius, Color top, Color bottom, Color border, float borderWidth)
    {
        w = Mathf.Max(2, w);
        h = Mathf.Max(2, h);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[w * h];
        float r = Mathf.Clamp(radius, 0.5f, Mathf.Min(w, h) * 0.5f);
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        float innerW = halfW - r;
        float innerH = halfH - r;

        for (int y = 0; y < h; y++)
        {
            float t = h > 1 ? y / (float)(h - 1) : 1f;
            Color fill = Color.Lerp(bottom, top, t);

            for (int x = 0; x < w; x++)
            {
                float px = x + 0.5f - halfW;
                float py = y + 0.5f - halfH;
                float qx = Mathf.Abs(px) - innerW;
                float qy = Mathf.Abs(py) - innerH;
                float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                float distance = Mathf.Min(Mathf.Max(qx, qy), 0f) + outside - r;
                float alpha = Mathf.Clamp01(0.5f - distance);

                Color c = borderWidth > 0f && distance > -borderWidth ? border : fill;
                c.a *= alpha;
                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    static Texture2D MakeBlob(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color32[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * 0.7f;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    Theme MakeTheme()
    {
        if (Settings.LightTheme)
        {
            return new Theme
            {
                Text = new Color(0.08f, 0.09f, 0.12f, 1f),
                Dim = new Color(0.36f, 0.38f, 0.45f, 1f),
                Faint = new Color(0.55f, 0.57f, 0.63f, 1f),
                Accent = new Color(0f, 0.48f, 1f, 1f),
                Green = new Color(0f, 0.62f, 0.30f, 1f),
                Pink = new Color(0.90f, 0.15f, 0.35f, 1f),
                Amber = new Color(0.90f, 0.60f, 0.05f, 1f),
                Red = new Color(0.85f, 0.15f, 0.15f, 1f),
                WindowTop = new Color(1f, 1f, 1f, 0.88f),
                WindowBottom = new Color(0.94f, 0.95f, 0.98f, 0.92f),
                WindowBorder = new Color(1f, 1f, 1f, 0.85f),
                SidebarTop = new Color(1f, 1f, 1f, 0.45f),
                SidebarBottom = new Color(1f, 1f, 1f, 0.25f),
                SidebarBorder = new Color(0.10f, 0.12f, 0.20f, 0.06f),
                CardTop = new Color(1f, 1f, 1f, 0.58f),
                CardBottom = new Color(1f, 1f, 1f, 0.36f),
                CardBorder = new Color(0.10f, 0.12f, 0.20f, 0.08f),
                RowHover = new Color(0f, 0f, 0f, 0.045f),
                TrackOff = new Color(0f, 0f, 0f, 0.12f),
                Backdrop = new Color(0.55f, 0.58f, 0.66f, 0.28f),
                Blob1 = new Color(0.20f, 0.50f, 1f, 0.14f),
                Blob2 = new Color(1f, 0.30f, 0.50f, 0.10f),
                BtnSecTop = new Color(1f, 1f, 1f, 0.70f),
                BtnSecBottom = new Color(1f, 1f, 1f, 0.45f),
                BtnSecBorder = new Color(0.10f, 0.12f, 0.20f, 0.08f),
                BtnDangerTop = new Color(1f, 0.30f, 0.35f, 0.14f),
                BtnDangerBottom = new Color(1f, 0.25f, 0.30f, 0.08f),
                BtnDangerBorder = new Color(0.80f, 0.10f, 0.15f, 0.15f)
            };
        }

        return new Theme
        {
            Text = new Color(0.97f, 0.97f, 1f, 1f),
            Dim = new Color(0.68f, 0.70f, 0.79f, 1f),
            Faint = new Color(0.44f, 0.46f, 0.55f, 1f),
            Accent = new Color(0.12f, 0.55f, 1f, 1f),
            Green = new Color(0.24f, 0.85f, 0.48f, 1f),
            Pink = new Color(1f, 0.32f, 0.46f, 1f),
            Amber = new Color(1f, 0.72f, 0.28f, 1f),
            Red = new Color(1f, 0.36f, 0.34f, 1f),
            WindowTop = new Color(0.16f, 0.18f, 0.23f, 0.92f),
            WindowBottom = new Color(0.06f, 0.07f, 0.10f, 0.95f),
            WindowBorder = new Color(1f, 1f, 1f, 0.14f),
            SidebarTop = new Color(1f, 1f, 1f, 0.055f),
            SidebarBottom = new Color(1f, 1f, 1f, 0.02f),
            SidebarBorder = new Color(1f, 1f, 1f, 0.07f),
            CardTop = new Color(1f, 1f, 1f, 0.075f),
            CardBottom = new Color(1f, 1f, 1f, 0.028f),
            CardBorder = new Color(1f, 1f, 1f, 0.09f),
            RowHover = new Color(1f, 1f, 1f, 0.05f),
            TrackOff = new Color(1f, 1f, 1f, 0.14f),
            Backdrop = new Color(0f, 0f, 0f, 0.42f),
            Blob1 = new Color(0.10f, 0.40f, 1f, 0.20f),
            Blob2 = new Color(1f, 0.20f, 0.50f, 0.12f),
            BtnSecTop = new Color(1f, 1f, 1f, 0.09f),
            BtnSecBottom = new Color(1f, 1f, 1f, 0.04f),
            BtnSecBorder = new Color(1f, 1f, 1f, 0.10f),
            BtnDangerTop = new Color(1f, 0.25f, 0.30f, 0.20f),
            BtnDangerBottom = new Color(1f, 0.20f, 0.26f, 0.10f),
            BtnDangerBorder = new Color(1f, 0.35f, 0.40f, 0.20f)
        };
    }

    static void LogErrorOnce(Exception ex, string where)
    {
        string key = where + ":" + ex.GetType().Name + ":" + ex.Message;
        if (!Logged.Add(key)) return;
        Plugin.Log?.LogError("[" + where + "] " + ex);
    }

    struct Theme
    {
        public Color Text;
        public Color Dim;
        public Color Faint;
        public Color Accent;
        public Color Green;
        public Color Pink;
        public Color Amber;
        public Color Red;
        public Color WindowTop;
        public Color WindowBottom;
        public Color WindowBorder;
        public Color SidebarTop;
        public Color SidebarBottom;
        public Color SidebarBorder;
        public Color CardTop;
        public Color CardBottom;
        public Color CardBorder;
        public Color RowHover;
        public Color TrackOff;
        public Color Backdrop;
        public Color Blob1;
        public Color Blob2;
        public Color BtnSecTop;
        public Color BtnSecBottom;
        public Color BtnSecBorder;
        public Color BtnDangerTop;
        public Color BtnDangerBottom;
        public Color BtnDangerBorder;
    }
}
