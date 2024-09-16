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
                    "sub"
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
                PLPersistantShipInfo plpersistantShipInfo = new PLPersistantShipInfo(EShipType.E_ABYSS_PLAYERSHIP, 0, PLServer.GetCurrentSector(), 0, false, false, true, -1, -1);
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
                Object.DestroyImmediate(prevShip);
                return;
            }

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


            PLAbyssShipInfo.Instance = null;
            await Task.Yield();
            PLAbyssShipInfo.Instance = ship as PLAbyssShipInfo;
        }
    }
}
