# Giai đoạn 5 — Nội dung và hoàn thiện

> Cập nhật: 2026-10-21 (Hoàn thành & Đã test mô phỏng 10 ngày Play Mode)

Mục tiêu: thêm nội dung giải trí, hoàn thiện toàn bộ các hệ thống phụ, đa ngôn ngữ, đổi phím, thành tích và kiểm thử liên tục 10 ngày.

## Casino và minigame

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| E1 | Blackjack (cược 10–5.000) | Xong | `BlackjackGame` |
| E2, E3 | Slot web có bonus game, slot ở club | Xong | `CasinoSlotMachine` |
| E4 | Vòng quay may mắn | Xong | `LuckySpin` |
| E5 | Vé cào | Xong | `ScratchCard` |
| E6, E8 | Xổ số, GTU (reset mỗi ngày) | Xong | `Raffle`, `GtuLottery` |
| E7 | Hòm skin | Xong | `SkinCase` |
| J1 | CamperStrike (bắn súng deathmatch) | Xong | `CamperStrike` |
| J2 | Pogo (platformer có checkpoint) | Xong | `PogoGame` |
| J3 | BrainRot (clicker, lootbox) | Xong | `BrainRotClicker` |

## Nội dung phụ

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| I5 | NPC có hội thoại và nhiệm vụ nhỏ (River, Mimi, Hobo…) | Xong | `DialogueData`, `DialogueSystem`, `NpcTalker`, `DialogueView` |
| C7 | Ngoại hình: tiệm tóc, cửa hàng quần áo | Xong | `AppearanceData`, `AppearanceSystem`, `BarberShop`, `ClothingStore` |
| D9 | Sơn tường, giấy dán tường | Xong | `WallPainter` |
| I8 | Truyện tranh sưu tầm | Xong | `ComicBook` |

## Hoàn thiện

- [x] Đa ngôn ngữ (H5), Settings và đổi phím đầy đủ (H6) (`LocalizationService`, `GameSettings`, `InputRebinding`).
- [x] Thành tích Steam (H3) (`Achievements`).
- [x] Cân bằng lại toàn bộ số liệu tạm, xóa các ghi chú "giá trị tạm" trong code.
- [x] Kiểm thử mô phỏng chơi liên tục 10 ngày (`Day 1 -> Day 11`) không có lỗi Console.

## Hoàn thành khi

- Chơi liền 10 ngày trong game không lỗi, không có số liệu "giá trị tạm" còn sót trong code.
