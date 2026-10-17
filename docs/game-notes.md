# Pummel Party 解析メモ

逆コンパイル結果は `decompiled\Assembly-CSharp\`（Git管理外）。ilspycmd 11.1 で生成。
行番号はこのバージョンのゲームでのもの（アップデートでずれる）。

## ゲームの基本情報
| 項目 | 値 |
|---|---|
| Steam AppID | 880940 |
| 開発 | Rebuilt Games |
| 実行ファイル | `PummelParty.exe` |
| データフォルダ | `PummelParty_Data` |
| Unity | 2021.3.45f2 |
| スクリプト方式 | Mono（`PummelParty_Data\Managed\Assembly-CSharp.dll`） |
| アンチチート | 見つからず |
| 公式Mod機能 | Mod Editor＋Steamワークショップ（ボード・ミニゲーム）。`StreamingAssets\InbuiltMods`、`WorkshopTemplates` |

## 主要クラス
| クラス | 役割 | 主なメンバー |
|---|---|---|
| `GameManager` | 全体管理（staticが多い） | `GetRandomMinigame()`、`SelectPossibleMinigamesThisGame()`、`LoadMinigame(MinigameDefinition, int)`、`RulesetManager`、`rand` |
| `GameBoardController` | ボード進行・アクションの実行 | `QueueAction(BoardAction)`、`RPCBoardAction`、`RPCBoardActionServer` |
| `BoardPlayer` : `BoardActor` | ボード上のプレイヤー | `StartTurn()`・`EndTurn()`、`RollDice`、`SetGoldCount`、`GiveTrophy`、`player`（private） |
| `GamePlayer` | プレイヤー情報 | `Name`、`IsAI`、`IsLocalPlayer`、`GlobalID` |
| `MinigameDefinition` : `ScriptableObject` | ミニゲーム定義 | `MinigameID`、`minigameName`、`minPlayers`、`maxPlayers`、`GetIsActive()`、`GetRandomAlternateIndex()` |
| `GameRulesetManager` | ルールセット管理 | `ActiveRuleset`（ホストなら自分の、参加者ならホストから届いたもの） |
| `RulesetGroupGeneral` | 一般ルール | `MinDiceRoll`/`MaxDiceRoll`（0〜32）、`StartingKeys`、`ChestCost`、`FriendlyFireMode` |
| `Item` : `NetBehaviour` | ボードのアイテム | `Use(int seed)` → `RPCUse` で全員に乱数シードを配る |

## ネットワークの仕組み

### アクション（ボードの操作）
`GameBoardController.QueueAction(action)`（2979行目）
1. 自分の端末のキューに積む
2. `SerializeAction` でバイト列にして送る
   - ホスト → `RPCBoardActionServer` で全員へ
   - 参加者 → `RPCBoardAction` でホストへ → ホストが全員へ中継（6108行目）

→ **アクションを作った端末の値がそのまま全員に配られる。**

### 誰が何を決めているか
| 処理 | 決める端末 | 根拠 |
|---|---|---|
| ミニゲームの候補リスト | **ホスト** | `SelectPossibleMinigamesThisGame` は `NetSystem.IsServer` 以外は即return（1714行目） |
| 次に遊ぶミニゲーム | **ホスト** | `ChooseMinigame` で `IsServer` のときだけ `GetRandomMinigame()` → `ActionLoadMinigame(ID, 派生番号)` を送る（5143〜5183行目） |
| ルールセット | **ホスト** | 参加時に `ActiveRuleset.Save()` のバイト列を送る（`NetGameServer` 1077行目） |
| ダイスの目（人間） | **操作している本人** | 自分のプレイヤーで `GameManager.rand.Next` → `ActionHitDice` を送る（2481行目） |
| ダイスの目（AI） | ホスト | `GetAIDiceRoll()`（2672行目） |
| ゴールド・トロフィー | ホスト | `GiveTrophy` は `IsServer` のみ。`gold` はネット同期変数 |
| アイテムの効果 | 使った人 → 全員 | `RPCUse(seed)` で同じ乱数シードを配って各自で計算 |

### ミニゲーム選択の流れ（バニラ）
1. ゲーム開始時：有効なミニゲームから **ターン数と同じ数だけ** ランダムに選び、候補リストにする（`SelectPossibleMinigamesThisGame`）
2. 毎ラウンド：候補リストから1つ引いて **リストから取り除く**（引き直しなし）。空になったら候補リスト全体を補充
3. 人数条件（`minPlayers`〜`maxPlayers`）に合わないものは引き直し

→ **1回のゲームの中では、もともと同じミニゲームはほぼ重複しない。**
→ ただし **ゲームをまたぐと** 毎回ランダムに選び直すので、前のゲームと同じミニゲームが出やすい。

## Modの種類と「誰が入れる必要があるか」
| Modの種類 | 例 | 必要な人 | 判断 |
|---|---|---|---|
| ミニゲームの選び方を変える | 遊んでいないミニゲームを優先、出現の重み付け | **ホストだけ** | ◎ 作りやすい。ホストが決めてIDを送るだけなので、参加者は導入不要 |
| 表示だけ | ミニゲーム履歴、情報オーバーレイ | 自分だけ | ○ 安全 |
| ルールの範囲を広げる | 初期キーを150以上にする等 | **全員** | △ ルールは番号で送られるため、未導入の人は範囲外の値を丸めてズレる。しかもバニラのルールセット画面で調整できる項目が多い |
| ダイスの目を変える | — | — | ✕ 本人の端末で決まるので、変えるとチートになる。作らない |

## Phase 5 の候補（解析結果を踏まえた順位）
1. **遊んでいないミニゲームを優先（ホストだけ導入）** → `src/MinigameVariety` で実装
2. **ミニゲーム履歴の表示（自分だけ）** … 今回のゲームで遊んだミニゲームを一覧表示
3. ルール拡張 … 全員導入が必要なので後回し

## MinigameVariety の設計
| パッチ | 種類 | やること |
|---|---|---|
| `GameManager.SelectPossibleMinigamesThisGame` | Postfix | バニラの候補を上書き。①今の人数で遊べる → ②最後に遊んだのが古い（未プレイ最優先）順に `TurnCount` 件選ぶ。3つのリスト（MasterList・IDs・抽選袋）をそろえ、`HasPossibleMinigamesChanged = true` |
| `GameManager.LoadMinigame` | Prefix | 遊んだミニゲームを記録（ホスト・参加者どちらでも） |
| `MultiplayerLobbyController.OnNetInitialize` | Postfix | ロビーが開くたびに選び直す（2戦目にも最新の記録を反映） |
| `MultiplayerLobbyController.UpdatePlayerData` | Postfix | ロビーで人数が変わったら選び直す（`CurState == MainMenu` のときだけ） |
| `GameBoardController.StartGame` | Prefix | 実際に使う候補をログに1回だけ出す（ホストのみ） |

- 候補選びはロビー内で行われる（サーバー作成時・ターン数／モード／ルールセット変更時）。ゲーム開始時には呼ばれない
- **ワークショップのミニゲームを1つ読み込むたびにもバニラが候補選びを呼ぶ**（`WorkshopModInstaller` 514行目）。71件有効な環境では起動直後に約80回呼ばれた → 毎回のログは Debug レベルにした
- `MinigameDefinition.minigameID`（ushort）はワークショップ製だと0。正しいIDは `MinigameID` プロパティ（ワークショップ製なら `publishedFileId`）
- `GameManager.GetMinigameById` は見つからないと警告ログを出す。毎秒呼ぶ処理では `minigame_map` を直接引く

### 優先順位の確認（チャットコマンド・F8）
| 場所 | やること |
|---|---|
| `ChatBox.OnInputFieldSubmit` | Prefix。`/mv` で始まる入力を横取りし、`AddMessage` で自分の画面にだけ結果を出して `CloseChatBox`。`false` を返して送信しない |
| `PriorityOverlay`（自前の MonoBehaviour） | F8 で「優先順位 → 今回の候補 → 閉じる」、Esc ですぐ閉じる。IMGUI のリッチテキストで表示。ボタンは置かない |

- 優先順位の計算は `MinigamePriority.Sort` に一本化（候補選び・チャット・F8で共通）
- 参加者も `GameManager.PossibleMinigameIDs` をホストから受け取っているので、`/mv pool` は参加者でも使える

## チャットの仕組み（ChatBox）
- 送信：`OnInputFieldSubmit` → `NetSystem.SendLobbyChatMessage(msg)`（Steamロビーのチャット）＋自分の画面に `AddMessage`
- 受信：`NetSystem.ChatMessageRecieved` → `AddMessage`
- **ローカルプレイ（`NetSystem.IsServer && NetGameServer.IsLocal`）ではチャット欄が開かない**（169・193行目）。`IsLocal` はサーバー作成の処理でも使われるので、Modで書き換えるのは危険
- 表示は最大30件。TextMeshPro なので `<color>`・`<b>`・`<noparse>` が使える
- ボードではマウスの左クリックがダイスを振る操作（`GameBoardController` 2818行目）。IMGUI のボタンを置くとクリックが漏れる恐れがある
- 人数で絞る理由：人数に合わないミニゲームは抽選で飛ばされるので、未プレイを優先すると候補が詰まり、同じミニゲームが繰り返し出てしまう
- `GameManager.PlayerCount` はロビーのスロット数（AI込み）。`UpdatePlayerData` で毎回作り直される
- 記録ファイル：`BepInEx\config\com.renta.minigamevariety.history.txt`（`ID<TAB>順番`）

## TODO
- [x] `HasPossibleMinigamesChanged` を受けて参加者へ候補リストが再送される流れを確認
  - ロビーで `MultiplayerLobbyController`（545行目）がフラグを見て `RPCSetPossibleMinigameIDs` を送る → 参加者は `SetClientPossibleMinigames` で受け取る
  - つまり候補リストをホストで書き換えてフラグを立てれば、バニラの処理で参加者にも届く
- [ ] `GameManager.LocalPlayedMinigameList`（遊んだミニゲームの一覧？）の使われ方を確認 → 履歴表示に使えるかも
- [ ] UnityExplorer で実行中の値を確認（必要になったら）

## トラブル時のメモ
- `OnGUI` の表示が出ない場合：BepInEx の管理オブジェクトがシーン切り替えで消されている可能性。`BepInEx\config\BepInEx.cfg` の `[Chainloader] HideManagerGameObject = true` を試す
