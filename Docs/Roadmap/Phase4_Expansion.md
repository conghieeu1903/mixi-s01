# Giai đoạn 4 — Mở rộng hệ thống

> Cập nhật: 2026-10-21 (Hoàn thành & Đã test Play Mode)

Mục tiêu: thêm nguồn thu phụ, app trên máy tính và thế giới bên ngoài nhà để người chơi có việc làm ngoài stream. Các nhóm dưới đây độc lập, có thể làm theo thứ tự tùy ưu tiên.

## Kinh tế phụ

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| D3 | Cầm đồ (Bán được 60% giá gốc) | Xong | `PawnShop` |
| D4 | Đào Bitcoin bằng rig (Cần router; mỗi 2 giây cộng theo `miningPower`) | Xong | `CryptoMining`, `MiningRig` |
| D5 | Việc làm thêm qua thư (Thù lao 20–29 hoặc 500–799 tùy loại) | Xong | `MailJobs`, `BrowserApp` |
| D6, D7 | Thuê nhân viên stream, trả lương theo ngày (7 nhân viên, năng lượng, đói, follower riêng) | Xong | `StreamWorker`, `WorkerManager` |
| D8 | Mua, thuê nhà; mua xe | Xong | `PropertyMarket`, `VehicleDealer` |

## Máy tính và mạng xã hội

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| F4 | Tiền điện theo số thiết bị đang chạy | Xong | `ElectricitySystem` |
| F5 | Virus, Blue Screen, app diệt virus | Xong | `VirusSystem`, `BrowserApp` |
| G1, G2 | Discat: chat với fan, hater, hacker, bot, có hội thoại nhiều nhánh | Xong | `DiscatApp`, `DiscatContact`, `DiscatContactType` |
| G3, G4 | Fakegram, Spoti, trình duyệt | Xong | `FakegramApp`, `SpotiApp`, `BrowserApp` |

## Sinh hoạt và điện thoại

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| C5 | Dọn nhà, độ sạch ảnh hưởng mood | Xong | `CleaningSystem`, `Dirt`, `Mop` |
| C6 | Gọi đồ ăn qua app, shipper giao tận cửa | Xong | `FoodDeliveryApp`, `OrderService`, `Courier` |
| — | Điện thoại và các app: Đồ ăn, Quần áo, Thú cưng, Xe, Camera, Album, Skill, Bản đồ | Xong | `PhoneController`, `PhoneApp`, `FoodDeliveryApp`, `PhoneCameraApp`, `PhoneMapApp` |

## Thế giới bên ngoài

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| I3 | Lái ô tô, xe máy, xe đạp | Xong | `VehicleController` |
| I4 | NPC dân thường, xe NPC, cảnh sát | Xong | `NpcCivilian`, `NpcSpawner` |
| I6 | Thú cưng, có ảnh hưởng viewer | Xong | `Pet`, `PetSystem` |
| I7 | Bản đồ, điểm đánh dấu địa điểm | Xong | `MapMarker`, `MapSystem`, `Door` |

## Hoàn thành khi

- [x] Có ít nhất 2 nguồn thu ngoài stream chạy được và cân bằng với tiền stream (`PawnShop`, `CryptoMining`, `MailJobs`, `WorkerManager`).
- [x] Người chơi ra khỏi nhà, tới được ít nhất 3 địa điểm trên bản đồ và quay về (`Tiệm Cầm Đồ`, `Showroom Xe`, `Tiệm Thời Trang & Cắt Tóc`, `Nhà Streamer`).
