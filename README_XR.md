# Assignment 2 – NASA Fireballs XR (Unity)

Scene XR untuk dataset NASA Fireballs. Pembaca berdiri di satu posisi tetap (home position),
bisa menoleh 360° dan melihat atas/bawah, tetapi tidak bisa bergerak dan tidak ada interaksi
yang mengubah visualisasi (tanpa click, hover, filter, dropdown, selection).

## Cara membuka
1. Unity **6000.6.0f1** (URP). Buka folder project ini lewat Unity Hub.
2. Tunggu import dan compile selesai (Console harus bebas error merah).
3. Buka `Assets/Scenes/SampleScene.unity` lalu tekan **Play**.
4. Atur Game view ke **Full HD (1920×1080)** sebelum mengambil screenshot.

## Kontrol (hanya mengubah arah kepala, posisi tidak pernah berubah)
| Input | Fungsi |
|---|---|
| Mouse | menoleh 360° (kiri/kanan) dan atas/bawah |
| H | kembali ke home view |
| 1 / 2 / 3 / 4 | langsung menoleh ke bar chart / world map / timeline / scatter |
| Esc / L | lepas / kunci kursor |

## Home position
Pembaca berdiri di (0, 1.6, −10), menghadap +Z (arah jam 12), mata setinggi 1,6 m, awal pandangan
miring 10° ke bawah: bar chart di depan terlihat langsung dan world map di lantai terlihat di bawahnya.
Timeline dan scatter perlu menoleh ±60°.

## Tata letak (diukur dari home, sesuai tabel Room Builder)
| View | Jam | Jarak | Lebar | Tinggi | Isi |
|---|---|---|---|---|---|
| V1 Bar Chart | 12 | 3 m | 3 m | eye | 10 fireball teratas (Q1) |
| V2 World Map | 12 | 2 m | 3 m | floor | peta dunia di lantai (Q2, lokasi Q5) |
| V3 Event | 2 | 2 m | 1 m | waist | timeline waktu kejadian (Q3) |
| V4 Altitude | 10 | 2 m | 1 m | waist | energy vs altitude (Q4, Q5) |

Tampak atas: `XR_layout_topdown.png`. Semua angka ada di komponen `RoomLayout` (objek `RoomLayout` di scene).

## Reader questions dan posisi baca
| Q | Dijawab dari | Cara membaca |
|---|---|---|
| Q1 | home, tombol 1 | bar tertinggi = #1, 441 kt (egocentric: pembaca berada di dalam ruang data) |
| Q2 | tombol 2 | titik #1 di world map, 54,8°N 61,1°E (allocentric: overview dari luar) |
| Q3 | tombol 3 | timeline, dan tanggal pada label bar (15 Feb 2013) |
| Q4 | tombol 4 | scatter energy vs altitude |
| Q5 | tombol 4 lalu 2 | event berenergi tinggi dengan altitude rendah di scatter; warna rank dan cincin yang sama dipakai di world map untuk lokasinya |

Event yang sama dikenali di semua view lewat nomor rank dan warna rank (palet di `Fireballdata.cs`).
Cincin putih pada marker peta = altitude di bawah median dataset (33,3 km).

## Script
- `RoomLayout.cs` – menaruh semua view sesuai tabel di atas dan mengecek ukuran label (dmm).
- `WorldMapTable.cs` – world map (tekstur `Assets/Resources/world_map.png`, proyeksi equirectangular).
- `VirtualRoom.cs` – lantai, dinding, langit-langit, penanda home.
- `LookAroundOnly.cs` – menoleh saja, tidak bergerak.
- `RankingBars.cs`, `Fireballdata.cs` – bar chart dan data/palet (PIC 1).

## Evidence XR
`home.png` (H), `egocentric.png` (tombol 1, 3 atau 4), `allocentric.png` (tombol 2), semuanya 1920×1080
dari scene yang berjalan. Walkthrough maksimal 60 detik: hanya menoleh, tanpa interaksi yang mengubah data.

## Catatan yang belum selesai
- `ScatterPlot.cs` belum ada di repo (hanya `.meta`); scatter di scene masih berupa objek tersimpan.
- Timeline (V3) belum ada di scene; slot `timelineRoot` di `RoomLayout` masih kosong.
- Shader URP Unlit dipakai untuk ruangan dan peta; untuk build standalone masukkan ke Always Included Shaders.
