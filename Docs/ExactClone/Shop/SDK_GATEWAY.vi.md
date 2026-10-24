# Điểm thay SDK IAP về sau

Owner đã yêu cầu fake tạm trong gateway và sẽ tích hợp SDK riêng sau. Hiện không cài Unity IAP hay gọi dịch vụ thanh toán thật.

`IPixelFlowStoreGateway` kế thừa contract đang có của `Dreamy.Shop.IShopPurchaseGateway` (package pin3fee66cfd839d2d626e5c15878dcf0a440c413f9). Đã đọc manifest/lock, source interface/request/result và ShopModel. Game dùng request/result của package; không dùng vòng grant từng reward của ShopModel để tránh commit một phần bundle. `PixelFlowShopService` giữ khóa purchase, kiểm tra offer/progression, phục hồi order và gọi giao dịch nguyên tử của ProgressStore. Không tạo wallet/save owner thứ hai hay chạy GameInstaller template song song.

Để thay SDK, cung cấp triển khai mới của gateway ở composition root `PixelFlowApp.InitializeShop`. Gateway phải:

1. Map offer ID và product ID của clone sang sản phẩm đã cấu hình trong SDK. Các ID `pixelflow.clone.*` hiện là ID fake riêng; không phải SKU của game gốc.
2. Chỉ trả `Purchased(transactionId)` khi có kết quả/receipt đã xác thực theo SDK. Không lấy reward từ callback/UI; domain tra catalog theo offer/product. Giữ order được duyệt trong `Unfulfilled` cho đến acknowledge.
3. Tách cancelled/failed/deferred. Cancellation của màn hình/app không được làm mất giao dịch đã được store chấp nhận. Khôi phục pending orders sau relaunch; mã giao dịch phải ổn định, duy nhất giữa các sản phẩm/store.
4. Chỉ consume/acknowledge sau khi ProgressStore đã ghi tất cả phần thưởng và transaction ID thành công. Callback lặp/recovery phải an toàn. Không xóa lịch sử dedupe khi reset UI.
5. Restore trả entitlement NoAds có bằng chứng; không grant lại coin, booster hay gia hạn unlimited của bundle cũ. Các consumable chưa acknowledge phải đi qua delivery/recover riêng.

FakeShopGateway lưu ledger dưới key `pixel-flow-fake-store`, tách khỏi profile game nhưng vẫn qua cùng DatasaveService. Mặc định success sau500ms. Điều khiển NextOutcome Success/Cancelled/Failed/Deferred, ResolveDeferred và FailRestore dùng cho fixtures. Các menu `Pixel Flow/Shop Review` chỉ điều khiển gateway trong save review cô lập, tránh chạm save người dùng khi thao tác QA.

Catalog `pixelFlowShop.json` đi qua Dreamy DataConfig; chứa11offer từ ảnh tham chiếu, EUR display prices và nguồn/ước lượng rõ ràng. SDK thật phải cấp metadata giá/tiền tệ đúng store; config EUR hiện tại không phải live price. Threshold/unlock/layout còn cần ảnh runtime đúng0.36.3 để nghiệm thu parity. Chưa thêm Season Pass SKU/flash deal/timer không có bằng chứng.
