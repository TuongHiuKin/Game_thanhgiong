# Tâm cố định, xoay chuột kiểu PUBG và sửa nền nhấp nháy

Yêu cầu cuối cùng ngày 13/10/2026: giữ dấu cộng giữa màn hình, di chuột để xoay toàn bộ góc nhìn như PUBG. Không dùng tâm bám vị trí con trỏ.

## Thay đổi

- Unity CLI đo được `Grass plain` và `Walkable ground` cùng có mặt trên tại y = -0.100000 m. Hai mặt cỏ/đất nâu cùng render gây z-fighting. `Walkable ground` giờ chỉ giữ collider; cỏ vẫn hiển thị và sân đất tròn có mặt trên tại y = -0.08 m, cao hơn cỏ 2 cm.
- Tâm luôn ở giữa màn hình trong cả ba góc nhìn. Chuột được khóa và ẩn khi đánh giặc/tìm tre; di chuột xoay ngang/dọc camera mà không phải giữ nút chuột.
- Esc/tạm dừng, intro, kết quả và cinematic thả chuột để dùng menu; tạm dừng không làm camera xoay. Tiếp tục trận khóa chuột lại.
- Bỏ thao tác camera luôn nhìn vào điểm phía trước ngựa: thao tác cũ hủy hướng ngắm bằng chuột. Camera dùng yaw/pitch chuột và vị trí qua vai, có chặn vị trí dưới mặt đất.
- Hướng cận chiến lấy từ tia giữa camera trên mặt phẳng ngang cao 1.25 m so với gốc nhân vật. Báo mục tiêu, sát thương, bước tiến khi đánh và hiệu ứng chém dùng cùng hướng. Hướng được giữ tại thời điểm bấm, không đổi trong thời gian vung đòn. Ngắm lên trời dùng hướng ngang camera; tre vẫn quét tròn theo thiết kế.
- Điều khiển di chuyển hiện có giữ W/S tiến/lùi, A/D đổi hướng, Shift chạy, V đổi góc, cuộn zoom. Chuột tự xoay camera, không tự xoay gốc ngựa, tránh vòng lặp xoay camera/nhân vật.
- Bộ lọc skin khôi phục lòng bàn tay và loại bỏ giáo/chân thừa của bản trước giữ nguyên.

## Kiểm chứng

Kết quả cuối và ảnh nằm tại `Artifacts/ArenaValidation/2026-10-13/center-mouse-look-ground/`.

- Unity 6000.6.0f1 biên dịch không lỗi. Scene dựng lại bằng Unity CLI và lưu qua Editor.
- Editor: 4/4 test scene/skin/nền; 3/3 test bốn chân ngựa đạt.
- Play Mode: 16/16 test đấu trường đạt, gồm chuột xoay cả 3 camera không giữ nút, tâm cố định khi chạy/quay/rung, tạm dừng thả chuột và ngừng mouse look, đánh bên hông và giữ hướng lúc bấm, zoom/đổi góc/lùi/vòng đấu.
- Windows development: build thành công, 0 lỗi; 524 cảnh báo shader/package đã có ở các bản kiểm chứng trước. Runtime verifier PASS, 0 lỗi runtime, xoay chuột 30.30°, tâm lệch 0 px qua 15,381 mẫu di chuyển, điểm ngắm chiếu lên tâm lệch tối đa 0.000137 px.
- Kiểm tra nền collider không render; khoảng cách camera tại biên 12.61 m với cách đặt camera mới. Đòn đánh đẩy lùi ngang 0.118 m, không nâng giặc lên trời.
- Ảnh HUD: góc nhìn quay từ yaw -28.8°/pitch 23.2° sang yaw 21.6°/pitch 32.8°; cả hai ảnh 1280×720 có tâm ở (640,360), chuột Locked. Ảnh cuối dùng bộ chuyển động ngựa đang hoạt động, không phải tư thế bind pose khi tắt controller.
- Đã mở bản ghép 12 ảnh; kiểm tra tự động không phát hiện ảnh trắng/đen/trống/lỗi màu. Pixel chính giữa hai ảnh HUD cùng RGB (249,248,246), xác nhận dấu tâm được vẽ tại giữa khung hình.

Không chạy lại suite campaign cũ ở lượt này. Báo cáo bàn giao trước liệt kê 3 lỗi campaign độc lập; không coi toàn dự án đã đạt mọi test.

## Công cụ tái kiểm tra

- `Tools/UnityDiagnostics/ArenaMouseGroundReview.cs`: đo các lớp nền, tạo chuột kiểm tra, gửi delta chuột không nhấn nút và chụp HUD khi góc nhìn xoay trái/phải bằng Unity CLI.
- `ThanhGiongArenaPresentationVerifier`: cờ `--verify-arena-presentation` trong bản Windows development; chạy thao tác Input System thật và ghi JSON/capture. Capture camera trong player không có HUD; ảnh HUD được kiểm tra riêng trong Editor.

Scene đã cập nhật: `Assets/Scenes/ThanhGiongArena/PhuDongArena.unity`.
