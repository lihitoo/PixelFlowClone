# Settings — prefab/client checkpoint

Đã sửa trực tiếp SettingsPanel.cs và SettingsPanel.prefab trong sample, giữ GUID và BaseFeaturePanel/SafeArea. Popup mở qua PanelManager từ Home, Shop và màn pause trong level; root inject SettingsScreenPresenter, dùng ProgressStore hiện có. Không tạo thêm settings store/installer. Hai phần Settings dựng bằng code cũ cùng HomeSliderCommit đã bỏ; script authoring tạm cũng đã xóa sau khi lưu prefab.

Giao diện dùng 12 ảnh gốc có ID/URI/hash/GUID trong asset-intake.json, cộng các slider/toggle/close đã nhập ở Home, Lilita One và material outline/shadow đã lưu. Music/Sound commit riêng khi thả/thoát slider; Haptic, Notification, Smooth Motion lưu và mở lại đúng. Restore chạy qua Shop gateway hiện có. Restart/Give Up cần xác nhận; hủy giữ session, xác nhận commit bỏ lượt và trừ một tim, không trừ khi unlimited hoặc lượt đã ghi thua. Save failure giữ dữ liệu, Retry hoàn tất luồng; không trừ lặp.

editor-final.xml đạt12/12, gồm7 rule tests và5 UI routes. Ảnh Game View thật ở1080×1920 trước, rồi1080×2340/720×1280; ảnh URP fixture được phân biệt trong review/receipt. Static validation kiểm103 file nhập cùng Runtime/Editor boundary. Không build player.

Các mục nguồn Privacy Settings/Send Feedback/Save Your Progress/Instagram/Delete Account/User ID hiện có entry và trạng thái chưa kết nối. Chưa có provider account/cloud/consent/support/link xác nhận từ bản nguồn; không báo đăng nhập, restore thật hoặc xóa tài khoản thành công giả. Notification chỉ lưu preference, chưa gửi notification. Gameplay vẫn có màn pause cũ trước Settings; chưa đạt source-native single settings entry. Chưa có ảnh runtime Settings đúng0.36.3 nên chỉ đối chiếu hierarchy/art, không tuyên bố pixel parity. Cutout/device và production services chưa kiểm.


Cập nhật2026-10-26: gear gameplay đã mở thẳng Settings, bỏ màn pause riêng; xem ../Gameplay/review.html cho checkpoint mới. Video runtime mới có footer0.32.1(263); cấu trúc Music/Sound của target0.36.3 vẫn được giữ theo prefab target, không thay bằng UI cũ một slider.
