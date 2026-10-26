# Shop — No Ads và toàn bộ gói, 2026-10-25

Theo chỉ dẫn mới, đã xóa hẳn nút Restore Shop Purchases khỏi ShopPanel.prefab, bỏ binding/event và khoảng trống footer của nút. Chức năng restore ở service/gateway được giữ để dùng từ Settings; đợt này không thêm UI Settings mới. Route Settings có sẵn được giữ và dùng trong regression.

Đã tìm và kiểm tra art No Ads gốc cùng sáu gold icon bản2× trong catalog trích xuất0.36.3. Các PNG được thay bằng bản2× cùng hình, giữ GUID/reference, Simple/preserve aspect và ghi lại source ID/URI/hash/nguồn trước đó trong ../shop-asset-intake.json. Không đổi SKU, giá, reward, mốc mở khóa hay cài billing/ads SDK.

Shop có11 offer:6 Gold packs, No Ads, No Ads Bundle, Small/Medium/Ultra Bundle. No Ads/No Ads Bundle bị ẩn khi đã sở hữu. Mốc mở khóa hiện tại vẫn là tuning ước lượng đã ghi trong brief; không tự cho rằng đây là threshold native. Khi review đủ11 gói, fixture cô lập seed level36 được ghi rõ.

Kiểm tra: editor-tests.xml đạt27/27. Có11 case đối chiếu reward/persistence của từng sản phẩm; pointer route mua lần lượt6 Gold packs,3 bundle và No Ads, cùng route riêng mua No Ads Bundle. Kiểm coin, cả4 booster, timer, entitlement, receipt/ack, mua lặp, cancel/error/Retry/deferred, restore không regrant và mini-shop return. Tất cả thanh toán dùng fake gateway đã được owner cho phép.

Ảnh mỗi bundle và No Ads ở Docs/Evidence/editor-review/shop-final-20261025; ảnh Game View và trang đối chiếu ở thư mục này. Preview URP và Game View readback được phân biệt trong receipts. Compile/Console và kiểm tra source/GUID ở static-validation.json/validation.json. Không build player; không tuyên bố thanh toán thật hoặc parity native0.36.3 đã được xác minh.
