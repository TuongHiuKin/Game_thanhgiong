# DỰ ÁN GAME THÁNH GIÓNG (THANH GIONG CAMPAIGN)
## Cấu Trúc Thư Mục Chuẩn Hóa
```
Assets
│── _Project/               # Tài liệu nội bộ, cốt truyện, hướng dẫn, screenshots
│── Animations/             # Animation clips (.anim) và Animator Controllers (.controller)
│── Art/                    # Tài nguyên đồ họa tự tạo (Models, Textures)
│   ├── Models/             # 3D models chính (base mesh, Hero, Ngựa sắt)
│   └── Textures/           # PBR Textures (Albedo, Normal, Metallic, Roughness)
│── Audio/                  # Nhạc nền dân gian ngũ cung & hiệu ứng âm thanh (BGM, SFX)
│── Code/                   # Toàn bộ mã nguồn C# của game
│   ├── Player/             # Điều khiển nhân vật Thánh Gióng, cưỡi ngựa, vươn vai, húc kẻ địch
│   ├── Enemies/            # AI giặc Ân, Tướng giặc Warlord, hiệu ứng vật lý Ragdoll
│   ├── UI/                 # HUD hiển thị tiến độ, mục tiêu, thanh nhiệt độ ngựa sắt
│   └── Scripts/            # Hệ thống chung: Camera, Audio, VFX, Lương thực, Vũ khí môi trường
│── Materials/              # Toàn bộ Material URP Lit và Shader
│   ├── Food/               # Vật liệu Cơm trắng, Cà pháo muối, Cuống cà
│   ├── World/              # Vật liệu địa hình, đường sá, đá, nước, cỏ
│   ├── Characters/         # Vật liệu nhân vật, giáp sắt, áo vàng, ngựa sắt
│   └── Campaign/           # Vật liệu khóm tre ngà, quầng lửa, đồn trại
│── Prefabs/                # Các Prefab tái sử dụng
│   ├── Player/             # Prefab Thánh Gióng cưỡi ngựa, chiến binh, ngựa sắt
│   └── Environment/        # Prefab cảnh quan, đồn trại
│── Scenes/                 # Các màn chơi chính (.unity)
│   ├── AlbionForestMap.unity   # BẢN ĐỒ CHIẾN DỊCH CHÍNH (4 MÀN CHƠI TUẦN TỰ)
│   └── ThanhGiongWorld/        # Các scene phân cảnh phụ
│── UI/                     # Tài nguyên hình ảnh UI, Icon, Font
│── VFX/                    # Hiệu ứng hạt (Particle Systems, Line Renderers)
│── Plugins/                # Gói tài nguyên bên thứ 3 (KayKit Packs, Kenney Forest)
│── Resources/              # Tài nguyên load động lúc runtime
│── StreamingAssets/        # Tệp giữ nguyên không nén
└── Editor/                 # Công cụ tùy biến Unity Editor
```

## Bốn Màn Chơi Chiến Dịch Tuần Tự
1. **Màn 1: Tiếng Rao Dưới Mái Tranh (Làng Phù Đổng)**
   - Thu thập 300/300 lương thực (Cơm gạo, Cà pháo, Bó củi, Mâm thịt) từ dân làng.
   - Gióng vươn vai hóa Khổng Lồ, mái nhà tranh sập đổ.
2. **Màn 2: Rèn Thép & Xuất Quân (Lò Rèn Triều Đình)**
   - Chuỗi QTE 4 bước: [E] Mặc giáp vàng -> [Q] Đội nón sắt -> [E] Lên ngựa sắt -> [F] Phun Hỏa Tuyến kiểm tra vũ khí.
3. **Màn 3: Trận Tuyến Núi Sóc (Càn Quét Giặc Ân)**
   - Đánh gãy gươm sắt ở 7 mạng -> Nhổ khóm tre ngà [E x3] -> Quét sạch 18 quân thù và Tướng giặc.
4. **Màn 4: Hóa Thánh Về Trời (Đỉnh Núi Sóc)**
   - Nghi thức cởi giáp trên đỉnh núi [E -> Q -> E] -> Ngựa sắt bay vút lên trời xanh.
