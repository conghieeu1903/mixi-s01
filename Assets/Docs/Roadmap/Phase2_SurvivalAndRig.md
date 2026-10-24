# Giai đoạn 2 — Sinh tồn và dàn máy

> Cập nhật: 2026-10-20

Mục tiêu: người chơi phải ăn, ngủ, vệ sinh để giữ mood, và phải mua, lắp linh kiện để máy mạnh hơn thì stream mới tăng trưởng.

## Cơ chế

| Mã | Cơ chế                                                             | Trạng thái | Code hiện có                              |
| --- | -------------------------------------------------------------------- | ------------ | ------------------------------------------- |
| C1  | Thức khuya, đói, đi vệ sinh, tắm                               | Đã xong     | `Survival/SurvivalNeeds`, `Fridge`, `Toilet`, `Bathtub` |
| C2  | Mood 0–100, các mức trạng thái, debuff giảm viewer             | Đã xong     | `Survival/MoodSystem`, `UI/NeedsView`       |
| C3  | Bệnh và thuốc                                                     | Đã xong     | `SurvivalNeeds.IsSick`, `Items/MedicineData` |
| C4  | Stress                                                               | Đã xong     | `SurvivalNeeds.Stress`, `UI/NeedsView`      |
| I2  | Cầm, đặt, xoay vật phẩm; chế độ build; kho đồ              | Đã xong     | `Items/WorldItem`, `ItemCarrier`, `BuildPlacer`, `Inventory`, `UI/InventoryView` |
| F1  | Lắp PC theo linh kiện, chất lượng máy → FPS → hệ số viewer | Đã xong     | `Rig/PcCase`, `PcSlot`, `PcPartData`, `RigPerformance`, `UI/PcBuildView` |
| F2  | Router, tốc độ mạng                                              | Đã xong     | `Rig/Router`, `RouterData`                  |
| F3  | Webcam, mic, đèn cộng chất lượng stream                        | Đã xong     | `Rig/StreamGear`, `StreamSetup`             |
| F6  | Bàn, màn hình, ghế; gán PC vào bàn                            | Đã xong     | `World/ComputerDesk`, `Rig/StreamSetup`     |
| D2  | Mua online (Zamazor), nhận hàng dạng thùng                       | Đã xong     | `Shop/ZamazorApp`, `OrderService`, `Courier`, `CargoBox`, `Computer/DesktopOS` |

## Việc cần làm

- [x] Đồ vật sinh tồn: tủ lạnh và đồ ăn (giảm đói), toilet (reset vệ sinh), bồn tắm (reset tắm), dùng prefab `Proto_Fridge`, `Proto_Toilet`, `Proto_Bathtub`.
- [x] Hiện debuff trên HUD và điện thoại: thanh năng lượng, stress, icon debuff.
- [x] Thuốc: mua và dùng để hết bệnh.
- [x] Dữ liệu vật phẩm (ScriptableObject): tên, giá, số sao, loại, chất lượng, dung lượng.
- [x] Cầm, thả, đặt, xoay vật phẩm; kho đồ (màn Inventory).
- [x] Vỏ case có slot linh kiện: mainboard, CPU, tản nhiệt, RAM ×4, card ×2, nguồn, HDD (màn Lắp ráp PC).
- [x] Tính chất lượng máy và FPS, đưa vào hệ số viewer của `StreamSystem`.
- [x] Router: không có router thì không stream được; tốc độ mạng ảnh hưởng chất lượng.
- [x] Thiết bị stream (webcam, mic, đèn) cộng điểm chất lượng.
- [x] App Zamazor trên PC: giỏ hàng, đặt hàng, shipper giao thùng `Proto_CargoBox` tới cửa.

## Hoàn thành khi

- Bỏ ăn hoặc thức quá giới hạn thì mood giảm và viewer tăng chậm hơn thấy rõ.
- Mua card đồ họa trên Zamazor → nhận thùng → lắp vào case → số viewer mỗi đợt tăng so với trước.
- Đồ đã đặt và linh kiện đã lắp được lưu và tải lại đúng vị trí.
