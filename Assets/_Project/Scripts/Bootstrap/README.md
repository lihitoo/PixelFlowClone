# Khởi tạo và tích hợp feature

`GameInstaller` là composition root của ứng dụng: tạo service, nối các feature và quản lý cleanup. Component tồn tại qua các scene nhờ `DontDestroyOnLoad`. Các file partial dưới đây thuộc cùng một component và cùng vòng đời:

| File | Trách nhiệm |
| --- | --- |
| `GameInstaller.cs` | Thứ tự bootstrap, trạng thái Ready/Failed, cancellation và hook lưu dữ liệu khi pause/quit. |
| `GameInstaller.Foundation.cs` | Datasave, pooling, audio, feedback, economy và config. |
| `GameInstaller.Features.cs` | Settings, Shop, callback billing và hiệu ứng entitlement của project. |
| `GameInstaller.Presentation.cs` | Presenter factory, gán factory cho manager của scene và điều hướng Rate Us. |
| `GameInstaller.Services.cs` | Theo dõi quyền sở hữu đăng ký service và cleanup theo thứ tự ngược. |

## Chạy project

Đặt một `GameInstaller` trong BootstrapScene; không đặt thêm installer ở các scene sau. Static owner guard từ chối component installer thứ hai trước khi đăng ký service. Destroy component bị từ chối không reset trạng thái hoặc dọn service của installer đang hoạt động.

`GameInit` và `HomeBootstrap` đọc trạng thái bootstrap. Khi tải lại BootstrapScene, installer trùng bị từ chối nhưng GameInit được giữ để luồng khởi động tiếp tục. `GameInit.Start` đặt mục tiêu 60 FPS trước khi chờ bootstrap.

Thứ tự khởi tạo: persistence/infrastructure → economy → đăng ký và tải config → tạo presenter factory → cài feature/presentation → Ready. Khi thất bại, đọc `InitializationException` và Console; GameInit không chuyển scene nếu trạng thái Failed.

## Thêm service hoặc feature

1. Đăng ký bảng config trong `InstallConfigurationAsync` trước bước load.
2. Tạo method cài feature riêng và gọi từ `InstallFeatures` khi dependency đã sẵn sàng.
3. Dùng `RegisterService` cho đăng ký trực tiếp. Chỉ đặt `ownsInstance` cho instance do project tạo và chịu trách nhiệm dispose.
4. Dùng `TrackFeatureInstallation` cho installer package tự đăng ký service.
5. Đăng ký presenter constructor trong factory; mở panel qua PanelManager.

Cleanup khôi phục đăng ký trước đó, giữ các đăng ký thay thế do owner khác cài và dispose instance thuộc project theo thứ tự ngược. Truyền dependency xuống presenter/component qua constructor hoặc Initialize; giữ việc resolve service ở root.

## MVP và vòng đời panel

View giữ reference Button/TMP/Image, phát event input và thực hiện render. Presenter subscribe event, gọi service/navigation và cập nhật View. Service giữ quy tắc và dữ liệu; View không tự thay đổi wallet hoặc ghi save.

`InstallPresentation` đăng ký Home/Demo và gán factory chung cho PanelManager của các scene. Settings/Shop đăng ký presenter qua feature installer. Manager tạo presenter khi mở panel và giải phóng khi đóng/disable/destroy hoặc mở lỗi; mở lại View cache sẽ tạo presenter mới. Không tạo presenter thứ hai trong lifecycle của View.

Presenter cần gỡ event và hủy tác vụ theo lần mở trong Dispose. Token OnDestroy của View không đủ khi panel chỉ đóng hoặc được cache. Giữ guard chống thao tác lặp, xử lý exception và kiểm tra dispose trước khi cập nhật View sau await.

Tham khảo `HomePresenter`, `HomePanel`, `HomeNavigator` và `ShopFeatureInstaller` để thêm feature theo cùng cách tổ chức.

Để test Shop, chạy BootstrapScene trong Editor hoặc bản build rồi mua gói IAP. Gateway tự giả lập mua thành công, không cần bật tùy chọn: mỗi lần mua có transaction ID riêng, Shop cấp thưởng qua wallet và lưu quyền sở hữu như bình thường. Gói consumable có thể mua nhiều lần; gói mua một lần sẽ chuyển sang đã sở hữu. Dữ liệu test được lưu vào save hiện tại. Gateway do host đăng ký sẵn vẫn được ưu tiên.

`PurchaseWithBillingAsync` hiện giả lập trên mọi nền tảng; thay phần giả lập bằng tích hợp SDK/backend thật khi thêm billing.

Xem [README chính](../../../../README.md) để biết cách cài project/package, sử dụng Addressables, UniTask, Sprite Atlas, asmdef và quy trình kiểm tra.


## Pixel Flow consumer: gi? lu?ng template

`BootstrapScene` l? Init scene c?a template, gi? nguy?n GUID/t?n. `GameInstaller` duy nh?t v? `PixelFlowInstallerProfile` n?m ? Bootstrap, t?n t?i qua chuy?n scene. `GameInit` ch? Ready, d?ng `SceneLoader` c?ng loading screen ?? c? ?? t?i `MainScene`. Main gi? UI root/layers c?a template v? controller Pixel Flow; sample `HomeBootstrap` ???c disable ?? tr?nh m? hai Home. Main kh?ng c?i service l?n hai.

Profile l? composition hook t?y ch?n; ?? tr?ng v?n ch?y consumer demo m?c ??nh. Profile ??ng k? config tr??c InitializeAsync v? c?i adapter sau config readiness. Presenter factory v?n ???c root g?n khi sceneLoaded. Kh?ng serialize controller Main v?o Bootstrap: controller bind v?o profile sau khi nh?n service v? release khi destroy; presenter ch? ???c t?o khi controller scene hi?n t?i ?? bind.

`GameInit.playTemplateMusic` m?c ??nh true cho demo; Pixel Flow ??t false v? controller ch?n cue game t? Dreamy Audio profile. Kh?ng ??i key nh?c c?a package ho?c t?o AudioSource pool ri?ng.

Editor: Play b?t ??u t? BootstrapScene; build settings ch? b?t BootstrapScene r?i MainScene. Menu `Pixel Flow/Foundation/Use Template Init And Main Scenes` c?u h?nh b?ng Editor API, gi? scene GUID/prefab reference v? t? ch?i scene ch?a l?u. Scene PixelFlow c? l? ngu?n chuy?n ??i l?ch s?, kh?ng ph?i entry runtime.

Consumer Pixel Flow x?a AudioBootstrap m?u kh?i scene khi GameInstaller/profile ch?u tr?ch nhi?m initialize audio sau config. Kh?ng gi? c? hai initializer; profile/library kh?c nhau c? th? khi?n ph?t tr?c ti?p file ch?y nh?ng key library/key kh?ng resolve ???c. Master volume ???c nh?n v?o bus Music/Sfx theo kh? n?ng b?n package ?ang c?i.
