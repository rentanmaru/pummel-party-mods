# PummelHello

A test plugin for Pummel Party modding with BepInEx 5.
Pummel Party の BepInEx Mod 制作用テストプラグインです。

## Features / 機能
- Shows a "Modded" label in the top-left corner (can be turned off in the config)
  画面左上に「Modded」と表示します（設定でオフにできます）
- Logs turn start/end and loaded minigames to the BepInEx log
  ターン開始・終了と読み込まれたミニゲームを BepInEx のログに出します

This plugin does not change gameplay. / ゲームの挙動は変えません。

## Install / 導入方法
1. Install BepInEx 5 (x64) into the Pummel Party folder.
   Pummel Party のフォルダに BepInEx 5（x64）を導入します。
2. Copy the `BepInEx` folder from this zip into the Pummel Party folder.
   この zip の中の `BepInEx` フォルダを、Pummel Party のフォルダに上書きコピーします。
3. Start the game. / ゲームを起動します。

## Config / 設定
`BepInEx/config/com.renta.pummelhello.cfg`
| Key / 項目 | Default / 初期値 | Description / 内容 |
|---|---|---|
| `ShowModdedLabel` | true | Show the "Modded" label in the top-left corner / 画面左上に「Modded」と表示する |
