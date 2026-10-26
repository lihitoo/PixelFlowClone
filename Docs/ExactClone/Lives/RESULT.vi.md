# Lives/refill — prefab/client checkpoint

LivesPanel.prefab là variant có skin riêng của BaseFeaturePanel đã cài, kế thừa SafeArea/CanvasGroup/tween. Dùng lại popup/nút/font/heart gốc đã nhập; heart giữ Simple/preserve aspect, các khung liên tục có border dùng Sliced. Root inject LivesPresenter; ProgressStore vẫn là chủ wallet/lives/save duy nhất. Đã bỏ hàm dựng UI Lives runtime cũ.

Full/empty/unlimited hiển thị đúng count, timer, icon và trạng thái nút. Không nạp khi đủ tim hoặc unlimited; không đủ tiền mở mini-shop rồi quay lại đúng Lives; lỗi lưu không trừ tiền/tăng tim một nửa. Refill và Restart/Give Up dùng cùng dữ liệu đã lưu, không sinh thêm chủ state.

Regression editor-verified.xml đạt138/138, có toàn bộ rule và real-scene pointer flows. Case unlimited/lượt đã ghi thua được tăng kiểm tra và chạy bổ sung1/1 ở abandon-loss-verified.xml. Hai XML137/138 trước giữ nguyên lỗi fixture: Simulator phục hồi giữa từng bài làm Game View/input đổi trạng thái; fixture nay giữ Simulator đóng suốt lượt chạy, kiểm actual1080×1920 rồi mới tạo pointer và phục hồi window khi RunFinished. Không thêm retry-click, không nới assertion của sản phẩm.

Ảnh Game View và receipt ở review.html/validation.json. URP preview trong Docs/Evidence/editor-review/settings-lives-verified-20261025 là phương pháp riêng. Runtime compile không lỗi; static validation kiểm103 asset nhập và Runtime/Editor boundaries. Không build player, không chạy billing/ads SDK.

Chưa có ảnh runtime refill native0.36.3; catalog không có prefab refill export đầy đủ và các field heart_refill_settings_json/Heart Refill Info bị strip. Max5/1800giây/900coin vẫn là tuning ước lượng hiện có. Không tự thêm rewarded-ad hoặc nhận reward giả. Cutout/device, production billing/ads/account/rank/pass/event và full-game parity chưa được xác minh. Gameplay HUD/pause và popup chung khác vẫn nằm trong hàng việc tiếp theo.
