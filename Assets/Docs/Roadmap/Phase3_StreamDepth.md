# Giai đoạn 3 — Chiều sâu stream

> Cập nhật: 2026-10-21 (Hoàn thành & Đã test Play Mode)

Mục tiêu: mỗi buổi stream có lựa chọn và cách chơi riêng theo chủ đề, người chơi lên kỹ năng để mở khóa hiệu ứng mới. Xong giai đoạn này là đủ bộ tối thiểu để game chơi được trọn vòng.

## Cơ chế

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| A5 | Thẻ hành động khi stream, thành công hoặc thất bại theo xác suất | Xong | `StreamCardData`, `StreamCardDeck`, `StreamCardView` |
| A6 | Chọn chủ đề: Just Chatting, Gaming, ASMR, Dance, Pool | Xong | `StreamCategory`, `StreamStartPanel` |
| B1 | Minigame Just Chatting (trả lời câu hỏi) | Xong | `JustChattingGame`, `JustChattingQuestion` |
| B2 | Minigame ASMR (5–9 slider) | Xong | `AsmrGame` |
| B3 | Minigame Dance (bấm phím theo nhịp) | Xong | `DanceGame` |
| A8 | Chat tự sinh theo số viewer, chat độc hại, cấm người xem | Xong | `ChatGenerator`, `ChatModeration`, `ChatView` |
| A9 | Lịch sử stream (Youhube) | Xong | `StreamRecord`, `YouhubeApp` |
| H1 | 22 kỹ năng, điểm kỹ năng | Xong | `SkillData`, `SkillSystem`, `SkillTreeView` |
| H2 | Hướng dẫn người chơi mới theo chặng | Xong | `TutorialStepData`, `TutorialSystem`, `TutorialView` |

## Việc cần làm

- [x] Bộ thẻ hành động: dữ liệu thẻ, xác suất, hiệu ứng lên viewer và mood.
- [x] Mỗi chủ đề mở một minigame; kết quả gọi `StreamSystem.UpdateViewerPercent`.
- [x] Minigame Just Chatting, ASMR, Dance (UI tương tác trong `StreamCardView` / `StreamOverlayView`).
- [x] Bộ sinh chat: tốc độ theo viewer, tin nhắn theo chủ đề, tin độc hại có nút cấm.
- [x] Hệ thống kỹ năng: nhận điểm theo tiến trình, bảng kỹ năng (phím K), bonus theo chủ đề.
- [x] App Youhube: danh sách buổi stream đã lưu.
- [x] Tutorial theo chặng: di chuyển → mở stream → ăn, ngủ → mua linh kiện.

## Hoàn thành khi

- Một người chơi mới làm theo tutorial và tự chơi được 3 ngày trong game mà không cần giải thích thêm.
- Mỗi chủ đề stream cho cảm giác chơi khác nhau, kỹ năng đã học thay đổi kết quả thấy được.
