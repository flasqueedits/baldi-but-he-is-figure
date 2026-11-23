# Figure Co-op Multiplayer (Mirror)

Bu proje artık **Mirror** tabanlı co-op prototipini içerir. Tüm oyuncular öğrencidir;
Baldi / Figure sunucuda yapay zekâ olarak çalışır ve en yakın öğrenciyi kovalar.

## 1. Kurulum (Unity Editor)

**Kurulum zaten uygulandı.** `School.unity` sahnesi ve oyuncu prefab'ı hazır;
Unity'yi açıp MainMenu'dan Host/Join yapabilirsin. Unity "sahne dışarıdan değiştirildi"
derse sahneyi yeniden yüklemen yeterli.

İstersen kurulumu yeniden çalıştırabilirsin (tekrar çalıştırmak güvenlidir):
**Tools → Figure Multiplayer → Co-op Prototipini Kur**

- Mirror zaten `Assets/Mirror` altına gömüldü; ek paket kurmana gerek yok.
- Bu araç `Assets/Scene/School.unity` dosyasını açar; mevcut oyuncudan
  `Assets/Multiplayer/NetworkPlayer.prefab` oluşturur, sahneye `NetworkManager`
  (Mirror + KCP, UDP port **7777**) ekler, Baldi'ye ağ bileşenini ekler ve
  sahnedeki her deftere benzersiz ID verir. Sahneyi otomatik kaydeder.

> Kurulum aracını Play modundayken çalıştıramazsın.

## 2. Oynama

- **MainMenu** sahnesinde `FIGURE - CO-OP MULTIPLAYER` paneli otomatik görünür:
  Host/Join butonları, IP alanı, Endless toggle'ı, durum mesajı ve yardım metni.
- **OYUN KUR (HOST)**: sen hem sunucu hem oyuncusun.
- **KATIL (CLIENT)**: IP alanına host'un IP'sini yaz (aynı bilgisayarda `127.0.0.1`).
- **TEK OYUNCUYA DÖN**: paneli kapatır, orijinal tek oyunculu akış çalışır.
- Oyun içinde sol üstte **oyuncu listesi** gösterilir; **Tab** ile açılıp kapanır.
  Yakalanınca uyarı, host kopunca "ana menüye dön" butonlu uyarı çıkar.

Aynı sürümü çalıştıran ikinci bir Unity penceresi / build daha açıp
birinci bilgisayarda Host, ikincisinde Client yaparak test edebilirsin.

## 2.1 Karakter animasyonları

`Tools → Figure Multiplayer → Karakter Animasyonlarını Kur` aracı (kuruluma dahil,
tekrar çalıştırılabilir):

- `Baldi Student Model 2.fbx` içindeki kemiklerle **yürüme, koşma, eğilme (crouch)**
  ve durağan (idle) klipleri üretir
- `Assets/Multiplayer/StudentAnimator.controller` oluşturur
  (parametreler: `Speed`, `Run`, `Crouch`)
- Modeli kameranın altından oyuncu köküne taşır (uzak oyuncuda kamera kapalı
  olduğu için model sabit kalmasın diye)
- Fazla kamera kopyalarını kapatır
- **Yerel oyuncunun modelini gizler** (birinci şahıs görünümünü bozmasın diye),
  diğer oyuncuların modeli görünür ve hareket/koşma/eğilme halleri senkronize olur

FBX'in kendi animasyon klibi olmadığı için klipler kemikler üzerinde prosedürel
olarak üretilir. Daha gerçekçi bir animasyon istersen klipleri Animator penceresinde
değiştirebilirsin.

## 3. Şu anda senkronize olanlar

- Oyuncu pozisyonu / rotasyonu (client-authoritative, 20 Hz)
- Oyuncu numarası ve ismi
- Baldi / Figure pozisyonu, rotasyonu, kükreme / tokat / çığlık sesleri
- Baldi'nin ses algılama (`Hear`) sistemi: istemciden sunucuya iletilir
- Baldi'nin en yakın öğrenciyi seçmesi ve yakalama olayı
- Defterlerin globally toplanması (bir oyuncu alınca herkeste kaybolur,
  sadece toplayan oyuncu defter sayısını alır ve matematik sorusunu görür)
- Karakter görünümü: sadece diğer oyuncular görünür; yürüme/koşma/eğilme durumları senkronize
- Kapılar (açma/kilitleme/sessizleştirme), eşya toplama, BSODA görsel efekti
- Diğer NPC'ler (Playtime, Gotta Sweep, Principal, 1st Prize, Bully) pozisyon senkronu
- Uzaktaki karakterlerin üstünde isim etiketi
- Sohbet (Enter ile gönder) ve konum ping'i (P tuşu)
- Defter sayısı ortak: biri defter alınca herkesin skoru ve final modu güncellenir

Kurulum aracı: **Tools → Figure Multiplayer → Ek Sistemleri Kur** (yeni sistemler için).

## 4. Bilinen sınırlar (prototip)

- Kapılar, eşyalar, diğer NPC'ler (Playtime, Gotta Sweep, Principal vb.) henüz ağ senkronize değil.
- Yakalanan oyuncu oyunu terk etmez; ekranda "Figure seni yakaladı" uyarısı görünür ve
  kontrolü kapanır. Diğer oyuncular oynamaya devam eder.
- Dedicated server (başsız sunucu) build'i henüz eklenmedi; Host modu sunucu görevi görür.
- Puan / defter sayısı oyuncular arasında paylaşılmaz (her oyuncunun kendi HUD'u vardır).

## 5. Windows + Linux

Mirror ve KCP transport çok platformludur. Linux build alırken:
- Aynı Unity sürümü ve aynı proje kullanılmalı
- UDP **7777** portu açık olmalı
- Host ile client aynı sürüm olmalı (protokol uyuşmazlığı olmasın)

## 6. Dosyalar

```
Assets/Mirror/                         Mirror 96.11.6 kaynakları
Assets/Scripts/Multiplayer/
  BaldiNetworkManager.cs               Host/client başlatma, oyuncu numaraları
  MultiplayerSession.cs                Menü → sahne geçiş durumu
  MultiplayerMenu.cs                   Runtime Host/Join paneli
  MultiplayerBootstrap.cs              Sahne referanslarını yeni oyuncuya bağlar
  NetworkPlayer.cs                     Oyuncu hareket senkronu
  NetworkBaldi.cs                      Sunucu tarafı Figure AI + senkron
  NetworkGameState.cs / NetworkNotebook.cs   Paylaşımlı defter durumu
  NetworkHud.cs                        Bağlantı / yakalanma arayüzü
Assets/Editor/MultiplayerSceneSetup.cs  Tek tıkla sahne kurulum aracı
```

Değiştirilen mevcut dosyalar: `PlayerScript.cs`, `CameraScript.cs`,
`BaldiScript.cs`, `NotebookScript.cs` (hepsi tek oyunculu modu koruyacak şekilde).
