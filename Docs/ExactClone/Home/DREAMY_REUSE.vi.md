# Tái sử dụng Dreamy cho Home

Chủ dự án cho phép tự tìm và thêm package cần thiết từ Dreamy-Game-Foundation. Danh sách repo lấy từ GitHub API được lưu trong `dreamy-organization.json`; không chỉ dựa trên trang web được cache. Quyền này áp dụng cho công việc tiếp theo, vẫn giữ yêu cầu không build player.

Đã kiểm tra manifest, lock và source package đang resolve. Home hiện không cần package mới: dự án đã có Core, Assets, UI, Audio, DataConfig, Datasave, Feedback, Feature, Economy, Settings và Shop. Không nâng hàng loạt các pin đang dùng trong lúc sửa Home.

- Campaign/config tiếp tục qua Dreamy DataConfig; tiến trình và giao dịch save-before-publish tiếp tục qua Dreamy Datasave, với `ProgressStore` là chủ sở hữu trạng thái Pixel Flow.
- Home dùng `Dreamy.UI.SafeArea` thay phần tính safe area riêng. Source kiểm tra: `com.dreamy.ui@91bfb340955d/Runtime/Utils/SafeArea.cs`, assembly `Dreamy.UI.Runtime`. Cần compile và kiểm tra UI tại consumer này; compatibility catalog của toolkit được xác minh ở revision cũ nên không coi catalog là bằng chứng cho toàn bộ revision hiện tại.
- Giữ template scenes/installers và GUID. Không chạy đồng thời GameInstaller của template với PixelFlowApp vì sẽ tạo hai vòng đời service/wallet/save. Bố cục Home dùng asset Pixel Flow và contract của game.
- Audio service và các feature package đã có sẵn nhưng chưa thay toàn bộ audio/economy/settings hiện tại trong task Home. Sự hiện diện của package không đồng nghĩa tích hợp xong. Chuyển chủ sở hữu về sau cần adapter và regression cho save cũ, dispose, failure và giao dịch.
- Localization, connectivity, progression, daily-reward, missions, tutorial và lucky-wheel là các repo phát hiện thêm. Chỉ thêm khi chức năng cụ thể cần API đã kiểm tra; các dịch vụ server/IAP/leaderboard thực tế vẫn cần provider, không thể có chỉ bằng import package.

Mọi package mới phải ghi URL/revision, mục đích sử dụng, dependencies và kết quả compile/test. Chỉ sửa manifest khi có nhu cầu cụ thể và để Unity sinh lock.
