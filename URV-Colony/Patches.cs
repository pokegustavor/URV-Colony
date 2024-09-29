using HarmonyLib;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Threading.Tasks;
using static PLBurrowArena;

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
                Assembler.Assemble();
            }
        }

        [HarmonyPatch(typeof(PLShipStats), "CalculateStats")]
        class ApplyEMP
        {
            static void Postfix(PLShipStats __instance)
            {
                if (PLAbyssShipInfo.Instance != null && __instance.Ship != PLAbyssShipInfo.Instance)
                {
                    PLAbyssShipInfo.UpdateShipStatsBasedOnEMP(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(PLInGameUI), "Update")]
        class ShowMoney
        {
            static void Prefix(PLInGameUI __instance, ref int __state)
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

        [HarmonyPatch(typeof(PLServer), "Start")]
        class FixGalaxyPathing
        {
            static void Postfix(PLServer __instance)
            {
                CreatePathing(__instance);
            }

            static async void CreatePathing(PLServer __instance)
            {
                float cachedNeighborDist = 0f;
                int cachedGalaxySeed = -1;
                int cachedSectorCount = 0;
                List<PLSectorInfo> cachedSectors = new List<PLSectorInfo>();
                while (Application.isPlaying)
                {
                    try
                    {
                        if (__instance == null) break;
                    }
                    catch
                    {
                        break;
                    }
                    if (PLAbyssShipInfo.Instance == null || PLServer.GetCurrentSector() == null)
                    {
                        await Task.Delay(200);
                        if (PLServer.GetCurrentSector() != null && PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.ABYSS) break;
                        continue;
                    }
                    if (PLGlobal.Instance != null && PLGlobal.Instance.Galaxy != null && PLGlobal.Instance.Galaxy.IsFullySetup && PLServer.Instance != null)
                    {
                        PLShipInfo plshipInfo = PLEncounterManager.Instance.PlayerShip;
                        if (PLAcademyShipInfo.Instance != null)
                        {
                            plshipInfo = PLAcademyShipInfo.Instance;
                        }
                        if (plshipInfo != null)
                        {
                            float neighborDistance = Mathf.Max(plshipInfo.MyStats.WarpRange, __instance.GetCurrentHunterWarpRange()) * 1.1f;
                            if (neighborDistance != cachedNeighborDist || cachedGalaxySeed != PLGlobal.Instance.Galaxy.Seed || cachedSectorCount != PLGlobal.Instance.Galaxy.AllSectorInfos.Count)
                            {
                                __instance.GalaxyPathing_InProgress = true;
                                cachedNeighborDist = neighborDistance;
                                cachedSectorCount = PLGlobal.Instance.Galaxy.AllSectorInfos.Count;
                                cachedGalaxySeed = PLGlobal.Instance.Galaxy.Seed;
                                cachedSectors.Clear();
                                cachedSectors.AddRange(PLGlobal.Instance.Galaxy.AllSectorInfos.Values);
                                cachedSectors.Sort(new Comparison<PLSectorInfo>(__instance.SortSectorByID));
                                await Task.Yield();
                                int numThisFrame = 0;
                                foreach (PLSectorInfo plsectorInfo in cachedSectors)
                                {
                                    if (plsectorInfo != null && plsectorInfo.GridCell == null)
                                    {
                                        PLGlobal.Instance.Galaxy.GetGridCellForSector(plsectorInfo, true);
                                    }
                                    int num = numThisFrame;
                                    numThisFrame = num + 1;
                                    int num2 = (PLStarmap.Instance != null && PLStarmap.Instance.IsActive) ? 400 : 100;
                                    if (numThisFrame > num2)
                                    {
                                        numThisFrame = 0;
                                        await Task.Yield();
                                    }
                                }
                                List<PLSectorInfo>.Enumerator enumerator = default(List<PLSectorInfo>.Enumerator);
                                foreach (PLSectorInfo plsectorInfo2 in cachedSectors)
                                {
                                    if (plsectorInfo2 != null)
                                    {
                                        plsectorInfo2.Neighbors = PLGlobal.Instance.Galaxy.GridSearch_FindSectorsWithinRange(plsectorInfo2.Position, neighborDistance * neighborDistance, plsectorInfo2);
                                    }
                                    int num = numThisFrame;
                                    numThisFrame = num + 1;
                                    int num3 = (PLStarmap.Instance != null && PLStarmap.Instance.IsActive) ? 16 : 4;
                                    if (numThisFrame > num3)
                                    {
                                        numThisFrame = 0;
                                        await Task.Yield();
                                    }
                                }
                                enumerator = default(List<PLSectorInfo>.Enumerator);
                                __instance.GalaxyPathing_InProgress = false;
                            }
                        }
                    }
                    await Task.Delay(200);
                }
            }
        }

        [HarmonyPatch(typeof(PLAbyssTurret), "Tick")]
        class AbyssTurretBuff
        {
            static void Prefix(PLAbyssTurret __instance)
            {
                if (PLServer.GetCurrentSector().VisualIndication != ESectorVisualIndication.ABYSS)
                {
                    __instance.projSpeed_Normal = 150f;
                    __instance.projSpeed_Fast = 480f;
                }
            }
        }

        [HarmonyPatch(typeof(PLAbyssShipInfo), "SendFireProbeMsg")]
        class ReplaceFlareWithProbe
        {
            static bool Prefix(PLAbyssShipInfo __instance, int inPlayerIDOwner, Vector3 fireLoc, Quaternion fireRot)
            {
                if (PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.ABYSS) return true;
                float num = 6000f;
                float num2 = 20f;
                float num3 = num / num2;
                Vector3 normalized = (fireRot * Vector3.forward).normalized;
                Vector3 vector = fireLoc + normalized * num;
                int num4 = PLServer.Instance.GetEstimatedServerMs() + Mathf.RoundToInt(num2 * 1000f);
                RaycastHit raycastHit;
                if (Physics.Raycast(new Ray(fireLoc, normalized), out raycastHit, num, 1))
                {
                    vector = raycastHit.point;
                    float num5 = (fireLoc - vector).magnitude / num3;
                    num4 = PLServer.Instance.GetEstimatedServerMs() + Mathf.RoundToInt(num5 * 1000f);
                }
                __instance.photonView.RPC("FireProbe", PhotonTargets.All, new object[]
                {
                    fireLoc,
                    fireRot,
                    PLServer.Instance.GetEstimatedServerMs(),
                    inPlayerIDOwner,
                    num4,
                    vector
                });
                return false;
            }
        }
    }
}
