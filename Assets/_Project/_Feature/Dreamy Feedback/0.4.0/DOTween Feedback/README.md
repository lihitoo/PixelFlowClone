# DOTween Feedback (optional sample)

Sample adapter cho `com.dreamy.feedback`; không cần cài một package/repo Feedback DOTween riêng. Code được import vào project nên bạn có thể chỉnh animation theo game. Package Feedback nền vẫn chạy bằng Unity backend khi không import sample này.

## Cài và dùng

1. Cài DOTween và hoàn tất setup của DOTween trước. Assembly của sample tham chiếu **DOTween.dll**; nếu distribution của bạn dùng asmdef khác, sửa `Runtime/Dreamy.Feedback.Dotween.Runtime.asmdef` cho phù hợp.
2. Trong **Window → Package Manager → Dreamy Feedback → Samples**, import **Basic Feedback** (rig/preset/preview), rồi **DOTween Feedback** (adapter). Import TMP Essential Resources nếu chưa có.
3. Thêm component `DotweenFeedbackProvider` lên `GameFeedbackRig` trong scene, hoặc prefab variant riêng của game. Gán component vào **FeedbackHost → Animation Provider** trước khi bật rig / initialize host.
4. Chạy preview để thử Coin, Star, Energy, Gem và năm kiểu icon fly. Gameplay vẫn gọi `IFeedbackService` / primitive service và nhận `FeedbackHandle`; không cần dùng Tween/Sequence trong gameplay.

Provider thay implementation của Icon Fly, UI Punch, Camera Shake, Floating Text và Screen Flash. VFX và haptic tiếp tục dùng service của rig. Không cần rebuild hay sinh lại rig. Camera Shake cần camera được gán vào Host hoặc truyền qua `host.Initialize(camera)`.

Sample không kèm thư viện DOTween. Đã kiểm tra với distribution `com.demigiant.dotween` 0.0.3 chứa DOTween.dll. Distribution này còn chứa module Audio/Physics/Physics2D: bật các Unity module tương ứng hoặc tắt module DOTween không dùng trong một project tối giản.

## Chỉnh icon fly

| Style | Typical use | Tuning |
| --- | --- | --- |
| Straight | Fast HUD collection | InCubic ease, .4–.6 s, low stagger |
| Arc | Coin/chest reward | ArcHeight 100–200 canvas units, .6–.9 s |
| ScatterMagnet | Match-3 clear, resource shower | Spread 60–120, .7–1 s; 30% scatter then accelerate to target |
| Fountain | Level win/star burst | ArcHeight 160–260, Spread 100, .9 s |
| Spiral | Booster/energy collection | Spread 50–100, two turns, .8–1.1 s |

All styles expose Count, Stagger, Duration, Size, Spread, ArcHeight, Spin and easing in authored node options. Target anchors follow live UI positions. Scale/pop/spin/fade use a shared trajectory evaluator so the fallback backend preserves layout and endpoint behavior. The DOTween implementation owns a sequence per leased icon; scatter uses distinct scatter and collect tweens.

Resource presets play icon collection followed by destination punch. Compose extra VFX/text/haptic nodes in your own definition when needed. Complete fast-forwards the request; Stop returns its icons to the pool. Play requests concurrently to compare independent leases. Neither animation completion nor icon arrival grants wallet resources.

## Chuyển từ adapter package cũ

Bỏ dependency và testables `com.dreamy.feedback.dotween` khỏi project manifest, rồi import sample này. Không giữ đồng thời adapter cũ và sample: chúng có cùng tên assembly và GUID script. Tên namespace/assembly và GUID được giữ để provider trên rig vẫn liên kết khi chuyển vị trí.

Trước khi xóa sample hoặc gỡ DOTween, bỏ assignment Animation Provider và component DotweenFeedbackProvider khỏi rig; host sẽ dùng Unity backend. Thư mục Tests là regression PlayMode cho adapter, không đi vào player build.
