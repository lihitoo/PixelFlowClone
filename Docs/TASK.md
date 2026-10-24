# Pixel Flow — đặc tả clone và nghiệm thu

**Owner correction mới nhất:** target là clone hoàn thiện y hệt toàn game, không tự giới hạn offline. Bộ chuẩn bị áp dụng trước khi sửa toolkit nằm ở [ExactClone/BRIEF.vi.md](ExactClone/BRIEF.vi.md), [65 surface contracts](ExactClone/screen-contracts.json), [review ảnh](ExactClone/review.html) và [catalog từng asset](AssetCatalog/manifest.json). Các phần offline/checkpoint dưới đây là lịch sử; loading/Home/shop/rank/settings/lives/pass/event/account và mọi function của target đều phải được kiểm kê/hoàn thiện. Chưa có parity acceptance.

Ngày lập lại đầu vào: 2026-10-24. Đây là đặc tả làm việc được bổ sung sau checkpoint đầu tiên; không phải bản đã được chủ dự án duyệt trước khi triển khai. [PROJECT_BRIEF.md](PROJECT_BRIEF.md) là điểm vào, [CURRENT_STATE.md](CURRENT_STATE.md) giữ trạng thái thực tế duy nhất.

**Scope mới theo yêu cầu full gameplay/UI:** triển khai offline campaign2.600 màn, các cơ chế có schema nguồn, onboarding/help, booster, shop coin, daily, settings, lưu/chơi tiếp, thắng/thua/continue/retry và xử lý lỗi. Hợp đồng chi tiết và ánh xạ code→test hiện tại ở [FULL_GAMEPLAY_PLAN.md](FULL_GAMEPLAY_PLAN.md) và [FULL_FLOW_MATRIX.md](FULL_FLOW_MATRIX.md). Các cột “hiện tại” của bảng checkpoint7 màn bên dưới là lịch sử trước lần mở rộng này; không dùng chúng làm trạng thái mới. Giữ nguyên các yêu cầu fidelity/device chưa được đo.

## 1. Mục tiêu và mức độ giống

Yêu cầu của chủ dự án: clone game Pixel Flow từ XAPK đã cung cấp, bằng Unity, tạo project mới trong `D:/Game`, dùng game-vibe-workflow, cân nhắc template/package Dreamy. Chỉ dẫn mới nhất: **không build thêm**. Được tiếp tục đọc source, chỉnh project, kiểm tra compile và Play Mode.

Mục tiêu làm việc đề xuất: người chơi nhận ra vòng puzzle của Pixel Flow qua bảng pixel, heo theo màu và đạn, băng chuyền, hàng chờ và khu vực chờ; thao tác dễ đọc, luật nhất quán, hình ảnh và phản hồi có reference để so sánh. Clone đạt yêu cầu cần cả hành vi lẫn trình bày; sao chép level hoặc test luật xanh chưa đủ.

Phân biệt ba phạm vi:

- Đã được yêu cầu: clone game bằng Unity và tận dụng nguồn đã chọn.
- Checkpoint hiện có do agent tự thu hẹp: vòng chơi cơ bản, bảy level gốc 1–7, offline. Đây chưa phải định nghĩa hoàn thành toàn bộ clone.
- Phạm vi mở rộng đã được yêu cầu: full gameplay/UI; hợp đồng offline cụ thể và các điều chưa suy được từ native được ghi ở FULL_GAMEPLAY_PLAN/FULL_FLOW_MATRIX. Không đồng nhất “đã code/test contract” với clone nguyên bản100%.

Không tự thêm multiplayer, dịch vụ tài khoản, ads/IAP/liveops hoặc phát hành store. Các hệ thống đó có thể được kiểm kê làm reference nếu xuất hiện, nhưng không được ngầm bật tích hợp. Không phát sinh chi phí AI, cài REA hay nâng Unity chỉ để làm đầy brief.

## 2. Đầu vào và độ tin cậy

Nguồn chính: `D:/Downloads/Pixel+Flow!_0.36.3_APKPure.xapk`, SHA-256 `ad7b8a8f7659ec86a583976e466458b69fd58f08f2667f6f17925fa570e9bad2`. Extraction nằm ở `D:/Downloads/PixelFlow_0.36.3_extracted`; receipt chọn lọc trong [Evidence/asset-intake.json](Evidence/asset-intake.json).

Nguồn có cấu trúc cho biết level, màu, vị trí pixel, queue/ammo/capacity. Nó không tự chứng minh camera, tốc độ băng chuyền, animation, targeting hay thời điểm thua của ứng dụng gốc. Hai ảnh marketing đang có chỉ là anchor về nhận diện/composition, chưa phải capture gameplay đối chứng cùng level. Chưa có bộ clip tương tác gốc được đo và gắn timestamp trong project.

Đọc [BEHAVIOR_MATRIX.md](BEHAVIOR_MATRIX.md), [STYLE_BIBLE.md](STYLE_BIBLE.md) và [refs/manifest.json](refs/manifest.json). Các nhãn bắt buộc: observed-data, observed-description, estimated, unknown. Lựa chọn do agent đề xuất không được nâng thành owner-approved.

## 3. Hành trình người chơi cần kiểm tra

1. Mở scene `Assets/_Project/PixelFlow/Scenes/PixelFlow.unity`, Play trong Editor → Home, logo và nút chơi rõ ràng, trạng thái save được nạp.
2. Save mới → Play vào level 1. Save cũ → có thể chơi level đã mở, level chưa hỗ trợ không được hiện như đã hỗ trợ.
3. Đọc silhouette bảng pixel, màu ngoài biên, màu/đạn của heo đầu mỗi cột, số heo đang chạy và slot chờ còn trống.
4. Chạm/click heo đầu cột → heo lên băng chuyền nếu còn capacity. Heo sau cột không được chọn xuyên. Không cần kéo hay nhấn giữ để bắn.
5. Heo di chuyển, tự bắn pixel hợp lệ cùng màu; mỗi hit hợp lệ giảm đúng một đạn và một pixel. Cần thấy/nghe quan hệ nhân quả của phát bắn.
6. Hết đạn → heo rời vòng. Hết vòng nhưng còn đạn → về slot chờ; chạm lại để tái phóng khi được phép.
7. Pause → simulation dừng, UI modal chặn click xuống board; đổi sound, Resume hoặc Restart/Home.
8. Xóa pixel cuối → Win một lần, cập nhật tiến độ một lần, Next vào level tiếp theo được hỗ trợ. Không có level sau → trở về lựa chọn hợp lệ, không index lỗi.
9. Trạng thái kẹt/thua → lý do đủ đọc, Retry reset đúng cùng level, Home hoạt động. Timing thua gốc chưa được xác định; xem B07.
10. Về Home rồi Play lại/reload save trong kiểm tra Editor → completed/unlock/sound còn đúng. Không chạy lại player build để kiểm chứng ở giai đoạn này.

## 4. Hợp đồng màn hình và input

| Màn hình/trạng thái | Nội dung cần nhìn thấy | Tương tác và hậu điều kiện | Cần đối chứng thêm |
|---|---|---|---|
| Home | Logo, primary Play, level/progress có thể hiểu | Play đúng level; chọn level hợp lệ; không cho nhảy vào data không hỗ trợ | Bố cục Home gốc chưa có anchor runtime; level chips hiện tại là lựa chọn agent |
| Playing | Bảng pixel, belt, queue, ammo, 5 waiting slots ở tập level hiện có, pause/home | Click queue-front/waiting; capacity-full có phản hồi; modal đóng thì board mới nhận input | Camera, thứ tự thông tin HUD và các icon chưa match đầy đủ |
| Paused | Tên trạng thái, Resume, Restart, Home, sound | Một click gây một action; không có simulation/click-through | Layout/animation modal gốc unknown |
| Win | Kết quả, next/replay/home theo context | Khóa input puzzle; ghi completion một lần; next không vượt tập data | Celebration và nhịp chuyển gốc unknown |
| Lose | Lý do và retry/home | Không bắn/tiêu đạn sau terminal; retry không giữ session cũ | Điều kiện/độ trễ fail gốc cần capture |
| Transient | Đầy belt, không thể chọn, launch, hit, return | Phản hồi không thay đổi luật; không che ammo/target/input | Cường độ, thời lượng và SFX cần nghe/xem |

Mouse trái là input Editor tương đương tap; target touch thực tế chưa kiểm tra. Mốc layout đề xuất: 720×1280, thêm 1080×1920 và 1080×2340 bằng Game View; safe area không che HUD/queue. Nút chạm đề xuất tối thiểu 48 đơn vị UI tại reference resolution. Đây là tiêu chí thiết kế mới, không phải số đo game gốc.

## 5. Luật, data và tình huống biên

PuzzleSession là chủ luật; presenter/UI chỉ phản ánh và gửi yêu cầu. Static level giữ bất biến; session có bản sao mutable; progress/sound lưu riêng. Không import dummy C# từ AssetRipper thành logic.

- Queue: chỉ phần tử đầu hoặc heo ở waiting được chọn. Hai click sát nhau không tạo hai instance cho cùng heo; full-belt không mất heo/đạn.
- Shooting: không xóa pixel sai màu, không xuyên pixel chắn nếu đang dùng giả thuyết occlusion; nhiều heo cùng nhắm một ô không được trừ hai lần. Kiểm tra ở bước thời gian lớn và khi pause/resume.
- Movement: queue entrance không chồng nhau; turn/belt return có trạng thái liên tục; tốc độ không phụ thuộc FPS theo cách làm đổi kết quả luật.
- Return/loss: tách đầy waiting khỏi thời điểm heo quay về. Implementation hiện thua khi heo còn đạn quay về mà không có slot, hoặc hết mọi đạn khi còn pixel. Không gắn nhãn đây là timing chính xác của bản gốc khi chưa có evidence.
- Reset: retry/next/home dọn heo/projectile/âm đang thuộc session; callback session cũ không sửa board mới. Save unlock/completion không bị reset theo Retry.
- Level validity: ID màu hợp lệ, pixel không trùng ô, tọa độ trong board, capacity dương, đạn đủ theo từng màu; reject/ẩn level chứa feature chưa hỗ trợ thay vì bỏ field đặc biệt.

Tập hiện có: 1 (16×15,180 pixel), 2 (20×20,400), 3 (20×20,230), 4 (20×20,400), 5 (22×25,550), 6 (20×20,400), 7 (20×20,400); tổng 2.560 pixel. Palette 34 ID từ source. SlotCount/ConveyorLimit của tập này bằng 5; không hardcode giả định đó cho mọi level tương lai.

## 6. Nghiệm thu theo yêu cầu, tách khỏi trạng thái hiện tại

Các ID dưới đây dùng trong backlog và evidence. P0 là đề xuất chất lượng cho lát cắt đang làm, chưa phải owner chốt phạm vi cuối.

| ID | Mức | Điều kiện đạt và cách kiểm tra | Bằng chứng hiện có / thiếu |
|---|---|---|---|
| C01 | P0 | Normal entry → Home → level 1 bằng input UI thật, không gọi thẳng win | Source Editor route mới pass qua bootstrap/InputSystem/EventSystem có bơm rõ; input vật lý/normal player scheduling chưa xác nhận |
| C02 | P0 | Queue-front/waiting launch, full-belt và double-click không mất state | Rule fixtures và Editor route xác nhận rear rejection, repeated moving-pig selection, full-belt không mất queue/ammo |
| C03 | P0 | Hit đúng màu, ammo/pixel giảm 1, không duplicate/occlusion sai | 20/20 suite là bằng chứng luật hiện có; targeting tương đương gốc còn unknown |
| C04 | P0 | Return/relaunch và loss/retry đúng trạng thái, không terminal hai lần | Luật có tests; gold path hiện có thấy return, chưa chứng minh UI loss đầy đủ |
| C05 | P0 | Cả 7 level clear được và source data không bị đổi | 7 source-level fixtures trong suite; chưa có manual UI playthrough cả 7 |
| C06 | P0 | Pause chặn simulation và click-through; resume/retry/home không rò session | Editor route pass pause blocking, same-frame board deactivation, audio cleanup và3 Home/Play cycles; receipt phân biệt direct button event với pointer |
| C07 | P0 | Completion/unlock/sound round-trip, save mới và cũ không lẫn test save | Source rule/save tests và Editor route mới pass save riêng, win/unlock reload, sound toggle; không đụng save người dùng |
| C08 | P0 | Pig nhìn được mặt, ammo đọc được, không clipped/overlap ở 3 aspect trên | Đã mở offscreen preview level 1 tại3 aspect, sửa face/scale/waiting spacing; Game View, variant level và device safe area vẫn chưa đủ |
| C09 | P0 | Board/belt/pig/UI so side-by-side với đúng reference và state; ghi sai khác | Hai anchor marketing và capture cũ; chưa có đối chứng runtime cùng level; không pass |
| C10 | P0 | Tap/launch/hit/win nghe được, sound-off chặn feedback, không clipping do spam | Bốn clip đã gắn; chưa nghe kiểm tra mix runtime |
| C11 | P0 | Nhịp launch→shot→hit→return rõ ở tốc độ thường; thông số gốc có provenance | Timing đang ước lượng, chưa đo gameplay gốc hoặc so clip |
| C12 | P0 | Compile/Play Mode source mới: không error, missing refs/material; input thật còn dùng được | Source compile và20 rule+2 Editor fixtures pass; cleanup phục hồi input settings/devices; input vật lý vẫn cần review riêng |
| C13 | P1 | Kiểm kê feature đặc biệt, chọn một feature có source sample rồi spec/test/integrate | Inventory3117definitions và FEATURE_MAP đã có; Surprise8/Connected14 chưa triển khai |
| C14 | P1 | Onboarding/Home/result có reference runtime và được match có chủ đích | Bố cục hiện là agent thiết kế; chưa chốt target |
| C15 | P1 | Mobile touch/safe area/frame-time/memory trên device đã chỉ định | Chưa chỉ định device và chưa đo; không suy FPS từ tests/recording |
| C16 | Deferred | Player artifact sau sửa source chạy normal launch và đủ capture | Build bị chủ dự án dừng; không chạy, không đánh dấu pass bằng artifact cũ |

Bằng chứng cũ: [Evidence/editmode-final.xml](Evidence/editmode-final.xml), [Evidence/player-final10/smoke.json](Evidence/player-final10/smoke.json). Runner của artifact đó [Evidence/player-final10/runner.json](Evidence/player-final10/runner.json) vẫn FAIL vì thiếu capture cuối; 12 scenario pass không biến runner thành pass. Owner acceptance của toàn bộ clone vẫn pending.

Bằng chứng source mới: [20 rule/data/save tests](Evidence/editmode-tests-20261024-105312.xml), [2 Editor scene fixtures](Evidence/editmode-review-20261024-105022.xml), [route receipt](Evidence/editor-review/verified-source-review-20261024/route.json) và [review](Evidence/editor-review/REVIEW.vi.md). Preview dùng URP offscreen và input fixture có explicit pumping, không phải xác nhận native input, thiết bị, audio listening hay fidelity. Lượt `editmode-tests-20261024-104702.xml` đã chọn nhầm review với Null graphics vì filter lỏng; không dùng nó làm bằng chứng hình ảnh. Filter và GPU guard đã được sửa, kết quả mới tách20/2 case.

## 7. Kiến trúc, asset và điều kiện làm việc

Giữ template commit `f6486d0e8fa7e877360d7b775c96ad419474fe0f`, Unity 6000.4.12f1, package pin hiện có. Quyết định template chi tiết ở [TEMPLATE_DECISION.md](TEMPLATE_DECISION.md). Không thêm package mới vì Oldcraft có. Giữ game code ở `Assets/_Project/PixelFlow`, Runtime/Editor/Rules/test tách assembly. Thay source art phải giữ GUID và kiểm tra binding/import, không chép toàn bộ extraction vào Assets.

Asset route hiện tại: existing cho pig/texture/UI/font/SFX/data, procedural cho board/belt/projectile/layout. [refs/asset-roles.json](refs/asset-roles.json) nối role → runtime asset → receipt → bước kiểm tra. AI là route có thể chọn sau khi có gap cụ thể và pilot; không cần gen khi asset có sẵn phù hợp. REA auto chỉ phục vụ câu hỏi hành vi có target cụ thể; hiện chưa cần setup để hoàn thiện bộ spec.

Deadline/freeze cuối, target device và ngưỡng performance chưa được cung cấp. Không bịa số FPS hay chi phí đã chi. Không phát sinh paid jobs. Đề xuất timebox cho một blocker tooling: tối đa 2 lần thử có thay đổi giả thuyết, tổng khoảng 30 phút rồi ghi kết quả/fallback; không để build-debug lấn việc match game. Lệnh dừng build của owner có ưu tiên cao hơn timebox và các gate cũ.

## 8. Thứ tự tiếp tục và định nghĩa hoàn thành

Đọc [TODO.md](TODO.md). Hoàn thiện đầu vào không có nghĩa game đã giống: bước tiếp theo là thu/định danh reference runtime, đo những thông số ảnh hưởng C03/C04/C09/C11, review source trong Editor, sửa nhóm lỗi nhìn/thao tác lớn nhất, rồi mới mở feature/level. Một lát cắt chỉ được gọi là đạt khi mọi P0 có bằng chứng phù hợp hoặc ngoại lệ được chủ dự án chấp nhận rõ. Hiện chưa đạt điều kiện đó; build gate được hoãn theo chỉ dẫn, không phải ngoại lệ đã xác nhận chất lượng.
