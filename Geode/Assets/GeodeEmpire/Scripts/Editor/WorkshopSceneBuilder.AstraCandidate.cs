using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GeodeEmpire.Build;
using GeodeEmpire.Core;
using GeodeEmpire.Retail;
using GeodeEmpire.Workshop;
using GeodeEmpire.UI;

namespace GeodeEmpire.EditorTools
{
    public static partial class WorkshopSceneBuilder
    {
        public const string AstraCandidatePath = "Assets/GeodeEmpire/Scenes/Workshop_AstraCandidate.unity";

        /// <summary>
        /// Apply the measured shell and primary fixtures to a native copy of Workshop, preserving component IDs.
        /// Career startup is disabled until migration and the remaining lease/fixture integration are validated.
        /// This scene is deliberately absent from build settings; the source Workshop is never saved here.
        /// </summary>
        [MenuItem("GeodeEmpire/Astra/Create Workshop Validation Copy")]
        public static void CreateAstraWorkshopCandidate()
        {
            var source = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || source.path != "Assets/GeodeEmpire/Scenes/Workshop.unity")
                throw new InvalidOperationException("Open the clean production Workshop in Edit Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("A loaded scene is dirty; candidate creation stopped.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AstraCandidatePath) != null)
                throw new InvalidOperationException("The validation copy already exists. Inspect and continue it; do not duplicate the operation.");
            var deliveryBefore = DeliveryFileIds(source);
            if (deliveryBefore.Length != 3) throw new InvalidOperationException("Expected the three repaired authored Delivery components.");
            if (!EditorSceneManager.SaveScene(source, AstraCandidatePath, true)) throw new InvalidOperationException("Could not copy Workshop.");
            var scene = EditorSceneManager.OpenScene(AstraCandidatePath, OpenSceneMode.Single);
            var env = CandidatePath(scene, "Environment");
            var stations = CandidatePath(scene, "Stations");
            var premises = CandidatePath(scene, "Environment/Premises").GetComponent<PremisesExpansion>();
            var retail = CandidatePath(scene, "Stations/RetailShop").GetComponent<RetailShop>();
            var session = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameSession>(true)).Single();
            if (premises == null || retail == null) throw new InvalidOperationException("Premises or retail component missing.");
            Undo.RecordObject(session, "Hold career startup for architecture migration");
            session.enabled = false;
            var archive = CandidateRoot(env, "LegacyGeometry_PENDING_REAUTHOR");
            archive.gameObject.tag = "EditorOnly";
            archive.gameObject.SetActive(false);

            // Keep the actual fixture objects and their zone/camera wiring; only move their existing roots.
            var fixtures = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlaceableFixture>(true)).ToArray();
            foreach (var fixture in fixtures)
            {
                Undo.SetTransformParent(fixture.transform, stations, "Keep owned fixture outside lease roots");
                var space = AstraLayout.Spaces.FirstOrDefault(s => s.Id == fixture.Id);
                if (space != null) CandidatePose(fixture.transform, space.Centre, space.Yaw);
            }
            // The shell is replaced as a whole. Preserve old authored objects in this review-only archive.
            foreach (Transform child in env.Cast<Transform>().ToArray())
                if (child != archive && child != premises.transform) Undo.SetTransformParent(child, archive, "Archive replaced shell");

            var shell = CandidateRoot(env, "AstraArchitecture");
            Undo.AddComponent<AstraWorkshop>(shell.gameObject);
            var plaster = StudyMaterial("Astra_Plaster_Layout", new Color(.76f, .75f, .70f));
            var concrete = StudyMaterial("Astra_Concrete_Layout", new Color(.48f, .49f, .47f));
            var cream = StudyMaterial("Astra_Cream_Layout", new Color(.78f, .77f, .70f));
            var blue = StudyMaterial("Astra_BlueSteel_Layout", new Color(.15f, .28f, .34f));
            BuildAstraShell(shell, plaster, concrete, cream);
            foreach (Transform floor in shell)
                if (floor.name.EndsWith("_Floor", StringComparison.Ordinal)) floor.name = "Floor";
            StudyBox(shell, "Ceiling", new Vector3(.3f, 2.96f, 1.65f), new Vector3(13.4f, .12f, 8.7f), plaster);
            StudyBox(shell, "StarterWindowGlass", new Vector3(-2.725f, 1.60f, -2.77f), new Vector3(3.95f, 1.51f, .018f), WorkshopMaterials.Get("M_CaseGlass"));

            // Remove old whole-room fit-out from the primary floor. Earned bodies are integrated next.
            var keep = new HashSet<string> { "CrackingBench", "WashStation", "AppraisalStation", "StorageShelf", "SellOutbox",
                "ReceivingArea", "FixtureDelivery", "DisplayCabinet", "SawStation", "PolishStation", "WorkshopExpansion", "RetailShop", "PlayerStart" };
            foreach (Transform child in stations.Cast<Transform>().ToArray())
                if (!keep.Contains(child.name) && child.GetComponent<PlaceableFixture>() == null)
                    Undo.SetTransformParent(child, archive, "Archive superseded starter decoration");

            var bench = CandidatePath(scene, "Stations/CrackingBench");
            CandidatePose(bench, AstraLayout.Get("cracking_bench").Centre, 90f);
            var stool = bench.Find("stool");
            if (stool != null) Undo.SetTransformParent(stool, archive, "Remove stool from working aisle");
            var wash = CandidatePath(scene, "Stations/WashStation");
            CandidatePose(wash, AstraLayout.Get("wash_station").Centre, 90f); // current carcase +Z is its working side
            var inspection = CandidatePath(scene, "Stations/AppraisalStation");
            CandidatePose(inspection, AstraLayout.Get("inspection_station").Centre, 180f);

            // Reuse the exact checkout kit and every payment/slot reference, reorienting its parent only once.
            var starter = CandidatePath(scene, "Stations/RetailShop/StarterCounter");
            CandidatePose(starter, AstraLayout.Get("starter_checkout").Centre, 90f);
            CandidatePoint(retail.StarterOutside, new Vector3(-5.6f, 0f, -3.6f));
            CandidatePoint(retail.StarterDoor, new Vector3(-5.6f, 0f, -2.1f));
            CandidatePoint(retail.StarterCounterCustomer, new Vector3(-4.45f, 0f, -.9f));
            for (int i = 0; i < retail.StarterQueue.Count; i++)
                CandidatePoint(retail.StarterQueue[i], new Vector3(AstraLayout.StarterQueue[i].x, 0f, AstraLayout.StarterQueue[i].y));
            var oldSign = starter.Find("Sign_SHOP");
            if (oldSign != null) Undo.SetTransformParent(oldSign, archive, "Replace starter entrance sign");
            Undo.RecordObject(premises, "Install minimum starter checkout");
            premises.Gates.RemoveAll(g => g != null && g.Root == starter.gameObject);
            if (!premises.HideWithShopFront.Contains(starter.gameObject)) premises.HideWithShopFront.Add(starter.gameObject);
            Undo.RecordObject(starter.gameObject, "Show starter checkout"); starter.gameObject.SetActive(true);

            FinishAstraCandidatePrimaryLayout(deliveryBefore);
        }

        /// <summary>Resume the exact primary-layout operation after the copy/shell/starter have been saved.</summary>
        public static void FinishAstraCandidatePrimaryLayout(ulong[] deliveryBefore)
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != AstraCandidatePath)
                throw new InvalidOperationException("Open the existing Astra validation copy in Edit Mode.");
            var env = CandidatePath(scene, "Environment");
            var shell = CandidatePath(scene, "Environment/AstraArchitecture");
            if (shell.Find("StarterEntrance") != null) throw new InvalidOperationException("Primary layout is already finished; inspect it instead of repeating.");
            var archive = CandidatePath(scene, "Environment/LegacyGeometry_PENDING_REAUTHOR");
            var premises = CandidatePath(scene, "Environment/Premises").GetComponent<PremisesExpansion>();
            var retail = CandidatePath(scene, "Stations/RetailShop").GetComponent<RetailShop>();
            var starter = CandidatePath(scene, "Stations/RetailShop/StarterCounter");
            var inspection = CandidatePath(scene, "Stations/AppraisalStation");
            var fixtures = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlaceableFixture>(true)).ToArray();
            var blue = StudyMaterial("Astra_BlueSteel_Layout", new Color(.15f, .28f, .34f));
            var mainStation = retail.ShowroomCounterItem != null ? retail.ShowroomCounterItem.GetComponentInParent<Checkout.CheckoutStation>(true) : null;
            if (mainStation == null) throw new InvalidOperationException("Inactive showroom checkout reference missing.");
            var mainCounter = mainStation.transform;
            CandidatePose(mainCounter, AstraLayout.Get("showroom_checkout").Centre, 270f);
            CandidatePoint(retail.ShowroomOutside, new Vector3(5.6f, 0f, -3.6f));
            CandidatePoint(retail.ShowroomDoor, new Vector3(5.6f, 0f, -2.1f));
            CandidatePoint(retail.ShowroomCounterCustomer, new Vector3(3.8f, 0f, -.65f));
            for (int i = 0; i < retail.ShowroomQueue.Count; i++) CandidatePoint(retail.ShowroomQueue[i], new Vector3(4.55f + i * .7f, 0f, -.65f));
            Undo.RecordObject(retail, "Use physical entrance doors"); retail.ShowroomDoorLeaf = null;
            CandidateDoor(shell, "StarterEntrance", -5.6f, blue);
            CandidateDoor(premises.ShopFrontRoot.transform, "ShowroomEntrance", 5.6f, blue);

            // Lease closures exactly fill the measured openings, instead of slicing through the starter room.
            Undo.SetTransformParent(premises.BackRoomHoarding.transform, archive, "Archive old back-room boards");
            Undo.SetTransformParent(premises.ShopFrontHoarding.transform, archive, "Archive old shop hoarding");
            var backClosed = CandidateRoot(shell, "BackRoomClosed");
            StudyBox(backClosed, "ClosedStaffDoor", new Vector3(-3.3f, 1.1f, 1.3f), new Vector3(1.2f, 2.2f, .10f), blue);
            StudyBox(backClosed, "ClosedOfficeDoor", new Vector3(.5f, 1.1f, 1.3f), new Vector3(1.2f, 2.2f, .10f), blue);
            var shopClosed = CandidateRoot(shell, "ShowroomClosed");
            StudyBox(shopClosed, "ClosedShowroomStaffDoor", new Vector3(1.4f, 1.1f, 2.6f), new Vector3(.10f, 2.2f, 1.2f), blue);
            StudyBox(shopClosed, "ClosedStreetDoor", new Vector3(5.6f, 1.1f, -2.77f), new Vector3(1.15f, 2.2f, .10f), blue);
            premises.BackRoomHoarding = backClosed.gameObject;
            premises.ShopFrontHoarding = shopClosed.gameObject;
            premises.HideWithBackRoom.Clear();
            var receiving = CandidatePath(scene, "Stations/ReceivingArea").GetComponent<ReceivingArea>();
            Undo.RecordObject(receiving, "Configure shared receiving marks");
            CandidatePoint(receiving.KerbAnchor, Vector3.zero); CandidatePoint(receiving.BayAnchor, Vector3.zero);
            receiving.SharedDeliveries = true;
            receiving.StarterCells = new[] { new Vector3(-1.2f, .12f, -2.05f), new Vector3(-2.55f, .12f, -2.05f) };
            receiving.BayCells = new[] { new Vector3(-4.7f, .12f, 4.47f), new Vector3(-3.35f, .12f, 4.47f),
                new Vector3(-4.7f, .12f, 5.47f), new Vector3(-3.35f, .12f, 5.47f) };
            foreach (var cell in receiving.StarterCells.Concat(receiving.BayCells))
                StudyBox(shell, "ReceivingMark", new Vector3(cell.x, .005f, cell.z), new Vector3(1.2f, .005f, .8f), blue, false);
            CandidatePoint(CandidatePath(scene, "Stations/PlayerStart"), new Vector3(-5.6f, 0f, -1.85f));
            CandidatePoint(CandidatePath(scene, "Player"), new Vector3(-5.6f, .08f, -1.85f));

            // The legacy tablet remains the input surface until the Blender laptop pass, with its own exact ID.
            var tablet = inspection.GetComponentInChildren<OrderTablet>(true);
            if (tablet == null) throw new InvalidOperationException("Management tablet is missing.");
            Undo.SetTransformParent(tablet.transform, starter, "Keep management access in the minimum kit");
            Undo.RecordObject(tablet.transform, "Seat management tablet");
            tablet.transform.SetPositionAndRotation(new Vector3(-3.32f, .95f, .05f), Quaternion.Euler(0f, 180f, 0f));

            // Neutral temporary illumination for real geometry checks. Art/material acceptance follows in Blender.
            var light = CandidateRoot(shell, "LayoutDaylight").gameObject;
            var sun = Undo.AddComponent<Light>(light); sun.type = LightType.Directional; sun.intensity = 1.0f;
            sun.transform.rotation = Quaternion.Euler(55f, -30f, 0f); sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.60f, .63f, .67f);
            foreach (var fixture in fixtures) fixture.CaptureDefault(true);
            Physics.SyncTransforms();
            if (!deliveryBefore.SequenceEqual(DeliveryFileIds(scene))) throw new InvalidOperationException("Delivery component IDs changed; candidate must not be promoted.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AstraCandidatePath)) throw new InvalidOperationException("Could not save the candidate.");
        }

        private static Transform CandidatePath(Scene scene, string path)
        {
            var parts = path.Split('/');
            var root = scene.GetRootGameObjects().SingleOrDefault(r => r.name == parts[0]);
            var result = root != null ? (parts.Length == 1 ? root.transform : root.transform.Find(string.Join("/", parts.Skip(1)))) : null;
            if (result == null) throw new InvalidOperationException("Missing candidate path: " + path);
            return result;
        }

        /// <summary>Minimum starter kit and earned station bodies; continues the existing candidate in place.</summary>
        public static void ConfigureAstraCandidateMinimumKit()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != AstraCandidatePath)
                throw new InvalidOperationException("Open the existing Astra validation copy in Edit Mode.");
            var archive = CandidatePath(scene, "Environment/LegacyGeometry_PENDING_REAUTHOR");
            var bench = CandidatePath(scene, "Stations/CrackingBench");
            var wash = CandidatePath(scene, "Stations/WashStation");
            var inspection = CandidatePath(scene, "Stations/AppraisalStation");
            var storage = CandidatePath(scene, "Stations/StorageShelf");
            var outbox = CandidatePath(scene, "Stations/SellOutbox");
            var pallet = outbox.Find("pallet");
            if (pallet != null) Undo.SetTransformParent(pallet, archive, "Remove separate starter dealer pallet");
            Undo.RecordObject(outbox, "Seat dealer tray on the bench shelf");
            outbox.SetPositionAndRotation(bench.TransformPoint(new Vector3(-.45f, .16f, 0f)), bench.rotation);
            var intercom = outbox.Find("DealerIntercom");
            if (intercom == null) throw new InvalidOperationException("Dealer intercom missing.");
            Undo.RecordObject(intercom, "Seat dealer intercom at the working side");
            intercom.SetPositionAndRotation(bench.TransformPoint(new Vector3(-.65f, 1.2f, .34f)), bench.rotation);
            CandidateEarnedBody(wash, Economy.UpgradeCatalog.WashStation, "Manual Wash Station", new Vector2(1.15f, .80f), 1.35f, new Vector2(0f, 1f));
            CandidateEarnedBody(inspection, Economy.UpgradeCatalog.AppraisalStation, "Inspection & Appraisal Bench", new Vector2(1.35f, .64f), 1.55f, new Vector2(0f, -1f));
            CandidatePose(storage, new Vector2(.95f, .15f), 90f);
            CandidateEarnedBody(storage, Economy.UpgradeCatalog.StorageShelf, "Utility Shelving", new Vector2(.92f, .40f), 1.85f, new Vector2(0f, -1f));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AstraCandidatePath)) throw new InvalidOperationException("Could not save the candidate minimum kit.");
        }

        private static void CandidateEarnedBody(Transform root, string id, string title, Vector2 footprint, float height, Vector2 workingSide)
        {
            var fixture = root.GetComponent<PlaceableFixture>();
            if (fixture == null) fixture = Undo.AddComponent<PlaceableFixture>(root.gameObject);
            Undo.RecordObject(fixture, "Make later equipment earned and placed");
            if (fixture.Body == null)
            {
                var children = root.Cast<Transform>().ToArray();
                var body = CandidateRoot(root, "Body");
                foreach (var child in children) Undo.SetTransformParent(child, body, "Keep station wiring in its body");
                fixture.Body = body.gameObject;
            }
            fixture.Id = id; fixture.RequiresUpgrade = id; fixture.DisplayName = title;
            fixture.Category = id == Economy.UpgradeCatalog.StorageShelf ? "STORAGE" : "MACHINES";
            fixture.Footprint = footprint; fixture.Height = height;
            fixture.Clearance = .9f; fixture.ClearanceDir = workingSide; fixture.ClearanceWidth = .9f;
            fixture.AllowedRooms = new[] { Room.BackOfHouse }; fixture.SitedByDefault = false; fixture.Movable = true;
            fixture.CaptureDefault(true);
            Undo.RecordObject(fixture.Body, "Keep unpurchased equipment absent"); fixture.Body.SetActive(false);
        }

        public static void AttachAstraCandidateOpenSigns()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != AstraCandidatePath)
                throw new InvalidOperationException("Open the existing Astra validation copy in Edit Mode.");
            var starter = CandidatePath(scene, "Stations/RetailShop/StarterCounter");
            var premises = CandidatePath(scene, "Environment/Premises").GetComponent<PremisesExpansion>();
            var retail = CandidatePath(scene, "Stations/RetailShop").GetComponent<RetailShop>();
            if (retail.LabelFont == null || retail.LabelMaterial == null) throw new InvalidOperationException("Retail label font/material missing.");
            if (starter.Find("OpenClosedSign") != null) throw new InvalidOperationException("Opening signs already attached; inspect instead of repeating.");
            var board = StudyMaterial("Astra_OpenSign_Blue", new Color(.15f, .28f, .34f));
            Attach(starter, new Vector3(-4.86f, 1.5f, -2.685f), .27f);
            Attach(premises.ShopFrontRoot.transform, new Vector3(6.46f, 1.5f, -2.685f), .40f);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AstraCandidatePath)) throw new InvalidOperationException("Could not save the opening signs.");

            void Attach(Transform parent, Vector3 position, float width)
            {
                var root = CandidateRoot(parent, "OpenClosedSign");
                root.SetPositionAndRotation(position, Quaternion.identity);
                var frame = StudyBox(root, "Board", Vector3.zero, new Vector3(width, .19f, .025f), board);
                var outsideFrame = StudyBox(root, "StreetBoard", new Vector3(0f, 0f, -.17f), new Vector3(width, .19f, .025f), board);
                var sign = Undo.AddComponent<OpenClosedSign>(root.gameObject);
                sign.Label = WorldLabel.Create(root, retail.LabelFont, retail.LabelMaterial, .035f, new Color(.94f, .89f, .76f), "Inside");
                Undo.RegisterCreatedObjectUndo(sign.Label.gameObject, "Create opening state label");
                sign.Label.transform.localPosition = new Vector3(0f, 0f, .014f);
                sign.Label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                sign.OutsideLabel = WorldLabel.Create(root, retail.LabelFont, retail.LabelMaterial, .035f, new Color(.94f, .89f, .76f), "Outside");
                Undo.RegisterCreatedObjectUndo(sign.OutsideLabel.gameObject, "Create street opening label");
                sign.OutsideLabel.transform.localPosition = new Vector3(0f, 0f, -.184f);
                sign.Label.Text = sign.OutsideLabel.Text = "CLOSED";
                sign.SetHighlightRenderers(new[] { frame.GetComponent<Renderer>(), outsideFrame.GetComponent<Renderer>() });
            }
        }
        /// <summary>Finish the remaining real fixtures without replacing their slots, stock references or imported meshes.</summary>
        public static void ConfigureAstraCandidateLateFixtures()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != AstraCandidatePath)
                throw new InvalidOperationException("Open the existing Astra validation copy in Edit Mode.");
            var stations = CandidatePath(scene, "Stations");
            if (stations.Find("MaterialCollectionRack") != null)
                throw new InvalidOperationException("Late fixture integration already started; inspect and resume it, do not repeat.");
            var archive = CandidatePath(scene, "Environment/LegacyGeometry_PENDING_REAUTHOR");
            var shell = CandidatePath(scene, "Environment/AstraArchitecture");
            var premises = CandidatePath(scene, "Environment/Premises").GetComponent<PremisesExpansion>();
            var expansion = CandidatePath(scene, "Stations/WorkshopExpansion").GetComponent<WorkshopExpansion>();
            var retail = CandidatePath(scene, "Stations/RetailShop").GetComponent<RetailShop>();
            var rack = CandidatePath(scene, "Stations/WorkshopExpansion/Stage2/RockRack");
            var trophy = CandidatePath(scene, "Stations/WorkshopExpansion/Stage2/TrophyWall");
            var shelf = CandidatePath(scene, "Stations/WorkshopExpansion/Stage2/ShopShelf");
            var gallery = CandidatePath(scene, "Stations/WorkshopExpansion/Stage3/Gallery");
            var case0 = CandidatePath(scene, "Environment/Premises/ShopFront/shop_case");
            var case3 = CandidatePath(scene, "Stations/WorkshopExpansion/Stage3/shop_case");
            var uv = CandidatePath(scene, "Stations/WorkshopExpansion/Stage3/uv_lamp");
            var uvLight = CandidatePath(scene, "Stations/WorkshopExpansion/Stage3/UvLight");
            var inspectionBody = CandidatePath(scene, "Stations/AppraisalStation/Body");
            var mainCounter = retail.ShowroomCounterItem.GetComponentInParent<Checkout.CheckoutStation>(true);
            var laptops = premises.BackRoomRoot.GetComponentsInChildren<OrderTablet>(true);
            if (mainCounter == null || laptops.Length != 1 || retail.LabelFont == null || retail.LabelMaterial == null)
                throw new InvalidOperationException("Expected one back-room management laptop and the wired showroom counter/font.");
            var beforeIds = DeliveryFileIds(scene);
            var beforeZones = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Interaction.PlacementZone>(true))
                .Select(z => GlobalObjectId.GetGlobalObjectIdSlow(z).targetObjectId).OrderBy(id => id).ToArray();

            // A real management surface remains available when the starter counter is superseded.
            Undo.SetTransformParent(laptops[0].transform, mainCounter.transform, "Move existing management laptop to mature checkout");
            Undo.RecordObject(laptops[0].transform, "Seat mature management laptop");
            laptops[0].transform.localPosition = new Vector3(.95f, .95f, .12f);
            laptops[0].transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // One acquired cabinet combines the nine rough slots below and eight collection slots above.
            var combined = CandidateRoot(stations, "MaterialCollectionRack");
            var combinedBody = CandidateRoot(combined, "Body");
            Undo.SetTransformParent(rack, combinedBody, "Keep material rack slots in the combined cabinet");
            Undo.SetTransformParent(trophy, combinedBody, "Keep collection slots above material storage");
            Undo.RecordObjects(new UnityEngine.Object[] { rack, trophy }, "Arrange cabinet shelves");
            rack.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            trophy.SetLocalPositionAndRotation(new Vector3(0f, .20f, 0f), Quaternion.identity);
            var rackFixture = CandidateFixture(combined, combinedBody.gameObject, "rock_rack", "Material & Collection Cabinet",
                Economy.UpgradeCatalog.Stage2, new Vector2(1.8f, .66f), 2.70f, Room.BackOfHouse);
            rackFixture.Category = "STORAGE"; rackFixture.BodyOffset = new Vector2(0f, -.08f); rackFixture.Slots = 17;
            rackFixture.ClearanceWidth = 1.2f;
            CandidatePose(combined, new Vector2(-6.15f, 4.55f), 270f);

            // The Stage-2 four-slot shelf is an attachment above the first case, clear of the floor route.
            var mainCase = CandidateWrappedFixture(stations, case0, "MainDisplayCase", "main_display_case", "Showroom Display Case",
                Economy.UpgradeCatalog.ShopFront, new Vector2(1.85f, .54f), 2.4f, Room.Showroom);
            Undo.SetTransformParent(shelf, case0, "Attach earned shelf above the display case");
            Undo.RecordObject(shelf, "Seat additional sales shelf");
            shelf.SetLocalPositionAndRotation(new Vector3(0f, .75f, 0f), Quaternion.identity);
            Undo.RecordObject(premises, "Keep earned attachments gated");
            premises.Gates.Add(new PremisesExpansion.Gate { Upgrade = Economy.UpgradeCatalog.Stage2, Root = shelf.gameObject });
            mainCase.Slots = 6;
            CandidatePose(mainCase.transform, new Vector2(1.82f, 4.50f), 270f);

            var galleryFixture = CandidateWrappedFixture(stations, gallery, "GalleryPlinths", "gallery_plinths", "Collection Gallery",
                Economy.UpgradeCatalog.Stage3, new Vector2(2.46f, .46f), 1.0f, Room.Showroom,
                new Vector3(4.95f, 0f, 5.72f));
            galleryFixture.Slots = 3; galleryFixture.ClearanceWidth = 1.2f;
            CandidatePose(galleryFixture.transform, new Vector2(5.30f, 5.68f), 0f);
            var specialistCase = CandidateWrappedFixture(stations, case3, "SpecialistDisplayCase", "specialist_display_case", "Specialist Display Case",
                Economy.UpgradeCatalog.Stage3, new Vector2(1.85f, .54f), 1.6f, Room.Showroom);
            specialistCase.Slots = 6;
            CandidatePose(specialistCase.transform, new Vector2(6.7f, -.90f), 90f);

            // Preserve the existing UV component reference, now physically attached to its moving station.
            var uvGroup = CandidateRoot(inspectionBody, "SpecialistVerification");
            Undo.SetTransformParent(uv, uvGroup, "Keep UV lamp with its station");
            Undo.SetTransformParent(uvLight, uvGroup, "Keep verification light with its station");
            Undo.RecordObjects(new UnityEngine.Object[] { uv, uvLight }, "Seat verification hardware");
            uv.SetLocalPositionAndRotation(new Vector3(-.29f, .9f, .12f), Quaternion.Euler(0f, 220f, 0f));
            uvLight.localPosition = new Vector3(-.2f, 1.22f, .02f);
            premises.Gates.Add(new PremisesExpansion.Gate { Upgrade = Economy.UpgradeCatalog.Stage3, Root = uvGroup.gameObject });

            var wallC = CandidatePath(scene, "Stations/DisplayWall_display_wall_c").GetComponent<PlaceableFixture>();
            Undo.RecordObject(wallC, "Make the third wall run an earned placement");
            wallC.RequiresUpgrade = Economy.UpgradeCatalog.Stage2; wallC.SitedByDefault = false;
            foreach (string name in new[] { "SawStation", "PolishStation", "CrackerStation" })
            {
                var f = CandidatePath(scene, "Stations/" + name).GetComponent<PlaceableFixture>();
                Undo.RecordObject(f, "Allow machines in the processing room"); f.AllowedRooms = new[] { Room.BackOfHouse };
            }
            var cabinet = CandidatePath(scene, "Stations/DisplayCabinet").GetComponent<PlaceableFixture>();
            CandidatePose(cabinet.transform, new Vector2(.5f, -2.34f), 180f);

            // All remaining old room dressing has old coordinates. Keep it for the Blender comparison, inactive.
            foreach (Transform child in premises.BackRoomRoot.transform.Cast<Transform>().ToArray())
                Undo.SetTransformParent(child, archive, "Archive superseded back-room dressing");
            var keepShop = new HashSet<Transform>(retail.ShowroomQueue) { mainCounter.transform, retail.ShowroomCounterCustomer,
                retail.ShowroomDoor, retail.ShowroomOutside, premises.ShopFrontRoot.transform.Find("ShowroomEntrance"),
                premises.ShopFrontRoot.transform.Find("OpenClosedSign") };
            foreach (Transform child in premises.ShopFrontRoot.transform.Cast<Transform>().ToArray())
                if (!keepShop.Contains(child)) Undo.SetTransformParent(child, archive, "Archive superseded showroom dressing");
            foreach (var stage in new[] { expansion.Stage2Root, expansion.Stage3Root })
                foreach (Transform child in stage.transform.Cast<Transform>().ToArray())
                    Undo.SetTransformParent(child, archive, "Archive old stage dressing after preserving its gameplay");
            Undo.RecordObject(expansion, "Owned shelves remain until the player moves them"); expansion.HideAtStage2.Clear();
            Undo.SetTransformParent(CandidatePath(scene, "Stations/RetailShop/FitOut"), archive, "Archive the old fit-out plants and signs");
            premises.Gates.RemoveAll(g => g == null || g.Root == null || g.Root.transform.IsChildOf(archive));
            var blue = StudyMaterial("Astra_Fitout_Blue", new Color(.15f, .28f, .34f));
            StudyBox(premises.BackRoomRoot.transform, "ReceivingShutter", new Vector3(-4f, 1.24f, 6.015f), new Vector3(2.78f, 2.48f, .045f), blue);
            var fitout = CandidateRoot(premises.ShopFrontRoot.transform, "EarnedShopSign");
            CandidateWallLabel(fitout, "GEODE EMPIRE", new Vector3(4.5f, 2.55f, 5.89f), 0f, 2.4f, retail, blue);
            premises.Gates.Add(new PremisesExpansion.Gate { Upgrade = Economy.UpgradeCatalog.ShopSignage, Root = fitout.gameObject });
            CandidateWallLabel(shell, "GEODE EMPIRE", new Vector3(-2.72f, 2.58f, -2.865f), 0f, 3.2f, retail, blue);

            var receiving = CandidatePath(scene, "Stations/ReceivingArea").GetComponent<ReceivingArea>();
            Undo.RecordObject(receiving, "Leave a usable approach to the material cabinet");
            receiving.BayCells = new[] { new Vector3(-4.15f, .12f, 4.47f), new Vector3(-2.80f, .12f, 4.47f),
                new Vector3(-4.15f, .12f, 5.47f), new Vector3(-2.80f, .12f, 5.47f) };
            var marks = shell.Cast<Transform>().Where(t => t.name == "ReceivingMark").ToArray();
            var cells = receiving.StarterCells.Concat(receiving.BayCells).ToArray();
            if (marks.Length != cells.Length) throw new InvalidOperationException("Receiving mark count changed during integration.");
            for (int i = 0; i < marks.Length; i++)
            { Undo.RecordObject(marks[i], "Move receiving mark"); marks[i].position = new Vector3(cells[i].x, .005f, cells[i].z); }
            foreach (var f in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlaceableFixture>(true)))
            {
                f.CaptureDefault(true);
                if (f.Body != null) { Undo.RecordObject(f.Body, "Keep earned fixture absent until owned and placed"); f.Body.SetActive(false); }
            }
            foreach (var g in premises.Gates) { Undo.RecordObject(g.Root, "Keep earned fit-out absent"); g.Root.SetActive(false); }
            Undo.RecordObjects(new UnityEngine.Object[] { expansion.Stage2Root, expansion.Stage3Root }, "Keep fresh-career stage roots off");
            expansion.Stage2Root.SetActive(false); expansion.Stage3Root.SetActive(false);
            Physics.SyncTransforms();
            var afterZones = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Interaction.PlacementZone>(true))
                .Select(z => GlobalObjectId.GetGlobalObjectIdSlow(z).targetObjectId).OrderBy(id => id).ToArray();
            if (!beforeIds.SequenceEqual(DeliveryFileIds(scene)) || !beforeZones.SequenceEqual(afterZones))
                throw new InvalidOperationException("A gameplay component ID changed; do not promote this candidate.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AstraCandidatePath)) throw new InvalidOperationException("Could not save late fixtures.");
        }

        /// <summary>One stable purchased body owns every machine variant, working zone and shared output surface.</summary>
        [MenuItem("GeodeEmpire/Astra/Configure Candidate Machine Bodies")]
        public static void ConfigureAstraCandidateMachineBodies()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != AstraCandidatePath || scene.isDirty)
                throw new InvalidOperationException("Open the clean saved Astra candidate in Edit Mode first.");
            var roots = new[] { "SawStation", "PolishStation", "CrackerStation" }
                .Select(name => CandidatePath(scene, "Stations/" + name)).ToArray();
            foreach (var root in roots)
                if (root.Find("Body") != null || root.GetComponent<PlaceableFixture>() == null)
                    throw new InvalidOperationException("Machine body is already configured or fixture missing: " + root.name);
            var shared = new[] { "tray", "SawLight", "SawTray", "MatSaw", "Sign_DIAMOND SAW" }
                .Select(name => CandidatePath(scene, "Stations/SawStation/Machine/" + name)).ToArray();
            var beforeIds = DeliveryFileIds(scene);
            var zones = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Interaction.PlacementZone>(true)).ToArray();
            var beforeZones = zones.Select(z => GlobalObjectId.GetGlobalObjectIdSlow(z).targetObjectId).OrderBy(x => x).ToArray();
            foreach (var root in roots)
            {
                var children = root.Cast<Transform>().ToArray();
                var body = CandidateRoot(root, "Body");
                foreach (var child in children) Undo.SetTransformParent(child, body, "Keep machine hardware with its purchased body");
                var fixture = root.GetComponent<PlaceableFixture>();
                Undo.RecordObject(fixture, "Use stable machine body across variants");
                fixture.Body = body.gameObject;
                if (root == roots[0])
                    foreach (var child in shared) Undo.SetTransformParent(child, body, "Share saw output and lighting between variants");
                body.gameObject.SetActive(false);
            }
            var afterZones = zones.Select(z => GlobalObjectId.GetGlobalObjectIdSlow(z).targetObjectId).OrderBy(x => x).ToArray();
            if (!beforeIds.SequenceEqual(DeliveryFileIds(scene)) || !beforeZones.SequenceEqual(afterZones))
                throw new InvalidOperationException("A gameplay component ID changed; do not promote this candidate.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AstraCandidatePath)) throw new InvalidOperationException("Could not save machine bodies.");
        }

        [MenuItem("GeodeEmpire/Astra/Configure Candidate Practical Lighting")]
        public static void ConfigureAstraCandidatePracticalLighting()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != AstraCandidatePath || scene.isDirty)
                throw new InvalidOperationException("Open the clean saved Astra candidate in Edit Mode first.");
            var shell = CandidatePath(scene, "Environment/AstraArchitecture");
            if (shell.Find("PracticalLighting") != null) throw new InvalidOperationException("Practical lighting already exists; inspect it instead of rebuilding.");
            var premises = CandidatePath(scene, "Environment/Premises").GetComponent<PremisesExpansion>();
            if (premises == null) throw new InvalidOperationException("Premises component missing.");
            var gallerySpots = CandidatePath(scene, "Stations/GalleryPlinths").GetComponentsInChildren<Light>(true)
                .Where(l => l.type == LightType.Spot).OrderBy(l => l.transform.position.x).ToArray();
            if (gallerySpots.Length != 3) throw new InvalidOperationException("Expected the three existing gallery spots.");
            var lighting = CandidateRoot(shell, "PracticalLighting");
            var housing = StudyMaterial("Astra_CeilingHousing_Layout", new Color(.69f, .72f, .73f));
            var diffuser = StudyMaterial("Astra_CeilingDiffuser_Layout", new Color(.90f, .92f, .94f));
            diffuser.EnableKeyword("_EMISSION"); diffuser.SetColor("_EmissionColor", new Color(1.1f, 1.14f, 1.18f));
            var starter = CandidateRoot(lighting, "Starter");
            var processing = CandidateRoot(lighting, "Processing");
            var office = CandidateRoot(lighting, "Office");
            var showroom = CandidateRoot(lighting, "Showroom");
            CandidateBatten(starter, "CounterLight", new Vector2(-4.55f, -.55f), 3.2f, 4.8f, true, housing, diffuser);
            CandidateBatten(starter, "WorkLight", new Vector2(-1.85f, -.55f), 3.2f, 4.8f, false, housing, diffuser);
            CandidateBatten(processing, "WetWorkLight", new Vector2(-4.8f, 2.95f), 3.6f, 4.8f, true, housing, diffuser);
            CandidateBatten(processing, "MachineLight", new Vector2(-.8f, 3.55f), 3.6f, 4.8f, false, housing, diffuser);
            CandidateBatten(office, "OfficeLight", new Vector2(.5f, -.65f), 2.2f, 3.2f, false, housing, diffuser);
            CandidateBatten(showroom, "RetailLight", new Vector2(3.4f, .15f), 4.2f, 5.6f, true, housing, diffuser);
            CandidateBatten(showroom, "GalleryLight", new Vector2(4.7f, 4.15f), 4.2f, 5.6f, false, housing, diffuser);
            Undo.RecordObject(premises, "Light only leased rooms");
            premises.Gates.Add(new PremisesExpansion.Gate { Upgrade = Economy.UpgradeCatalog.BackRoom, Root = processing.gameObject });
            premises.Gates.Add(new PremisesExpansion.Gate { Upgrade = Economy.UpgradeCatalog.BackRoom, Root = office.gameObject });
            premises.Gates.Add(new PremisesExpansion.Gate { Upgrade = Economy.UpgradeCatalog.ShopFront, Root = showroom.gameObject });
            processing.gameObject.SetActive(false); office.gameObject.SetActive(false); showroom.gameObject.SetActive(false);
            for (int i = 0; i < gallerySpots.Length; i++)
            {
                Undo.RecordObject(gallerySpots[i], "Budget gallery shadows alongside room lighting");
                gallerySpots[i].shadows = i == 1 ? LightShadows.Soft : LightShadows.None;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AstraCandidatePath)) throw new InvalidOperationException("Could not save practical lighting.");
        }

        private static void CandidateBatten(Transform parent, string name, Vector2 point, float intensity, float range,
            bool shadows, Material housing, Material diffuser)
        {
            var root = CandidateRoot(parent, name);
            root.position = new Vector3(point.x, 2.87f, point.y);
            StudyBox(root, "Housing", Vector3.zero, new Vector3(1.08f, .06f, .14f), housing, false);
            StudyBox(root, "Diffuser", new Vector3(0f, -.036f, 0f), new Vector3(.96f, .012f, .10f), diffuser, false);
            var source = CandidateRoot(root, "Light"); source.localPosition = new Vector3(0f, -.05f, 0f);
            source.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var light = Undo.AddComponent<Light>(source.gameObject);
            light.type = LightType.Spot; light.spotAngle = 125f; light.innerSpotAngle = 85f;
            light.color = new Color(.97f, .985f, 1f); light.intensity = intensity; light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowBias = .025f; light.shadowNormalBias = .18f;
        }

        private static PlaceableFixture CandidateWrappedFixture(Transform stations, Transform body, string name, string id,
            string title, string upgrade, Vector2 size, float height, Room room, Vector3? centre = null)
        {
            var root = CandidateRoot(stations, name);
            root.SetPositionAndRotation(centre ?? body.position, body.rotation);
            Undo.SetTransformParent(body, root, "Preserve original body and slot references");
            return CandidateFixture(root, body.gameObject, id, title, upgrade, size, height, room);
        }

        private static PlaceableFixture CandidateFixture(Transform root, GameObject body, string id, string title,
            string upgrade, Vector2 size, float height, Room room)
        {
            var f = Undo.AddComponent<PlaceableFixture>(root.gameObject);
            f.Id = id; f.DisplayName = title; f.RequiresUpgrade = upgrade; f.Body = body;
            f.Footprint = size; f.Height = height; f.Clearance = .9f; f.ClearanceDir = new Vector2(0f, -1f);
            f.ClearanceWidth = .9f; f.Category = "DISPLAYS"; f.AllowedRooms = new[] { room };
            f.SitedByDefault = false; f.Movable = true;
            return f;
        }

        private static void CandidateWallLabel(Transform parent, string text, Vector3 position, float yaw, float width,
            RetailShop retail, Material material)
        {
            var root = CandidateRoot(parent, "BusinessSign"); root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            StudyBox(root, "Board", Vector3.zero, new Vector3(width, .30f, .025f), material);
            var label = WorldLabel.Create(root, retail.LabelFont, retail.LabelMaterial, .09f, new Color(.94f, .89f, .76f));
            Undo.RegisterCreatedObjectUndo(label.gameObject, "Create business label");
            label.transform.localPosition = new Vector3(0f, 0f, -.014f); label.Text = text;
        }

        private static ulong[] DeliveryFileIds(Scene scene)
        {
            var parent = CandidatePath(scene, "Stations/FixtureDelivery");
            return parent.GetComponentsInChildren<DeliveryCrate>(true).OrderBy(d => d.transform.GetSiblingIndex()).Select(d =>
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(d).targetObjectId;
                if (id == 0)
                    throw new InvalidOperationException("Delivery local file ID missing.");
                return id;
            }).ToArray();
        }
        private static Transform CandidateRoot(Transform parent, string name)
        {
            var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "Create Astra architecture");
            go.transform.SetParent(parent, false); return go.transform;
        }
        private static void CandidatePose(Transform target, Vector2 point, float yaw)
        {
            Undo.RecordObject(target, "Place measured fixture");
            target.SetPositionAndRotation(new Vector3(point.x, 0f, point.y), Quaternion.Euler(0f, yaw, 0f));
        }
        private static void CandidatePoint(Transform target, Vector3 point)
        {
            if (target == null) throw new InvalidOperationException("A required route/anchor reference is null.");
            Undo.RecordObject(target, "Set measured route anchor"); target.position = point;
        }
        private static void CandidateDoor(Transform parent, string name, float x, Material frame)
        {
            var root = CandidateRoot(parent, name);
            root.position = new Vector3(x, 0f, -2.77f);
            var hinge = CandidateRoot(root, "Hinge"); hinge.localPosition = new Vector3(-.555f, 0f, 0f);
            StudyBox(hinge, "DoorLeaf", new Vector3(.555f, 1.1f, 0f), new Vector3(1.11f, 2.18f, .04f), frame);
            var controller = Undo.AddComponent<ShopEntranceDoor>(root.gameObject); controller.Leaf = hinge;
        }
    }
}
