# Dựng lại Home Pixel Flow

Owner đã yêu cầu thực hiện Home theo brief đã mô tả. Không build. Giữ gameplay/progress cũ và dùng source art; không dùng screenshot làm giao diện.

## Evidence và giới hạn

- Đúng target asset: XAPK0.36.3, `Start Page Variant.prefab`, source RectTransform ở `source-home-layout.json`; cây level, nút Play, dock trái/phải được giữ trong export.
- Source art: background/pattern, level nodes/labels/glow/column, vàng Play, heart/coin/plus/gear và bottom tabs. Nguồn từng file và imported GUID được ghi trong `home-asset-intake.json` và catalog hiện có.
- Ảnh kiểm composition: `../Research/images/ui-triptych.jpg` và `first-hour-02.png`. Home36 có current Very Hard,37 normal,38 Hard; data0.36.3 cũng khớp các difficulty này. Ảnh không có version stamp0.36.3, nên chưa khẳng định pixel-perfect native0.36.3.
- AssetRipper Image/Text/Button là dummy không có binding field; chỉ dùng hierarchy/RectTransform như structural evidence. Không import dummy assemblies.

## Trạng thái và control phải chạy

| Thành phần | Contract |
|---|---|
| Home shell | Nền gradient/pattern gốc phủ màn; safe area; không còn logo/Hero/tagline/menu grid. |
| Level path | Node hiện tại và phía trước, màu/difficulty theo catalog; đường vàng, current glow/pulse; scroll dọc hữu hạn; current snap khi Home/return. Future tap giải thích khóa, không đổi progress; completed tap có thể chọn replay qua data hiện có. Replay là khả năng clone đã có, chưa đo native. |
| Play | Nút vàng gốc, chữ Play; tiếp tục snapshot nếu cùng màn; đổi màn giữ replacement confirmation. Không cấp tiến độ/coin để tạo ảnh. |
| Header | Heart/max/timer/infinite khi có, coin/plus và gear. Coin là wallet thật; Heart là state local có owner riêng/migration, không vẽ số giả. |
| Lives | Regeneration theo UTC persisted; max5 từ ảnh; interval/refill cost chưa có config native đầy đủ nên tách tuning/assumption. Refill chỉ trừ khi hợp lệ và ghi save thành công. Empty life có refill/close/error, không tự grant. |
| Bottom bar | Shop, Start và rank gated; dùng icon/bars/selected gốc. Shop mở luồng đang có rồi Back về Home; Start reset focus; rank không tạo bảng người chơi giả. |
| Dock | Chỉ hiện entry theo progression/config. Asset gốc cho No Ads/pass/Fire Quest/Arena/daily. Khi destination cần service chưa có, hiện trạng thái unavailable/retry/close rõ ràng, không claim hoặc purchase giả. |
| Gear | Modal settings Home có music/sound slider và haptic, các action liên quan được nối hợp lệ; close/back chặn click-through. Không thay thế phần gameplay Pause ngoài scope khi không cần. |
| Navigation | Return Home giữ wallet/life/profile; không có colliders board còn sống; backdrop/modal chặn Play; không save-per-frame. |

## Tuning chưa có bằng chứng native

Không dùng mốc unlocked hoặc price trong một ảnh khác version như measured fact. Threshold dock/rank, heart interval/refill là dữ liệu cấu hình clone được gắn nhãn estimated trong evidence. Nếu còn phụ thuộc service hoặc reference chưa xác nhận, không công bố “Home y hệt100%”. Không loại luồng khỏi scope để che phần thiếu.

## Cách xác nhận

1. MCP preflight đã thấy interactive Editor6000.4.12f1 đúng project/scene, not playing, scene clean,0 console error. Không mở thêm instance.
2. Compile/Console sau C#; test deterministic lives/save/migration/insufficient coin. Giữ regression rules/data/save hiện có.
3. Fixture UI pointer/synthetic input: fresh/current36/late progress chỉ trong save cô lập; Play/return, map locked/scroll/replay, header plus/refill, settings close/mute/volume, bottom tabs/dock unavailable/retry.
4. Game View trong Editor đang chạy qua MCP/Editor-supported capture;3 portrait aspect previews ghi riêng với real Game View. Không gọi offscreen bằng chứng là device test.
5. Mở source/candidate side by side, kiểm pattern/node/button/header/nav/spacing; sửa rồi chụp lại. Record source version và progression differences; không dùng raw pixel score cho ảnh khác state.

Những screenshot/service chưa xác nhận vẫn là gap; hoàn thành UI/client flow khác với native/backend equivalence.
