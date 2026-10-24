# Home / Shop — prefab-first revision

Owner yêu cầu ngày 2026-10-24: chỉnh trực tiếp HomePanel và ShopPanel của template/sample, giữ GUID và flow Bootstrap → Ready → Main. Không build player.

1. Đối chiếu Home level 36 và Shop với ảnh gốc đã lưu; kiểm source layout/art/font và package pin.
2. Author sẵn cây UI trong prefab gốc; SafeArea chứa nội dung tương tác, nền bleed ra ngoài. Canvas Scale With Screen Size 1080×1920, Match Width Or Height 0.5. View chỉ render/intent; presenter nhận dependency từ root; progress/gateway hiện có giữ quyền ghi dữ liệu.
3. Bỏ adapter Shop dư thừa và constructor Home runtime; dùng font trích xuất cùng preset outline/shadow lưu thành asset. Simple/preserve aspect cho icon/art; chỉ Sliced với frame/bar có vùng giữa liền mạch và border phù hợp.
4. Compile, rule/save/gateway tests, pointer routes, preview ở 1080×1920 rồi các aspect khác; so ảnh và sửa. Lưu evidence và phần chưa xác minh. Không coi fake IAP hay ảnh web khác version là production/native parity.

Toolkit: cập nhật unity-ui, dreamy-ui, unity-ui-reskin và game-vibe-workflow; dùng lại template convention, generic class names và MVP/DI, không thêm framework không cần thiết.
