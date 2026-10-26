# Gameplay HUD và luồng tiếp theo

1. Bổ sung ref mới: giữ URL, phiên bản/timecode, ảnh/clip và nguồn art; phân biệt runtime thật, bài phân tích và quảng cáo.
2. Dùng variant BaseFeaturePanel cho HUD: gear → Settings trực tiếp, capsule level/difficulty, coin → mini-shop rồi về đúng lượt, bốn booster có amount/plus. Giữ controller/progress hiện tại, bỏ các constructor HUD và màn pause riêng.
3. Đối chiếu 1080×1920 trước; kiểm thao tác, đóng/mở Settings, Restart/Give Up có xác nhận, lỗi lưu, mua booster/Shop quay về đúng context; sau đó kiểm các tỷ lệ khác. Không build player.
4. Dựa trên ref mới để tiếp tục booster/tutorial/result/account. Không gán rules của video0.32.1 cho0.36.3, không tạo backend/reward thành công giả. Source UI Powerups có bốn nút; logic Unicorn cũ vẫn giữ để nghiên cứu tiếp, không tiếp tục trình bày như một nút ngang hàng thứ năm chưa có proof.

Nguồn layout0.36.3: Core UI.prefab và UI Powerups.prefab, custom fields bị strip. Ref hình hiện có ui-triptych.jpg; ref runtime bổ sung trong Research/Expansion20261025. Bố cục có proof, sprite binding và numeric tuning còn cần ghi mức tin cậy riêng.
