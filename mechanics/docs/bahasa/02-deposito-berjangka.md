# Deposito Berjangka (Certificate of Deposit)
## Referensi Mekanik Game Investasi

---

## 🎯 Gambaran Produk

### Pitch Singkat
**Ruang VIP Uangmu** — Keamanan sama seperti tabungan, tapi dengan keuntungan lebih. Kunci uangmu untuk waktu tertentu, dapat hadiah bunga lebih tinggi. Ini penundaan kepuasan yang benar-benar membayar.

### Apa Sih Sebenarnya?
Deposito Berjangka adalah produk simpanan dengan jangka waktu tetap. Kamu setuju mengunci uangmu untuk periode tertentu (1 bulan sampai 24 bulan), dan sebagai gantinya, bank membayarmu bunga lebih tinggi dari tabungan biasa. Tetap dilindungi penuh oleh LPS sampai Rp 2 miliar.

---

## 📊 Statistik Utama untuk Mekanik Game

| Atribut | Nilai |
|---------|-------|
| Level Risiko | ⭐ (1/5) - Sangat Rendah |
| Likuiditas | ★★ (2/5) - Terkunci Sampai Jatuh Tempo |
| Kompleksitas | ★★ (2/5) - Mudah Dipahami |
| Min. Investasi | Rp 1.000.000 (tipikal: Rp 8.000.000) |
| Rentang Bunga | 2,5% - 6% per tahun (bervariasi per tenor) |
| Pajak Bunga | 20% |
| Jaminan Pemerintah | Hingga Rp 2 Miliar (LPS) |

---

## 🏛️ Kerangka Regulasi (OJK & LPS)

### Perlindungan Sama, Hasil Lebih Baik
Deposito menikmati perlindungan LPS yang sama seperti tabungan:
- **Jaminan Maksimum**: Rp 2.000.000.000 per nasabah per bank
- **Cakupan**: Pokok + bunga terakumulasi
- **Aturan 3T yang Sama Berlaku**

### Suku Bunga per Tenor (Rentang Tipikal)

| Tenor | Rentang Suku Bunga |
|-------|---------------------|
| 1 Bulan | 2,5% - 4,0% |
| 3 Bulan | 3,0% - 4,5% |
| 6 Bulan | 3,5% - 5,0% |
| 12 Bulan | 4,0% - 5,5% |
| 24 Bulan | 4,5% - 6,0% |

⚠️ **Peringatan Suku Bunga LPS**: Kalau bank menawarkan bunga melebihi maksimum LPS (saat ini 3,5% untuk bank umum), depositomu mungkin tidak sepenuhnya dilindungi!

---

## 🎮 Mekanik Game

### Membuka Deposito

```
PEMAIN HARUS MEMILIH:
1. Jumlah (min Rp 1.000.000)
2. Tenor (1, 3, 6, 12, atau 24 bulan)
3. Opsi Perpanjangan (ARO, ARO+Bunga, Non-ARO)

SISTEM KEMUDIAN:
- Mengunci dana sampai tanggal jatuh tempo
- Mencatat suku bunga (tetap selama tenor)
- Menerbitkan sertifikat digital
```

### Opsi Perpanjangan Dijelaskan

| Opsi | Yang Terjadi Saat Jatuh Tempo |
|------|-------------------------------|
| **ARO** (Auto Roll Over) | Pokok diperpanjang, bunga dikirim ke tabungan |
| **ARO + Bunga** | Pokok + bunga diperpanjang (bunga berbunga!) |
| **Non-ARO** | Semua kembali ke rekening tabungan |

### Perhitungan Bunga

```
Total Bunga = Pokok × Suku Bunga Tahunan × (Tenor dalam hari ÷ 365)
Bunga Bersih = Total Bunga × (1 - 0,20)

Contoh (deposito 12 bulan):
Pokok: Rp 100.000.000
Suku Bunga Tahunan: 5%
Bunga Kotor: 100.000.000 × 0,05 = Rp 5.000.000
Pajak (20%): Rp 1.000.000
Bunga Bersih: Rp 4.000.000
```

### Penalti Pencairan Dini

```
JIKA pemain mencairkan sebelum jatuh tempo:
- Kehilangan SEMUA bunga terakumulasi
- Bayar biaya penalti: 1% dari pokok
- Harus memberitahu bank 3 hari sebelumnya
```

### Event Random

| Event | Probabilitas | Efek |
|-------|--------------|------|
| Promo Suku Bunga Tersedia | 8% | +1% bonus bunga untuk deposito baru |
| Kenaikan BI Rate | 8% | Deposito baru dapat bunga lebih baik |
| Penurunan BI Rate | 7% | Deposito baru dapat bunga lebih rendah |
| Bank Gagal | 0,1% | Jaminan LPS diaktifkan |

---

## 🏆 Pencapaian & Skor

### Sistem Poin
- **+1 poin** per Rp 10.000 bunga yang didapat
- **+25 poin** untuk deposito 6 bulan ditahan sampai jatuh tempo
- **+50 poin** untuk deposito 12 bulan ditahan sampai jatuh tempo
- **+100 poin** untuk deposito 24 bulan ditahan sampai jatuh tempo
- **-30 poin** untuk pencairan dini

### Pencapaian yang Bisa Dibuka

| Pencapaian | Kondisi | Poin |
|------------|---------|------|
| 🔐 Terkunci & Siap | Buka deposito pertama | 30 |
| ⏳ Investor Sabar | Tahan deposito 12 bulan sampai jatuh tempo | 75 |
| 🔄 Master Bunga Berbunga | ARO dengan bunga 3 kali | 100 |
| 🏦 Diversifikasi Deposito | Deposito di 3 tenor berbeda | 60 |

---

## 💡 Tips Strategis (Untuk AI Game)

### Petunjuk Strategi Optimal
1. **Strategi Tangga**: Sebarkan deposito di berbagai jatuh tempo untuk likuiditas + hasil
2. **Timing Kunci Bunga**: Kunci tenor lebih panjang saat bunga tinggi
3. **Kekuatan Bunga Berbunga**: Gunakan ARO+Bunga untuk pertumbuhan maksimum
4. **Kesadaran LPS**: Tetap di bawah Rp 2M per bank

### Kapan Merekomendasikan Produk Ini
- Pemain punya dana darurat stabil di tabungan
- Pemain punya uang yang tidak dibutuhkan selama 3+ bulan
- Pemain ingin hasil terjamin (tanpa risiko pasar)
- Pemain lebih suka pendapatan yang bisa diprediksi

### Kapan TIDAK Merekomendasikan
- Pemain mungkin butuh uang secara tak terduga
- Pemain mencari pertumbuhan lebih tinggi (sarankan reksa dana indeks)
- Pemain sudah mendekati batas LPS
- Suku bunga diperkirakan akan naik signifikan

### Contoh Strategi Tangga
```
Alih-alih: Rp 120 Juta di satu deposito 12 bulan
Lakukan ini:
- Rp 30 Juta di deposito 3 bulan
- Rp 30 Juta di deposito 6 bulan
- Rp 30 Juta di deposito 9 bulan
- Rp 30 Juta di deposito 12 bulan

Hasil: Satu deposito jatuh tempo setiap 3 bulan = akses reguler + bunga lumayan
```

---

## 📝 Contoh Dialog Dalam Game

### Pengenalan
> "Siap naik level dari tabungan? Deposito itu seperti tabungan VIP — kamu berkomitmen untuk jangka waktu tertentu, dan bank memberi hadiah atas kesabaranmu dengan bunga lebih baik. Keamanan sama, keuntungan lebih."

### Saat Membuka Deposito Pertama
> "🔐 Deposito TERKUNCI! Uangmu sekarang bekerja lebih keras untukmu. Jam sudah berjalan menuju hari gajian. Ingat saja: kesabaran benar-benar membayarmu sekarang."

### Notifikasi Jatuh Tempo
> "⏰ DING DING! Depositomu [X] bulan jatuh tempo dalam 7 hari! Waktu keputusan: Perpanjang untuk keajaiban bunga berbunga, atau ambil kemenangan dan pindahkan ke tempat lain?"

### Saat Jatuh Tempo Berhasil
> "💰 HARI GAJIAN! Kesabaranmu telah dihadiahi Rp [X] dalam bunga. Itu [Y]% pertumbuhan sementara kamu tidak melakukan apa-apa. Begini cara kekayaan dibangun diam-diam."

### Peringatan Pencairan Dini
> "⚠️ Tunggu dulu! Mencairkan lebih awal berarti kehilangan SEMUA bunga terakumulasi PLUS penalti 1%. Itu Rp [X] hilang. Yakin? Kadang langkah terbaik adalah tidak bergerak."

### Saat Perpanjangan Berbunga
> "🔄 BUNGA BERBUNGA AKTIF! Bungamu sekarang menghasilkan bunga. Einstein menyebut ini keajaiban dunia ke-8. Kamu sekarang bermain jangka panjang seperti pro."

---

## 🔗 Koneksi ke Produk Lain

### Jalur Perkembangan Natural
```
Tabungan (dana darurat selesai)
    ↓
Deposito Berjangka ← Kamu di sini! 📍
    ↓
Obligasi Pemerintah (jangka lebih panjang, keamanan serupa)
    ↓
Reksa Dana Indeks (saat nyaman dengan risiko pasar)
```

### Strategi Pelengkap
- **Kombo Tabungan + Deposito**: Dana darurat di tabungan, kelebihan di deposito
- **Tangga Deposito**: Beberapa deposito jatuh tempo di waktu berbeda
- **Jembatan Deposito → SBN**: Saat deposito jatuh tempo selama periode penawaran SBN

---

## ⚠️ Pengungkapan Risiko (Wajib Ditampilkan)

1. Pencairan dini mengakibatkan kehilangan bunga dan biaya penalti
2. Suku bunga ditetapkan saat pembukaan — kamu mungkin kehilangan bunga lebih baik nantinya
3. Inflasi mungkin melebihi bunga deposito, mengurangi daya beli riil
4. Jaminan LPS hanya berlaku jika semua persyaratan terpenuhi
5. Suku bunga yang ditampilkan adalah kotor; hasil aktual setelah pajak 20%
6. Bunga masa lalu tidak menjamin hasil di masa depan

---

*Versi Dokumen: 1.0*
*Terakhir Diperbarui: Januari 2025*
*Referensi Regulasi: Peraturan OJK & LPS*
