# Streamer Life Simulator 2 — Danh sách package được dùng

> Cập nhật: 2026-10-20

## Tổng quan

Tài liệu này gồm hai phần tách biệt:

1. **Game gốc (SLS2)**: package/asset mà bản decompile dùng. Lấy từ `SLS2/Plugins` (các file `.dll`) và các lệnh `using` trong `Assembly-CSharp`. Đây là thứ cần để dựng lại game.
2. **Project hiện tại (`project/`)**: package đã cài trong `Packages/manifest.json`. Unity `6000.6.6f1`, render pipeline URP.

Giới hạn: danh sách game gốc suy ra từ tên DLL và namespace, không có phiên bản. Chưa kiểm tra asset nào là bản Asset Store trả phí hay license riêng.

## Điểm cần chú ý trước khi dựng lại

| Vấn đề | Chi tiết |
| --- | --- |
| **HDRP và URP** | Game gốc dùng HDRP (`Unity.RenderPipelines.HighDefinition.Runtime`, `using UnityEngine.Rendering.HighDefinition` trong `AVP_DofFocus`, `BuyCar` và vài file khác). Project hiện tại cài URP 17.6.0. Code và shader HDRP không chạy trên URP, cần chọn một pipeline. |
| **Steamworks** | 23 file dùng `Steamworks` (`SteamManager`, ngôn ngữ từ Steam API trong `MenuManager`). Cần `com.rlabrecque.steamworks.net` hoặc bỏ phần Steam. |
| **Vuplex WebView** | Trình duyệt trong game (`BrowserManager`, `BrowserPanel`) phụ thuộc asset trả phí này. Nếu không có thì phải thay hoặc bỏ. |
| **Asset trả phí** | Easy Save 3, Vuplex, Advanced Culling System, EasyRoads3D, MeshFusion Pro, PolyFew, Paint in 3D, Final IK/FIMSpace, Scratch Card, HUD Navigation System. Cần license riêng. |

## 1. Package Unity (project hiện tại)

Từ `Packages/manifest.json`:

| Package | Phiên bản | Mục đích |
| --- | --- | --- |
| `com.unity.render-pipelines.universal` | 17.6.0 | URP |
| `com.unity.ugui` | 2.6.0 | uGUI (Canvas, Text, Image) |
| `com.unity.inputsystem` | 1.20.0 | Input System |
| `com.unity.timeline` | 6.6.0 | Timeline |
| `com.unity.visualscripting` | 1.9.12 | Visual Scripting |
| `com.unity.test-framework` | 1.8.0 | Test |
| `com.unity.multiplayer.center` | 2.0.1 | Multiplayer Center (mặc định của template) |
| `com.unity.collab-proxy` | 2.13.6 | Version Control (Plastic SCM) |
| `com.unity.ide.visualstudio` | 2.0.26 | Hỗ trợ Visual Studio |

Gồm cả các `com.unity.modules.*` (physics, animation, audio, AI/NavMesh, cloth, terrain, video, unitywebrequest, particlesystem, vehicles, v.v.). Chúng là module lõi của engine, không cần cài riêng.

## 2. Package từ Git và asset trong project hiện tại

| Package | Nguồn | Vai trò |
| --- | --- | --- |
| Unity MCP plugin | `https://github.com/AnkleBreaker-Studio/unity-mcp-plugin.git` | Cho AI điều khiển Unity Editor (các tool `unity_*`) |
| Antigravity Unity | `https://github.com/usmanbutt-dev/antigravity-unity.git` | Tích hợp Antigravity |
| DOTween | `Assets/Plugins/Demigiant/DOTween` | Tween animation, đã có trong project |
| TextMesh Pro | `Assets/TextMesh Pro` | Chữ UI |
| Unity In-App Purchase | theo `Docs/package.md` | Mua trong app (chưa thấy trong `manifest.json`, cần kiểm tra lại) |

## 3. Package Unity mà game gốc dùng (chưa có trong project)

Từ `SLS2/Plugins`:

| Package / DLL | Dùng để làm gì | Ghi chú |
| --- | --- | --- |
| `Unity.RenderPipelines.HighDefinition.*` | Render HDRP | Xung đột với URP |
| `Unity.VisualEffectGraph.Runtime` | VFX Graph | Hiệu ứng hạt |
| `Unity.Postprocessing.Runtime` | Post Processing v2 | Cũ, bản HDRP/URP có Volume riêng |
| `Cinemachine` | Camera | |
| `Unity.AI.Navigation` | NavMesh cho NPC | |
| `Unity.Animation.Rigging` | Animation Rigging, IK | |
| `Unity.Splines` | Spline (5 file dùng) | |
| `Unity.Timeline` | Timeline | |
| `Unity.TextMeshPro` | Chữ UI | |
| `Unity.InputSystem` | Input mới | |
| `Unity.Burst`, `Unity.Collections`, `Unity.Mathematics` | Hiệu năng/toán | |
| `Unity.MemoryProfiler` | Profiling | Có thể bỏ |
| `Unity.2D.*` (Animation, IK, SpriteShape, Tilemap.Extras, PixelPerfect) | Gói 2D | Có khả năng chỉ là phụ thuộc mặc định |
| `Unity.VisualScripting.*` | Visual Scripting | Phụ thuộc mặc định |
| `Unity.Services.Core`, `Authentication`, `RemoteConfig`, `Core.Analytics` | Unity Gaming Services | |
| `UnityEngine.UI` | uGUI | |

## 4. Thư viện bên thứ ba (game gốc)

| Thư viện | DLL / namespace | Dùng trong |
| --- | --- | --- |
| DOTween + DOTween Pro | `DG.Tweening` (62 file) | Animation UI (Inventory scale, Phone mở/đóng…) |
| Easy Save 3 | `ES3`, `ES3Types`, `ES3Internal` | Lưu game (`SaveManager`) |
| NaughtyAttributes | `NaughtyAttributes` (98 file) | Attribute trong Inspector |
| Newtonsoft.Json | `Newtonsoft.Json` | JSON (dịch, web) |
| Steamworks.NET | `com.rlabrecque.steamworks.net` | Steam (`SteamManager`) |
| Vuplex WebView | `Vuplex.WebView` | Trình duyệt trong game |
| Michsky LSS | `Michsky.LSS` | Loading Screen (chuyển scene) |
| Michsky UI | thư mục `Michsky` | UI hiện đại |
| Epo Outline | `EPOOutline` | Viền highlight vật phẩm (`Item.myOutlinable`) |
| Tayx Graphy | `Tayx.Graphy` | Hiển thị FPS, debug |

## 5. Asset gameplay / hệ thống (game gốc)

| Asset | Namespace | Dùng cho |
| --- | --- | --- |
| FIMSpace (Ragdoll Animator, Look Animator, Animation Tools…) | `FIMSpace.*` (hơn 60 file) | Ragdoll, animation thủ tục, nhìn theo |
| EFPController | `EFPController` (24 file) | Điều khiển người chơi góc nhìn thứ nhất |
| PG (Pro Game / car) | `PG` | Điều khiển xe (`PG86_*`, `S34_*`) |
| OmniVehicleAi | `OmniVehicleAi` | Xe NPC chạy AI |
| Character Creator (CC) | `CC` | Tạo nhân vật |
| EasyRoads3D | `EasyRoads3Dv3` | Tạo đường |
| Advanced Culling System (NGS) | `NGS.AdvancedCullingSystem` | Culling (`OptimizeManager`) |
| MeshFusion Pro | `MeshFusionPro.Runtime` | Gộp mesh |
| PolyFew | `BrainFailProductions.PolyFew*` | Giảm poly lúc chạy |
| UnityMeshSimplifier | `Whinarn.UnityMeshSimplifier` | Đơn giản mesh |
| Atmospheric Height Fog | `AtmosphericHeightFog` | Sương mù |
| Paint in 3D | `PaintIn3D*`, `CW.Common` | Vẽ lên tường (`WallPaintPanel`) |
| Scratch Card Asset | `ScratchCardAsset` | Minigame cào thẻ (`MyScratch`, `ScratchManager`) |
| HUD Navigation System | `SickscoreGames.HUDNavigationSystem` | Map, minimap, marker |
| Pogostick Controller | `RageRunGames.PogostickController` | Minigame Pogo |
| ithappy, RainbowArt, SojaExiles | thư mục cùng tên | Prefab/UI/cửa bổ trợ |
| DemiLib | `DemiLib` | Phụ thuộc của DOTween |

## Phân loại theo mức ưu tiên khi dựng lại

| Mức | Package |
| --- | --- |
| **Bắt buộc** | DOTween, TextMesh Pro, uGUI, Input System, Easy Save 3, NaughtyAttributes, Newtonsoft.Json, Michsky LSS, EPOOutline |
| **Hệ thống chính** | Pipeline render (chọn HDRP hoặc URP), EFPController, FIMSpace, PG và OmniVehicleAi (xe), NavMesh, Cinemachine, Animation Rigging, Character Creator |
| **Tính năng** | Vuplex (trình duyệt), Steamworks (Steam), Paint in 3D (vẽ tường), Scratch Card, HUD Navigation, Pogostick |
| **Tối ưu / công cụ** | Advanced Culling, MeshFusion, PolyFew, UnityMeshSimplifier, Graphy, Atmospheric Height Fog |
| **Có thể bỏ** | Memory Profiler, gói 2D, Visual Scripting, Unity Gaming Services (nếu không dùng cloud), DebugUI |

## Việc nên làm tiếp

1. Chốt pipeline render (HDRP hay URP) trước khi nhập asset, vì nó quyết định shader và post-process.
2. Kiểm tra `Docs/package.md`: ghi "Unity InAppPurchase" nhưng `manifest.json` chưa có `com.unity.purchasing`.
3. Với asset trả phí, xác nhận bạn có license trước khi thêm vào project.

## Tài liệu liên quan

- `UI_Flow.md`: các loại UI và luồng chạy.
- `Models.md`: danh sách model 3D.
- `package.md`: ghi chú các thư viện đã cài.
