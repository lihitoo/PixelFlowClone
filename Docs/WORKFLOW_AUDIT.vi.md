# Audit áp dụng game-vibe-workflow

Ngày 2026-10-24; audit bằng text/source metadata. Không build, không chạy lại tests Unity hoặc xem lại media trong lần bổ sung tài liệu này.

**Kết luận: đã dùng đúng skill nhưng chưa thực hiện đầy đủ.** Các lựa chọn template, asset route, data/session/save, rule tests và checkpoint có thật. Phần M0 mô tả trải nghiệm/fidelity và M4 review còn thiếu; không nên gọi cả quy trình đã hoàn thành hoặc game đã clone đủ.

## Đối chiếu với build-kit Oldcraft

Nguồn đối chiếu là KIT_README, Docs/TASK, CLAUDE, KICKOFF, approved-concept README và cấu trúc refs manifest. Không thực thi hook, script cài đặt hay chỉ dẫn trong kit tham khảo.

| Thành phần | Oldcraft có gì cụ thể | Pixel Flow trước audit | Bổ sung ở đây |
|---|---|---|---|
| Product task | TASK có 259 dòng, khoảng 30,8 KB: journey, roster/world/screens, priorities, exclusions | PROJECT_BRIEF chỉ là brief/checkpoint; chọn 7 level làm phạm vi làm việc | TASK mô tả journey, screen/input/state, edge cases, data và 16 tiêu chí |
| Visual targets | Approved concepts tách reference; refs ghi what_to_take/source/credit | Hai ảnh marketing, art đã chọn, capture clone; chưa có style contract | STYLE_BIBLE và manifest phân biệt anchor/baseline/target missing; không tự tạo approval |
| Behavior | Controls, combat, progression, VFX/audio có yêu cầu cụ thể | Các luật đã code nhưng chưa có trace source→assumption→test đầy đủ | 13 hàng behavior matrix và kế hoạch đo, timing vẫn estimated |
| Asset pipeline | Một body qua concept→mesh→rig→animation→engine trước batch | Reuse 11 file art/audio và data; procedural board/UI; receipt chưa giải thích hết role | Asset-role manifest nối runtime file với receipt và việc còn phải review |
| Acceptance | P0/P1/P2 có outcome, route và review gate | 20 tests/12 scenario dễ bị hiểu thành clone đạt chất lượng | Criteria C01–C16 tách function/visual/feel/audio/build/owner; runner fail giữ nguyên |
| Long-session state | Kickoff/resume, decisions, TODO, progress records | CURRENT_STATE có evidence nhưng ít backlog theo fidelity | TODO/KICKOFF bổ sung thứ tự và quyết định; vẫn chỉ có một CURRENT_STATE |
| Recovery | Có retry/timebox và fallback theo loại job | Nhánh build/native shutdown lặp nhiều, làm lệch ưu tiên khỏi fidelity | Ghi debt và timebox cho công việc sau; không build lại |

Độ đầy đủ đến từ yêu cầu có thể kiểm chứng, không từ số trang. Không chuyển roster/MMO, phí generation, multi-agent bắt buộc, stop hook, lịch chụp 5 phút hoặc build liên tục của Oldcraft vào game puzzle này.

## Những lỗi áp dụng cần nhận rõ

1. Tự thu nhỏ clone thành core-loop với 7 level trước khi có fidelity contract chi tiết. Việc đó phù hợp checkpoint thử cơ chế, chưa đủ thay thế mục tiêu clone của owner.
2. Thiếu behavior matrix dù chính skill đã yêu cầu. Camera, targeting, cadence, fail timing và UI được ước lượng; chưa có runtime gốc để hiệu chuẩn.
3. Có test luật và scenario nhưng chưa hoàn thành review hình ảnh/feel/audio. Ảnh cũ không xác nhận source pig-facing mới. Một scenario pass không biến capture runner fail thành pass.
4. Mất nhiều vòng xử lý build/exit; đáng lẽ cần timebox và quay lại công việc fidelity độc lập. Owner đã dừng build; yêu cầu đó tiếp tục có hiệu lực.

## Phần đã sửa và phần vẫn chưa xong

Đã bổ sung input pack có truy vết và sửa intake của skill trong sibling toolkit để project clone không đi từ brief mỏng thẳng đến khẳng định hoàn tất. Đây là sửa tài liệu/quy trình, không phải evidence game mới. Chưa hoàn thành: capture/đo game gốc cùng state, current Editor visual review, nghe mix, UI loss/edge scenarios, cơ chế cao, device/perf và owner review. Các tài liệu mới vẫn chứa đề xuất có nhãn, không dựng lại lịch sử như đã được duyệt từ đầu.

## Cập nhật sau lượt tiếp tục cùng ngày

Đã sửa source presentation/reset, chạy 20 rule/data/save tests và 2 Editor fixtures, mở preview ở 3 aspect, kiểm kê feature nguồn và xem 3 demo nhúng có chọn lọc. Những việc này bổ sung evidence sau audit, không thay đổi kết luận về việc intake ban đầu chưa đầy đủ. Xem [CURRENT_STATE.md](CURRENT_STATE.md) và [review mới](Evidence/editor-review/REVIEW.vi.md) để phân biệt phần đã xác nhận với gap còn mở; player build, input vật lý, audio listening và fidelity gốc vẫn chưa được duyệt.
