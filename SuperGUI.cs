using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BepMarket
{
    internal class SuperGUI : MonoBehaviour
    {
        private static readonly int INTERACTIVE_LAYER_MASK = 1 << 7;

        private ManagerBlackboard blackboard;
        private MethodInfo productCountMethod;
        private ProductListing products;

        private Camera playerCam;
        private GUIStyle stocklabelStyle;

        private GUIStyle priceLabelStyle;
        private readonly List<Data_Product> lowPriceProducts = [];
        private bool showRecommendedPrices = true;
        private bool pricesUpdated = false;

        private void Start()
        {
            // label style for DisplayShelfInfo()
            stocklabelStyle = new GUIStyle();
            stocklabelStyle.alignment = TextAnchor.MiddleCenter;
            stocklabelStyle.fontSize = 24;
            stocklabelStyle.fontStyle = FontStyle.Bold;

            // label style for ShowOptimalPrices()
            priceLabelStyle = new GUIStyle();
            stocklabelStyle.fontSize = 20;
            priceLabelStyle.normal.textColor = Color.yellow;

            // register events
            GameEvents.LobbyJoined += LobbyStart;
            GameEvents.LobbyJoined += CheckPriceDifferences;
            GameEvents.NewDayStarted += CheckPriceDifferences;
            GameEvents.NewItemsUnlocked += CheckPriceDifferences;
            GameEvents.LobbyLeft += LobbyDestroy;
            GameEvents.ItemUpdated += OnItemPriceUpdate;
        }

        private void LobbyStart()
        {
            playerCam = GameObject.Find("Player_Camera").GetComponent<Camera>();
            blackboard = FindObjectsByType<ManagerBlackboard>(FindObjectsSortMode.None).First();
            productCountMethod = typeof(ManagerBlackboard).GetMethod("GetProductsExistences", BindingFlags.NonPublic | BindingFlags.Instance);
            products = ProductListing.Instance;
        }

        private void LobbyDestroy()
        {
            // reset price update vars to defaults
            lowPriceProducts.Clear();
            pricesUpdated = false;
            showRecommendedPrices = true;
        }

        private void OnGUI()
        {
            // wait until lobby has been created
            if (playerCam == null)
            {
                return;
            }

            DisplayShelfInfo();
            ShowOptimalPrices();
        }

        private void DisplayShelfInfo()
        {
            RaycastHit hit;
            Collider shelfCollider;
            GameObject shelf;
            Data_Container shelfData;
            int productIndex;
            int[] productCounts;

            // interactables on layer 7
            Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out hit, 5F, INTERACTIVE_LAYER_MASK);

            // filter for shelf subcontainers
            shelfCollider = hit.collider;
            if (shelfCollider == null)
            {
                return;
            }

            shelf = shelfCollider.gameObject;
            if (shelf.gameObject.name != "SubContainer")
            {
                return;
            }


            shelfData = shelf.GetComponentInParent<Data_Container>();
            productIndex = shelf.transform.GetSiblingIndex() * 2;
            if (shelfData.productInfoArray[productIndex] == -1 || shelfData.productInfoArray[productIndex + 1] == 0)
            {
                return;
            }

            // display product name
            stocklabelStyle.normal.textColor = Color.white;
            GUI.Label(
                new Rect(Screen.width / 2 - 128, Screen.height / 2 + 60, 256, 24),
                LocalizationManager.instance.GetLocalizationString(
                    $"product{shelfData.productInfoArray[productIndex]}"
                ),
                stocklabelStyle
            );

            // get product counts
            productCounts = productCountMethod.Invoke(blackboard, [shelfData.productInfoArray[productIndex]]) as int[];

            // draw background
            GUI.Box(new Rect(Screen.width / 2 - 128, Screen.height / 2 + 36, 256, 128), "");

            // display product count - shelved
            stocklabelStyle.normal.textColor = Color.red;
            GUI.Label(
                new Rect(Screen.width / 2 - 32 - 96, Screen.height / 2 + 60 + 48, 64, 24),
                productCounts[0].ToString(),
                stocklabelStyle
            );

            // display product count - stocked
            stocklabelStyle.normal.textColor = Color.green;
            GUI.Label(
                new Rect(Screen.width / 2 - 32, Screen.height / 2 + 60 + 48, 64, 24),
                productCounts[1].ToString(),
                stocklabelStyle
            );

            // display product count - boxed and unshelved
            // note: game does not keep track of player held items
            stocklabelStyle.normal.textColor = Color.yellow;
            GUI.Label(
                new Rect(Screen.width / 2 - 32 + 96, Screen.height / 2 + 60 + 48, 64, 24),
                productCounts[2].ToString(),
                stocklabelStyle
            );
        }

        private void ShowOptimalPrices()
        {
            int labelHeight = Screen.height - 20;

            // prices are updated if:
            // - prices for specified products have been changed
            // - player dismisses
            if (pricesUpdated)
            {
                return;
            }

            if (!showRecommendedPrices)
            {
                GUI.Label(
                    new Rect(4, labelHeight, 256, 20),
                    "Prices were updated! [Home] to show or [End] to dismiss.",
                    priceLabelStyle
                );
                return;
            }

            foreach (Data_Product product in lowPriceProducts)
            {
                GUI.Label(
                    new Rect(4, labelHeight, 256, 20),
                    $"{LocalizationManager.instance.GetLocalizationString("product" + product.productID)}: " +
                        $"{products.productPlayerPricing[product.productID].ToString("c2")} -> " +
                        $"{(CalculateInflatedPrice(product) * 2).ToString("c2")}",
                    priceLabelStyle
                );
                labelHeight -= 24;
            }

            GUI.Label(
                new Rect(4, labelHeight, 256, 28),
                "[End] to close.",
                priceLabelStyle
            );
        }

        private void CheckPriceDifferences()
        {
            Data_Product productData;
            float playerPrice;

            // clear previous product updates
            lowPriceProducts.Clear();
            showRecommendedPrices = false;

            if (products == null)
            {
                products = ProductListing.Instance;
            }

            // compare user prices with base price
            foreach (int id in products.availableProducts)
            {
                productData = products.productPrefabs[id].GetComponent<Data_Product>();
                playerPrice = products.productPlayerPricing[id];

                if (Mathf.Abs(playerPrice - (CalculateInflatedPrice(productData) * 2)) > 0.1)
                {
                    lowPriceProducts.Add(productData);
                }
            }

            pricesUpdated = lowPriceProducts.Count == 0;
        }

        private void OnItemPriceUpdate(int productId)
        {
            Data_Product productData = products.productPrefabs[productId].GetComponent<Data_Product>();
            float playerPrice = products.productPlayerPricing[productId];

            if (Mathf.Abs(playerPrice - (CalculateInflatedPrice(productData) * 2)) <= 0.1)
            {
                lowPriceProducts.Remove(productData);
            }

            if (lowPriceProducts.Count == 0)
            {
                pricesUpdated = true;
            }
        }

        private float CalculateInflatedPrice(Data_Product product)
        {
            // inflation array does not get updated when new items get unlocked, so it should be
            // safe to assume the price will remain unchanged
            if (product.productID >= products.tierInflation.Count())
            {
                return product.basePricePerUnit;
            }

            return product.basePricePerUnit * products.tierInflation[product.productID];
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                showRecommendedPrices = true;
            }
            if (Input.GetKeyDown(KeyCode.End))
            {
                pricesUpdated = true;
            }
        }
    }
}
