using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Công cụ dựng tự động dữ liệu vật phẩm và 2 scene Menu, Home cho Phase 1 & Phase 2
public static class Phase1SceneBuilder
{
    const string MenuScenePath = "Assets/Scenes/Menu.unity";
    const string HomeScenePath = "Assets/Scenes/Home.unity";
    const string PanelSettingsPath = "Assets/UI Toolkit/Common/PanelSettings.asset";
    const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
    const string ItemsFolder = "Assets/Resources/Items";
    const string CatalogPath = "Assets/Resources/ShopCatalog.asset";

    [MenuItem("Tools/Phase 1/Build Menu And Home Scenes")]
    public static void BuildAllScenes()
    {
        ShopCatalog catalog = BuildItemCatalog();
        BuildMenuScene();
        BuildHomeScene(catalog);
        RegisterBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ItemData.ClearCache();
        Debug.Log("[SceneBuilder] Đã dựng xong dữ liệu vật phẩm, Menu.unity và Home.unity");
    }

    // Tạo bộ dữ liệu ScriptableObject vật phẩm trong Resources/Items và ShopCatalog
    public static ShopCatalog BuildItemCatalog()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(ItemsFolder);

        List<ItemData> all = new List<ItemData>
        {
            CreatePcPart("mb_basic", "Mainboard B450", PcPartType.Motherboard, 45f, 8f, 0, 2f, "Assets/Prefabs/Prototype/PcParts/Proto_Motherboard.prefab"),
            CreatePcPart("cpu_i3", "CPU Core i3", PcPartType.Cpu, 60f, 10f, 0, 2f, "Assets/Prefabs/Prototype/PcParts/Proto_Cpu.prefab"),
            CreatePcPart("cpu_i7", "CPU Core i7", PcPartType.Cpu, 160f, 24f, 0, 4f, "Assets/Prefabs/Prototype/PcParts/Proto_Cpu.prefab"),
            CreatePcPart("cooler_air", "Tản nhiệt khí", PcPartType.CpuCooler, 25f, 6f, 0, 2f, "Assets/Prefabs/Prototype/PcParts/Proto_CpuCooler.prefab"),
            CreatePcPart("ram_8gb", "RAM 8GB DDR4", PcPartType.Ram, 35f, 6f, 8, 2f, "Assets/Prefabs/Prototype/PcParts/Proto_Ram.prefab"),
            CreatePcPart("ram_16gb", "RAM 16GB RGB", PcPartType.Ram, 75f, 14f, 16, 3.5f, "Assets/Prefabs/Prototype/PcParts/Proto_Ram.prefab"),
            CreatePcPart("gpu_1050", "Card GTX 1050", PcPartType.GraphicsCard, 90f, 12f, 4, 2f, "Assets/Prefabs/Prototype/PcParts/Proto_GraphicsCard.prefab"),
            CreatePcPart("gpu_4080", "Card RTX 4080", PcPartType.GraphicsCard, 220f, 35f, 16, 5f, "Assets/Prefabs/Prototype/PcParts/Proto_GraphicsCard.prefab"),
            CreatePcPart("psu_550w", "Nguồn 550W", PcPartType.Psu, 40f, 6f, 0, 2f, "Assets/Prefabs/Prototype/PcParts/Proto_Psu.prefab"),
            CreatePcPart("hdd_1tb", "Ổ cứng 1TB", PcPartType.Storage, 35f, 6f, 1000, 2f, "Assets/Prefabs/Prototype/PcParts/Proto_Hdd.prefab"),
            CreateRouter("router_basic", "Router Wifi Chuẩn", 45f, 20f, 100f, 2.5f, "Assets/Prefabs/Prototype/PcParts/Proto_Router.prefab"),
            CreateRouter("router_pro", "Router Gaming 5G", 120f, 35f, 300f, 4.5f, "Assets/Prefabs/Prototype/PcParts/Proto_Router.prefab"),
            CreateFood("food_pizza", "Pizza nóng", 12f, 5, -5f, 3f, "Assets/Prefabs/Prototype/Food/Proto_PizzaBox.prefab"),
            CreateFood("food_burger", "Burger bò", 8f, 4, -3f, 2.5f, "Assets/Prefabs/Prototype/Food/Proto_BurgerBox.prefab"),
            CreateFood("food_soda", "Nước ngọt", 3f, 2, -4f, 1.5f, "Assets/Prefabs/Prototype/Food/Proto_Can.prefab"),
            CreateMedicine("med_pills", "Thuốc cảm cúm", 18f, 3f, "Assets/Prefabs/Prototype/Items/Proto_Medicine.prefab")
        };

        ShopCatalog catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        catalog.items = all;
        EditorUtility.SetDirty(catalog);
        ItemData.ClearCache();
        return catalog;
    }

    static PcPartData CreatePcPart(string id, string name, PcPartType type, float price, float quality, int gb, float stars, string prefabPath)
    {
        string path = $"{ItemsFolder}/{id}.asset";
        PcPartData asset = AssetDatabase.LoadAssetAtPath<PcPartData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<PcPartData>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.id = id;
        asset.displayName = name;
        asset.category = ItemCategory.PcPart;
        asset.partType = type;
        asset.price = price;
        asset.quality = quality;
        asset.capacityGb = gb;
        asset.stars = stars;
        asset.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static RouterData CreateRouter(string id, string name, float price, float upMbps, float downMbps, float stars, string prefabPath)
    {
        string path = $"{ItemsFolder}/{id}.asset";
        RouterData asset = AssetDatabase.LoadAssetAtPath<RouterData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<RouterData>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.id = id;
        asset.displayName = name;
        asset.category = ItemCategory.Network;
        asset.price = price;
        asset.uploadMbps = upMbps;
        asset.downloadMbps = downMbps;
        asset.stars = stars;
        asset.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static FoodData CreateFood(string id, string name, float price, int hungerRelief, float stressDelta, float stars, string prefabPath)
    {
        string path = $"{ItemsFolder}/{id}.asset";
        FoodData asset = AssetDatabase.LoadAssetAtPath<FoodData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<FoodData>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.id = id;
        asset.displayName = name;
        asset.category = ItemCategory.Food;
        asset.price = price;
        asset.hungerRelief = hungerRelief;
        asset.stressDelta = stressDelta;
        asset.stars = stars;
        asset.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static MedicineData CreateMedicine(string id, string name, float price, float stars, string prefabPath)
    {
        string path = $"{ItemsFolder}/{id}.asset";
        MedicineData asset = AssetDatabase.LoadAssetAtPath<MedicineData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<MedicineData>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.id = id;
        asset.displayName = name;
        asset.category = ItemCategory.Medicine;
        asset.price = price;
        asset.stars = stars;
        asset.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    // Dựng scene Main Menu
    public static void BuildMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        camObj.AddComponent<AudioListener>();

        GameObject lightObj = new GameObject("Directional Light");
        Light dirLight = lightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject worldObj = new GameObject("GameWorld");
        worldObj.AddComponent<GameWorld>();
        worldObj.AddComponent<TutorialSystem>();

        GameObject uiRoot = new GameObject("UI");

        SettingsMenu settingsMenu = CreateUIDoc<SettingsMenu>(
            "SettingsMenu",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Settings/Settings.uxml",
            10);
        SetPrivateBool(settingsMenu, "freezePlayer", false);

        MainMenu mainMenu = CreateUIDoc<MainMenu>(
            "MainMenu",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Menu/MainMenu.uxml",
            0);
        SetPrivateRef(mainMenu, "settingsMenu", settingsMenu);

        CreateUIDoc<LoadingView>(
            "LoadingView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Loading/Loading.uxml",
            100);

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // Dựng scene phòng streamer (Home)
    public static void BuildHomeScene(ShopCatalog catalog = null)
    {
        if (catalog == null)
            catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath) ?? BuildItemCatalog();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

        GameObject dirLightObj = new GameObject("Directional Light");
        Light dirLight = dirLightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.intensity = 1.1f;
        dirLightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject roomLightObj = new GameObject("Room Light");
        roomLightObj.transform.position = new Vector3(0f, 2.65f, 0f);
        Light roomLight = roomLightObj.AddComponent<Light>();
        roomLight.type = LightType.Point;
        roomLight.range = 10f;
        roomLight.intensity = 1.5f;
        roomLight.color = new Color(1f, 0.97f, 0.92f);

        GameObject worldObj = new GameObject("GameWorld");
        worldObj.AddComponent<GameWorld>();
        worldObj.AddComponent<TutorialSystem>();
        worldObj.AddComponent<DialogueSystem>();
        worldObj.AddComponent<AppearanceSystem>();
        InputRebinding rebinding = worldObj.AddComponent<InputRebinding>();
        SetPrivateRef(rebinding, "actions", inputActions);

        GameObject envRoot = new GameObject("Environment");
        GameObject roomObj = InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_Room.prefab", envRoot.transform, Vector3.zero, Vector3.zero);
        roomObj.AddComponent<WallPainter>();
        GameObject doorObj = InstantiatePrefab("Assets/Prefabs/Prototype/Furniture/Proto_Door.prefab", envRoot.transform, new Vector3(0f, 0.1f, 3f), Vector3.zero);
        doorObj.AddComponent<Door>();
        MapMarker homeMarker = doorObj.AddComponent<MapMarker>();
        homeMarker.SetLabel("Nhà Streamer");

        GameObject deskObj = InstantiatePrefab(
            "Assets/Prefabs/Prototype/StreamSetup/Proto_ComputerDesk.prefab",
            envRoot.transform,
            new Vector3(-1.5f, 0.1f, -2.35f),
            Vector3.zero);
        ComputerDesk desk = deskObj.AddComponent<ComputerDesk>();

        GameObject seatPointObj = new GameObject("SeatPoint");
        seatPointObj.transform.SetParent(deskObj.transform, false);
        seatPointObj.transform.localPosition = new Vector3(0f, 1.185f, 0.34f);
        seatPointObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        SetPrivateRef(desk, "seatPoint", seatPointObj.transform);

        GameObject monitorObj = InstantiatePrefab("Assets/Prefabs/Prototype/StreamSetup/Proto_Monitor.prefab", deskObj.transform, new Vector3(0f, 0.76f, -0.15f), Vector3.zero);
        Transform meshScreen = monitorObj.transform.Find("Mesh_Screen");
        if (meshScreen != null)
            SetPrivateRef(desk, "monitorScreen", meshScreen);
        InstantiatePrefab("Assets/Prefabs/Prototype/StreamSetup/Proto_Keyboard.prefab", deskObj.transform, new Vector3(-0.08f, 0.76f, 0.15f), Vector3.zero);
        InstantiatePrefab("Assets/Prefabs/Prototype/StreamSetup/Proto_Mouse.prefab", deskObj.transform, new Vector3(0.25f, 0.76f, 0.15f), Vector3.zero);

        GameObject webcamObj = InstantiatePrefab("Assets/Prefabs/Prototype/StreamSetup/Proto_Webcam.prefab", monitorObj.transform, new Vector3(0f, 0.61f, 0.01f), Vector3.zero);
        StreamGear webcamGear = webcamObj.AddComponent<StreamGear>();
        SetPrivateEnum(webcamGear, "gearType", (int)StreamGearType.Webcam);
        SetPrivateFloat(webcamGear, "qualityBonus", 6f);

        GameObject micObj = InstantiatePrefab("Assets/Prefabs/Prototype/StreamSetup/Proto_Microphone.prefab", deskObj.transform, new Vector3(-0.42f, 0.76f, 0.08f), Vector3.zero);
        StreamGear micGear = micObj.AddComponent<StreamGear>();
        SetPrivateEnum(micGear, "gearType", (int)StreamGearType.Microphone);
        SetPrivateFloat(micGear, "qualityBonus", 6f);

        GameObject pcCaseObj = InstantiatePrefab("Assets/Prefabs/Prototype/PcParts/Proto_PcCase.prefab", deskObj.transform, new Vector3(0.88f, 0f, 0f), new Vector3(0f, 180f, 0f));
        PcCase pcCase = pcCaseObj.AddComponent<PcCase>();
        pcCase.EnsureSlots();
        pcCase.Install(AssetDatabase.LoadAssetAtPath<PcPartData>($"{ItemsFolder}/mb_basic.asset"));
        pcCase.Install(AssetDatabase.LoadAssetAtPath<PcPartData>($"{ItemsFolder}/cpu_i3.asset"));
        pcCase.Install(AssetDatabase.LoadAssetAtPath<PcPartData>($"{ItemsFolder}/cooler_air.asset"));
        pcCase.Install(AssetDatabase.LoadAssetAtPath<PcPartData>($"{ItemsFolder}/ram_8gb.asset"));
        pcCase.Install(AssetDatabase.LoadAssetAtPath<PcPartData>($"{ItemsFolder}/gpu_1050.asset"));
        pcCase.Install(AssetDatabase.LoadAssetAtPath<PcPartData>($"{ItemsFolder}/psu_550w.asset"));
        pcCase.Install(AssetDatabase.LoadAssetAtPath<PcPartData>($"{ItemsFolder}/hdd_1tb.asset"));
        EditorUtility.SetDirty(pcCase);

        GameObject routerObj = InstantiatePrefab("Assets/Prefabs/Prototype/PcParts/Proto_Router.prefab", envRoot.transform, new Vector3(-2.55f, 0.1f, -2.55f), Vector3.zero);
        Router router = routerObj.AddComponent<Router>();
        router.SetData(AssetDatabase.LoadAssetAtPath<RouterData>($"{ItemsFolder}/router_basic.asset"));

        StreamSetup streamSetup = deskObj.AddComponent<StreamSetup>();
        SetPrivateRef(streamSetup, "desk", desk);
        SetPrivateRef(streamSetup, "pcCase", pcCase);
        SetPrivateRef(streamSetup, "router", router);
        deskObj.AddComponent<JustChattingGame>();
        deskObj.AddComponent<AsmrGame>();
        deskObj.AddComponent<DanceGame>();
        deskObj.AddComponent<CamperStrike>();
        deskObj.AddComponent<PogoGame>();
        deskObj.AddComponent<BrainRotClicker>();
        deskObj.AddComponent<ScratchCard>();
        deskObj.AddComponent<SkinCase>();

        GameObject chairObj = InstantiatePrefab("Assets/Prefabs/Prototype/StreamSetup/Proto_GamingChair.prefab", deskObj.transform, new Vector3(0f, 0f, 0.65f), new Vector3(0f, 180f, 0f));

        GameObject playerMeshObj = BuildPlayerMesh(envRoot.transform, chairObj.transform);
        SetPrivateRef(desk, "chairSitPoint", chairObj.transform);
        SetPrivateRef(desk, "playerMesh", playerMeshObj);

        GameObject bedObj = InstantiatePrefab(
            "Assets/Prefabs/Prototype/Furniture/Proto_Bed.prefab",
            envRoot.transform,
            new Vector3(2.1f, 0.1f, -1.75f),
            Vector3.zero);
        Bed bed = bedObj.AddComponent<Bed>();

        GameObject fridgeObj = InstantiatePrefab(
            "Assets/Prefabs/Prototype/Furniture/Proto_Fridge.prefab",
            envRoot.transform,
            new Vector3(-2.45f, 0.1f, 0.4f),
            new Vector3(0f, 90f, 0f));
        Fridge fridge = fridgeObj.AddComponent<Fridge>();
        fridge.Store(AssetDatabase.LoadAssetAtPath<FoodData>($"{ItemsFolder}/food_pizza.asset"));
        fridge.Store(AssetDatabase.LoadAssetAtPath<FoodData>($"{ItemsFolder}/food_burger.asset"));
        EditorUtility.SetDirty(fridge);

        GameObject toiletObj = InstantiatePrefab(
            "Assets/Prefabs/Prototype/Furniture/Proto_Toilet.prefab",
            envRoot.transform,
            new Vector3(2.55f, 0.1f, 2.2f),
            new Vector3(0f, -90f, 0f));
        toiletObj.AddComponent<Toilet>();

        GameObject bathtubObj = InstantiatePrefab(
            "Assets/Prefabs/Prototype/Furniture/Proto_Bathtub.prefab",
            envRoot.transform,
            new Vector3(2.35f, 0.1f, 0.55f),
            new Vector3(0f, -90f, 0f));
        bathtubObj.AddComponent<Bathtub>();

        GameObject courierObj = new GameObject("Courier");
        courierObj.transform.SetParent(envRoot.transform, false);
        courierObj.transform.position = new Vector3(0f, 0.1f, 2.15f);
        Courier courier = courierObj.AddComponent<Courier>();
        SetPrivateRef(courier, "defaultDropPoint", courierObj.transform);

        BuildPhase4Content(envRoot.transform, worldObj);

        PlayerInteractor interactor = BuildPlayer(inputActions, out PlayerInputReader playerInput, out ItemCarrier carrier);

        GameObject uiRoot = new GameObject("UI");

        HudView hudView = CreateUIDoc<HudView>(
            "HudView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/HUD/HUD.uxml",
            0);
        SetPrivateRef(hudView, "interactor", interactor);
        SetPrivateRef(hudView, "carrier", carrier);
        hudView.gameObject.AddComponent<NeedsView>();
        hudView.gameObject.AddComponent<TutorialView>();

        StreamOverlayView streamOverlayView = CreateUIDoc<StreamOverlayView>(
            "StreamOverlayView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/StreamOverlay/StreamOverlay.uxml",
            20);
        streamOverlayView.gameObject.AddComponent<ChatView>();

        StreamStartPanel streamStartPanel = CreateUIDoc<StreamStartPanel>(
            "StreamStartPanel",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/WorldPanels/WorldPanels.uxml",
            10);

        BillPanel billPanel = CreateUIDoc<BillPanel>(
            "BillPanel",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/WorldPanels/WorldPanels.uxml",
            15);

        InventoryView inventoryView = CreateUIDoc<InventoryView>(
            "InventoryView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Inventory/Inventory.uxml",
            16);

        PcBuildView pcBuildView = CreateUIDoc<PcBuildView>(
            "PcBuildView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/WorldPanels/WorldPanels.uxml",
            17);

        DesktopOS desktopOS = CreateUIDoc<DesktopOS>(
            "DesktopOS",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Computer/Computer.uxml",
            18);
        ZamazorApp zamazorApp = desktopOS.gameObject.AddComponent<ZamazorApp>();
        SetPrivateRef(zamazorApp, "catalog", catalog);
        desktopOS.gameObject.AddComponent<YouhubeApp>();
        desktopOS.gameObject.AddComponent<DiscatApp>();
        desktopOS.gameObject.AddComponent<FakegramApp>();
        desktopOS.gameObject.AddComponent<SpotiApp>();
        desktopOS.gameObject.AddComponent<BrowserApp>();

        PhoneController phoneController = CreateUIDoc<PhoneController>(
            "PhoneController",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Phone/Phone.uxml",
            19);
        phoneController.gameObject.AddComponent<FoodDeliveryApp>();
        phoneController.gameObject.AddComponent<EatJobApp>();
        phoneController.gameObject.AddComponent<PhoneCameraApp>();
        phoneController.gameObject.AddComponent<PhoneAlbumApp>();
        phoneController.gameObject.AddComponent<PhonePetApp>();
        phoneController.gameObject.AddComponent<PhoneCarApp>();
        phoneController.gameObject.AddComponent<PhoneClothApp>();
        phoneController.gameObject.AddComponent<PhoneSkillApp>();
        phoneController.gameObject.AddComponent<PhoneMapApp>();

        StreamCardView streamCardView = CreateUIDoc<StreamCardView>(
            "StreamCardView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/WorldPanels/WorldPanels.uxml",
            19);

        SkillTreeView skillTreeView = CreateUIDoc<SkillTreeView>(
            "SkillTreeView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/WorldPanels/WorldPanels.uxml",
            19);

        CreateUIDoc<DialogueView>(
            "DialogueView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/WorldPanels/WorldPanels.uxml",
            22);

        SettingsMenu settingsMenu = CreateUIDoc<SettingsMenu>(
            "SettingsMenu",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Settings/Settings.uxml",
            25);

        PauseMenu pauseMenu = CreateUIDoc<PauseMenu>(
            "PauseMenu",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Pause/Pause.uxml",
            20);
        SetPrivateRef(pauseMenu, "settingsMenu", settingsMenu);

        ScreenFader screenFader = CreateUIDoc<ScreenFader>(
            "ScreenFader",
            uiRoot.transform,
            panelSettings,
            null,
            90);

        CreateUIDoc<LoadingView>(
            "LoadingView",
            uiRoot.transform,
            panelSettings,
            "Assets/UI Toolkit/Screens/Loading/Loading.uxml",
            100);

        GameObject uiManagerObj = new GameObject("UIManager");
        uiManagerObj.transform.SetParent(uiRoot.transform, false);
        UIManager uiManager = uiManagerObj.AddComponent<UIManager>();
        SetPrivateRef(uiManager, "playerInput", playerInput);
        SetPrivateRef(uiManager, "pauseMenu", pauseMenu);
        SetPrivateRef(uiManager, "billPanel", billPanel);
        SetPrivateRef(uiManager, "inventoryView", inventoryView);
        SetPrivateRef(uiManager, "desktopOS", desktopOS);
        SetPrivateRef(uiManager, "skillTreeView", skillTreeView);
        SetPrivateRef(uiManager, "phoneController", phoneController);

        SetPrivateRef(desk, "streamStartPanel", streamStartPanel);
        SetPrivateRef(bed, "fader", screenFader);

        EditorSceneManager.SaveScene(scene, HomeScenePath);
    }

    // Dựng nhân vật góc nhìn thứ nhất (capsule độc lập không chứa mesh cơ thể, giống SLS2)
    static PlayerInteractor BuildPlayer(InputActionAsset inputActions, out PlayerInputReader inputReader, out ItemCarrier carrier)
    {
        GameObject playerObj = new GameObject("Player");
        playerObj.transform.position = new Vector3(0f, 0.12f, 0.5f);
        playerObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.28f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.skinWidth = 0.03f;
        cc.minMoveDistance = 0f;

        inputReader = playerObj.AddComponent<PlayerInputReader>();
        SetPrivateRef(inputReader, "actions", inputActions);

        playerObj.AddComponent<PlayerMotor>();

        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(playerObj.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        camObj.transform.localRotation = Quaternion.identity;

        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.fieldOfView = 70f;
        camObj.AddComponent<AudioListener>();

        GameObject holdPointObj = new GameObject("HoldPoint");
        holdPointObj.transform.SetParent(camObj.transform, false);
        holdPointObj.transform.localPosition = new Vector3(0.28f, -0.22f, 0.65f);

        PlayerLook look = playerObj.AddComponent<PlayerLook>();
        SetPrivateRef(look, "cameraPivot", camObj.transform);

        PlayerInteractor interactor = playerObj.AddComponent<PlayerInteractor>();
        SetPrivateRef(interactor, "playerCamera", cam);
        SetPrivateFloat(interactor, "range", 3f);

        carrier = playerObj.AddComponent<ItemCarrier>();
        SetPrivateRef(carrier, "holdPoint", holdPointObj.transform);
        carrier.EnsureItemCamera();
        carrier.UpdateItemCameraState();
        playerObj.AddComponent<BuildPlacer>();

        return interactor;
    }

    // Dựng các hệ thống mở rộng Phase 4: dọn dẹp, đào coin, thú cưng, đường phố, cửa hàng, xe, NPC, bản đồ
    static void BuildPhase4Content(Transform envRoot, GameObject worldObj)
    {
        worldObj.AddComponent<MapSystem>();

        CleaningSystem cleaning = envRoot.gameObject.AddComponent<CleaningSystem>();
        GameObject mopObj = InstantiatePrefab("Assets/Prefabs/Prototype/Items/Proto_Mop.prefab", envRoot, new Vector3(-2.4f, 0.1f, 2.2f), Vector3.zero);
        mopObj.AddComponent<Mop>();

        GameObject dirt1 = new GameObject("DirtSpot_1");
        dirt1.transform.SetParent(envRoot, false);
        dirt1.transform.localPosition = new Vector3(-0.8f, 0.11f, 0.6f);
        dirt1.transform.localScale = new Vector3(0.6f, 0.02f, 0.6f);
        dirt1.AddComponent<MeshFilter>();
        dirt1.AddComponent<MeshRenderer>();
        BoxCollider dirtBox = dirt1.AddComponent<BoxCollider>();
        dirtBox.isTrigger = true;
        dirt1.AddComponent<Dirt>();
        dirt1.SetActive(false);

        GameObject miningObj = InstantiatePrefab("Assets/Prefabs/Prototype/PcParts/Proto_PcCase.prefab", envRoot, new Vector3(-2.45f, 0.1f, -1.2f), new Vector3(0f, 90f, 0f));
        miningObj.name = "BitcoinMiningRig";
        MiningRig miningRig = miningObj.AddComponent<MiningRig>();
        SetPrivateFloat(miningRig, "miningPower", 2.5f);

        GameObject catObj = InstantiatePrefab("Assets/Prefabs/Prototype/Characters/Proto_Cat.prefab", envRoot, new Vector3(0.9f, 0.1f, -0.4f), Vector3.zero);
        catObj.AddComponent<Pet>();

        GameObject cityRoot = new GameObject("OutsideCity");
        cityRoot.transform.SetParent(envRoot, false);

        InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_RoadSegment.prefab", cityRoot.transform, new Vector3(-12f, 0f, 8f), Vector3.zero);
        InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_RoadSegment.prefab", cityRoot.transform, new Vector3(0f, 0f, 8f), Vector3.zero);
        InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_RoadSegment.prefab", cityRoot.transform, new Vector3(12f, 0f, 8f), Vector3.zero);
        InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_StreetLamp.prefab", cityRoot.transform, new Vector3(-4f, 0f, 5.5f), Vector3.zero);
        InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_StreetLamp.prefab", cityRoot.transform, new Vector3(4f, 0f, 5.5f), Vector3.zero);
        InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_Tree.prefab", cityRoot.transform, new Vector3(-6f, 0f, 11.5f), Vector3.zero);
        InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_Tree.prefab", cityRoot.transform, new Vector3(6f, 0f, 11.5f), Vector3.zero);

        GameObject pawnShopObj = InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_Shop.prefab", cityRoot.transform, new Vector3(-10f, 0f, 12.5f), new Vector3(0f, 180f, 0f));
        pawnShopObj.name = "PawnShopBuilding";
        pawnShopObj.AddComponent<PawnShop>();
        MapMarker pawnMarker = pawnShopObj.AddComponent<MapMarker>();
        pawnMarker.SetLabel("Tiệm Cầm Đồ");

        GameObject dealerObj = InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_Shop.prefab", cityRoot.transform, new Vector3(10f, 0f, 12.5f), new Vector3(0f, 180f, 0f));
        dealerObj.name = "VehicleDealerBuilding";
        VehicleDealer dealer = dealerObj.AddComponent<VehicleDealer>();
        MapMarker dealerMarker = dealerObj.AddComponent<MapMarker>();
        dealerMarker.SetLabel("Showroom Xe");

        GameObject propShopObj = InstantiatePrefab("Assets/Prefabs/Prototype/World/Proto_Apartment.prefab", cityRoot.transform, new Vector3(0f, 0f, 15.5f), new Vector3(0f, 180f, 0f));
        propShopObj.name = "PropertyAndFashionBuilding";
        propShopObj.AddComponent<PropertyMarket>();
        propShopObj.AddComponent<BarberShop>();
        propShopObj.AddComponent<ClothingStore>();
        MapMarker propMarker = propShopObj.AddComponent<MapMarker>();
        propMarker.SetLabel("Tiệm Thời Trang & Cắt Tóc");

        GameObject casinoRoot = new GameObject("CasinoZone");
        casinoRoot.transform.SetParent(cityRoot.transform, false);
        casinoRoot.transform.localPosition = new Vector3(-5f, 0.1f, 11.5f);
        MapMarker casinoMarker = casinoRoot.AddComponent<MapMarker>();
        casinoMarker.SetLabel("Khu Giải Trí Casino");

        GameObject bjObj = InstantiatePrefab("Assets/Prefabs/Prototype/Minigames/Proto_BlackjackTable.prefab", casinoRoot.transform, new Vector3(-1.8f, 0f, 0f), Vector3.zero);
        bjObj.AddComponent<BlackjackGame>();

        GameObject slotObj = InstantiatePrefab("Assets/Prefabs/Prototype/Minigames/Proto_SlotMachine.prefab", casinoRoot.transform, Vector3.zero, Vector3.zero);
        slotObj.AddComponent<CasinoSlotMachine>();

        GameObject spinObj = InstantiatePrefab("Assets/Prefabs/Prototype/Minigames/Proto_LuckySpinMachine.prefab", casinoRoot.transform, new Vector3(1.8f, 0f, 0f), Vector3.zero);
        spinObj.AddComponent<LuckySpin>();

        GameObject shelfObj = InstantiatePrefab("Assets/Prefabs/Prototype/Furniture/Proto_Shelf.prefab", envRoot, new Vector3(0.4f, 0.1f, -2.5f), Vector3.zero);
        ComicBook.MakeShelfHollowForRaycast(shelfObj.transform);
        GameObject comicBookObj = new GameObject("ComicBook_Vol_1");
        comicBookObj.transform.SetParent(shelfObj.transform, false);
        comicBookObj.transform.localPosition = new Vector3(0.04f, 0.935f, 0.04f);
        comicBookObj.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
        ComicBook comicComp = comicBookObj.AddComponent<ComicBook>();
        comicComp.EnsureBookMeshAndCollider();

        GameObject talkerObj = InstantiatePrefab("Assets/Prefabs/Prototype/Characters/Proto_NpcCivil.prefab", cityRoot.transform, new Vector3(2.2f, 0.1f, 5.2f), new Vector3(0f, 180f, 0f));
        talkerObj.name = "NpcTalker_River";
        talkerObj.AddComponent<NpcTalker>();

        GameObject bikeObj = InstantiatePrefab("Assets/Prefabs/Prototype/Vehicles/Proto_Bicycle.prefab", cityRoot.transform, new Vector3(-3f, 0.1f, 6.8f), new Vector3(0f, 90f, 0f));
        bikeObj.AddComponent<VehicleController>();

        GameObject motoObj = InstantiatePrefab("Assets/Prefabs/Prototype/Vehicles/Proto_Motorbike.prefab", cityRoot.transform, new Vector3(3f, 0.1f, 6.8f), new Vector3(0f, 90f, 0f));
        motoObj.AddComponent<VehicleController>();

        GameObject carObj = InstantiatePrefab("Assets/Prefabs/Prototype/Vehicles/Proto_Car.prefab", cityRoot.transform, new Vector3(8.5f, 0.1f, 9.2f), new Vector3(0f, -90f, 0f));
        VehicleController carCtrl = carObj.AddComponent<VehicleController>();
        SetPrivateRef(dealer, "showroomVehicle", carCtrl);

        GameObject npcRoot = new GameObject("NpcWalkPoints");
        npcRoot.transform.SetParent(cityRoot.transform, false);
        Vector3[] pts =
        {
            new Vector3(-10f, 0.1f, 6.2f),
            new Vector3(0f, 0.1f, 6.2f),
            new Vector3(10f, 0.1f, 6.2f),
            new Vector3(0f, 0.1f, 10.2f)
        };
        for (int i = 0; i < pts.Length; i++)
        {
            GameObject wp = new GameObject($"WalkPoint_{i}");
            wp.transform.SetParent(npcRoot.transform, false);
            wp.transform.localPosition = pts[i];
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject civObj = InstantiatePrefab("Assets/Prefabs/Prototype/Characters/Proto_NpcCivil.prefab", cityRoot.transform, pts[i], Vector3.zero);
            NpcCivilian civ = civObj.AddComponent<NpcCivilian>();
            List<Transform> wps = new List<Transform>();
            foreach (Transform t in npcRoot.transform) wps.Add(t);
            civ.SetWaypoints(wps);
        }
    }

    // Dựng model 3D riêng biệt của người chơi (playerMesh), mặc định tắt khi đi bộ FPS và chỉ bật khi ngồi ghế/stream giống SLS2
    static GameObject BuildPlayerMesh(Transform parent, Transform chairTransform)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Prototype/Characters/Proto_Player.prefab");
        GameObject meshObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        meshObj.name = "PlayerMesh";
        meshObj.transform.SetPositionAndRotation(chairTransform.position, chairTransform.rotation);

        CapsuleCollider existingCapsule = meshObj.GetComponent<CapsuleCollider>();
        if (existingCapsule != null)
            Object.DestroyImmediate(existingCapsule);

        meshObj.SetActive(false);
        return meshObj;
    }

    // Tạo một GameObject có gắn UIDocument và component điều khiển
    static T CreateUIDoc<T>(string name, Transform parent, PanelSettings panelSettings, string uxmlPath, int sortingOrder) where T : Component
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        UIDocument doc = go.AddComponent<UIDocument>();
        doc.panelSettings = panelSettings;
        doc.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(uxmlPath))
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
        return go.AddComponent<T>();
    }

    // Khởi tạo prefab tại vị trí và góc xoay cục bộ
    static GameObject InstantiatePrefab(string path, Transform parent, Vector3 localPos, Vector3 localEuler)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        return instance;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void SetPrivateRef(Object target, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetPrivateBool(Object target, string fieldName, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetPrivateFloat(Object target, string fieldName, float value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetPrivateEnum(Object target, string fieldName, int enumValueIndex)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.intValue = enumValueIndex;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void RegisterBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(HomeScenePath, true)
        };

        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing.path != MenuScenePath && existing.path != HomeScenePath)
                scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
