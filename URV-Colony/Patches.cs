﻿using HarmonyLib;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace URV_Colony
{
    internal class Patches
    {
        [HarmonyPatch(typeof(PLServer), "NetworkBeginWarp")]
        class ResetMapOnWarp
        {
            static void Postfix()
            {
                PLAbyssShipInfo.Instance?.Map.m_AbyssGridCells.Clear();
            }
        }

        [HarmonyPatch(typeof(PLAbyssMap), "Update")]
        class ReplaceMap
        {
            static void Postfix(PLAbyssMap __instance)
            {
                PLSectorInfo sector = PLServer.GetCurrentSector();
                if (sector != null && sector.VisualIndication != ESectorVisualIndication.ABYSS && __instance.ShowOnScreen)
                {
                    __instance.ShowOnScreen = false;
                    PLTabMenu.Instance.Starmap.IsActive = !PLTabMenu.Instance.Starmap.IsActive;
                }
            }
        }

        [HarmonyPatch(typeof(PLPersistantEncounterInstance), "PlayMusicBasedOnShipType")]
        class ReplaceMusic
        {
            static void Postfix(EShipType inType, bool combat)
            {
                if (inType != EShipType.E_ABYSS_PLAYERSHIP || (PLServer.GetCurrentSector() != null && PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.ABYSS)) return;
                List<string> list = new List<string>();
                if (combat)
                {
                    if (UnityEngine.Random.value < 0.25f)
                    {
                        list.Add("mx_finalstand");
                    }
                    if (UnityEngine.Random.value < 0.25f)
                    {
                        list.Add("mx_gap");
                    }
                    if (UnityEngine.Random.value < 0.25f)
                    {
                        list.Add("mx_boarders");
                    }
                    if (UnityEngine.Random.value < 0.25f)
                    {
                        list.Add("mx_lasttogo");
                    }
                    list.Add("mx_CUAttack");
                    list.Add("mx_CUAttackAlt");
                }
                else
                {
                    list.Add("mx_ambient_1");
                    list.Add("mx_ambient_2");
                    list.Add("mx_ambient_3_loop");
                    list.Add("mx_ivm_genamb01");
                    list.Add("mx_ivm_genamb02");
                    list.Add("mx_colunion_v4");
                    list.Add("mx_CUExplore_lp");
                }
                if (list.Count > 0)
                {
                    PLMusic.Instance.PlayMusic(list[UnityEngine.Random.Range(0, list.Count)], combat, false, false, false);
                }
            }
        }

        [HarmonyPatch(typeof(PLWarpDrive), "AddStats")]
        class WarpDriveFix
        {
            static void Postfix(PLWarpDrive __instance)
            {
                if (__instance.SubType == (int)EWarpDriveType.SUB_WARPDRIVE)
                {
                    __instance.Desc = "Medium range, high charge rate and program charges.";
                    __instance.ChargeSpeed = 5f;
                    __instance.WarpRange = 0.06f;
                    __instance.EnergySignatureAmt = 10;
                }
            }
        }

        [HarmonyPatch(typeof(PLAbyssShipInfo), "Start")]
        class AutoAssemble
        {
            static void Postfix()
            {
                if (PLServer.GetCurrentSector().VisualIndication != ESectorVisualIndication.ABYSS)
                {
                    Assembler.Assemble();
                }
            }
        }

        [HarmonyPatch(typeof(PLShipStats), "CalculateStats")]
        class ApplyEMP 
        {
            static void Postfix(PLShipStats __instance) 
            {
                if(PLAbyssShipInfo.Instance != null && __instance.Ship != PLAbyssShipInfo.Instance) 
                {
                    PLAbyssShipInfo.UpdateShipStatsBasedOnEMP(__instance);
                }
            }
        }


        [HarmonyPatch(typeof(PLInGameUI),"Update")]
        class ShowMoney 
        {
            static void Prefix(PLInGameUI __instance,ref int __state) 
            {
                if (PLServer.Instance != null)
                {
                    __state = __instance.CachedCredits;
                }
            }
            static void Postfix(PLInGameUI __instance, int __state) 
            {
                if (PLAbyssShipInfo.Instance != null && PLServer.GetCurrentSector() != null && PLServer.GetCurrentSector().VisualIndication != ESectorVisualIndication.ABYSS) 
                {
                    __instance.CachedCredits = __state;
                    if (Time.time - __instance.LastCachedCreditsUpdateTime > 1f && __instance.CachedCredits != PLServer.Instance.CurrentCrewCredits)
                    {
                        int num13 = Mathf.RoundToInt((float)(PLServer.Instance.CurrentCrewCredits - __instance.CachedCredits));
                        if (num13 > 0)
                        {
                            __instance.CreditsScrolling.text = "+" + num13.ToString("N0") + " " + PLLocalize.Localize("Cr", false);
                        }
                        else
                        {
                            __instance.CreditsScrolling.text = num13.ToString("N0") + " " + PLLocalize.Localize("Cr", false);
                        }
                        __instance.CachedCredits = PLServer.Instance.CurrentCrewCredits;
                        __instance.CreditsScrollTime = 0f;
                        __instance.LastCachedCreditsUpdateTime = Time.time;
                        PLGameProgressManager.Instance.OnCreditsEarned(num13);
                    }
                    if (Mathf.RoundToInt(__instance.SlowCredits) != __instance.CachedCredits || __instance.CreditsValueForceUpdate || __instance.Cached_PurchaseLimitsEnabled != PLServer.Instance.CrewPurchaseLimitsEnabled || __instance.Cached_AllowanceCredits != PLServer.Instance.CurrentCrewCreditsAvailableToCrew)
                    {
                        __instance.Cached_PurchaseLimitsEnabled = PLServer.Instance.CrewPurchaseLimitsEnabled;
                        __instance.Cached_AllowanceCredits = PLServer.Instance.CurrentCrewCreditsAvailableToCrew;
                        float slowCredits = __instance.SlowCredits;
                        if (Mathf.Abs(__instance.SlowCredits - (float)__instance.CachedCredits) > 1000f)
                        {
                            __instance.SlowCredits = Mathf.Lerp(__instance.SlowCredits, (float)__instance.CachedCredits, Mathf.Clamp01(Time.deltaTime));
                        }
                        if (__instance.SlowCredits < (float)__instance.CachedCredits)
                        {
                            if ((float)__instance.CachedCredits - __instance.SlowCredits > 5000f)
                            {
                                __instance.SlowCredits += Time.deltaTime * 40000f;
                            }
                            else if ((float)__instance.CachedCredits - __instance.SlowCredits > 1000f)
                            {
                                __instance.SlowCredits += Time.deltaTime * 2000f;
                            }
                            else if ((float)__instance.CachedCredits - __instance.SlowCredits > 80f)
                            {
                                __instance.SlowCredits += Time.deltaTime * 800f;
                            }
                            else
                            {
                                __instance.SlowCredits += Time.deltaTime * 120f;
                            }
                            __instance.SlowCredits = Mathf.Clamp(__instance.SlowCredits, 0f, (float)__instance.CachedCredits);
                        }
                        else
                        {
                            if (__instance.SlowCredits - (float)__instance.CachedCredits > 5000f)
                            {
                                __instance.SlowCredits -= Time.deltaTime * 50000f;
                            }
                            else if (__instance.SlowCredits - (float)__instance.CachedCredits > 1000f)
                            {
                                __instance.SlowCredits -= Time.deltaTime * 2500f;
                            }
                            else if (__instance.SlowCredits - (float)__instance.CachedCredits > 80f)
                            {
                                __instance.SlowCredits -= Time.deltaTime * 650f;
                            }
                            else
                            {
                                __instance.SlowCredits -= Time.deltaTime * 110f;
                            }
                            __instance.SlowCredits = Mathf.Clamp(__instance.SlowCredits, (float)__instance.CachedCredits, float.MaxValue);
                        }
                        __instance.CreditDiffBuffer += Mathf.Abs(slowCredits - __instance.SlowCredits);
                        if (__instance.CreditDiffBuffer > 1f && Time.time - __instance.LastCreditSFXPlayedTime > 0.2f * (1f - Mathf.Clamp(Mathf.Abs(__instance.SlowCredits - (float)__instance.CachedCredits) * 0.01f, 0f, 0.65f)))
                        {
                            __instance.CreditDiffBuffer = 0f;
                            __instance.LastCreditSFXPlayedTime = Time.time;
                            PLMusic.PostEvent("play_titlemenu_ui_click", PLMusic.Instance.gameObject);
                        }
                        if (PLServer.Instance.CrewPurchaseLimitsEnabled)
                        {
                            __instance.CreditsValue.text = string.Concat(new string[]
                            {
                            Mathf.RoundToInt((float)PLServer.Instance.CurrentCrewCreditsAvailableToCrew).ToString("N0"),
                            " ",
                            PLLocalize.Localize("Cr", false),
                            "\n<size=11>",
                            Mathf.RoundToInt(__instance.SlowCredits).ToString("N0"),
                            " ",
                            PLLocalize.Localize("total", false),
                            "</size>"
                            });
                        }
                        else
                        {
                            __instance.CreditsValue.text = Mathf.RoundToInt(__instance.SlowCredits).ToString("N0") + " Cr";
                        }
                        __instance.CreditsValueForceUpdate = false;
                    }
                    if (__instance.CreditsScrollTime < 1f)
                    {
                        __instance.CreditsScrolling.enabled = true;
                        __instance.CreditsScrollTime += Time.deltaTime * 0.5f;
                        __instance.CreditsScrolling.transform.localPosition = new Vector3(-130f, (__instance.CreditsScrollTime - 0.5f) * 20f);
                        float alpha = 1f - Mathf.Clamp01((__instance.CreditsScrollTime - 0.5f) * 2f);
                        __instance.CreditsScrolling.color = PLInGameUI.FromAlpha(__instance.CreditsScrolling.color, alpha);
                    }
                    else
                    {
                        __instance.CreditsScrolling.enabled = false;
                    }
                }
            }
        }
    }
}
