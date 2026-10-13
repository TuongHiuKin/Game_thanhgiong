# Tâm đánh cố định và khôi phục bàn tay — 13/10/2026

Scene: `Assets/Scenes/ThanhGiongArena/PhuDongArena.unity`.

## Điều khiển và camera

- Tâm đánh cố định tại `(Screen.width / 2, Screen.height / 2)` trong cả ba góc nhìn, vẫn đổi màu khi có mục tiêu trong tầm melee.
- Camera thứ ba bám hướng ngựa thay vì hướng cố định của bản đồ. Cảnh quan xoay trên màn hình khi ngựa đổi hướng; các object của scene giữ nguyên tọa độ.
- W/S đi tiến/lùi theo hướng ngựa; A/D quay trái/phải, có thể vừa W vừa A/D để đi theo đường cong. Đi lùi không đảo hướng ngựa/camera. Điều khiển campaign cũ giữ cách di chuyển trước đây.
- Camera nhìn vào điểm đánh trước ngựa sau bước làm mượt và rung. Điểm đánh khớp tâm màn hình trong lúc di chuyển, quay và chịu rung. V, zoom và chuột phải vẫn hoạt động.
- Góc nhìn thứ ba lệch vai nhẹ để đầu người cưỡi không che tâm và mục tiêu ở giữa màn hình. Khoảng cách zoom vẫn tính theo bán kính camera, không tăng do độ lệch vai.

## Phần tay bị mất

Đối chiếu ảnh mesh nguồn cho thấy nhóm nối thông có landmark 2564 chứa cả lòng bàn tay/cổ tay và đoạn cán giáo cũ phía dưới. Bộ lọc trước xóa cả nhóm, để lại các ngón tay rời; test trước chỉ kiểm tra các cụm ngón tay nên bỏ sót lòng bàn tay.

Giữ các tam giác vùng tay riêng trong nhóm chung, vẫn xóa cán giáo ngoài vùng tay và mẩu chân thừa bên trái. Khôi phục 214 đỉnh có mặt trong các tam giác lòng bàn tay/cổ tay của nhóm này. Giữ nguyên đỉnh, bone weights và các vật liệu gốc.

Khi cập nhật asset Mesh đang được Editor sử dụng, `CopySerialized` có thể để hình render dùng index buffer cũ. Bước dựng skin gọi lại `SetTriangles` trên asset đã lưu để cập nhật buffer; kiểm chứng bằng ảnh trước/sau và bản Windows.

## Kiểm thử

- Phạm vi đấu trường: 21/21 đạt (6 Editor và 15 Play Mode).
- Hai test mới kiểm tra tâm màn hình khi vừa W+D vừa rung camera, và đi lùi theo hướng 90° không đảo camera.
- Test mesh bổ sung kiểm tra bề mặt lòng bàn tay, không chỉ các ngón tay; trục roi sắt và tre vẫn đi qua nắm tay.
- Editor assembly game: 9/11 đạt; hai lỗi cũ của world/campaign được ghi trong `editor-tests.json`, giống báo cáo ngày 12/10. Không thay đổi các world scene đó.
- Bản Windows cuối kiểm chứng PASS, không có lỗi runtime. Đã lấy 13.302 mẫu trong phiên đi/quay: tâm HUD lệch 0 pixel, điểm đánh lệch tối đa 0,000205 pixel. W/S đi tổng cộng 20,71 m; zoom và chu kỳ ba góc nhìn vẫn đạt.
- Build Windows thành công, 0 lỗi; còn 524 cảnh báo shader/package đã ghi trong `build-result.json`.
- Đã xem bảng đối chiếu 12 ảnh cuối và kiểm tra tự động; không phát hiện ảnh đen, trắng hoặc shader hồng. Pixel tâm trong hai ảnh HUD là RGB (255,38,26).

Kết quả và ảnh: `Artifacts/ArenaValidation/2026-10-13/`.
Build Windows mới: `Builds/PhuDongArena/ThanhGiongArena.exe`.

Ảnh HUD có tâm đỏ tại (640,360) trên Game View 1280×720 ở cả hướng 0° và 90°. Phiên kiểm chứng đặt ba giặc đứng yên để đối chiếu ảnh; điều khiển, đổi góc nhìn, zoom và đòn đánh trong bản Windows dùng sự kiện phím/chuột Input System. Ảnh `player/` dùng render camera URP nên không chứa HUD; hai ảnh `center-reticle-*.png` được chụp Game View và có HUD.
