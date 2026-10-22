# BananaItem

Adds a new board item: **Banana**.

ボードに新しいアイテム「**バナナ**」を追加します。

> **Everyone in the lobby needs this mod.** If someone doesn't have it, the banana is turned off automatically.
>
> **ロビーの全員が導入する必要があります。** 入れていない人がいるときは、バナナは自動で無効になります。

## How it works / しくみ
- Use it to drop a banana on the space you are standing on<br>
  使うと、今いるマスにバナナを置きます
- For **2 rounds**, the next other player who **passes over or stops on** that space takes **7-9 damage** (random)<br>
  **2ラウンド**の間、そのマスを**通過したり止まったりした**他のプレイヤーは**7〜9ダメージ**（ランダム）を受けます
- Stopping on it also makes you slip. Passing over it keeps you moving<br>
  止まったときは転びます。通過したときは、そのまま進みます
- If passing over it would knock you out, the damage is dealt when you stop<br>
  通過のダメージで倒れてしまう場合は、止まったときにダメージを受けます
- The banana disappears after it hits someone. The player who dropped it is safe<br>
  踏まれたバナナは消えます。置いた本人は踏みません
- In team mode, teammates are safe when board-item friendly fire is off<br>
  チーム戦では、ボードアイテムのフレンドリーファイアが無効なら味方は踏みません
- You can turn the banana on/off in the item settings of the ruleset<br>
  ルールセットのアイテム設定で、オン・オフできます

## How to get one / 手に入れ方
| Where / 場所 | Rate / 確率 |
|---|---|
| Item spaces<br>アイテムマス | Same base rate as other items (about 1 in the number of enabled items). Lower once you have picked one up<br>他のアイテムと同じ基本確率（有効なアイテム数分の1くらい）。一度もらうと出にくくなる |
| Minigame rewards, 2nd/3rd place<br>ミニゲームの報酬（2位・3位） | About 12-17%. 1st place keeps its usual strong items<br>約12〜17%。1位の報酬はいつもの強いアイテムのまま |
| Team mode rewards<br>チーム戦の報酬 | 3rd team's first reward and 2nd team's second reward<br>3位チームの1つ目、2位チームの2つ目の報酬 |

## Install / 導入方法
1. Install BepInEx 5 (x64) into the Pummel Party folder.<br>
   Pummel Party のフォルダに BepInEx 5（x64）を導入します。
2. Copy the `BepInEx` folder from this zip into the Pummel Party folder.<br>
   この zip の中の `BepInEx` フォルダを、Pummel Party のフォルダに上書きコピーします。
3. **Everyone who plays with you must do the same.**<br>
   **一緒に遊ぶ人も全員、同じように導入してください。**

## Online play / オンラインでの動作
- Each player with the mod tells the lobby which version they have<br>
  導入している人は、自分のバージョンをロビーに知らせます
- If anyone is missing the mod (or has a different version), the host turns the banana off and shows a message in the lobby<br>
  1人でも未導入（またはバージョン違い）なら、ホストがバナナを無効にし、ロビーに表示します

## Testing keys / テスト用キー
Local play only. / ローカルプレイのみ。

| Key / キー | Action / 動作 |
|---|---|
| `F10` | Get a banana<br>バナナを1個もらう |
| `Ctrl+F10` | Put bananas on every space<br>全マスにバナナを置く |
| `Shift+F10` | Toggle: every minigame item reward is a banana<br>切り替え：ミニゲームのアイテム報酬を全部バナナにする |

## Config / 設定
`BepInEx/config/com.renta.bananaitem.cfg`

| Key / 項目 | Default / 初期値 | Description / 内容 |
|---|---|---|
| `DebugGiveKey` | F10 | Testing: get a banana (local play only)<br>テスト用：バナナを1個もらう（ローカルのみ） |
| `DebugFillKey` | Ctrl+F10 | Testing: put bananas on every space (local play only)<br>テスト用：全マスにバナナを置く（ローカルのみ） |
| `DebugRewardKey` | Shift+F10 | Testing: toggle banana-only minigame rewards (local play only)<br>テスト用：ミニゲーム報酬をバナナだけにする切り替え（ローカルのみ） |

Damage (7-9) and duration (2 rounds) can't be changed, so that everyone has the same values. The random damage is decided from each banana's shared seed, so every player sees the same number.

ダメージ（7〜9）と持続（2ラウンド）は、全員で同じ値になるよう変更できません。ランダムなダメージも、バナナごとに全員で共有しているシードから決めるので、全員で同じ値になります。

## Known limitations / 既知の制限
- Bananas on the board are lost when a game is resumed or a player reconnects<br>
  中断したゲームの再開や、途中の再接続では、置いてあるバナナは消えます
- Please use it in private lobbies with friends or in local play<br>
  フレンドとのプライベートロビーやローカルプレイで使ってください
