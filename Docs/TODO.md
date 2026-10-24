## Architecture migration ? owner priority2026-10-24

- [x] Preserve template BootstrapScene (Init) ? GameInstaller/GameInit/SceneLoader ? MainScene; keep scene GUIDs/build order/Editor Play entry.
- [x] Use optional composition profile on existing GameInstaller; one progress authority exposed through Economy adapter.
- [x] Reuse imported ShopPanel/ShopOfferItem/CoinHolder, presenter installer/factory, Addressables and tween components; remove old runtime Shop entry.
- [x] Replace custom audio pool with Dreamy profile/library/service; remove competing AudioBootstrap sample component and apply supported volume semantics.
- [x] Document local sample patches, upgrade/merge policy and toolkit skill reuse rules.
- [ ] Finish final consumer regression evidence, normal Game View comparison and remaining parity gaps.
- [ ] Migrate Home/HUD/common popup/settings presentation to appropriate existing prefab/panel/feature capabilities.
- [ ] Real IAP/SDK, player/device/store gates remain deferred by owner; no player builds.

Historical task list follows; current architecture takes precedence. See Architecture/DREAMY_INTEGRATION.vi.md and CURRENT_STATE.md.

# Hàng việc ưu tiên

Ưu tiên mới theo owner: đọc [brief exact-clone](ExactClone/BRIEF.vi.md) và [review](ExactClone/review.html), khép các gap source/version/screenshot, sau đó thay shell/loading/Home/settings/shop và hoàn thiện rank/lives/pass/events/account theo graph65 luồng. Bảng offline dưới đây là checkpoint cũ, không phải phạm vi được phép tự loại các chức năng đó. Catalog từng file đã có ở [AssetCatalog](AssetCatalog/manifest.json). Báo trước nếu cần chuyển từ batch sang Unity/MCP tương tác; không build player.

Trạng thái chạy và next action duy nhất nằm ở [CURRENT_STATE.md](CURRENT_STATE.md). Đây là thứ tự công việc, không phải danh sách đã được owner chốt hết. Không build player theo yêu cầu mới nhất.

| Thứ tự | Việc / tiêu chí | Đầu ra và cách xác nhận | Trạng thái |
|---|---|---|---|
| 1 | Full offline gameplay/UI | FULL_FLOW_MATRIX nối code→test; hướng dẫn, chọn màn, booster, shop/daily/settings, save/resume, win/loss/continue/retry/error | Đã triển khai;6 Editor fixtures pass; xem CURRENT_STATE cho XML mới nhất |
| 2 | Catalog và cơ chế nguồn |2.600 màn,12 nhóm shooter,24 board kinds, health/reveal, chunk lazy load | Đã nhập và validate toàn bộ; có snapshot/family tests và preview8 màn cao. Không phải chứng nhận solve/fidelity toàn campaign |
| 3 | Transaction và recovery | Save-before-publish; rollback model nếu không ghi được; không duplicate reward/claim | Store tests và injected-save-failure UI fixture pass |
| 4 | Trình bày dễ đọc | Nhãn HP/cage/hidden/ice/support, link/slot/door, booster icons;3 aspect early-level preview | Đã sửa overlap nhãn cage và xem lại preview. Shape/animation/timing nguyên bản còn mở |
| 5 | Đối chứng native nâng cao | Cùng level/state/launch order, đo hướng, lớp, support event, shrink và targeting | Chưa có đối chứng live đủ; giữ inferred contract rõ trong FULL_GAMEPLAY_PLAN |
| 6 | Audio/feel | Nhạc menu/level OGG, SFX owner và settings; nghe normal speed, đo cadence | Đã nối nhạc/settings; listening và original cadence chưa xác nhận |
| 7 | Touch/lifecycle/device/perf | Target device, safe area, app background/kill/relaunch, memory/frame time | Not-run trên device; không suy FPS từ Editor |
| 8 | Player build/handoff | Chỉ build lại khi owner yêu cầu; giữ native quit debt đúng version | Deferred-by-owner; không tạo artifact mới |

## Quyết định và sai lệch cần giữ qua lần resume

- D01: dùng Dreamy template tương thích Editor hiện có, source facts ở TEMPLATE_DECISION; không kéo thêm package theo danh sách org.
- D02: bảy level hiện tại là checkpoint agent chọn, chưa phải scope cuối được owner phê duyệt.
- D03: original asset/data + procedural geometry; không có nhu cầu paid AI hiện tại; REA auto không đồng nghĩa setup.
- D04: không build thêm; các prompt/gate cũ nhắc early/final build không override điều này.
- D05: native Windows quit workaround là technical debt version-specific, không đưa thành quy trình mặc định; xem CURRENT_STATE và native-exit-diagnosis. Không mở lại nhánh debug này khi chỉ đang review spec/hình ảnh.
- D06: nếu một tool blocker lặp lại, đổi giả thuyết và giới hạn khoảng 30 phút / 2 lần thử cho batch; ghi gap và tiếp tục việc độc lập. Đây là giới hạn làm việc đề xuất mới, không phải lịch đã từng được tuân thủ.
- D07: yêu cầu full gameplay/UI đã mở rộng scope offline;7 level là lịch sử. Giá/reward, booster charge và các semantics chưa có native code là tuning/contract của clone, không phải số đo game gốc.
