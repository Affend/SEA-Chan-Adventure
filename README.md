# SEA Chan Adventure

Game **showcase** bergaya *Vampire Survivors* (survivor-like) bertema **futuristik** untuk
memperkenalkan **Game Development Study Group SEA GEN 11**. Kamu mengendalikan **SEA Chan**,
bertahan dari gelombang **robot** yang makin kuat, mengambil XP untuk naik level, memilih
**upgrade senjata**, dan mengalahkan **Boss** untuk naik ke **Tier** berikutnya — tanpa akhir.

Seluruh isi game dibangun lewat kode saat runtime, jadi **tidak ada scene yang perlu di-rakit
manual** dan **tidak ada referensi file yang bisa "missing"**.

---

## Persyaratan

- **Unity 6000.4.4f1** (Unity 6). Versi lain kemungkinan besar jalan.

## Cara Menjalankan

1. Buka **Unity Hub** → **Add → Add project from disk** → pilih folder **`SEA Chan Adventure`**.
2. Buka dengan Unity **6000.4.4f1**, tunggu proses import selesai.
3. Tekan **Play**. Akan muncul **Menu Utama** (layar opening), bukan langsung ke game.
   - Script `AutoBoot` otomatis membangun game walau scene kosong.
   - Script editor `ProjectSetup` otomatis membuat `Assets/Scenes/Main.unity` + Build Settings,
     dan memastikan *Active Input Handling* mengizinkan input lama.

## Alur Tampilan (Showcase)

```
Opening (intro sinematik: teks fade in/out di atas layar hitam — bisa dilewati)
└── Menu Utama
    ├── MULAI BERMAIN ──► Gameplay (endless)
    │                      ├── naik level ──► panel pilih Upgrade
    │                      ├── ESC ─────────► Pause (lanjut / pengaturan / menu)
    │                      └── mati ────────► Game Over (input nama → papan skor)
    ├── PAPAN SKOR ──────► 10 besar (nama + skor)
    ├── PENGATURAN ──────► Master volume, Musik, Efek Suara
    └── KELUAR
```

## Kontrol

| Tombol | Fungsi |
|---|---|
| **W A S D** / **Panah** | Gerak SEA Chan |
| **Kursor (mouse)** | Membidik arah tembakan |
| Tembakan | **Otomatis** ke arah kursor (tanpa klik) |
| **ESC** | Pause / lanjut |
| Pilih upgrade | Klik tombol saat naik level |

## Fitur

- **Menu utama (opening)** dengan judul, tombol Mulai / Papan Skor / Pengaturan / Keluar,
  plus **layar opening sinematik** (fade dari hitam + teks cerita, bisa dilewati).
- **Drop kotak obat (health pack)** dengan **RNG**: robot biasa jarang (3–5%), robot berat
  (heavy/tank/hybrid) lebih sering (16%), dan **Boss hampir selalu (75%)** — jadi nyawa bisa
  terisi kembali tanpa terlalu mudah.
- **Pengaturan audio**: slider **Master Volume**, **Musik**, dan **Efek Suara**
  (tersimpan otomatis dengan `PlayerPrefs`).
- **Audio**: BGM loop + efek suara (tembakan, laser, kena damage, mati, ambil XP, naik level,
  Boss muncul, game over, klik UI).
- **4 jenis senjata** (arah mengikuti **kursor**, tembakan **otomatis**):
  1. **Pistol Orbit** — beberapa pistol *melingkari* SEA Chan dan menembak ke arah kursor
     (jumlah pistol & peluru bertambah saat naik level).
  2. **Laser** — berkas lurus ke arah kursor yang **menembus** semua musuh di garisnya.
  3. **Aura** — damage melingkar di sekitar player (pasif).
  4. **Nova Burst** — ledakan peluru ke segala arah.
- **Peta futuristik**: lantai panel logam dengan garis bercahaya cyan + **dinding/pilar**
  sebagai obstacle (player tertahan dinding), plus dekorasi senada (tong, peti, obor energi,
  terminal, kristal, tumpukan rongsokan).
- **Musuh robot**: drone, walker, spider-bot, tank, heavy-bot, dan **hybrid robot-monster**
  (badan robot + capit organik) — semua **beranimasi** (2 frame).
- **Papan skor (leaderboard)**: masukkan namamu setelah Game Over; skor disimpan
  (`Skor = musuh ×10 + boss ×500 + detik bertahan ×2`) dan ditampilkan di menu utama.
- **Boss** muncul berkala dengan **HP bar Boss** di atas layar (3 Boss mech berbeda).
- **Naik level & upgrade**: pilih 1 dari 3 (senjata baru, level senjata, Max HP, kecepatan,
  damage, cooldown, regen, radius ambil).
- **HUD baru**: bar HP & EXP, level, timer, jumlah musuh, Tier, **crosshair kursor**, dan
  **panel daftar senjata** (nama + level).
- **Pause** & **Game Over** dengan statistik, serta tombol kembali ke menu.

---

## Aset yang Dipakai

Semua aset gambar diambil dari **`Showcase-GameDev-SEA11/Assets`** (paket aset gratis),
kecuali karakter SEA Chan dari gambar buatanmu. Audio dibuat sendiri (disintesis).

| Bagian | Aset asal |
|---|---|
| Karakter **SEA Chan** | Sprite sheet pixel-art (dipotong 12 frame) + **artwork utama** dari `bonus-stablediffusion_seachan_ai_stand.png` (latar putih dibuat transparan) |
| **Lantai & Dinding** | **Dibuat sendiri** — panel logam futuristik + garis bercahaya cyan (dijamin menyambung) |
| **Dekorasi** | **Dibuat sendiri** — tong, peti, obor energi, terminal, kristal, pilar, rongsokan |
| **Musuh & Boss** | **Dibuat sendiri** — 6 robot + 1 hybrid robot-monster, 3 Boss mech (animasi 2 frame) |
| **Audio** (BGM + 9 SFX) | Dibuat sendiri (sintesis gelombang suara) |

> **Lisensi:** **lantai, dinding, dan dekorasi dibuat sendiri** (bebas dipakai), jadi peta tidak
> bergantung pada pack luar dan tampilannya konsisten. Karakter SEA Chan dari gambar buatanmu.
> Audio disintesis sendiri. Untuk pemakaian komersial, tetap cek lisensi paket yang tersisa
> (monster dari `Swish's Monster Pack`).

## Struktur File

```
Assets/
  Scripts/
    GameBootstrap.cs   // membangun game dari kode (kamera, audio, UI, gameplay)
    GameManager.cs     // alur: menu → main → pause → game over → upgrade
    UIManager.cs       // menu utama, pengaturan, HUD, pause, game over, upgrade
    UIFactory.cs       // helper UI (panel, tombol, slider, bar, teks)
    AudioManager.cs    // BGM, SFX, master volume (PlayerPrefs)
    GroundLayer.cs     // lantai tak berujung
    PropsLayer.cs      // sebar dekorasi
    WallLayer.cs       // dinding batu (obstacle) per chunk
    MonsterArt.cs      // memuat sprite monster (multi-frame) dari Resources
    Enemies.cs         // musuh, boss, XP gem, spawner endless
    Weapons.cs         // PlayerController, PlayerStats, senjata (Gun, Laser, Aura, Nova)
    PlayerVisual.cs    // sprite SEA Chan + animasi jalan
    Upgrades.cs        // daftar upgrade saat naik level
    Leaderboard.cs     // papan skor (nama + skor) disimpan di PlayerPrefs
    SpriteFactory.cs   // gambar sederhana dari kode (fallback)
    InputHelper.cs     // pembungkus input
    Health.cs, CameraFollow.cs
  Editor/
    ProjectSetup.cs    // menyiapkan scene & setting otomatis
  Resources/
    SEAChan/           // 12 frame sprite SEA Chan
    Monsters/          // monster (mon_*) & boss (boss_*)
    Environment/       // floor_stone.png, floor_sand.png
    Props/             // 13 dekorasi
    Audio/             // bgm.wav + 8 sfx
```

## Menambah / Mengganti Aset

- **Monster**: taruh PNG ke `Assets/Resources/Monsters/`. Nama mengandung `boss` → jadi Boss.
  Gambar berbentuk strip (lebar = kelipatan tinggi) otomatis dianimasikan.
- **Dekorasi**: taruh PNG ke `Assets/Resources/Props/`.
- **Karakter**: sprite dimuat dengan pola `seachan_<arah>_<nomor>.png` (arah: `front`/`left`/`right`).
- **Audio**: taruh file `.wav` di `Assets/Resources/Audio/` dengan nama yang sama seperti kode.

## Cara Mengedit (Scene & Hierarchy)

> **Project ini 100% Unity + C# (bukan Python).** Tidak ada satu pun file `.py` di dalamnya.
> Python hanya dipakai sekali oleh asisten untuk *membuat gambar & suara* (di luar project),
> bukan bagian dari game dan tidak dibutuhkan untuk menjalankannya. Divisi Python Fundamental
> tidak terkait dengan project ini.

**Kenapa scene-nya kosong / tidak ada Hierarchy?**
Ini **disengaja**. Seluruh isi game (kamera, UI, lantai, dinding, player, musuh) dibangun
**otomatis oleh kode saat Play** (`GameBootstrap`). Karena tidak ada objek yang disimpan di
scene, tidak ada satu pun referensi yang bisa "missing" — game selalu bisa dijalankan.
Scene `Assets/Scenes/Main.unity` sengaja dibiarkan kosong.

**Struktur game ditampilkan otomatis di Hierarchy saat project dibuka:**

```
PREVIEW_World
└── SEA Chan World
    ├── Main Camera          (Camera, AudioListener, CameraFollow)
    ├── AudioManager         (AudioManager)
    ├── UI                   (UIManager — menunya dibuat saat Play)
    ├── GameManager          (GameManager)
    └── Gameplay
        ├── Ground           (GroundLayer — lantai tak berujung)
        ├── Props            (PropsLayer — dekorasi)
        ├── Walls            (WallLayer — dinding/obstacle)
        ├── SEA Chan         (SpriteRenderer, Rigidbody2D, CircleCollider2D,
        │                     Health, PlayerStats, WeaponManager,
        │                     PlayerController, PlayerVisual)
        └── EnemySpawner     (EnemySpawner — robot & boss)
```

- Klik objeknya (mis. **SEA Chan**) lalu lihat komponennya di **Inspector**.
- Mau sembunyikan? matikan **`SEA Chan > Tampilkan otomatis di Hierarchy`**.
- Mau tampilkan/hapus manual? pakai **`2. Tampilkan Struktur di Hierarchy`** dan
  **`3. Hapus Struktur Pratinjau`**.
- Objek pratinjau ditandai *DontSave*: **tidak tersimpan ke scene** dan otomatis hilang
  saat Play (game membangun ulang sendiri). Jadi tidak akan dobel.

**Cara mengubah permainan (yang benar-benar berpengaruh):** edit nilai di `Assets/Scripts/`

| Mau ubah apa | File | Bagian |
|---|---|---|
| Kecepatan / HP player | `Weapons.cs` | `PlayerController`, `PlayerStats` |
| Kekuatan & jumlah pistol orbit | `Weapons.cs` | `GunWeapon` (`GunCount`, `Interval`, `Damage`) |
| Laser / Aura / Nova | `Weapons.cs` | `LaserWeapon`, `AuraWeapon`, `NovaWeapon` |
| Musuh (jumlah, HP, kecepatan) | `Enemies.cs` | `EnemySpawner` |
| Peluang drop kotak obat | `MonsterArt.cs` | `HealthDropChance` |
| Harga/isi upgrade | `Upgrades.cs` | `UpgradeManager.Roll` |
| Tampilan & isi UI | `UIManager.cs` | `BuildHud`, `BuildMenu`, dst |
| Lantai (ukuran grid) | `GroundLayer.cs` | `TileSize`, `Cols`, `Rows` |
| Dinding (bentuk & jarak) | `WallLayer.cs` | `ChunkSize`, `Block`, `Line/Room/Corner/Pillar` |
| Dekorasi | `PropsLayer.cs` | `Cell`, peluang muncul |

**Menambah / mengganti gambar & suara** (tanpa ubah kode):
- Lantai/dinding → `Assets/Resources/Environment/`
- Dekorasi → `Assets/Resources/Props/`
- Musuh/boss → `Assets/Resources/Monsters/` (nama memuat "boss" = boss)
- Karakter → `Assets/Resources/SEAChan/` (`seachan_front_*`, `seachan_side_*`)
- Gambar menu → `Assets/Resources/UI/menu_art.png`
- Suara → `Assets/Resources/Audio/` (`bgm.wav`, `hit.wav`, `laser.wav`, dst)

## Panduan Mengedit Tiap Aset

Semua gambar & suara ada di `Assets/Resources/`. Cukup **timpa / tambah file** — tidak perlu ubah kode.

| Aset | Lokasi | Ketentuan |
|---|---|---|
| **Player SEA Chan** | `Resources/SEAChan/` | Pola nama `seachan_front_0..3.png` (menghadap depan) & `seachan_side_0..3.png` (**menghadap KANAN**; arah kiri dicerminkan otomatis). Tinggi karakter diatur di `PlayerVisual.TargetHeight` (default 1.25 unit). |
| **Robot musuh** | `Resources/Monsters/` | `robot_00_drone.png`, `robot_01_walker.png`, dst. Nama **berisi "boss"** → jadi boss; **berisi "elite"** → boss cutscene (mis. `boss_3_elite_warframe.png`). Bisa 1 gambar atau **strip animasi** (lebar = kelipatan tinggi, mis. 2 frame). |
| **Lantai & dinding** | `Resources/Environment/` | `floor_block_0/1.png` (blok lantai 2 unit) & `wall_block_0/1.png` (blok dinding 1 unit). Ganti dengan tile yang **menyambung** (tepi rata). |
| **Dekorasi** | `Resources/Props/` | Nama bebas; semua PNG di folder ini otomatis disebar sebagai dekorasi. |
| **Gambar menu** | `Resources/UI/menu_art.png` | Artwork besar di menu utama (latar transparan lebih bagus). |
| **Musik** | `Resources/Audio/` | `bgm_menu.wav`, `bgm_game.wav`, `bgm_boss.wav`, `bgm_defeat.wav` (loop otomatis; `bgm_defeat` sekali putar). |
| **Efek suara** | `Resources/Audio/` | `hit`, `hurt`, `pickup`, `levelup`, `shoot`, `laser`, `boss`, `gameover`, `ui_click`. |

**Setelah mengganti gambar**, jalankan menu **`SEA Chan > 5. Perbaiki Kualitas Gambar`** supaya gambar tetap tajam (Filter Point, Mip Maps mati).

### Scene Authoring (gampang diedit)

Kalau ingin mengedit seperti project Unity biasa (objek terlihat jelas di Hierarchy):

1. Menu **`SEA Chan > 6. Buat Scene Authoring (gampang diedit)`** → membuat `Assets/Scenes/Authoring.unity`.
2. Buka scene itu, klik objeknya (mis. **SEA Chan**) → ubah nilainya di **Inspector**:
   `Move Speed`, `Pickup Radius`, `Dash Speed`, `Dash Cooldown`, dan **posisi Player**.
3. Tekan **Play** di scene itu — nilai hasil editan kamu **akan dipakai** oleh game.
   (Objek authoring otomatis disembunyikan saat bermain supaya tidak dobel.)

> Bagian yang diproses kode (lantai, dinding, dekorasi, gelombang musuh) tetap dibuat otomatis saat Play —
> itu disengaja supaya tidak ada referensi yang "missing".

## Kredit

- **Dibuat oleh: Mentor Divisi Game Dev** — Study Group SEA GEN 11.
- Karakter **SEA Chan**, kode, UI, robot, dan efek dibuat untuk keperluan *showcase* Study Group Game Development.

## Troubleshooting

| Masalah | Solusi |
|---|---|
| **Keyboard tidak berfungsi** | `Edit > Project Settings > Player > Active Input Handling` = **Both**, lalu restart Unity. |
| Layar hitam / kosong saat Play | Tunggu import selesai; cek Console tidak ada error merah. |
| Suara tidak keluar | Cek slider di **PENGATURAN** (jangan 0%), dan volume sistem. |
| Ingin build | `File > Build Settings`, pastikan `Assets/Scenes/Main.unity` ada di daftar, lalu **Build**. |

## Ide Pengembangan Lanjutan

- Tambah senjata (Nova meledak, Boomerang, Pet yang mengikuti).
- Tambah animasi SEA Chan untuk arah atas (butuh sprite tambahan).
- Tambah efek partikel & screen shake.
- Tambah papan skor tertinggi (high score) dan penyimpanan progres.
