# Hệ thống âm thanh

> Cập nhật: 2026-10-21

Mọi âm thanh phát qua `AudioManager` theo **mã định danh** (id) khai báo trong `Assets/Resources/AudioCatalog.asset`. Không tạo `AudioSource` riêng trong script gameplay.

## Cách gọi

| Mục đích | Gọi | Ghi chú |
| --- | --- | --- |
| SFX 2D (ăn, nhặt đồ, tiền) | `AudioManager.Play("eat_chew")` | Có thể truyền `pitch` |
| SFX 3D tại vật thể (cửa, tủ lạnh) | `AudioManager.PlayAt("door_open", transform.position)` | Xa hơn 18 m thì không phát |
| Âm UI | `AudioManager.PlayUi("ui_click")` | Vẫn nghe khi Pause |
| Nhạc theo ngữ cảnh | `AudioManager.Instance.PlayMusic("bgm_chill")` | Tự fade, nhạc Spoti được ưu tiên hơn |
| Nhạc người chơi chọn | `PlayPlayerMusic(id)` / `StopPlayerMusic()` | Dùng cho app Spoti |
| Âm môi trường | `PlayAmbience("amb_room")` / `StopAmbience()` | Tự fade |
| Tạm dừng âm gameplay | `SetGameplayPaused(true)` | Pause menu đã gọi sẵn |

Mã sai hoặc chưa có trong catalog sẽ báo một lần trong Console: `Không tìm thấy âm thanh '...'`.

## Âm tự động (không cần gọi)

`GameAudioDirector` (tự gắn cùng `AudioManager`) lo phần này:

| Sự kiện | Âm |
| --- | --- |
| Scene Menu | Nhạc `bgm_menu_theme` |
| Trong nhà / ra ngoài phố (đi qua cửa) | `bgm_home_theme` + `amb_room` / `bgm_downtown` + `amb_street` |
| Mở stream theo chủ đề | `stream_start`, nhạc: Just Chatting `bgm_chill`, Gaming `bgm_cyberpunk`, ASMR `bgm_asmr`, Dance `bgm_nightclub`, Pool `bgm_downtown`; âm `amb_pc_fan` |
| Donate | `donate_notify` + một âm meme ngẫu nhiên |
| Tiền giảm (mua, trả hóa đơn) | `cash_beep` |
| Hóa đơn mới, giao hàng, bị bệnh | `ui_notification`, `door_bell`, `ui_alert` |
| Nhận điểm kỹ năng, học kỹ năng | `level_up`, `skill_unlock` |
| Thẻ stream thành công / thất bại | `ui_success` / `ui_error` |
| Mở/đóng panel, bấm nút trong panel | `ui_popup_open/close`, `ui_click` (trong `UIPanel`) |
| Bước chân, nhảy | `FootstepAudio` tự gắn vào người chơi, chọn gỗ / bê tông / cỏ theo tên vật thể dưới chân |

## Tối ưu hiệu năng

- **Pool dùng lại:** 8 kênh 2D, 10 kênh 3D, 4 kênh UI tạo sẵn một lần; hết kênh thì lấy kênh phát lâu nhất. Không tạo GameObject khi phát.
- **Chống dồn tiếng:** cùng một âm không phát lại trong 0,05 giây và tối đa 3 lần chồng nhau.
- **Âm lượng qua mixer** (`Assets/Audio/AudioMixer.mixer`, nhóm BGM / SFX / UI), không nhân thêm lần nữa vào nguồn phát.
- **Import tự động theo thư mục** (`Editor/AudioImportRules.cs`):

| Thư mục | Load Type | Nén | Khác |
| --- | --- | --- | --- |
| `Audio/BGM` | Streaming | Vorbis 60% | Không preload |
| `Audio/Ambience` | Compressed In Memory | Vorbis 50% | Mono, preload |
| `Audio/SFX` | Decompress On Load | ADPCM | Mono, preload để phát không khựng |

Đo trong Play mode: 131 clip đang nạp tốn **3,43 MB** RAM; file nhạc mp3 4 MB khi stream chỉ giữ khoảng 193 KB.

## Thêm âm mới

1. Đặt file vào đúng thư mục `Assets/Audio/BGM|Ambience|SFX/...`, cấu hình import tự áp dụng.
2. Thêm vào catalog: âm tự tổng hợp thì thêm công thức trong `PrototypeAudioBuilder` rồi chạy **Tools → Prototype → Build Audio Library**; file có sẵn thì thêm vào `ExistingClips` rồi chạy **Tools → Audio → Sync Existing Clips To Catalog**.
3. Đổi cấu hình import hàng loạt: **Tools → Audio → Reapply Import Settings**.
