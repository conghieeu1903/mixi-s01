# Roadmap — Các giai đoạn phát triển

> Cập nhật: 2026-10-20

Game được làm qua 5 giai đoạn. Mỗi giai đoạn kết thúc bằng một bản **chơi được**, và chỉ chuyển sang giai đoạn sau khi đạt tiêu chí "Hoàn thành khi" của giai đoạn trước. Mã cơ chế (A1, C2…) lấy từ `../MechanicsChecklist.md`.

| Giai đoạn | Mục tiêu | Tài liệu |
| --- | --- | --- |
| 1. Vòng lặp lõi | Đi lại, mở stream, kiếm tiền, ngủ sang ngày mới | [Phase1_CoreLoop.md](Phase1_CoreLoop.md) |
| 2. Sinh tồn và dàn máy | Phải chăm bản thân và nâng cấp máy thì stream mới tốt lên | [Phase2_SurvivalAndRig.md](Phase2_SurvivalAndRig.md) |
| 3. Chiều sâu stream | Mỗi buổi stream có lựa chọn, minigame và kỹ năng | [Phase3_StreamDepth.md](Phase3_StreamDepth.md) |
| 4. Mở rộng hệ thống | Nguồn thu phụ, app máy tính, thế giới bên ngoài | [Phase4_Expansion.md](Phase4_Expansion.md) |
| 5. Nội dung và hoàn thiện | Casino, minigame ngoài stream, thay asset thật | [Phase5_ContentPolish.md](Phase5_ContentPolish.md) |

Giai đoạn 1–3 là bộ tối thiểu để game chơi được trọn vòng. Giai đoạn 4–5 là nội dung mở rộng, có thể cắt bớt hoặc đổi thứ tự.

## Trạng thái

| Ký hiệu | Nghĩa |
| --- | --- |
| Có code | Đã có class và logic, chưa chắc đã nối vào scene hay chạy thử |
| Một phần | Có một phần logic, còn thiếu phần chính |
| Chưa làm | Chưa có code |

Trạng thái được ghi theo việc đọc `Assets/Scripts` ngày 2026-10-20, chưa chạy thử. Cập nhật lại cột trạng thái mỗi khi xong một việc.

## Tài nguyên đã có

- **UI:** wireframe UI Toolkit cho 17 màn trong `Assets/UI/Screens`, xem thử bằng scene `Assets/Scenes/Wireframe.unity`.
- **Model và animation:** 59 prefab greybox trong `Assets/Prefabs/Prototype`, animation trong `Assets/Arts/Animations/Prototype`, xem bằng scene `Assets/Scenes/PrototypeModels.unity`. Dựng lại bằng menu **Tools → Prototype**.

## Script khung

Mỗi giai đoạn đã có sẵn script khung trong `Assets/Scripts`: class, field và chữ ký hàm kèm comment, thân hàm để trống.

| Giai đoạn | Thư mục và file chính |
| --- | --- |
| 1 | `Interaction/` (IInteractable, PlayerInteractor), `World/` (ComputerDesk, Bed), `UI/` (UIPanel, UIManager, HudView, StreamOverlayView, StreamStartPanel, PauseMenu, SettingsMenu, MainMenu, BillPanel, ScreenFader, LoadingView), `Core/SceneLoader`, `Settings/GameSettings` |
| 2 | `Items/` (ItemData, FoodData, MedicineData, WorldItem, Inventory, ItemCarrier, BuildPlacer), `Survival/` (Fridge, Toilet, Bathtub), `Rig/` (PcCase, PcSlot, PcPartData, RigPerformance, Router, StreamGear, StreamSetup), `Computer/` (DesktopOS, PcWindow, PcApp), `Shop/` (ShopCatalog, Order, OrderService, CargoBox, Courier, ZamazorApp), `UI/` (NeedsView, InventoryView, PcBuildView) |
| 3 | `Streaming/Cards/`, `Streaming/Minigames/` (StreamMinigame, JustChattingGame, AsmrGame, DanceGame), `Streaming/Chat/`, `Streaming/YouhubeApp`, `Skills/` (SkillId, SkillData, SkillSystem), `Tutorial/`, `UI/` (ChatView, StreamCardView, SkillTreeView, TutorialView) |
| 4 | `Economy/` (PawnShop, CryptoMining, MiningRig, MailJobs, StreamWorker, WorkerManager, PropertyMarket, VehicleDealer), `Computer/` (VirusSystem, ElectricitySystem), `Social/` (Discat, Fakegram, Spoti, Browser), `Phone/`, `Home/` (CleaningSystem, Dirt, Mop), `Vehicles/`, `Npc/`, `Pets/`, `Map/` |
| 5 | `Casino/`, `Minigames/` (CamperStrike, PogoGame, BrainRotClicker), `Dialogue/`, `UI/DialogueView`, `Appearance/`, `Home/WallPainter`, `Collectibles/`, `Localization/`, `Settings/InputRebinding`, `Platform/Achievements` |

Vật nào người chơi tương tác được thì implement `IInteractable`. App trên máy tính kế thừa `PcApp`, app điện thoại kế thừa `PhoneApp`, panel UI kế thừa `UIPanel`, minigame khi stream kế thừa `StreamMinigame`.

## Nguyên tắc

- Số liệu cân bằng ban đầu là giá trị tạm, ghi chú `giá trị tạm` trong code để dễ tìm khi chỉnh.
- Logic tách khỏi MonoBehaviour (như `GameClock`, `StreamSystem`) để test được mà không cần scene.
- Giữ tên element UXML và tên tham số Animator (`Speed`, `Action`, `IsOpen`, `IsSpinning`) khi thay asset thật.

## Tài liệu liên quan

- `../Features.md`: chức năng theo hệ thống và script.
- `../CoreMechanics.md`: công thức của các hệ thống chính.
- `../MechanicsChecklist.md`: checklist toàn bộ cơ chế.
- `../UI_Flow.md`, `../Models.md`, `../AudioList.md`, `../Packages.md`.
