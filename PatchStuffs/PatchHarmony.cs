using HarmonyLib;
using static Deadpan.Enums.Engine.Components.Modding.WildfrostMod;

[HarmonyPatch(typeof(DebugLoggerTextWriter), nameof(DebugLoggerTextWriter.WriteLine))]
class PatchHarmony
{
	static bool Prefix()
	{
		Postfix();
		return false;
	}

	static void Postfix() =>
		HarmonyLib.Tools.Logger.ChannelFilter = HarmonyLib.Tools.Logger.LogChannel.Warn | HarmonyLib.Tools.Logger.LogChannel.Error;
}