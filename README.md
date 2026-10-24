# Dreamy Template Project

Yêu cầu clone y hệt toàn game đang được chuẩn bị/đối chiếu lại: [brief65 luồng](Docs/ExactClone/BRIEF.vi.md), [ảnh gốc/clone và sai lệch](Docs/ExactClone/review.html), [JSON link từng asset](Docs/AssetCatalog/manifest.json). Source hiện tại là checkpoint offline; chưa đạt parity toàn game. Không build mới.

Project này hiện chứa **Pixel Flow Clone** tại `Assets/_Project/PixelFlow/Scenes/PixelFlow.unity` (Unity 6000.4.12f1). Xem [hướng dẫn mở/chơi clone](Docs/RUN_CLONE.vi.md), [trạng thái kiểm chứng](Docs/CURRENT_STATE.md) và [quyết định dùng Dreamy template/package](Docs/TEMPLATE_DECISION.md). Theo yêu cầu chủ dự án, không chạy thêm player build. Phần dưới giữ tài liệu template gốc.

Source clone đã mở rộng campaign2.600 màn và các luồng offline: onboarding, cơ chế đặc biệt, booster, shop coin/daily/settings, save/resume, win/loss/continue/retry/error. Xem [ánh xạ gameplay/UI và test](Docs/FULL_FLOW_MATRIX.md); dữ liệu và fixture pass không thay cho đối chứng hành vi native hay chứng nhận solve toàn campaign.

Template Unity dành cho game mobile của Dreamy. Project cung cấp luồng khởi động, các service nền tảng và UI mẫu để bắt đầu một game mới: Home, Foundation Demo, Settings, Shop, wallet và feedback.

Code dùng chung nằm trong các package Dreamy; `Assets/_Project` chứa scene, prefab, dữ liệu và code tích hợp riêng của game. Template là điểm khởi đầu; billing, product ID, nội dung game và cấu hình phát hành cần được hoàn thiện theo từng sản phẩm.

## Mục lục

- [Cài đặt và chạy lần đầu](#cài-đặt-và-chạy-lần-đầu)
- [Cấu trúc project](#cấu-trúc-project)
- [Tổng quan các package](#tổng-quan-các-package)
- [Bootstrap và service](#bootstrap-và-service)
- [Config, save và economy](#config-save-và-economy)
- [UI feature theo MVP](#ui-feature-theo-mvp)
- [Addressables](#addressables)
- [UniTask](#unitask)
- [Sprite Atlas](#sprite-atlas)
- [Assembly Definition — asmdef](#assembly-definition--asmdef)
- [Audio, feedback và pooling](#audio-feedback-và-pooling)
- [Thêm feature và chuẩn bị build](#thêm-feature-và-chuẩn-bị-build)
- [Kiểm tra và xử lý lỗi](#kiểm-tra-và-xử-lý-lỗi)

## Cài đặt và chạy lần đầu

### Yêu cầu

- Unity **6000.4.12f1**, theo [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt).
- Git được cài và nằm trong PATH để Unity Package Manager tải dependency từ GitHub.
- Kết nối mạng và quyền truy cập các repository package nếu repository yêu cầu đăng nhập.
- Cài Android Build Support hoặc iOS Build Support trong Unity Hub khi làm việc với nền tảng tương ứng. Build/sign iOS cần môi trường macOS và Xcode.

### Mở project

1. Clone repository hoặc sao chép template, giữ `Assets`, `Packages`, `ProjectSettings` và các file `.meta`.
2. Trong Unity Hub, chọn **Add project from disk**, trỏ tới thư mục chứa ba thư mục trên rồi mở bằng đúng phiên bản Unity.
3. Chờ import asset và Package Manager resolve dependency. Kiểm tra Console trước khi chạy.
4. Mở [BootstrapScene](Assets/_Project/Scenes/BootstrapScene.unity), vào Play Mode.
5. Bootstrap khởi tạo service, tải config rồi chuyển sang [MainScene](Assets/_Project/Scenes/MainScene.unity). Home cung cấp các lối vào Demo, Shop và Settings.

Dùng BootstrapScene làm scene vào đầu tiên: mở MainScene trực tiếp sẽ thiếu các service do bootstrap cài đặt. `GameInit` đặt `Application.targetFrameRate = 60` trước khi chờ bootstrap.

### Cài hoặc cập nhật package

Project đã khai báo dependency trong [manifest.json](Packages/manifest.json); không cần cài lại từng package khi clone. [packages-lock.json](Packages/packages-lock.json) ghi dependency đã resolve, bao gồm commit của package Git.

Để cài một package Dreamy vào project khác: mở **Window > Package Management > Package Manager**, chọn **Install package from git URL** và lấy URL có tag từ manifest. Ví dụ package Core trong template:

```text
https://github.com/Dreamy-Game-Foundation/com.dreamy.core.git#v1.2.0
```

Package Unity như Addressables được cài qua Unity Registry; chọn phiên bản phù hợp với Editor. UniTask hiện được cài bằng URL Git:

```text
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

Khi nâng cấp, chọn tag/commit cụ thể, để Unity resolve lại rồi kiểm tra cả manifest và lock file. UniTask, DOTween và một số công cụ trong manifest hiện chưa ghim tag; commit đang dùng được ghi trong lock file. Không coi nội dung trên nhánh mới nhất là API chắc chắn của bản đang cài.

Với package có **Samples**, chọn sample cần dùng và **Import** trong Package Manager. Unity thường import vào `Assets/Samples`; các sample đã được tích hợp trong template này nằm ở `Assets/_Project/_Feature`. Khi nâng phiên bản package, đối chiếu sample đã import, asmdef và prefab; tránh import bản thứ hai có cùng tên assembly/type. Không sửa `Library/PackageCache` để làm bản vá lâu dài.

## Cấu trúc project

```text
Assets/
  _Project/
    Scenes/          BootstrapScene, MainScene và scene thử nghiệm
    Scripts/
      Bootstrap/     GameInstaller, GameInit, SceneLoader, Address
      Config/        Các model config của template
      Save/          TemplateSave
      Home/          HomePanel, HomePresenter, HomeNavigator
      Demo/          Foundation Demo và luồng save/load
      Pooling/       Adapter LeanPool
    _Feature/        Sample Dreamy đã import và tích hợp theo phiên bản
    Prefabs/         Panel, loading screen và prefab của project
    SpriteAtlas/     ShopOfferAtlas.spriteatlasv2
    Textures/        Texture và sprite của project
    Tests/Editor/    Regression test của template
  Resources/         Config JSON và các tài nguyên nạp bằng Resources
  AddressableAssetsData/  Settings, group và schema Addressables
  Settings/          URP asset và Build Profiles
  Plugins/           Các plugin có source/asset trong Assets
Packages/            Manifest và lock file
ProjectSettings/     Thiết lập Unity của project
```

Giữ code riêng của game trong `_Project`; đưa khả năng tái sử dụng vào package thích hợp. Commit `.meta` cùng asset để giữ GUID và reference scene/prefab. Không commit các thư mục cache sinh ra như `Library`, `Temp`, `Logs` hoặc `obj`.

## Tổng quan các package

Phiên bản bên dưới lấy từ manifest và package đã resolve tại thời điểm cập nhật README. Khi có khác biệt, kiểm tra manifest, lock file và source package đang cài.

| Package | Phiên bản | Vai trò và cách dùng trong game |
| --- | --- | --- |
| `com.dreamy.core` | 1.2.0 | Service locator, event bus, state machine, lifecycle, logging và contract pooling. Dùng ở composition root hoặc controller cấp cao để kết nối các hệ thống. |
| `com.dreamy.dataconfig` | 0.2.1 | Config JSON có kiểu, chỉ đọc và validation. Dùng cho catalog, chỉ số, level, reward và tuning; đăng ký bảng trước khi khởi tạo config. |
| `com.dreamy.datasave` | 0.2.0 | Lưu/đọc JSON có version, atomic write, migration và codec. Dùng cho tiến trình và trạng thái người chơi. |
| `com.dreamy.assets` | 0.1.1 | Loader Addressables/Resources, cache, request/progress và helper atlas. Xác định chủ sở hữu asset và chính sách giải phóng trước khi dùng. |
| `com.dreamy.ui` | 0.2.0 | Panel, navigation, tab, button, safe area, tween và presenter lifecycle. Mở panel qua manager, đăng ký presenter tại installer. |
| `com.dreamy.audio` | 0.1.0 | Audio library, music/SFX, mixer bus, pooling và component trigger. Cấu hình catalog/key rồi phát âm thanh qua audio service. |
| `com.dreamy.editor-tools` | 0.2.2 | Tiện ích chỉ chạy trong Editor. Dùng cho authoring/workflow; không tham chiếu từ assembly Runtime. |
| `com.dreamy.feature` | 0.1.0 | Contract, dữ liệu, enum và tiện ích chung cho các feature casual. Dùng khi feature cần chia sẻ contract, không đặt logic riêng của game vào đây. |
| `com.dreamy.feature.economy` | 0.2.1 | Resource ID ổn định, wallet lưu qua Datasave và contract cấp tài nguyên chống lặp. Dùng chung một wallet cho Shop và các view số dư. |
| `com.dreamy.feature.settings` | 0.2.2 | Feature MVP cho audio/haptic và hành động nền tảng. Import Settings Feature rồi cung cấp gateway của host. |
| `com.dreamy.feature.shop` | 0.1.2 | Catalog Shop, trao đổi tài nguyên và luồng mua qua gateway. Import Shop Feature, nối catalog, wallet và billing thật của game. |
| `com.dreamy.feedback` | 0.4.0 | VFX, haptic, floating text, icon fly, hiệu ứng màn hình/camera và sequence. Dùng để phản hồi kết quả, giữ nghiệp vụ trong service. |

Tên package và tên assembly không luôn giống nhau: `com.dreamy.feature.economy` cung cấp assembly `Dreamy.Economy.Runtime`, còn Shop sử dụng `Dreamy.Shop.Runtime`. Chọn reference theo file asmdef thực tế.

### Dependency và công cụ khác

| Thành phần | Vai trò |
| --- | --- |
| Addressables 2.9.1 | Tải asset theo address, catalog và bundle. |
| UniTask | Async/await theo PlayerLoop của Unity; có assembly tích hợp Addressables. |
| DOTween | Tween UI/feedback; kiểm tra integration và asmdef của bản đang cài trước khi dùng. |
| URP 17.4.0 | Render pipeline; cấu hình mobile tại `Assets/Settings/Mobile_RPAsset.asset`. |
| UGUI 2.0.0 và TextMeshPro | Canvas, layout, Button/Image và text UI. |
| Input System 1.19.0 | Input action và thiết bị; UI cần EventSystem/input module phù hợp. |
| Newtonsoft Json 3.2.2 | Serialize/deserialize JSON cho dữ liệu. |
| Unity Test Framework 1.6.0 | EditMode/PlayMode test. |
| LeanPool | Pool GameObject, dùng qua adapter của project. |
| Odin Inspector, QuickNav, Rainbow Folders, Unity MCP | Công cụ hỗ trợ Inspector, điều hướng, tổ chức và thao tác Editor. |

`compatibility/dreamy-packages.json` từng là hồ sơ kiểm tra riêng của Dreamy, **không phải file mặc định của Unity** và hiện không có trong working tree. Unity quản lý dependency bằng manifest và lock file; không cần tạo lại hồ sơ đó để chạy template.

## Bootstrap và service

Luồng khởi động hiện tại:

```text
BootstrapScene
  -> GameInstaller: persistence, pooling, audio, feedback
  -> Economy: wallet dùng chung
  -> Đăng ký và tải config
  -> Tạo presenter factory, cài Settings/Shop và presenter của project
  -> BootstrapState.Ready
  -> GameInit gọi SceneLoader.LoadScene(MainScene)
  -> HomeBootstrap mở HomePanel
```

[GameInstaller](Assets/_Project/Scripts/Bootstrap/GameInstaller.cs) là composition root tồn tại qua các scene. Chỉ đặt một installer trong BootstrapScene; các scene sau dùng service đã được cài. Installer trùng bị từ chối trước khi đăng ký service.

| File partial | Trách nhiệm |
| --- | --- |
| `GameInstaller.cs` | Thứ tự khởi tạo, trạng thái Ready/Failed, cancellation và hook save khi pause/quit. |
| `GameInstaller.Foundation.cs` | Datasave, pooling, audio, feedback, economy và config. |
| `GameInstaller.Features.cs` | Settings, Shop, billing callback và callback entitlement của host. |
| `GameInstaller.Presentation.cs` | Presenter factory, nối factory với PanelManager của scene và Rate Us. |
| `GameInstaller.Services.cs` | Theo dõi quyền sở hữu đăng ký, khôi phục đăng ký trước đó và cleanup theo thứ tự ngược. |

Thêm service trực tiếp bằng helper `RegisterService` trong installer; đánh dấu `ownsInstance` khi project tạo và chịu trách nhiệm dispose instance. Với installer feature tự đăng ký service, dùng `TrackFeatureInstallation` theo mẫu hiện có.

Chỉ resolve `ServiceLocator` ở root, installer hoặc controller cấp cao. Truyền dependency qua constructor/`Initialize` xuống presenter và component nhỏ. View item, object trong pool và VFX nên nhận dữ liệu rõ ràng thay vì tự tìm service toàn cục.

Xem thêm [hướng dẫn bootstrap](Assets/_Project/Scripts/Bootstrap/README.md).

## Config, save và economy

| Loại dữ liệu | Nơi quản lý | Ví dụ |
| --- | --- | --- |
| Config tĩnh | DataConfig | Giá offer, stat, reward table, điểm ban đầu. |
| Trạng thái bền vững | Datasave / wallet | Score tốt nhất, tiến trình, tiền, entitlement. |
| Trạng thái phiên/UI | Service/presenter | Tab chọn, đang mua, loading, dữ liệu render. |
| Unity asset | Prefab, atlas, Addressables/Resources | Hình ảnh, audio, panel. |

### DataConfig

Config của template nằm tại [Assets/Resources/DataConfig](Assets/Resources/DataConfig). Shop có catalog riêng trong [sample Shop](<Assets/_Project/_Feature/Dreamy Shop/0.1.2/Shop Feature/Resources/DataConfig/shopCatalog.json>).

Để thêm bảng: tạo model theo mẫu [TemplateConfig](Assets/_Project/Scripts/Config/TemplateConfig.cs), tạo JSON tương ứng, đăng ký trong `InstallConfigurationAsync` trước `InitializeAsync`, rồi truyền bảng đã tải vào service/presenter. Với `ResourcesJsonConfigSource`, key là đường dẫn tương đối trong Resources, không kèm `.json`. Kiểm tra ID trùng, giá âm và reference thiếu trước khi dùng dữ liệu. Không parse JSON trong panel hoặc ghi trạng thái người chơi vào config.

### Datasave và wallet

[TemplateSave](Assets/_Project/Scripts/Save/TemplateSave.cs) chứa trạng thái Foundation Demo. [FoundationDemoRoot](Assets/_Project/Scripts/Demo/FoundationDemoRoot.cs) minh họa luồng đọc, cập nhật và lưu dữ liệu.

- Lưu ID/key và dữ liệu thuần; tránh lưu reference `GameObject`, `Sprite` hoặc prefab.
- Thay đổi schema đã phát hành cần version/migration và kiểm tra dữ liệu cũ.
- Lưu sau giao dịch quan trọng; bootstrap cũng gọi SaveAll khi app pause/quit.
- Khi load thất bại, Demo vẫn tương tác nhưng khóa Save đến khi Load thành công; không tự xóa dữ liệu lỗi.
- Shop và resource holder dùng chung wallet do bootstrap tạo. Không dựng wallet riêng trong mỗi panel hoặc sửa tiền trực tiếp từ View.

Codec hiện dùng plain text trong Editor và XOR ở player. XOR chỉ che nội dung, không bảo đảm chống gian lận; giao dịch IAP và quyền sở hữu cần cơ chế xác thực phù hợp của game.

## UI feature theo MVP

MVP giúp UI thay đổi layout mà nghiệp vụ vẫn dễ kiểm tra và tái sử dụng.

| Thành phần | Trách nhiệm | Mẫu trong project |
| --- | --- | --- |
| Model / service | Dữ liệu, quy tắc, giao dịch và persistence; không thao tác Button/TMP/Image. | Config, wallet, Shop service, Settings service. |
| View | Hiển thị, binding component Unity và phát event input; không quyết định giao dịch. | `HomePanel` triển khai `IHomeView`; panel trong sample Shop/Settings. |
| Presenter | Nhận event View, gọi service/navigation, xử lý busy/error rồi yêu cầu View render. | `HomePresenter`, presenter Shop/Settings trong package. |
| Installer | Tạo service, cung cấp dependency và đăng ký factory tạo presenter. | `GameInstaller`, `ShopFeatureInstaller`, `SettingsFeatureInstaller`. |

Luồng Shop điển hình:

```text
Người chơi bấm Mua
  -> View phát yêu cầu
  -> Presenter gọi Shop service
  -> Service kiểm tra catalog/wallet hoặc gọi purchase gateway
  -> Presenter cập nhật View và phát feedback theo kết quả
```

View không tự trừ tiền hay cấp item. Service trả kết quả nghiệp vụ; Presenter chuyển kết quả đó thành loading, thông báo, số dư và feedback.

### Ví dụ thật: Home

Đọc [IHomeView](Assets/_Project/Scripts/Home/IHomeView.cs), [HomePanel](Assets/_Project/Scripts/Home/HomePanel.cs), [HomePresenter](Assets/_Project/Scripts/Home/HomePresenter.cs) và [HomeNavigator](Assets/_Project/Scripts/Home/HomeNavigator.cs) theo thứ tự này:

1. `IHomeView` khai báo event `DestinationRequested` và method `SetNavigationInteractable`.
2. `HomePanel` nối Button với event, đồng thời cập nhật trạng thái tương tác. Listener được thêm ở OnEnable và gỡ ở OnDisable; giữ lời gọi lifecycle của base panel.
3. `HomePresenter.Show()` subscribe event một lần. Khi nhận input, presenter khóa navigation và gọi navigator.
4. `HomeNavigator` dùng PanelManager để chuyển sang Demo/Shop hoặc mở Settings, có guard chặn transition trùng.
5. Presenter xử lý exception và chỉ bật lại View nếu chưa dispose. `Dispose()` gỡ event và dispose navigator.

Presenter của Home được đăng ký trong `InstallPresentation` bằng đoạn code hiện có:

```csharp
presenterFactory.Register<HomePanel>(view =>
    new HomePresenter(view, new HomeNavigator()));
```

Factory được gán vào PanelManager của scene. Code điều hướng chỉ mở panel; manager tạo và quản lý presenter. Không tạo thêm presenter trong `Awake`/`OnEnable` của panel đã đăng ký factory.

### Tạo UI feature mới

1. Xác định service/model và trạng thái cần lưu; tạo interface View gồm event input và method render.
2. Tạo panel kế thừa `UIPanel`, triển khai interface View; gán Button, TMP, Image trong prefab.
3. Tạo presenter nhận View và service qua constructor, theo contract `IPanelPresenter` của bản UI đang cài.
4. Subscribe trong `Show`, gỡ event trong `Dispose`; cleanup phải chịu được gọi lại. Tác vụ async theo vòng đời panel cần cancellation khi presenter bị dispose.
5. Đăng ký constructor presenter trong factory của installer sau khi dependency/config sẵn sàng. Với feature dùng chung, làm installer riêng như sample Shop.
6. Đưa prefab vào Addressables, thêm constant trong [Address.cs](Assets/_Project/Scripts/Bootstrap/Address.cs), rồi mở qua PanelManager.
7. Kiểm tra đóng/mở lại, Back, bấm liên tiếp, lỗi service và đóng panel giữa tác vụ async.

`Show` và `Transition` có mẫu sử dụng trong HomeNavigator. `CanCache` quyết định panel có được giữ để mở lại; cache View không đồng nghĩa giữ presenter cũ. Manager giải phóng presenter khi đóng/disable/destroy hoặc mở lỗi, và tạo presenter mới khi mở lại panel cache. Với panel không cache, Destroy được hoãn; kiểm tra Hide/mở lại cần chờ một frame.

Không cập nhật View sau dispose, không subscribe lại mỗi lần render, không dùng polling trong Update để đọc số dư. Presenter/service nên đẩy thay đổi qua event. Test presenter bằng fake View/service để kiểm tra hành vi mà không cần scene; dùng PlayMode cho wiring prefab, animation và navigation.

## Addressables

Addressables tải asset bằng address và quản lý dependency/bundle. Template có settings tại `Assets/AddressableAssetsData`, với `Default Local Group` chứa panel và folder asset.

### Thêm asset và build content

1. Mở **Window > Asset Management > Addressables > Groups**. Project đã có settings; chỉ tạo settings khi tích hợp vào project mới.
2. Chọn prefab/asset và bật **Addressable** trong Inspector, hoặc kéo vào group phù hợp.
3. Đặt address ổn định, cập nhật constant trong `Address.cs`; khi addressable hóa folder, kiểm tra địa chỉ asset con được sinh thay vì đoán từ tên file.
4. Chọn profile, build target và Build/Load Path phù hợp. Local content không cần CDN; remote content cần nơi lưu catalog/bundle và URL thực tế.
5. Trong Groups, chọn **Build > New Build > Default Build Script** để build content cho target hiện tại.
6. Play Mode dùng **Use Asset Database** khi phát triển; dùng **Use Existing Build** để thử content đã build. Kiểm tra tùy chọn build Addressables cùng player trước khi tạo build ứng dụng.

Các key code đang sử dụng gồm `Panel/HomePanel.prefab`, `Panel/FoundationDemoPanel.prefab`, `Panel/ShopPanel.prefab`, `Panel/SettingsPanel.prefab`, `Panel/RateUsPanel.prefab` và `SpriteAtlas/ShopOfferAtlas.spriteatlasv2`. Folder `Panel`/`SpriteAtlas` cũng có entry trong group. `MainScene` được SceneLoader tải bằng SceneManager và danh sách scene build, không phải bằng Addressables.

Panel đã dùng Dreamy UI thì để manager/loader quản lý việc tải và giải phóng. Đừng release handle hay asset của manager từ code gọi mở panel.

### Ví dụ quản lý handle trực tiếp

Ví dụ dưới dùng Unity Addressables và UniTask, dành cho asset do chính method sở hữu. Không trộn ownership này với cache của loader Dreamy.

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class TextureProbe
{
    public static async UniTask InspectAsync(string address, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var handle = Addressables.LoadAssetAsync<Texture2D>(address);
        try
        {
            // Chờ operation xong trước khi release; token không hủy tải Unity.
            var texture = await handle.ToUniTask();
            token.ThrowIfCancellationRequested();
            if (handle.Status != AsyncOperationStatus.Succeeded || texture == null)
                throw new InvalidOperationException($"Không tải được: {address}");
            Debug.Log($"Texture: {texture.width} x {texture.height}");
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }
}
```

Method này giải phóng asset khi kết thúc; nếu cần hiển thị texture lâu hơn, owner phải giữ handle đến khi View thôi dùng asset. Instance tạo bằng `Addressables.InstantiateAsync` cần `Addressables.ReleaseInstance`; object do pool tạo phải trả về pool. Dọn cả trường hợp lỗi và khi owner kết thúc. Cancellation của caller không có nghĩa operation bên dưới đã dừng.

Xem [tài liệu Addressables 2.9](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/AddressableAssetsGettingStarted.html).

## UniTask

UniTask giúp viết tác vụ async theo PlayerLoop, phù hợp tải config/asset, chờ bootstrap và luồng UI. Namespace là `Cysharp.Threading.Tasks`; asmdef dùng async cần reference `UniTask`.

Ví dụ delay gắn với vòng đời component:

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class AsyncUiExample : MonoBehaviour
{
    private void Start()
    {
        RunAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTask RunAsync(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(300, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            Debug.Log("Owner vẫn còn hoạt động.");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
```

- Ưu tiên trả về `UniTask`/`UniTask<T>` để caller await và nhận lỗi. Dùng `.Forget()` ở ranh giới event/lifecycle với cách báo lỗi rõ ràng.
- Truyền CancellationToken xuyên suốt. Token OnDestroy không tự hủy tác vụ khi panel chỉ đóng hoặc được cache; presenter cần token riêng cho mỗi lần mở.
- Hủy và dispose CancellationTokenSource khi owner kết thúc; guard chống double click hoặc tác vụ trùng.
- Sau await, kiểm tra cancellation/dispose trước khi chạm View. Không dùng `.Wait()`, `.Result` hoặc `Thread.Sleep` trên main thread.
- Không await lặp lại cùng một UniTask nếu chưa dùng cơ chế chia sẻ được hỗ trợ; tạo operation mới hoặc dùng cơ chế cache phù hợp.
- Không thao tác Unity object từ worker thread. Dùng timing phù hợp; UI vẫn chạy trên main thread.

Project minh họa cancellation ở SceneLoader và UILoadingScreen. SceneLoader cho operation scene đã bắt đầu hoàn tất/activate khi hủy, vì Unity không hủy được scene load thực sự.

Xem [UniTask README chính thức](https://github.com/Cysharp/UniTask).

## Sprite Atlas

Sprite Atlas gom sprite vào texture atlas để giảm đổi texture khi render. Atlas không tự bảo đảm mọi Image chung một draw call: material, mask và thứ tự UI cũng ảnh hưởng batching.

### Tạo và sử dụng

1. Import ảnh dưới dạng **Sprite (2D and UI)**; với sheet, chọn Multiple và slice trong Sprite Editor nếu cần.
2. Tạo **Sprite Atlas** trong Project, đặt vào `_Project/SpriteAtlas`; project hiện dùng atlas V2 (`.spriteatlasv2`).
3. Thêm sprite/folder vào **Objects for Packing**, dùng **Pack Preview** để kiểm tra tên và kết quả pack.
4. Với UI, thường tắt rotation/tight packing và mipmap nếu không cần; chọn padding, kích thước và compression theo chất lượng thực tế trên thiết bị.
5. Chọn một cách quản lý vòng đời: reference trực tiếp từ prefab, hoặc loader giữ atlas đã tải theo address. Kiểm tra bundle dependency trước khi phối hợp cả hai.

### Mẫu Shop trong template

[ShopOfferAtlas](Assets/_Project/SpriteAtlas/ShopOfferAtlas.spriteatlasv2) chứa icon Gold/Gem; ShopPanel đã gán `Icon Atlas` trong Inspector. Catalog dùng `iconKey` trùng tên sprite, ví dụ `Gold_0`, `Gem_0`.

ShopPanel lấy sprite bằng `GetSprite`, cache clone theo key và truyền xuống item. Clone được dọn khi panel destroy; khi View cache, sprite cache tồn tại cùng panel. Atlas giữ bằng serialized reference, đi kèm dependency của prefab; không release thủ công atlas này. Constant address atlas có sẵn để dùng cho luồng tải khác, nhưng Shop hiện không tải icon qua constant đó.

Khi thêm offer: thêm icon vào folder pack, kiểm tra Pack Preview, điền đúng `iconKey`, kiểm tra fallback và rebuild Addressables content nếu dùng bundle. Nếu thiếu key/atlas/sprite, panel dùng Fallback Icon; nếu không có fallback thì ẩn Image và vẫn hiển thị thông tin offer.

Đọc [README Shop](<Assets/_Project/_Feature/Dreamy Shop/0.1.2/Shop Feature/README.md>) và [Unity Sprite Atlas](https://docs.unity3d.com/6000.0/Documentation/Manual/class-SpriteAtlas.html).

## Assembly Definition — asmdef

Asmdef chia script thành assembly có dependency rõ ràng, giúp kiểm soát ranh giới module và phạm vi compile. Namespace và assembly là hai khái niệm khác nhau: có `using` chưa đủ nếu thiếu reference assembly.

Template có [Dreamy.Template.Runtime.asmdef](Assets/_Project/Scripts/Dreamy.Template.Runtime.asmdef) cho code game và [Dreamy.Template.Editor.Tests.asmdef](Assets/_Project/Tests/Editor/Dreamy.Template.Editor.Tests.asmdef) cho EditMode test. Các sample `_Feature` có assembly integration riêng.

### Tạo assembly cho feature

Trong folder script feature, chọn **Create > Scripting > Assembly Definition**, đặt tên rõ ràng và thêm reference tối thiểu. Ví dụ khởi đầu cho UI feature mới; thêm assembly service/config nếu code thực sự dùng:

```json
{
  "name": "Game.Feature.Inventory.Runtime",
  "rootNamespace": "Game.Feature.Inventory",
  "references": [
    "Dreamy.UI.Runtime",
    "Dreamy.UI.Presentation",
    "UniTask",
    "Unity.TextMeshPro",
    "UnityEngine.UI"
  ],
  "autoReferenced": true
}
```

| Code sử dụng | Reference cần kiểm tra |
| --- | --- |
| UniTask cơ bản | `UniTask` |
| Addressables + ToUniTask | `Unity.Addressables`, `Unity.ResourceManager`, `UniTask`, `UniTask.Addressables` |
| Panel/presenter | `Dreamy.UI.Runtime`, `Dreamy.UI.Presentation` |
| TextMeshPro / Button, Image | `Unity.TextMeshPro` / `UnityEngine.UI` |
| Shop / wallet | `Dreamy.Shop.Runtime` / `Dreamy.Economy.Runtime` |
| Sample Shop của template | `Dreamy.Feature.Shop.Integration.Runtime` |

Assembly chỉ tham chiếu một chiều: host nối feature/package; package không tham chiếu code project. Nếu hai feature phụ thuộc vòng tròn, tách contract chung hoặc chuyển việc kết nối lên installer. Assembly có asmdef cũng không thể dựa vào code còn nằm trong `Assembly-CSharp`; chuyển dependency đó vào assembly rõ ràng.

Editor tool cần asmdef riêng với **Include Platforms: Editor**; Runtime không reference UnityEditor hoặc assembly Editor. Khi tạo asmdef cha, kiểm tra các folder con: script có thể đổi assembly và reference đang dùng sẽ cần điều chỉnh. Không đổi/xóa `.meta` của script hoặc asmdef để chữa lỗi reference.

Xem [Unity Assembly Definition](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-files.html).

## Audio, feedback và pooling

### Audio

Bootstrap đăng ký audio service của Dreamy; GameInit phát nhạc key `core/music.main` sau khi scene được tải. Cấu hình Audio Library, key và mixer/profile theo game; dùng key cho music/SFX thay vì rải reference AudioClip trong gameplay. Settings nối preference vào audio service. Kiểm tra key thiếu, âm lượng, mute và hành vi pause/resume.

### Feedback

GameInstaller cần `FeedbackHost` prefab, tạo rig và đăng ký các service được host cung cấp. Trigger feedback sau kết quả thành công/thất bại từ service; feedback không quyết định có được mua/cấp thưởng hay không. Các sample Basic Feedback và DOTween Feedback nằm trong `_Feature/Dreamy Feedback/0.4.0`.

### Pooling

[LeanPoolService](Assets/_Project/Scripts/Pooling/LeanPoolService.cs) là adapter của project cho contract pool. Dùng cho object tạo/hủy thường xuyên như item danh sách, floating text, VFX và projectile.

Object spawn bằng pool phải despawn bằng cùng pool. Reset text/icon/trạng thái khi reuse; gỡ event và dừng async/tween của lần dùng cũ. Không gọi Destroy cho object vẫn thuộc pool. Asset nguồn do Addressables tải phải sống đủ lâu cho các instance trong pool; chỉ release khi owner đã dọn pool.

## Thêm feature và chuẩn bị build

### Quy trình thêm feature

1. Chọn owner của config, save, service và View; kiểm tra package đã có khả năng cần dùng chưa.
2. Tạo script/asmdef trong project hoặc import sample phù hợp, giữ dependency một chiều.
3. Đăng ký config trước bước khởi tạo; nối service qua installer và theo dõi cleanup.
4. Với UI, tạo View/Presenter, đăng ký factory, cấu hình prefab/Addressables và navigation.
5. Thêm audio/feedback, pooling hoặc atlas nếu feature cần.
6. Kiểm tra lỗi và vòng đời: mở/đóng lặp, async bị hủy, dữ liệu thiếu, load/save lỗi và bấm liên tiếp.

### Baseline mobile và build

- Có [Android Development](<Assets/Settings/Build Profiles/Android Development.asset>) và [Android Release](<Assets/Settings/Build Profiles/Android Release.asset>) trong Build Profiles. Chọn profile phù hợp rồi kiểm tra setting nền tảng thực tế.
- Kiểm tra scene list: BootstrapScene đầu tiên, MainScene bật; scene thử nghiệm `T` hiện bị tắt trong danh sách build chung.
- Giữ mục tiêu 60 FPS theo GameInit, kiểm tra URP/quality, portrait/layout, safe area và EventSystem trên tỷ lệ màn hình mục tiêu.
- Kiểm tra IL2CPP/ARM64, identifier, version, signing và symbol theo nền tảng/game.
- Build Addressables đúng target/profile; thử trên player, không chỉ Use Asset Database trong Editor.
- Kiểm tra memory của texture/atlas/audio, kích thước bundle, GC, pool và frame time trên thiết bị.
- Shop billing hiện có TODO trong `PurchaseWithBillingAsync` và trả thất bại khi chưa nối SDK. Product ID/giá và các callback entitlement vẫn là dữ liệu/luồng mẫu; hoàn thiện và xác thực giao dịch trước khi phát hành.

Sự hiện diện của profile/sample không phải bằng chứng build Android/iOS hay kiểm tra thiết bị đã thành công. Ghi lại kết quả build và smoke test của chính phiên bản sắp phát hành.

## Kiểm tra và xử lý lỗi

### Kiểm tra thay đổi

- Chờ compile xong và kiểm tra Console không có lỗi mới.
- Chạy BootstrapScene, thử Home → Demo/Shop/Settings, Back và mở lại panel cache.
- Với thay đổi service/save/presenter, chạy test liên quan ở **Window > General > Test Runner > EditMode**. Test hiện có: `TemplateSaveTests`, `InstallerOwnershipTests`, `HomePresenterLifecycleTests`, `LeanPoolServiceTests`.
- Với scene/prefab/async UI, kiểm tra PlayMode; với content/build, thử Existing Build và player đúng target.
- Test package upstream là phạm vi khác với regression của host; không suy ra mọi test package đã pass khi chỉ chạy test template.

Kiểm tra whitespace/diff nhanh từ thư mục root:

```bash
git diff --check
git diff -- README.md
```

### Lỗi thường gặp

| Hiện tượng | Cách kiểm tra |
| --- | --- |
| Package Git không tải được | Git/PATH, mạng, quyền repository và URL/tag trong manifest. |
| Type/namespace không tồn tại | Console compile, phiên bản đã resolve, asmdef reference và platform/define constraint. |
| Bootstrap Failed hoặc thiếu service | Chạy từ BootstrapScene, đọc exception đầu tiên; kiểm tra FeedbackHost prefab và JSON config. |
| Không tải được panel/asset | Address, entry/folder trong group, active profile, Play Mode Script và content build đúng target. |
| Panel không có nghiệp vụ hoặc input gọi hai lần | Presenter factory đã gán chưa, registration đúng panel type, listener có được gỡ và presenter có bị tạo thủ công thêm không. |
| Icon Shop trống | Icon Atlas/Fallback trên prefab, `iconKey`, tên sprite và Pack Preview. |
| Đóng panel rồi async vẫn cập nhật UI | Token theo lần mở, Dispose/unsubscribe, guard sau await; OnDestroy không đủ cho panel cache. |
| UI không bấm được | EventSystem, input module, raycast target, Canvas và overlay đang chặn input. |
| Save không hoạt động | Exception đọc/ghi, schema/codec, đường dẫn lưu và trạng thái Load của Demo; giữ dữ liệu lỗi để chẩn đoán. |
| IAP luôn thất bại | Callback billing trong GameInstaller chưa nối SDK/backend; không dùng kết quả giả để cấp hàng. |

## Tài liệu và code nên đọc tiếp

- [Bootstrap](Assets/_Project/Scripts/Bootstrap/README.md).
- [Home MVP](Assets/_Project/Scripts/Home/HomePresenter.cs) và [navigation](Assets/_Project/Scripts/Home/HomeNavigator.cs).
- [Shop Feature](<Assets/_Project/_Feature/Dreamy Shop/0.1.2/Shop Feature/README.md>).
- [Settings Feature](<Assets/_Project/_Feature/Dreamy Settings/0.2.2/Settings Feature/README.md>).
- [Economy — Wallet Demo](<Assets/_Project/_Feature/Dreamy Economy Contracts/0.2.1/Wallet Demo/README.md>).
- [Economy — Resource Holders](<Assets/_Project/_Feature/Dreamy Economy Contracts/0.2.1/Resource Holders/README.md>).
- [Feedback cơ bản](<Assets/_Project/_Feature/Dreamy Feedback/0.4.0/Basic Feedback/README.md>).
- README, Samples và source của từng package trong Package Manager để đối chiếu API của bản đang cài.
