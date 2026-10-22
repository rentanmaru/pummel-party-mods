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
        private const float KeyCooldownSeconds = 0.6f;
        private const float MaxReleaseWaitSeconds = 1.5f;

        private bool _registerFailed;
        private GUIStyle _testStyle;
        private float _nextKeyTime;
        private bool _waitForRelease;
        private float _releaseDeadline;

        // テストモードがオンのときは、画面にはっきり出す（ログだけだと状態が分からないため）
        private void OnGUI()
        {
            if (!Patches.LootTableChooseItemIndexPatch.ForceBananaForTesting || !(NetSystem.IsServer && NetGameServer.IsLocal))
            {
                return;
            }
            if (_testStyle == null)
            {
                _testStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _testStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            }
            GUI.Label(new Rect(10f, 58f, 700f, 26f), "テスト中：ミニゲームのアイテム報酬は全部バナナ（Shift+F10 で解除） / Banana test: rewards = banana", _testStyle);
        }

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

            // 押しっぱなしで何回も反応しないよう、反応したらキーを離すまで（かつ少しの間）受け付けない
            // ただし、ゲームがキーを「押されたまま」と判定し続けることがあるので、一定時間で必ず解除する
            if (_waitForRelease)
            {
                if (IsAnyTestKeyHeld() && Time.unscaledTime < _releaseDeadline)
                {
                    return;
                }
                _waitForRelease = false;
            }
            if (Time.unscaledTime < _nextKeyTime)
            {
                return;
            }

            // Ctrl+F10・Shift+F10 を押すと F10 の判定も通るので、修飾キー付きの方を先に見る
            bool handled = true;
            if (WasPressed(Plugin.DebugFillKey.Value))
            {
                FillBoardForTesting();
            }
            else if (WasPressed(Plugin.DebugRewardKey.Value))
            {
                ToggleRewardTest();
            }
            else if (WasPressed(Plugin.DebugGiveKey.Value))
            {
                GiveBananaForTesting();
            }
            else
            {
                handled = false;
            }

            if (handled)
            {
                _nextKeyTime = Time.unscaledTime + KeyCooldownSeconds;
                _waitForRelease = true;
                _releaseDeadline = Time.unscaledTime + MaxReleaseWaitSeconds;
            }
        }

        private static bool IsAnyTestKeyHeld()
        {
            return UnityInput.Current.GetKey(Plugin.DebugGiveKey.Value.MainKey)
                || UnityInput.Current.GetKey(Plugin.DebugFillKey.Value.MainKey)
                || UnityInput.Current.GetKey(Plugin.DebugRewardKey.Value.MainKey);
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
                    // テストはローカルプレイだけなので、シードはマスIDとラウンドから作る
                    BananaField.Place(node, player, node.NodeID * 7919 + GameManager.Board.CurTurnNum, quiet: true);
                    count++;
                }
            }
            Plugin.Log.LogInfo($"テスト用に {count} マスにバナナを置きました（持ち主 {player.Name}）");
        }

        // テスト用：ミニゲームのアイテム報酬を全部バナナにするかを切り替える（ローカルプレイのみ）
        private static void ToggleRewardTest()
        {
            if (!(NetSystem.IsServer && NetGameServer.IsLocal))
            {
                Plugin.Log.LogInfo("テスト機能はローカルプレイでだけ使えます");
                return;
            }

            Patches.LootTableChooseItemIndexPatch.ForceBananaForTesting = !Patches.LootTableChooseItemIndexPatch.ForceBananaForTesting;
            Plugin.Log.LogInfo(Patches.LootTableChooseItemIndexPatch.ForceBananaForTesting
                ? "テスト：ミニゲームのアイテム報酬を全部バナナにします（もう一度押すと元に戻ります）"
                : "テスト：ミニゲームのアイテム報酬を元に戻しました");
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
                Plugin.Log.LogInfo("テスト機能はボードの上でだけ使えます（ミニゲーム中・ロビーでは使えません）");
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
            Plugin.Log.LogInfo("テスト機能：この端末で遊んでいる人（AI以外）が見つかりません");
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
