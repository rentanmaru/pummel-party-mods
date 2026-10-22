using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ZP.Net;

namespace BananaItem.Patches
{
    // ミニゲームの報酬（MinigameResults の抽選表）にバナナを入れる
    // 抽選はホストだけで行われ、結果のアイテムIDが全員に送られる
    [HarmonyPatch(typeof(MinigameResults), MethodType.Constructor, typeof(int))]
    internal static class MinigameResultsConstructorPatch
    {
        private static void Postfix(MinigameResults __instance)
        {
            if (!BananaDefinition.IsRegistered)
            {
                return;
            }

            int id = (int)BananaDefinition.Details.itemID;
            // 1位の表は強いアイテムのままにして、2位・3位あたりに入れる
            AddBanana(__instance.m_soloLootTables, 1, id);           // 個人戦 2位
            AddBanana(__instance.m_soloLootTables, 2, id);           // 個人戦 3位
            AddBanana(__instance.m_teamPrimaryLootTables, 2, id);    // チーム戦 3位チームの1つ目
            AddBanana(__instance.m_teamSecondaryItemLootTables, 1, id); // チーム戦 2位チームの2つ目
        }

        private static void AddBanana(MinigameItemLootTable[] tables, int index, int id)
        {
            if (tables != null && index < tables.Length && tables[index] != null)
            {
                tables[index].Add(new LootTableItem<int>(id, BananaRules.RewardWeight));
            }
        }
    }

    // 抽選の重み計算。バニラは「Items の番号」で GetItemFromEnum と ObtainedInventory[番号] を引くので、
    // バナナ（ID 250）が入った表をそのまま計算すると落ちる。バナナが入っている表だけ、安全な計算に置き換える
    [HarmonyPatch(typeof(MinigameItemLootTable), "GetWeights")]
    internal static class LootTableGetWeightsPatch
    {
        private static bool Prefix(MinigameItemLootTable __instance, BoardPlayer player, bool considerPlayersObtainedItems, ref List<int> __result)
        {
            if (!BananaDefinition.IsRegistered || !ContainsBanana(__instance.m_items))
            {
                return true; // バナナの入っていない表はバニラのまま
            }

            ulong bananaId = BananaDefinition.Details.itemID;
            var weights = new List<int>();
            foreach (LootTableItem<int> item in __instance.m_items)
            {
                int weight = item.Weight;
                if (item.ItemID != byte.MaxValue)
                {
                    bool isBanana = (ulong)item.ItemID == bananaId;
                    ItemDetails details = isBanana ? BananaDefinition.Details : GameManager.GetItemFromEnum((Items)item.ItemID);
                    if (details != null && details.GetIsActive())
                    {
                        // バニラと同じく、すでに持ったことがあるアイテムは出にくくする
                        int obtainedIndex = isBanana ? details.itemIndex : item.ItemID;
                        if (considerPlayersObtainedItems && player != null && obtainedIndex < player.ObtainedInventory.Length
                            && player.ObtainedInventory[obtainedIndex] > 0)
                        {
                            weight = (int)(weight * (0.5f / player.ObtainedInventory[obtainedIndex]));
                            weight = Mathf.Max(weight, 1);
                        }
                    }
                    else
                    {
                        weight = 0;
                    }
                }
                weights.Add(weight);
            }

            __result = weights;
            return false;
        }

        private static bool ContainsBanana(List<LootTableItem<int>> items)
        {
            ulong bananaId = BananaDefinition.Details.itemID;
            foreach (LootTableItem<int> item in items)
            {
                if ((ulong)item.ItemID == bananaId)
                {
                    return true;
                }
            }
            return false;
        }
    }

    // テスト用：報酬バナナ固定モード（ローカルプレイのみ）。アイテムが出るはずの報酬を全部バナナにする
    [HarmonyPatch(typeof(MinigameItemLootTable), nameof(MinigameItemLootTable.ChooseItemIndex))]
    internal static class LootTableChooseItemIndexPatch
    {
        internal static bool ForceBananaForTesting;

        private static void Postfix(ref int __result)
        {
            // オンにしたままオンラインに行っても効かないよう、ローカルプレイ中だけ有効
            bool isLocalPlay = NetSystem.IsServer && NetGameServer.IsLocal;
            if (ForceBananaForTesting && isLocalPlay && __result != byte.MaxValue && BananaDefinition.IsRegistered
                && BananaDefinition.Details.GetIsActive())
            {
                __result = (int)BananaDefinition.Details.itemID;
            }

            if (BananaDefinition.IsRegistered && __result == (int)BananaDefinition.Details.itemID)
            {
                Plugin.Log.LogInfo("ミニゲームの報酬でバナナが出ました");
            }
        }
    }

    // バナナを手に入れたとき（アイテムマス・ミニゲーム報酬・テスト用キー）にログを出す（確認用）
    [HarmonyPatch(typeof(BoardPlayer), nameof(BoardPlayer.GiveItem))]
    internal static class BoardPlayerGiveItemPatch
    {
        private static void Postfix(BoardPlayer __instance, byte itemIndex)
        {
            if (BananaDefinition.IsRegistered && BananaDefinition.Details.GetIsActive() && itemIndex == BananaDefinition.Details.itemIndex)
            {
                Plugin.Log.LogInfo($"{__instance.GamePlayer.Name} がバナナを手に入れました");
            }
        }
    }

    // ボード開始時：アイテムマスでのバナナの基本の確率をログに出す（確認用）
    [HarmonyPatch(typeof(ItemList), nameof(ItemList.Setup))]
    internal static class ItemListSetupPatch
    {
        private static void Postfix(ItemList __instance)
        {
            if (!BananaDefinition.IsRegistered || __instance.enabledItems == null)
            {
                return;
            }

            int pool = 0;
            bool bananaEnabled = false;
            foreach (ItemDetails item in __instance.enabledItems)
            {
                if (item != null && !item.weaponSpaceItem)
                {
                    pool++;
                    bananaEnabled |= BananaDefinition.IsBanana(item);
                }
            }

            Plugin.Log.LogInfo(bananaEnabled
                ? $"このゲームのバナナ：アイテムマスの基本確率 1/{pool}（約{100f / pool:0}%）、ミニゲーム2位・3位の報酬にも入ります"
                : "このゲームではバナナは無効です（アイテム設定、または未導入の人がいるため）");
        }
    }
}
