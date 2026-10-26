# Gameplay UI — art/ref và client checkpoint

Đã đối chiếu hierarchy0.36.3 (Core UI/UI Powerups) với ảnh UI triptych và video runtime mới. HUD nay là GameplayPanel.prefab kế thừa BaseFeaturePanel, có một SafeArea, gear/capsule level/coin-plus/bốn booster bằng art gốc và Lilita/preset outline-shadow. Bỏ constructor HUD cũ, back/II, progress/pixel prose và màn pause riêng. Root truyền panel vào view hiện có, giữ PixelFlowApp/PuzzleSession/ProgressStore làm chủ intent/model/save; không thêm presenter wrapper hoặc installer khác.

Gear/Escape mở Settings trực tiếp; đóng giữ cùng attempt và không mất tim. Coin mở Mini Shop rồi về cùng lượt. Counter thể hiện chỗ trống conveyor (5/5, dùng Tray thành6/6), tách khỏi hàng chờ. Popup hết booster được author sẵn trong GameplayPanel, gồm icon/tên/mô tả/giá/close; không dựng lại bằng code. Lỗi lưu purchase giữ tiền/stock/model và Retry hoàn tất mua/dùng một lần. Tham chiếu/native unlock/cost của target chưa đủ proof nên không tự thay numeric tuning hiện có.

Refill Lives dùng art tím/banner cam và số tim/countdown theo ref runtime mới, giữ GUID prefab. Rewarded-life đang unavailable, không grant giả khi chưa có provider. Get Coins vẫn giữ luồng client Mini Shop hiện có; vị trí/cách xuất hiện của nó và icon rewarded ad chưa khép exact parity. Các màn full/empty/unlimited dùng cùng ProgressStore đã có.

[Gallery nghiên cứu](../Research/Expansion20261025/review.html) có10 nguồn,10 clip có giới hạn và24 frame runtime được chọn; ledger giữ URL/hash/timecode/role. MGFTUE thể hiện footer0.32.1(263), không phải0.36.3; video09GameReview chưa xác minh build. Không dùng ref cũ để tự gán unlock, giá, profile/account hay backend cho target. Nhánh mẫu/code/package pin và source extraction được giữ nguyên. Không build player hoặc cài billing/ads SDK.

Các XML đầu được giữ để review: lỗi dispose HUD khi scene hủy đã sửa; fixture click xuyên world có thể chạm một action Settings phía trước nên cần đóng notice đó trước khi đo slider. Kết quả cuối và ảnh thật/URP preview được ghi trong validation.json/review.html, phân biệt phương pháp rõ ràng.

Chưa hoàn thiện toàn game: normal win/fail/continue/target/error UI còn cần prefab/source matching; board/belt/animation/ammo typography và rules nâng cao chưa được nghiệm thu native; account/rank/pass/events/ads production thiếu provider/evidence. Logic Unicorn cũ vẫn giữ để nghiên cứu, không trình bày thành nút ngang hàng thứ năm khi source HUD chỉ xác nhận bốn nút. Không coi checksum/compile/Editor tests là chứng nhận full-game parity.
