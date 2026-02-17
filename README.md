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
- **Tahun 1**: Hanya investasi risiko rendah (Tabungan, Deposito, Obligasi)
- **Tahun 2**: Unlock investasi risiko sedang (Emas, ETF/Reksa Dana)
- **Tahun 3+**: Unlock investasi risiko tinggi (Saham, Equity Crowdfunding)

### Educational Features
- Info button pada setiap investasi dengan penjelasan lengkap:
  - Apa itu instrumen tersebut
  - Tingkat risiko dan penjelasannya
  - Cocok untuk siapa
  - Ekspektasi return
- Intro screen sebelum bermain dengan penjelasan investasi dasar
- Auto-pause saat membuka info

### Gameplay
- Sell All button untuk menjual semua unit sekaligus
- Progress bar bulan untuk melihat progress dalam bulan
- Notifikasi unlock investasi baru
- Random events (pengeluaran & bonus)

## Aturan Permainan

- **Durasi**: 20 tahun dalam game = 20-30 menit real time
- **1 tahun game** = 60 detik (12 bulan x 5 detik per bulan)
- **Modal awal**: Rp 10.000.000
- **Gaji tahunan**: Rp 12.000.000
- **Pembelian**: Rp 1.000.000 per klik

## Jenis Investasi

| Aset | Risiko | Unlock | Volatilitas |
|------|--------|--------|-------------|
| Tabungan | Sangat Rendah | Tahun 1 | ~0.1-0.3% |
| Deposito | Rendah | Tahun 1 | ~0.2-0.5% |
| Obligasi | Rendah | Tahun 1 | ~0.3-0.8% |
| Emas | Sedang | Tahun 2 | ~0.8-2.5% |
| ETF/Reksa Dana | Sedang | Tahun 2 | ~1-3.5% |
| Saham | Tinggi | Tahun 3 | ~2-8% |
| Equity Crowdfunding | Sangat Tinggi | Tahun 3 | ~5-15% |

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
    └── Models/
        └── PortfolioItem.cs # Shared models
```

## UI Language

- User Interface: Bahasa Indonesia
- Code: English

## License

Open source - Free to use for educational purposes.
