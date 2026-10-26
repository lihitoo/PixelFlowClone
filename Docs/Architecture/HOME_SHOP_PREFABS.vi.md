# Home / Shop — prefab authoring, 2026-10-24

Home dùng trực tiếp `Assets/_Project/Prefabs/Panel/HomePanel.prefab` và `Dreamy.Template.Home.HomePanel`. Shop dùng trực tiếp `_Feature/Dreamy Shop/0.1.2/Shop Feature/ShopPanel.cs` và `Prefabs/ShopPanel.prefab`. Giữ GUID của ba asset gốc HomePanel, ShopPanel và ShopOfferItem. Không có constructor runtime dựng cây Home/Shop hay fallback dựng UI khi Addressables lỗi.

`PixelFlowApp` nhận Home từ `PanelManager.Create<HomePanel>("Panel/HomePanel.prefab")`. View map/chrome là reference serialize trong HomePanel; panel phát intent, controller hiện có quyết định navigation/progress. Shop mở qua cùng PanelManager với key `PixelFlow/Panel/Shop.prefab`, nay trỏ thẳng prefab sample. Factory của root inject service/controller vào `ShopScreenPresenter`; presenter dùng `Dreamy.Shop.ShopPresenter` cho giao dịch. Header và feed binding nằm trong partial `ShopPanel.Feed.cs`, không còn `PixelFlowShopPrefabView` hay `IShopViewAdapter`.

Sáu gói Gold dùng chung sample ShopOfferItem đã sửa skin, bind icon/giá/reward từ catalog. Năm prefab No Ads/bundle còn lại là các skin khác nhau; không còn sáu prefab Gold theo SKU hay variant root PixelFlowShopPanel cũ. Những prefab skin giữ GUID hiện có; code item/panel không bị copy thành class thứ hai. Không đổi package pins và không sửa PackageCache.

Canvas UI MainScene: Scale With Screen Size, 1080×1920, Match Width Or Height 0.5. HomePanel và ShopPanel có Safe Area dùng Dreamy.UI.SafeArea; nền bleed nằm ngoài. Icon/node/Play/roof dùng Simple và giữ tỉ lệ. Frame/bar có vùng giữa liền mạch dùng Sliced khi border phù hợp; đường nối level dùng Simple kéo theo chiều dọc, pattern dùng Tiled. Không suy ra Sliced chỉ từ `preserveAspect=false`.

Font Home/Shop là Lilita One SDF và atlas gốc đã trích xuất, không import dummy C#. Script reference font được remap sang TMP_FontAsset thật trong Unity; preset outline/shadow dùng atlas tương ứng. Provenance và hash ở `home-shop-font-intake.json`. Các sprite hiện có giữ nguyên source intake tại Docs/ExactClone/Home và Shop.

Giữ flow BootstrapScene → GameInstaller.Ready → GameInit/SceneLoader/loading → MainScene. Một root, một ProgressStore/wallet; fake IAP chỉ ở gateway đã được owner cho phép. Không build player. Popup settings/lives/purchase hiện có vẫn dùng renderer modal chung; phần chuyển các modal và gameplay HUD sang prefab riêng là công việc tiếp theo, không coi là đã chuyển toàn bộ UI của game.

Khi merge sample upstream: giữ HomePanel.Map và ShopPanel.Feed, serialized slots, button references, safe area, material và address keys; merge lifecycle OnEnable/OnDisable/OnDestroy cùng factory overload. Kiểm compile, reopen/teardown, pointer routes, restore/recovery và ảnh render. Không import một bộ sample assembly/class trùng.

Ảnh web Home và Shop là reference composition, chưa xác định đúng version 0.36.3. Remote rank/pass/event/account và billing SDK thật giữ nguyên gap đã ghi trong whole-game brief; không dựng dữ liệu online giả. Xem kết quả kiểm tra và cặp ảnh ở `home-shop-review.html` và Docs/CURRENT_STATE.md.
