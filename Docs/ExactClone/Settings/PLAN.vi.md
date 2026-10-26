# Settings và các luồng tiếp theo

1. Chốt Shop: bỏ Restore ở prefab, kiểm đủ 11 gói/No Ads và lưu ảnh Game View cùng kết quả kiểm tra.
2. Sửa trực tiếp SettingsPanel của sample: slider Music/Sound, Haptic/Notification/Smooth Motion, các action nguồn và SafeArea; dùng art/font đã trích xuất, prefab sẵn ở 1080×1920, Match 0.5.
3. Nối presenter từ root hiện có, giữ ProgressStore là chủ dữ liệu duy nhất. Settings dùng chung cho Home và trong level; close/Back không mất lượt chơi, Restart/Give Up có xác nhận. Restore đi qua gateway Shop hiện có.
4. Kiểm thao tác, lưu/lỗi/đóng/mở lại, pause/resume và các tỷ lệ màn hình. Sau đó tiếp tục lives/refill và các surface có art/contract nguồn; thiếu backend hoặc ảnh native vẫn ghi rõ, không dựng thành công giả.

Gap đã kiểm: package SettingsPresenter chỉ có toggle/volume đơn, haptic và platform actions; không có Notification/Smooth Motion, game context, xác nhận bỏ lượt hay phản hồi save-error. Dùng một presenter cho màn Settings nguồn trên chính sample view, không thêm settings store hoặc installer khác.

Nguồn0.36.3: `Settings View Variant.prefab` có đầy đủ hierarchy/control/RectTransform nhưng các component custom bị strip. Chưa tìm được ảnh runtime Settings đúng phiên bản qua research hiện có/tìm web bổ sung ngày2026-10-25. Layout được đối chiếu hierarchy và art nguồn; chưa được coi là pixel parity native.
