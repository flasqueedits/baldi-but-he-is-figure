# Render.com üzerinde çoklu oyuncu sunucusu

Render **UDP portlarını desteklemediği için** yerel KCP transport'u orada çalışmaz.
Bu yüzden sunucu **WebSocket (ws/wss)** ile dinler, istemci `wss://...` adresiyle bağlanır.
Render proxy TLS'i sonlandırır; konteyner içinde sunucu düz `ws` kullanır.

## 1) Linux sunucu build'i (HAZIR)

Linux Build Support modülü kuruldu ve sunucu build'i alındı:

```
Builds/LinuxServer/FigureServer.x86_64      (launcher)
Builds/LinuxServer/UnityPlayer.so
Builds/LinuxServer/FigureServer_Data/...    (toplam ~206 MB)
```

Yeniden build gerekirse: **Tools → Figure Multiplayer → Build Linux Dedicated Server**

## 2) Projeyi GitHub'a yükle

Klasör şu an git repo değil. Proje kökünde:

```bash
git init
git add .
git commit -m "figure coop server"
git remote add origin <GitHub repo URL>
git push -u origin main
```

Kök dizinde şu dosyaların bulunduğundan emin ol:
`Dockerfile`, `render.yaml`, `.dockerignore`, `Builds/LinuxServer/FigureServer.x86_64`.

> `Builds/` klasörü `.gitignore` ile dışlanmışsa eklemeyi unutma.

## 3) Render'da servisi kur

1. https://dashboard.render.com → **New → Web Service**
2. GitHub repo'sunu seç
3. Runtime otomatik **Docker** olmalı (`render.yaml` varsa otomatik okunur)
4. Plan: **Starter ($7/ay)** — ücretsiz plan 15 dk hareketsizlikte uyur, oyun sunucusu için uygun değil
5. Region: istediğin (render.yaml'da `frankfurt` yazılı)
6. Environment variable: `PORT = 10000` (render.yaml'da tanımlı)
7. **Create Web Service**

Deploy loglarında şunu görmelisin:

```
[Server] Dedicated server listening on port 10000
```

## 4) Client'tan bağlan

1. Oyunu aç → MainMenu → FIGURE CO-OP paneli
2. IP alanına Render URL'sini yapıştır:
   ```
   wss://figure-coop-server.onrender.com
   ```
   (Kendi servis adın farklıysa onu yaz)
3. **KATIL (CLIENT)** butonuna bas.

Host açmana gerek yok — Render sunucusu zaten ayakta.
Yerel LAN testinde olduğu gibi `127.0.0.1` + Host modu da çalışmaya devam eder.

## 5) Kullanım tuşları

- **Enter** — sohbet gönder
- **P** — konum ping'i
- **Tab** — oyuncu listesini aç/kapat

## Sorun giderme

- **Bağlanamıyorum:** Render loglarına bak; `PORT` env değeri 10000 mi?
- **wss hatası:** URL'de `wss://` olduğundan ve servisin ayakta olduğundan emin ol (ücretsiz plan uyumuş olabilir).
- **Windows'ta test ediyorum:** Önce `127.0.0.1` ile Host/Client testi yap; bu KCP ile çalışır.
- **Build alınamıyor:** Linux Build Support modülü kurulu mu? (adım 1)

## Dosyalar

```
Dockerfile                  Unity Linux server'ı Ubuntu içinde çalıştırır
render.yaml                 Render Web Service tanımı (PORT=10000)
.dockerignore               Docker context'i küçültür
Assets/Editor/DedicatedServerBuilder.cs   Linux build menü komutu
Assets/Scripts/Multiplayer/BaldiNetworkManager.cs  StartDedicatedServer / StartOnlineClient
Assets/Scripts/Multiplayer/MultiplayerBootstrap.cs -server argümanını yakalar
```
