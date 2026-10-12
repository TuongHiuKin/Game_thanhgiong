# Handoff Document & Prompt cho OpenAI Codex / Coding Agent

> **Đã tiếp quản và kiểm chứng ngày 12/10/2026:** xem [báo cáo hoàn tất](CODEX_ARENA_VERIFICATION_2026-10-12.md). Scene/skin đã dựng lại, đấu trường đạt 19/19 test và bản Windows đạt kiểm chứng runtime. Các thông số góc vũ khí và camera bên dưới là trạng thái bàn giao; báo cáo mới ghi các hiệu chỉnh cuối, gồm khôi phục nắm tay bị bộ lọc giáo xóa nhầm. Toàn suite game còn 3 lỗi campaign cũ được liệt kê riêng trong báo cáo.

**Dự án:** Thánh Gióng - Phù Đổng Arena (`Game_thanhgiong`)  
**Nhánh Git:** `feature/mounted-horse-motion`  
**Môi trường:** Unity 6 (`6000.6.0f1`), Universal Render Pipeline (URP), Windows Standalone x64  
**Scene chính:** `Assets/Scenes/ThanhGiongArena/PhuDongArena.unity`  
**Ngày tạo:** 12/10/2026  

---

## 1. Tóm tắt bối cảnh & Trạng thái hiện tại (Status Overview)
Người dùng yêu cầu giải quyết 7 vấn đề về đồ họa, góc quay và cơ chế chiến đấu dựa trên ảnh chụp thực tế và trải nghiệm chơi:
1. **Sửa cảnh nền phía sau bị ẩn/hiện giật cục khi di chuyển** (Background scenery popping / occlusion / fog clip).
2. **Góc nhìn xa hơn để quan sát toàn trận đấu + chuyển đổi giữa góc nhìn thứ nhất (1st-person) và thứ 3 (3rd-person) + cuộn chuột thu phóng**.
3. **Thanh máu nổi trên đầu quân giặc** (Floating enemy health bars) để dễ theo dõi tiến độ tiêu diệt.
4. **Dấu chấm tâm ngắm & góc đánh** (Aim reticle / crosshair) định hướng đòn đánh của Thánh Gióng.
5. **Giặc bị đánh thì thụt lùi (knockback lùi mặt đất) chứ không bay lên trời**.
6. **Hình 1: Cây giáo / roi sắt bị lệch ra sau cánh tay, cần đặt chính xác vào trong lòng bàn tay cầm**.
7. **Hình 2: Chân trái con ngựa bị lồi ra một mẩu chân/móng lơ lửng giữa không trung do lỗi mesh 3D gốc**.

---

## 2. Chi tiết các file đã sửa đổi (Changes Applied)

### A. [`Assets/Code/Enemies/ThanhGiongRagdollPhysics.cs`](file:///D:/School/PRU/Game_thanhgiong/Assets/Code/Enemies/ThanhGiongRagdollPhysics.cs) & [`Assets/Code/Enemies/ThanhGiongEnemy.cs`](file:///D:/School/PRU/Game_thanhgiong/Assets/Code/Enemies/ThanhGiongEnemy.cs) *(Vấn đề 5 - Knockback thụt lùi)*
- Đã bỏ hoàn toàn lực nâng thẳng đứng (`upwardLift = 0f`, `lift = 0f`).
- Khóa góc quay (`rb.constraints = FreezeRotationX | FreezeRotationZ`) trong suốt thời gian trúng đòn để lính giữ tư thế đứng và thụt lùi phẳng dọc mặt đất theo hướng `flatAway.normalized`.
- Giảm vận tốc nảy khi chết xuống $\le 0.4\text{m}$.

### B. [`Assets/Code/Player/ThanhGiongArenaInput.cs`](file:///D:/School/PRU/Game_thanhgiong/Assets/Code/Player/ThanhGiongArenaInput.cs) *(Vấn đề 2 - Input đổi góc nhìn & Zoom)*
- Đã thêm binding phím `KeyCode.V` (`"v"`) trong cả Input System mới và legacy input: `Bind(KeyCode.V, "v")`.
- Đã thêm property `ScrollDelta` đọc giá trị cuộn chuột (`Mouse.current.scroll` hoặc `Input.mouseScrollDelta.y`).

### C. [`Assets/Code/Scripts/IsometricCameraFollow.cs`](file:///D:/School/PRU/Game_thanhgiong/Assets/Code/Scripts/IsometricCameraFollow.cs) *(Vấn đề 1 & 2 - Camera modes & Scenery pop-in fix)*
- **Enum `ArenaCameraMode`**:
  - `ThirdPersonFar` (Mặc định): `arenaDistance = 15.5f`, `lookHeight = 1.4f`, `arenaPitch = 28f`, `fieldOfView = 62f` (Góc nhìn rộng, bao quát toàn chiến trường).
  - `ThirdPersonClose`: `arenaDistance = 9.0f`, `lookHeight = 1.2f`, `arenaPitch = 24f`, `fieldOfView = 58f`.
  - `FirstPerson`: `arenaDistance = 0f`, `lookHeight = 1.95f`, `arenaPitch = 12f`, `fieldOfView = 75f` (Đặt camera ngay tầm mắt Thánh Gióng nhìn thẳng về phía trước, theo hướng ngựa xoay `target.eulerAngles.y + arenaYaw`).
- **Chuyển đổi góc nhìn & Zoom**:
  - Nhấn `V` gọi `CycleArenaCameraMode()`.
  - Cuộn chuột (`ScrollDelta`) điều chỉnh khoảng cách `arenaDistance` từ $4.5\text{m}$ đến $28\text{m}$.
  - Giữ chuột phải (`OrbitHeld`) xoay góc nhìn cả ở góc thứ 1 và thứ 3.
- **Sửa lỗi cảnh bị ẩn/hiện giật cục**:
  - Bộ lọc `SphereCastAll` phát hiện vật cản trước đây vô tình va chạm vào `Arena boundary` (4 bức tường tàng hình bao quanh đấu trường) và mặt phẳng `Walkable ground`/`Grass plain`, giật khoảng cách camera về `1.2m`!
  - Đã thêm bộ lọc bỏ qua các collider chứa tên `boundary`, `ground`, `plain`, `grass`, `battlefield` và `isTrigger`.
  - Tăng `farClipPlane = 1000f`.

### D. [`Assets/Code/UI/ThanhGiongArenaHUD.cs`](file:///D:/School/PRU/Game_thanhgiong/Assets/Code/UI/ThanhGiongArenaHUD.cs) *(Vấn đề 3 & 4 - Thanh máu giặc & Tâm ngắm)*
- **`DrawEnemyHealthBars()`**: Duyệt qua danh sách `arena.waveManager.LivingEnemies`. Với mỗi lính trong phạm vi 65m, vẽ thanh máu nổi theo tọa độ màn hình với màu gradient (xanh -> vàng -> đỏ) theo tỉ lệ máu `HealthRatio`, viền màu vàng cho Boss.
- **`DrawAimReticle()`**:
  - Góc nhìn thứ 1: Hiển thị ngay chính giữa màn hình.
  - Góc nhìn thứ 3: Chiếu điểm ngắm đòn đánh phía trước nhân vật (`transform.position + Vector3.up * 1.25f + transform.forward * 3.4f`) lên màn hình.
  - Kiểm tra xem có giặc nằm trong tầm với và góc quét không; nếu có mục tiêu, tâm ngắm chuyển sang màu đỏ rực để báo hiệu khóa mục tiêu (target lock).
- **Instruction Bar**: Đã cập nhật dòng hướng dẫn bên dưới bổ sung: `V: đổi góc nhìn (Thứ 1/Thứ 3)   Cuộn chuột: zoom`.

### E. [`Assets/Editor/ThanhGiongArenaBuilder.cs`](file:///D:/School/PRU/Game_thanhgiong/Assets/Editor/ThanhGiongArenaBuilder.cs) *(Vấn đề 1, 6, 7 - Cảnh quan, Vị trí tay cầm & Lọc mesh)*
- **Loại bỏ mẩu chân trái lơ lửng (Hình 2)**:
  - Cập nhật hàm `CreateArenaSkin()`: Bộ lọc `isCullGroup` đã thêm điều kiện loại bỏ cụm đỉnh chân thừa từ thế ngựa chồm gốc (`maxX < -0.15f && minX > -0.40f && minY >= 0.25f && maxY <= 0.50f && minZ >= -0.30f && maxZ <= 0.05f`, bao gồm Root 6850 - 264 đỉnh). Đã kiểm chứng qua ảnh render `Artifacts/ArenaValidation/left_flank_verification.png`.
- **Căn chỉnh vũ khí vào tay cầm (Hình 1)**:
  - Tọa độ roi sắt: `ironWeapon.transform.localPosition = new Vector3(0.235f, 1.55f, 0.025f); ironWeapon.transform.localRotation = Quaternion.Euler(15f, 5f, -12f);`
  - Tọa độ tre ngà: `bambooWeapon.transform.localPosition = new Vector3(0.235f, 1.62f, 0.035f); bambooWeapon.transform.localRotation = Quaternion.Euler(15f, 5f, -12f);`
  - Đã kiểm chứng qua `Artifacts/ArenaValidation/test_iron_in_hand.png`: thân vũ khí xỏ thẳng qua các ngón tay nắm của găng tay giáp.
- **Tầm nhìn sương mù & Camera (Hình nền)**:
  - `farClipPlane = 1000f`.
  - `RenderSettings.fogStartDistance = 180f`, `fogEndDistance = 600f`.

---

## 3. Các công việc còn lại cần Codex thực hiện (Remaining Tasks for Codex)
1. **Re-bake và cập nhật Scene trong Unity Editor**:
   - Chạy hàm tác vụ `ThanhGiongArenaBuilder.Build()` (hoặc menu `Tools > Thanh Giong > Build Phu Dong Arena`) để nạp các script đã recompile, tạo mới asset `ThanhGiong_Mounted_ArenaSkin.asset` và lưu lại scene `Assets/Scenes/ThanhGiongArena/PhuDongArena.unity`.
2. **Kiểm tra trực quan (Verification & Playtest)**:
   - Chạy test kiểm tra hoặc render snapshot để xác nhận:
     - Nhấn phím `V` chuyển đổi mượt mà giữa 3 chế độ camera (Thứ 3 xa, Thứ 3 gần, Thứ 1).
     - Cuộn chuột phóng to/thu nhỏ mượt mà.
     - Thanh máu giặc và tâm ngắm hiển thị chuẩn xác khi vào trận.
     - Đòn đánh đẩy lùi giặc theo mặt phẳng ngang.
     - Vị trí vũ khí nằm chuẩn trong lòng bàn tay phải.
     - Mạn sườn trái ngựa không còn mẩu móng lơ lửng.
3. **Commit & Push Git**:
   - Thêm các file đã chỉnh sửa vào git (`git add -A`).
   - Tạo commit với thông điệp rõ ràng theo chuẩn Conventional Commits.
   - Push lên nhánh `feature/mounted-horse-motion` trên remote fork.

---

## 4. Prompt mẫu để dán trực tiếp cho Codex (Copy-Paste Prompt for Codex)
*(Xem nội dung trong khung code bên dưới)*
