using HarmonyLib;
using static Deadpan.Enums.Engine.Components.Modding.WildfrostMod;
using BlindModeMode;
using UnityEngine.Localization.Tables;
using Deadpan.Enums.Engine.Components.Modding;
using UnityEngine;
using UnityEngine.Localization.Components;

[HarmonyPatch(typeof(CardPopUpTarget), nameof(CardPopUpTarget.Pop))]
class PatchNoHover
{
	static bool Prefix(CardPopUpTarget __instance)
	{
		if ((bool)__instance.card?.entity?.gameObject.GetComponentInChildren<Canvas>().transform.Find("="))
			return false;
		else
			return true;
	}
}
[HarmonyPatch(typeof(InspectSystem), nameof(InspectSystem.Inspect), typeof(Entity))]
class PatchNoInspect
{
	static bool Prefix(Entity entity)
	{
		if ((bool)entity.gameObject.GetComponentInChildren<Canvas>().transform.Find("="))
			return false;
		else
			return true;
	}
}
[HarmonyPatch(typeof(MapNodeSpriteSetterBattle), nameof(MapNodeSpriteSetterBattle.Set), typeof(MapNode))]
class PatchChangeSprite
{
	static bool Prefix(MapNodeSpriteSetterBattle __instance, MapNode mapNode)
	{
		bool blindEnemy = BlindMode.instance.blindmode == BlindingMode.Enemy || BlindMode.instance.blindmode == BlindingMode.EnemyAndItem || BlindMode.instance.blindmode == BlindingMode.EnemyAndFriendlyAndItem;
		if (!blindEnemy) return true;

		if ((bool)__instance.@base)
		{
			AreaData areaData = References.Areas[mapNode.campaignNode.areaIndex];
			__instance.@base.sprite = areaData.battleBaseSprite;
		}

		if (mapNode.campaignNode.type is CampaignNodeTypeBattle && mapNode.campaignNode.data.TryGetValue("battle", out var value) && value is string assetName)
		{
			BattleData battleData = AddressableLoader.Get<BattleData>("BattleData", assetName);
			if ((object)battleData != null)
			{
				StringTable uiText = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);

				string key = mapNode.name + "Ribbon";
				uiText.SetString(key, "???");

				__instance.icon.sprite = BlindMode.instance.ScaledSprite("Mystery.png", 200);
				if ((bool)__instance.battleNameString)
				{
					__instance.battleNameString.StringReference = uiText.GetString(key);
				}
			}
		}

		if (mapNode.campaignNode.cleared && (bool)__instance.flagObj)
		{
			__instance.flagObj.SetActive(value: true);
			__instance.iconObj.SetActive(value: false);
		}
		return false;
	}
}