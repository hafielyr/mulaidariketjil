# Tabungan (Saving Account)
## Referensi Mekanik Game Investasi

---

## 🎯 Gambaran Produk

### Pitch Singkat
**Rumah Aman Uangmu** — Produk keuangan paling klasik. Sebelum mengejar keuntungan, bangun dulu bentengmu. Tabungan memang nggak glamor, tapi ini fondasi yang dibutuhkan setiap pembangun kekayaan.

### Apa Sih Sebenarnya?
Tabungan adalah produk simpanan bank yang memungkinkan kamu menyimpan uang dengan aman sambil mendapat bunga kecil. Likuid sepenuhnya (ambil uangmu kapan saja), dilindungi pemerintah sampai Rp 2 miliar, dan tempat sempurna untuk dana darurat.

---

## 📊 Statistik Utama untuk Mekanik Game

| Atribut | Nilai |
|---------|-------|
| Level Risiko | ⭐ (1/5) - Sangat Rendah |
| Likuiditas | ★★★★★ (5/5) - Akses Instan |
| Kompleksitas | ★ (1/5) - Ramah Pemula |
| Min. Investasi | Rp 10.000 |
| Bunga Tipikal | 0,5% - 6% per tahun |
| Pajak Bunga | 20% |
| Jaminan Pemerintah | Hingga Rp 2 Miliar (LPS) |

---

## 🏛️ Kerangka Regulasi (OJK & LPS)

### Sang Penjaga: LPS (Lembaga Penjamin Simpanan)
Bayangkan LPS sebagai polis asuransi uangmu. Kalau bank bangkrut (jarang, tapi mungkin), LPS yang akan menanggung.

**Angka Ajaib: Rp 2.000.000.000**
- Jaminan maksimum per nasabah per bank
- Mencakup pokok + bunga
- Berlaku untuk tabungan, deposito, dan giro

### Aturan 3T (Syarat Perlindungan LPS)
Simpananmu dilindungi JIKA:
1. **Tercatat** - Tercatat dengan benar di pembukuan bank
2. **Tingkat Bunga** - Suku bunga tidak melebihi maksimum LPS
3. **Tidak Merugikan** - Kamu tidak melakukan tindakan yang merugikan bank

### Suku Bunga Penjaminan LPS Terkini (Okt 2025 - Jan 2026)
| Jenis Bank | Suku Bunga IDR | Mata Uang Asing |
|------------|----------------|-----------------|
| Bank Umum | 3,50% | 2,00% |
| BPR | 6,00% | - |

⚠️ **Tips Pro**: Kalau bank menawarkan bunga DI ATAS angka ini, simpananmu mungkin TIDAK dilindungi LPS!

---

## 🎮 Mekanik Game

### Event Bulanan

```
SETIAP BULAN:
1. Hitung rata-rata saldo harian
2. Terapkan bunga: saldo × (suku_bunga_tahunan ÷ 12)
3. Potong pajak: bunga × 20%
4. Potong biaya admin (jika ada)
5. Cek event random
```

### Rumus Perhitungan Bunga
```
Bunga Bersih Bulanan = (Rata-rata Saldo × Suku Bunga Tahunan ÷ 12) × (1 - 0,20)

Contoh:
Saldo: Rp 10.000.000
Suku Bunga Tahunan: 3%
Bunga Kotor: 10.000.000 × 0,03 ÷ 12 = Rp 25.000
Pajak (20%): Rp 5.000
Bunga Bersih: Rp 20.000
```

### Event Random

| Event | Probabilitas | Efek |
|-------|--------------|------|
| Promo Bonus Bunga | 5% | Bunga 1,5x bulan ini |
| Kenaikan BI Rate | 10% | +0,5% ke semua suku bunga |
| Penurunan BI Rate | 10% | -0,5% ke semua suku bunga |
| Bank Gagal | 0,1% | Jaminan LPS diaktifkan |

### Aksi Pemain

| Aksi | Detail |
|------|--------|
| **Setor** | Min Rp 10.000, Tanpa biaya |
| **Tarik** | Instan, Tanpa penalti |
| **Transfer (Bank Sama)** | Gratis |
| **Transfer (Bank Lain)** | Rp 6.500 |
| **Transfer (BI-FAST)** | Rp 2.500 |

---

## 🏆 Pencapaian & Skor

### Sistem Poin
- **+1 poin** per Rp 1.000 bunga yang didapat
- **+50 poin** untuk setor konsisten bulanan (3+ bulan)
- **+100 poin** untuk menyelesaikan target dana darurat
- **-100 poin** jika saldo melebihi batas LPS di satu bank

### Pencapaian yang Bisa Dibuka

| Pencapaian | Kondisi | Poin |
|------------|---------|------|
| 🎯 Langkah Pertama | Setor pertama kali | 25 |
| 🛡️ Pembangun Jaring Pengaman | Saldo mencapai Rp 1 Juta | 50 |
| 💪 Siap Finansial | Saldo mencapai Rp 10 Juta | 100 |
| 🏦 Penabung Terdiversifikasi | Rekening di 2+ bank | 75 |

---

## 💡 Tips Strategis (Untuk AI Game)

### Petunjuk Strategi Optimal
1. **Dana Darurat Dulu**: 3-6 bulan pengeluaran di tabungan sebelum investasi lain
2. **Kesadaran Batas LPS**: Pisahkan dana ke beberapa bank jika mendekati Rp 2M
3. **Berburu Suku Bunga**: Bank digital sering menawarkan bunga lebih tinggi (tapi cek perlindungan LPS!)
4. **Hindari Biaya Admin**: Pertahankan saldo minimum untuk menghindari biaya bulanan

### Kapan Merekomendasikan Produk Ini
- Pemain belum punya dana darurat
- Pemain butuh likuiditas langsung
- Pemain menghindari risiko (profil konservatif)
- Pemain baru memulai perjalanan keuangan

### Kapan TIDAK Merekomendasikan
- Pemain sudah punya dana darurat cukup
- Uang pemain akan diam selama 1+ tahun (sarankan deposito)
- Pemain mencari pertumbuhan (inflasi akan menggerus hasil)

---

## 📝 Contoh Dialog Dalam Game

### Pengenalan
> "Setiap kerajaan finansial dimulai dengan kebenaran sederhana: kamu nggak bisa investasi apa yang belum kamu tabung. Tabunganmu adalah markas keuangan — aman, mudah diakses, dan dilindungi pemerintah sampai Rp 2 miliar."

### Saat Setor Pertama
> "Boom! 💥 Kamu baru saja mengambil langkah pertama yang kebanyakan orang nggak pernah ambil. Uangmu sekarang menghasilkan bunga 24/7 sementara kamu tidur. Kecil? Ya. Tapi dari sinilah para jutawan memulai."

### Saat Mencapai Rp 1 Juta
> "Jaring Pengaman: AKTIF. Kamu sekarang punya bantalan untuk kejutan hidup yang tak terduga. Terus bangun — angka ajaibnya adalah 3-6 bulan pengeluaran."

### Peringatan LPS
> "⚠️ Perhatian! Kamu mendekati batas jaminan LPS Rp 2 miliar di bank ini. Pertimbangkan untuk menyebar kekayaanmu ke beberapa bank agar tetap terlindungi penuh."

### Saat Pembayaran Bunga
> "Ka-ching! 💰 Uangmu baru saja menghasilkan Rp [X] sementara kamu menjalani hidupmu. Memang nggak banyak, tapi ini kerja jujur. Bunga berbunga sekarang bekerja untukmu."

---

## 🔗 Koneksi ke Produk Lain

### Jalur Perkembangan Natural
```
Tabungan → Deposito Berjangka (bunga lebih tinggi, terkunci)
        → Obligasi Negara (hasil lebih baik, tetap aman)
        → Reksa Dana Indeks (saat dana darurat selesai)
```

### Produk Pelengkap
- **Auto-debit ke Reksa Dana Indeks**: Atur investasi rutin dari tabungan
- **Auto-debit ke Deposito**: Pindahkan kelebihan ke deposito dengan yield lebih tinggi
- **Cadangan untuk Trading Saham**: Simpan modal trading di sini sebelum digunakan

---

## ⚠️ Pengungkapan Risiko (Wajib Ditampilkan)

1. Suku bunga dapat berubah berdasarkan kebijakan Bank Indonesia
2. Inflasi mungkin melebihi bunga yang didapat, mengurangi daya beli riil
3. Jaminan LPS hanya berlaku jika persyaratan terpenuhi
4. Biaya admin dapat mengurangi atau menghilangkan pendapatan bunga pada saldo kecil
5. Suku bunga masa lalu tidak menjamin hasil di masa depan

---

*Versi Dokumen: 1.0*
*Terakhir Diperbarui: Januari 2025*
*Referensi Regulasi: Peraturan OJK & LPS*
