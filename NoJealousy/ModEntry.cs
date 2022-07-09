using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace NoJealousy
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            // example patch, you'll need to edit this for your patch
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.NPC), nameof(StardewValley.NPC.tryToReceiveActiveObject)),
               prefix: new HarmonyMethod(typeof(NPCPatch), nameof(NPCPatch.Patch_Prefix))
            );
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            this.Monitor.Log("Button pressed; change2", LogLevel.Debug);
        }
    }
}
