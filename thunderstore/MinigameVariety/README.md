# MinigameVariety

**Host-only mod.** Prefers minigames you haven't played recently.
**ホストだけ導入すれば効くModです。** 最近遊んでいないミニゲームを優先して選びます。

## Why / なぜ必要？
At the start of each game, Pummel Party randomly picks as many minigames as there are turns.
Because it picks again at random every game, you often see the same minigames as last time.
Pummel Party はゲーム開始時に、ターン数と同じ数のミニゲームをランダムに選びます。
毎回ランダムに選び直すため、前のゲームと同じミニゲームがよく出ます。

## How it works / しくみ
- Records every minigame you play / 遊んだミニゲームを記録します
- When the host picks the minigames for the game, it chooses in this order:
  ホストがそのゲームのミニゲームを選ぶとき、次の順で選びます
  1. Minigames that fit the current player count / 今の人数で遊べるもの
  2. Never played, then the least recently played / 未プレイ → 遊んだのが古いもの
- Players who join your lobby don't need this mod / ロビーに参加する人は導入不要です

## Check the priority / 優先順位の確認
Results are shown only on your screen. Nothing is sent to other players.
結果は自分の画面にだけ表示され、他の人には送られません。

### Chat commands / チャットコマンド（online lobbies only / オンラインのロビーのみ）
| Command | Description |
|---|---|
| `/mv rank [page]` | Priority of all enabled minigames / 有効なミニゲームの優先順位 |
| `/mv pool [page]` | Minigames picked for this game / 今回のゲームの候補 |
| `/mv help` | Help / ヘルプ |

The vanilla chat can't be opened in local (offline) play. Use the F8 key there.
バニラのチャットはローカルプレイでは開けません。その場合は F8 キーを使ってください。

### F8 key / F8キー（works everywhere / どこでも使える）
Press F8 to cycle: priority list → this game's pool → close. Press Esc to close. Scroll with the mouse wheel.
F8 を押すたびに「優先順位 → 今回の候補 → 閉じる」と切り替わります。Esc ですぐ閉じます。マウスホイールでスクロールできます。

### How to read / 見かた
- `★` = picked for this game / 今回の候補に入っている
- `未プレイ` = never played / まだ遊んでいない
- `N個前に遊んだ` = played N minigames ago / N個前に遊んだ
- `人数NG` = doesn't fit the current player count / 今の人数では遊べない

## Install / 導入方法
1. Install BepInEx 5 (x64) into the Pummel Party folder.
   Pummel Party のフォルダに BepInEx 5（x64）を導入します。
2. Copy the `BepInEx` folder from this zip into the Pummel Party folder.
   この zip の中の `BepInEx` フォルダを、Pummel Party のフォルダに上書きコピーします。

## Config / 設定
`BepInEx/config/com.renta.minigamevariety.cfg`
| Key | Default | Description |
|---|---|---|
| `Enabled` | true | Turn the mod on/off / Modのオン・オフ |
| `ResetHistory` | false | Set to true to clear the history on next launch / true で次の起動時に記録を消す |
| `LogPool` | true | Write the minigames for the game to the log when it starts / ゲーム開始時に候補をログに出す |
| `ToggleKey` | F8 | Key for the priority list / 一覧を出すキー |
| `CloseKey` | Escape | Key to close the list / 一覧を閉じるキー |

The play history is saved to `BepInEx/config/com.renta.minigamevariety.history.txt`.
遊んだ記録は `BepInEx/config/com.renta.minigamevariety.history.txt` に保存されます。
