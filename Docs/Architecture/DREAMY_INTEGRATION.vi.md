# T?ch h?p v? c?p nh?t Dreamy trong Pixel Flow

## Kh?i ??ng

Gi? nguy?n hai scene template/GUID: `Assets/_Project/Scenes/BootstrapScene.unity` l? Init; `Assets/_Project/Scenes/MainScene.unity` l? scene game. Build settings b?t ??ng th? t? ??. Editor Play entry l? Bootstrap. `GameInstaller` ch? ? Bootstrap, c?i persistence/infrastructure/config/feature/presenter r?i Ready. `GameInit` g?i SceneLoader v?i loading screen hi?n c?; Main d?ng service ?? c?i. Sample HomeBootstrap b? disable v? Home c?a game c? n?i dung ri?ng.

PixelFlowInstallerProfile l? composition hook t?y ch?n tr?n installer hi?n c?; default demo gi? khi ?? tr?ng. Main controller bind/release ? v?ng ??i scene, kh?ng serialize tham chi?u t? Init t?i Main. Khi ??i t?n scene ho?c address ph?i s?a constant/config/receipt v? ki?m tra load; kh?ng th?m fallback ?m th?m.

## Shop v? asset

?? s?a tr?c ti?p `ShopPanel`, `ShopFeatureInstaller` trong `_Feature` ?? h? tr? passive native view hook v? presenter factory t?y ch?n. Factory chung t?o ??ng m?t presenter cho m?i l?n m?. Khi PreHide, panel t?t raycast tr??c khi tr? v? m?n tr??c ?? tr?nh backdrop ?ang fade ch?n thao t?c. PixelFlowFoundationShopPresenter b?c presenter package ?? n?i header/restore; giao d?ch v?n thu?c service, kh?ng thu?c view. Kh?ng new presenter ? caller sau khi factory ?? bind.

`PixelFlowShopPanel.prefab` l? variant skin c?a ShopPanel nh?p s?n;11 item l? variant ShopOfferItem; CoinHolder d?ng ResourceUIHolder nh?p s?n. Addressables group PixelFlow UI c? key `PixelFlow/Panel/Shop.prefab`, m? qua PanelManager.Create/Show. JSON [binding prefab](shop-prefab-bindings.json) li?n k?t GUID/lineage/address; asset ngu?n c? receipt ri?ng trong Docs/ExactClone/Shop.

D?ng UITweenPlayer g?c c?a sample, Auto collection v?i UITweenFade Auto. Fade tr? CanvasGroup g?c c?a panel; kh?ng fade ri?ng nh?m con r?i ?? ancestor alpha0. Manual effect ch? ch?y khi ???c g?i ri?ng ho?c thu?c manual collection. Ki?m tra c? alpha/interactable c?a ancestor v? ?nh th?c, kh?ng ch? s? l??ng offer hay k?ch th??c PNG.

Source Shop builder c? ch? c?n d??i UNITY_EDITOR cho c?ng th?c authoring/chuy?n ??i; runtime controller lu?n d?ng Addressables prefab. ?? b? entry ShowShop c? kh?ng c?n caller. Kh?ng d?ng builder l?m fallback khi t?i asset th?t b?i.

## D? li?u, giao d?ch v? audio

ProgressStore gi? nguy?n save key/schema/codec v? l? ngu?n tr?ng th?i duy nh?t. PixelFlowResourceWallet th?ch nghi Economy contracts tr?n aggregate n?y, kh?ng c?i wallet th? hai. Commit save tr??c publish; notification l?i kh?ng bi?n giao d?ch ?? ghi th?nh failure. Fake gateway l?u receipt ri?ng nh? provider ledger, grant m?t l?n r?i acknowledge sau persistence. SDK th?t thay gateway sau, kh?ng ?i v?o presenter.

Dreamy Audio profile/library/cues thay pool AudioSource ri?ng. Component AudioBootstrap m?u b? x?a kh?i BootstrapScene c?a consumer v? GameInstaller/profile ?? s? h?u vi?c kh?i t?o audio; kh?ng ?? hai initializer ghi ?? profile theo th? t? Awake. Root initialize profile m?t l?n cho m?i v?ng ??i, gi? l?i instance khi retry. Profile-backed preferences l? ngu?n l?u; SetVolume kh?ng ghi PlayerPrefs l?n hai. B?n Audio hi?n t?i kh?ng nh?n m?t master bus to?n c?c: consumer ?p d?ng master ? music/sfx level ? bus t??ng ?ng, kh?ng g?i m?t master bus kh?ng t?n t?i. Controller ch?n cue ? Main; GameInit t?t nh?c sample cho profile n?y. Package owns global audio host; consumer gi?i ph?ng registration/subscription v? d?ng bus khi teardown.

## C?p nh?t sample/package

??c manifest/lock, README v? source revision ?ang c?i tr??c khi d?ng API. ?u ti?n s?a tr?c ti?p code/prefab sample c? tr?ch nhi?m ph? h?p; x?a ph?n superseded sau khi ki?m tra serialized reference. Kh?ng copy feature song song, kh?ng ch?nh PackageCache. Ghi patch c?c b? ?? merge khi n?ng sample: ShopPanel view hook, ShopFeatureInstaller presenter hook v? GameInstaller optional profile. Gi? meta/GUID, pin revision, tr?nh import assembly sample tr?ng.

## Patch c?c b? c?n ??i chi?u khi c?p nh?t

| N?i s?a | Tr?ch nhi?m | Khi n?ng source |
| --- | --- | --- |
| Scripts/Bootstrap/GameInstaller partials + IGameCompositionProfile | Hook composition t?y ch?n, d?ng c?ng lifecycle/cleanup | Gi? default demo khi kh?ng g?n profile; ch?y template regression |
| Scripts/Bootstrap/GameInit | T?y ch?n nh?c demo, gi? Ready/Failed v? SceneLoader | Pixel Flow t?t nh?c demo, kh?ng ??i ???ng chuy?n Main |
| Scripts/Bootstrap/SceneLoader | IsPresenting quan s?t c? loading animation | Ki?m tra t? normal entry v? input sau loading |
| _Feature/ShopPanel + IShopViewAdapter | Native passive skin hook | Gi? factory/presenter, feedback v? default sample khi hook tr?ng |
| _Feature/ShopFeatureInstaller | Factory override h?p | M?t ??ng k? presenter; kh?ng bind th? c?ng l?n hai |
| Prefabs/Foundation | Skin variant + item/holder/tween c? s?n | Gi? GUID, serialized art v? Addressables key |

Header Mini Shop ???c t?ch kh?i header Home/Shop ?? n?t Close kh?ng b? gear ??; Rank d?ng sprite kh?a/m? theo ti?n tr?nh. N?n Image m?u tr?n item/holder b? t?t ? skin variant, logic component ???c gi?. ??y l? thay ??i tr?c ti?p tr?n c?u h?nh prefab hi?n c?, kh?ng d?ng m?t UI runtime song song.

## Ph?n c?n ph?i ho?n thi?n

Home/HUD/common popup/settings skin c?n code d?ng UI; c?n ti?p t?c chuy?n v?o prefab/panel lifecycle v? reuse Settings integration. Manager legacy Input polling hi?n b? t?t t?i consumer ?? Input System controller x? l? back; ??y l? adapter chuy?n ti?p c?n gi?i quy?t khi c?c popup chuy?n ho?n to?n. Loading ?ang d?ng skin template, ch?a ph?i ??i ch?ng gi?ng native Pixel Flow. Rank/events/provider v?n c?n gate trong brief.

L??t ki?m tra ri?ng Shop ?? pass sau khi ?i qua Bootstrap/loading/Main, g?m success/cancel/failure/pending/repeat (XML `editor-shop-startup-pass.xml`). To?n b? regression ?ang ???c ch?y l?i.

Compile/Editor transactions v? native fidelity l? c?c gate ri?ng. Kh?ng build player theo y?u c?u; ch?a x?c nh?n device, store SDK, content build hay ship-ready. Ch? d?ng XML/?nh/Console c?a ??ng revision, ghi c? failure v? retest.
