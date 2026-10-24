# SAGE Addressables UI

Unity UPM package tách từ `Assets/QuickFramework` của project Shelvoria Triple Goods Sort.
Yêu cầu Unity 2022.3+, Addressables 1.22.3 và uGUI 1.0.0. Namespace: `SAGE.AddressablesUI`.
Không cần MEC, Odin, UniTask, EasyTransition hay singleton/logger của project nguồn.

## Cài từ ZIP

Giải nén. Trong Unity: **Window > Package Manager > + > Add package from disk**,
chọn `com.sage.addressables-ui/package.json`.
Giữ thư mục package ở vị trí cố định sau khi cài. Package Manager tự resolve các dependency.

## Push lên GitHub

Đưa **nội dung** thư mục `com.sage.addressables-ui` vào root repository riêng
(`package.json` nằm ngay tại root), rồi commit/push bằng tài khoản của bạn.
Sau đó project khác cài bằng **Add package from git URL**, dùng URL repo của bạn.
Nên tạo tag `v0.1.0` và thêm `#v0.1.0` vào Git URL để cố định phiên bản.
Không cần upload project game hay các file kiểm tra biên dịch.

## Gọi API

Tạo một UI prefab có `RectTransform` và script kế thừa `BaseScreen` ở root.
Đánh dấu prefab là Addressable, đặt address ví dụ `ShopScreen`.
Tạo/build Addressables content cho từng nền tảng theo workflow của project.
Thêm một EventSystem vào scene, dùng input module phù hợp với Input System của project.

```csharp
using SAGE.AddressablesUI;

public class ShopScreen : BaseScreen { }

// Gọi trong một phương thức async trên Unity main thread:
var ui = UIManager.Create(); // Tự tạo manager persistent, 3 Canvas và overlay.
await ui.ShowAndLoadScreenAsync<ShopScreen>("ShopScreen", parent: CanvasType.Popup);
ui.HideScreen("ShopScreen"); // Giữ instance trong cache.
ui.Back();                  // Đóng screen ở đầu back stack.
ui.UnloadScreen("ShopScreen"); // Hủy instance và release Addressables.
```

`ShowAndLoadScreenAsync<T>` trả về screen để bạn tiếp tục gọi API riêng.
`IUIData` dùng truyền data vào `BaseScreen.SetData`.
Lần gọi đầu load và instantiate prefab; các lần sau tái sử dụng instance.
Nhiều yêu cầu đồng thời cho cùng một key dùng chung tác vụ load; mỗi yêu cầu vẫn gọi SetData/Show.
`GetScreen<T>(key)`, `HideAllPreviousScreens()` và bản callback
`ShowAndLoadScreen<T>(key, data, parent, callback)` cũng được cung cấp.

Nếu game dùng key riêng, gán `ui.AddressResolver = key => YourAddressMap.GetAddress(key)`.
Mặc định key chính là address Addressables; package không chứa GUID hoặc danh sách screen của game.

```csharp
// Chủ sở hữu riêng để quản lý lifetime asset theo label:
var assets = new AddressableManager();
var prefabs = await assets.LoadAllAssetByLabelAsync("Gameplay");
// Dùng prefabs; giữ handle cho đến khi mọi instance tạo từ chúng đã bị hủy.
assets.UnloadAllAssetByLabel("Gameplay");
assets.Dispose();

// Có sẵn owner toàn cục nếu cần:
await AddressableManager.Instance.LoadAllAssetByLabelAsync("Common");
AddressableManager.Instance.UnloadAllAssets();
```

`ui.Assets` là owner riêng của UIManager. `await ui.DoInitializeAsync("BaseScreen")`
preload toàn bộ prefab có label; bước này tùy chọn, có thể bỏ qua để load screen theo nhu cầu.
`IELoadAllAssetByLabel` trả về IEnumerator tiêu chuẩn, gọi bằng StartCoroutine, không dùng MEC.
Unload label đang tải đánh dấu hủy, release sau khi operation hoàn thành; người đang await nhận exception.

```csharp
ui.ShowCircleLoading(0.3f); // Chặn input ngay, hiển thị overlay sau 0.3 giây.
ui.HideCircleLoading();
await ui.FadeInLoadingAsync();
await LoadingManager.LoadSceneAsync("MainMenu"); // Load và activate scene Single.
await ui.FadeOutLoadingAsync();

var scene = await LoadingManager.LoadSceneAsync("Gameplay",
    UnityEngine.SceneManagement.LoadSceneMode.Additive);
await scene.UnloadAsync();
```

Overlay mặc định là nền tối đơn giản; có thể gắn UIManager vào GameObject của bạn,
gán Canvas và LoadingOverlay có CanvasGroup riêng trong Inspector.
Fade và delay dùng unscaled time. Khi Show/Hide/fade mới thay thế fade đang chạy,
Task fade cũ bị cancel. Không chạy manual fade đồng thời với ShowAndLoadScreenAsync vì
screen loading cũng điều khiển cùng overlay. API `ShowCircleLoading` giữ tên cũ nhưng
không kèm sprite spinner của game; bạn có thể thêm hình spinner vào overlay tùy chỉnh.

## Khác biệt với bản trong game

- Giữ nhóm API tải label, quản lý screen, back stack, loading và fade; chuyển namespace
  từ `SAGE.Framework.Core.*` sang `SAGE.AddressablesUI` để có thể cùng tồn tại với framework cũ.
- Các prefab/script screen từ game cần đổi base class/namespace và loại bỏ dependency riêng
  của game khi chuyển sang package. Đây không phải thay thế trực tiếp UIManager.prefab cũ.
- Không chứa art, UIRewardCoin, mã generate BaseScreenAddress hoặc prefab thương mại.
- LoadingManager cung cấp load/activate scene và unload additive. Không sao chép cơ chế preload
  nhiều scene chưa activate của bản cũ vì nó có thể chặn hàng đợi async của Unity.
- Async dùng Task và completion callback Addressables, không dùng handle.Task hay Task.Run
  để gọi Unity API. Tất cả API phải được gọi trên Unity main thread.
- UIManager release các screen và label nó sở hữu khi bị hủy. Không tự hủy screen đã cache
  từ bên ngoài; dùng UnloadScreen. Không gỡ package hoặc hủy Canvas khi đang dùng UI.
- Screen prefab được Unity instantiate trước khi gọi Initialize/SetData; tránh logic cần data
  trong Awake/OnEnable. Đặt logic hiển thị trong Show và luôn gọi base.Show/base.Hide khi override.

Import sample **API Example** ở Package Manager để có ExampleScreen và ExampleBootstrap.
Sample là script hướng dẫn; bạn cần tự tạo prefab Addressable tương ứng.

## Kiểm tra

Xem VALIDATION.md cho phạm vi kiểm tra thực tế. Sau khi cài vào game, kiểm tra load thành công,
address sai, hai lần mở đồng thời, Back/Hide, UnloadScreen, fade khi timeScale = 0 và chuyển scene.
