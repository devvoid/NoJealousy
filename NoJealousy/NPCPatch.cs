using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Quests;

namespace NoJealousy
{
    public class NPCPatch
    {
        private static IMonitor Monitor;

        // call instance method from your Entry class
        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        public static bool Patch_Prefix(StardewValley.NPC __instance, Farmer who)
        {
            try
            {
				// To simplify things, we run the normal logic if the item the player gives is one of certain quest items.
				if (who.ActiveObject != null)
                {
					switch (who.ActiveObject.ParentSheetIndex)
                    {
						// Void Mayonnaise
						// Technically not exclusively a quest item, but everyone but Krobus hates it,
						// and hated gifts or gifts to Krobus (I think) can't trigger jealousy.
						// Including this removes the "Goblin Problem" quest handling.
						case 308:
						// Pierre's Stocklist
						case 897:
						// Mayor Lewis's lucky purple shorts (trimmed and untrimmed)
						case 71:
						case 789:
						// Movie ticket
						case 809:
						// Void Ghost Pendant (Krobus roommate proposal item)
						case 808:
						// Bouquet
						case 458:
						// Wilted Bouquet (breakup item)
						case 277:
						// Mermaid's Pendant
						case 460:
							Monitor.Log("Gift given; item is detected as a special item, using patched logic", LogLevel.Info);
							return true;
                    }

					if (who.ActiveObject.questItem.Value)
                    {
						Monitor.Log("Gift given; item is detected as a quest item, using patched logic", LogLevel.Info);
						return true;
                    }
                }

				// New logic
				PatchLogic(__instance, who);

                // If all went well, don't re-run the original logic
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(Patch_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }

        private static void PatchLogic(StardewValley.NPC instance, Farmer who)
        {
			Monitor.Log("Gift given; using patched logic", LogLevel.Info);
			// Stop moving and face towards the person you're gifting to.
			who.Halt();
			who.faceGeneralDirection(instance.getStandingPosition(), 0, opposite: false, useTileCalculations: false);

			// Special Order handling
			if (Game1.player.team.specialOrders != null)
			{
				foreach (SpecialOrder order in Game1.player.team.specialOrders)
				{
					if (order.onItemDelivered != null)
					{
						Delegate[] invocationList = order.onItemDelivered.GetInvocationList();
						for (int i = 0; i < invocationList.Length; i++)
						{
							if (((Func<Farmer, NPC, Item, int>)invocationList[i])(Game1.player, instance, who.ActiveObject) > 0)
							{
								if (who.ActiveObject.Stack <= 0)
								{
									who.ActiveObject = null;
									who.showNotCarrying();
								}
								return;
							}
						}
					}
				}
			}
			
			// Quest of the day
			if (Game1.questOfTheDay != null && Game1.questOfTheDay.accepted.Value && !Game1.questOfTheDay.completed.Value && Game1.questOfTheDay is ItemDeliveryQuest && ((ItemDeliveryQuest)Game1.questOfTheDay).checkIfComplete(instance, -1, -1, who.ActiveObject))
			{
				who.reduceActiveItemByOne();
				who.completelyStopAnimatingOrDoingAction();
				if (Game1.random.NextDouble() < 0.3 && !instance.Name.Equals("Wizard"))
				{
					instance.doEmote(32);
				}
			}
			
			// Quest of the day fishing??
			else if (Game1.questOfTheDay != null && Game1.questOfTheDay is FishingQuest && ((FishingQuest)Game1.questOfTheDay).checkIfComplete(instance, who.ActiveObject.ParentSheetIndex, 1))
			{
				who.reduceActiveItemByOne();
				who.completelyStopAnimatingOrDoingAction();
				if (Game1.random.NextDouble() < 0.3 && !instance.Name.Equals("Wizard"))
				{
					instance.doEmote(32);
				}
			}
			
			// If instance NPC has a dialog key to reject instance item, reject it.
			// I think instance is used for the "The Pirate's Wife" quest
			else if (who.ActiveObject != null && instance.Dialogue.ContainsKey("reject_" + who.ActiveObject.ParentSheetIndex))
			{
				instance.setNewDialogue(instance.Dialogue["reject_" + who.ActiveObject.ParentSheetIndex]);
				Game1.drawDialogue(instance);
			}
			
			// No quests, no orders, no nothin', just givin' a gift.
			else
			{
				// I don't know what this does
				if (who.checkForQuestComplete(instance, -1, -1, null, "", 10))
				{
					return;
				}

				if (!Game1.NPCGiftTastes.ContainsKey(instance.Name))
				{
					return;
				}
				foreach (string s in who.activeDialogueEvents.Keys)
				{
					if (s.Contains("dumped") && instance.Dialogue.ContainsKey(s))
					{
						instance.doEmote(12);
						return;
					}
				}

				// Quest 25 is the gifting tutorial
				who.completeQuest(25);

				if ((who.friendshipData.ContainsKey(instance.Name) && who.friendshipData[instance.Name].GiftsThisWeek < 2) || (who.spouse != null && who.spouse.Equals(instance.Name)) || instance is Child || instance.isBirthday(Game1.currentSeason, Game1.dayOfMonth))
				{
					if (who.friendshipData[instance.Name].IsDivorced())
					{
						instance.CurrentDialogue.Push(new Dialogue(Game1.content.LoadString("Strings\\Characters:Divorced_gift"), instance));
						Game1.drawDialogue(instance);
						return;
					}
					if (who.friendshipData[instance.Name].GiftsToday == 1)
					{
						Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3981", instance.displayName)));
						return;
					}
					instance.receiveGift(who.ActiveObject, who);
					who.reduceActiveItemByOne();
					who.completelyStopAnimatingOrDoingAction();
					instance.faceTowardFarmerForPeriod(4000, 3, faceAway: false, who);
				}
				else
				{
					Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3987", instance.displayName, 2)));
				}				
			}
		}
    }
}
