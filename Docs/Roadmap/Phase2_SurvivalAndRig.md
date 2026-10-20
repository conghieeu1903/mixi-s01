# Giai đoạn 2 — Sinh tồn và dàn máy

> Cập nhật: 2026-10-20

Mục tiêu: người chơi phải ăn, ngủ, vệ sinh để giữ mood, và phải mua, lắp linh kiện để máy mạnh hơn thì stream mới tăng trưởng.

## Cơ chế

| Mã | Cơ chế                                                             | Trạng thái | Code hiện có                              |
| --- | -------------------------------------------------------------------- | ------------ | ------------------------------------------- |
| C1  | Thức khuya, đói, đi vệ sinh, tắm                               | Có code     | `Survival/SurvivalNeeds`                  |
| C2  | Mood 0–100, các mức trạng thái, debuff giảm viewer             | Có code     | `Survival/MoodSystem`                     |
| C3  | Bệnh và thuốc                                                     | Một phần   | `SurvivalNeeds.IsSick` (chưa có thuốc) |
| C4  | Stress                                                               | Một phần   | `SurvivalNeeds.Stress`                    |
| I2  | Cầm, đặt, xoay vật phẩm; chế độ build; kho đồ              | Chưa làm   |                                             |
| F1  | Lắp PC theo linh kiện, chất lượng máy → FPS → hệ số viewer | Chưa làm   |                                             |
| F2  | Router, tốc độ mạng                                              | Chưa làm   |                                             |
| F3  | Webcam, mic, đèn cộng chất lượng stream                        | Chưa làm   |                                             |
| F6  | Bàn, màn hình, ghế; gán PC vào bàn                            | Chưa làm   |                                             |
| D2  | Mua online (Zamazor), nhận hàng dạng thùng                       | Chưa làm   |                                             |

## Việc cần làm

- [ ] Đồ vật sinh tồn: tủ lạnh và đồ ăn (giảm đói), toilet (reset vệ sinh), bồn tắm (reset tắm), dùng prefab `Proto_Fridge`, `Proto_Toilet`, `Proto_Bathtub`.
- [ ] Hiện debuff trên HUD và điện thoại: thanh năng lượng, stress, icon debuff.
- [ ] Thuốc: mua và dùng để hết bệnh.
- [ ] Dữ liệu vật phẩm (ScriptableObject): tên, giá, số sao, loại, chất lượng, dung lượng.
- [ ] Cầm, thả, đặt, xoay vật phẩm; kho đồ (màn Inventory).
- [ ] Vỏ case có slot linh kiện: mainboard, CPU, tản nhiệt, RAM ×4, card ×2, nguồn, HDD (màn Lắp ráp PC).
- [ ] Tính chất lượng máy và FPS, đưa vào hệ số viewer của `StreamSystem`.
- [ ] Router: không có router thì không stream được; tốc độ mạng ảnh hưởng chất lượng.
- [ ] Thiết bị stream (webcam, mic, đèn) cộng điểm chất lượng.
- [ ] App Zamazor trên PC: giỏ hàng, đặt hàng, shipper giao thùng `Proto_CargoBox` tới cửa.

## Hoàn thành khi

- Bỏ ăn hoặc thức quá giới hạn thì mood giảm và viewer tăng chậm hơn thấy rõ.
- Mua card đồ họa trên Zamazor → nhận thùng → lắp vào case → số viewer mỗi đợt tăng so với trước.
- Đồ đã đặt và linh kiện đã lắp được lưu và tải lại đúng vị trí.
