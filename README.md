# Lightweight-TempMonitor

A lightweight Windows **system-tray temperature monitor**. Single-file, self-contained executable (~47 MB) built with C# / .NET 9 (WinForms) and [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).

## Features

- **Dynamic tray icon** — shows the hottest component (CPU / GPU / SSD / MB / Other) with a color state:
  green (ok), amber (warm), red (hot). Gray when no data is available.
- **Hover tooltip** — current temperatures, fan speed, load and session-peak, e.g.
  `CPU  51°C ↑87  12%  900RPM`.
- **Threshold alerts** — balloon notification that names the offending hardware when a threshold is
  breached, with an optional beep. Hysteresis + cooldown prevent notification spam.
- **Details window** — a live 10-minute history graph (300 samples at 2 s) with dynamic °C gridlines
  that follow real hardware temperatures, plus a full sensor table (value / min / max per session).
- **Settings window** — per-group thresholds and per-sensor overrides, beep toggle, live refresh.
- **Auto-start** with Windows (HKCU Run), toggleable from the tray menu.
- **Truly lightweight** — 2 s poll interval, zero-allocation tick, cached sensor references.
  Steady state: ~78 MB private memory.

## Requirements

- Windows 10 / 11 (x64)
- To build: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). The published output is
  self-contained and needs no runtime installed.
- **Run as Administrator for full sensor coverage** (CPU MSR temps, motherboard Super I/O sensors,
  NVMe temperatures via the kernel driver). Without elevation, only GPU sensors are typically available.

## Usage

1. Publish or download the build output (see below).
2. Run `TempMonitor.exe`.
3. Right-click the tray icon for the menu: **Details**, **Settings**, **Auto-start**, **Exit**.

If the sensor driver fails to initialize, an error dialog appears suggesting to run as administrator.
UI exceptions are written to `error.log` next to the executable.

## Configuration

`settings.json` is created automatically next to the executable on first run.

| Key | Default | Description |
| --- | --- | --- |
| `pollIntervalMs` | `2000` | Sensor polling interval in ms. |
| `alertCooldownMinutes` | `10` | Minimum minutes between alerts for the same sensor. |
| `hysteresisC` | `5` | °C a value must drop below the threshold before it can alert again. |
| `warnMarginC` | `10` | °C below the threshold at which the icon turns amber (warm). |
| `alertBeep` | `true` | Play a beep when an alert fires. |
| `thresholds.default` | `85` | Fallback threshold for unlisted groups. |
| `thresholds.cpu` | `85` | CPU alert threshold (°C). |
| `thresholds.gpu` | `85` | GPU alert threshold (°C). |
| `thresholds.storage` | `60` | SSD/Storage alert threshold (°C). |
| `thresholds.motherboard` | `70` | Motherboard alert threshold (°C). |
| `thresholds.perSensor` | `{}` | Per-sensor overrides, e.g. `{ "hw_1": 65 }`. |
| `groups.cpu / gpu / storage / motherboard / controller` | `true` | Enable/disable hardware groups. |

## Build & publish

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Output: `bin/Release/net9.0-windows/win-x64/publish/TempMonitor.exe` (+ `libMonoPosixHelper.dll`,
`MonoPosixHelper.dll`).

## Project structure

```
Program.cs              Entry point, single-instance mutex, global exception log
Config/                 AppConfig + settings.json load/save
Core/                   Groups, sensor catalog, alert engine, polling engine
Ui/                     Tray icon + renderer, Details window, history graph, Settings window
```

## Acknowledgements

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (MPL-2.0) —
  hardware sensor library used as a dependency.

## License

[MIT](LICENSE)

---

# Lightweight-TempMonitor (Bahasa Indonesia)

Monitor suhu PC yang ringan di **system tray Windows**. File tunggal, self-contained (~47 MB),
dibangun dengan C# / .NET 9 (WinForms) dan [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).

## Fitur

- **Ikon tray dinamis** — menampilkan komponen terpanas (CPU / GPU / SSD / MB / Lainnya) dengan
  status warna: hijau (normal), kuning (warm), merah (panas). Abu-abu jika tidak ada data.
- **Tooltip saat hover** — suhu terkini, kecepatan fan, beban, dan puncak sesi, contoh:
  `CPU  51°C ↑87  12%  900RPM`.
- **Peringatan threshold** — notifikasi balloon yang menyebutkan hardware yang melewati ambang batas,
  dengan beep opsional. Hysteresis + cooldown mencegah notifikasi berulang.
- **Jendela Details** — grafik history 10 menit live (300 sampel tiap 2 detik) dengan garis grid °C
  dinamis yang mengikuti suhu hardware nyata, plus tabel lengkap semua sensor (nilai / min / max sesi).
- **Jendela Settings** — ambang batas per grup dan override per sensor, toggle beep, refresh langsung.
- **Auto-start** saat Windows menyala (HKCU Run), bisa diaktifkan dari menu tray.
- **Sangat ringan** — interval polling 2 detik, tick tanpa alokasi, referensi sensor di-cache.
  Kondisi stabil: ~78 MB memori privat.

## Kebutuhan

- Windows 10 / 11 (x64)
- Untuk build: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). Hasil publish sudah
  self-contained, tanpa perlu install runtime.
- **Jalankan sebagai Administrator untuk cakupan sensor penuh** (suhu CPU via MSR, sensor Super I/O
  motherboard, suhu NVMe via driver kernel). Tanpa elevasi, biasanya hanya sensor GPU yang terbaca.

## Penggunaan

1. Publish atau unduh hasil build (lihat di bawah).
2. Jalankan `TempMonitor.exe`.
3. Klik kanan ikon tray untuk menu: **Details**, **Settings**, **Auto-start**, **Exit**.

Jika driver sensor gagal diinisialisasi, dialog error muncul menyarankan menjalankan sebagai
administrator. Exception UI ditulis ke `error.log` di samping executable.

## Konfigurasi

`settings.json` dibuat otomatis di samping executable saat pertama kali dijalankan.

| Key | Default | Deskripsi |
| --- | --- | --- |
| `pollIntervalMs` | `2000` | Interval polling sensor (ms). |
| `alertCooldownMinutes` | `10` | Jarak menit minimum antar peringatan untuk sensor yang sama. |
| `hysteresisC` | `5` | °C nilai harus turun di bawah ambang sebelum bisa memperingatkan lagi. |
| `warnMarginC` | `10` | °C di bawah ambang saat ikon berubah kuning (warm). |
| `alertBeep` | `true` | Bunyi beep saat peringatan aktif. |
| `thresholds.default` | `85` | Ambang cadangan untuk grup yang tidak terdaftar. |
| `thresholds.cpu` | `85` | Ambang peringatan CPU (°C). |
| `thresholds.gpu` | `85` | Ambang peringatan GPU (°C). |
| `thresholds.storage` | `60` | Ambang peringatan SSD/Storage (°C). |
| `thresholds.motherboard` | `70` | Ambang peringatan motherboard (°C). |
| `thresholds.perSensor` | `{}` | Override per sensor, contoh: `{ "hw_1": 65 }`. |
| `groups.cpu / gpu / storage / motherboard / controller` | `true` | Aktif/nonaktif grup hardware. |

## Build & publish

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Hasil: `bin/Release/net9.0-windows/win-x64/publish/TempMonitor.exe` (+ `libMonoPosixHelper.dll`,
`MonoPosixHelper.dll`).

## Struktur proyek

```
Program.cs              Entry point, mutex instance tunggal, log exception global
Config/                 AppConfig + baca/tulis settings.json
Core/                   Groups, katalog sensor, mesin peringatan, mesin polling
Ui/                     Ikon tray + renderer, jendela Details, grafik history, jendela Settings
```

## Ucapan terima kasih

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) (MPL-2.0) —
  library sensor hardware yang dipakai sebagai dependency.

## Lisensi

[MIT](LICENSE)