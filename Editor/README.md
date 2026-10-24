# Editor API

Namespace `SAGE.AddressablesUI.Editor`, assembly `SAGE.AddressablesUI.Editor` chỉ chạy trong Editor.
Script gọi API phải nằm trong thư mục Editor hoặc asmdef có includePlatforms: Editor.
Nếu dùng asmdef riêng, thêm reference tới assembly này và `Unity.Addressables.Editor`
khi truy cập kiểu dữ liệu Addressables Editor.

## Dùng Inspector

1. Chọn **Assets > Create > SAGE > Addressables UI > Address Reference**.
2. Nhập group, className, getNameMethod; thêm các dòng key, labels, reference hoặc asset.
   Dòng Addressable nhận AssetReference hoặc asset trực tiếp khi reference để trống.
3. Tạo **Address Library**, đặt generatePath, namespaceName và thêm các reference table vào screenReferences.
4. Nhấn **Build Address Library**. Tool tạo/cập nhật group, entry, label và script C# trong Assets.

Không cần Odin hay template bên ngoài. Config `.asset` mới được lưu trong Assets của project,
không lưu bên trong package. Thay đổi đường dẫn sinh code không tự xóa các file cũ.

## Gọi bằng code

```csharp
using SAGE.AddressablesUI.Editor;
using UnityEditor;
using UnityEngine;

// Trong phương thức của script Editor:
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/ShopScreen.prefab");
AddressablesEditorApi.RegisterAsset(prefab, "BaseScreen", "ShopScreen", new[] { "BaseScreen" });
AssetDatabase.SaveAssets();

var table = AssetDatabase.LoadAssetAtPath<AddressReferenceEditor>("Assets/Config/Screens.asset");
table.InsertFromFolder("Assets/UI", filter: "t:Prefab"); // Append và bỏ qua key/GUID đã có.
AddressLibTool.Build(new[] { table }, "Assets/Generated/Addresses", "Game.Addresses");

// Hoặc build cấu hình Address Library đã lưu:
AssetDatabase.LoadAssetAtPath<AddressLibTool>("Assets/Config/AddressLibTool.asset").Build();

// Chủ động tìm và build tất cả AddressReferenceEditor của project:
AddressLibTool.BuildAllReferences("Assets/Generated/Addresses", "Game.Addresses");
```

Các API công khai khác:

- `GetOrCreateSettings()` tạo Addressables settings mặc định khi chưa tồn tại.
- `GetOrCreateGroup(name, settings)` bảo đảm group có BundledAssetGroupSchema và ContentUpdateGroupSchema.
- `RegisterGuid(guid, group, address, labels, replaceLabels, settings)` đăng ký/chuyển group bằng GUID.
  Hai hàm Register mặc định bổ sung label; chỉ xóa label cũ nếu truyền replaceLabels: true.
- `AddressReferenceEditor.LoadAssetsAtPath<T>(folder, filter)` quét main asset trong folder con của Assets.
- `AddressCodeGenerator.GenerateSource(namespaceName, className, getNameMethod, entries)` chỉ trả về
  chuỗi code, không thay đổi Addressables hoặc ghi file.

Build sinh lớp runtime không phụ thuộc UnityEditor, có `const string` key,
`GetName(key)` (hoặc tên method bạn cấu hình) và `IsResources(key)`.
Với Addressable, GetName trả về GUID để load; entry.address vẫn là key dễ đọc.
Nếu nhiều key trỏ cùng GUID trong cùng group, key xuất hiện đầu tiên là address chính;
các key còn lại là alias trong lớp sinh code. Các label của alias được hợp nhất.
Một GUID không được đặt ở nhiều group trong cùng một lần Build.

Sau khi lớp đã compile, kết nối với UIManager trong code runtime:

```csharp
var ui = SAGE.AddressablesUI.UIManager.Create();
ui.AddressResolver = Game.Addresses.BaseScreenAddress.GetName;
await ui.ShowAndLoadScreenAsync<ShopScreen>(Game.Addresses.BaseScreenAddress.SHOPSCREEN);
```

## Resources và khác biệt với tool cũ

Dòng `isAddressable = false` yêu cầu asset có sẵn dưới `Assets/**/Resources/`.
Lớp sinh code trả về path tương đối, không có extension, để gọi `Resources.Load`.
UIManager của package hiện tải screen qua Addressables; bảng screen dùng với UIManager phải
chứa các dòng Addressable. IsResources/GetName của bảng mixed dùng cho loader riêng của project.

Tool không copy/xóa asset Resources, không xóa toàn bộ script trong folder output,
không gỡ Addressable entry của các dòng Resources, không thay đổi label ngoài cấu hình.
Chỉ ghi đè file có header do generator này sở hữu và bỏ qua việc ghi nếu code không đổi.
Kiểm tra key trùng, tên C# không hợp lệ, constant trùng và path output trước khi đăng ký asset.
Build không phải transaction: lỗi I/O hoặc Unity API sau bước kiểm tra có thể để lại thay đổi một phần.
Nó không build AssetBundle/catalog; chạy Addressables content build theo workflow của game sau đó.

Mã gốc được rà soát: `Editor/Tools/SO/AddressLibTool.asset`, các reference table,
`Editor/Templete/AddressReferenceTemplete.txt`, và các script `Scripts/Tools/Editor/AddressLibTool.cs`,
`AddressReferenceEditor.cs`, `AddressReferenceConfig.cs`, `AddressLibPostprocessor.cs`.
Postprocessor cũ có path và EditorPrefs cố định, phần gọi Build đang bị comment; package dùng
Build chủ động để tránh generate script trong vòng lặp import.

Không mang các `.asset` chứa GUID của game nguồn vào package. Tạo cấu hình mới hoặc chuyển dữ liệu
key/group/label/reference sang asset mới; GUID script và namespace đã thay đổi nên `.asset` cũ
không tự liên kết sang kiểu mới. Không có templatePath vì template đã được thay bằng generator C#.
