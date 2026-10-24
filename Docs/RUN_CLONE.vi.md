# Ch?y Pixel Flow qua lu?ng template

1. M? project b?ng Unity6000.4.12f1, ch? Package Manager v? compile ho?n t?t.
2. M? `Assets/_Project/Scenes/BootstrapScene.unity` r?i Play. ??y l? Init scene c?a template: GameInstaller ? Ready ? GameInit ? SceneLoader/loading ? MainScene ? Home. Editor Play entry v? build settings d?ng c?ng th? t? Bootstrap/Main.
3. Kh?ng ??t th?m installer ? Main ho?c d?ng scene PixelFlow c? l?m entry. Main d?ng Canvas/PanelManager/layers c?a template v? controller game; sample HomeBootstrap b? disable. AudioBootstrap m?u ?? b? v? root/profile s? h?u audio.

## Thao t?c

Home ch?n level r?i Play; click/ch?m heo kh? d?ng ?? ??a l?n b?ng chuy?n. Pause/Back m? menu; Resume/Retry/Home v? save/resume thu?c game. Booster kh?ng ?? m? popup mua b?ng coin; thi?u coin d?n t?i Mini Shop r?i tr? v? ??ng ng? c?nh.

Shop d?ng prefab ShopPanel/ShopOfferItem/CoinHolder, Addressables v? presenter/tween c?a Dreamy. IAP hi?n fake qua gateway theo y?u c?u: success/cancel/fail/deferred/restore/receipt recovery. Kh?ng k?t n?i SDK thanh to?n th?t; product ID/gi? hi?n ch? thu?c catalog clone. Home/settings/lives/rank/events v? m?c ?? gi?ng b?n g?c xem [brief](ExactClone/BRIEF.vi.md) v? [tr?ng th?i](CURRENT_STATE.md), kh?ng coi m?n h?nh m? ???c l? parity ho?n t?t.

## Save v? d? li?u

ProgressStore l? ngu?n coin/inventory/ti?n tr?nh/settings duy nh?t qua Datasave; adapter Economy kh?ng t?o wallet th? hai. Save th??ng ? `Application.persistentDataPath/PixelFlowCloneSaves`; c?c review/test d?ng th? m?c ri?ng `PixelFlowCloneSmoke_*`. Campaign/catalog thu?c DataConfig, session ch? gi? tr?ng th?i puzzle hi?n t?i.

## Ki?m tra kh?ng build

- `python -B Tools/verify_project.py`: ki?m tra source boundary v? hash/GUID asset ch?n l?c.
- Unity: Window > General > Test Runner > EditMode > Run All. Fixture UI b?t ??u t? Bootstrap, ch? loading ??ng, d?ng input/save ri?ng v? ph?c h?i sau ch?y. Fixture t?m ??ng r?i m? l?i Simulator ?? tr?nh ch?n Mouse gi?; Game View ph?i c? graphics device. XML v? JSON/PNG b?ng ch?ng ???c gi? ri?ng.
- `Pixel Flow/Shop Review/Prepare Test Evidence` t?o run m?i tr??c khi ch?y l?i ?? kh?ng ghi ?? ?nh c?.
- Khi review b?ng tay: m? Bootstrap ?? l?u, d?ng `Pixel Flow/Home Review/Begin Isolated Review`; k?t th?c b?ng `End Isolated Review`. Ch? c?c menu review c? guard save ri?ng m?i ???c ??t fixture/gateway outcome.

?nh render offscreen c?a test ch?ng minh layout, kh?ng ph?i ?nh player/device hay b?ng ch?ng gi?ng game g?c. C?n ??i chi?u th?m Game View v? reference native.

Kh?ng build player theo y?u c?u. C?c menu build ch? d?nh cho khi ch? d? ?n y?u c?u l?i; ch?a x?c nh?n device/store SDK/content build ho?c ship-ready.

Xem [lu?ng t?ch h?p/c?p nh?t](Architecture/DREAMY_INTEGRATION.vi.md) v? [k? ho?ch reuse](Architecture/DREAMY_REUSE_PLAN.md) tr??c khi s?a scene, panel, save ho?c n?ng package/sample.
