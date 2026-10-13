# Khôi phục input chuột và bàn phím trong Unity Editor

## Nguyên nhân thực tế

Phiên người dùng đang chơi có chuột native ID 2 bị `enabled=false`, còn `Mouse.current` trỏ tới chuột giả ID 167. Bàn phím giả ID 166 cũng còn tồn tại. Các lượt chụp CLI trước đó tạo thiết bị và tắt chuột thật nhưng không dọn khi thoát Play Mode. Việc thoát Play không đảm bảo các thiết bị Input System do script Editor tạo ra biến mất.

Game lúc kiểm tra đang ở Intro, nơi di chuyển và mouse look chỉ bật sau Enter. Ngoài yêu cầu bắt đầu trận này, thiết bị thật bị chặn là lỗi cần sửa.

## Đã sửa

- Khôi phục ngay chuột/bàn phím native và loại bỏ các thiết bị giả có tên thuộc công cụ kiểm tra cũ.
- Thêm `Assets/Editor/ArenaReviewInputScope.cs`: sở hữu thiết bị giả, lưu thiết bị đang bật trước khi cách ly chuột, lưu thiết lập focus/background; dọn khi thoát Play Mode, trước reload assembly và khi đóng Editor.
- Hai công cụ `ArenaMouseOnlyReview` và `ArenaMouseGroundReview` dùng scope này. Callback chặn AI cũng được gỡ, enemy được bật lại khi kết thúc.
- `ArenaInputReview` cung cấp Inspect/Recover và kiểm chứng input bằng sự kiện mô phỏng gửi vào thiết bị native, không tạo chuột/bàn phím giả.

## Đã kiểm chứng

- Unity recompile: hoàn tất, không lỗi.
- Probe thiết bị native: PASS; yaw từ 0 thành 14,39999°, nhân vật di chuyển 44,37599 m khi giữ W trong lượt đo. Đây là sự kiện mô phỏng trên thiết bị native, không phải xác nhận trực tiếp thao tác tay của người dùng.
- Tái chạy đúng công cụ từng gây lỗi: Prepare → Start; lúc đo có hai thiết bị giả và chuột native tắt. Thoát Play Mode rồi Inspect **không gọi cleanup thủ công**: chỉ còn keyboard ID 1 và mouse ID 2, đều native/enabled; `Mouse.current=2`; thiết lập focus/background về giá trị trước kiểm tra.
- Đã xem ảnh và contact sheet; image review không có cờ lỗi. Bằng chứng tại `Artifacts/ArenaValidation/2026-10-13/input-recovery/`.
- Không thay đổi runtime gameplay trong lượt sửa này; kết quả 24/24 đấu trường và bản Windows từ commit 045f1e0 vẫn là kết quả lần chạy trước, không chạy lại trong lượt sửa input Editor.

Khi thử thủ công: mở Game View, Play, Enter để vào trận; chuột xoay góc nhìn, WASD di chuyển; Esc thả chuột/tạm dừng.
