# Validation

Ngày kiểm tra: 2026-10-24.

- Runtime assembly và sample assembly đã compile thành công bằng C# compiler đi kèm
  Unity 2022.3.62f3, language version 9, netstandard 2.1, bật warnings-as-errors.
- Reference đến UnityEngine DLL của Unity 2022.3.62f3 và Unity.Addressables,
  Unity.ResourceManager, UnityEngine.UI DLL đã có trong Library/ScriptAssemblies của project nguồn.
- Kiểm tra asmdef uGUI với package nguồn: reference đúng là `UnityEngine.UI`.
- Phiên bản 0.2.0: Editor assembly compile thành công với UnityEditor và Unity.Addressables.Editor
  từ project nguồn, dùng Unity .NET Framework 4.8 reference assemblies và warnings-as-errors.
- Generator vượt qua 13 kiểm tra về validation/output; code sinh ra compile thành DLL và vượt qua
  6 kiểm tra lookup/literal round trip/Resources/constant. Tổng cộng 19 kiểm tra tự động đã pass.
- Các file JSON/asmdef được parse và metadata GUID được kiểm tra trùng lặp trước khi nén.

Đây là kiểm tra biên dịch trực tiếp, chưa phải xác nhận import/resolve package trong Unity Editor.
Chưa chạy Play Mode, build player, tải remote catalog, hoặc kiểm tra runtime WebGL.
Chưa chạy các API thay đổi AssetDatabase/Addressables settings trong Unity Editor.
Package không kèm prefab, scene, catalog hay art của game; các bước kiểm tra runtime cần asset
Addressable do project sử dụng package cung cấp. Xem checklist trong README.md.
