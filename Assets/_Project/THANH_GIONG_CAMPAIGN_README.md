# Thánh Gióng — Vertical Slice

Scene chơi chính: `Assets/Scenes/AlbionForestMap.unity`.

## Điều khiển

- `WASD`: di chuyển
- `Left Shift`: phi nhanh
- `Space`: chém Gươm Sắt / quét Tre Ngà 360 độ
- `F`: Hỏa Tuyến khi Heat Bar đầy
- `E`, `Q`: chuỗi QTE trang bị, nhổ tre và hóa thánh

## Luồng chơi

1. Thu thập đủ 12 phần lương thực quanh làng. Gióng tăng kích thước qua 4 phase.
2. Hoàn thành QTE `E → Q → E`, rồi nhấn `F` để xuất quân.
3. Đi tới trận tuyến Núi Sóc, hạ quân Ân. Sau 7 địch, Gươm Sắt gãy.
4. Tới Khóm Tre Ngà phát sáng, nhấn `E` đủ 3 lần để nhổ tre.
5. Quét Tre Ngà, tích Heat và dùng Hỏa Tuyến. Hạ đủ 18 địch.
6. Hoàn thành QTE `E → Q → E` để cởi giáp và cưỡi Ngựa Sắt bay về trời.

## Cấu trúc kỹ thuật

- `ThanhGiongCampaignController`: state machine 4 màn, tăng trưởng, vũ khí, Heat, combat và QTE.
- `ThanhGiongEnemy`: AI nhẹ, damage, stun, boss flag.
- `ThanhGiongCollectible`: lương thực, tre và vật phẩm môi trường.
- `ThanhGiongCampaignVFX`: vòng nét cọ, quét tre, gươm gãy và Hỏa Tuyến.
- `ThanhGiongCampaignAudio`: nhạc/SFX tổng hợp runtime làm placeholder.
- `ThanhGiongCampaignHUD`: HUD nhẹ hiển thị mục tiêu, tăng trưởng và Heat.
- `ThanhGiongCampaignBuilder`: tái tạo toàn bộ nội dung scene qua menu `Tools/Thanh Giong/Build Four-Chapter Campaign`.

Đây là vertical slice có thể chơi, dùng placeholder tối ưu thay cho hàng trăm quân và asset sản xuất cuối. Các điểm mở rộng đã tách riêng để thay model, animation, nhạc thu và pool quân về sau.
