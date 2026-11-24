# Gamer Struggles 0.2.4.1 (Windows DEMO) — Việt Hóa & Mod Menu

Bản **Gamer Struggles 0.2.4.1 (Windows DEMO)** đã được tích hợp sẵn **Việt hóa** (hội thoại, UI, font tiếng Việt) và **Mod Menu Liquid Glass** (BepInEx 6 IL2CPP + ESP + Cheat Menu).

---

## Yêu cầu & Cách tải về (Quan trọng)

Repository này sử dụng **Git LFS** để lưu các file dung lượng lớn (> 50 MB). Nếu tải bằng nút *Download ZIP* trên GitHub, các file LFS có thể chỉ là file con trỏ vài trăm byte khiến game không chạy được.

Hãy dùng Git + Git LFS để clone đầy đủ:

```powershell
# 1. Cài Git LFS (nếu chưa có): https://git-lfs.com
git lfs install

# 2. Clone repository (tự động tải kèm các file LFS)
git clone https://github.com/duongvantrong18102012-cmd/gamer-struggles-viethoa.git
```

Nếu đã clone trước đó nhưng thiếu file lớn, chạy lệnh sau trong thư mục repo:

```powershell
git lfs pull
```

> **Danh sách file được quản lý bởi Git LFS:**
> - `GameAssembly.dll`
> - `Gamer Struggles_Data/resources.assets.resS`
> - `Gamer Struggles_Data/sharedassets1.assets.resS`
> - `Gamer Struggles_Data/sharedassets37.assets.resS`
> - `Gamer Struggles_Data/sharedassets78.assets.resS`

---

## Cách chơi

1. Đảm bảo đã tải đầy đủ file (kể cả Git LFS).
2. Chạy file `Gamer Struggles.exe` trong thư mục gốc.
3. Trong game, ngôn ngữ vẫn hiển thị tên là **English** trong menu cài đặt, nhưng nội dung đã được Việt hóa.

---

## Chi tiết bản Việt hóa

Các hạng mục đã được xử lý trực tiếp vào dữ liệu game trong `Gamer Struggles_Data`:

1. **Bảng chuỗi hội thoại & UI (`StreamingAssets/aa/StandaloneWindows64/localization-string-tables-english(en)_assets_all.bundle`)**
   - Dịch **3.033 / 3.033** chuỗi hội thoại và giao diện sang tiếng Việt (kết hợp máy dịch và chỉnh tay các nhãn UI quan trọng).
2. **Tin tức & Nhật ký thay đổi (`localization-assets-english(en)_assets_all.bundle`)**
   - Dịch các mục *News*, *ChangeLog*, *NewFeatures*.
3. **Text UI tĩnh trong các màn chơi (`level0` .. `level87`, `resources.assets`)**
   - Dịch **1.427** đoạn text UI tĩnh (nhãn nút, tên khu vực, hướng dẫn...).
4. **Font hiển thị dấu tiếng Việt (`resources.assets` — TMP Settings)**
   - Bổ sung `LiberationSans SDF - Fallback` vào danh sách font dự phòng để hiển thị đầy đủ dấu tiếng Việt (`ả`, `ệ`, `ộ`...).
5. **Addressables Catalog (`StreamingAssets/aa/catalog.bin`)**
   - Đặt `CRC = 0` cho 2 bundle đã chỉnh sửa để Addressables không chặn khi nạp asset.

### Ghi chú bản dịch
- Một số nhãn ngắn đặc thù được giữ nguyên: `HP`, `ATK`, `CUMBUSTERS`, `1x`...
- Tên ngôn ngữ trong menu vẫn là `English` (nội dung bên trong là tiếng Việt).

---

## Chi tiết Mod Menu — Liquid Glass

Game đi kèm **BepInEx 6 (IL2CPP)** và plugin `GamerStrugglesLiquidGlass.dll` cài sẵn tại `BepInEx/plugins/GamerStrugglesLiquidGlass.dll`. Mã nguồn của mod nằm trong [`_mod/GamerStrugglesLiquidGlass/`](_mod/GamerStrugglesLiquidGlass/README.md).

### Phím tắt

| Phím | Chức năng |
| --- | --- |
| `F1` / `Insert` | Mở / đóng menu |
| `Esc` | Đóng menu khi đang mở |
| `F6` | Lưu vị trí hiện tại |
| `F7` | Quay lại vị trí đã lưu |

### Các nhóm chức năng
- **Nhân vật:** Bất tử / máu vô hạn, chỉnh máu tối đa (10 – 9.999 HP) và máu hiện tại, tự hồi máu, hồi đầy máu, một đòn hạ gục, hệ số sát thương (1x – 25x), tốc độ chạy (0.5x – 5x), lực nhảy (0.5x – 3x), thể lực & dash vô hạn, hút vật phẩm, mở khóa double jump / dash / charge attack.
- **Hiển thị (ESP):** Bật/tắt ESP cho kẻ địch và vật phẩm (khung, thanh máu, tên, khoảng cách, đường chỉ dẫn), phạm vi quét 5 – 100 m, độ mờ, cỡ chữ, lọc mục tiêu còn sống.
- **Thế giới:** Lưu / quay lại vị trí, xem cấp & EXP hiện tại, hệ số EXP (1x – 10x), cộng 1.000 EXP / đặt EXP về 0, tạm dừng game, tốc độ game (0.1x – 3x), kết liễu toàn bộ kẻ địch đang theo dõi.
- **Cài đặt:** Giao diện sáng / tối, giảm chuyển động, tự tạm dừng khi mở menu, độ trong suốt khung kính, khôi phục mặc định.

### Gỡ mod (nếu chỉ muốn chơi bản Việt hóa thuần)
- Xóa file `BepInEx/plugins/GamerStrugglesLiquidGlass.dll` (hoặc xóa `winhttp.dll` nếu muốn tắt toàn bộ BepInEx).
- Xem thêm hướng dẫn build lại mod tại [`_mod/GamerStrugglesLiquidGlass/README.md`](_mod/GamerStrugglesLiquidGlass/README.md).

---

## Cấu trúc thư mục chính

```text
.
├── Gamer Struggles.exe          # File chạy game chính
├── GameAssembly.dll             # Mã IL2CPP của game (Git LFS)
├── UnityPlayer.dll              # Engine Unity
├── Gamer Struggles_Data/        # Dữ liệu game (đã áp dụng Việt hóa)
├── BepInEx/                     # Mod loader BepInEx 6 (IL2CPP)
│   └── plugins/
│       └── GamerStrugglesLiquidGlass.dll
├── _mod/
│   └── GamerStrugglesLiquidGlass/   # Mã nguồn C# của Mod Menu Liquid Glass
├── _viethoa/                    # Công cụ phụ trợ Việt hóa
├── dotnet/                      # Runtime .NET cho BepInEx IL2CPP
├── D3D12/                       # Thư viện Direct3D 12
├── doorstop_config.ini          # Cấu hình Unity Doorstop (nạp BepInEx)
├── winhttp.dll                  # Proxy DLL của Doorstop
└── .gitattributes               # Cấu hình theo dõi Git LFS
```
