# Gamer Struggles — Liquid Glass Menu

Menu cheat + ESP phong cách Apple Liquid Glass cho **Gamer Struggles 0.2.4.1 (IL2CPP)**.

## Yêu cầu

- Game bản Windows IL2CPP.
- **BepInEx 6 bleeding-edge (IL2CPP)** đã cài trong thư mục game (bản đang dùng: `6.0.0-be.789`).
- Các interop assembly đã được BepInEx sinh ra trong `BepInEx/interop`.

## Cài đặt

1. Đóng game.
2. Chép `GamerStrugglesLiquidGlass.dll` vào:
   `Gamer Struggles\BepInEx\plugins\`
3. Mở game. Menu mở bằng **F1** hoặc **Insert**.

Không cần sửa file game gốc.

## Gỡ cài đặt

- Xoá `BepInEx\plugins\GamerStrugglesLiquidGlass.dll`.
- Cài đặt của mod được lưu bằng `PlayerPrefs` với tiền tố `lg_` (không tạo file cấu hình riêng).

## Phím tắt

| Phím | Chức năng |
| --- | --- |
| `F1` / `Insert` | Mở / đóng menu |
| `Esc` | Đóng menu khi đang mở |
| `F6` | Lưu vị trí hiện tại |
| `F7` | Quay lại vị trí đã lưu |

## Chức năng

### Nhân vật
- Bất tử, máu vô hạn.
- Chỉnh máu tối đa **10 – 9.999 HP** và máu hiện tại.
- Tự hồi máu, chỉnh tốc độ hồi.
- Hồi đầy máu.
- Một đòn hạ gục, hệ số sát thương **1x – 25x**.
- Tốc độ chạy **0.5x – 5x**, lực nhảy **0.5x – 3x**.
- Thể lực vô hạn, dash vô hạn, hút vật phẩm.
- Mở khoá double jump, dash và charge attack.

### Hiển thị (ESP)
- Bật/tắt ESP; chọn kẻ địch / vật phẩm.
- Khung, thanh máu, tên, khoảng cách, đường chỉ dẫn.
- Phạm vi quét **5 – 100 m**, độ mờ, cỡ chữ.
- Chỉ hiện mục tiêu còn sống.

### Thế giới
- Lưu / quay lại vị trí.
- Xem cấp và EXP hiện tại.
- Hệ số EXP **1x – 10x**, cộng 1.000 EXP, đặt EXP về 0.
- Tạm dừng game, tốc độ game **0.1x – 3x**.
- Kết liễu toàn bộ kẻ địch đang theo dõi.

### Cài đặt
- Giao diện sáng / tối.
- Giảm chuyển động.
- Tự tạm dừng khi mở menu (mặc định bật).
- Độ trong suốt khung kính.
- Khôi phục cài đặt mặc định.

## Ghi chú kỹ thuật

- Menu dùng IMGUI tự vẽ, không sửa scene/prefab của game.
- Game bị tạm dừng và input nhân vật bị khoá khi menu mở (tuỳ chọn `Tạm dừng khi mở menu`).
- ESP dùng `Camera.WorldToScreenPoint` và quét object mỗi 0,35 giây.
- Các cheat sát thương/EXP/God Mode dùng Harmony patch lên `PlayerHealth`, `EnemyHealth`, `PlayerExperience`, `PlayerStaminaController`.
- Cài đặt được lưu qua `PlayerPrefs`, tự lưu sau 2 giây khi có thay đổi.

## Build lại

```powershell
dotnet build ".\GamerStrugglesLiquidGlass.csproj" -c Release `
  -p:GameDir="C:\Users\duong\Documents\Game\GS 0.2.4.1(Windows DEMO)"
```

DLL đầu ra nằm trong `bin\Release\GamerStrugglesLiquidGlass.dll`.

## Trạng thái kiểm tra

- Build Release: **thành công, 0 warning, 0 error**.
- Chưa chạy thử trong game theo yêu cầu (không mở game). Các API game đã được đối chiếu với `dump.cs` và interop assembly, nhưng hành vi runtime (đặc biệt là ESP, input blocking và Harmony patch) chưa được xác nhận trực tiếp.
