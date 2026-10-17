# Pummel Party Mods

Pummel Party（Steam AppID 880940）用の BepInEx 5 コードModです。

## 収録Mod
| Mod | 内容 |
|---|---|
| [PummelHello](src/PummelHello) | 動作確認用。Modded表示、ターン・ミニゲームのログ出力（挙動は変えない） |
| [MinigameVariety](src/MinigameVariety) | **ホストだけ導入**。人数に合い、最近遊んでいないミニゲームを優先して候補に入れる。チャットの `/mv rank`・`/mv pool` や F8 キーで優先順位を確認できる |

## 開発環境の準備
1. .NET SDK（8以降）を入れる
2. Pummel Party に BepInEx 5（`BepInEx_win_x64_5.4.x.zip`）を導入し、1回起動する
3. `GamePath.user.props.example` を `GamePath.user.props` にコピーし、ゲームの場所を書き換える

## ビルド
```bash
dotnet build PummelMods.sln -c Release
```
ビルドすると DLL が `<ゲーム>\BepInEx\plugins\<Mod名>\` に自動でコピーされます。

## 配布用zipの作成
```bash
powershell -ExecutionPolicy Bypass -File tools/package.ps1 -Project MinigameVariety
```
`dist\<Mod名>-<version>.zip` ができます（Thunderstore形式）。`-Project` にMod名を指定します。

## 新しいModの追加
1. `src/<Mod名>/<Mod名>.csproj` を作る（中身は `AssemblyName`・`RootNamespace`・`Version` だけ。参照はルートの `Directory.Build.props` で共通化済み）
2. `dotnet sln PummelMods.sln add src/<Mod名>/<Mod名>.csproj`
3. `thunderstore/<Mod名>/` に manifest.json・icon.png（256x256）・README.md・CHANGELOG.md を置く

## フォルダ構成
| 場所 | 内容 |
|---|---|
| `src/` | 各Modのソースコード |
| `thunderstore/` | 配布用の manifest.json・icon.png・README など |
| `tools/` | 配布zip作成スクリプト |
| `docs/` | ゲーム解析メモ、Thunderstore申請メモ |

## 注意
- ゲームのDLLやアセットはリポジトリに含めません（再配布になるため）
- 挙動を変えるModは、フレンドとのプライベートロビーやローカルプレイで使ってください
