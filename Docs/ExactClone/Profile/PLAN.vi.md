# Profile / polish

1. Đối chiếu ref profile/name/confirm trong video0.32.1 với nút avatar Home và art0.36.3. FAQ hiện hành chỉ bổ trợ hành vi, không xác nhận layout/build target. Ghi source/timecode và những gì chưa biết.
2. Dùng BaseFeaturePanel variant cho Edit Profile, sửa trực tiếp HomePanel/prefab hiện có để gắn avatar đúng top bar. Một SafeArea, font/preset nguồn; tĩnh hóa grid9 avatar nhìn thấy trong ref. Chín avatar còn lại vẫn ở catalog, chưa tự gán unlock.
3. Presenter giữ draft; xác nhận tên rồi Save commit tên/avatar một lần qua ProgressStore hiện có. Hủy/đóng bỏ draft; lỗi lưu không đổi committed state và có Retry. Không tạo account/cloud/rank giả.
4. Kiểm thử migration save cũ, validation, atomic save failure, cancel/reopen, name No/Yes, avatar/header binding và round-trip. Chụp Game View1080×1920 rồi responsive, sửa sai khác và chạy kiểm tra liên quan. Không build player.
5. Cập nhật contracts/CURRENT_STATE, gallery và skill toolkit với bài học có evidence.

Giới hạn: native giới hạn tên, uniqueness/moderation, persistence timing riêng của confirm/Save, avatar unlock và target-version popup geometry còn chưa chứng minh. Client giới hạn128 UTF-16 code units để bảo vệ dữ liệu; không coi đây là rule gốc. Chỉ lưu local trên owner hiện có, không đổi hành vi Save Progress/account provider.
