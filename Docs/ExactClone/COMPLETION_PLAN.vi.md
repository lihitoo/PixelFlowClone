# Kế hoạch hoàn thiện game

Mục tiêu vẫn là toàn game theo bản nguồn 0.36.3. Đây là thứ tự thực hiện, không phải xác nhận clone đã hoàn tất. Không build player khi chưa có yêu cầu mới.

1. **Chốt checkpoint hiện tại:** HUD bốn booster, Settings trực tiếp, Mini Shop quay về cùng lượt, popup mua booster và Lives. Hồi quy cuối 140/140; đối chiếu ảnh preview baseline 1080×1920 rồi 1080×2340/720×1280. Ảnh Game View bổ sung đang thiếu vì ổ C đầy làm save review không ghi được.
2. **Win/fail/continue và chọn mục tiêu:** tìm đúng prefab/art đã xuất, giữ GUID và luồng model/save; author UI tĩnh, kiểm tra mua/tiếp tục/hủy/thất bại/Retry và không phát thưởng hai lần. Ref khác phiên bản chỉ chứng minh trạng thái quan sát được.
3. **Tutorial và booster:** đối chiếu Tray/Hand/Shuffle/Super Pig, điều kiện mở khóa, số lượng và animation. Dùng REA trên metadata/native được chọn để giải quyết câu hỏi cụ thể; không suy công thức từ tên method hoặc dummy C#.
4. **Rank/pass/events/account:** hoàn thiện từng entry/action/close/error bằng art phù hợp. Ghi riêng phần client có thể kiểm thử và phần cần provider/config/screenshot nguồn; không tạo score, mua hàng hay reward thành công giả.
5. **Đối chiếu hành trình đầy đủ:** loading → Home → chơi → kết quả → Shop/Settings/Lives và các nhánh meta. Sửa sai khác lớn trước; kiểm tra save/reload/lifecycle, baseline rồi responsive, cập nhật screen contracts và gallery.

Sau mỗi nhóm: cập nhật CURRENT_STATE/TODO và evidence; chỉ đưa bài học đã có bằng chứng vào skill game-vibe/extract của toolkit. Mỗi claim giữ nguồn/build/timecode/hash, mức chắc chắn và phép kiểm chứng tương ứng. Không mở rộng code chỉ để có thêm abstraction.

REA: repo chính thức https://github.com/morluto/rea, source đã chọn `1173c0a9dc1b352a60943a491462294db8f353ae`; CLI npm 6.8.0, Node riêng 24.22.1. Dữ liệu phân tích nằm trong Research/REA20261026, không vào Assets hoặc toolkit.
