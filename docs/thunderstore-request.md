# Thunderstore 申請メモ

## 申請の条件（Thunderstore Wiki「Adding a new game」より）
- すでにMod開発者がいること（"pre-existing mod developer interest"）
- できればアップロードできるModが用意できていること
- 申請場所：Thunderstore Discord（https://discord.thunderstore.io/）の `#game-requests`。投稿ガイドライン（New Post ボタンの横）に従う

## 申請前チェックリスト
- [ ] GitHubで公開しているModが1つ以上ある
- [ ] 実際に使っている人・他のMod開発者の関心がある（Steamディスカッション等で告知）
- [ ] 配布zipがThunderstore形式（manifest.json / icon.png / README.md）になっている
- [ ] Modのリンク一覧を用意した

## 申請時に出す情報
| 項目 | 値 |
|---|---|
| Game | Pummel Party |
| Developer | Rebuilt Games |
| Steam AppID | 880940 |
| Store page | https://store.steampowered.com/app/880940/Pummel_Party/ |
| Executable | `PummelParty.exe` |
| Data folder | `PummelParty_Data` |
| Engine | Unity 2021.3.45f2 (Mono) |
| Mod loader | BepInEx 5 (x64) |
| Existing mods | （GitHubのリンクをここに） |

## 投稿文の下書き（英語）
> **Pummel Party** (Steam AppID 880940)
> Unity 2021.3 Mono game. BepInEx 5 + Harmony works out of the box.
> Existing BepInEx mods: (links)
> We'd like a Thunderstore community so players can install mods with the mod managers.

## 承認後の流れ（Thunderstore側の作業）
1. `ecosystem-schema` にゲーム定義を追加
2. コミュニティページ作成（背景画像などはThunderstoreチームが設定）
3. Thunderstore Mod Manager／r2modman の更新版をリリース

## ページができたら
- そのコミュニティ用の BepInExPack を manifest.json の `dependencies` に追加する
- `website_url` に GitHub のURLを入れる
- `tools\package.ps1` で作った zip をアップロードする
