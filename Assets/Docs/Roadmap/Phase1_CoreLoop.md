# Giai đoạn 1 — Vòng lặp lõi

> Cập nhật: 2026-10-20

Mục tiêu: người chơi đi lại trong phòng, ngồi vào máy mở stream, thấy viewer và tiền tăng, rồi đi ngủ sang ngày mới. Phần lớn logic đã có, việc chính là nối vào scene và UI.

## Cơ chế

| Mã    | Cơ chế                                                                                   | Trạng thái | Code hiện có                                                |
| ------ | ------------------------------------------------------------------------------------------ | ------------ | ------------------------------------------------------------- |
| I1     | Di chuyển góc nhìn thứ nhất, nhìn                                                    | Đã xong     | `Player/PlayerMotor`, `PlayerLook`, `PlayerInputReader` |
| I1     | Tương tác bằng raycast, gợi ý phím trên HUD                                        | Đã xong     | `Interaction/IInteractable`, `PlayerInteractor`, `UI/HudView` |
| A1     | Đồng hồ ngày/giờ, sự kiện mỗi phút/giờ/ngày                                     | Đã xong     | `Core/GameClock`                                            |
| A1     | Ngủ để tua tới sáng                                                                   | Đã xong     | `GameWorld`, `World/Bed`, `UI/ScreenFader`                  |
| A2–A4 | Mở/đóng stream, viewer theo đợt 30 phút, follower, sub, giới hạn viewer theo ngày | Đã xong     | `Streaming/StreamSystem`, `World/ComputerDesk`, `UI/StreamStartPanel`, `UI/StreamOverlayView` |
| A7     | Donate                                                                                     | Đã xong     | `StreamSystem.Donated`, `UI/StreamOverlayView`, `UI/HudView` |
| D1     | Tiền, hóa đơn tiền nhà mỗi ngày                                                    | Đã xong     | `Economy/Economy`, `UI/BillPanel`, `UI/HudView`             |
| H4     | Lưu/tải, tự lưu khi qua ngày                                                          | Đã xong     | `Core/SaveService`, `SaveData`, `GameWorld`, `UI/MainMenu`  |

## Việc cần làm

- [x] Dựng scene phòng streamer bằng prefab greybox: `Proto_Room`, `Proto_ComputerDesk`, `Proto_Monitor`, `Proto_GamingChair`, `Proto_Bed`, `Proto_Door`.
- [x] Hệ thống tương tác: interface `IInteractable`, raycast từ camera, hiện gợi ý phím (khung `interactionHints` trên HUD).
- [x] Bàn máy: tương tác thì mở panel chọn chủ đề stream, gọi `StreamSystem.Open` / `Close`.
- [x] Giường: tương tác thì gọi hàm ngủ của `GameWorld`, có màn Fader khi tua giờ.
- [x] Nối HUD (UI Toolkit) với dữ liệu thật: giờ, ngày, tiền, badge hóa đơn.
- [x] Nối Stream overlay: viewer, follower, thời lượng, thu nhập, popup donate.
- [x] Pause bằng ESC (dừng đồng hồ), Settings tối thiểu (âm lượng, độ nhạy chuột).
- [x] Main Menu: Continue tải save, New game tạo save mới, qua màn Loading.
- [x] Panel hóa đơn: xem và trả hóa đơn tiền nhà.

## Hoàn thành khi

- Chơi liền một vòng: vào game → stream vài giờ → nhận tiền và donate → trả hóa đơn → ngủ → sang ngày 2, không lỗi Console.
- Thoát ra Main Menu rồi Continue thì khôi phục đúng ngày, giờ, tiền, follower, sub.
