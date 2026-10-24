# Shop — brief trước triển khai

Yêu cầu: tiếp tục clone Shop giống giao diện và chức năng; IAP thật để SDK riêng tích hợp sau. **Owner cho phép fake tạm trong gateway.** Không cài SDK billing, không build player, không thay dữ liệu game thành số dư của package khác.

## Nguồn và trạng thái

- Target XAPK0.36.3/versionCode410; hash và catalog toàn bộ ở `../BRIEF.vi.md` và `../../AssetCatalog/manifest.json`.
- Ảnh gốc đã tải/xem: `../Research/images/shop-analysis-01.png` có bốn trạng thái cuộn/progression; `first-hour-02.png` có Shop level74 và giá USD. Bài phân tích gốc: https://www.gamigion.com/whats-wrong-with-pixel-flows-game-shop/ ; https://balancy.co/blog/2026/03/20/pixel-flow-shop-liveops-monetization-patterns/ . Không khẳng định web reference này đúng build0.36.3.
- Prefab0.36.3: Shop Page Variant, UI Shop/Mini Shop, Shop Gold View, Shop Bundle View và các variant No Ads/Blue/Purple/Yellow, UI IAP. Chỉ đọc YAML/names/RectTransforms; không import dummy scripts AssetRipper. Dữ liệu claimable_shop_bundles_json/claimable_presets_shop_golds_json bị strip fields; không coi tên asset là recovered logic.
- Source sprite: mái Shop, section divider,6 gold icons, gold-card frame, giá/button, bundle/content frame và4booster/heart/NoAds. Mỗi asset được chọn phải có source ID/URI/hash, output và GUID/border.

## Đặc tả hình ảnh

Canvas720×1280 mở rộng theo aspect; nền xanh đậm, mái sọc cyan/blue, header lives/coin/settings, feed dọc có mask và quán tính, tab Shop được chọn. Coin grid3cột×2hàng; card nền vàng kem với coin icon/amount và nút giá xanh. No Ads banner trước Gold Packs khi đủ điều kiện; No Ads Bundles và Bundles phía dưới. Bundle Small xanh/Medium tím/Ultra vàng, nội dung coin +4booster +unlimited heart, tên/giá ở hàng dưới. Không giữ bốn nút đổi coin cũ trên Shop.

Ảnh EUR quan sát: gold1000/5000/10000/25000/50000/100000 với1.99/8.99/17.99/34.99/59.99/99.99; No Ads8.99; No Ads Bundle12.99,1000coin,4×1booster,1h; Small9.99,5000coin,4×1,2h; Medium22.99,10000coin,4×2,2h; Ultra39.99,25000coin,4×5,6h. Đây là reference config cho fake store, không phải giá live hoặc bằng chứng native0.36.3. Giữ đúng sự bằng nhau2h của Small/Medium trong reference, không tự rebalance.

Mốc bundle20 và NoAds theo HomeTuning hiện tại là ước lượng, phải gắn nhãn trong config/evidence. Không tự thêm timer giảm giá, quà Shop hằng ngày, Season Pass SKU hoặc banner mới không được reference chứng minh. Nội dung Pass/offerwall vẫn giữ trong whole-game backlog nếu cần đối chiếu tiếp.

## Graph chức năng và ownership

1. Home tab Shop/coin plus, NoAds dock và mini shop khi thiếu tài nguyên → cùng catalog; mở đúng scroll section; Back/Start/quay lại game giữ context. Scroll không click xuyên xuống Home/game. Header/settings/lives vẫn hoạt động và quay lại Shop đúng vị trí.
2. Offers chỉ hiển thị khi đúng unlock; NoAds/NoAdsBundle không bán lại khi đã có entitlement. Coin/bundle repeatable. Price/title/rewards là config qua Dreamy DataConfig; coin/booster/lives/entitlement/receipt IDs trong ProgressStore qua Datasave.
3. Một purchase in flight; request gửi qua contract `Dreamy.Shop.IShopPurchaseGateway`. Fake chỉ nằm trong gateway; mặc định success sau độ trễ ngắn, có điều khiển cancel/fail/deferred/approve/reject/restore qua fixture/Editor. Không trừ coin để mua IAP và không trừ tiền thật.
4. Gateway lưu order trước callback. Domain ghi **toàn bộ** coin+4booster+unlimited-lives+NoAds+transaction ID trong một lần save-before-publish. Chỉ acknowledge sau khi commit. Duplicate/replay/relaunch không grant hai lần; save lỗi giữ pending order để Retry/recover. Không dùng vòng grant từng reward của ShopModel vì có thể để bundle commit một phần.
5. Purchase loading/progress, success/Continue, cancelled/no reward, failure/Retry/close, pending/resolve, storage failure/recover, interruption/relaunch, restore success/empty/failure đều có luồng. Restore NoAds chỉ khôi phục entitlement; không regrant coin/booster/unlimited duration của bundle cũ.
6. Gateway fake là triển khai tạm được yêu cầu; SKU dùng namespace clone riêng. Adapter SDK sau này phải thay gateway và đưa receipt đã xác thực/acknowledgement/restore; không dùng credential/SKU server nguyên bản để charge.
7. Gameplay booster thiếu item mở popup mua bằng coin riêng nếu còn cần contract này; không đặt shop đổi coin giả vào feed IAP. Insufficient coin dẫn Mini Shop rồi quay lại đúng modal/game; các test cũ phải cập nhật theo entry mới, không chỉ đổi assertion cho qua.

## Quy trình kiểm tra

- Trước implementation: brief này, ảnh nguồn đã mở, source layout/candidate roles/package API được kiểm tra. Hiện có Dreamy Shop/Economy; không cần thêm package/SDK IAP.
- Rules/save/gateway: bundle atomic, duplicate receipt, repeat consumable, owned NoAds, insufficient/full inventory, overflow, cancellation/deferred, restore không regrant, disk failure và restart recovery.
- Real-scene UI pointer: Home→Shop→purchase→success→Home; scroll→bundle; failure/Retry/cancel; NoAds owned/restore; settings/back, mini shop từ lives/gameplay; repeat/relaunch. Fake setup được ghi rõ.
- Unity compile/Console và regression hiện có. Ảnh actual Game View được ghi riêng với preview URP3portrait aspect; so sánh reference/full feed bằng trang review. Không nhận100% native parity khi còn version/geometry/tuning chưa khớp.
