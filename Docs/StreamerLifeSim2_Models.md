# Streamer Life Simulator 2 — Danh sách model cần thiết

> Cập nhật: 2026-10-20

## Tổng quan

"Model" trong tài liệu này là model 3D / prefab mà code tham chiếu. Nguồn là thư mục `SLS2` đã export từ bản decompile: 1.023 prefab (`GameObject`), 8.784 mesh, 493 `ItemObject` (ScriptableObject mô tả vật phẩm), 2 scene (`MainMenu`, `MainScene`).

Giới hạn: danh sách lấy từ tên prefab và code `Assembly-CSharp`. Một số NPC thoại (Mimi, Frog, River, BigBoy, FanBoy) chưa thấy prefab riêng, có thể nằm trực tiếp trong scene. Các bản `Variant` và biến thể màu (`_0`, `_1`) được gộp lại.

## 1. Nhân vật người chơi (bắt buộc)

- Điều khiển: `PlayerController`, `PlayerController_ForMobile`, `ThirdPersonController`, `MainCamera`, `CameraController`, `char_ragDoll`.
- Tạo nhân vật (Character Creator): `CC_Dummy`, `base_mesh_costume_1`, `base_mesh_costume_2` và các set đi kèm.
- Quần áo và phụ kiện (nam/nữ, mỗi loại có bản `Variant`):

| Loại | Prefab |
| --- | --- |
| Tóc | `Hair_Afro_*`, `Hair_Bob_01`, `Hair_Long_*`, `Hair_Ponytail_*`, `Hair_Wavey_01`, `Hair_Side_Part_*`, `Hair_Short_*`, `Hair_Receded_01`, `Hair_Beard_01-04` |
| Áo | `Shirt_01`, `TShirt_01`, `Hoodie_01`, `Jacket_01`, `Leather_Jacket`, `Denim_Jacket_*`, `Suit_Jacket`, `Sweater`, `Polo_Shirt_01`, `Vest`, `Crop_Top_*`, `Sleeveless`, `Casual_Shirt_01`, `Bra` |
| Quần, váy | `Jeans_*`, `Jeans_Baggy`, `Shorts_01`, `Suit_Pants`, `Jogging_Pants`, `Cargo_Pants_01`, `Skirt*`, `Underwear_*` |
| Giày, tất | `Sneaker_01/02`, `Boots_01`, `Ankle_Boots`, `Sandals_*`, `High_Heels`, `Dress_Shoes_01`, `Socks_*` |
| Phụ kiện | `Glasses_*`, `Wristwatch_*`, `Bracelet`, `Cowboy_Hat`, `Kettle_Helmet`, `Bicycle_Helmet` |

## 2. NPC

| Loại | Prefab | Script liên quan |
| --- | --- | --- |
| NPC thoại | `Hobo_Jerry`, `Hermit`, `Hippie_Matt` (có bản guitar), `crazy_old_man_ver1-3`, `GiantMan`, `Gopnik`, `streetcop3`, `FangBaby`, `SahurWalk` | `Hobo`, `Hippie`, `Mimi`, `Swat`, `Talk*Panel` |
| Dân thường | `NpcCivil 1-17` | `NpcCivil`, `NpcManager` |
| Giao hàng | Shipper, shipper đồ ăn, shipper xe máy | `Courier`, `FoodCourier`, `ScooterCourier` |
| Đặc biệt | SWAT, bot minigame CamperStrike, lao công, vũ công club, `DollParent` | `Swat`, `CamperAI`, `Janitor`, `ClubDancer`, `MidDancer`, `Doll` |
| Thú cưng | `Cat`, `CatFat` | `Pet`, `PetManager` |

## 3. Phương tiện

- Xe người chơi: `Motorbike`, `PG86_Stock`, `PG86_Drift`, `S34_Stock`, `S34_Race`, `GMS_4x4`, `Truck`, `Trailer_Green/Red/White`.
- Xe NPC: `Demo Car Ai`, `Demo Car Ai 2-6`, `Wheel_01`.
- Khác: `PogoStick Controller`, `PogoStickBase`, `Prop_MotorBike_Skeleton_Rope`.
- Hiệu ứng và nội thất xe: `CarSFX`, `CarVFX`, `CarSpotLight`, `Dashboard_OldCar`.
- Script: `CarManager`, `BikeManager`, `NpcCarManager`, `BuyCar`, `MyCar`.

## 4. Linh kiện PC (lõi gameplay build máy)

Mỗi linh kiện có nhiều đời, mỗi cái gắn một `ItemObject` (giá, sao, `quality`, `gb`). Các slot được định nghĩa trong `PcCase`.

| Linh kiện | Prefab | Số lượng |
| --- | --- | --- |
| Vỏ case | `PcCase1-16`, `Pc_Case_c_`, `pc_case_g_` | 16+ |
| Rig case (đào coin) | `Rig Case`, `Bit Miner` | 1 + 1 |
| Mainboard | `motherboard1-7` | 7 |
| CPU | `cpu_1-9` | 9 |
| Tản nhiệt CPU | `cpu_cooler1-14` | 14 |
| RAM | `Ram1-6` | 6 |
| Card đồ họa | `GraphicsCard1-12` | 12 |
| Nguồn | `psu1-2` | 2 |
| Ổ cứng | `hdd` | 1 |
| Ổ đĩa | `cd_rom` | 1 |
| Router | `Router1-6` | 6 |

## 5. Thiết bị setup stream

| Loại | Prefab |
| --- | --- |
| Bàn máy tính | `ComputerDesk`, `ComputerDesk2-11` |
| Màn hình | `ComputerMonitor1-6`, `Monitor_c`, `Monitor_d` |
| Bàn phím | `Keyboard1-13` |
| Chuột | `Mouse1-10`, `Mouse_b_1` |
| Lót chuột | `MousePad1-12` |
| Loa | `Pc_Speaker_c_Bass`, `Pc_Speaker_d_Left_Right` |
| Tay cầm | `gamepad_a_` |
| Micro | `Blue_Yeti`, `Green Mic`, `Black Mic`, `Old Mic`, `VintageMicrophone`, `LowMic1-2`, `Prefab_Microphone_a_` |
| Camera | `Camera1-10`, `Prefab_Camera_a_`, `camera_Holder_a` |
| Green screen | `Greenbox`, `Greenbox_Big`, `Greenbox_Stand` |
| Đèn, cách âm | `lamp_g_onair`, `Soundproof_a` |

## 6. Nội thất và trang trí nhà

| Loại | Prefab |
| --- | --- |
| Ghế, sofa | `Chair`, `Chair2-16`, `DecorChair1-5`, `DecorSofa1-7`, `Barcelona*`, `MrChair`, `MrChaiseLounge` |
| Bàn | `CoffeeTable1-11` |
| Giường | `Bed`, `Bed1-5` |
| Tủ, kệ | `Cabinet1-5`, `Shelf1-2`, `WallShelf1-12` |
| Tủ lạnh | `Fridge1-3` |
| Đèn | `Lamp`, `Lamp 2-11` |
| Tranh | `Painting2-32`, `Victorian Painting`, `FrogGiftPainting` |
| Phòng tắm | `Toilet`, `Toilet1-5`, `RR_Toilet_01`, `RR_ToiletPaperDispenser`, `Bathtub1/3`, `BathTubAngle4`, `BathTub5Jacuzzi`, `Jacuzzi02_vray`, `InflatablePool1-4`, `PlasticWashBasin*` |
| Tượng | `Statue`, `Angel Statue`, `Girl Statue`, `Koala Statue`, `Monkey Statue`, `The Thinker Statue`, `Venus`, `Abstract *`, `Hand Sculptures` |
| Thú nhồi bông | `Plush_*` (khoảng 20 con, mỗi con có bản `_0`, `_1`) |
| Khác | `Globe`, `Clock`, `OldRadio`, `Plant`, `Skull`, `The Cube`, `The Mask`, `Mop` |

## 7. Đồ ăn và vật phẩm

- Đồ ăn: `FoodBanana`, `FoodBurgerBox`, `FoodCan`, `FoodChips`, `FoodCup`, `FoodDonut`, `FoodFrenchFriesBox`, `FoodPancake`, `FoodPizza`, `FoodWrap`.
- Vật phẩm khác: `BasketFoodObject`, `BasketItem`, `SpawnedMedicine`, `CargoBox`, `ScratchCard`, `Toilet Paper`.

## 8. Minigame và casino

- Blackjack: 52 lá bài `Card_<Clover|Diamond|Spades|...>_R_<giá trị>` (cần đủ 4 chất).
- Máy: `Slot_Machine`, `Lucky_Spin_Machine_2_1`, `Lucky_Spin_Machine_2_1_V`.
- Nhảy: `DanceKey`, `DanceKeyLong`.
- Khác: `raffleparticipant`, `DEER BAR`, `TaterGrower`, `Levitron`, `CreatorAward1000/10000/100000`.

## 9. Môi trường và thế giới

| Loại | Prefab |
| --- | --- |
| Đường | `Primary *`, `Secondary *`, `Tertiary *`, `Motorway_*`, `MotorwayEntrance/Exit`, `X Crossing RA *`, `Road_Marker_Prefab`, `Secondary - Train Rail Prefab` |
| Công trình | `bridge01Pilar`, `bridgeSimplePrefab`, `1x2_Viaduct_Pillar`, `concreteTunnel`, `rockTunnelv2_terrain`, `guardRailv3Prefab`, `concreteBarrierWide_Hi`, `viking_fence_*`, `bootcamp_fence_*`, `fence_*`, `powerline_wood_*`, `lamppost_motorway`, `trainConcrete`, `steppedWall`, `sharpTurnSign*` |
| Thực vật (bộ `SM_*`) | `RedWood`, `Spruce`, `Fern`, `Cattail`, `FireWeed`, `BalsamRoot`, `Bush2`, `GroundCover`, `Grass_04-07`, `Palm`, `AlderBig/Small`, `A Leaf` |
| Địa hình | `rocks_prefab`, `Volcanic Rock`, `Countryside`, `Mountain_Barrier_Block_Prefab` |
| Điểm đánh dấu | `IndicatorPrefab_*`: Apartment, BeachVilla, CozyHouse, SuburbanHome, Clinic, Club, CoffeeShopDoor, Hairdresser, PawnShop, CarGallery, CarParking, Cloth, DeliveryPoint, AntMiner, Bicycle, Fork |

Dữ liệu đi kèm scene: `TerrainData`, `TerrainLayer`, `NavMeshData`, `OcclusionCullingData`, `Cubemap`.

## 10. Prefab UI và VFX

- UI spawn lúc chạy: `ChatMessage(TMP)`, `ChatPanelSample`, `Donate`, `FakegramPost`, `BillUI`, `Mail`, `AlbumPhoto`, `StreamHistory`, `DiscatUser`, `SpawningDcMessages`, `SpawningDcPlayer`, `ZamazorOrder`, `UploadedVideos`, `DetectedMinerUI`, `WallPaintSample`, `BottomPanel`, `InventoryItem`, `dopamineinfotext`.
- Prefab `DebugUI*` thuộc Unity Debug UI, có thể bỏ.
- VFX: `BuildParticle`, `BoxOpenedVFX`, `CleaningFlares`, `BulletImpact`, `TameParticle`, `HorizontalShine1`.

## Bộ tối thiểu để chạy được

| Việc cần có | Nhóm |
| --- | --- |
| Người chơi và camera | 1 |
| `NpcCivil`, `Courier` | 2 |
| 1 xe | 3 |
| Mỗi loại linh kiện PC 1 cái | 4 |
| Bàn, màn hình, bàn phím, chuột | 5 |
| 1 giường, 1 toilet, 1 tủ lạnh | 6 |
| Đường, cây, terrain | 9 |
| Các prefab UI spawn lúc chạy | 10 |

Phần còn lại (quần áo mở rộng, trang trí, minigame, NPC thoại) là nội dung mở rộng và có thể thêm dần.

## Tài liệu liên quan

- `StreamerLifeSim2_UI_Flow.md`: các loại UI và luồng chạy.
