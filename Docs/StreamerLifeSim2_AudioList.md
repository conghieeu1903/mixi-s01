# Streamer Life Simulator 2 — Danh sách âm thanh cần có

> Cập nhật: 2026-10-20

Nguồn: thư mục `SLS2/AudioClip` (491 clip) và `SLS2/AudioMixerController/MainMixer.mixer`. Tên clip giữ nguyên như file gốc. Phân nhóm là suy ra từ tên file, chưa mở từng clip để nghe.

## Mixer

`MainMixer` có group `Master`, `Music`, `Effects`. Nhạc nền đi qua `Music`, mọi SFX đi qua `Effects`. Cần slider âm lượng riêng cho từng group trong Settings.

## 1. Nhạc nền (Music)

| Clip | Dùng cho (suy từ tên) |
| --- | --- |
| `Chillin'`, `Peacefully - E's Jammy Jams`, `Ice Cream - Joey Pecoraro` | Nhạc thư giãn trong nhà / Spoti |
| `Downtown`, `Nightclub`, `Gangsta`, `Modern western`, `Modern western_B` | Nhạc xe, club, radio |
| `The Race`, `Unleashed`, `Unstoppable`, `Strength of a Thousand`, `Enemy Territory LOOP` | Nhạc đua / bắn súng (CamperStrike, Pogo) |
| `Cutscenes`, `Cutscenes_2` | Cutscene |
| `BERKACE_RETRORADIO_CREEPYWEIRDINTERVIEW` | Radio |
| `guitarsfxx` | Nhạc ngắn |
| `cafeambiance`, `naturesound`, `stadiumcrowd`, `Wind`, `rain_heavy_loop_01`, `Street Light Buzzing Soundd` | Âm môi trường (loop) |

## 2. UI và hệ thống

| Nhóm | Clip |
| --- | --- |
| Nút bấm | `ButtonClick`, `ClickyButton10a`, `ClickyButton5a`, `ClickyButton5b`, `GenericButton5/6/13`, `SnappyButton5`, `uimenuclick`, `UI_Sound_Select`, `UI_Sound_Nav_Light`, `UI_Sound_Nav_Strong`, `UI_Sound_Cancel`, `priceincreasebutton` |
| Mở/đóng cửa sổ | `Popup3`, `ui_menu_popup_04`, `Minimize2`, `OpenOrEnable3`, `OpenOrEnable4b`, `PcFocusnonFocusSfx` |
| Thông báo | `GenericNotification3/4/5/10a`, `notificationsfx`, `donatenotification2`, `alert1/2/4/5/6`, `WarningPanel` |
| Lỗi | `Error1`, `Error2`, `Error3`, `Error5`, `bluescreensfx` |
| Thành công | `Success1`, `Success2`, `Success5`, `Success7a`, `Success7b`, `Success9`, `Success13`, `successwin` |
| Điện thoại | `clickphone`, `PhoneTap`, `lockphone`, `unlockphone`, `takephotosfx`, `cameraopened`, `cameraclosed`, `camerashoot` |
| Tiền | `cashbeep`, `Store Bell Sound Effect` |

## 3. Stream, donate, chat

| Nhóm | Clip |
| --- | --- |
| Hoàn thành minigame | `AsmrCompleted`, `AsmrOneCompleted`, `dance completed`, `DanceOne` |
| Donate âm thanh (người xem chọn phát) | 30 clip đánh số `1-usb` … `30-ewwwbro` (đuôi `_sounddonate`): usb, doorknoc, senpai, oof, woof, nope, yeeyt, windowerror, ssssdh, oiuiuiu, fortdead, heheboi, angryani, avidi, nonocat, boink, megafart, arara, haaah, imrightbehind, trumped, uwu, andronotif, sus, huh, laughhoho, meowsad, megalaugh, liedetect, ewwwbro |
| Tiếng cười, tiếng hét | `justlaughrussian`, `ragerussi` |

## 4. Giọng NPC (voice)

| Nhóm | Số clip | Ghi chú |
| --- | --- | --- |
| `greeting_*` (chào) | 24 | 5 giọng: alex, ian, karen, meghan, sean |
| `miscellaneous_*` | 28 | Câu lẻ của các giọng trên |
| `imp_*` (cảm xúc) | 69 | agree, alert, amused, angry, ask, bored, caution, confused, content, cool, demand, disagree, doubt, irritated, joy, normal, okay, prompt, rage, terrified, tough; mỗi cảm xúc 1–5 biến thể |

## 5. Bước chân và di chuyển

| Nhóm | Số clip |
| --- | --- |
| `footstep_concrete_walk_01…22` | 22 |
| `footstep_wood_walk_01…20` | 20 |
| `footstep_grass_walk_01…10` | 10 |
| `footstep_dirt_walk_run_01…07` | 7 |
| `Footstep_Metal_1_1…13` | 13 |
| `MoveGravel`, `getup`, `sitdown`, `DivingIn`, `DivingOut`, `FallInWater`, `FallInDeepWater`, `SwimmingOnSurface`, `SwimmingUnderSurface`, `Underwater`, `Dash1…4` | 14 |

## 6. Tương tác đồ vật và nhà

| Nhóm | Clip |
| --- | --- |
| Nhặt/cầm | `foley_object_grab_pickup_*` (8), `foley_soldier_gear_equipment_movement_grab_item_*` (11), `DropItem`, `xdropsound` |
| Cửa | `DOOR OPEN`, `DOOR CLOSE`, `doorbell`, `doorbuzzer`, `door_lock_fail_02`, `knockdoorsfx` |
| Nhà | `LightSwitch`, `fridgeopen`, `fridgeclose`, `showersfx`, `toiletsfx`, `soapcutting`, `typingsfx`, `papersfx`, `papersfx1`, `papersfxshort`, `pillbottle` |
| Ăn uống | `EAT Chew 3 Times (mono)`, `DRINK Long (mono)` |
| Dọn dẹp | `broomlong`, `cleanshine`, `cleansound1/2/3` |
| Hộp, giao hàng | `boxopened`, `boxopened2`, `DeliverySearching`, `DeliveryFounded`, `DeliveryCompleted`, `DeliveryCanceled` |

## 7. Máy tính và dàn máy

`Computer Fan`, `pcstartbutton`, `pcshutdown`, `bootingseq`, `pccoveropen 1`, `pccoverclose`, `partbuild4`, `partbuildSfx`, `partbuildsfx2`, `partbuildsfx3`, `CaseSound`, `caseBuyed`, `caseEND`, `gamefoundsfx`, `Air`, `BlowOff`, `inflate`, `linedrop`.

## 8. Casino và may rủi

| Mechanic | Clip |
| --- | --- |
| Slot | `slotstart`, `slotstop`, `slotrollloopw`, `SlotWinSFx`, `bonusdropped`, `spin`, `spinwhoosh` |
| Cào thẻ | `ScratchSfx` |
| Rơi thưởng | `DropGems07 - Mono` |

## 9. Phương tiện

| Nhóm | Clip |
| --- | --- |
| Xe hơi | `Engine_01_Idle/Medium/High/Start`, `Engine_02_Idle/Medium/High/Start`, `Cadillac`, `Crusing`, `Turbo`, `BackFire_01…03`, `Suspension_1…3`, `skid`, `driftsfx`, `SlipAsphalt`, `SlipGravel`, `Crash_Strong`, `carhit`, `cardoorOpen`, `cardoorClose`, `carkeyenter` |
| Xe máy | `MotorbikeEngine_01_Idle/High`, `motorengine idle`, `BikeStopped` |
| Xe tải | `TruckEngine_01_Idle/High/Start` |
| Xe đạp | `Bicycle Brake sound`, `bikechainloop2`, `bikehorn`, `cyclingloop` |
| Ma sát, va chạm | `MetalCollision_Easy/Medium`, `MetalFriction`, `Shards_Easy/Medium/Strong` |

## 10. Chiến đấu (CamperStrike, SWAT, đánh nhau)

`deaglehit`, `pistolhitt`, `shotgunhit`, `semihit`, `reload`, `no ammo sfx`, `buyweapon sfx`, `triggersfx1…6`, `aifire`, `csdead`, `csplayergetdamage`, `flashbangsfx`, `swattingsfx`, `fightfps`, `victoryfps`, `defeatfs`, `punch1`, `punch2`, `kick1`, `alarmloop`, `extungiesh`.

Hiệu ứng nổ, lửa: `Toon explosion`, `Toon ground explosion`, `SFX_FireBig_L`, `SFX_FireMedium_L`, `SFX_FireSmall_L`, `SFX_Vefects_Vignettes_Fire_Loop`.

## 11. Thú cưng và động vật

`catmeow`, `dogbarking`, `TamingStartA`, `TamingStartB`, `TamingSuccess`, `TamingFail`.

## 12. Khác

`Searching (Loop)`, `1`–`5`, `1_0`–`5_0` (clip đánh số, chưa rõ mục đích).

## Cách dùng khi làm lại

- Không cần giữ đủ 491 clip. Bộ tối thiểu: nhạc nền 3 bài, UI (nút, popup, thông báo, lỗi, thành công), donate, bước chân 1 bề mặt, cửa, máy tính, ăn/ngủ/tắm, và các SFX của minigame bạn giữ lại.
- Nhóm bước chân (72 clip), giọng NPC (121 clip) và donate âm thanh (30 clip) chiếm phần lớn số lượng nhưng dễ cắt giảm.
- Với các clip lấy từ asset bên thứ ba (nhạc có tên tác giả như `Ice Cream - Joey Pecoraro`, `Peacefully - E's Jammy Jams`), cần kiểm tra giấy phép trước khi dùng.
