# Pixel Flow — hướng hình ảnh, UI và feedback

Đọc cùng [TASK.md](TASK.md), B11/B12 trong [BEHAVIOR_MATRIX.md](BEHAVIOR_MATRIX.md) và role của từng ảnh trong [refs/manifest.json](refs/manifest.json). Đây là baseline và mục tiêu đề xuất, **chưa phải style đã owner duyệt**. Lần bổ sung spec này chỉ đọc text/metadata, không mở lại asset nặng hay tạo thêm ảnh.

## Nhận diện cần giữ

Puzzle có silhouette rõ: bảng pixel nhiều màu ở trung tâm; belt bao quanh; heo là shooter có màu và số đạn; queue/waiting tách nhau; background xanh, khối bo/bevel kiểu toy, UI tương phản và ít che board. Dùng asset nguồn khi phù hợp thay vì dựng lại hình heo gần giống. Tránh thêm môi trường 3D, texture nhiễu hay hiệu ứng che pixel đang bắn.

Marketing anchor R03/R04 cho cảm giác và thứ bậc chung; chưa dùng chúng làm bản vẽ chính xác Home/HUD. Capture clone R05–R07 là baseline cũ để thấy tiến bộ, không phải target chuẩn hay bằng chứng sửa source mới.

## Hợp đồng từng nhóm

| Nhóm | Baseline source/code đang có | Mục tiêu review | Lỗi cần chặn |
|---|---|---|---|
| Camera | Orthographic; `max(8.4,4.55/aspect)` trong PixelFlowApp | Toàn bộ board, belt, ammo, hàng chờ nằm trong safe area ở portrait; mặt heo đọc được | Clipped queue, UI đè belt, thấy lưng heo thay mặt |
| Board/pixel | Bevel cube procedural; footprint khoảng .94 cell, palette34 ID từ material gốc | Ô vuông tách biệt, silhouette source-level đúng, cạnh không hòa màu | Sai color ID, mất pixel vì shading, shadow che target |
| Belt | Slate/blue procedural; đường rectangle, treads và viền sáng | Thấy hướng chạy và vị trí shooting; bo góc và tốc độ có chủ đích | Heo xuyên viền, corner snap, entry chồng nhau |
| Pig/ammo | Pig.obj/Pig.png từ source; body Y180, scale0.90, queue row spacing1.15 | Mắt/mõm hướng hợp camera; ammo đủ tương phản ở queue/conveyor/waiting | Label lộn, bị model che, màu khó phân biệt, mặt sai hướng |
| UI | uGUI/TMP, RobotoBold; reference720×1280 Expand; nút sprite gốc, panel/progress dùng rounded nine-slice riêng | Một hành động chính/màn; thứ tự level→board→queue; safe area, modal blocking | Không kéo sprite nút thành bar/card; text tràn, generic modal không có anchor gốc |
| VFX | Projectile/pixel shrink procedural | Hit đọc được trong normal speed, feedback khớp đúng ô | Diễn hoạt làm tưởng xóa sai màu, quá nhiều projectile/flash |
| Audio | 4clip Tap/Launch/Pop/Win | SFX nhất quán với input/hit/result, mute đúng và không chói khi bắn dày | Chọn đúng file nhưng trigger/mix sai; không nghe đã nhận là đạt |

Palette/material nguồn có receipt trong `Evidence/asset-intake.json`; RGB background trong clone được lấy từ nguồn là `(0.19215687,0.3019608,0.4745098)`. Các màu procedural board/belt và camera code là lựa chọn implementation, chưa chứng minh match original. Không diễn giải tất cả số code thành design token của game gốc.

## Những frame cần có trước khi gọi visual pass

Batch Editor ngày2026-10-24 tạo preview bằng URP render request tại3 aspect, tạm đổi canvas overlay thành ScreenSpaceCamera rồi phục hồi. Đã mở preview level 1: mặt heo nhìn thấy, hai màu nguồn và ammo đọc được, progress không còn hình thoi. Panel pause/win đã chuyển sang card bo góc. Đây là preview offscreen có giới hạn: không phải Game View, không mô phỏng device safe area, không đủ để duyệt fidelity với game gốc. Camera phải được truyền đúng khi chuyển world label sang canvas camera; helper cũng tắt shader async tạm thời để tránh frame đầu dùng shader preview chưa hoàn tất.

Lượt cuối đã tách vị trí belt → chữ WAITING → tray → ammo/queue; preview mới không còn chữ WAITING đè viền belt và ammo hàng đầu đã tách khỏi tray. Khoảng trống phía trên board ở màn hình dài, hình belt còn góc vuông và độ giống UI gốc vẫn cần review. Không gọi C08/C09 đạt đầy đủ từ preview level 1 hoặc thay cho device safe area.

1. Home với save mới, Home với progress; đối chứng Home gốc còn thiếu.
2. Level1 vừa mở, chưa input; cùng board/input/camera khi so game gốc.
3. Belt có nhiều heo, queue còn ít nhất 2 hàng, waiting có heo; mọi ammo/slot đọc được.
4. Pause trên board đang chạy, Win sau pixel cuối, Lose do return-full; kiểm tra modal và hierarchy.
5. Cùng level/state tại 720×1280,1080×1920,1080×2340; chú thích device/safe-area simulated.
6. Clip tốc độ thường: launch→corner→10 hits→return→relaunch; nghe riêng sound-on/off.

Mỗi capture ghi source fingerprint/revision, Editor hay player, state/level, resolution và ngày. Mở ảnh thực tế trước khi nhận xét. Không dùng ảnh có từ artifact10 để duyệt hướng pig mới. Sau thay đổi UI/camera, compare cùng state rồi sửa ba sai khác lớn nhất trước thêm art/feature. Điểm số giống game không được tự điền khi chưa có đối chứng.

## Asset mới nếu cần

Giữ route existing cho asset đang có; procedural phù hợp geometry đơn giản nhưng cần match silhouette. Nếu thiếu icon/VFX/background: ghi role, kích thước, alpha/pivot/material, reference và tiêu chí import; làm một pilot trong scene trước batch. AI gen không thay logic, không mặc định cần chi phí; giữ prompt/job/nguồn và output receipt khi dùng. Texture/mesh master ở extraction/source; chỉ runtime selection vào project. Không chép ảnh marketing nguyên màn làm giao diện giả.
