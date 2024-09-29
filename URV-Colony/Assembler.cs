using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PulsarModLoader;
using HarmonyLib;
using PulsarModLoader.Chat.Commands.CommandRouter;
using PulsarModLoader.Utilities;
using UnityEngine;
using static UIPopupList;
using ExitGames.Demos.DemoAnimator;
using UnityEngine.UI;

namespace URV_Colony
{
    internal class Assembler
    {
        class Command : ChatCommand
        {
            public override string[] CommandAliases()
            {
                return new string[]
                {
                    "urv",
                    "sub",
                    "umbra"
                };
            }

            public override string Description()
            {
                return "Spawns the URV-500";
            }

            public override void Execute(string arguments)
            {
                if (!PhotonNetwork.isMasterClient)
                {
                    Messaging.Notification("You must be the host to use this command!");
                    return;
                }
                if (PLEncounterManager.Instance.PlayerShip != null && PLEncounterManager.Instance.PlayerShip.ShipTypeID == EShipType.E_ABYSS_PLAYERSHIP)
                {
                    Messaging.Notification("Ship is already the URV-500!");
                    return;
                }
                Assemble(true);
            }
        }

        public static async void Assemble(bool newShip = false)
        {
            if (newShip)
            {
                PLShipInfo prevShip = PLEncounterManager.Instance.PlayerShip;
                PLPersistantShipInfo plpersistantShipInfo = new PLPersistantShipInfo(EShipType.E_ABYSS_PLAYERSHIP, 0, PLServer.GetCurrentSector(), 0, false, false, true, -1, -1)
                {
                    ShipName = prevShip.ShipName
                };
                PLServer.Instance.AllPSIs.Add(plpersistantShipInfo);
                PLShipInfoBase shipS = PLEncounterManager.Instance.GetCPEI().SpawnEnemyShip(plpersistantShipInfo.Type, plpersistantShipInfo, PLEncounterManager.Instance.PlayerShip.Exterior.transform.position + new Vector3(0, 200, 0), default);
                await Task.Delay(500);
                bool renable = PLServer.Instance.LongRangeCommsDisabled;
                PLServer.Instance.LongRangeCommsDisabled = true;
                PLServer.Instance.photonView.RPC("ClaimShip", PhotonTargets.All, new object[] { shipS.ShipID });
                PLServer.Instance.LongRangeCommsDisabled = renable;
                if (PLNetworkManager.Instance.MyLocalPawn != null)
                {
                    while (PLAbyssShipInfo.Instance == null || PLAbyssShipInfo.Instance.MyTLI == null)
                    {
                        await Task.Yield();
                    }
                    int ttiidofTTI = PLAbyssShipInfo.Instance.MyTLI.GetTTIIDOfTTI(PLAbyssShipInfo.Instance.MyTLI.AllTTIs[0]);
                    foreach (PLPlayer player in PLServer.Instance.AllPlayers)
                    {
                        if(player == null || player.TeamID != 0 || player.GetPawn() == null || player.photonView == null) continue;
                        player.photonView.RPC("NetworkTeleportToSubHub", PhotonTargets.All, new object[]
                        {
                        PLAbyssShipInfo.Instance.MyTLI.SubHubID,
                        ttiidofTTI
                        });
                        player.photonView.RPC("RecallPawnToPos", PhotonTargets.All, new object[]
                        {
                        ((shipS as PLShipInfo).Spawners[player.GetClassID()] as GameObject).transform.position
                        });
                    }
                }
                PhotonNetwork.Destroy(prevShip.photonView);
                return;
            }

            while (!PLLoader.Instance.IsLoaded || PLServer.GetCurrentSector() == null) 
            {
                await Task.Yield();
            }
            if (PLServer.GetCurrentSector().VisualIndication == ESectorVisualIndication.ABYSS) return;


            //Cargo pads
            PLShipInfo ship = PLAbyssShipInfo.Instance;
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CARGO, 9);
            Transform Parent = ship.InteriorStatic.transform;
            GameObject Intrepid = Resources.Load("NetworkPrefabs/Intrepid") as GameObject;
            GameObject CargoPadOri = Intrepid.transform.Find("InteriorStatic").Find("Cargo").Find("Cargo_Base_01").gameObject;
            GameObject CargoPad = Object.Instantiate(CargoPadOri, Parent);
            List<GameObject> cargo = new List<GameObject>();
            CargoPad.transform.localPosition = new Vector3(0, -3.3435f, 27.0455f);
            CargoPad.transform.localRotation = new Quaternion(0, 0, 0, 1);
            cargo.Add(CargoPad);
            for (int i = 0; i < 6; i++)
            {
                CargoPad = Object.Instantiate(CargoPadOri, Parent);
                CargoPad.transform.localPosition = new Vector3(-1.0073f + (i % 2 * 2 * 1.0073f), -3.3435f, 24.0582f - (i / 2 * 1.7927f));
                CargoPad.transform.localRotation = new Quaternion(0, 0, 0, 1);
                cargo.Add(CargoPad);
            }
            CargoPad = Object.Instantiate(CargoPadOri, Parent);
            CargoPad.transform.localPosition = new Vector3(-13.5745f, -3.191f, -7.4618f);
            CargoPad.transform.localRotation = new Quaternion(0, 0, 0, 1);
            cargo.Add(CargoPad);
            CargoPad = Object.Instantiate(CargoPadOri, Parent);
            CargoPad.transform.localPosition = new Vector3(-13.6182f, -3.191f, -9.6291f);
            CargoPad.transform.localRotation = new Quaternion(0, 0, 0, 1);
            cargo.Add(CargoPad);
            ship.CargoObjectDisplays.Clear();
            int index = 0;
            foreach (GameObject cargoslot in cargo)
            {
                Object.DontDestroyOnLoad(cargoslot);
                CargoObjectDisplay cargoDisplay = new CargoObjectDisplay();
                cargoDisplay.RootObj = cargoslot;
                cargoDisplay.DisplayedItem = null;
                cargoDisplay.DisplayObj = null;
                cargoDisplay.Index = index;
                cargoDisplay.Hidden = false;
                ship.CargoObjectDisplays.Add(cargoDisplay);
                index++;
            }
            ship.CargoBases = cargo.ToArray();

            //Safety toggle
            GameObject Safety = Object.Instantiate(Intrepid.transform.Find("InteriorStatic").Find("EngineRoom").Find("SafetySwitchboard_01").gameObject, Parent);
            GameObject Safety1 = Object.Instantiate(Intrepid.transform.Find("InteriorStatic").Find("EngineRoom").Find("MetalBlock_01").gameObject, Safety.transform);
            Safety1.transform.localPosition = new Vector3(-0.2066f, 1.0049f, 0.0363f);
            Safety1.transform.localRotation = new Quaternion(0, 0, 0, 1);
            GameObject Safety2 = Object.Instantiate(Intrepid.transform.Find("InteriorStatic").Find("EngineRoom").Find("Decal_CoreSafety").gameObject, Safety.transform);
            Safety2.transform.localPosition = new Vector3(-0.4018f, 0.974f, -0.0097f);
            Safety2.transform.localRotation = new Quaternion(0, 0.7019f, 0, 0.7122f);
            GameObject Lever = Object.Instantiate(Intrepid.transform.Find("IntreiorDynamic").Find("Lever_01").gameObject, Safety.transform);
            Lever.GetComponent<PLReactorSafetyPanel>().MyShipInfo = ship;
            Lever.transform.localPosition = new Vector3(-0.2032f, 0.0746f, 0.019f);
            Lever.transform.localRotation = new Quaternion(0, 0, 0, 1);
            Safety.transform.localPosition = new Vector3(-3.0782f, -1.8142f, -29.6182f);
            Safety.transform.rotation = new Quaternion(0, 0.8646f, 0, 0.5025f);

            //Teleporter
            PLTeleportationLocationInstance teleport = ship.MyTLI;
            teleport.ActivatingAABBVolume = null;
            teleport.AllTTIs[0].gameObject.transform.localPosition = new Vector3(6, -2, -14);
            GameObject TepScreenObj = Object.Instantiate(Intrepid.transform.Find("IntreiorDynamic").Find("Screen_TeleportScreen").gameObject, ship.InteriorDynamic.transform);
            TepScreenObj.transform.localPosition = new Vector3(6.98f, -1.9963f, -14.2036f);
            TepScreenObj.transform.localRotation = new Quaternion(0, 0.7071f, 0, -0.7071f);
            Object.DestroyImmediate(TepScreenObj.GetComponent<Light>());
            PLTeleportationScreen TepScreen = TepScreenObj.GetComponent<PLTeleportationScreen>();
            TepScreen.MyScreenHubBase = ship.MyScreenBase;
            TepScreen.MyRootPanel = null;
            TepScreen.Start();
            TepScreen.SetupUI();
            TepScreen.ScreenID = 4;

            //Exosuits
            GameObject Shelf = Object.Instantiate(Intrepid.transform.Find("InteriorStatic").Find("LifeSupportRoom").Find("Shelf_02").gameObject, ship.InteriorDynamic.transform);
            Shelf.transform.localPosition = new Vector3(5.9018f, -3.0178f, -15.3073f);
            Shelf.transform.localRotation = new Quaternion(0.0039f, 0.7071f, -0.0039f, 0.7071f);
            PLExosuitVisualAsset[] Exosuits = new PLExosuitVisualAsset[5];
            GameObject Exosuit = Object.Instantiate(Intrepid.transform.Find("InteriorStatic").Find("LifeSupportRoom").Find("ExoSuitAsset").gameObject, ship.InteriorDynamic.transform);
            Exosuit.transform.localPosition = new Vector3(4.9564f, -2.1816f, -15.34f);
            Exosuit.transform.localRotation = new Quaternion(0.0039f, 0.7071f, -0.0039f, 0.7071f);
            PLExosuitVisualAsset ExoAss = Exosuit.GetComponent<PLExosuitVisualAsset>();
            Exosuits[0] = ExoAss;
            ExoAss.SuitID = 0;
            ExoAss.MyShipInfo = ship;
            for (int i = 1; i < 5; i++)
            {
                Exosuit = Object.Instantiate(Intrepid.transform.Find("InteriorStatic").Find("LifeSupportRoom").Find("ExoSuitAsset").gameObject, ship.InteriorDynamic.transform);
                Exosuit.transform.localPosition = new Vector3(4.9564f + (i * 0.4364f), -2.1816f, -15.34f);
                Exosuit.transform.localRotation = new Quaternion(0.0039f, 0.7071f, -0.0039f, 0.7071f);
                ExoAss = Exosuit.GetComponent<PLExosuitVisualAsset>();
                Exosuits[i] = ExoAss;
                ExoAss.SuitID = i;
                ExoAss.MyShipInfo = ship;
            }
            ship.ExosuitVisualAssets = Exosuits;


            //WarpScreen
            GameObject WarpScreenObj = Object.Instantiate(Intrepid.transform.Find("IntreiorDynamic").Find("Screen_WarpDrive").gameObject, ship.InteriorDynamic.transform);
            GameObject Replacement1 = ship.InteriorDynamic.transform.Find("ClonedScreen_Status 1 (3)").gameObject;
            WarpScreenObj.transform.localPosition = Replacement1.transform.localPosition;
            WarpScreenObj.transform.localRotation = Replacement1.transform.localRotation;
            Object.DestroyImmediate(Replacement1);
            Object.DestroyImmediate(WarpScreenObj.GetComponent<Light>());
            PLWarpDriveScreen WarpScreen = WarpScreenObj.GetComponent<PLWarpDriveScreen>();
            WarpScreen.MyScreenHubBase = ship.MyScreenBase;
            WarpScreen.MyRootPanel = null;
            WarpScreen.Start();
            WarpScreen.SetupUI();
            GameObject Replacement2 = ship.InteriorDynamic.transform.Find("ClonedScreen_Status 1 (2)").gameObject;
            Replacement2.GetComponent<PLClonedScreen>().MyTargetScreen = WarpScreen;

            //CommsScreen
            GameObject CommScreenObj = Object.Instantiate(Intrepid.transform.Find("IntreiorDynamic").Find("CommsScreen").gameObject, ship.InteriorDynamic.transform);
            CommScreenObj.transform.localPosition = new Vector3(-5.3745f, - 1.658f, 25.5945f);
            CommScreenObj.transform.localEulerAngles = new Vector3(0, 100.6492f, 0);
            Object.DestroyImmediate(CommScreenObj.GetComponent<Light>());
            PLCommsScreen CommScreen = CommScreenObj.GetComponent<PLCommsScreen>();
            CommScreen.MyScreenHubBase = ship.MyScreenBase;
            CommScreen.MyRootPanel = null;
            CommScreen.Start();
            CommScreen.SetupUI();
            CommScreen.ScreenID = 8;
            GameObject CommTextObj = Object.Instantiate(Intrepid.transform.Find("InteriorStatic").Find("DialogueWorldRoot_Text").gameObject, ship.InteriorStatic.transform);
            ship.DialogueWorldRoot_Text = CommTextObj.transform;
            ship.DialogueWorldRoot_Choices = Object.Instantiate(Intrepid.transform.Find("InteriorStatic").Find("DialogueWorldRoot_Choices").gameObject, ship.InteriorStatic.transform).transform;
            Comms(ship);
            ship.DialogueTextBG.transform.localPosition = new Vector3(-125.0904f, -60.8948f, 1410.355f);
            ship.DialogueChoiceBGObj.transform.localPosition = new Vector3(-66.1816f, -60.8948f, 1312.09f);

            //VirusScreen
            GameObject VirusScreenObj = Object.Instantiate(Intrepid.transform.Find("IntreiorDynamic").Find("Scientist_VirusScreen").gameObject, ship.InteriorDynamic.transform);
            Replacement1 = ship.InteriorDynamic.transform.Find("ClonedScreen_Status 1 (1)").gameObject;
            VirusScreenObj.transform.localPosition = Replacement1.transform.localPosition;
            VirusScreenObj.transform.localRotation = Replacement1.transform.localRotation;
            Object.DestroyImmediate(Replacement1);
            Object.DestroyImmediate(VirusScreenObj.GetComponent<Light>());
            PLScientistVirusScreen VirusScreen = VirusScreenObj.GetComponent<PLScientistVirusScreen>();
            VirusScreen.MyScreenHubBase = ship.MyScreenBase;
            VirusScreen.MyRootPanel = null;
            VirusScreen.Start();
            VirusScreen.SetupUI();
            VirusScreen.ScreenID = 12;
            VirusScreen.OriginalScale = 1.5f;
            Replacement2 = ship.InteriorDynamic.transform.Find("ClonedScreen_Status 6 (2)").gameObject;
            Replacement2.GetComponent<PLClonedScreen>().MyTargetScreen = VirusScreen;

            //Atomizer
            GameObject atomizerObj = Object.Instantiate(Intrepid.transform.Find("IntreiorDynamic").Find("Research_Atomizer_Frame_01").gameObject, ship.InteriorDynamic.transform);
            ship.ResearchLockerAnimator = atomizerObj.transform.GetComponentInChildren<Animation>();
            ship.ResearchLockerFrame = atomizerObj.transform.GetComponent<MeshRenderer>();
            ship.ResearchLockerCollider = Object.Instantiate(Intrepid.transform.Find("IntreiorDynamic").Find("ResearchLockerCollider").gameObject, ship.InteriorDynamic.transform).transform.GetComponent<BoxCollider>();
            ship.ResearchLockerWorldRoot = Object.Instantiate(Intrepid.transform.Find("IntreiorDynamic").Find("ResearchLockerWorldUI").gameObject, ship.InteriorDynamic.transform).transform;
            ship.ResearchLockerCollider.transform.SetParent(atomizerObj.transform);
            ship.ResearchLockerWorldRoot.transform.SetParent(atomizerObj.transform);
            atomizerObj.transform.localPosition = new Vector3(13.1418f, -2.3236f, -11.32f);
            atomizerObj.transform.localEulerAngles = new Vector3(0, 160.1154f, 0);
            Atomizer(ship);
            ship.ResearchLockerWorldRootBGObj.transform.localPosition = new Vector3(667.75f, -80.7892f, -560);
            ship.ResearchLockerWorldRootBGObj.transform.localEulerAngles = new Vector3(0, 158.5544f, 0);

            //CPU and jump processor
            ship.MyStats.SetSlotLimit(ESlotType.E_COMP_CPU, 4);
            ship.MyStats.AddShipComponent(PLShipComponent.CreateShipComponentFromHash((int)PLShipComponent.createHashFromInfo(7, 0, 0, 0, 12), null), -1, ESlotType.E_COMP_CPU);

            //Turret Icons
            for (int i = 1; i < 3; i++) 
            {
                PLTurret turret = ship.GetTurretAtID(i);
                if (turret != null && turret.SubType == 19 && turret is PLAbyssTurret)
                {
                    turret.m_IconTexture = (Texture2D)Resources.Load("Icons/10_Weapons");
                }
            }
        }

        static void Comms(PLShipInfo ship) 
        {

            GameObject gameObject = ship.InteriorStatic.transform.Find("ShipWorldUICanvas").gameObject;
            GameObject gameObject2 = new GameObject("DialogueBG", new System.Type[]
            {
                typeof(Image)
            });
            gameObject2.transform.SetParent(gameObject.transform);
            gameObject2.transform.localPosition = new Vector3(-125.0904f, - 60.8948f, 1410.355f);
            gameObject2.transform.localEulerAngles = new Vector3(0, 16.7273f, 0);
            gameObject2.transform.localScale = new Vector3(0.18f,0.18f,1);
            gameObject2.layer = 3;
            ship.DialogueTextBG = gameObject2.GetComponent<Image>();
            ship.DialogueTextBG.sprite = PLGlobal.Instance.TabFillSprite;
            ship.DialogueTextBG.type = Image.Type.Sliced;
            ship.DialogueTextBG.color = new Color(0.5f, 0.5f, 1f, 0.1f);
            ship.DialogueTextBG.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
            ship.DialogueTextBG.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 350f);
            ship.DialogueTextBG.raycastTarget = false;
            gameObject2.transform.localPosition = ship.DialogueWorldRoot_Text.transform.localPosition / 0.02f;
            GameObject gameObject3 = new GameObject("CloseCommsBtnGO", new System.Type[]
            {
                typeof(Image),
                typeof(Button)
            });
            Button component2 = gameObject3.GetComponent<Button>();
            gameObject3.GetComponent<Image>().sprite = PLGlobal.Instance.CloseCommsSprite;
            component2.transform.SetParent(ship.DialogueTextBG.transform);
            component2.transform.localPosition = new Vector3(280f, 187f, 0f);
            component2.transform.localRotation = Quaternion.identity;
            component2.transform.localScale = Vector3.one;
            component2.gameObject.layer = 3;
            ColorBlock colors = component2.colors;
            colors.normalColor = Color.gray;
            component2.colors = colors;
            component2.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32f);
            component2.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32f);
            component2.transform.localPosition = new Vector3(280f, 187f, 0f);
            component2.onClick.AddListener(delegate ()
            {
                PLCommsScreen.SelectHailTarget(null, ship);
            });
            ship.DialogueChoiceBGObj = new GameObject("DialogueChoiceBG", new System.Type[]
            {
                typeof(Image)
            });
            ship.DialogueChoiceBGObj.transform.SetParent(gameObject.transform);
            ship.DialogueChoiceBGObj.transform.localPosition = new Vector3(-66.1816f, - 60.8948f, 1312.09f);
            ship.DialogueChoiceBGObj.transform.localEulerAngles = new Vector3(0, 90.6673f, 0);
            ship.DialogueChoiceBGObj.transform.localScale = new Vector3(0.18f,0.18f,1);
            ship.DialogueChoiceBGObj.layer = 3;
            ship.DialogueChoiceBG = ship.DialogueChoiceBGObj.GetComponent<Image>();
            ship.DialogueChoiceBG.sprite = PLGlobal.Instance.TabFillSprite;
            ship.DialogueChoiceBG.type = Image.Type.Sliced;
            ship.DialogueChoiceBG.color = new Color(0.5f, 0.5f, 1f, 0.1f);
            ship.DialogueChoiceBG.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
            ship.DialogueChoiceBG.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 340f);
            ship.DialogueChoiceBGObj.transform.localPosition = ship.DialogueWorldRoot_Choices.transform.localPosition / 0.02f;
            GameObject gameObject4 = new GameObject("DialogueChoiceBGTimer", new System.Type[]
            {
                typeof(Image)
            });
            gameObject4.transform.SetParent(ship.DialogueChoiceBGObj.transform);
            gameObject4.transform.localPosition = Vector3.one;
            gameObject4.transform.localRotation = Quaternion.identity;
            gameObject4.transform.localScale = Vector3.one;
            gameObject4.layer = 3;
            ship.DialogueChoiceBG_Timer = gameObject4.GetComponent<Image>();
            ship.DialogueChoiceBG_Timer.sprite = PLGlobal.Instance.DialogueChoiceBG_Timer;
            ship.DialogueChoiceBG_Timer.type = Image.Type.Filled;
            ship.DialogueChoiceBG_Timer.fillMethod = Image.FillMethod.Radial360;
            ship.DialogueChoiceBG_Timer.fillOrigin = 3;
            ship.DialogueChoiceBG_Timer.color = new Color(1f, 0f, 0f, 1f);
            ship.DialogueChoiceBG_Timer.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
            ship.DialogueChoiceBG_Timer.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 340f);
            gameObject4.transform.localPosition = Vector3.one;
            ship.CreateOutlineObject(ship.DialogueChoiceBG.transform, PLGlobal.Instance.OutlineSprite, new Vector3(0f, 0f, 4f), 0.3f, 340f);
            ship.CreateOutlineObject(ship.DialogueChoiceBG.transform, PLGlobal.Instance.OutlineSprite, new Vector3(0f, 0f, -6f), 0.05f, 340f);
            ship.CreateOutlineObject(ship.DialogueChoiceBG.transform, PLGlobal.Instance.OutlineSprite, new Vector3(0f, 0f, -12f), 0.05f, 340f);
            ship.CreateOutlineObject(ship.DialogueChoiceBG.transform, PLGlobal.Instance.OutlineSprite, new Vector3(0f, 0f, -18f), 0.02f, 340f);
            ship.CreateOutlineObject(ship.DialogueTextBG.transform, PLGlobal.Instance.OutlineSprite, new Vector3(0f, 0f, 4f), 0.3f, 350f);
            ship.CreateOutlineObject(ship.DialogueTextBG.transform, PLGlobal.Instance.OutlineSprite, new Vector3(0f, 0f, -6f), 0.05f, 350f);
            ship.CreateOutlineObject(ship.DialogueTextBG.transform, PLGlobal.Instance.OutlineSprite, new Vector3(0f, 0f, -12f), 0.05f, 350f);
            ship.CreateOutlineObject(ship.DialogueTextBG.transform, PLGlobal.Instance.OutlineSprite, new Vector3(0f, 0f, -18f), 0.02f, 350f);
            ship.DialogueGlitchImages = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                GameObject gameObject5 = new GameObject("dialogueGlitch", new System.Type[]
                {
                    typeof(Image)
                });
                Image component3 = gameObject5.GetComponent<Image>();
                if (i < 4)
                {
                    gameObject5.transform.SetParent(ship.DialogueTextBG.transform);
                }
                else
                {
                    gameObject5.transform.SetParent(ship.DialogueChoiceBG.transform);
                }
                gameObject5.transform.localPosition = new Vector3(0f, 0f, 5f);
                gameObject5.transform.localRotation = Quaternion.identity;
                gameObject5.transform.localScale = Vector3.one;
                gameObject5.layer = 3;
                component3.sprite = PLGlobal.Instance.TabShadowSprite;
                component3.type = Image.Type.Sliced;
                component3.color = new Color(0.5f, 0.5f, 1f, 0.1f);
                component3.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
                component3.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, UnityEngine.Random.Range(70f, 190f));
                gameObject5.transform.localPosition = new Vector3(0f, 0f, 5f);
                component3.raycastTarget = false;
                ship.DialogueGlitchImages[i] = component3;
            }
            GameObject gameObject6 = new GameObject("DialogueTitle", new System.Type[]
            {
                typeof(Text)
            });
            gameObject6.transform.SetParent(gameObject2.transform);
            gameObject6.transform.localPosition = new Vector3(20f, 0f, 0f);
            gameObject6.transform.localRotation = Quaternion.identity;
            gameObject6.transform.localScale = Vector3.one;
            gameObject6.layer = 3;
            ship.DialogueTitle = gameObject6.GetComponent<Text>();
            ship.DialogueTitle.font = PLGlobal.Instance.MainFont;
            ship.DialogueTitle.alignment = TextAnchor.UpperLeft;
            ship.DialogueTitle.resizeTextForBestFit = true;
            ship.DialogueTitle.resizeTextMaxSize = 28;
            ship.DialogueTitle.color = Color.white * 0.75f;
            ship.DialogueTitle.raycastTarget = false;
            ship.DialogueTitle.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 620f);
            ship.DialogueTitle.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 410f);
            gameObject6.transform.localPosition = new Vector3(20f, 0f, 0f);
            ship.DialogueTitle.text = "";
            GameObject gameObject7 = new GameObject("DialogueTextRight", new System.Type[]
            {
                typeof(Text)
            });
            gameObject7.transform.SetParent(gameObject2.transform);
            gameObject7.transform.localPosition = new Vector3(0f, 0f, 0f);
            gameObject7.transform.localRotation = Quaternion.identity;
            gameObject7.transform.localScale = Vector3.one;
            gameObject7.layer = 3;
            ship.DialogueTextRight = gameObject7.GetComponent<Text>();
            ship.DialogueTextRight.font = PLGlobal.Instance.MainFont;
            ship.DialogueTextRight.alignment = TextAnchor.LowerRight;
            ship.DialogueTextRight.resizeTextForBestFit = false;
            ship.DialogueTextRight.fontSize = 14;
            ship.DialogueTextRight.color = Color.white * 0.8f;
            ship.DialogueTextRight.verticalOverflow = VerticalWrapMode.Overflow;
            ship.DialogueTextRight.horizontalOverflow = HorizontalWrapMode.Overflow;
            ship.DialogueTextRight.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 540f);
            ship.DialogueTextRight.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300f);
            gameObject7.transform.localPosition = new Vector3(0f, 0f, 0f);
            ship.DialogueTextRight.text = "";
            GameObject gameObject8 = new GameObject("DialogueText", new System.Type[]
            {
                typeof(Text)
            });
            gameObject8.transform.SetParent(gameObject2.transform);
            gameObject8.transform.localPosition = new Vector3(0f, 0f, 0f);
            gameObject8.transform.localRotation = Quaternion.identity;
            gameObject8.transform.localScale = Vector3.one;
            gameObject8.layer = 3;
            ship.DialogueText = gameObject8.GetComponent<Text>();
            ship.DialogueText.font = PLGlobal.Instance.MainFont;
            ship.DialogueText.alignment = TextAnchor.LowerLeft;
            ship.DialogueText.resizeTextForBestFit = false;
            ship.DialogueText.fontSize = 14;
            ship.DialogueText.color = Color.white * 0.8f;
            ship.DialogueText.verticalOverflow = VerticalWrapMode.Overflow;
            ship.DialogueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            ship.DialogueText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 540f);
            ship.DialogueText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300f);
            gameObject8.transform.SetParent(gameObject2.transform);
            gameObject8.transform.localPosition = new Vector3(0f, 0f, 0f);
            ship.DialogueText.text = "";
            GameObject gameObject9 = new GameObject("DialogueChoice_PageLabel", new System.Type[]
            {
                typeof(Text)
            });
            ship.DialogueChoice_PageLabel = gameObject9.GetComponent<Text>();
            ship.DialogueChoice_PageLabel.font = PLGlobal.Instance.MainFont;
            ship.DialogueChoice_PageLabel.transform.SetParent(ship.DialogueChoiceBG.transform);
            ship.DialogueChoice_PageLabel.alignment = TextAnchor.MiddleCenter;
            ship.DialogueChoice_PageLabel.transform.localPosition = new Vector3(0f, 110f, 0f);
            ship.DialogueChoice_PageLabel.transform.localRotation = Quaternion.identity;
            ship.DialogueChoice_PageLabel.transform.localScale = Vector3.one;
            ship.DialogueChoice_PageLabel.gameObject.layer = 3;
            ship.DialogueChoice_PageLabel.resizeTextForBestFit = true;
            ship.DialogueChoice_PageLabel.resizeTextMinSize = 8;
            ship.DialogueChoice_PageLabel.resizeTextMaxSize = 18;
            ship.DialogueChoice_PageLabel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 80f);
            ship.DialogueChoice_PageLabel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25f);
            ship.DialogueChoice_PageLabel.transform.localPosition = new Vector3(0f, 110f, 0f);
            GameObject gameObject10 = new GameObject("DialogueChoice_Left", new System.Type[]
            {
                typeof(Image),
                typeof(Button)
            });
            ship.DialogueChoice_Left = gameObject10.GetComponent<Button>();
            gameObject10.GetComponent<Image>().sprite = PLGlobal.Instance.LeftArrowSprite;
            ship.DialogueChoice_Left.transform.SetParent(ship.DialogueChoiceBG.transform);
            ship.DialogueChoice_Left.transform.localPosition = new Vector3(-40f, 110f, 0f);
            ship.DialogueChoice_Left.transform.localRotation = Quaternion.identity;
            ship.DialogueChoice_Left.transform.localScale = Vector3.one;
            ship.DialogueChoice_Left.gameObject.layer = 3;
            ship.DialogueChoice_Left.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32f);
            ship.DialogueChoice_Left.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32f);
            ship.DialogueChoice_Left.transform.localPosition = new Vector3(-40f, 110f, 0f);
            ship.DialogueChoice_Left.onClick.AddListener(delegate ()
            {
                ship.DialogueChoicePage--;
            });
            GameObject gameObject11 = new GameObject("DialogueChoice_Right", new System.Type[]
            {
                typeof(Image),
                typeof(Button)
            });
            ship.DialogueChoice_Right = gameObject11.GetComponent<Button>();
            gameObject11.GetComponent<Image>().sprite = PLGlobal.Instance.RightArrowSprite;
            ship.DialogueChoice_Right.transform.SetParent(ship.DialogueChoiceBG.transform);
            ship.DialogueChoice_Right.transform.localPosition = new Vector3(40f, 110f, 0f);
            ship.DialogueChoice_Right.transform.localRotation = Quaternion.identity;
            ship.DialogueChoice_Right.transform.localScale = Vector3.one;
            ship.DialogueChoice_Right.gameObject.layer = 3;
            ship.DialogueChoice_Right.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32f);
            ship.DialogueChoice_Right.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 32f);
            ship.DialogueChoice_Right.transform.localPosition = new Vector3(40f, 110f, 0f);
            ship.DialogueChoice_Right.onClick.AddListener(delegate ()
            {
                ship.DialogueChoicePage++;
            });
        }

        static void Atomizer(PLShipInfo ship) 
        {
            ship.ResearchLockerWorldRootBGObj = new GameObject("ResearchLockerWorldRootBG", new System.Type[]
            {
                typeof(Image)
            });
            ship.ResearchLockerWorldRootBGObj.transform.SetParent(ship.worldUiCanvas.gameObject.transform);
            ship.ResearchLockerWorldRootBGObj.transform.localPosition = ship.ResearchLockerWorldRoot.transform.localPosition / 0.02f;
            ship.ResearchLockerWorldRootBGObj.transform.localRotation = ship.ResearchLockerWorldRoot.transform.localRotation;
            ship.ResearchLockerWorldRootBGObj.transform.localScale = ship.ResearchLockerWorldRoot.transform.localScale * 50f;
            ship.ResearchLockerWorldRootBGObj.layer = 3;
            ship.ResearchLockerWorldRoot_RoomArea = ship.GetRoomAreaForTransform(ship.ResearchLockerWorldRootBGObj.transform);
            Image component = ship.ResearchLockerWorldRootBGObj.GetComponent<Image>();
            component.sprite = PLGlobal.Instance.TabFillSprite;
            component.type = Image.Type.Sliced;
            component.color = new Color(0.5f, 1f, 0.5f, 0.1f);
            component.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 160f);
            component.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 160f);
            ship.ResearchLockerWorldRootBGObj.transform.localPosition = ship.ResearchLockerWorldRoot.transform.localPosition / 0.02f;
            GameObject gameObject = new GameObject("ResearchLockerTitle", new System.Type[]
            {
                typeof(Text)
            });
            gameObject.transform.SetParent(component.transform);
            gameObject.transform.localPosition = new Vector3(0f, 30f, 0f);
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            gameObject.layer = 3;
            Text component2 = gameObject.GetComponent<Text>();
            component2.font = PLGlobal.Instance.MainFont;
            component2.alignment = TextAnchor.MiddleCenter;
            component2.resizeTextForBestFit = true;
            component2.resizeTextMinSize = 8;
            component2.resizeTextMaxSize = 30;
            component2.color = Color.green;
            component2.text = "ATOMIZER";
            component2.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 145f);
            component2.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 45f);
            gameObject.transform.localPosition = new Vector3(0f, 30f, 0f);
            GameObject gameObject2 = new GameObject("ResearchProcessLockerButton", new System.Type[]
            {
                typeof(Image),
                typeof(Button)
            });
            gameObject2.transform.SetParent(component.transform);
            gameObject2.transform.localPosition = new Vector3(0f, -30f, 0f);
            gameObject2.transform.localRotation = Quaternion.identity;
            gameObject2.transform.localScale = Vector3.one;
            gameObject2.layer = 3;
            ship.ProcessLockerButton = gameObject2.GetComponent<Button>();
            Image component3 = gameObject2.GetComponent<Image>();
            ColorBlock colors = ship.ProcessLockerButton.colors;
            colors.normalColor = Color.green * 0.5f;
            ship.ProcessLockerButton.colors = colors;
            component3.sprite = PLGlobal.Instance.TabFillSprite;
            component3.type = Image.Type.Sliced;
            component3.color = new Color(0f, 1f, 0f, 1f);
            component3.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 140f);
            component3.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
            gameObject2.transform.localPosition = new Vector3(0f, -30f, 0f);
            GameObject gameObject3 = new GameObject("ResearchResearchButtonText", new System.Type[]
            {
                typeof(Text)
            });
            gameObject3.transform.SetParent(component3.transform);
            gameObject3.transform.localPosition = new Vector3(0f, 0f, 0f);
            gameObject3.transform.localRotation = Quaternion.identity;
            gameObject3.transform.localScale = Vector3.one;
            gameObject3.layer = 3;
            ship.ProcessButtonText = gameObject3.GetComponent<Text>();
            ship.ProcessButtonText.font = PLGlobal.Instance.MainFont;
            ship.ProcessButtonText.fontSize = 13;
            ship.ProcessButtonText.alignment = TextAnchor.MiddleCenter;
            ship.ProcessButtonText.color = Color.black;
            ship.ProcessButtonText.text = PLLocalize.Localize("READY", false);
            ship.ProcessLockerButton.onClick.AddListener(delegate ()
            {
                ship.ClickAtomize();
            });
            ship.ProcessButtonText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 140f);
            ship.ProcessButtonText.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
            gameObject3.transform.localPosition = new Vector3(0f, 0f, 0f);
        }
    }
}
