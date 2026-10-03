# THIẾT KẾ & CẤU TRÚC KỸ THUẬT NHÂN VẬT & QUÁI VẬT TRONG "UNDERSTORY"
> **Nguồn trích xuất:** Dynamic JS Chunks: `index-CpAHig4n.js` (Player), `index-IpFcVWTA.js` (Combat/Enemies), `limbs-BUiAq27t.js`, `ribbon-DWXHTtqt.js`, `fox-views-F1Fd5oAh.js`  
> **Người thực hiện:** Antigravity AI — Dự Án Game Thánh Gióng 3D  

---

## 1. TỔNG QUAN KIẾN TRÚC MÔ HÌNH HÓA (2.5D PROCEDURAL RIG & RIBBON)

Trò chơi **Understory** hoàn toàn **không sử dụng model 3D có khung xương FBX/GLTF truyền thống**. Thay vào đó, toàn bộ nhân vật và quái vật được xây dựng bằng sự kết hợp đột phá giữa:
1. **Thẻ Thể Động 2.5D (Dynamic Animated Canvas Cards):** Cơ thể, đầu, chân tay, áo choàng được vẽ bằng thuật toán Canvas 2D thời gian thực, phủ lên một mặt phẳng 3D (Billboard Card).
2. **Khung Xương Mềm Bằng Lát Cắt Toán Học (Parametric Profile & Spine):** Xương sống có thể uốn lượn, gù lưng, nghiêng mình theo hàm số học (IK mềm).
3. **Mô Phỏng Vải & Dải Lụa 3D Thực Thụ (3D Ribbon Mesh):** Khăn quàng cổ, đuôi cáo lửa, vệt chém kiếm là các dải lưới 3D tương tác vật lý với gió và trọng lực.
4. **Shader Hội Họa Sống & Tan Rã Ánh Sáng (Blight & Noise Dissolve):** Khi trúng đòn hoặc bị tiêu diệt, sinh vật không ngã gục mà tan biến thành tro bụi ánh sáng vàng kim rực rỡ.

---

## 2. CẤU TRÚC NHÂN VẬT CHÍNH (LỮ KHÁCH — WANDERER)

### 2.1. Cấu Tạo Hình Thể
- **Thân và Áo Choàng (`drawCloak`):** 
  - Thân trên được vẽ hình giọt nước hoặc nón chuông mềm mại bằng màu áo choàng tối (`R.cloak: #2c2a33`).
  - Viền áo choàng được viền bằng màu mực vẽ tay (`R.ink: #15201c`), có lớp bóng mờ sáng phía trên (`R.cloakLight`) tạo độ khối như nét cọ sơn dầu.
- **Bàn Tay & Chân (`drawLeg`):**
  - Chân là các nét cọ tròn đầu (`lineCap = "round"`, `lineJoin = "round"`), vung nhịp nhàng theo chu kỳ bước đi sin/cos (`stride`).
- **Chiếc Khăn Quàng Đỏ Huyền Thoại (`scarf` & `RibbonMesh`):**
  - Khăn quàng không phải ảnh 2D mà là một **dải lụa 3D thực thụ** gồm các điểm mắt xích (nodes) liên kết với nhau.
  - Điểm neo (`anchorPoint(0.9)`): Khăn được gắn chặt vào sau gáy lữ khách ở độ cao `y = 0.9m`.
  - Khi nhân vật chạy hoặc lướt né (`dodge`), các mắt xích của khăn quàng bị kéo giật về sau theo quán tính vận tốc và lực cản không khí (`drag: 3`).
  - Khi lướt né, khăn quàng phát ra các hạt lấp lánh hình ngôi sao màu đỏ thắm (`shape: "star"`, `color: et.scarf`, `colorEnd: et.scarfDark`).

### 2.2. Vòng Lặp Trạng Thái & Thuộc Tính Của Lữ Khách
```javascript
const player = {
  hp: 5,               // Máu khởi điểm: 5 vạch tim
  radius: 0.45,        // Bán kính va chạm vật lý
  runSpeed: 7.2,       // Tốc độ chạy tối đa
  accel: 18,           // Gia tốc tăng tốc mượt mà
  decel: 24,           // Độ ma sát hãm phanh
  state: "idle",       // Các trạng thái: idle, walk, run, dodge, attack, kneel, sleep, dead
  invulnerable: false, // Khung thời gian bất tử (I-Frames) khi lướt né
  dodgeCd: 0.35,       // Thời gian hồi lướt né
  comboGrace: 0.45     // Cửa sổ thời gian duy trì đòn chém liên hoàn
};
```

---

## 3. THIẾT KẾ & PHÂN LOẠI QUÁI VẬT (MONSTERS & BEASTS)

Mã nguồn chiến đấu (`index-IpFcVWTA.js`) chia hệ thống quái vật thành 2 cấp độ: **Quái Thường (Minions & Stalkers)** và **Trùm Khổng Lồ Đa Khớp (Multi-Segment Bosses)**.

### 3.1. Nhóm Quái Thường (Minions)

#### A. Quái Bùn Đầm Lầy — Lurker (Đặc Trưng Chương Whisperbrook Fens)
- **Cơ Chế Sinh Tồn:** Lặn ngụp hoàn toàn dưới lớp nước và bùn lầy sâu, không thể bị chém khi đang lặn.
- **Hành Vi AI:**
  1. *Thức giấc:* Khi người chơi tiến vào bán kính 17m (`wakeRange: 17`).
  2. *Trồi lên:* Nhô phần đầu và lưng lên khỏi mặt nước (`surface: 0.9s`).
  3. *Phun bọc bùn độc:* Bắn từ 2 đến 3 cầu bùn hắc ám (`lobs: [2, 3]`) về phía người chơi.
  4. *Tạo vũng lầy làm chậm:* Điểm rơi của cầu bùn biến thành bãi sình lầy bán kính 3.2m (`puddle: 3.2`), làm giảm 45% tốc độ chạy của lữ khách (`slow: 0.45`).
  5. *Lặn trốn:* Ngay sau khi bắn, Lurker lặn xuống bùn từ 1.6 - 2.6 giây rồi mới trồi lên ở vị trí khác.

#### B. Bóng Ma Sương Mù — Fogling
- **Cơ Chế Ẩn Nấp:** Cơ thể hoàn toàn tàng hình trong màn sương mù dày đặc của đầm lầy.
- **Điểm Yếu:** Chỉ lộ nguyên hình khi bước vào vầng sáng đèn lồng Lumen của nhân vật (`heroLight: 2.6m`, `revealAt: 0.28`).
- **Hành Vi Tấn Công:** Khi bị lộ diện hoặc người chơi quay lưng, Fogling sẽ tụ lực trong 0.45 giây rồi phóng vụt tới cực nhanh (`dash: 11m/s`) để ám sát.

#### C. Linh Hồn Ánh Ma — Wisp
- Quái vật bay lơ lửng, di chuyển theo quỹ đạo hình elip quay quanh người chơi ở cự ly an toàn 3.8m - 5.2m (`orbitRadius: [3.8, 5.2]`).
- Bắn các tia đạn ma trĩu nặng từ khoảng cách 7m (`attackRange: 7`).

#### D. Lực Sĩ Hắc Ám — Brute
- Thân hình đồ sộ, máu dày (`hp: 7`, gấp đôi quái thường).
- Không né tránh mà lù lù tiến thẳng tới, vung nắm đấm đất chấn động càn quét diện rộng.

---

### 3.2. Nhóm Trùm Khổng Lồ Đa Khớp (Colossal Bosses)

#### A. Trùm Diệc Khổng Lồ (The Heron — Boss Vùng Đầm Lầy Fens)
Đây là con trùm biểu tượng của chương *Whisperbrook Fens* mà bạn đã trải nghiệm qua liên kết.
- **Thanh Máu Đa Khớp (Multi-Segment HP):** 
  - Cơ thể chia thành **50 phân đoạn máu** (`segment: 50`), có các **"Nút thắt sinh mệnh" (Knots)**: khi đánh trúng nút thắt, trùm nhận sát thương gấp đôi (`knotHeavy: 6`).
- **3 Giai Đoạn Chiến Đấu (Phases):**
  1. **Phase 1: The Hunt (Đi Săn):** Diệc sải bước dài qua các bãi bồi, dùng mỏ nhọn lùng sục người chơi.
  2. **Phase 2: Wary (Rình Rập & Đề Phòng):** Lùi ra vùng nước sâu (`wadeSpeed: 3.0m/s`), bay lướt tạo khoảng cách và thăm dò.
  3. **Phase 3: The Storm (Cuồng Phong Bão Tố):** Triệu hồi mưa bão sấm sét, vỗ cánh tạo gió giật liên hoàn.
- **Đòn Đánh & Cơ Chế Sơ Hở (Vulnerability Window):**
  - **Mổ Giáo Cắm Bùn (`spear`):** Diệc vươn cổ bổ mỏ nhọn như ngọn giáo cắm phập xuống đất (`damage: 1, knock: 11`).
  - **Cơ Chế Mắc Kẹt (`mudLodge`):** Khi người chơi lướt né (`dodge`) thành công cú mổ, mỏ của Diệc sẽ **bị cắm sâu vào lớp bùn lầy trong 1.2 đến 1.9 giây (`lodgeIn: 1.9s`)**. Đây là thời khắc vàng để người chơi lao vào chém xối xả vào phần đầu và cổ!
  - **Cuồng Phong Quét Sạch (`gust`):** Diệc đập cánh tạo luồng gió bão bán kính 12.5m (`radius: 12.5`), thổi bay lữ khách và dập tắt các mầm ánh sáng.

#### B. Trùm Hươu Thần Bị Ô Nhiễm (The Corrupted Stag — Boss Rừng Cổ Thụ)
- **Đòn Dẫm Chấn Động (`stomp`):** Nện móng guốc xuống mặt đất, tạo ra 3 vòng sóng xung kích lan tỏa với tốc độ 10.5m/s ra bán kính tối đa 18m (`ringMax: 18`). Người chơi phải canh nhịp nhảy hoặc lướt né xuyên qua vòng sóng.
- **Đòn Húc Càn Rừng Già (`charge`):** Lao thẳng một mạch 44m với vận tốc khủng khiếp 25m/s (`speed: 25`), ủi bay cây cối và gây choáng cực nặng (`knock: 16`).
- **Rễ Ma Trồi Đất (`roots`):** Triệu hồi 3 hàng rễ cây quỷ dị từ dưới lòng đất đâm thẳng lên theo hướng người chơi đang đứng.

---

## 4. SHADER NGHỆ THUẬT: HIỆU ỨNG TÀ KHÍ (BLIGHT) & TAN RÃ (DISSOLVE)

Mọi sinh vật trong thế giới này đều chịu sự chi phối của Shader ma thuật `uw_look`:

```glsl
vec3 uw_look(vec3 col, vec3 wp, vec2 uv) {
  vec4 m = uw_map(wp.xz);
  float healed = uw_healed(wp.xz);

  // 1. TÀ KHÍ BLIGHT: Hút cạn màu sắc của quái vật thành màu xám tro lạnh
  #ifndef UW_NO_BLIGHT
    float b = m.b * uwBlight * (1.0 - healed);
    float l = dot(col, vec3(0.3, 0.59, 0.11));
    vec3 grey = vec3(l) * vec3(0.86, 0.92, 1.02) * 0.82;
    col = mix(col, grey, b * 0.88);
  #endif

  // 2. KHI CHẾT / THANH LỌC: Tan rã bằng Perlin Noise với viền vàng rực phát sáng
  #ifdef UW_DISSOLVE
    float nz = uw_noise(uv * 34.0) * 0.6 + uw_noise(uv * 9.0) * 0.4;
    float cut = healed * 1.15 - 0.08;
    if (nz < cut) discard; // Cắt bỏ pixel tan rã
    col = mix(col, vec3(1.0, 0.82, 0.45) * 2.4, smoothstep(0.1, 0.0, nz - cut) * step(0.001, healed));
  #endif

  // 3. HÀO QUANG THẦN THOẠI (Rim Light)
  #ifndef UW_NO_RIM
    col += vec3(1.0, 0.78, 0.42) * uw_rim(wp.xz) * (0.1 + 0.45 * m.b);
  #endif

  // 4. BÓNG MÂY TRÔI THỜI GIAN THỰC
  #ifndef UW_NO_CLOUD
    col *= 1.0 - uwCloud * 0.42 * uw_cloud(wp.xz, uTime);
  #endif
}
```

---

## 5. BÀI HỌC ÁP DỤNG TRỰC TIẾP CHO GAME THÁNH GIÓNG

| Cơ Chế Trong "Understory" | Ứng Dụng Nâng Tầm Cho Game Thánh Gióng 3D |
| :--- | :--- |
| **Chiếc Khăn Quàng Đỏ Vật Lý (3D Ribbon)** | **Dải Lụa Đỏ & Áo Giáp Vàng Của Gióng:** Ứng dụng dải Ribbon bay phấp phới sau lưng khi phi ngựa, tạo vệt ánh sáng rực rỡ khi Gióng tăng tốc. |
| **Cơ Chế "Mắc Kẹt Mỏ Bùn" Của Trùm Heron** | **Tướng Giặc Ân Chém Hụt Găm Đao Xuống Đất:** Khi Tướng Giặc bổ đại đao trượt, đao găm sâu vào đất/đá trong 1.5 giây, tạo cơ hội cho Gióng nhổ tre ngà quất tới tấp! |
| **Quái Ẩn Bùn Đầm Lầy (Lurker)** | **Giặc Ân Nấp Lùm & Đầm Sình Lầy:** Lính giặc phục kích từ các ao bèo, đầm lau sậy quanh Hồ Nguyệt Đãng, bắn tên độc làm chậm ngựa sắt. |
| **Shader Tan Rã Ánh Sáng (Noise Dissolve)** | **Quân Giặc Tan Thành Tro Bụi & Hóa Thánh Về Trời:** Khi hạ gục giặc Ân hoặc khi Gióng cởi giáp bay về trời ở Màn 4, sử dụng dissolve shader cháy sáng vàng kim thay cho ragdoll vật lý thông thường. |
