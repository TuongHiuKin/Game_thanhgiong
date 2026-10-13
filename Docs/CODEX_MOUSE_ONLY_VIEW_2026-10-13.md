# Chỉ chuột điều khiển hướng khung hình

Yêu cầu tiếp nối ngày 13/10/2026: dấu cộng cố định giữa màn hình, chỉ chuột xoay camera; WASD không xoay khung hình.

- `arenaYaw` là hướng tuyệt đối trong thế giới, không cộng góc quay ngựa. Cả camera qua vai và vị trí mắt thứ nhất dùng hướng này.
- Camera theo vị trí nhân vật nhưng không theo góc quay nhân vật.
- W/S tiến/lùi theo góc nhìn; A/D di chuyển ngang trái/phải. Ngựa nhìn theo hướng camera; phím di chuyển không điều khiển góc nhìn.
- Chuột xoay ngang/dọc ngay trong trận, tâm cố định, Esc thả chuột; V/cuộn zoom và sửa nền/tay trước đó được giữ.
- Bổ sung kiểm tra đủ W/A/S/D ở cả ba camera và cố tình thay đổi góc quay ngựa: nhân vật phải đi đúng hướng, góc camera giữ nguyên.
- Bản Windows có thêm phép đo `maximumKeyboardViewRotation` trong runtime verifier.

Kết quả kiểm chứng cuối nằm tại `Artifacts/ArenaValidation/2026-10-13/mouse-only-view/`.

## Kết quả đã chạy

- Kiểm tra Editor: 4/4 đấu trường và 3/3 chân ngựa; PlayMode: 17/17. Tổng kiểm tra liên quan đấu trường: **24/24 đạt**. Không chạy lại toàn bộ suite campaign trong lượt sửa này.
- Bản Windows x64 build thành công, 0 lỗi, 28 cảnh báo; file chạy tại `Builds/PhuDongArena/ThanhGiongArena.exe`.
- Runtime Windows: **PASS**, 0 lỗi runtime; góc xoay camera tối đa khi giữ W/D/S/A là **0°**, chuột xoay được **30,30°**. Tâm lệch **0 pixel** qua 15.961 mẫu khi di chuyển.
- Kiểm tra đủ W/A/S/D ở cả ba góc nhìn: chuyển động đúng hướng camera, camera không xoay theo ngựa.
- Ảnh Editor trước/sau giữ D: yaw giữ **37°**, pitch giữ **24°**, nhân vật dịch chuyển khoảng **6,7 m**. Sau di chuột, yaw thành **58,6°**, pitch **28,8°**; tâm vẫn tại `(640, 360)` trong khung 1280×720.
- Đã xem ba ảnh Editor và contact sheet gồm thêm hai ảnh bản Windows. Không có cờ lỗi hình ảnh trong `image-review.json`; bàn tay/vũ khí vẫn hiện, nền không có mặt đất chồng nhau.
- Thoát Play Mode sau kiểm chứng để khôi phục thiết bị chuột và trạng thái Editor bình thường. Bộ giả lập input/chặn AI trong `ArenaMouseOnlyReview.cs` chỉ dùng trong lượt chụp kiểm chứng, không lưu vào scene hoặc bản Windows.

Điều này thay thế hướng di chuyển W/S tiến theo ngựa, A/D quay ngựa trong báo cáo trước. Scene không cần dựng lại: script và các giá trị scene hiện có tương thích.
