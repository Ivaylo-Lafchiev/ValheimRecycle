using HarmonyLib;

using UnityEngine;

namespace ValheimRecycle
{
	[HarmonyPatch(typeof(Console), "InputText")]
	public static class ConsolePatch
	{

		public static bool Prefix(Console __instance)
		{
			string text = __instance.m_input.text;

			if (text.StartsWith("run"))
            {
				ConsolePatch.Run();
            }
			return true;

		}


		public static void Run()
        {
			Debug.Log("Test");
        }
	}
}
