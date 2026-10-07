# Chuyển bản Thánh Gióng từ Godot sang Unity

Nguồn: `E:/Game/pru`. Project Unity: `E:/Game/Game_thanhgiong`.
Kết nối MCP đã chọn `Game_thanhgiong@a7c0dbbb`, Unity 6000.6.0f1.

## Nội dung đã tích hợp

- Sáu map chiến dịch trong `Assets/Scenes/ThanhGiongWorld`, cùng nhân vật mới ở map thử nghiệm `AlbionForestMap`.
- Môi trường xuất thành GLB, collider sàn/cầu/biên được dựng lại bằng collider Unity. Chuyển hệ tọa độ và giữ các điểm tương tác của chiến dịch Unity.
- Nhân vật có hai skinned meshes: ngựa 17 xương, Gióng 21 xương. Animation procedural chạy bằng C#: cưỡi ngựa, bước chân, đuôi, áo choàng, đội/tháo nón, mặc/cởi giáp. Dùng toàn bộ scaled delta time và chia bước solver để animation không kéo dài khi FPS thấp.
- Tre ngà, cành/lá/rễ và trạng thái vũ khí; phản hồi chém, va chạm, bụi chân ngựa.
- Shader URP cho đất/đường, gió cây tre, vải và nước; hai búa rèn, ánh lửa, khói lò và tia lửa va đập bằng ParticleSystem Unity chạy theo thời gian pause của game. Màu vertex trên thân tre là mặt nạ đốt tre, không phải màu RGB.
- UI Kenney đỏ sẫm/vàng đồng: nhiệm vụ 6 giây, hướng dẫn 4 giây, Tab gọi lại, F1 trợ giúp, Esc tạm dừng. Menu pause có chơi lại màn và chọn bảy map.
- 26 WAV gốc từ bản Godot trong `Assets/Resources/ThanhGiongAudio`, nhạc/ambience theo sáu vùng và hiệu ứng gameplay. Đây là âm thanh tổng hợp đã tạo cho project.
- Sinh lực, thời gian miễn sát thương, khóa điều khiển khi chết và hồi sinh trên cùng nhân vật/enemy.

Đây là bản chuyển hành vi sang Unity C#/URP. Các file `.gd`, `.tscn` và shader Godot không chạy trực tiếp trong Unity; project nguồn vẫn được giữ riêng. Chưa tạo build phát hành.

## Tái nhập và kiểm tra

1. Trong Godot chạy scene `res://tools/export_unity_port.tscn` để xuất sáu môi trường, nhân vật, tre và dữ liệu collider vào project Unity.
2. Trong Unity, thoát Play mode, chọn `Tools > Thanh Giong > Godot Port > Reimport Models`, sau đó `Import All Maps`. Bước import thay thế môi trường/nhân vật và lưu các scene.
3. Chọn `Tools > Thanh Giong > Godot Port > Verify Play Mode`. Bộ kiểm tra chuyển qua bảy map, kiểm tra rig, chạm đất, âm thanh, bốn animation trang bị, pause, popup và sinh lực/hồi sinh. Kết quả và ảnh Game view nằm ở `PortVerification`.

Các file chính: `Assets/Code/GodotPort`, `Assets/Editor/GodotPort`, `Assets/GodotPort`, cùng controller/HUD/audio Unity hiện hữu được cập nhật.

## Thay đổi cấu hình và phục hồi

GPU Resident Drawer của PC URP được tắt sau khi quan sát lỗi BatchDrawCommand khi thay mesh/material lúc chạy. Texture con trỏ được nhập lại dưới dạng Cursor/readable. Các scene và một số file trước chuyển đổi được lưu tại `Backups/GodotPort_20261007`; thư mục này là bản sao bổ sung, không phải snapshot đầy đủ của toàn bộ repository.

Kiểm tra Play mode ngày 2026-10-07: **61 kiểm tra PASS, 0 thất bại** qua bảy map, bao gồm chọn chương/chơi lại qua menu pause, rig, va chạm sàn, bốn thao tác trang bị, popup hết hạn/gọi lại, pause âm thanh/môi trường và sát thương/miễn thương/chết/hồi sinh. Hai lò rèn có sáu hệ particle native. Console không có error sau lần chạy này; vẫn có cảnh báo Account API của Unity không truy cập được trong 30 giây.

Kết quả chi tiết xem `PortVerification/runtime_results.txt`, ảnh trực tiếp Game view cùng thư mục. Không suy ra chất lượng âm thanh đã nghe, build phát hành hoặc mọi tổ hợp input từ các kiểm tra trên.
