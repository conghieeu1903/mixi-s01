# Streamer Life Simulator 2 — Các chức năng game cần có

> Cập nhật: 2026-10-20

## Tổng quan

Game là sim đời sống streamer: người chơi sống trong nhà, xây dựng dàn máy, mở stream để kiếm tiền, đồng thời giữ các chỉ số sinh tồn (ngủ, ăn, vệ sinh, mood). Tài liệu liệt kê chức năng theo hệ thống, mỗi hệ thống ghi script chính.

Giới hạn: suy ra từ tên class và các field/method đọc được trong `Assembly-CSharp`. Công thức cân bằng (tiền, lượng viewer, xác suất sự kiện) nằm trong thân hàm và chưa được ghi lại ở đây. Mức ưu tiên ở cuối là đề xuất, không phải đọc từ code.

## 1. Nhân vật và điều khiển

| Chức năng | Script chính |
| --- | --- |
| Di chuyển góc nhìn thứ nhất (đi, chạy, nhảy, ngồi) | `PlayerMotor`, `EFPController`, `FirstPersonAIO` |
| Góc nhìn thứ ba, camera tự do | `ThirdPersonController`, `FreeCameraManager`, `CameraControllerEasy` |
| Tạo và tùy biến nhân vật (nam/nữ, tóc, quần áo) | `CustomizeCharacter`, `CC/*` |
| Cửa hàng quần áo, tiệm tóc | `ClothStore`, `ClothApps`, `BarberShop`, `HairDresserNpc` |
| Đóng băng người chơi khi mở UI, ẩn/hiện chuột | `PlayerMotor.SetFreezed`, `InputManager` |
| Cấu hình phím, đổi phím | `Keybinding`, `KeybindingManager`, `ControlsPanel` |
| Hỗ trợ mobile | `mobileControls`, `PlayerController_ForMobile` |

## 2. Thời gian và môi trường

| Chức năng | Script chính |
| --- | --- |
| Đồng hồ trong game (giờ, phút, ngày) | `GameManager` (`hour`, `minute`, `day`) |
| Chu kỳ ngày đêm (mặt trời mọc 6h, lặn 18h) | `DayNightManager` |
| Ngủ qua ngày, hiển thị ngày | `Bed`, `UIManager.sleepPanel`, `sleepDayText` |
| Cảnh trong nhà và ngoài trời, ra/vào nhà | `UIManager.PlayerEnterHouse/PlayerExitHouse` |
| Dịch chuyển, điểm đặt lại vị trí | `TeleportManager`, `PlayerTeleportHome`, `ResetPosition` |
| Tối ưu hiển thị (culling, độ phân giải động) | `OptimizeManager`, `DynamicResManager` |

## 3. Chỉ số sinh tồn và tâm trạng

| Chức năng | Script chính |
| --- | --- |
| Năng lượng và stress (hiện trên điện thoại) | `PhoneManager` (`energyBar`, `stressBar`) |
| Bộ đếm thức, đói, đi vệ sinh, lâu không tắm | `GameManager` (`awekingTime`, `hungerTime`, `toiletTime`, `notShowerTime`) |
| Mood và debuff theo mood | `MoodController`, `MoodDebuff` |
| Bệnh, thuốc | `GameManager.playerGetSick`, `Medicines`, `SpawnedMedicine` |
| Ăn uống: tủ lạnh, đồ ăn, ứng dụng gọi món, shipper | `Fridge`, `Food`, `EatApps`, `FoodDeliveryApps`, `FoodCourier` |
| Ngủ, tắm, đi vệ sinh (có panel riêng) | `Bed`, `UIManager.showerPanel`, `toiletPanel` |
| Dọn dẹp nhà (lau, quét, bụi bẩn) | `CleanManager`, `Mop`, `Dirt` |

## 4. Kinh tế

| Chức năng | Script chính |
| --- | --- |
| Tiền (`money`) và Bitcoin (`ownedBitcoin`) | `GameManager`, `MoneyTextShower` |
| Hóa đơn định kỳ, số hóa đơn chưa đọc | `BillPanel`, `BillUI` |
| Mua sắm online (giỏ hàng, đơn, giao hàng) | `ZamazorPanel`, `ZamazorOrderManager`, `ZamazorItem`, `Courier`, `CargoBox` |
| Thuê/mua nhà | `BuyableHouse`, `House`, `HousePage` |
| Mua xe, xe đạp, xe máy | `BuyCar`, `CarManager`, `BikeManager` |
| Tiệm cầm đồ (bán lại đồ) | `PawnShop`, `PawnItem` |
| Làm việc phụ: shipper thư, tìm việc | `MailJobManager`, `MailPanel`, `WorkerManager` |
| Giá và độ hiếm của vật phẩm | `ItemObject` (`itemPrice`, `star` 0.5–5 sao) |

## 5. Xây dựng dàn máy (lõi gameplay)

| Chức năng | Script chính |
| --- | --- |
| Đặt, xoay, hủy đặt, nhặt, thả vật phẩm trong nhà | `Item`, `Buildable`, `BuildIndicators`, `PlayerMotor` |
| Lắp PC: mainboard, CPU, tản nhiệt, RAM ×4, card đồ họa ×2, nguồn, HDD, CD-ROM | `PcCase`, `CaseBuildSlot`, `Motherboard` |
| Lắp rig đào coin | `RigCase`, `RigBuildSlot`, `BitMiner` |
| Chất lượng máy (`caseQuality`), tổng RAM, FPS máy | `ComputersPanel`, `GameManager.GetComputerFPS` |
| Gán PC vào bàn, nhiều màn hình (tối đa 3) | `ComputerDesk`, `AssignComputerDesk`, `GlobalComputerCanvas` |
| Router và tốc độ mạng (tải lên/xuống) | `Router`, `GlobalComputerCanvas.downloadSpeed/uploadSpeed` |
| Kho đồ, hộp đồ | `Inventory`, `InventoryItem`, `ChestInventory`, `Slot` |
| Sơn tường, thay hình nền | `WallPaintPanel`, `WallPaintSample`, `WallpaperSelect` |

## 6. Hệ thống stream (trung tâm)

| Chức năng | Script chính |
| --- | --- |
| Mở/đóng stream, tên và stream key | `StreamEventManager.StreamOpened/StreamClosed`, `GameManager` |
| Viewer, follower, sub, giới hạn viewer theo ngày | `StreamEventManager`, `GameManager.totalFollowers/totalSubs/currentViewer` |
| Chat người xem sinh tự động, độ trễ chat | `ChatMessageManager`, `ChatPanel`, `StreamEventManager.ShowChatMessage` |
| Donate (tiền, âm thanh, hiển thị) | `Donate`, `DonateMessages`, `SoundDonate`, `SoundDonatePanel` |
| Thẻ chọn hành động khi stream, bộ chọn thẻ | `CardScript`, `Card`, `StreamEventManager.SelectedCard` |
| Minigame khi stream: Just Chatting, Dance, ASMR, Pool, Game | `JustChattingGame`, `DanceGame`, `AsmrGame`, `StreamEventManager` |
| Sự cố khi stream (lag, hỏng máy, virus) | `StreamEventManager.OpenProblem`, `GameStreamProblemDetect` |
| Độc hại (toxicity), kiểm duyệt, cấm người xem | `GameManager.totalToxicity`, `UIManager.ToxicUserBanned` |
| Partner, thành tích tượng trưng | `GameManager.partner`, `CreatorAward` |
| Lịch sử stream, thống kê, top stream | `StreamHistory`, `TopStreamInfoPanel`, `FeaturedStream` |
| Nhân viên stream (làm việc, theo, ngủ, tuần tra, ăn) | `StreamerWorker`, `StreamerWorkerCanvas`, `StreamWorkerPanel`, `StreamerEnum` |
| Công cụ và overlay stream | `StreamerTool`, `StreamerToolsItem`, `GlobalCanvasInfoBar` |

## 7. Ứng dụng trên máy tính

| Ứng dụng | Chức năng | Script |
| --- | --- | --- |
| Browser | Duyệt web thật bằng WebView, tạm dừng khi pause | `BrowserManager`, `BrowserPanel` |
| Web nội bộ | Trang web giả, back/forward/trang chủ | `WebPanel`, `WebContentPanel`, `FastWeb` |
| Youhube | Đăng/xem video, phim | `Youhube`, `YouhubeManagers`, `YouhubeFilmStrip`, `UploadedVideos` |
| Discat | Chat kiểu Discord, bot, fan, hater, hacker | `Discat`, `Discat*Manager`, `DiscatMessage` |
| Smoke | Thư viện game kiểu Steam | `Smoke`, `SmokeGameBox` |
| Zamazor | Mua sắm | `ZamazorPanel` |
| Mail, Bill | Thư, hóa đơn | `MailPanel`, `BillPanel` |
| Fakegram | Mạng xã hội giả | `Fakegram`, `FakegramPost` |
| Spoti | Nghe nhạc | `Spoti`, `SpotiSong`, `MusicItem` |
| GTU | Panel GTU (mini game dev) | `GtuPanel`, `GTU`, `GtuDevelopingLever` |
| Virus Scanner | Quét virus | `VirusScannerApp` |
| Crypto | Đào, xem coin | `CryptoWeb`, `CryptoMiningManager`, `DetectedMinerUI` |
| Webcam | Camera | `Webcam` |
| Ops | Hệ điều hành/tiện ích | `Ops`, `OpsDownload`, `Apps` |
| Tiện ích | Link worker, BBS, Slot web | `LinkWorkerWeb`, `BBSWindow`, `SlotWebGame` |

Hệ thống cửa sổ: `PcPanel` (kéo, thu nhỏ, phóng to, focus), taskbar `Bottom`/`BottomPanel`, màn hình boot `BootingPanel`, Blue Screen khi dính virus.

## 8. Điện thoại

| Chức năng | Script chính |
| --- | --- |
| Mở/đóng, màn hình Home, app có màn loading 2 giây | `PhoneManager`, `PhoneAppsPanel`, `PhoneAppsInfo` |
| App đồ ăn | `EatApps` |
| App quần áo | `ClothApps` |
| App thú cưng | `PetAppsPanel` |
| App xe (gọi xe, gỡ kẹt xe, bãi đỗ) | `PhoneManager.UnstuckCarButton`, `CarParkingIndicator` |
| App camera, album ảnh | `CameraApps`, `AlbumAppsPanel`, `AlbumPhoto` |
| App kỹ năng | `SkillAppsPanel` |
| App bản đồ | `MapPanel`, `PhoneManager.OpenMapApp` |
| Chặn app khi lái xe hoặc chưa mặc đồ (cảnh báo 11 ngôn ngữ) | `PhoneManager.OpenApp` |

## 9. Kỹ năng và tiến trình

| Chức năng | Script chính |
| --- | --- |
| Điểm kỹ năng (`currentSkillPoint`) và thông báo nhận điểm | `SkillManager`, `UIManager.NewSkillPointEanerdInfoBox` |
| 22 kỹ năng: Sleeper, Thrifty, PartTime, CryptoExpert, Zombie, PetLover, Ambitious, Satiated, Energetic, PickMe, Donate, Donator, SweetDream, Chatter, Tongue, TheWhip, Gaming, Dance, Pool, Boss, Sponsored, Youhuber | `SkillApp` |
| Hướng dẫn người chơi mới | `TutorialManager`, `Tutorials`, `TutorialObject`, `DiscatTutorialManager` |

## 10. Thế giới, NPC và phương tiện

| Chức năng | Script chính |
| --- | --- |
| NPC dân thường: đi bộ, nhận ra người chơi, ragdoll | `NpcCivil`, `NpcCivilEnum`, `NpcManager`, `CivilWalkPoint` |
| NPC có hội thoại và nhiệm vụ | `Talk*Panel` (River, Hobo, Mimi, LoverMimi, Frog ×3, Police, Hermit, BigBoy, FanBoy, CrispyJerry) |
| Cửa, interphone, cửa AI | `Door`, `DoorAI`, `DoorIntercom`, `DoorCharactersManager` |
| Shipper giao hàng (trạng thái đến, chờ cửa, quay về) | `Courier`, `CourierEnum`, `ScooterCourier` |
| Lái xe: xe người chơi, xe NPC, đèn, nhạc trên xe | `CarManager`, `MyCar`, `NpcCarManager`, `CarWithMusic` |
| Xe đạp, xe máy | `BikeManager`, `BikeCamera`, `biker_logic_mecanim` |
| Thú cưng: hoang, nuôi, đi theo, cầm tay | `Pet`, `PetManager`, `PetStatus` |
| Bản đồ, minimap, điểm đánh dấu địa điểm | `MapPanel`, `MapCamera`, `IndicatorPrefab_*` |
| Cảnh sát/SWAT | `Swat`, `SwatManager`, `SwatCamera` |

## 11. Minigame và giải trí

| Minigame | Script |
| --- | --- |
| Blackjack | `BlackjackManager`, `BlackjackCard` |
| Slot machine (club và web) | `SlotMachineClub`, `SlotRoll`, `SlotWebGame` |
| Vòng quay may mắn | `LuckySpinMachine`, `LuckySpinCollider`, `LuckySpinDetector` |
| Cào thẻ | `ScratchManager`, `MyScratch` |
| Rút thăm | `Raffle`, `RaffleManager` |
| Nhảy theo nhịp | `DanceGame`, `DanceKey`, `DanceBar` |
| ASMR | `AsmrGame`, `AsmrGameSlider` |
| Brain rot (clicker, lootbox, toilet) | `BrainRotClicker`, `BrainRotLootbox`, `BrainRotToilet` |
| CamperStrike (FPS nhỏ) | `CamperStrike`, `CamperStrikeManager`, `CamperAI` |
| Pogo | `PogoCameraFollow`, `PogoCheckpoint`, `PogoWinningTrigger` |
| Đua xe | `CheckpointManager` |
| Skin case (mở hộp skin) | `SkinCase`, `SkinInventory`, `CaseSelectline` |
| Câu cá/vẽ/quà | `FrogGiftPainting`, `GiftBox` |

## 12. Hệ thống nền

| Chức năng | Script chính |
| --- | --- |
| Lưu/tải game (Easy Save 3), auto save | `SaveManager`, `Apps.Save/Load`, `ES3Types` |
| Cài đặt: đồ họa, độ phân giải, FPS, vsync, âm lượng, độ nhạy, FOV, đảo trục Y, motion blur | `SettingsMenu` |
| Đa ngôn ngữ (11 ngôn ngữ), dịch tự động | `TranslatorManager`, `TranslatedLanguage`, `DeepLTranslator`, `GptTranslator`, `PrefabTranslator` |
| Âm thanh: nhạc, hiệu ứng, UI | `AudioManager`, `AVP_Manager_Audio` |
| Chuyển scene có loading | `Michsky.LSS`, `MenuManager`, `PausePanel` |
| Pause (`Time.timeScale = 0`) | `UIManager.OpenPauseMenu` |
| Steam: ngôn ngữ, thành tích | `SteamManager`, `MenuManager.GetLanguageFromSteamApi` |
| Con trỏ chuột theo ngữ cảnh | `CursorChanger`, `CursorIconManager`, `CursorEnum` |
| Điều khiển từ xa/ghi log | `RemoteManager`, `DownloadHelperManager` |
| Hiệu ứng hình: bloom, DOF, lens flare, fader, flashbang | `MenuManager.EnableDof`, `GameManager.faderImage` |

## Mức ưu tiên đề xuất khi dựng lại

| Giai đoạn | Chức năng |
| --- | --- |
| **1. Lõi chơi được** | Mục 1 (di chuyển), 2 (thời gian), 5 (đặt vật phẩm, lắp PC), 12 (lưu, cài đặt, pause), UI cơ bản |
| **2. Vòng lặp chính** | Mục 6 (stream), 7 (browser và vài app), 4 (tiền, mua sắm), 3 (chỉ số sinh tồn) |
| **3. Mở rộng** | Mục 8 (điện thoại), 9 (kỹ năng), 10 (NPC, xe, thú cưng) |
| **4. Nội dung thêm** | Mục 11 (minigame), dịch tự động, tích hợp Steam |

## Tài liệu liên quan

- `StreamerLifeSim2_UI_Flow.md`: các loại UI và luồng chạy.
- `StreamerLifeSim2_Models.md`: danh sách model 3D.
- `StreamerLifeSim2_Packages.md`: danh sách package.
