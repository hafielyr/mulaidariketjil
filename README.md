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
- **Modal awal**: Rp 5.000.000 (`CashBalance`)
- **Gaji tahunan**: Rp 10.000.000 (`YEARLY_INCOME`)
- **Pembelian**: Rp 1.000.000 per klik (`UNIT_COST`)

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
> (`MAX_YEARS`, `CashBalance`, `YEARLY_INCOME`, `UNIT_COST`). Daftar aset, tingkat risiko,
> volatilitas, dan jadwal unlock ada di `InitializeAssets()` pada
> `Server/Services/GameEngine.cs`. Kalau salah satu berubah, perbarui README ini di PR yang sama.

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
