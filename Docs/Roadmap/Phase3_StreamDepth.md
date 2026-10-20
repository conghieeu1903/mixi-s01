# Giai đoạn 3 — Chiều sâu stream

> Cập nhật: 2026-10-20

Mục tiêu: mỗi buổi stream có lựa chọn và cách chơi riêng theo chủ đề, người chơi lên kỹ năng để mở khóa hiệu ứng mới. Xong giai đoạn này là đủ bộ tối thiểu để game chơi được trọn vòng.

## Cơ chế

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| A5 | Thẻ hành động khi stream, thành công hoặc thất bại theo xác suất | Chưa làm | |
| A6 | Chọn chủ đề: Just Chatting, Gaming, ASMR, Dance, Pool | Một phần | enum `StreamCategory` |
| B1 | Minigame Just Chatting (trả lời câu hỏi) | Chưa làm | |
| B2 | Minigame ASMR (5–9 slider) | Chưa làm | |
| B3 | Minigame Dance (bấm phím theo nhịp) | Chưa làm | |
| A8 | Chat tự sinh theo số viewer, chat độc hại, cấm người xem | Chưa làm | |
| A9 | Lịch sử stream (Youhube) | Một phần | `StreamRecord` |
| H1 | 22 kỹ năng, điểm kỹ năng | Chưa làm | `StreamSystem.SkillViewerBonus` đã chừa chỗ |
| H2 | Hướng dẫn người chơi mới theo chặng | Chưa làm | |

## Việc cần làm

- [ ] Bộ thẻ hành động: dữ liệu thẻ, xác suất, hiệu ứng lên viewer và mood.
- [ ] Mỗi chủ đề mở một minigame; kết quả gọi `StreamSystem.UpdateViewerPercent`.
- [ ] Minigame Just Chatting, ASMR, Dance (UI theo wireframe `Minigames`).
- [ ] Bộ sinh chat: tốc độ theo viewer, tin nhắn theo chủ đề, tin độc hại có nút cấm.
- [ ] Hệ thống kỹ năng: nhận điểm theo tiến trình, app Skill trên điện thoại, bonus theo chủ đề.
- [ ] App Youhube: danh sách buổi stream đã lưu.
- [ ] Tutorial theo chặng: di chuyển → mở stream → ăn, ngủ → mua linh kiện.

## Hoàn thành khi

- Một người chơi mới làm theo tutorial và tự chơi được 3 ngày trong game mà không cần giải thích thêm.
- Mỗi chủ đề stream cho cảm giác chơi khác nhau, kỹ năng đã học thay đổi kết quả thấy được.
