# Streamer Life Simulator 2 — Danh sách đầy đủ core mechanic

> Cập nhật: 2026-10-20

Tài liệu này là **checklist** toàn bộ cơ chế game, đối chiếu với 469 script trong `SLS2/Scripts/Assembly-CSharp`. Công thức chi tiết của các hệ thống chính nằm ở `StreamerLifeSim2_CoreMechanics.md`. Tài liệu này bổ sung các hệ thống mà file đó chưa phủ, kèm số liệu đọc trực tiếp từ code.

Cột "Mức" cho biết vai trò: **Core** (thiếu thì game không chạy), **Hỗ trợ** (tăng chiều sâu), **Phụ** (nội dung thêm, có thể bỏ khi làm lại).

## A. Vòng lặp lõi

| # | Mechanic | Mức | Script chính | Ghi chú |
| --- | --- | --- | --- | --- |
| A1 | Đồng hồ ngày/giờ/phút, sự kiện mỗi giờ/ngày | Core | `GameManager`, `DayNightManager` | Chi tiết ở CoreMechanics mục 1 |
| A2 | Mở/đóng stream, vòng đời stream | Core | `StreamEventManager`, `Ops` | CoreMechanics mục 2 |
| A3 | Viewer: dao động, tăng theo %, giới hạn theo ngày | Core | `StreamEventManager` | `UpdateViewerPercent`, `ViewerWave`, `ViewerLimitation` |
| A4 | Follower, Sub | Core | `StreamEventManager` | `EarnFollower`, `SubsEarnRoutine` |
| A5 | Thẻ hành động khi stream (thành công/thất bại) | Core | `CardScript`, `StreamEventManager.SelectedCard` | Xác suất theo thẻ |
| A6 | Chọn category (just chatting, gaming, ASMR, dancing, pool) | Core | `Ops`, `StreamerEnum` | Mỗi category gắn một kỹ năng và một minigame |
| A7 | Donate | Core | `DonateLab`, `Donate`, `SoundDonate`, `DonateMessages` | `SpawnDonate(nick, msg, tiền)`; kỹ năng `donateSkill` đổi hành vi; có donate âm thanh và donate video/link |
| A8 | Chat tự sinh, toxic, cấm người xem | Hỗ trợ | `ChatController`, `ChatMessageManager`, `FakeViewer` | Tốc độ chat theo số viewer |
| A9 | Lịch sử stream/video đã đăng | Hỗ trợ | `Youhube`, `StreamHistory`, `UploadedVideos` | Mục sinh ra khi stream từ 10 phút |
| A10 | Top stream, giải thưởng sáng tạo | Phụ | `TopStreamInfoPanel`, `FeaturedStream`, `CreatorAward` | |

## B. Minigame trong stream

Mỗi minigame đóng khi tắt stream và đẩy viewer qua `UpdateViewerPercent`.

| # | Minigame | Mức | Script | Cách chơi / thưởng (đọc từ code) |
| --- | --- | --- | --- | --- |
| B1 | Just Chatting | Core | `JustChattingGame`, `JCGameScriptable` | Chọn ngẫu nhiên một câu hỏi từ danh sách ScriptableObject; trả lời đúng/sai (xác suất 50% ở nhánh ngẫu nhiên) |
| B2 | ASMR | Core | `AsmrGame`, `AsmrGameSlider` | 5–9 slider mục tiêu, phát âm kích hoạt ngẫu nhiên; xong thì `UpdateViewerPercent(2–5, 5, 30s)` |
| B3 | Dance | Core | `DanceGame`, `DanceKey`, `DanceBar`, `DancePressArea` | Bấm phím theo nhịp; xong thì `UpdateViewerPercent(2–5, 5, 30s)` |
| B4 | Pool | Hỗ trợ | `Pool` | Gắn kỹ năng Pool |
| B5 | Gaming (chơi game thật) | Hỗ trợ | `CamperStrikeManager`, `PogoWebsite`, `BrainRot*` | Xem mục D |

## C. Sinh tồn, mood, bệnh

| # | Mechanic | Mức | Script chính |
| --- | --- | --- | --- |
| C1 | Thức khuya, đói, vệ sinh, tắm | Core | `GameManager`, `Bed`, `Food`, `Fridge` |
| C2 | Mood 0–100, 6 mức trạng thái, debuff icon/banner | Core | `MoodController`, `MoodDebuff` |
| C3 | Bệnh và thuốc | Hỗ trợ | `GameManager.MaybePlayerGetSick`, `Medicines` |
| C4 | Stress | Hỗ trợ | `PlayerMotor` |
| C5 | Dọn nhà, độ sạch | Hỗ trợ | `CleanManager`, `Dirt`, `Mop`, `MyMop`, `Janitor` |
| C6 | Nấu/ăn, giao đồ ăn | Hỗ trợ | `EatApps`, `FoodDeliveryApps`, `FoodCourier`, `BasketFoodObject`, `MealBag` |
| C7 | Ngoại hình: tóc, quần áo | Phụ | `BarberShop`, `HairDresserNpc`, `ClothStore`, `ClothApps`, `CustomizeCharacter` |

## D. Kinh tế và nguồn thu phụ

| # | Mechanic | Mức | Script | Số liệu từ code |
| --- | --- | --- | --- | --- |
| D1 | Tiền `money`, hóa đơn/tiền nhà mỗi ngày | Core | `GameManager`, `BillPanel`, `BillUI` | |
| D2 | Mua sắm online, giao hàng bằng thùng | Core | `ZamazorPanel`, `ZamazorOrderManager`, `CargoBox`, `Courier`, `ScooterCourier` | |
| D3 | Cầm đồ | Hỗ trợ | `PawnShop`, `PawnItem` | Bán đồ được **60% giá gốc** (`itemPrice * 0.6`) |
| D4 | Đào Bitcoin | Hỗ trợ | `CryptoMiningManager`, `RigCase`, `BitMiner`, `MinerShop`, `CryptoWeb` | Mỗi 2 giây: `miningPower / 3 × Random(3,5e-6 … 6e-6)` Bitcoin; phải có Router, nếu không thì không cộng. Quét thiết bị gần mất 3 giây; bật/tắt đèn rig |
| D5 | Việc phụ qua thư | Hỗ trợ | `MailJobManager`, `MailContent`, `MailPanel` | Thù lao ngẫu nhiên 20–29 hoặc 500–799 tùy loại việc; 7 người gửi |
| D6 | Nhân viên stream thuê | Hỗ trợ | `WorkerManager`, `StreamerWorker`, `LinkWorkerUI`, `StreamWorkerPanel` | 7 nhân viên (lao công, thợ sơn, 5 streamer). Mỗi streamer có `motivation`, `energy`, `hunger`, `earnedMoney`, follower, sub riêng. Tiền thu = `salary × 0,1 × hệ số / 100`; mỗi đợt follower +100–499 hoặc −100–349, sub +2–14 hoặc −2–9; cần bàn máy (`GetDeskQuality`), ăn, nghỉ |
| D7 | Lương nhân viên theo ngày | Hỗ trợ | `GameManager.CheckWorkerPayment` | |
| D8 | Mua nhà/thuê nhà, xe | Hỗ trợ | `BuyableHouse`, `HousePage`, `BuyCar`, `MyCar`, `CarManager` | |
| D9 | Sơn tường, giấy dán tường | Phụ | `WallPaintPanel`, `WallpaperSelect`, `PainterWorker` | |

## E. Casino và minigame cờ bạc (nguồn tiền phụ, có rủi ro)

| # | Mechanic | Mức | Script | Chi tiết từ code |
| --- | --- | --- | --- | --- |
| E1 | Blackjack | Phụ | `BlackjackManager`, `BlackjackCard` | Cược 10–5.000. Bước tăng: dưới 100 thì +10, dưới 1.000 thì +100, còn lại +500; bước giảm tương ứng, tối thiểu 10 |
| E2 | Slot web + bonus game | Phụ | `SlotWebGame`, `SlotWebGameLine`, `SlotRoll` | Có hệ số nhân rơi (`multiplierdropChange`, nhánh riêng trong bonus), vòng bonus cộng dồn `bonusGameTotalWin`, tỷ lệ vào bonus `bonusDropChange` |
| E3 | Slot ở club | Phụ | `SlotMachineClub` | |
| E4 | Lucky Spin | Phụ | `LuckySpinMachine`, `LuckySpinDetector`, `LuckySpinCollider` | |
| E5 | Cào thẻ (scratch) | Phụ | `ScratchManager`, `MyScratch` | |
| E6 | Rút thăm hằng ngày, giải thưởng | Phụ | `RaffleManager`, `Raffle`, `PrizeManager` | Reset mỗi ngày |
| E7 | Hòm skin (skin case) | Phụ | `SkinCase`, `SkinInventory` | |
| E8 | GTU (xổ số/giải thưởng) | Phụ | `GTU`, `GtuPanel`, `GtuDevelopingLever` | Reset mỗi ngày |

## F. Dàn máy và thiết bị

| # | Mechanic | Mức | Script |
| --- | --- | --- | --- |
| F1 | PC theo linh kiện, chất lượng → FPS | Core | `PcCase`, `CaseItem`, `CaseBuildSlot`, `Motherboard` |
| F2 | Mạng, router, tốc độ tải/lên | Core | `Router`, `OpsDownload`, `DownloadHelperManager` |
| F3 | Thiết bị stream: webcam, mic, camera, đèn | Core | `StreamerTool`, `StreamerToolsItem`, `Webcam`, `Ops.CheckSetupHasCamera`, `Ops.CheckOpsMicrophone` |
| F4 | Điện năng, hóa đơn điện | Hỗ trợ | `GameManager.UpdateElectricWorkingCount` |
| F5 | Virus, Blue Screen, antivirus | Hỗ trợ | `VirusScannerApp`, `BootingPanel` |
| F6 | Màn hình, bàn, ghế, vị trí máy | Hỗ trợ | `ComputerDesk`, `Monitor`, `Chair`, `AssignComputerDesk` |

## G. Mạng xã hội trong game (hệ thống Discat/Fakegram)

| # | Mechanic | Mức | Script | Dòng code |
| --- | --- | --- | --- | --- |
| G1 | Discat (chat Discord giả) | Hỗ trợ | `Discat`, `DiscatChatPanelSample`, `DiscatTutorialManager` | 207 / 134 / 1.902 |
| G2 | Fan, Hater, Hacker, Bot, Bit-man (NPC tương tác) | Hỗ trợ | `DiscatFanManManager`, `DiscatHaterManManager`, `DiscatHackerManManager`, `DiscatBotManManager`, `DiscatBitManManager` | 1.389 / 518 / 781 / 984 / 2.066; mỗi cái có `DailyCheck` và hội thoại nhiều nhánh; Bit-man liên quan donate (`Ops.CheckBitManDonate`) |
| G3 | Fakegram (feed ảnh, bài đăng mỗi ngày) | Phụ | `Fakegram`, `FakegramPost`, `PlayerPost` | |
| G4 | Spoti (nhạc), điện thoại, trình duyệt | Phụ | `Spoti`, `PhoneManager`, `BrowserManager`, `CellPhone` | |

## H. Kỹ năng và tiến trình

| # | Mechanic | Mức | Script |
| --- | --- | --- | --- |
| H1 | Cây 22 kỹ năng, điểm kỹ năng | Core | `SkillManager`, `SkillApp`, `SkillAppsPanel` |
| H2 | Hướng dẫn (tutorial) theo chặng | Core | `TutorialManager`, `TutorialObject`, `Tutorials` |
| H3 | Thành tích Steam | Phụ | `SteamManager` |
| H4 | Lưu/tải (Easy Save 3) | Core | `SaveManager` (850 dòng) |
| H5 | Đa ngôn ngữ 11 ngôn ngữ, dịch động | Hỗ trợ | `TranslatorManager`, `DeepLTranslator`, `GptTranslator` |
| H6 | Cài đặt, phím tắt, âm thanh | Hỗ trợ | `SettingsMenu`, `KeybindingManager`, `AudioManager` |

## I. Thế giới mở và nhân vật

| # | Mechanic | Mức | Script |
| --- | --- | --- | --- |
| I1 | Di chuyển FPS/TPS, tương tác raycast | Core | `PlayerMotor`, `FirstPersonAIO`, `ThirdPersonController` |
| I2 | Cầm/đặt/xoay vật phẩm, chế độ build | Core | `Inventory`, `Item`, `ItemObject`, `Buildable`, `ItemArea`, `BuildIndicators` |
| I3 | Xe hơi, xe máy, xe đạp | Hỗ trợ | `CarManager`, `BikeManager`, `ScooterCourier`, `bicycle_code` |
| I4 | NPC dân thường, xe NPC, cảnh sát/SWAT | Hỗ trợ | `NpcManager`, `NpcCivil`, `NpcCarManager`, `SwatManager` |
| I5 | Hội thoại NPC và nhiệm vụ nhỏ | Phụ | `Talk*Panel` (13 panel), `DoorCharactersManager` |
| I6 | Thú cưng | Hỗ trợ | `Pet`, `PetManager`, `PetStatus` (hệ số ảnh hưởng viewer qua `petManager.Calculate`) |
| I7 | Bản đồ, teleport | Phụ | `MapPanel`, `TeleportManager` |
| I8 | Truyện tranh sưu tầm | Phụ | `ComicBook`, `ComicBookManager` |

## J. Minigame chơi được (ngoài stream)

| # | Game | Script | Ghi chú |
| --- | --- | --- | --- |
| J1 | CamperStrike (bắn súng deathmatch) | `CamperStrikeManager` (465 dòng), `CamperAI*`, `CsPlayerMovement` | Có vòng đấu (`roundNumber`), bảng xếp hạng theo số kill, hồi sinh ngẫu nhiên |
| J2 | Pogo (platformer) | `PogoWebsite`, `PogoCheckpoint`, `PogoWinningTrigger` | Có checkpoint |
| J3 | BrainRot (clicker, lootbox, toilet) | `BrainRotClicker` (1.042 dòng), `BrainRotLootbox`, `BrainRotSpecial`, `BrainRotToilet` | |
| J4 | Các script lẻ chưa rõ vai trò | `Doll`, `SmokeGameBox`, `JumperSpecial` | Chưa đọc, chỉ biết tên |

## Chưa đọc kỹ

Đã liệt kê vai trò nhưng chưa trích công thức, cần đọc thêm nếu định làm lại:

- Nội dung từng thẻ trong `SelectedCard` (hơn 1.300 dòng) và bảng xác suất.
- Bảng giá/thưởng chi tiết của Blackjack, Slot, Lucky Spin, GTU, Raffle.
- Hội thoại và điều kiện kích hoạt của 5 manager Discat.
- Công thức hỏng linh kiện (`PcCase`) và tiêu thụ điện.
- Logic AI của CamperStrike và các NPC.

## Gợi ý khi làm lại bản nhỏ

Bộ tối thiểu để có game chạy được: **A1–A7, B1–B3, C1–C2, D1–D2, F1–F3, H1, H2, H4, I1–I2**. Phần casino (E), Discat (G), minigame ngoài stream (J) và thế giới mở (I3–I8) là nội dung mở rộng.
