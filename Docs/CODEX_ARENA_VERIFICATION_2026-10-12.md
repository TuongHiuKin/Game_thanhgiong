# Kết quả tiếp quản Phù Đổng Arena — 12/10/2026

Project: `D:\School\PRU\Game_thanhgiong` · Unity `6000.6.0f1` · URP · Windows x64.
Branch: `feature/mounted-horse-motion`.
Đầu vào: `Docs/CODEX_HANDOFF_ThanhGiong_Arena.md`.

## Thực hiện

- Chạy `ThanhGiongArenaBuilder.Build()` qua Unity CLI trên Editor đang mở; lưu lại `PhuDongArena.unity` và sinh lại `ThanhGiong_Mounted_ArenaSkin.asset`.
- Sửa dấu ngoặc dư trong HUD gây lỗi CS1022.
- Cho input giữ sự kiện đến khi các bộ xử lý có đủ một frame để đọc. Phím V được camera đọc trước khi input hết hạn; sự kiện CLI đến muộn trong frame cũng không bị mất.
- Camera góc nhìn thứ nhất dùng vị trí mắt `(0, 3.25, 0.65)` tính từ gốc nhân vật. Vị trí cũ ở độ cao 1.95 m nằm trong thân/giáp, được xác nhận bằng ảnh `first-person-before.png`.
- Chuẩn hóa cuộn chuột Windows 120 đơn vị/nấc và zoom 2 m/nấc, giới hạn 4.5–28 m. Cuộn chuột không thay đổi khoảng cách trong góc nhìn thứ nhất.
- Tâm ngắm dùng cùng truy vấn collider, bán kính tăng trưởng, biến thể tre và góc đánh với đòn melee. Bỏ các giá trị HUD tự đặt 4.2/5.8 m không khớp với combat.
- Bỏ lực đẩy lên ở cả đòn thường và đòn kết liễu; giữ nhân vật giặc thẳng đứng và đẩy theo mặt đất. Giữ hiệu ứng tan biến màu vàng.
- Giữ tọa độ vũ khí và bộ lọc tam giác chân thừa theo bàn giao; không thêm chân giả. Sửa bộ lọc giáo cũ: nắm tay thật nằm ở Y=1.13–1.235, gồm các cụm 1284, 1341, 26819, 26837. Các cụm cao hơn ở Y≈1.46 là trang trí của giáo, được phân biệt bằng ảnh mesh nguồn `source-hand-comparison.png`; chúng vẫn bị loại. Giữ nắm tay, loại thân/mũi/trang trí giáo và fragment chân 6850.
- Đổi góc roi sắt và tre từ `(15,5,-12)` sang `(8,5,12)` để cán đi đúng qua fist sau khi tay được khôi phục. Test mesh kiểm tra cả phần tay còn đủ tam giác lẫn trục của hai vũ khí thực sự đi qua bounds của nắm tay trong scene đã lưu.

## Kiểm thử

| Nhóm | Kết quả |
|---|---:|
| Rig bốn chân gốc | 3/3 đạt |
| Scene đấu trường đã lưu | 2/2 đạt |
| Flow đấu trường | 8/8 đạt |
| Test mới: V, zoom/mắt, boundary, knockback, reticle | 5/5 đạt |
| Test mới: giữ nắm tay, bỏ giáo/chân thừa | 1/1 đạt |
| Tổng phạm vi đấu trường | **19/19 đạt** |

Test cinematic cũ chờ cố định 0.6 giây đôi khi kiểm tra trước coroutine kết thúc trong Editor bận. Đổi sang chờ trạng thái Victory với giới hạn 2 giây thực; không bỏ assertion.

Đã chạy thêm toàn bộ hai assembly test của game: **22/25 đạt**. Ba lỗi còn lại thuộc campaign/world cũ, ngoài scene đấu trường:

1. `ThanhGiongProjectTests.BattleMaps_HaveInvadersAndRequiredBosses`: Pháo Đài Ngầm có 0 component giặc thay vì ít nhất 20.
2. `ThanhGiongProjectTests.WorldMaps_HavePlayerCampaignCameraAndFourLegRig`: Làng Gióng Tiền Tuyến thiếu campaign.
3. `ThanhGiongPlayModeTests.UndergroundFortress_StartsAsPlayableBattle`: Pháo Đài Ngầm không khởi tạo campaign.

Các lỗi tương ứng đã có trong kết quả trước bàn giao (`Artifacts/ArenaValidation/editor-tests-final.xml` và báo cáo campaign trước đó). Không tái dựng sáu world scene để che các lỗi này. Không tuyên bố toàn dự án đã xanh.

Kết quả chi tiết: `Artifacts/ArenaValidation/2026-10-12/editor-tests.json`, `play-tests.json`, `arena-play-tests.json`, `test-summary.json`. Lọc theo assembly `ThanhGiong` khi chạy suite game; manifest cũng bật hơn một nghìn test của plugin Unity Skills.

## Kiểm chứng bản Windows

Bộ kiểm chứng development tùy chọn: `ThanhGiongArenaPresentationVerifier`, chỉ chạy khi có cờ `--verify-arena-presentation`. Dùng phím/chuột Input System thật cho Enter, V, zoom, WASD và đòn đánh. Tắt AI và cấu hình ba giặc 500 HP trong phiên này để ảnh và số đo ổn định; đây là phiên kiểm chứng tính năng, không phải chứng minh thắng một trận với độ khó mặc định.

Ảnh và số đo bản Windows được lưu vào `Artifacts/ArenaValidation/2026-10-12/player/` sau khi chạy. Bản chạy thông thường không kích hoạt bộ kiểm chứng này.

Vì Windows chạy cửa sổ ẩn có thể trả ảnh ScreenCapture đen/null, các ảnh player dùng render request URP và không chứa HUD. HUD được chụp từ Game View 1280×720: `editor-hud.png`. Tâm ngắm đỏ ở (640,283) có pixel RGB (255,38,26); bốn vạch xung quanh cũng được kiểm tra, cùng thanh máu nổi trên giặc.

Kết quả player cuối: vòng V Far → Close → First → Far, zoom 15.5 → 13.5 m, khoảng cách camera gần boundary 15.505 m, WASD đi 37.765 m, mắt cao 3.250 m. Knockback đi ngang 0.132 m; tăng độ cao đòn thường/kết liễu dưới 0.000001 m. Không có lỗi runtime. Số đo cụ thể nằm trong `player/result.json`.

Build Windows development thành công, 0 lỗi và 497 cảnh báo shader/package; báo cáo cuối ở `build-result.json`. File chạy: `Builds/PhuDongArena/ThanhGiongArena.exe` (build output bị Git ignore). Đã xem bảng đối chiếu `contact-sheet.png`; kiểm tra tự động 12 ảnh cuối không phát hiện ảnh đen, trắng hoặc shader hồng.

## Tái hiện bằng Unity CLI

Trong PowerShell, tại thư mục project:

```powershell
$env:UNITY_NO_UPDATE_CHECK = '1'
$unityCli = 'C:\Users\ANKIN\AppData\Local\Unity\bin\unity.exe'
& $unityCli command recompile --json
& $unityCli command recompile_status --json
& $unityCli command menu --path 'Tools/Thanh Giong/Build Phu Dong Arena' --json
& $unityCli command run_tests --mode editor --filter ThanhGiong --filter_type assembly --json
& $unityCli command run_tests --mode playmode --filter ThanhGiong --filter_type assembly --async_tests true --json
& $unityCli command test_status --json
```

Chờ `test_status` completed và Editor thoát Play Mode trước lượt tiếp theo. Không chạy thêm một Editor batch mode trên cùng project khi Editor hiện tại còn mở.
