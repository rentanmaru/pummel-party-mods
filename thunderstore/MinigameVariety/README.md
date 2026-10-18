# MinigameVariety

**Host-only mod.** Prefers minigames you haven't played recently.

**ホストだけ導入すれば効くModです。** 最近遊んでいないミニゲームを優先して選びます。

## Why / なぜ必要？
At the start of each game, Pummel Party randomly picks as many minigames as there are turns. Because it picks again at random every game, you often see the same minigames as last time.

Pummel Party はゲーム開始時に、ターン数と同じ数のミニゲームをランダムに選びます。毎回ランダムに選び直すため、前のゲームと同じミニゲームがよく出ます。

## How it works / しくみ
- Records every minigame you play<br>
  遊んだミニゲームを記録します
- When the host picks the minigames for the game, it chooses in this order:<br>
  ホストがそのゲームのミニゲームを選ぶとき、次の順で選びます
  1. Minigames that fit the current player count<br>
     今の人数で遊べるもの
  2. Never played, then the least recently played<br>
     未プレイ → 遊んだのが古いもの
- Players who join your lobby don't need this mod<br>
  ロビーに参加する人は導入不要です

## Check the priority / 優先順位の確認
Results are shown only on your screen. Nothing is sent to other players.

結果は自分の画面にだけ表示され、他の人には送られません。

### Chat commands / チャットコマンド
Online lobbies only. / オンラインのロビーのみ。

| Command / コマンド | Description / 内容 |
|---|---|
| `/mv rank [page]` | Priority of all enabled minigames<br>有効なミニゲームの優先順位 |
| `/mv pool [page]` | Minigames picked for this game<br>今回のゲームの候補 |
| `/mv help` | Help<br>ヘルプ |

The vanilla chat can't be opened in local (offline) play. Use the F8 key there.

バニラのチャットはローカルプレイでは開けません。その場合は F8 キーを使ってください。

### F8 key / F8キー
Works everywhere, including local play. / ローカルプレイを含め、どこでも使えます。

- Press F8 to cycle: priority list → this game's pool → close<br>
  F8 を押すたびに「優先順位 → 今回の候補 → 閉じる」と切り替わります
- Press Esc to close<br>
  Esc ですぐ閉じます
- Scroll with the mouse wheel<br>
  マウスホイールでスクロールできます

### How to read / 見かた
| Mark / 表示 | Meaning / 意味 |
|---|---|
| `★` | Picked for this game<br>今回の候補に入っている |
| `未プレイ` | Never played<br>まだ遊んでいない |
| `N個前に遊んだ` | Played N minigames ago (1 = the most recent)<br>N個前に遊んだ（1個前 = いちばん最近） |
| `人数NG` | Doesn't fit the current player count<br>今の人数では遊べない |

## Install / 導入方法
1. Install BepInEx 5 (x64) into the Pummel Party folder.<br>
   Pummel Party のフォルダに BepInEx 5（x64）を導入します。
2. Copy the `BepInEx` folder from this zip into the Pummel Party folder.<br>
   この zip の中の `BepInEx` フォルダを、Pummel Party のフォルダに上書きコピーします。

## Config / 設定
`BepInEx/config/com.renta.minigamevariety.cfg`

| Key / 項目 | Default / 初期値 | Description / 内容 |
|---|---|---|
| `Enabled` | true | Turn the mod on/off<br>Modのオン・オフ |
| `ResetHistory` | false | Set to true to clear the history on next launch<br>true で次の起動時に記録を消す |
| `LogPool` | true | Write the minigames for the game to the log when it starts<br>ゲーム開始時に候補をログに出す |
| `ToggleKey` | F8 | Key for the priority list<br>一覧を出すキー |
| `CloseKey` | Escape | Key to close the list<br>一覧を閉じるキー |

The play history is saved to `BepInEx/config/com.renta.minigamevariety.history.txt`.

遊んだ記録は `BepInEx/config/com.renta.minigamevariety.history.txt` に保存されます。
