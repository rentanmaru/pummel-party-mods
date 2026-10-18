using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using ZP.Net;

namespace BananaItem
{
    // ゲームの準備ができたらアイテムを登録し、テスト用キーを受け付ける
    internal class BananaRunner : MonoBehaviour
    {
        private bool _registerFailed;

        private void Update()
        {
            if (!BananaDefinition.IsRegistered && !_registerFailed)
            {
                try
                {
                    BananaDefinition.TryRegister();
                }
                catch (Exception e)
                {
                    // 登録で例外が出たら、毎フレーム繰り返さないように止める
                    _registerFailed = true;
                    Plugin.Log.LogError($"バナナの登録に失敗しました: {e}");
                }
            }

            if (!BananaDefinition.IsRegistered)
            {
                return;
            }

            // Ctrl+F10 を押すと F10 の判定も通るので、全マスの方を先に見る
            if (WasPressed(Plugin.DebugFillKey.Value))
            {
                FillBoardForTesting();
            }
            else if (WasPressed(Plugin.DebugGiveKey.Value))
            {
                GiveBananaForTesting();
            }
        }

        // テスト用：ローカルプレイのときだけ、自分（キーボードの人）にバナナを1個渡す
        private static void GiveBananaForTesting()
        {
            GamePlayer player = GetTestPlayer();
            if (player == null)
            {
                return;
            }

            if (Array.IndexOf(GameManager.ItemList.enabledItems, BananaDefinition.Details) < 0)
            {
                Plugin.Log.LogInfo("このゲームではバナナが無効になっています（アイテム設定を確認してください）");
                return;
            }

            player.BoardObject.GiveItem(BananaDefinition.Details.itemIndex);
            Plugin.Log.LogInfo($"テスト用にバナナを渡しました: {player.Name}");
        }

        // テスト用：ローカルプレイのときだけ、全マスに自分のバナナを置く（AIが止まったときの確認用）
        private static void FillBoardForTesting()
        {
            GamePlayer player = GetTestPlayer();
            if (player == null || GameManager.Board.BoardNodes == null)
            {
                return;
            }

            int count = 0;
            foreach (BoardNode node in GameManager.Board.BoardNodes)
            {
                if (node != null)
                {
                    BananaField.Place(node, player, quiet: true);
                    count++;
                }
            }
            Plugin.Log.LogInfo($"テスト用に {count} マスにバナナを置きました（持ち主 {player.Name}）");
        }

        // テスト機能を使える状態なら、キーボードで遊んでいる人を返す
        private static GamePlayer GetTestPlayer()
        {
            if (!(NetSystem.IsServer && NetGameServer.IsLocal))
            {
                Plugin.Log.LogInfo("テスト機能はローカルプレイでだけ使えます");
                return null;
            }

            if (GameManager.Board == null || !GameManager.Board.IsInitialized)
            {
                return null;
            }

            for (int i = 0; i < GameManager.PlayerCount; i++)
            {
                GamePlayer player = GameManager.GetPlayerAt(i);
                if (player.IsLocalPlayer && !player.IsAI && player.BoardObject != null)
                {
                    return player;
                }
            }
            return null;
        }

        // キーが押された瞬間か（ほかのキーが押されていても反応する）
        private static bool WasPressed(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None || !UnityInput.Current.GetKeyDown(shortcut.MainKey))
            {
                return false;
            }

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!UnityInput.Current.GetKey(modifier))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
