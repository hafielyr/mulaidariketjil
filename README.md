# Tjoean - Game Edukasi Investasi

Game edukasi investasi berbasis browser untuk belajar tentang berbagai instrumen investasi di Indonesia.

## Teknologi

- ASP.NET Core 8.0
- Blazor WebAssembly
- SignalR (real-time communication)
- In-memory storage (tanpa database)

## Cara Menjalankan

```bash
cd Server
dotnet run
```

Buka browser dan akses: http://localhost:5000

## Fitur Baru

### Progressive Learning System
Instrumen investasi terbuka bertahap sesuai jadwal unlock (`UnlockYear` / `UnlockMonth`):

| Instrumen | Terbuka pada |
|---|---|
| Tabungan | Tahun 1, Bulan 1 |
| Deposito | Tahun 1, Bulan 6 |
| Reksa Dana Indeks | Tahun 2, Bulan 1 |
| Obligasi Negara | Tahun 3, Bulan 1 |
| Saham | Tahun 4, Bulan 1 |
| Emas | Tahun 5, Bulan 1 |
| Urun Dana (Securities Crowdfunding) | Tahun 6, Bulan 6 |
| Crypto | Tahun 12, Bulan 1 |

Tahun 1 permainan setara tahun kalender 2006 (`GameConfig.BaseCalendarYear`), sehingga
Tahun 12 = 2017 — tahun saat data historis crypto mulai tersedia.

### Educational Features
- Info button pada setiap investasi dengan penjelasan lengkap:
  - Apa itu instrumen tersebut
  - Tingkat risiko dan penjelasannya
  - Cocok untuk siapa
  - Ekspektasi return
- Mode Kids dan Mode Dewasa dengan penjelasan yang disesuaikan
- Intro screen sebelum bermain dengan penjelasan investasi dasar
- Auto-pause saat membuka info

### Gameplay
- Sell All button untuk menjual semua unit sekaligus
- Progress bar bulan untuk melihat progress dalam bulan
- Notifikasi unlock investasi baru
- Random events (pengeluaran & bonus)

## Aturan Permainan

- **Durasi**: 15 tahun dalam game (`MAX_YEARS`) = sekitar 15 menit real time
- **1 tahun game** = 60 detik (12 bulan x 5 detik per bulan)
- **Modal awal**: Rp 20.000.000 (`CashBalance`, sama untuk pemain dan bot)
- **Gaji tahunan**: Rp 12.000.000 di tahun 1 (`YEARLY_INCOME`), naik **10% majemuk setiap tahun**.
  Rumus: `gaji(tahun N) = 12.000.000 x 1,10^(N-1)` -> tahun 1 = Rp 12.000.000, tahun 2 =
  Rp 13.200.000, tahun 3 = Rp 14.520.000, dst. (`GameSession.GetYearlySalary`). Gaji tahun N
  dibayarkan saat tahun N berakhir.
- **Pembelian**: Rp 1.000.000 per klik (`UNIT_COST`)
- **Random event**: terjadi di tahun-tahun tertentu (`EventYears`) dengan biaya 20%-45% dari gaji
  tahunan (dibatasi Rp 2.000.000 - Rp 4.500.000)
- **Tidak bisa bayar event?** Aset dijual otomatis (portofolio → obligasi → deposito). Jika masih
  kurang, sisanya menjadi **Utang Darurat** dengan bunga 3% per bulan (~36% per tahun, berbunga
  majemuk, `DEBT_MONTHLY_INTEREST_RATE`). Utang dikurangi dari kekayaan bersih dan dilunasi
  otomatis dari kas serta gaji tahunan. Event **tidak lagi** mengakhiri permainan.
- **Game over**: hanya saat permainan mencapai akhir 15 tahun

## Jenis Investasi

Diurutkan sesuai jadwal unlock. Kolom volatilitas adalah rentang perubahan nilai per bulan
(`MinReturn` / `MaxReturn`).

| Aset | Risiko | Unlock | Volatilitas per bulan |
|------|--------|--------|-----------------------|
| Tabungan | Sangat Rendah | Tahun 1, Bulan 1 | +0.04% s/d +0.5% (selalu positif) |
| Deposito | Rendah | Tahun 1, Bulan 6 | Bunga tetap sesuai tenor (2.5%-6% per tahun) |
| Reksa Dana Indeks | Sedang | Tahun 2, Bulan 1 | -3% s/d +4% |
| Obligasi Negara | Rendah | Tahun 3, Bulan 1 | Kupon tetap sesuai seri ORI/SR |
| Saham | Tinggi | Tahun 4, Bulan 1 | -10% s/d +15% |
| Emas | Sedang | Tahun 5, Bulan 1 | -0.2% s/d +1.2% |
| Urun Dana | Tinggi | Tahun 6, Bulan 6 | -10% s/d +8% |
| Crypto | Sangat Tinggi | Tahun 12, Bulan 1 | -20% s/d +30% |

> **Jaga agar tetap sinkron:** semua angka di dua bagian di atas berasal langsung dari kode.
> Durasi, modal awal, gaji, dan harga per unit ada di `Server/Services/GameSession.cs`
> (`MAX_YEARS`, `CashBalance`, `YEARLY_INCOME`, `GetYearlySalary()`, `UNIT_COST`); nilai
> ekonominya berasal dari `Shared/GameConfig.cs` (`StartingCapital`, `BaseYearlyIncome`,
> `AnnualRaiseRate`). Daftar aset, tingkat risiko,
> volatilitas, dan jadwal unlock ada di `InitializeAssets()` pada
> `Server/Services/GameEngine.cs`. Kalau salah satu berubah, perbarui README ini di PR yang sama.

### Saham: 4 blue chip + 1 saham yang benar-benar gagal

Setiap sesi permainan mendapat **5 saham**: 3 syariah + 1 konvensional dari 20 blue chip yang
bertahan 2006-2021, ditambah **tepat 1 saham yang benar-benar gagal di BEI** (dipilih acak dari 5).
Tanpa saham terakhir ini, daftar saham hanya berisi perusahaan yang selamat (*survivorship bias*)
dan pemain belajar bahwa "saham selalu pulih".

| Ticker | Perusahaan | Yang terjadi | Akhir |
|---|---|---|---|
| BUMI | Bumi Resources | Utang akuisisi + jatuhnya harga batu bara | Rp 8.750 (2008) → Rp 50 (2015), tetap tercatat |
| INVS | Inovisi Infracom | Laporan keuangan bermasalah | Disuspensi 13 Feb 2015, delisting 23 Okt 2017 |
| DAVO | Davomas Abadi | Gagal bayar kupon obligasi USD | Disuspensi 9 Mar 2012, delisting 21 Jan 2015 |
| SIAP | Sekawan Intipratama | Cerita tambang batu bara yang tak pernah produksi | Disuspensi 9 Nov 2015, delisting 17 Jun 2019 |
| BORN | Borneo Lumbung Energi & Metal | Utang USD 1 miliar untuk masuk Bumi Plc | IPO Rp 1.170 (2010), delisting 20 Jan 2020 |

Mekanisme di dalam permainan:

- Saham yang **disuspensi** tidak bisa dibeli maupun dijual (termasuk untuk membayar kejadian acak) —
  posisinya terkunci di harga terakhir, persis seperti di bursa.
- Saat **delisting**, kepemilikan dihapus dari portofolio dengan nilai sisa (`residual_value_per_share`,
  nol untuk empat dari lima saham) dan selisihnya dicatat sebagai kerugian terealisasi, sehingga P/L
  dan kekayaan bersih ikut turun.
- INVS dan BORN baru muncul di pasar pada bulan IPO-nya (Juli 2009 dan November 2010).
- Bot pembanding tidak pernah membeli saham berisiko tinggi ini.

Harga bulanan dan metadata kegagalan ada di `Data/Stocks/13_failed_stock_monthly_prices.json`.
Harga BUMI adalah data riil (Yahoo Finance); empat lainnya **direkonstruksi** dari tanggal dan harga
yang terdokumentasi publik karena feed gratis tidak lagi menyediakan riwayat saham yang sudah
delisting — lihat `data_quality_notes` dan `sources` di file tersebut.

## Struktur Project

```
InvestmentGame/
├── Server/
│   ├── Program.cs          # Entry point
│   ├── Hubs/
│   │   └── GameHub.cs      # SignalR Hub
│   └── Services/
│       ├── GameEngine.cs   # Core game logic
│       └── GameSession.cs  # Player session state
├── Client/
│   ├── Program.cs          # Blazor WASM entry
│   ├── Pages/
│   │   └── Game.razor      # Main game UI
│   └── Services/
│       └── GameClient.cs   # SignalR client
└── Shared/
    ├── GameConfig.cs       # Calendar-year mapping (Year 1 = 2006)
    └── Models/
        └── PortfolioItem.cs # Shared models
```

## UI Language

- User Interface: Bahasa Indonesia
- Code: English

## License

Open source - Free to use for educational purposes.
