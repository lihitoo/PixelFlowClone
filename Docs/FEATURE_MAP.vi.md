# Phạm vi cơ chế từ dữ liệu nguồn

Code đã mở rộng sau kiểm kê:12 nhóm shooter,24 board kinds, pixel health/hidden và catalog2.600 màn. Hợp đồng runtime, các semantics inferred và test hiện tại nằm ở [FULL_GAMEPLAY_PLAN.md](FULL_GAMEPLAY_PLAN.md), [FULL_FLOW_MATRIX.md](FULL_FLOW_MATRIX.md), [CURRENT_STATE.md](CURRENT_STATE.md). Roadmap pilot7 màn bên dưới là lịch sử; không còn mô tả phạm vi code hiện tại.

Kiểm kê ngày 2026-10-24 bằng `Tools/profile_source_features.py`; chỉ đọc JSON đã decode, không import asset hay mở thêm level trong game. Receipt: [Evidence/source-feature-profile.json](Evidence/source-feature-profile.json).

Tổng 3.117 definition trong 22 funnel khớp số lượng manifest. Campaign chính có 2.600 level; 517 definition còn lại thuộc các funnel khác, không được tự xem là phần tiếp theo của campaign chính. Các con số dưới đây là sự hiện diện của field/list, chưa xác nhận logic, độ khó hay khả năng chơi đúng của clone.

## Những phần hiện còn thiếu ngoài checkpoint

| Nhóm field nguồn | Số level trong campaign chính | Sample đầu | Điều cần xác định trước triển khai |
|---|---:|---|---|
| SurpriseShooters | 2.080 | 8,9,10 | Ẩn màu/đạn nào, thời điểm reveal, trạng thái khi về waiting và presentation dấu hỏi |
| ConnectedShooters | 2.258 | 14,15,16 | Nhóm liên kết, chọn/phóng cùng nhau, tính belt capacity, return và fail của nhóm |
| Locks | 143 | 40,41,42 | Điều kiện mở khóa, key/owner, chặn chọn và thứ tự feedback |
| ShooterPipes | 108 | 171,172,173 | Nguồn spawn và hàng ẩn, thứ tự xuất hiện, giới hạn và ammo |
| Hammers | 137 | 230,231,232 | Trigger, target, lượng tác động, tương tác với màu/đạn |
| ShooterIceBlocks | 108 | 290,291,292 | Durability/trigger phá băng, hành vi khóa shooter, âm và VFX |
| ChainedShooters | 267 | 801,802,803 | Quan hệ chain, điều kiện phá, targeting/input và nhóm liên kết |
| BullTotems | 130 | 1301,1302,1303 | Số lớp, damage/target, màu và tác động lên vòng chơi |
| MusicToyMallets | 98 | 1401,1402,1403 | Trigger, nhịp, target và thay đổi state; chưa suy từ tên field |
| SlotCages | 207 | 1801,1802,1803 | Slot nào bị chiếm/khóa, cách mở, fail khi return và UI capacity |
| AstronautShooters | 100 | 2001,2002,2003 | State đặc biệt, movement/targeting và trigger chuyển trạng thái |
| ConveyorDoors | 58 | 2201,2202,2203 | Vị trí cửa, thời điểm đóng/mở, belt/shot occlusion và queueing |

Các nhóm chồng lấp, không cộng cột để ra số level. Trong campaign chính có 107 level có mọi list feature trên rỗng và không có time limit; còn 2.493 level có ít nhất một feature. “Basic schema” chưa bảo đảm level giải được hay được importer/runtime hỗ trợ. SlotCount và ConveyorLimit bằng 5 ở 2.599 level, bằng 6 ở một level; không được coi 5 là bất biến toàn game. Không thấy HasTimeLimit bật trong campaign chính ở snapshot này; không suy ra tính năng đó vĩnh viễn không tồn tại.

Các sample basic đầu: 1–7,12,13,17,21,23,24,25,27,30,31,33,34,35,37,38,39,46,48. Không đưa chúng thành “level 8 tiếp theo” để né cơ chế đặc biệt; progression và SourceNumber cần nhất quán.

## Reference nhúng đã xem có chọn lọc

Ba clip nhỏ được lấy từ extraction hiện có; metadata/hash và cách lấy frame trong [Evidence/source-video-review/manifest.json](Evidence/source-video-review/manifest.json). Không sao chép chúng vào runtime Assets.

- `merged_shooter_tutorial_new_click`: video demo 4,52 giây, 1080×1080/60fps, không có audio stream trong metadata. Contact sheet cho thấy booster tạo shooter kiểu unicorn/rainbow và nhiều tia màu; không đồng nhất booster này với field ConnectedShooters chỉ vì tên file có “merged”.
- `out_of_space_offer`: video demo 3,43 giây, 1284×898/60fps. Thấy waiting có heo, số capacity 4/5→5/5 và một slot waiting trống khi heo chuyển lên belt. Không cho biết chính xác loss grace period của ứng dụng thật.
- `frozen_offer`: video demo 4,37 giây, 1284×898/60fps. Thấy nhóm shooter đóng băng rồi lộ heo bên dưới với hiệu ứng mảnh băng; chưa đủ để xác nhận field durability, trigger hay transaction của booster.

Các frame này bổ sung anchor về mặt heo, ammo, waiting và belt. Chúng là tutorial/offer dựng sẵn, có crop và kịch bản riêng; không thay thế capture Home/HUD/pause/result hoặc phép đo runtime cùng level. Không suy cadence/belt speed từ video quảng bá. Chưa có kiểm tra nghe audio trong batch này.

## Thứ tự mở rộng đề xuất

1. Khép checkpoint cơ bản: C02/C06 input/reset, C08 presentation ở nhiều aspect và C10/C11 audio/feel; giữ rule path đang chơi được.
2. Ưu tiên SurpriseShooters ở source level 8 vì đây là chỗ progression hiện tại bị ngắt. Viết contract reveal trước, chọn một pilot và test queue→conveyor→waiting→retry; không âm thầm loại bỏ field khi normalize.
3. ConnectedShooters ở source level 14 là pilot tiếp theo; cần chốt counting/capacity/fail theo nhóm trước mở hàng loạt level.
4. Locks, Pipes, Hammers, Ice rồi các nhóm sau: mỗi nhóm một sample có source receipt, state/input contract, rule fixtures và một lượt tích hợp có hình/âm.
5. Booster/meta/UI xuất hiện trong clip là một nhóm phạm vi riêng. Chưa triển khai/phe duyệt chỉ vì asset nhúng có sẵn. Không tự kéo ads/IAP/services vào project.

Đây là roadmap dựa trên quan sát dữ liệu, chưa phải owner acceptance cho toàn bộ scope hay cam kết rằng mọi cơ chế đã được khôi phục.

Tái kiểm kê:

```powershell
python Tools/profile_source_features.py D:/Downloads/PixelFlow_0.36.3_extracted/exports/Levels --output Docs/Evidence/source-feature-profile.json
```
