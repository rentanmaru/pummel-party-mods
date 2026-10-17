# Pummel Party 解析メモ

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

## 主要クラス（Assembly-CSharp.dll）
| クラス | 役割 | 主なメンバー |
|---|---|---|
| `GameManager` | 全体管理（staticが多い） | `m_players`、`ForceMinigame`、`GetRandomMinigame()`（public static）、`LoadMinigame(MinigameDefinition, int)`（public static） |
| `GameBoardController` | ボード進行 | （未調査） |
| `BoardPlayer` : `BoardActor` | ボード上のプレイヤー | `StartTurn()`・`EndTurn()`（public）、`GiveItem`、`SetGoldCount`、`ApplyDamage`、`player`（private, `GamePlayer`） |
| `GamePlayer` | プレイヤー情報 | `Name`、`IsAI`、`IsLocalPlayer`、`GlobalID`、`NetOwner`、`BoardObject` |
| `MinigameDefinition` : `ScriptableObject` | ミニゲーム定義 | `minigameID`、`minigameName`、`enabled`、`minPlayers`、`maxPlayers`、`isWorkshop` |
| `GameRuleset` / `GameRulesetManager` | ルール設定 | `m_general`、`m_minigames`、`m_items`、`m_modifiers` |
| `Item` / `ItemList` | アイテム | （未調査） |
| `BoardAction` 派生 | ネット送信される操作 | `ActionHitDice`（`player_id`、`roll_number`）、`ActionStartTurn`、`ActionUseItem` など。`SerializeAction` を持つ |

主な名前空間：`ZP.Net`（独自ネットワーク層）、`ModSystem.Logic`（公式Mod Editor）、`VisualScripting`

## ネットワーク同期の分類
ボードの操作は `BoardAction` として送受信されるため、1人だけ内部ロジックを変えるとズレる可能性がある。

| 種類 | 例 | 誰が入れる必要があるか |
|---|---|---|
| 表示だけ | UI追加・情報表示・カメラ・ログ | 自分だけでOK（安全） |
| ホストが決めて送る処理 | ミニゲーム選択、ダイスの目（`ActionHitDice`で配信） | ホストだけで動く可能性あり（要検証） |
| 各自の端末で計算する処理 | 移動・ダメージ計算などのローカルロジック | 全員必須（入れないとデシンク） |

## TODO（Phase 4）
- [ ] dnSpyEx/ILSpy で `GameManager.GetRandomMinigame` の中身を読む（ホストだけで呼ばれるか）
- [ ] `ActionHitDice` を誰が作って送っているか確認
- [ ] `GameRuleset` の各グループの中身を確認
- [ ] UnityExplorer で実行中の `GameManager` を確認

## トラブル時のメモ
- `OnGUI` の表示が出ない場合：BepInEx の管理オブジェクトがシーン切り替えで消されている可能性。`BepInEx\config\BepInEx.cfg` の `[Chainloader] HideManagerGameObject = true` を試す
