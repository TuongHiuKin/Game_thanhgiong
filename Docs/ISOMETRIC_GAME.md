# Thánh Gióng: hướng chơi 2.5D/isometric

Áp dụng cho sáu chương chiến dịch và map rừng thử nghiệm của project Unity.

## Mỹ thuật và chuyển động

Camera orthographic, pitch 35.264°, yaw 45°, giữ hướng cố định và chuyển zoom giữa khám phá, chiến đấu, boss, cảnh rộng. Chuyển động camera dùng vận tốc thực, có look-ahead và rung giới hạn; pause đóng băng camera/animation gameplay.

Phong cách dùng lại assets của project: đất/đường biến thiên mềm, xanh rêu và xám nước, điểm nhấn đồng/vàng, ánh sáng mềm, fog, bloom nhẹ, grain và vignette. Vật liệu nhân vật được chuyển sang shader tranh vẽ và giữ nguyên texture/rig. Địch dùng Barbarian/Knight có animation của KayKit, thay capsule tạm. Cảnh có gió tre/vải, hạt bụi/đom đóm, vòng gợn nước, che khuất tiền cảnh bằng dither và đốm sáng nhận diện nhân vật.

Các giới hạn môi trường: tối đa 36 hạt không khí, 12 vòng gợn, bốn tia nắng vàng mờ và 128 renderer ứng viên che tiền cảnh. Tia nắng neo trong thế giới; chuyển pha dừng khi pause. Đồ trang trí mới không có collider. Lò rèn vẫn có hai búa, lửa, khói và tia lửa. Model quân địch sống dò mặt đất tám lần/giây và hiệu chỉnh riêng phần hình ảnh; giữ vật lý, va chạm và chuyển động khi bị đánh bật.

## Điều khiển

| Phím | Hành động |
|---|---|
| WASD / Shift | Di chuyển / phi ngựa |
| Chuột trái | Đánh cận chiến; chuẩn bị 0.12 giây, chuỗi ba nhịp, đòn ba mạnh hơn 15% |
| C / Left Alt / Left Ctrl | Né trên ngựa: 4 m trong 0.38 giây, hồi 1 giây, miễn sát thương 0.24 giây |
| Space | Nhảy |
| E / F | Tương tác / phun lửa |
| 1–5 / Q | Chọn / gieo hạt khi khám phá hoặc chiến đấu |
| Tab / F1 / Esc | Xem lại nhiệm vụ / trợ giúp / tạm dừng |

Gióng luôn cưỡi ngựa trong vòng chơi hiện tại, nên C / Alt / Ctrl là bước né của ngựa kèm nghiêng yên thay cho animation lăn người xuống đất. Né dùng CharacterController chia bước va chạm, không xuyên tường. Trong hai chương trang bị/hóa Thánh, Q vẫn dùng cho nón; các kỹ năng hạt bị khóa để tránh xung đột.

## Năm hạt thiêng

| Phím | Hạt | Hiệu ứng | Linh lực | Hồi |
|---|---|---|---:|---:|
| 1 | Quang căn (Lumen) | Giữ chân lính 4 giây, boss 1 giây, không hất văng; giữ cơ chế sơ hở của boss | 20 | 6 s |
| 2 | Mầm tre | Cụm tre gây sát thương theo nhịp | 30 | 8 s |
| 3 | Sen hồi phục | Hồi sinh lực khi Gióng đứng trong vùng | 25 | 9 s |
| 4 | Hạt lửa | Sát thương vùng trong thời gian ngắn | 35 | 10 s |
| 5 | Hạt gió | Đánh bật/choáng mục tiêu vào vùng | 20 | 6 s |

Mỗi loại có ba lượt dùng tại trạng thái checkpoint, linh lực tối đa 100 và hồi 4/giây. Q gieo phía trước ngựa; giới hạn 8 m, cần đất bằng và đường nhìn không bị tường chắn. Tối đa 12 phép đang hoạt động, tồn tại 8 giây, riêng lửa 4 giây. Hạt không dùng được lúc pause, đang né, chết hoặc trang bị. Kỹ năng không phải bản sao assets của Understory.

## Checkpoint và âm thanh

Checkpoint tự lưu đầu chương và khi đi vào vòng ký hiệu mặt trời đồng. Trong pause, **Từ điểm dừng chân** khôi phục vị trí, HP, tiến độ/chương/trang bị, địch đã hạ/còn sống, collectible và tài nguyên hạt. **Chơi lại màn** xóa checkpoint của map và bắt đầu lại. Tiến độ lưu bằng PlayerPrefs theo scene; snapshot định dạng v1. Phép đang nở, telegraph, root và bộ đếm AI/combat được đặt lại khi phục hồi, không tiếp tục các hiệu ứng tạm dở dang.

HUD dùng Kenney đỏ sẫm trong suốt, tiêu đề/nhiệm vụ và lời dẫn có thời gian, HP nhỏ và hotbar năm hạt. Esc có ba thanh âm lượng tổng/nhạc/hiệu ứng áp dụng lên AudioSource thực, lưu lựa chọn và hạ nhạc mềm khi hiệu ứng sự kiện phát. Giữ 26 WAV tổng hợp gốc, nhạc/ambience theo vùng và tiếng vó theo nền đất/gỗ/đá/ướt. Chưa đánh giá chất âm bằng nghe trực tiếp.

## Nhập lại và xác minh

`Tools > Thanh Giong > Isometric > Apply To All Maps` áp dụng camera, shader/môi trường, model địch, profile URP, kỹ năng/checkpoint và lưu bảy scene. Bản sao scene trước đổi phong cách nằm trong `Backups/Isometric_20261007`.

Hai material tham chiếu trong `Assets/Resources/LegendMaterials` giữ shader tùy biến cho build dùng vật liệu tạo lúc chạy.

`Tools > Thanh Giong > Isometric > Verify All` chạy bộ hồi quy chiến dịch trước, rồi bộ kiểm tra isometric. Hai báo cáo nằm ở `PortVerification/runtime_results.txt` và `IsometricVerification/runtime_results.txt`, cùng ảnh Game view. Kiểm tra cô lập và khôi phục checkpoint/âm lượng đã lưu của người chơi, kể cả khi dừng Play sớm; file sao lưu tùy chọn ở `IsometricVerification/preferences_backup.json`.

Kết quả Play mode ngày 07/10/2026: 61/61 kiểm tra chiến dịch và 98/98 kiểm tra isometric đạt, không có lỗi Console. Đã kiểm tra tất cả bảy scene, va chạm khi né, pause, combo, năm hạt/Lumen/hồi phục, khôi phục địch và tài nguyên checkpoint, âm lượng AudioSource, ducking nhạc, tia nắng, gợn nước và nền đỡ model địch. Checkpoint/âm lượng gốc được khôi phục sau kiểm tra. Một số model KayKit vẫn báo cảnh báo importer MaterialLocation.External đã lỗi thời; đây không phải lỗi runtime.

Chưa tạo build phát hành. Đây là chuyển đồ họa/camera và phát triển gameplay trên Unity; project Godot nguồn vẫn được giữ riêng.
