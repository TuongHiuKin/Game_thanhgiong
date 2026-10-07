# Chuyển giao thiết kế Understory → Game Thánh Gióng

## 1. Tầm nhìn & Nguyên tắc Thiết kế

Không sao chép máy móc mã nguồn hay cốt truyện của *Understory*, mà chắt lọc tinh hoa về lối chơi hành động chiến thuật, chiều sâu thẩm mỹ thị giác tranh vẽ 2.5D, hoạt họa chuyển động đa tầng và kiến trúc âm thanh thích ứng không gian. Trò chơi bảo tồn trọn vẹn bản sắc huyền thoại Thánh Gióng: tiếng rao thuở ấu thơ – vươn vai tráng sĩ – tiếp nhận gươm giáp ngựa sắt – xông pha tiền tuyến càn quét giặc Ân – gươm sắt gãy nhổ bụi tre ngà – hóa Thánh bay về trời.

---

## 2. Bốn Trụ Cột Đột Phá Đã Hiện Thực Hóa End-to-End

### I. Lối chơi & Chiến thuật (Tactical Action & Seed Combat)
- **Hệ thống Ngũ Hạt Thiêng (Seeds 1–5 & Phím Q)**:
  - Phím `1`: **Quang Căn (Lumen Seed)** – Tạo bẫy ánh sáng giữ chân (Root) quân địch trong 4 giây (boss 1 giây), không đẩy lùi hỗn loạn, đi kèm hiệu ứng lóe sáng hoàng kim `GodotCombatFeedback.PlayImpact` và âm thanh nở hoa đặc trưng.
  - Phím `2`: **Mầm Tre (Bamboo Seed)** – Trồi cọc tre đẩy lùi kẻ địch xung quanh với lực chấn động.
  - Phím `3`: **Tịnh Liên (Lotus Seed)** – Vùng đầm sen chữa lành 3 HP mỗi nhịp cho Thánh Gióng và chiến mã.
  - Phím `4`: **Hạt Lửa (Ember Seed)** – Sát thương thiêu đốt đa mục tiêu trong 4 giây.
  - Phím `5`: **Hạt Gió (Wind Seed)** – Vòng xoáy gió cuốn gây choáng và đẩy kẻ địch ra xa.
  - *Cơ chế điều tiết*: Tiêu tốn Mana (hồi 4/s), kiểm tra độ bằng phẳng của mặt đất, tầm gieo 8m và đường nhìn không bị cản trở. Tự động khóa gieo hạt trong nghi lễ mặc giáp (Chương 2) và cưỡi mây về trời (Chương 6).
- **Lướt né chiến mã (Mounted Dodge)**:
  - Phím linh hoạt: `C`, `LeftAlt`, `LeftControl`.
  - Lướt nhanh 4m trong 0.38 giây, 0.24 giây khung hình bất tử (I-frames), bụi đất vó ngựa bốc tung, rung chấn camera nhẹ và không cho phép spam chiêu liên tục.
- **Điểm dừng chân (Checkpoint Replay)**:
  - Tự động lưu trữ vị trí, lượng máu, mana, hạt giống, số lượng địch tiêu diệt và trạng thái sống/chết của địch. Khôi phục hoàn hảo và tức thì khi ngã xuống (phím `R`) hoặc từ Pause menu.

---

### II. Hình ảnh & Bố cục Tranh vẽ 2.5D (Visuals & Composition)
- **Góc nhìn 2.5D Đẳng giác điện ảnh**:
  - Camera Orthographic góc cố định (Pitch `35.264°`, Yaw `45°`), tạo chiều sâu tranh thủy mặc.
- **Bảng màu & Shader `LegendSurface`**:
  - Tông màu chủ đạo: Xanh rêu cổ kính pha xám mặt nước, điểm xuyết vàng ấm hoàng hôn và sắc đỏ hào hùng của vạt áo choàng Gióng.
  - Phối hợp Film Grain và Color Grading tạo nên chất tranh vẽ mềm mại như đang chuyển động.
- **Bố cục đa tầng & Nhận diện**:
  - *Tiền cảnh*: Tán cây đại thụ và lùm tre che nhẹ mép màn hình, dither mờ khi nhân vật đi ra sau.
  - *Mặt đất & Sông nước*: Bờ kênh không đều, cụm lau sậy đung đưa theo gió, lớp sóng nước gợn lăn tăn (`water ripples`) trên sông.
  - *Ánh sáng*: 4 luồng tia nắng vàng (`sunshafts`) chiếu xiên; đĩa hào quang nhận diện (`recognition disc`) dưới chân chiến mã giúp định vị Gióng giữa hỗn chiến.
  - *Cụm cảnh quan*: Cây cối và đá tảng được xếp thành cụm (clusters) dẫn hướng di chuyển, 0 vi phạm lối đi (`RouteViolationCount = 0`).

---

### III. Hoạt họa Chuyển động Đa tầng (Animation Polish & Secondary Motion)
- **Rig 2 lớp 38 xương (Procedural & Blended)**:
  - 17 xương ngựa sắt + 21 xương tráng sĩ Gióng được tính toán chuyển động hoàn toàn bằng mã code.
  - Chuyển động phụ (Secondary motion): Vạt áo choàng (`cape`) và dải buộc yên phấp phới theo quán tính và gió; Gióng nghiêng người khi ôm cua và cúi rạp người (`lean & tuck`) khi lướt né.
- **Phản hồi hành động (Game-Feel Juice)**:
  - Vệt kiếm sáng (slash trails), tia lửa va chạm, bụi đất dưới từng bước vó ngựa (phân biệt đất khô và bùn ướt).
  - Chuỗi combo 3 đòn cận chiến: Chuẩn bị 0.12s, đòn thứ ba xoay vòng vung gậy tre mạnh hơn 15% kèm bước tiến nhẹ.
  - Đóng băng tuyệt đối toàn bộ animation khi mở Pause menu.

---

### IV. Thiết kế Âm thanh Đa lớp (Spatial Audio & Adaptive Mix)
- **Âm thanh môi trường đa vùng**:
  - Tích hợp tiếng ếch đầm lầy `ambient_frogs`, tiếng sóng vỗ bờ lau sậy `ambient_water_lap`, tiếng rèn sắt búa đập.
  - Tự động suy giảm theo khoảng cách người chơi, tuân thủ ngân sách tối đa 5 nguồn phát không gian (`SpatialEmitterCount <= 5`).
- **Tiếng bước chân & Tương tác**:
  - Tiếng vó ngựa phân biệt bề mặt (đất, đá, gỗ, ướt).
  - Tiếng vút gió lướt né (`dodge whoosh`), tiếng nảy mầm và tiếng hoa nở thứ cấp sau 0.45s.
  - Tự động hạ nhạc nền (Ducking) khi có sự kiện tấn công hoặc chiêu thức bùng nổ; thanh âm lượng Master điều chỉnh mượt mà mọi nguồn âm.

---

## 3. Bản đồ 6 Màn Chiến dịch

| Màn | Tên Scene | Nội dung | Thẩm mỹ Cảnh quan |
|---|---|---|---|
| **1** | `LangGiongTienTuyen` | Tiếng rao, thu gom cơm cà, lớn nhanh | Sương ấm, mái tranh, đường đất làng quê |
| **2** | `KinhThanhRenThep` | Lò rèn triều đình, nhận giáp/gươm/ngựa | Than hồng, khói lửa, tia lửa rèn đập |
| **3** | `PhaoDaiNgamQuanAn` | Phá căn cứ giặc Ân, thử nghiệm bẫy hạt | Rêu xanh, hang ngầm, ánh sáng tương phản |
| **4** | `ThungLungVuotSong` | Vượt sông hiểm trở, tiến công | Sông xanh rêu, bờ lau sậy, sóng gợn lăn tăn |
| **5** | `TranTuyenNuiSoc` | Đại chiến tiền tuyến, gãy gươm, nhổ tre | Khói lửa chiến trường, đất đỏ bốc cháy |
| **6** | `DinhSocHoaThanh` | Cởi giáp, tháo nón, cưỡi ngựa bay về trời | Mây ngàn, bậc đá cổ, ánh hào quang bất tử |

---

## 4. Công cụ & Bộ Skills Đã Tích Hợp

1. **`awesome-gamedev-agent-skills`** (`.agents/skills/`):
   - 74 kỹ năng chuyên sâu gamedev: `game-feel`, `audio-design`, `camera-systems`, `unity-animation`, `level-design`, `physics-tuning`, v.v.
2. **`Unity-Skills`** (`.agents/skills/unity-skills/` & `Packages/com.besty.unity-skills`):
   - REST server trên cổng 8091 với 805 kỹ năng Unity Editor; kết nối cùng `unityMCP` trên cổng 6401.
3. **Bộ kiểm tra tự động**:
   - `Tools > Thanh Giong > Isometric > Verify All`: Chạy toàn bộ 169+ kiểm thử tự động, 100% PASS, 0 lỗi runtime.
