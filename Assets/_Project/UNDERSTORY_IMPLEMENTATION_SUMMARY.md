# TỔNG KẾT TRIỂN KHAI 3 BƯỚC NÂNG CẤP GAME THEO CẢM HỨNG "UNDERSTORY"

> **Dự án:** Game Thánh Gióng 3D (Unity 6000.6.0f1 - URP)  
> **Nguồn cảm hứng:** WebGL Game *Understory* (Phân tích mã nguồn và cơ chế nghệ thuật từ Whisperbrook Fens)  
> **Trạng thái:** HOÀN THÀNH TOÀN BỘ 3 BƯỚC (100% Sạch lỗi biên dịch — 0 Compile Errors)  

---

## 1. BƯỚC 1: CƠ CHẾ "MẮC KẸT ĐẠI ĐAO & CỬA SỔ SƠ HỞ" (VULNERABILITY WINDOW)
*Lấy cảm hứng từ chiêu thức mổ giáo cắm bùn (`mudLodge: 1.2s - 1.9s`) của trùm The Heron trong Understory.*

- **Các tệp liên quan:**
  - [`Assets/Code/Enemies/ThanhGiongEnemy.cs`](file:///e:/Game/Game_thanhgiong/Assets/Code/Enemies/ThanhGiongEnemy.cs)
  - [`Assets/Code/UI/ThanhGiongCampaignHUD.cs`](file:///e:/Game/Game_thanhgiong/Assets/Code/UI/ThanhGiongCampaignHUD.cs)
  - [`Assets/Code/Player/ThanhGiongCampaignController.cs`](file:///e:/Game/Game_thanhgiong/Assets/Code/Player/ThanhGiongCampaignController.cs)
- **Cơ chế hoạt động:**
  1. **Telegraphing Warning Ring (0.85s):** Khi Tướng Giặc Ân tiếp cận người chơi trong cự ly $4.2m$, Boss sẽ dừng lại dồn lực, giơ cao đại đao và sinh ra một vòng tròn cảnh báo nguy hiểm màu đỏ rực dưới đất (`SpawnWarningRing`).
  2. **Bổ Đao Chấn Động Đất (Ground Slam):** Tướng Giặc bổ đại đao xuống đất kèm hiệu ứng nứt đất (`SpawnGroundImpact`) và rung chấn màn hình (`IsometricCameraFollow.Shake`).
  3. **Cơ Chế Sơ Hở (Stuck in Ground - 2.0 giây):**
     - Nếu người chơi né tránh kịp (bổ trượt): Đại đao bị găm phập sâu vào lòng đất trong 2 giây.
     - Tướng Giặc bất động, gập người rung bần bật (`procedural struggle shake`) cố gắng nhổ đao lên.
     - Bật thông báo rực rỡ trên màn hình: `"★ SƠ HỞ! ĐẠI ĐAO CỦA TƯỚNG GIẶC GĂM XUỐNG ĐẤT — PHẢN CÔNG GÂY X2 SÁT THƯƠNG! ★"`.
     - Thanh máu Boss trên HUD chuyển sang viền đỏ rực và hiển thị cảnh báo sơ hở.
  4. **Hệ Số Bạo Kích x2 ($2.0\times$ Critical Multiplier):** Trong 2 giây này, mọi đòn đánh của Gióng gây gấp đôi sát thương, sinh ra chùm tia lửa vàng bạo kích (`SpawnCritSparks`) và rung nảy camera dữ dội.
  5. **Hồi Phục (Recovering):** Hết 2 giây, Tướng Giặc giật phắt đại đao lên khỏi mặt đất, tung bụi mù mịt và tiếp tục giao chiến.

---

## 2. BƯỚC 2: HỆ THỐNG DẢI LỤA ĐỎ & VỆT KIẾM BAY 3D (3D RIBBON TRAIL SYSTEM)
*Lấy cảm hứng từ chiếc khăn quàng đỏ vật lý (`RibbonMesh`) và vệt kiếm thủy mặc của Lữ Khách trong Understory.*

- **Các tệp liên quan:**
  - [`Assets/Code/Scripts/ThanhGiongRibbonCape.cs`](file:///e:/Game/Game_thanhgiong/Assets/Code/Scripts/ThanhGiongRibbonCape.cs) *(Mới)*
  - [`Assets/Code/Player/MountedHorseController.cs`](file:///e:/Game/Game_thanhgiong/Assets/Code/Player/MountedHorseController.cs)
  - [`Assets/Editor/ThanhGiongCombatEffectsInstaller.cs`](file:///e:/Game/Game_thanhgiong/Assets/Editor/ThanhGiongCombatEffectsInstaller.cs)
- **Cơ chế hoạt động:**
  1. **Khăn Quàng & Dải Lụa Đôi Thần Tướng (Verlet Node Physics Simulation):**
     - Gồm 2 dải lụa song song bay phấp phới sau 2 bờ vai của Gióng, mỗi dải gồm 20 mắt xích (nodes) vật lý.
     - Khi đứng yên: Gió nhẹ lay động dải lụa theo nhịp sóng sin tự nhiên (`windFlutter`).
     - Khi phi nước đại (`Left Shift`): Lực cản không khí (`airDrag: 3.6f`) kéo dải lụa duỗi dài và căng ngang về sau.
     - Nhấp nhô nhịp nhàng theo tần số phi của ngựa sắt (`gallopWaveForce: 2.4f`).
     - Màu sắc: Chuyển mềm mại từ viền vàng kim (`Gold trim`) sang đỏ son thắm (`Crimson red`) và đỏ nhung (`Deep wine red`).
  2. **Bụi Sao & Tàn Lửa (Star Dust Embers):** Khi phi nước đại, chóp đuôi dải lụa liên tục phát ra các chùm bụi sao vàng lấp lánh (`SpawnStarSpark`) bay là là trong gió.
  3. **Vệt Chém Thư Pháp 3D (Calligraphy Ribbon Slash):** Khi Gióng bấm `[SPACE]` vung kiếm hoặc quét tre, hệ thống sinh ra dải vệt chém 2 tầng (lõi vàng kim phát sáng + viền mực đỏ thắm) quét cong trong không gian 3D và tan dần mềm mại như nét bút lông vẽ trên tranh cuộn.

---

## 3. BƯỚC 3: SHADER & HIỆU ỨNG TAN RÃ ÁNH SÁNG (GOLD EMBERS NOISE DISSOLVE)
*Lấy cảm hứng từ shader ma thuật `uw_look` & tan rã Perlin Noise trong Understory.*

- **Các tệp liên quan:**
  - [`Assets/Shaders/ThanhGiongGoldenNoiseDissolve.shader`](file:///e:/Game/Game_thanhgiong/Assets/Shaders/ThanhGiongGoldenNoiseDissolve.shader)
  - [`Assets/Code/VFX/ThanhGiongGoldenDissolve.cs`](file:///e:/Game/Game_thanhgiong/Assets/Code/VFX/ThanhGiongGoldenDissolve.cs)
  - [`Assets/Code/Enemies/ThanhGiongRagdollPhysics.cs`](file:///e:/Game/Game_thanhgiong/Assets/Code/Enemies/ThanhGiongRagdollPhysics.cs)
  - [`Assets/Code/Scripts/ThanhGiongCampaignVFX.cs`](file:///e:/Game/Game_thanhgiong/Assets/Code/Scripts/ThanhGiongCampaignVFX.cs)
- **Cơ chế hoạt động:**
  1. **Tan Rã Quân Giặc Khi Bị Tiêu Diệt:**
     - Khi lính giặc Ân hoặc Tướng Giặc bị hạ gục: Sau cú văng vật lý ban đầu, shader `ThanhGiong/Golden Noise Dissolve` được kích hoạt.
     - Toàn bộ cơ thể bị ăn mòn dần bởi thuật toán 3D Perlin Noise, mép cắt bốc cháy rực rỡ với ánh sáng vàng hổ phách (`_EdgeColor.rgb * edge * 2.4`).
     - Hệ thống hạt `Bụi Tro Ánh Sáng Vàng` phun trào các đốm tro vàng bốc lên cao và hòa vào hư không.
     - Tướng Giặc Ân có thời gian tan rã hào hùng kéo dài $2.2$ giây với $80$ hạt bụi tro phát sáng.
  2. **Nghi Thức Hóa Thánh Về Trời (Màn 4):**
     - Tại đỉnh Núi Sóc, sau khi hoàn tất nghi thức cởi giáp sắt đặt lên đỉnh núi, Gióng cưỡi ngựa sắt bay vút lên trời xanh.
     - Khi đạt độ cao mây trời ($t \ge 0.7$), hàm `PlayAscensionDivineLight` kích hoạt chùm tia sáng mặt trời Đông Sơn 24 tia quét dài $20m$ cùng các vòng tròn hào quang thần thoại.
     - Đồng thời, `ThanhGiongGoldenDissolve` biến đổi cả tráng sĩ Gióng và ngựa sắt thành luồng ánh sáng vàng rực thần thánh hòa tan vào mây trời, để lại non sông thái bình và di sản bất tử!

---

## 4. HƯỚNG DẪN TRẢI NGHIỆM TRỰC TIẾP TRONG UNITY

1. Mở Unity Editor với scene [`Assets/Scenes/AlbionForestMap.unity`](file:///e:/Game/Game_thanhgiong/Assets/Scenes/AlbionForestMap.unity).
2. Nhấn nút **Play** trên thanh công cụ Unity.
3. Trải nghiệm:
   - **Màn 1:** Gom lương thực, quan sát dải lụa đỏ phấp phới sau lưng Gióng theo từng bước chạy.
   - **Màn 2:** Chuỗi QTE mặc giáp vàng, lên ngựa sắt.
   - **Màn 3 (Chiến trường):** 
     - Giữ `Left Shift` phi ngựa nước đại: dải lụa đỏ căng ngang ra sau và phun bụi sao vàng lấp lánh!
     - Nhấn `Space`: vung đòn chém thư pháp 2 tầng vàng - đỏ rực rỡ.
     - Đối đầu **Tướng Giặc Ân**: quan sát vòng đỏ cảnh báo dưới đất, né đòn bổ đao để đại đao cắm sâu vào đất, sau đó xông vào quất tre ngà nhận x2 sát thương chí mạng!
     - Khi tiêu diệt giặc: quan sát quân thù tan rã thành tro bụi ánh sáng vàng kim bốc lên trời.
   - **Màn 4:** Cởi giáp trên đỉnh Núi Sóc và chứng kiến nghi thức Hóa Thánh Về Trời bằng luồng ánh sáng vàng thần thánh.
