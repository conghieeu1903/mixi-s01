# Giai đoạn 1 — Vòng lặp lõi

> Cập nhật: 2026-10-20

Mục tiêu: người chơi đi lại trong phòng, ngồi vào máy mở stream, thấy viewer và tiền tăng, rồi đi ngủ sang ngày mới. Phần lớn logic đã có, việc chính là nối vào scene và UI.

## Cơ chế

| Mã | Cơ chế | Trạng thái | Code hiện có |
| --- | --- | --- | --- |
| I1 | Di chuyển góc nhìn thứ nhất, nhìn | Có code | `Player/PlayerMotor`, `PlayerLook`, `PlayerInputReader` |
| I1 | Tương tác bằng raycast, gợi ý phím trên HUD | Chưa làm | |
| A1 | Đồng hồ ngày/giờ, sự kiện mỗi phút/giờ/ngày | Có code | `Core/GameClock` |
| A1 | Ngủ để tua tới sáng | Có code | `GameWorld` (ngủ tới 8h) |
| A2–A4 | Mở/đóng stream, viewer theo đợt 30 phút, follower, sub, giới hạn viewer theo ngày | Có code | `Streaming/StreamSystem` |
| A7 | Donate | Có code | `StreamSystem.Donated` |
| D1 | Tiền, hóa đơn tiền nhà mỗi ngày | Có code | `Economy/Economy` |
| H4 | Lưu/tải, tự lưu khi qua ngày | Có code | `Core/SaveService`, `SaveData`, `GameWorld` |

## Việc cần làm

- [ ] Dựng scene phòng streamer bằng prefab greybox: `Proto_Room`, `Proto_ComputerDesk`, `Proto_Monitor`, `Proto_GamingChair`, `Proto_Bed`, `Proto_Door`.
- [ ] Hệ thống tương tác: interface `IInteractable`, raycast từ camera, hiện gợi ý phím (khung `interactionHints` trên HUD).
- [ ] Bàn máy: tương tác thì mở panel chọn chủ đề stream, gọi `StreamSystem.Open` / `Close`.
- [ ] Giường: tương tác thì gọi hàm ngủ của `GameWorld`, có màn Fader khi tua giờ.
- [ ] Nối HUD (UI Toolkit) với dữ liệu thật: giờ, ngày, tiền, badge hóa đơn.
- [ ] Nối Stream overlay: viewer, follower, thời lượng, thu nhập, popup donate.
- [ ] Pause bằng ESC (dừng đồng hồ), Settings tối thiểu (âm lượng, độ nhạy chuột).
- [ ] Main Menu: Continue tải save, New game tạo save mới, qua màn Loading.
- [ ] Panel hóa đơn: xem và trả hóa đơn tiền nhà.

## Hoàn thành khi

- Chơi liền một vòng: vào game → stream vài giờ → nhận tiền và donate → trả hóa đơn → ngủ → sang ngày 2, không lỗi Console.
- Thoát ra Main Menu rồi Continue thì khôi phục đúng ngày, giờ, tiền, follower, sub.
