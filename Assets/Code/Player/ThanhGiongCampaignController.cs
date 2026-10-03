using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MountedHorseController))]
public class ThanhGiongCampaignController : MonoBehaviour
{
    public enum Chapter { Prologue, Preparation, Battle, Ascension, Complete }
    public enum Weapon { None, IronSword, Bamboo, Environment }

    [Header("Scene references")]
    public GameObject battleRoot;
    public GameObject bambooRoot;
    public Transform ascensionTarget;
    public ThanhGiongCampaignVFX vfx;
    public ThanhGiongCampaignHUD hud;

    [Header("Balance")]
    public float foodPerGrowth = 100f;
    public float maxHeat = 100f;
    public int swordBreakKills = 7;
    public int victoryKills = 18;
    [Tooltip("Màn bắt đầu khi scene được mở trực tiếp.")]
    public Chapter startingChapter = Chapter.Prologue;

    public Chapter CurrentChapter { get; private set; }
    public Weapon CurrentWeapon { get; private set; }
    public float Food { get; private set; }
    public float Heat { get; private set; }
    public int GrowthPhase { get; private set; }
    public int Kills { get; private set; }
    public bool IsBattleActive => CurrentChapter == Chapter.Battle;
    public float FoodProgress => CurrentChapter == Chapter.Prologue ? Mathf.Clamp01(Food / (foodPerGrowth * 3f)) : 1f;
    public float Heat01 => Heat / maxHeat;
    public string CenterMessage { get; private set; }
    public ThanhGiongImprovisedWeapon CarriedWeapon { get; private set; }

    public string StageTitle => CurrentChapter switch
    {
        Chapter.Prologue => "MÀN 1 · TIẾNG RAO DƯỚI MÁI TRANH (LÀNG PHÙ ĐỔNG)",
        Chapter.Preparation => "MÀN 2 · RÈN THÉP & XUẤT QUÂN (LÒ RÈN TRIỀU ĐÌNH)",
        Chapter.Battle => "MÀN 3 · TRẬN TUYẾN NÚI SÓC (CÀN QUÉT GIẶC ÂN)",
        Chapter.Ascension => "MÀN 4 · HÓA THÁNH VỀ TRỜI (ĐỈNH NÚI SÓC)",
        _ => "THÁNH GIÓNG · TRUYỀN THUYẾT & DI SẢN BẤT TỬ"
    };

    public string Objective
    {
        get
        {
            if (CurrentChapter == Chapter.Prologue)
                return $"MỤC TIÊU MÀN 1: Gom đủ {foodPerGrowth * 3f} lương thực để Gióng lớn vọt thành tráng sĩ khổng lồ ({Mathf.RoundToInt(Food)}/{foodPerGrowth * 3f}). Hoàn thành sẽ tự động chuyển sang Màn 2!";
            if (CurrentChapter == Chapter.Preparation)
                return $"MỤC TIÊU MÀN 2: Hoàn tất 4 bước trang bị vua ban (Bước {Mathf.Min(qteIndex + 1, 4)}/4): Nhấn E (Mặc giáp) → Q (Đội nón) → E (Lên ngựa sắt) → F (Phun lửa kiểm tra vũ khí)!";
            if (CurrentChapter == Chapter.Battle && CurrentWeapon == Weapon.IronSword)
                return $"MỤC TIÊU MÀN 3: Càn quét tiền tuyến bằng Gươm Sắt (Hạ {Kills}/{victoryKills} giặc). Chuột trái chém gươm, SPACE nhảy, SHIFT phi ngựa, E nhặt đá/cây!";
            if (CurrentChapter == Chapter.Battle && CurrentWeapon == Weapon.None)
                return $"CẢNH BÁO: Gươm sắt đã gãy! Chạy ngay đến khóm tre ngà phát sáng và nhấn [E] {(3 - bambooQte)} lần nữa để NHỔ TRE TIẾP CHIẾN!";
            if (CurrentChapter == Chapter.Battle)
                return $"MỤC TIÊU MÀN 3: Dùng Khóm Tre Ngà quét sạch tàn quân & Tướng giặc (Hạ {Kills}/{victoryKills} giặc). Khi đủ 100% nhiệt, nhấn F phun Hỏa Tuyến!";
            if (CurrentChapter == Chapter.Ascension)
                return $"MỤC TIÊU MÀN 4: Nghi thức cởi giáp trên đỉnh Núi Sóc (Bước {Mathf.Min(qteIndex + 1, 3)}/3): Nhấn E → Q → E đặt từng mảnh giáp sắt xuống núi rồi bay về trời!";
            return "Hoàn thành đại nghiệp cứu quốc. Non sông thái bình, truyền thuyết Thánh Gióng sống mãi muôn đời.";
        }
    }

    public string StatusLine => CurrentChapter switch
    {
        Chapter.Prologue => $"Tiến độ: Lương thực {Mathf.RoundToInt(Food)}/{foodPerGrowth * 3f}  ·  Thể chất: Bậc {GrowthPhase + 1}/4" + (Food >= foodPerGrowth * 3f ? " [HOÀN THÀNH - ĐANG CHUYỂN MÀN 2...]" : ""),
        Chapter.Preparation => $"Tiến độ: Trang bị {qteIndex}/4 bước  ·  Trạng thái: " + (qteIndex switch { 0 => "Chờ mặc giáp [E]", 1 => "Đã mặc giáp vàng -> Chờ đội nón [Q]", 2 => "Đã đội nón sắt -> Chờ lên ngựa [E]", 3 => "Đã lên ngựa sắt -> Nhấn [F] phun lửa xuất quân!", _ => "Sẵn sàng xuất trận!" }),
        Chapter.Battle => $"Tiến độ: Hạ địch {Kills}/{victoryKills}  ·  Vũ khí: {WeaponName}  ·  Nhiệt lượng: {Mathf.RoundToInt(Heat01 * 100)}%" + (CarriedWeapon != null ? "  [Đang vác vật thể]" : ""),
        Chapter.Ascension => $"Tiến độ: Cởi giáp {qteIndex}/3 bước  ·  Địa điểm: Đỉnh Núi Sóc",
        _ => "Hoàn thành đại nghiệp cứu quốc · Đất nước thái bình"
    };

    private string WeaponName => CurrentWeapon switch
    {
        Weapon.IronSword => "Gươm Sắt (Chém Thư Pháp)",
        Weapon.Bamboo => "Khóm Tre Ngà (Quét 360°)",
        Weapon.Environment => "Đá / Gốc cây",
        _ => "Tay không"
    };

    private readonly Collider[] hits = new Collider[128];
    private MountedHorseController movement;
    private ThanhGiongCampaignAudio audioFx;
    private Vector3 originalScale;
    private float nextAttackTime;
    private int qteIndex;
    private int bambooQte;
    private bool isTransitioning;
    private KeyCode[] equipQte = { KeyCode.E, KeyCode.Q, KeyCode.E };

    private void Awake()
    {
        movement = GetComponent<MountedHorseController>();
        audioFx = GetComponent<ThanhGiongCampaignAudio>();
        originalScale = transform.localScale;
        if (vfx == null) vfx = FindAnyObjectByType<ThanhGiongCampaignVFX>();
        if (hud == null) hud = FindAnyObjectByType<ThanhGiongCampaignHUD>();
        if (hud != null) hud.campaign = this;
    }

    private void Start()
    {
        WarpToChapter(startingChapter);
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (CurrentChapter == Chapter.Prologue) UpdatePrologue();
        else if (CurrentChapter == Chapter.Preparation) UpdateEquipQte();
        else if (CurrentChapter == Chapter.Battle) UpdateBattle();
        else if (CurrentChapter == Chapter.Ascension) UpdateAscensionQte();
    }

    public void TeleportPlayer(Vector3 destinationPosition)
    {
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = destinationPosition;
        if (cc != null) cc.enabled = true;
        IsometricCameraFollow.Instance?.SnapToTarget();
    }

    public void WarpToChapter(Chapter targetChapter, Vector3? destinationPosition = null)
    {
        CurrentChapter = targetChapter;
        qteIndex = 0;
        bambooQte = 0;

        if (destinationPosition.HasValue)
        {
            TeleportPlayer(destinationPosition.Value);
        }

        switch (targetChapter)
        {
            case Chapter.Prologue:
                CurrentWeapon = Weapon.None;
                GrowthPhase = Mathf.Min(GrowthPhase, 1);
                ApplyGrowthScale();
                if (battleRoot != null) battleRoot.SetActive(false);
                if (bambooRoot != null) bambooRoot.SetActive(false);
                foreach (var f in FindObjectsByType<ThanhGiongCollectible>(FindObjectsInactive.Include))
                {
                    if (f.kind <= ThanhGiongCollectible.Kind.Meat) f.gameObject.SetActive(true);
                }
                ShowMessage("ĐÃ ĐI QUA CỔNG: BƯỚC VÀO MÀN 1 · LÀNG PHÙ ĐỔNG!", 3.2f);
                break;

            case Chapter.Preparation:
                GrowthPhase = 3;
                ApplyGrowthScale();
                CurrentWeapon = Weapon.None;
                if (battleRoot != null) battleRoot.SetActive(false);
                if (bambooRoot != null) bambooRoot.SetActive(false);
                ShowMessage("ĐÃ ĐI QUA CỔNG: BƯỚC VÀO MÀN 2 · RÈN THÉP & XUẤT QUÂN! [NHẤN E: MẶC GIÁP]", 3.5f);
                break;

            case Chapter.Battle:
                GrowthPhase = 3;
                ApplyGrowthScale();
                CurrentWeapon = Weapon.IronSword;
                Heat = maxHeat * 0.5f;
                Kills = 0;
                if (battleRoot != null)
                {
                    battleRoot.SetActive(true);
                    foreach (ThanhGiongEnemy enemy in battleRoot.GetComponentsInChildren<ThanhGiongEnemy>(true))
                        enemy.Initialize(this, transform);
                }
                if (bambooRoot != null) bambooRoot.SetActive(true);
                ShowMessage("ĐÃ ĐI QUA CỔNG: BƯỚC VÀO MÀN 3 · TRẬN TUYẾN NÚI SÓC!", 3.5f);
                break;

            case Chapter.Ascension:
                GrowthPhase = 3;
                ApplyGrowthScale();
                CurrentWeapon = Weapon.Bamboo;
                if (battleRoot != null) battleRoot.SetActive(false);
                movement.enabled = true;
                ShowMessage("ĐÃ ĐI QUA CỔNG: BƯỚC VÀO MÀN 4 · HÓA THÁNH VỀ TRỜI! [CỞI GIÁP ĐẶT ĐỈNH NÚI]", 3.8f);
                break;
        }

        audioFx?.PlayGrowth();
        IsometricCameraFollow.Instance?.Shake(0.4f, 0.5f);
    }

    private void UpdatePrologue()
    {
        // Interact with near collectibles
    }

    public bool TryCollect(ThanhGiongCollectible item)
    {
        if (CurrentChapter == Chapter.Prologue && item.kind <= ThanhGiongCollectible.Kind.Meat)
        {
            if (isTransitioning) return false;

            Food += item.foodValue;
            int phase = Mathf.Clamp(Mathf.FloorToInt(Food / foodPerGrowth), 0, 3);
            if (phase > GrowthPhase)
            {
                GrowthPhase = phase;
                ApplyGrowthScale();
                vfx?.PlayGrowth(transform);
                audioFx?.PlayGrowth();

                if (GrowthPhase == 3)
                {
                    // Collapse village roofs
                    CollapseVillageRoofs();
                    ShowMessage("GIÓNG VƯƠN VAI THÀNH TRÁNG SĨ KHỔNG LỒ — MÁI NHÀ TRANH SẬP ĐỔ!", 3.2f);
                }
                else
                {
                    ShowMessage(GrowthPhase == 1 ? "GIÓNG LỚN NHANH NHƯ THỔI!" : "GIÓNG VƯƠN VAI — DÂN LÀNG REO HÒ!", 2.2f);
                }
            }
            else
            {
                string foodName = item.kind switch
                {
                    ThanhGiongCollectible.Kind.Rice => "7 NONG CƠM GẠO",
                    ThanhGiongCollectible.Kind.Eggplant => "3 NONG CÀ PHÁO",
                    ThanhGiongCollectible.Kind.Firewood => "BÓ CỦI NHÓM LỬA",
                    ThanhGiongCollectible.Kind.Meat => "MÂM THỊT KHAO QUÂN",
                    _ => "LƯƠNG THỰC DÂN LÀNG"
                };
                ShowMessage($"ĐÃ GOM: {foodName} (+{Mathf.RoundToInt(item.foodValue)}) · TIẾN ĐỘ: {Mathf.RoundToInt(Food)}/300", 2.0f);
                audioFx?.PlayFoodPickup();
            }

            vfx?.PlayFoodBurst(item.transform.position, item.GetAuraColor());

            if (Food >= foodPerGrowth * 3f && !isTransitioning)
            {
                StartCoroutine(CompleteStage1AndTransition());
            }
            return true;
        }

        if (CurrentChapter == Chapter.Battle && CurrentWeapon == Weapon.None && item.kind == ThanhGiongCollectible.Kind.Bamboo)
        {
            bambooQte++;
            audioFx?.PlayBamboo();
            IsometricCameraFollow.Instance?.Shake(0.35f, 0.6f);
            if (bambooQte >= 3)
            {
                CurrentWeapon = Weapon.Bamboo;
                if (bambooRoot != null) bambooRoot.SetActive(false);
                vfx?.PlayBambooSweep(transform, 7.5f);
                ShowMessage("NHỔ TRE THÀNH CÔNG! KHÓM TRE NGÀ QUÉT 360° SẴN SÀNG!", 3f);
            }
            else
            {
                item.gameObject.SetActive(true);
                ShowMessage("DÙNG SỨC NHỔ TRE! NHẤN 'E' THÊM " + (3 - bambooQte) + " LẦN", 1.2f);
            }
            return bambooQte >= 3;
        }
        return false;
    }

    private void CollapseVillageRoofs()
    {
        audioFx?.PlayRoofCrash();
        IsometricCameraFollow.Instance?.Shake(0.85f, 0.8f);

        ThanhGiongRoofCollapse[] roofs = FindObjectsByType<ThanhGiongRoofCollapse>();
        foreach (var r in roofs)
        {
            r.Collapse();
        }
    }

    private void UpdateEquipQte()
    {
        if (isTransitioning) return;

        if (qteIndex < equipQte.Length && Input.GetKeyDown(equipQte[qteIndex]))
        {
            qteIndex++;
            audioFx?.PlaySlash();
            IsometricCameraFollow.Instance?.Shake(0.25f, 0.4f);

            if (qteIndex == 1) ShowMessage("MẶC GIÁP SẮT! (ÁO GIÁP VÀNG SÁNG RỰC) [BƯỚC TIẾP: NHẤN 'Q' ĐỘI NÓN SẮT]", 2.2f);
            else if (qteIndex == 2) ShowMessage("ĐỘI NÓN SẮT! (HÀO QUANG ĐÔNG SƠN CHIẾU RỌI) [BƯỚC TIẾP: NHẤN 'E' LÊN NGỰA SẮT]", 2.2f);
            else if (qteIndex == 3)
            {
                CurrentWeapon = Weapon.IronSword;
                Heat = maxHeat;
                audioFx?.PlayHorseRoar();
                IsometricCameraFollow.Instance?.Shake(0.8f, 0.9f);
                ShowMessage("CƯỠI NGỰA SẮT! NGỰA SẮT KÍCH HOẠT CORE LỬA! [NHẤN 'F' ĐỂ PHUN HỎA TUYẾN KIỂM TRA]", 3.2f);
            }
        }

        if (qteIndex == equipQte.Length && Input.GetKeyDown(KeyCode.F))
        {
            qteIndex = 4;
            CastFireLine();
            StartCoroutine(CompleteStage2AndTransition());
        }
    }

    private void UpdateBattle()
    {
        // Check picking up improvised weapons
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (CurrentWeapon == Weapon.None && bambooRoot != null && Vector3.Distance(transform.position, bambooRoot.transform.position) < 7f)
            {
                ThanhGiongCollectible bamboo = bambooRoot.GetComponentInChildren<ThanhGiongCollectible>(true);
                if (bamboo != null) TryCollect(bamboo);
            }
            else if (CarriedWeapon == null)
            {
                TryPickUpImprovisedWeapon();
            }
        }

        // Throw carried weapon
        if (Input.GetMouseButtonDown(0))
        {
            if (CarriedWeapon != null)
            {
                CarriedWeapon.Throw(transform.forward);
                CarriedWeapon = null;
                audioFx?.PlayStoneSmash();
                ShowMessage("NÉM VŨ KHÍ MÔI TRƯỜNG!", 1.2f);
                return;
            }
            if (Time.time >= nextAttackTime) Attack();
        }

        if (Input.GetKeyDown(KeyCode.F) && Heat >= maxHeat) CastFireLine();
    }

    private void TryPickUpImprovisedWeapon()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, 4.5f);
        foreach (var c in cols)
        {
            ThanhGiongImprovisedWeapon iw = c.GetComponentInParent<ThanhGiongImprovisedWeapon>();
            if (iw != null && iw != CarriedWeapon)
            {
                CarriedWeapon = iw;
                iw.PickUp(transform);
                ShowMessage("ĐÃ NHẶT VŨ KHÍ MÔI TRƯỜNG! [CHUỘT TRÁI ĐỂ NÉM]", 1.8f);
                break;
            }
        }
    }

    private void Attack()
    {
        if (CurrentWeapon == Weapon.None) return;

        nextAttackTime = Time.time + (CurrentWeapon == Weapon.Bamboo ? .68f : .38f);
        float growthMultiplier = 1f + GrowthPhase * 0.15f;
        float radius = (CurrentWeapon == Weapon.Bamboo ? 5.8f : 3.4f) * growthMultiplier;
        float damage = (CurrentWeapon == Weapon.Bamboo ? 52f : 36f) * (1f + GrowthPhase * 0.25f);

        Vector3 center = CurrentWeapon == Weapon.Bamboo ? transform.position : transform.position + transform.forward * 2.2f;
        int count = Physics.OverlapSphereNonAlloc(center, radius, hits, ~0, QueryTriggerInteraction.Ignore);
        int struck = 0;

        for (int i = 0; i < count; i++)
        {
            ThanhGiongEnemy enemy = hits[i].GetComponentInParent<ThanhGiongEnemy>();
            if (enemy == null || !enemy.gameObject.activeSelf) continue;
            if (CurrentWeapon == Weapon.IronSword && Vector3.Dot(transform.forward, (enemy.transform.position - transform.position).normalized) < -.15f) continue;

            float stunSecs = CurrentWeapon == Weapon.Bamboo ? 1.5f : 0.2f;
            enemy.TakeDamage(damage, stunSecs, transform.position);
            struck++;
        }

        if (struck > 0)
        {
            Heat = Mathf.Min(maxHeat, Heat + struck * 10f);
            IsometricCameraFollow.Instance?.Shake(0.2f, 0.35f);
        }

        if (CurrentWeapon == Weapon.Bamboo)
        {
            vfx?.PlayBambooSweep(transform, radius);
            audioFx?.PlayBamboo();
        }
        else
        {
            vfx?.PlayCalligraphySlash(transform, radius);
            audioFx?.PlaySlash();
        }
    }

    private void CastFireLine()
    {
        Heat = 0f;
        GetComponentInChildren<ThanhGiong.Mounts.HorseController>(true)?.BreatheFire();
        const float length = 28f;
        vfx?.PlayFireLine(transform, length);
        audioFx?.PlayFire();
        IsometricCameraFollow.Instance?.Shake(0.85f, 0.9f);

        Vector3 center = transform.position + transform.forward * (length * .5f);
        int count = Physics.OverlapBoxNonAlloc(center, new Vector3(2.5f, 2.5f, length * .5f), hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            ThanhGiongEnemy enemy = hits[i].GetComponentInParent<ThanhGiongEnemy>();
            if (enemy != null) enemy.TakeDamage(1000f, 2.5f, transform.position);
        }
        ShowMessage("HỎA TUYẾN · THIÊU RỤI ĐỒN TRẠI GIẶC (LÀNG CHÁY)!", 2.2f);
    }

    public void NotifyEnemyDefeated(ThanhGiongEnemy enemy)
    {
        Kills++;
        Heat = Mathf.Min(maxHeat, Heat + (enemy.isBoss ? 45f : 15f));

        if (CurrentWeapon == Weapon.IronSword && Kills >= swordBreakKills)
        {
            CurrentWeapon = Weapon.None;
            vfx?.PlaySwordBreak(transform);
            audioFx?.PlayBamboo();
            audioFx?.PlayStoneSmash();
            IsometricCameraFollow.Instance?.Shake(0.9f, 1.2f);

            if (bambooRoot != null) bambooRoot.SetActive(true);
            ShowMessage("RẮC! BỊ ĐAO MẺ CỦA TƯỚNG GIẶC ĐẬP GÃY GƯƠM — TÌM KHÓM TRE NGÀ PHÁT SÁNG ĐỂ NHỔ!", 4f);
        }

        if (Kills >= victoryKills && CurrentWeapon == Weapon.Bamboo && !isTransitioning)
        {
            StartCoroutine(CompleteStage3AndTransition());
        }
    }

    private void UpdateAscensionQte()
    {
        if (isTransitioning) return;

        if (qteIndex < equipQte.Length && Input.GetKeyDown(equipQte[qteIndex]))
        {
            qteIndex++;
            audioFx?.PlaySlash();
            IsometricCameraFollow.Instance?.Shake(0.3f, 0.45f);

            if (qteIndex == 1) ShowMessage("ĐẶT MẢNH GIÁP THỨ NHẤT XUỐNG ĐỈNH NÚI SÓC... [BƯỚC TIẾP: NHẤN 'Q' ĐẶT NÓN SẮT]", 2.2f);
            else if (qteIndex == 2) ShowMessage("ĐẶT NÓN SẮT XUỐNG ĐỈNH NÚI — TRỞ THÀNH DI TÍCH LỊCH SỬ! [BƯỚC TIẾP: NHẤN 'E' CƯỠI NGỰA VỀ TRỜI]", 2.5f);
            else if (qteIndex == 3)
            {
                audioFx?.PlayHorseRoar();
                IsometricCameraFollow.Instance?.Shake(1.1f, 1.2f);
                ShowMessage("NGỰA SẮT HÍ VANG DỘI TRỜI CAO — CƯỠI NGỰA VỀ TRỜI!", 3.0f);
                StartCoroutine(FlyToSky());
            }
        }
    }

    private IEnumerator CompleteStage1AndTransition()
    {
        isTransitioning = true;
        ShowMessage("★ HOÀN THÀNH MÀN 1 ★\nGIÓNG ĐÃ LỚN VỌT THÀNH TRÁNG SĨ KHỔNG LỒ!\nĐANG CHUYỂN ĐẾN KHU VỰC SỨ GIẢ & LÒ RÈN TRIỀU ĐÌNH...", 3.2f);
        IsometricCameraFollow.Instance?.Shake(0.6f, 0.7f);
        yield return new WaitForSeconds(2.6f);

        if (ThanhGiongSceneTransition.TryTransitionTo("KinhThanhRenThep", "MÀN 2 · RÈN THÉP & XUẤT QUÂN"))
            yield break;

        TeleportPlayer(new Vector3(-12f, 0f, -6f));
        BeginPreparation();
        isTransitioning = false;
    }

    private IEnumerator CompleteStage2AndTransition()
    {
        isTransitioning = true;
        ShowMessage("★ HOÀN THÀNH MÀN 2 ★\nĐÃ TRANG BỊ ĐẦY ĐỦ VŨ KHÍ & NGỰA CHIẾN!\nXUẤT QUÂN RA TIỀN TUYẾN NÚI SÓC ĐỂ CÀN QUÉT GIẶC ÂN...", 3.2f);
        IsometricCameraFollow.Instance?.Shake(0.7f, 0.8f);
        yield return new WaitForSeconds(2.6f);

        if (ThanhGiongSceneTransition.TryTransitionTo("PhaoDaiNgamQuanAn", "MÀN 3 · PHÁO ĐÀI NGẦM QUÂN ÂN"))
            yield break;

        TeleportPlayer(new Vector3(-3f, 0f, 1f));
        BeginBattle();
        isTransitioning = false;
    }

    private IEnumerator CompleteStage3AndTransition()
    {
        isTransitioning = true;
        ShowMessage("★ HOÀN THÀNH MÀN 3 ★\nQUÂN ÂN ĐẠI BẠI, TƯỚNG GIẶC ĐÃ BỊ TIÊU DIỆT!\nTIẾN LÊN ĐỈNH NÚI SÓC ĐỂ HÓA THÁNH VỀ TRỜI...", 3.5f);
        IsometricCameraFollow.Instance?.Shake(0.8f, 1.0f);
        yield return new WaitForSeconds(2.8f);

        if (ThanhGiongSceneTransition.TryTransitionToNext("CHIẾN TUYẾN MỚI ĐANG MỞ RA..."))
            yield break;

        TeleportPlayer(new Vector3(36f, 48.5f, 48f));
        BeginAscension();
        isTransitioning = false;
    }

    private IEnumerator FlyToSky()
    {
        movement.enabled = false;
        Vector3 start = transform.position;
        Vector3 end = ascensionTarget != null ? ascensionTarget.position : start + new Vector3(40f, 55f, 40f);

        float time = 0f;
        const float flyDuration = 8.5f;

        // Overview camera looking over the geographic map
        IsometricCameraFollow.Instance?.SetOverview(start + new Vector3(-25f, 42f, -25f), start + new Vector3(15f, 0f, 15f));

        bool ascensionDissolved = false;
        while (time < flyDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / flyDuration);
            // Smooth ascent curve
            transform.position = Vector3.Lerp(start, end, t * t * (3f - 2f * t));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((end - start).normalized), Time.deltaTime * 2.5f);

            // Periodically drop fire sparks and create lakes below
            if (time > 1.2f && time < 6.5f)
            {
                if (Mathf.FloorToInt(time * 3f) != Mathf.FloorToInt((time - Time.deltaTime) * 3f))
                {
                    Vector3 dropGround = new Vector3(transform.position.x + Random.Range(-6f, 6f), 0.5f, transform.position.z + Random.Range(-6f, 6f));
                    vfx?.SpawnLegendaryRemains(transform.position, dropGround);
                    audioFx?.PlayFire();
                }
            }

            // Divine Golden Dissolve into clouds (Hóa Thánh Về Trời)
            if (t >= 0.7f && !ascensionDissolved)
            {
                ascensionDissolved = true;
                vfx?.PlayAscensionDivineLight(transform.position);
                ThanhGiongGoldenDissolve heroDissolve = GetComponent<ThanhGiongGoldenDissolve>();
                if (heroDissolve == null) heroDissolve = gameObject.AddComponent<ThanhGiongGoldenDissolve>();
                StartCoroutine(heroDissolve.Play(2.2f, new Color(1.0f, 0.92f, 0.35f, 1f), 90, true));
            }

            yield return null;
        }

        CurrentChapter = Chapter.Complete;
        ShowMessage("THÁNH GIÓNG · HÓA THÁNH VỀ TRỜI · TRUYỀN THUYẾT & DI SẢN BẤT TỬ", 999f);
    }

    private void BeginPreparation()
    {
        CurrentChapter = Chapter.Preparation;
        GrowthPhase = 3;
        ApplyGrowthScale();
        CurrentWeapon = Weapon.None;
        qteIndex = 0;
        if (battleRoot != null) battleRoot.SetActive(false);
        if (bambooRoot != null) bambooRoot.SetActive(false);
        ShowMessage("BƯỚC VÀO MÀN 2: RÈN THÉP & XUẤT QUÂN!\n[NHẤN 'E': MẶC ÁO GIÁP VÀNG VUA BAN]", 3.8f);
    }

    private void BeginBattle()
    {
        CurrentChapter = Chapter.Battle;
        GrowthPhase = 3;
        ApplyGrowthScale();
        CurrentWeapon = Weapon.IronSword;
        Heat = maxHeat * 0.5f;
        Kills = 0;
        if (battleRoot != null)
        {
            battleRoot.SetActive(true);
            foreach (ThanhGiongEnemy enemy in battleRoot.GetComponentsInChildren<ThanhGiongEnemy>(true))
                enemy.Initialize(this, transform);
        }
        if (bambooRoot != null) bambooRoot.SetActive(true);
        ShowMessage("BƯỚC VÀO MÀN 3: TRẬN TUYẾN NÚI SÓC!\nCÀN QUÉT QUÂN GIẶC BẰNG GƯƠM SẮT & NGỰA SẮT!", 3.5f);
    }

    private void BeginAscension()
    {
        CurrentChapter = Chapter.Ascension;
        GrowthPhase = 3;
        ApplyGrowthScale();
        CurrentWeapon = Weapon.Bamboo;
        if (battleRoot != null) battleRoot.SetActive(false);
        movement.enabled = true;
        qteIndex = 0;
        ShowMessage("BƯỚC VÀO MÀN 4: HÓA THÁNH VỀ TRỜI!\n[NHẤN 'E': CỞI MẢNH GIÁP ĐẦU TIÊN ĐẶT LÊN ĐỈNH NÚI SÓC]", 4f);
    }

    private void ApplyGrowthScale()
    {
        // Dynamic scale from boy (0.42) to huge titan (1.45)
        float[] scales = { 0.42f, 0.72f, 1.05f, 1.45f };
        float scale = scales[GrowthPhase];
        transform.localScale = originalScale * scale;

        // Dynamic speed based on scale
        movement.walkSpeed = Mathf.Lerp(3.8f, 6.2f, GrowthPhase / 3f);
        movement.runSpeed = Mathf.Lerp(6.2f, 11.5f, GrowthPhase / 3f);
    }

    public void ShowMessage(string message, float duration)
    {
        StopCoroutine(nameof(ClearMessage));
        CenterMessage = message;
        StartCoroutine(ClearMessage(duration));
    }

    private IEnumerator ClearMessage(float duration)
    {
        yield return new WaitForSeconds(duration);
        CenterMessage = string.Empty;
    }
}
