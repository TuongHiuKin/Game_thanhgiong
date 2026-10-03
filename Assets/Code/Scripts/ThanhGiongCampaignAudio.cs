using UnityEngine;

public class ThanhGiongCampaignAudio : MonoBehaviour
{
    private AudioSource music;
    private AudioSource sfx;
    private AudioClip growth;
    private AudioClip slash;
    private AudioClip bamboo;
    private AudioClip fire;
    private AudioClip hoof;
    private AudioClip horseRoar;
    private AudioClip roofCrash;
    private AudioClip stoneSmash;
    private AudioClip foodPickup;
    private float nextHoof;

    private void Awake()
    {
        music = gameObject.AddComponent<AudioSource>();
        sfx = gameObject.AddComponent<AudioSource>();
        music.volume = .22f;
        music.loop = true;
        music.spatialBlend = 0f;
        sfx.volume = .78f;
        sfx.spatialBlend = 0f;

        growth = Tone("Vuon vai", 150f, 520f, .85f, .22f, false);
        slash = Tone("Guom sat thu phap", 720f, 110f, .26f, .14f, true);
        bamboo = Tone("Tre gay gio tan", 310f, 65f, .42f, .48f, true);
        fire = Tone("Lua rit hoa tuyen", 110f, 48f, .95f, .65f, true);
        hoof = Tone("Vo sat nang", 68f, 38f, .18f, .42f, true);
        horseRoar = Tone("Ngua sat hi vang", 320f, 880f, 1.4f, .28f, false);
        roofCrash = Tone("Mai tranh sap", 90f, 35f, .7f, .75f, true);
        stoneSmash = Tone("Dap da pha cay", 140f, 45f, .55f, .68f, true);
        foodPickup = Tone("Thu thap luong thuc", 523.25f, 1046.5f, .35f, .05f, true);

        music.clip = BuildEpicVietnameseFolkLoop();
        music.Play();
    }

    private void Update()
    {
        ThanhGiongCampaignController campaign = GetComponent<ThanhGiongCampaignController>();
        if (campaign == null || campaign.CurrentChapter == ThanhGiongCampaignController.Chapter.Prologue) return;
        if ((Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f) && Time.time >= nextHoof)
        {
            nextHoof = Time.time + (Input.GetKey(KeyCode.LeftShift) ? .22f : .34f);
            Play(hoof, .48f);
        }
    }

    public void PlayGrowth() => Play(growth, .95f);
    public void PlayFoodPickup() => Play(foodPickup, .85f);
    public void PlaySlash() => Play(slash, .72f);
    public void PlayBamboo() => Play(bamboo, .95f);
    public void PlayFire() => Play(fire, 1f);
    public void PlayHorseRoar() => Play(horseRoar, 1f);
    public void PlayRoofCrash() => Play(roofCrash, .88f);
    public void PlayStoneSmash() => Play(stoneSmash, .9f);

    private void Play(AudioClip clip, float volume)
    {
        if (sfx != null && clip != null) sfx.PlayOneShot(clip, volume);
    }

    private static AudioClip Tone(string name, float startHz, float endHz, float duration, float noise, bool decay)
    {
        const int rate = 22050;
        int count = Mathf.CeilToInt(duration * rate);
        float[] data = new float[count];
        uint seed = 192837u;
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float hz = Mathf.Lerp(startHz, endHz, t);
            phase += hz / rate * Mathf.PI * 2f;
            seed = seed * 1664525u + 1013904223u;
            float random = ((seed >> 8) & 0xffff) / 32768f - 1f;
            float envelope = decay ? Mathf.Pow(1f - t, 2.2f) : Mathf.Sin(t * Mathf.PI);
            data[i] = (Mathf.Sin(phase) * (1f - noise) + random * noise) * envelope * .55f;
        }
        AudioClip clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip BuildEpicVietnameseFolkLoop()
    {
        const int rate = 22050;
        const float duration = 12f;
        int count = Mathf.CeilToInt(duration * rate);
        float[] data = new float[count];
        // Pentatonic scale (Do, Re, Mi, Sol, La - 220Hz, 247Hz, 277Hz, 330Hz, 370Hz, 440Hz)
        float[] notes = { 220f, 261.63f, 293.66f, 329.63f, 392f, 440f, 392f, 329.63f, 293.66f, 261.63f, 220f, 329.63f };
        for (int i = 0; i < count; i++)
        {
            float seconds = i / (float)rate;
            int beat = Mathf.FloorToInt(seconds * 2.5f);
            float beatT = seconds * 2.5f - beat;
            float note = notes[beat % notes.Length];
            // T'rung / Dan day pluck
            float pluck = Mathf.Sin(seconds * note * Mathf.PI * 2f) * Mathf.Exp(-beatT * 4.8f);
            // Heavy Battle Drum (Trong tran)
            float drumPhase = seconds % 1.2f;
            float drum = Mathf.Sin(seconds * 55f * Mathf.PI * 2f) * Mathf.Exp(-drumPhase * 15f);
            // Synthwave bass pulse
            float bass = Mathf.Sin(seconds * 110f * Mathf.PI * 2f) * 0.12f;

            data[i] = pluck * .24f + drum * .32f + bass;
        }
        AudioClip clip = AudioClip.Create("Trong tran, T'rung va dan day - Epic Folk", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
