using UnityEngine;

public class ThanhGiongCampaignHUD : MonoBehaviour
{
    public ThanhGiongCampaignController campaign;
    [Header("Kenney Fantasy UI")]
    public Texture2D panelFrame;
    public Texture2D bannerFrame;
    public Texture2D dividerTexture;
    public Texture2D combatCursor;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle objectiveStyle;
    private GUIStyle controlKeyStyle;
    private GUIStyle controlDescStyle;
    private GUIStyle statStyle;
    private GUIStyle barTextStyle;
    private GUIStyle bannerStyle;
    private GUIStyle promptStyle;
    private GUIStyle panelFrameStyle;
    private GUIStyle bannerFrameStyle;

    private readonly Color panelBg = new Color(0.06f, 0.08f, 0.14f, 0.94f);
    private readonly Color panelBorder = new Color(0.85f, 0.70f, 0.25f, 0.85f);
    private readonly Color goldColor = new Color(1.0f, 0.85f, 0.20f);
    private readonly Color cyanColor = new Color(0.35f, 0.92f, 1.0f);
    private readonly Color foodGreen = new Color(0.20f, 0.88f, 0.38f);
    private readonly Color heatOrange = new Color(1.0f, 0.36f, 0.08f);
    private readonly Color shadowColor = new Color(0f, 0f, 0f, 0.9f);
    private ThanhGiongEnemy cachedBoss;
    private float nextBossCheckTime;
    private bool guidesVisible;
    private float promptVisibleUntil;
    private ThanhGiongCampaignController.Chapter previousChapter;
    private ThanhGiongCampaignController.Weapon previousWeapon;
    private bool previousHeatReady;
    private bool previousCarrying;

    private void Start()
    {
        if (campaign != null)
        {
            previousChapter = campaign.CurrentChapter;
            previousWeapon = campaign.CurrentWeapon;
            previousHeatReady = campaign.Heat01 >= 1f;
            previousCarrying = campaign.CarriedWeapon != null;
        }
        if (combatCursor != null)
            Cursor.SetCursor(combatCursor, new Vector2(combatCursor.width * 0.18f, combatCursor.height * 0.18f), CursorMode.Auto);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) guidesVisible = !guidesVisible;
        if (campaign == null) return;

        bool heatReady = campaign.Heat01 >= 1f;
        bool carrying = campaign.CarriedWeapon != null;
        if (campaign.CurrentChapter != previousChapter ||
            campaign.CurrentWeapon != previousWeapon ||
            (heatReady && !previousHeatReady) ||
            (carrying && !previousCarrying))
            promptVisibleUntil = Time.time + 4f;

        previousChapter = campaign.CurrentChapter;
        previousWeapon = campaign.CurrentWeapon;
        previousHeatReady = heatReady;
        previousCarrying = carrying;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        objectiveStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };

        controlKeyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        controlDescStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft
        };

        statStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        barTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        bannerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        promptStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        panelFrameStyle = CreateFrameStyle(panelFrame, 8);
        bannerFrameStyle = CreateFrameStyle(bannerFrame != null ? bannerFrame : panelFrame, 8);
    }

    private void OnGUI()
    {
        if (campaign == null) return;
        EnsureStyles();

        if (guidesVisible)
        {
        // 1. LEFT MAIN OBJECTIVE & STATUS PANEL
        float leftW = Mathf.Clamp(Screen.width * 0.43f, 420f, 720f);
        float leftH = 265f;
        Rect leftRect = new Rect(20f, 20f, leftW, leftH);

        DrawFantasyBox(leftRect, panelBg, panelBorder, panelFrameStyle, 2.5f);

        // Header Title
        DrawShadowLabel(new Rect(leftRect.x + 18f, leftRect.y + 12f, leftW - 36f, 34f), campaign.StageTitle, titleStyle, goldColor, shadowColor);

        // Objective Text
        DrawShadowLabel(new Rect(leftRect.x + 18f, leftRect.y + 50f, leftW - 36f, 54f), campaign.Objective, objectiveStyle, Color.white, shadowColor);
        DrawDivider(new Rect(leftRect.x + 18f, leftRect.y + 103f, leftW - 36f, 4f));

        // Food & Growth Bar
        int currentPhase = campaign.GrowthPhase + 1;
        string foodLabel = campaign.CurrentChapter == ThanhGiongCampaignController.Chapter.Prologue
            ? $"LƯƠNG THỰC / THỂ CHẤT: {Mathf.RoundToInt(campaign.FoodProgress * 100)}% (BẬC {currentPhase}/4)"
            : $"THỂ CHẤT THÁNH KHỔNG LỒ (BẬC {currentPhase}/4 - TĂNG LỰC ĐÁNH & PHẠM VI)";
        DrawBar(new Rect(leftRect.x + 18f, leftRect.y + 112f, leftW - 36f, 26f), campaign.FoodProgress, foodGreen, foodLabel, barTextStyle);

        // Horse Heat / Fireline Bar
        string heatLabel = campaign.Heat01 >= 1f
            ? "NHIỆT LƯỢNG NGỰA SẮT: 100% >> [NHẤN F: HỎA TUYẾN SẴN SÀNG!]"
            : $"NHIỆT LƯỢNG NGỰA SẮT: {Mathf.RoundToInt(campaign.Heat01 * 100)}% (TÍCH NHIỆT KHI ĐÁNH TRÚNG)";
        Color currentHeatColor = campaign.Heat01 >= 1f ? new Color(1.0f, 0.85f, 0.1f) : heatOrange;
        DrawBar(new Rect(leftRect.x + 18f, leftRect.y + 148f, leftW - 36f, 26f), campaign.Heat01, currentHeatColor, heatLabel, barTextStyle);

        // Status Line
        DrawShadowLabel(new Rect(leftRect.x + 18f, leftRect.y + 186f, leftW - 36f, 32f), campaign.StatusLine, statStyle, cyanColor, shadowColor);

        // Subtext / Tip
        string tipText = campaign.CurrentChapter switch
        {
            ThanhGiongCampaignController.Chapter.Prologue => "Nhiệm vụ: Gom đủ 300 lương thực để Gióng lớn vụt lên, mái nhà tranh sập đổ và mở khóa Màn 2!",
            ThanhGiongCampaignController.Chapter.Preparation => "Nhiệm vụ: Hoàn tất chuỗi QTE: E (Mặc giáp) → Q (Đội nón) → E (Lên ngựa) → F (Phun lửa) để xuất quân!",
            ThanhGiongCampaignController.Chapter.Battle when campaign.CurrentWeapon == ThanhGiongCampaignController.Weapon.None => "CẢNH BÁO: Gươm sắt đã gãy! Chạy ngay đến khóm tre ngà phát sáng và nhấn [E] 3 lần để nhổ tre!",
            ThanhGiongCampaignController.Chapter.Battle => "Nhiệm vụ: Dùng gươm/tre tiêu diệt sạch 18 quân giặc và Tướng giặc để mở đường lên đỉnh Núi Sóc!",
            ThanhGiongCampaignController.Chapter.Ascension => "Nhiệm vụ: Nhấn E → Q → E cởi giáp sắt đặt lên đỉnh núi Sóc rồi bay về trời!",
            _ => ""
        };
        if (!string.IsNullOrEmpty(tipText))
        {
            DrawShadowLabel(new Rect(leftRect.x + 18f, leftRect.y + 224f, leftW - 36f, 28f), tipText, controlDescStyle, new Color(1f, 0.92f, 0.65f), shadowColor);
        }

        // 2. RIGHT CONTROLS & COMBAT TECHNIQUES PANEL
        float rightW = Mathf.Clamp(Screen.width * 0.32f, 340f, 520f);
        float rightH = 285f;
        Rect rightRect = new Rect(Screen.width - rightW - 20f, 20f, rightW, rightH);

        DrawFantasyBox(rightRect, panelBg, panelBorder, panelFrameStyle, 2.5f);

        DrawShadowLabel(new Rect(rightRect.x + 16f, rightRect.y + 12f, rightW - 32f, 30f), "ĐIỀU KHIỂN & VÕ CÔNG THÁNH GIÓNG", subtitleStyle, goldColor, shadowColor);

        float lineY = rightRect.y + 48f;
        DrawControlRow(rightRect.x + 16f, ref lineY, rightW - 32f, "[ W, A, S, D ]", "Di chuyển bốn hướng trên chiến trường");
        DrawControlRow(rightRect.x + 16f, ref lineY, rightW - 32f, "[ LEFT SHIFT ]", "Phi ngựa nước đại & Húc văng quân giặc");
        DrawControlRow(rightRect.x + 16f, ref lineY, rightW - 32f, "[ CHUỘT TRÁI ]", "Vung Gươm Sắt / Quét Tre Ngà 360° / Ném đá");
        DrawControlRow(rightRect.x + 16f, ref lineY, rightW - 32f, "[ SPACE ]", "Nhảy qua địa hình thấp và vật cản");
        DrawControlRow(rightRect.x + 16f, ref lineY, rightW - 32f, "[ E ]", "Nhặt lương thực / Nhổ Tre Ngà / Vác vật thể");
        DrawControlRow(rightRect.x + 16f, ref lineY, rightW - 32f, "[ F ]", "HỎA TUYẾN: Ngựa sắt phun lửa thiêu rụi đồn trại");
        DrawControlRow(rightRect.x + 16f, ref lineY, rightW - 32f, "[ Q / E ]", "QTE Mặc giáp vua ban & Cởi giáp về trời");
        }

        // 3. CENTER ANNOUNCEMENT BANNER
        if (!string.IsNullOrEmpty(campaign.CenterMessage))
        {
            float bannerW = Mathf.Min(880f, Screen.width * 0.86f);
            float bannerH = 88f;
            float bannerX = (Screen.width - bannerW) * 0.5f;
            float bannerY = Screen.height * 0.65f;
            Rect bannerRect = new Rect(bannerX, bannerY, bannerW, bannerH);

            DrawFantasyBox(bannerRect, new Color(0.04f, 0.05f, 0.08f, 0.96f), goldColor, bannerFrameStyle, 3f);
            DrawShadowLabel(new Rect(bannerX + 16f, bannerY + 14f, bannerW - 32f, bannerH - 28f), campaign.CenterMessage, bannerStyle, goldColor, shadowColor);
        }

        // 4. TOP-CENTER BOSS HEALTH & VULNERABILITY BAR (Understory Boss Bar)
        DrawBossHealthBar();

        // 5. CONTEXTUAL DYNAMIC BOTTOM PROMPT
        DrawContextualBottomPrompt();
    }

    private void DrawBossHealthBar()
    {
        if (campaign == null || campaign.CurrentChapter != ThanhGiongCampaignController.Chapter.Battle) return;

        if (cachedBoss == null || !cachedBoss.gameObject.activeInHierarchy || cachedBoss.HealthRatio <= 0f)
        {
            if (Time.time >= nextBossCheckTime)
            {
                nextBossCheckTime = Time.time + 1.0f;
                var enemies = FindObjectsByType<ThanhGiongEnemy>();
                foreach (var e in enemies)
                {
                    if (e.isBoss && e.gameObject.activeInHierarchy && e.HealthRatio > 0f)
                    {
                        cachedBoss = e;
                        break;
                    }
                }
            }
        }

        if (cachedBoss == null || !cachedBoss.gameObject.activeInHierarchy || cachedBoss.HealthRatio <= 0f) return;

        // Render Boss HP bar at top center
        float bossW = Mathf.Clamp(Screen.width * 0.42f, 440f, 620f);
        float bossH = 56f;
        float bossX = (Screen.width - bossW) * 0.5f;
        float bossY = 16f;
        Rect bossRect = new Rect(bossX, bossY, bossW, bossH);

        bool isVulnerable = cachedBoss.IsStuckInGround;
        Color frameBorder = isVulnerable ? new Color(1f, 0.2f, 0.05f) : panelBorder;
        DrawFantasyBox(bossRect, panelBg, frameBorder, panelFrameStyle, isVulnerable ? 3.5f : 2.5f);

        string bossTitle = isVulnerable
            ? "★ TƯỚNG GIẶC ÂN — ĐẠI ĐAO MẮC KẸT [SƠ HỞ X2 SÁT THƯƠNG]! ★"
            : $"TƯỚNG GIẶC ÂN · MÁU {Mathf.RoundToInt(cachedBoss.HealthRatio * 100)}%";

        Color barCol = isVulnerable ? new Color(1.0f, 0.22f, 0.05f) : new Color(0.85f, 0.15f, 0.15f);
        Color textCol = isVulnerable ? goldColor : Color.white;

        DrawShadowLabel(new Rect(bossX + 12f, bossY + 4f, bossW - 24f, 22f), bossTitle, statStyle, textCol, shadowColor);
        DrawBar(new Rect(bossX + 12f, bossY + 26f, bossW - 24f, 22f), cachedBoss.HealthRatio, barCol, $"{Mathf.RoundToInt(cachedBoss.HealthRatio * cachedBoss.maxHealth)} / {Mathf.RoundToInt(cachedBoss.maxHealth)}", barTextStyle);
    }

    private void DrawControlRow(float startX, ref float currentY, float rowW, string keycap, string desc)
    {
        float keycapW = 125f;
        DrawShadowLabel(new Rect(startX, currentY, keycapW, 30f), keycap, controlKeyStyle, cyanColor, shadowColor);
        DrawShadowLabel(new Rect(startX + keycapW + 6f, currentY, rowW - keycapW - 6f, 30f), desc, controlDescStyle, Color.white, shadowColor);
        currentY += 31f;
    }

    private void DrawContextualBottomPrompt()
    {
        if (!guidesVisible && Time.time >= promptVisibleUntil) return;
        string promptText = null;
        Color promptColor = goldColor;

        if (campaign.CurrentChapter == ThanhGiongCampaignController.Chapter.Prologue)
        {
            promptText = "GOM ĐỦ LƯƠNG THỰC DÂN LÀNG ĐỂ GIÓNG LỚN NHANH NHƯ THỔI!";
            promptColor = foodGreen;
        }
        else if (campaign.CurrentChapter == ThanhGiongCampaignController.Chapter.Preparation)
        {
            promptText = "NHẤN [ E ] HOẶC [ Q ] THEO YÊU CẦU ĐỂ HOÀN TẤT TRANG BỊ!";
            promptColor = goldColor;
        }
        else if (campaign.CurrentChapter == ThanhGiongCampaignController.Chapter.Battle)
        {
            if (campaign.CurrentWeapon == ThanhGiongCampaignController.Weapon.None)
            {
                promptText = ">> TIẾN LẠI KHÓM TRE VÀ NHẤN [ E ] ĐỂ NHỔ TRE NGÀ! <<";
                promptColor = new Color(1.0f, 0.35f, 0.1f);
            }
            else if (campaign.Heat01 >= 1f)
            {
                promptText = ">> NHIỆT LƯỢNG 100%! NHẤN [ F ] ĐỂ PHUN HỎA TUYẾN NGỰA SẮT! <<";
                promptColor = new Color(1.0f, 0.85f, 0.1f);
            }
            else if (campaign.CarriedWeapon != null)
            {
                promptText = ">> ĐANG VÁC VẬT THỂ — NHẤN [ CHUỘT TRÁI ] ĐỂ NÉM CHOÁNG QUÂN THÙ! <<";
                promptColor = cyanColor;
            }
        }
        else if (campaign.CurrentChapter == ThanhGiongCampaignController.Chapter.Ascension)
        {
            promptText = ">> NHẤN [ E ] ĐỂ CỞI GIÁP SẮT ĐẶT LÊN ĐỈNH NÚI SÓC! <<";
            promptColor = goldColor;
        }

        if (string.IsNullOrEmpty(promptText)) return;

        float promptW = Mathf.Min(780f, Screen.width * 0.8f);
        float promptH = 50f;
        float promptX = (Screen.width - promptW) * 0.5f;
        float promptY = Screen.height - promptH - 24f;
        Rect promptRect = new Rect(promptX, promptY, promptW, promptH);

        DrawFantasyBox(promptRect, new Color(0.04f, 0.06f, 0.10f, 0.92f), promptColor, bannerFrameStyle, 2f);
        DrawShadowLabel(promptRect, promptText, promptStyle, promptColor, shadowColor);
    }

    private static GUIStyle CreateFrameStyle(Texture2D texture, int border)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.normal.background = texture;
        style.border = new RectOffset(border, border, border, border);
        style.padding = new RectOffset(border + 4, border + 4, border + 4, border + 4);
        return style;
    }

    private void DrawDivider(Rect rect)
    {
        if (dividerTexture == null) return;
        Color previous = GUI.color;
        GUI.color = new Color(1f, .82f, .32f, .72f);
        GUI.DrawTexture(rect, dividerTexture, ScaleMode.StretchToFill, true);
        GUI.color = previous;
    }

    private static void DrawFantasyBox(Rect rect, Color bg, Color border, GUIStyle frameStyle, float borderWidth = 2f)
    {
        GUI.color = bg;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        if (frameStyle != null && frameStyle.normal.background != null)
        {
            GUI.color = border;
            GUI.Box(rect, GUIContent.none, frameStyle);
            GUI.color = Color.white;
            return;
        }

        GUI.color = border;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderWidth), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - borderWidth, rect.width, borderWidth), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, borderWidth, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - borderWidth, rect.y, borderWidth, rect.height), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void DrawBar(Rect rect, float value, Color fillColor, string label, GUIStyle fontStyle)
    {
        DrawFantasyBox(rect, new Color(0.08f, 0.10f, 0.14f, 0.95f), new Color(0.55f, 0.48f, 0.28f, 0.8f), panelFrameStyle, 1.5f);

        // Fill bar
        float clamped = Mathf.Clamp01(value);
        float fillW = (rect.width - 4f) * clamped;
        if (fillW > 0.01f)
        {
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, fillW, rect.height - 4f), Texture2D.whiteTexture);
        }

        // Drop shadow for label
        Color prev = fontStyle.normal.textColor;
        fontStyle.normal.textColor = new Color(0f, 0f, 0f, 0.95f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), label, fontStyle);

        fontStyle.normal.textColor = Color.white;
        GUI.Label(rect, label, fontStyle);
        fontStyle.normal.textColor = prev;

        GUI.color = Color.white;
    }

    private static void DrawShadowLabel(Rect rect, string text, GUIStyle style, Color textColor, Color shadow)
    {
        Color prev = style.normal.textColor;
        style.normal.textColor = shadow;
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);

        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
        style.normal.textColor = prev;
    }
}
