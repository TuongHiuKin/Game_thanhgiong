# Thanh Giong Hero V2 Asset Pack

`Assets/fbcd2ed9-1e6d-4f6a-b216-a1d89c359a7d` là thư mục nguồn của bộ nhân vật 3D Thánh Gióng V2. Đây là asset nhân vật humanoid chất lượng cao dùng làm Thần Tướng Phù Đổng: giáp đồng, hoa văn Đông Sơn, dải lụa đỏ, texture PBR và các prefab kéo thả.

## Cấu trúc đã chuẩn hoá

- Nguồn gốc giữ nguyên: `Assets/fbcd2ed9-1e6d-4f6a-b216-a1d89c359a7d/`
- Model và prefab dùng trong project: `Assets/Art/Models/ThanhGiong_Hero_V2/`
- Texture dùng trong project: `Assets/Art/Textures/ThanhGiong_Hero_V2/`
- Material dùng trong project: `Assets/Art/Materials/ThanhGiong_Hero_V2/`

## Prefab chính

- `ThanhGiong_Hero_V2_Painted.prefab`: bản khuyến nghị cho gameplay 2.5D/isometric vì dùng shader tranh vẽ `ThanhGiong/LegendSurface`.
- `ThanhGiong_Hero_V2_PBR.prefab`: bản kiểm tra ánh sáng vật lý với URP Lit, đầy đủ albedo, normal, metallic/smoothness và occlusion.
- `ThanhGiong_Hero_V2_Shaded.prefab`: bản dùng texture đổ bóng sẵn, phù hợp preview nhanh hoặc phong cách cel-shade nhẹ.
- `ThanhGiong_Hero_V2_Monument.prefab`: bản tượng đài có bệ đá, dùng để đặt tại Làng Phù Đổng, quảng trường hoặc Đỉnh Sóc.

## Cách cập nhật lại

Chạy menu Unity:

`Tools/Thanh Giong/Update Hero V2 Asset Pack`

Menu này chuẩn hoá import FBX/texture, tạo lại material trong thư mục Art và dựng lại 4 prefab Art từ asset chuẩn.
