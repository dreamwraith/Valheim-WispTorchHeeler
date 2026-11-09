using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace WispTorchHeeler.Configuration
{
    // config manager drawer that shows a recipe string as editable PrefabName : Amount rows
    public static class RecipeConfigDrawer
    {
        public const string DefaultPlaceholderPrefab = "<PrefabName>";
        private const char ItemSeparator = ',';
        private const char AmountSeparator = ':';

        private static readonly GUILayoutOption s_amountWidth = GUILayout.Width(40f);
        private static readonly GUILayoutOption s_buttonWidth = GUILayout.Width(24f);

        public record struct RecipeItemEntry(string PrefabName, int Amount);

        // handed to ConfigurationManagerAttributes.CustomDrawer
        public static void Draw(ConfigEntryBase entry)
        {
            string rawValue = entry.BoxedValue is string boxedValue ? boxedValue : string.Empty;
            List<RecipeItemEntry> items = Deserialize(rawValue);

            bool isChanged = false;
            List<RecipeItemEntry> updatedItems = new List<RecipeItemEntry>();

            GUILayout.BeginVertical();

            if (items.Count == 0)
            {
                if (GUILayout.Button("+ Add Requirement"))
                {
                    updatedItems.Add(new RecipeItemEntry(DefaultPlaceholderPrefab, 1));
                    isChanged = true;
                }
            }
            else
            {
                for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                {
                    RecipeItemEntry item = items[itemIndex];
                    GUILayout.BeginHorizontal();

                    string newPrefabName = GUILayout.TextField(item.PrefabName, GUILayout.ExpandWidth(true), GUILayout.MinWidth(60f));
                    if (newPrefabName != item.PrefabName)
                    {
                        item.PrefabName = newPrefabName;
                        isChanged = true;
                    }

                    string amountString = GUILayout.TextField(item.Amount.ToString(), s_amountWidth);
                    if (int.TryParse(amountString, out int parsedAmount) && parsedAmount != item.Amount)
                    {
                        item.Amount = parsedAmount;
                        isChanged = true;
                    }

                    if (GUILayout.Button("-", s_buttonWidth))
                    {
                        isChanged = true;
                    }
                    else
                    {
                        updatedItems.Add(item);
                    }

                    if (GUILayout.Button("+", s_buttonWidth))
                    {
                        updatedItems.Add(new RecipeItemEntry(DefaultPlaceholderPrefab, 1));
                        isChanged = true;
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();

            if (isChanged)
            {
                entry.BoxedValue = Serialize(updatedItems);
            }
        }

        public static List<RecipeItemEntry> Deserialize(string rawRecipeString)
        {
            List<RecipeItemEntry> parsedEntries = new List<RecipeItemEntry>();
            if (string.IsNullOrWhiteSpace(rawRecipeString)) return parsedEntries;

            string[] tokens = rawRecipeString.Split(new[] { ItemSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                string[] parts = token.Trim().Split(AmountSeparator);
                string prefabName = parts[0].Trim();
                int amount = 1;
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int parsedAmount))
                {
                    amount = parsedAmount;
                }
                parsedEntries.Add(new RecipeItemEntry(prefabName, amount));
            }

            return parsedEntries;
        }

        public static string Serialize(IEnumerable<RecipeItemEntry> entries) =>
            string.Join(ItemSeparator.ToString(), entries.Select(entry => $"{entry.PrefabName.Trim()}{AmountSeparator}{Mathf.Max(1, entry.Amount)}"));
    }
}
