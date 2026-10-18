using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace BananaItem.Patches
{
    // 公式Mod用の MOD_Item の空フックに、バナナの動きを入れる（details がバナナのときだけ）
    [HarmonyPatch(typeof(MOD_Item))]
    internal static class ModItemPatches
    {
        // 手に持ったときの見た目の大きさ（ワールドでの倍率。1 でモデルそのままの大きさ）
        private const float HeldWorldScale = 1.2f;

        // 構えたとき：回復キットと同じように両手で持つ
        // （MOD_Item.Setup は base.Setup() で手のモデルを作ってから MOD_OnSetup を呼ぶ）
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MOD_Item.MOD_OnSetup))]
        private static void OnSetupPostfix(MOD_Item __instance)
        {
            if (!BananaDefinition.IsBanana(__instance.details))
            {
                return;
            }
            PlayerAnimation animation = __instance.player.BoardObject.PlayerAnimation;
            animation.CarryingSide = 1;
            animation.Carrying = true;
            FitHeldObjectSize(__instance.heldObject);
        }

        // 手の骨の大きさはキャラによって違うので、親の倍率から逆算して見た目の大きさをそろえる
        private static void FitHeldObjectSize(GameObject held)
        {
            if (held == null || held.transform.parent == null)
            {
                return;
            }

            Vector3 parentScale = held.transform.parent.lossyScale;
            held.transform.localScale = new Vector3(
                HeldWorldScale / NonZero(parentScale.x),
                HeldWorldScale / NonZero(parentScale.y),
                HeldWorldScale / NonZero(parentScale.z));
            Plugin.Log.LogInfo($"手に持つバナナの大きさを調整しました（手の倍率 {parentScale.x:0.####}）");
        }

        private static float NonZero(float value)
        {
            return Mathf.Abs(value) < 0.0001f ? 1f : value;
        }

        // 使ったとき（全員の端末で呼ばれる）：今いるマスにバナナを置いて終わる
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MOD_Item.MOD_OnUse))]
        private static void OnUsePostfix(MOD_Item __instance)
        {
            if (BananaDefinition.IsBanana(__instance.details))
            {
                __instance.StartCoroutine(UseBanana(__instance));
            }
        }

        private static IEnumerator UseBanana(MOD_Item item)
        {
            BoardPlayer owner = item.player.BoardObject;
            owner.PlayerAnimation.CarryingSide = 0;
            owner.PlayerAnimation.Carrying = false;
            yield return new WaitForSeconds(0.2f);

            BananaField.Place(owner.CurrentNode, item.player);
            AudioSystem.PlayOneShot("MultimediaButtonClick007_STD_ZapSplat", 0.5f);
            yield return new WaitForSeconds(0.5f);

            item.MOD_Finish(relay: false);
        }

        // 外したとき：持つ姿勢を戻す
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MOD_Item.MOD_OnUnEquip))]
        private static void OnUnEquipPrefix(MOD_Item __instance)
        {
            if (!BananaDefinition.IsBanana(__instance.details))
            {
                return;
            }
            PlayerAnimation animation = __instance.player.BoardObject.PlayerAnimation;
            animation.CarryingSide = 0;
            animation.Carrying = false;
        }
    }

    // AIの使いどき：すでにバナナがあるマスでなければ、ほどほどの優先度で使う
    [HarmonyPatch(typeof(Item), nameof(Item.GetTarget))]
    internal static class ItemGetTargetPatch
    {
        private static void Postfix(Item __instance, BoardPlayer user, ref ItemAIUse __result)
        {
            if (!BananaDefinition.IsBanana(__instance.details))
            {
                return;
            }
            __result = BananaField.HasBanana(user.CurrentNode) ? null : new ItemAIUse(user, 0.3f);
        }
    }

    // 名前・説明を言語に合わせて出す／未導入の人がいるロビーでは無効にする
    [HarmonyPatch(typeof(ItemDetails))]
    internal static class ItemDetailsPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemDetails.TranslatedName), MethodType.Getter)]
        private static bool TranslatedNamePrefix(ItemDetails __instance, ref string __result)
        {
            if (!BananaDefinition.IsBanana(__instance))
            {
                return true;
            }
            __result = BananaText.Name;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemDetails.TranslatedDescription), MethodType.Getter)]
        private static bool TranslatedDescriptionPrefix(ItemDetails __instance, ref string __result)
        {
            if (!BananaDefinition.IsBanana(__instance))
            {
                return true;
            }
            __result = BananaText.Description;
            return false;
        }

        // ボード開始時の ItemList.Setup() でここが呼ばれ、false なら候補から外れる
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemDetails.GetIsActive))]
        private static bool GetIsActivePrefix(ItemDetails __instance, ref bool __result)
        {
            if (!BananaDefinition.IsBanana(__instance) || MultiplayerGuard.IsBananaAllowed())
            {
                return true; // 普段どおりルールセットの設定を見る
            }
            __result = false;
            return false;
        }
    }
}
