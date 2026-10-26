# Phân tích REA — 0.36.3

Đã chạy CLI `rea-agents` 6.8.0 bằng Node riêng 24.22.1 trên Windows. Repo chính thức đã clone tại D:/Tools/rea, commit `1173c0a9dc1b352a60943a491462294db8f353ae`. Không đăng ký MCP, thay cấu hình Codex, chạy game nguồn hay cài disassembler.

Input: global-metadata.dat từ raw/base của extraction 0.36.3; SHA-256 `6141d724c7c6c6ffc1a58bff4b49246380d1c26cdc388570ba1fc80f4fa59609`, 47,292,648 byte. Metadata version 31. REA đọc được 388 images, 33,230 types, 303,157 methods. Evidence gốc trong metadata-evidence.json; summary và selected-gameplay-members.json giữ nguồn/index.

Quan sát tĩnh có ích cho bước tiếp theo:

- HandPowerupController: OnShooterTapped, OnPowerupRequested, OnPowerupCanceledOrFinished, PerformPowerup; fields allQueueShootersTappable/outlineAnimation. Đây là đầu mối để đối chiếu target mode trong video, chưa chứng minh quy tắc chọn shooter.
- ShufflePowerupController: ExecutePowerup và field performDuration; giá trị duration chưa được khôi phục.
- LevelFailOfferMiniShopPurchaseCoordinator: get_MiniShopId, get_LevelFailOfferId, OnPurchaseCompleted, ShowLevelFailOfferView. Có cấu trúc phối hợp continue/Mini Shop; chưa chứng minh điều kiện, giá hay kết quả giao dịch.
- LevelFailRvContinueGranter: Grant/GrantAsync; LevelFailOfferPopupView có titleVar/descriptionVar/videoPlayer. Không coi đây là ads provider hoạt động hoặc video đã được kiểm chứng.

Nhiều member bị obfuscate. Không liên kết native binary trong lượt này; không có địa chỉ hàm, kiểu field/parameter/return, method body, numeric config hoặc bằng chứng runtime. Không import dummy C# và không sửa gameplay chỉ theo tên.

Lần gọi module nội bộ đầu tiên thoát 0 nhưng không xuất Evidence; chỉ lần chạy qua scripts/rea.mjs có export 37,784,254 byte mới được tính là thành công. Cache tải đầu trên C gặp ENOSPC; cache và runtime tách sang D. Phiên review Unity bổ sung cũng gặp disk-full khi ghi save trên C, đã dừng và khôi phục review scope; không gắn lỗi môi trường này vào kết quả hồi quy 140/140 trước đó.
