# Kendi multiplayer GUI'ni yapmak

Ağ kodu hazır. Sen sadece arayüzü yapacaksın. Aşağıdaki adımları izle.

## 1) Benim hazır GUI'mi kapat

Kendi menünü oluşturan script'in `Awake()` veya `Start()` içine şunları yaz:

```csharp
MultiplayerMenu.UseRuntimeMenu = false;   // ana menüdeki hazır Host/Join panelini kapatır
NetworkHud.UseRuntimeHud = false;         // oyundaki hazır oyuncu listesini kapatır
```

Bunu MainMenu sahnesinde bir objeye koyduğun script'te yaparsan, School sahnesine
geçildiğinde `NetworkHud.UseRuntimeHud = false` değeri korunur (statik değişken).

> Şimdilik kapatma, kendi GUI'ni test ederken ikisi üst üste görünür. Tasarım bitince kapat.

## 2) Ana menü butonları

MainMenu sahnesine Canvas + butonlar + InputField (IP) + Toggle (Endless) ekle.
Butonların `OnClick` alanına kendi script'inden şunları çağır:

```csharp
// Host
MultiplayerActions.Host(endlessToggle.isOn);

// Client
MultiplayerActions.Join(ipInput.text, endlessToggle.isOn);

// Tek oyunculu
MultiplayerActions.SinglePlayer(endlessToggle.isOn);

// Menüye dön (kopma panelindeki buton da bunu çağırsın)
MultiplayerActions.ReturnToMenu();
```

Bu metodlar modu ayarlar, `PlayerPrefs.CurrentMode` değerini yazar ve `School`
sahnesini yükler. IP boş bırakılırsa `127.0.0.1` kullanılır.

> IP alanını boş bırakma: `ipInput.text` boşsa otomatik `127.0.0.1` olur.

## 3) Oyun içi HUD verileri

School sahnesine kendi HUD'unu koy. Örnek script:

```csharp
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MyMultiplayerHud : MonoBehaviour
{
    public Text playerListText;

    private void Update()
    {
        int count = NetworkServer.active
            ? NetworkServer.connections.Count
            : (NetworkClient.active ? 1 : 0);

        string s = "Bağlı oyuncu: " + count + "\n";
        foreach (NetworkPlayer p in FindObjectsOfType<NetworkPlayer>())
        {
            s += "#" + p.playerNumber + " " + p.playerName;
            if (p.isLocalPlayer) s += " (sen)";
            if (p.caught) s += " [yakalandı]";
            s += "\n";
        }
        playerListText.text = s;
    }
}
```

## 4) Yakalanma olayı

Kendi yakalama ekranın için:

```csharp
private void OnEnable()  { NetworkPlayer.Caught += OnCaught; }
private void OnDisable() { NetworkPlayer.Caught -= OnCaught; }

private void OnCaught(NetworkPlayer player)
{
    if (player.isLocalPlayer) yakalamaPaneli.SetActive(true);
}
```

## 5) Bağlantı kopması

`NetworkClient.active` değerini her frame kontrol et; `true`'dan `false`'a düştüğü
anda kopma panelini aç. Paneldeki buton `MultiplayerActions.ReturnToMenu()` çağırsın.

## 6) Dokunmaman gereken yerler

- School sahnesindeki **NetworkManager** objesi (Mirror + KCP + port 7777)
- `Assets/Multiplayer/NetworkPlayer.prefab` içindeki `NetworkPlayer`,
  `NetworkCharacterAnimator`, `NetworkIdentity` bileşenleri
- `NetworkBaldi` ve defterlerdeki `NetworkNotebook` bileşenleri

Bunları silersen ağ çalışmaz. GUI tamamen bağımsız; istediğin gibi tasarla.

## 7) Test

1. Unity'de Play → MainMenu → kendi Host butonun
2. İkinci bir Unity penceresi/build → kendi Join butonun → `127.0.0.1`
3. İkinci oyuncu görünüyorsa, yürüyor/eğiliyorsa ve liste doğruysa tamamdır.

Takıldığın yer olursa exact hata mesajını veya ekran görüntüsünü yaz, ben düzelteceğim.
