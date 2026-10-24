# Review source/Editor — 2026-10-24

**Lịch sử checkpoint trước full gameplay/UI.** Source hiện đã mở rộng2.600 màn và các luồng offline. Bằng chứng mới nhất nằm ở [CURRENT_STATE](../../CURRENT_STATE.md) và [FULL_FLOW_MATRIX](../../FULL_FLOW_MATRIX.md); không dùng các câu “không import thêm campaign” hoặc số2 fixture dưới đây như trạng thái code mới.

Phạm vi: source Unity clone, không build player. Chủ dự án đã đóng Unity tương tác; các lượt compile/test dùng process Editor batchmode 6000.4.12f1 rồi thoát. Không thay đổi XAPK/extraction, không import thêm campaign hay gen asset trả phí.

## Thay đổi đang được kiểm tra

- Pig giữ mesh/texture gốc, body Y180; tăng scale0.74→0.90 và queue spacing1.05→1.15 để đọc mặt/đạn rõ hơn.
- Progress và modal dùng rounded nine-slice64×64 do view sở hữu, không kéo hình octagon của sprite nút thành bar/card. Nút vẫn dùng art nguồn.
- World label chọn đúng UI camera khi canvas dùng camera. Runtime thường vẫn dùng overlay; helper preview dùng camera nên cần phép chiếu đúng.
- Board cũ được deactivate trước deferred Destroy khi retry/home. ClearSession dừng audio còn thuộc session, reset cooldown/result timer; save/progress giữ riêng.
- Editor fixtures dùng save scope riêng. Không sửa input settings asset của project; clone settings và thiết bị bị tạm cô lập được phục hồi.

## Điều quan sát được từ ảnh đã mở

Đã mở preview level 1 ở 720×1280,1080×1920,1080×2340 từ `verified-source-review-20261024`: mặt/mõm heo nhìn thấy, board đủ hai màu nguồn, ammo đọc được, progress không còn hình thoi, board/queue không bị cắt ở các frame đó. Chữ WAITING đã rời viền belt, tray và ammo đầu queue đã tách nhau. Đã mở pause và Home-progress ở batch cuối; card/title/detail/buttons đọc được, Home thể hiện level 2 đã mở. Win/modal của batch trước cũng đã được mở, ảnh Win mới được giữ cùng receipt.

So với capture Windows cũ `../player-final10/level-1.png`, hướng mặt và kích thước heo, hình progress/modal đã thay đổi. Đây là so sánh tiến bộ của clone; không chấm điểm giống game gốc từ hai ảnh khác version/render context.

Lỗi còn thấy: màn hình dài có nhiều khoảng trống phía trên board; belt còn góc vuông/tread rõ, khác các anchor toy/rounded. Chưa có Home/HUD/result gốc cùng state để match chính xác. Chưa nghe mix hoặc review normal-speed animation/feel, chưa đo device/performance.

## Giới hạn fixture và chẩn đoán

Các lần thử đầu phát hiện những khác biệt của EditMode runner khi EnterPlayMode: NUnit setup chạy lại qua domain reload; queued input không tự được bơm như player; canvas vừa bật trong batch Editor chưa có rendered depth cho GraphicRaycaster. Fixture giữ state bằng SessionState, dùng InputSystem/EventSystem processing rõ ràng và render offscreen UI khi depth chưa có. Không gọi trực tiếp Launch/win hoặc cấp completion để đi qua gold path. Có một assertion restart cùng frame dùng actual button event đồng bộ, được ghi riêng trong receipt.

Kết quả không được đổi thành xác nhận input vật lý hoặc normal player scheduling. Những report fail trước vẫn được giữ, không gộp thành pass. Bộ lọc namespace lỏng từng chọn cả assembly review khi chạy `-nographics`; lượt `editmode-tests-20261024-104702.xml` xanh22 case không được dùng làm bằng chứng hình ảnh. Đã sửa filter theo fixture/namespace, chặn review với Null graphics và từ chối ghi đè PNG có sẵn. Lượt cuối tách đúng Tests 20 case và Review 2 case; xem các XML timestamp và receipt tổng hợp mới nhất.

## Reference nguồn mới

Đã kiểm kê JSON nguồn: [feature profile](../source-feature-profile.json), [roadmap](../../FEATURE_MAP.vi.md). Đã mở contact sheet của ba clip nhúng: merged/unicorn tutorial, out-of-space offer và frozen offer; [manifest](../source-video-review/manifest.json) giữ hash, metadata và cách lấy frame. Chúng bổ sung state/mẫu nhìn, không thay phép đo runtime bản gốc cùng level. Video demo không có audio stream trong metadata, nên không dùng để duyệt mix.
