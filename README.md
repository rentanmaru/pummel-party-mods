# Pummel Party Mods

Pummel Party（Steam AppID 880940）用の BepInEx 5 コードModです。

BepInEx 5 code mods for Pummel Party (Steam AppID 880940).

> 非公式のファンメイドModです。Rebuilt Games とは関係ありません。
>
> This is an unofficial fan-made mod, not affiliated with Rebuilt Games.

## 収録Mod / Mods
| Mod | 内容 / Description |
|---|---|
| [MinigameVariety](thunderstore/MinigameVariety/README.md) | **ホストだけ導入**。人数に合い、最近遊んでいないミニゲームを優先して候補に入れる。チャットの `/mv rank`・`/mv pool` や F8 キーで優先順位を確認できる<br><br>**Host-only.** Prefers minigames that fit the player count and haven't been played recently. Check the priority with `/mv rank` / `/mv pool` in chat or the F8 key |
| [BananaItem](thunderstore/BananaItem/README.md) | **全員導入が必要**。新アイテム「バナナ」を追加。使ったマスに置き、2ラウンドの間、止まった他のプレイヤーに15ダメージ<br><br>**Everyone needs it.** Adds a new item, Banana. Drop it on your space; for 2 rounds the next other player who stops there takes 15 damage |
| [PummelHello](thunderstore/PummelHello/README.md) | 開発者向けの動作確認用。Modded表示、ターン・ミニゲームのログ出力（挙動は変えない）<br><br>A test plugin for developers. Shows a "Modded" label and logs turns and minigames (does not change gameplay) |

ダウンロードは [Releases](https://github.com/rentanmaru/pummel-party-mods/releases) から。

Download from [Releases](https://github.com/rentanmaru/pummel-party-mods/releases).

## 遊ぶ人向け：導入方法 / For players: Install
1. **BepInEx 5 を入れる / Install BepInEx 5**
   - [BepInEx の Releases](https://github.com/BepInEx/BepInEx/releases) から 5.4.x の `BepInEx_win_x64_5.4.x.zip` をダウンロード<br>
     Download `BepInEx_win_x64_5.4.x.zip` (5.4.x) from [BepInEx Releases](https://github.com/BepInEx/BepInEx/releases)
   - Steam のライブラリで Pummel Party を右クリック →「管理」→「ローカルファイルを閲覧」でゲームフォルダを開く<br>
     In your Steam library, right-click Pummel Party → "Manage" → "Browse local files" to open the game folder
   - zip の中身をゲームフォルダにそのまま展開し、ゲームを1回起動する<br>
     Extract the zip into the game folder as is, then start the game once
2. **Mod を入れる / Install a mod**
   - [このリポジトリの Releases](https://github.com/rentanmaru/pummel-party-mods/releases) から Mod の zip をダウンロード<br>
     Download the mod's zip from [this repository's Releases](https://github.com/rentanmaru/pummel-party-mods/releases)
   - zip の中の `BepInEx` フォルダを、ゲームフォルダに上書きコピーする<br>
     Copy the `BepInEx` folder from the zip into the game folder (overwrite)
3. **外すとき / Uninstall**
   - Mod だけ外す：`BepInEx\plugins\<Mod名>` フォルダを消す<br>
     Remove one mod: delete the `BepInEx\plugins\<ModName>` folder
   - BepInEx ごと外す：ゲームフォルダの `winhttp.dll` を消す<br>
     Remove BepInEx: delete `winhttp.dll` in the game folder

## 開発環境の準備 / Development setup
1. .NET SDK（8以降）を入れる<br>
   Install the .NET SDK (8 or later)
2. Pummel Party に BepInEx 5（`BepInEx_win_x64_5.4.x.zip`）を導入し、1回起動する<br>
   Install BepInEx 5 (`BepInEx_win_x64_5.4.x.zip`) into Pummel Party and start the game once
3. `GamePath.user.props.example` を `GamePath.user.props` にコピーし、ゲームの場所を書き換える<br>
   Copy `GamePath.user.props.example` to `GamePath.user.props` and set your game folder

## ビルド / Build
```bash
dotnet build PummelMods.sln -c Release
```
ビルドすると DLL が `<ゲーム>\BepInEx\plugins\<Mod名>\` に自動でコピーされます。

The DLL is copied to `<game>\BepInEx\plugins\<ModName>\` automatically after the build.

## 配布用zipの作成 / Packaging
```bash
powershell -ExecutionPolicy Bypass -File tools/package.ps1 -Project MinigameVariety
```
`dist\<Mod名>-<version>.zip` ができます（Thunderstore形式）。`-Project` にMod名を指定します。

This creates `dist\<ModName>-<version>.zip` (Thunderstore format). Pass the mod name to `-Project`.

## 新しいModの追加 / Adding a new mod
1. `src/<Mod名>/<Mod名>.csproj` を作る（中身は `AssemblyName`・`RootNamespace`・`Version` だけ。参照はルートの `Directory.Build.props` で共通化済み）<br>
   Create `src/<ModName>/<ModName>.csproj` with only `AssemblyName`, `RootNamespace` and `Version` (references are shared in the root `Directory.Build.props`)
2. ソリューションに追加する / Add it to the solution<br>
   `dotnet sln PummelMods.sln add src/<ModName>/<ModName>.csproj`
3. `thunderstore/<Mod名>/` に manifest.json・icon.png（256x256）・README.md・CHANGELOG.md を置く<br>
   Put manifest.json, icon.png (256x256), README.md and CHANGELOG.md in `thunderstore/<ModName>/`

## フォルダ構成 / Folders
| 場所 / Path | 内容 / Contents |
|---|---|
| `src/` | 各Modのソースコード<br>Source code of each mod |
| `thunderstore/` | 配布用の manifest.json・icon.png・README など<br>manifest.json, icon.png, README, etc. for packaging |
| `tools/` | 配布zip作成スクリプト<br>Packaging script |
| `docs/` | ゲーム解析メモ、Thunderstore申請メモ（日本語）<br>Game analysis notes and Thunderstore request notes (Japanese) |

## 注意 / Notes
- ゲームのDLLやアセットはリポジトリに含めません（再配布になるため）<br>
  The game's DLLs and assets are not included in this repository (that would be redistribution)
- 挙動を変えるModは、フレンドとのプライベートロビーやローカルプレイで使ってください<br>
  Please use gameplay-changing mods in private lobbies with friends or in local play

## ライセンス / License
[MIT](LICENSE)
