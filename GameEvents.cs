using HarmonyLib;
using System;

namespace BepMarket
{
    public delegate void SimpleGameEvent();
    public delegate void ItemUpdateEvent(int productId);

    public class GameEvents
    {
        public static event SimpleGameEvent LobbyJoined;
        public static event SimpleGameEvent LobbyLeft;
        public static event SimpleGameEvent NewDayStarted;
        public static event SimpleGameEvent NewItemsUnlocked;

        public static event ItemUpdateEvent ItemUpdated;

        // --- SIMPLE GAME EVENTS ---

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerObjectController), "OnStartClient")]
        private static void LobbyJoinedHook()
        {
            LobbyJoined();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerObjectController), "OnStopClient")]
        private static void LobbyLeftHook()
        {
            LobbyLeft();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameData), "WaitUntilNewDay")]
        private static void NewDayStartedHook()
        {
            NewDayStarted();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductListing), "updateProductList")]
        private static void NewItemsUnlockedHook()
        {
            NewItemsUnlocked();
        }

        // --- ITEM-SPECIFIC GAME EVENTS ---

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ProductListing), "RpcUpdateProductPricer")]
        private static void ItemUpdatedHook(int productID)
        {
            ItemUpdated(productID);
        }
    }
}
