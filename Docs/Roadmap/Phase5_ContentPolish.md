# Giai đoạn 5 — Nội dung và hoàn thiện

> Cập nhật: 2026-10-20

Mục tiêu: thêm nội dung giải trí, thay toàn bộ asset prototype bằng asset thật và chuẩn bị phát hành.

## Casino và minigame

| Mã | Cơ chế |
| --- | --- |
| E1 | Blackjack (cược 10–5.000) |
| E2, E3 | Slot web có bonus game, slot ở club |
| E4 | Vòng quay may mắn |
| E5 | Vé cào |
| E6, E8 | Xổ số, GTU (reset mỗi ngày) |
| E7 | Hòm skin |
| J1 | CamperStrike (bắn súng deathmatch) |
| J2 | Pogo (platformer có checkpoint) |
| J3 | BrainRot (clicker, lootbox) |

## Nội dung phụ

| Mã | Cơ chế |
| --- | --- |
| I5 | NPC có hội thoại và nhiệm vụ nhỏ (River, Mimi, Hobo…) |
| C7 | Ngoại hình: tiệm tóc, cửa hàng quần áo |
| D9 | Sơn tường, giấy dán tường |
| I8 | Truyện tranh sưu tầm |

## Hoàn thiện

- [ ] Thay model greybox bằng model thật (giữ prefab gốc, thay các object `Mesh_*`).
- [ ] Thay animation bằng bộ humanoid thật, giữ tên tham số Animator.
- [ ] Thay wireframe UI bằng thiết kế thật, giữ tên element UXML.
- [ ] Âm thanh theo `../AudioList.md`.
- [ ] Đa ngôn ngữ (H5), Settings và đổi phím đầy đủ (H6).
- [ ] Thành tích Steam (H3).
- [ ] Cân bằng lại toàn bộ số liệu tạm, tối ưu hiệu năng.

## Hoàn thành khi

- Không còn asset prototype nào trong bản build.
- Chơi liền 10 ngày trong game không lỗi, không có số liệu "giá trị tạm" còn sót trong code.
