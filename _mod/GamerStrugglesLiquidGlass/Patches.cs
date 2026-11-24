using System;
using GamerStruggles.DamageSystem;
using GamerStruggles.Player;
using HarmonyLib;
using UnityEngine;

namespace GamerStrugglesLiquidGlass;

internal static class Patches
{
    static IntPtr lastScaledTarget;
    static int lastScaledFrame = -1;

    static bool NeedsEnemyScale()
        => Settings.OneHitKill || Settings.DamageMultiplier > 1.001f;

    static bool AlreadyScaled(EnemyHealth enemy)
    {
        IntPtr ptr = enemy.Pointer;
        if (lastScaledFrame == Time.frameCount && lastScaledTarget == ptr)
            return true;

        lastScaledTarget = ptr;
        lastScaledFrame = Time.frameCount;
        return false;
    }

    static void ApplyEnemyScale(ref int amount)
    {
        if (Settings.OneHitKill)
        {
            amount = 999999;
            return;
        }

        long scaled = (long)Math.Round(amount * (double)Settings.DamageMultiplier);
        amount = scaled > int.MaxValue ? int.MaxValue : (int)scaled;
        if (amount < 1) amount = 1;
    }

    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.TakeDamage), new Type[] { typeof(DamageEventArgs) })]
    [HarmonyPrefix]
    static bool BlockPlayerDamageArgs() => !Settings.GodMode;

    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.TakeDamage), new Type[] { typeof(int) })]
    [HarmonyPrefix]
    static bool BlockPlayerDamageInt() => !Settings.GodMode;

    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Kill))]
    [HarmonyPrefix]
    static bool BlockPlayerKill() => !Settings.GodMode;

    [HarmonyPatch(typeof(Health), nameof(Health.TakeRawDamage), new Type[] { typeof(int), typeof(DamageType) })]
    [HarmonyPrefix]
    static bool BlockPlayerRawDamage(Health __instance)
        => !(Settings.GodMode && __instance is PlayerHealth);

    [HarmonyPatch(typeof(EnemyHealth), nameof(EnemyHealth.TakeDamage), new Type[] { typeof(DamageEventArgs) })]
    [HarmonyPrefix]
    static void ScaleEnemyDamageArgs(EnemyHealth __instance, DamageEventArgs damageEventArgs)
    {
        if (!NeedsEnemyScale() || damageEventArgs == null || AlreadyScaled(__instance))
            return;

        int amount = damageEventArgs.DamageAmount;
        ApplyEnemyScale(ref amount);
        damageEventArgs.DamageAmount = amount;
    }

    [HarmonyPatch(typeof(EnemyHealth), nameof(EnemyHealth.TakeDamage), new Type[] { typeof(int) })]
    [HarmonyPrefix]
    static void ScaleEnemyDamageInt(EnemyHealth __instance, ref int damageAmount)
    {
        if (!NeedsEnemyScale() || AlreadyScaled(__instance))
            return;

        ApplyEnemyScale(ref damageAmount);
    }

    [HarmonyPatch(typeof(PlayerExperience), nameof(PlayerExperience.ReceiveExp))]
    [HarmonyPrefix]
    static void ScaleExperience(ref int amount)
    {
        if (Settings.ExpMultiplier <= 1.001f || amount <= 0)
            return;

        double scaled = amount * (double)Settings.ExpMultiplier;
        amount = scaled > int.MaxValue ? int.MaxValue : (int)Math.Round(scaled);
    }

    [HarmonyPatch(typeof(PlayerStaminaController), nameof(PlayerStaminaController.TrySpendStamina))]
    [HarmonyPrefix]
    static bool InfiniteStamina(ref bool __result)
    {
        if (!Settings.InfiniteStamina)
            return true;

        __result = true;
        return false;
    }
}
