﻿using Helpers;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace EquipBestItem
{
    class BestEquipmentUpgrader
    {
        private CharacterData _characterData;

        private SPInventoryVM _inventory;
        private InventoryLogic _inventoryLogic;

        private BestEquipmentCalculator _bestEquipmentCalculator;

        public BestEquipmentUpgrader()
        {
            _characterData = null;
            _bestEquipmentCalculator = new BestEquipmentCalculator();
        }

        /// <summary>
        /// Refresh the equipment upgrader with new inventory and inventory logic
        /// </summary>
        public void RefreshValues()
        {
            _inventoryLogic = InventoryManager.InventoryLogic;
            _inventory = InventoryBehavior.Inventory;
        }

        /// <summary>
        /// Get the best item from inventory side
        /// </summary>
        /// <param name="itemListVM">List of items on the inventory side</param>
        /// <param name="equipmentElement">Current item</param>
        /// <param name="slot">Type of item</param>
        /// <param name="isCivilian">Civilian Type</param>
        /// <returns></returns>
        public EquipmentElement GetBetterItemFromSide(MBBindingList<SPItemVM> itemListVM, EquipmentElement equipmentElement, EquipmentIndex slot, bool isCivilian)
        {
            EquipmentElement bestEquipmentElement = new EquipmentElement();

            CharacterObject character = _characterData.GetCharacterObject();

            // Loops through the inventory list to find the best equipment item
            foreach (SPItemVM item in itemListVM)
            {
                // Skips through the process if the item is camel or camel harness
                if (IsCamel(item) || IsCamelHarness(item))
                    continue;

                // Skips only if the character cannot use the item or the item is not equipable
                if (!CharacterHelper.CanUseItemBasedOnSkill(character, item.ItemRosterElement.EquipmentElement) || !item.IsEquipableItem)
                    continue;

                // Skips only if we are looking for civilian equipment and item is not civilian equipment
                if (isCivilian && !item.IsCivilianItem)
                    continue;

                if (slot < EquipmentIndex.NonWeaponItemBeginSlot &&
                    item.ItemRosterElement.EquipmentElement.Item.PrimaryWeapon != null)
                {
                    // Check that both weapons have same weapon class and same item usage
                    bool sameWeaponClass = equipmentElement.Item.WeaponComponent.PrimaryWeapon.WeaponClass == item.ItemRosterElement.EquipmentElement.Item.PrimaryWeapon.WeaponClass;
                    bool sameItemUsage = GetItemUsage(item) == equipmentElement.Item.PrimaryWeapon.ItemUsage;

                    // Check that both weapons are both couch weapons or not
                    // TODO: Rewrite this portion since it is confusing to follow
                    bool couchWeapon = IsCouchWeapon(equipmentElement);
                    bool couchUsage = !couchWeapon || IsCouchWeapon(item.ItemRosterElement.EquipmentElement);

                    if (sameWeaponClass && sameItemUsage && couchUsage)
                    {
                        bestEquipmentElement = GetBestEquipmentElement(slot, item.ItemRosterElement.EquipmentElement, equipmentElement, bestEquipmentElement);
                    }
                }
                else if (item.ItemType == slot)
                {
                    bestEquipmentElement = GetBestEquipmentElement(slot, item.ItemRosterElement.EquipmentElement, equipmentElement, bestEquipmentElement);
                }

            }

            return bestEquipmentElement;
        }

        /// <summary>
        /// Returns the best equipment element
        /// </summary>
        /// <param name="slot">Equipment slot</param>
        /// <param name="inventoryEquipmentElement">inventory equipment</param>
        /// <param name="currentEquipmentElement">current equipment</param>
        /// <param name="bestEquipmentElement">best equipment</param>
        /// <returns></returns>
        private EquipmentElement GetBestEquipmentElement(EquipmentIndex slot, EquipmentElement inventoryEquipmentElement, EquipmentElement currentEquipmentElement, EquipmentElement bestEquipmentElement)
        {
            float inventoryItemValue = ItemIndexCalculation(inventoryEquipmentElement, slot);
            float currentItemValue = bestEquipmentElement.IsEmpty ? ItemIndexCalculation(currentEquipmentElement, slot) : ItemIndexCalculation(bestEquipmentElement, slot);

            if (inventoryItemValue > currentItemValue && inventoryItemValue != 0f)
            {
                return inventoryEquipmentElement;
            }
            return bestEquipmentElement;
        }

        /// <summary>
        /// Returns a boolean if the item is a camel
        /// </summary>
        /// <param name="item">inventory item</param>
        /// <returns></returns>
        private static bool IsCamel(SPItemVM item)
        {
            if (item != null)
                if (!item.ItemRosterElement.IsEmpty)
                    if (!item.ItemRosterElement.EquipmentElement.IsEmpty)
                        if (item.ItemRosterElement.EquipmentElement.Item.HasHorseComponent)
                            if (item.ItemRosterElement.EquipmentElement.Item.HorseComponent.Monster.MonsterUsage == "camel")
                                return true;
            return false;
        }

        /// <summary>
        /// Returns a boolean if the is a camel harness
        /// </summary>
        /// <param name="item">inventory item</param>
        /// <returns></returns>
        private static bool IsCamelHarness(SPItemVM item)
        {
            return item != null && item.StringId.StartsWith("camel_sadd");
        }

        private static bool IsCouchWeapon(EquipmentElement weapon)
        {
            // Make sure the weapon item is not empty first
            if (weapon.IsEmpty)
                return false;

            // Make sure that there are any weapons
            if (weapon.Item.Weapons == null || weapon.Item.Weapons.Count == 0)
                return false;

            // Loop through the weapons of a given item
            foreach (var temp in weapon.Item.Weapons)
            {
                // Check that the weapon has a usage
                if (temp.ItemUsage == null)
                    continue;

                // Check that item usage has a usage for couch weapon
                if (temp.ItemUsage.Contains("couch"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns item usage in string
        /// </summary>
        /// <param name="item">inventory item</param>
        /// <returns></returns>
        public static string GetItemUsage(SPItemVM item)
        {
            if (item == null || item.ItemRosterElement.IsEmpty || item.ItemRosterElement.EquipmentElement.IsEmpty || item.ItemRosterElement.EquipmentElement.Item.WeaponComponent == null)
                return "";
            return item.ItemRosterElement.EquipmentElement.Item.PrimaryWeapon.ItemUsage;
        }

        /// <summary>
        /// Equips the character with the best items.
        ///
        /// With a given character, a character data is created using the character and character settings loaded from
        /// settings loader using the character's name. Character's equipment is obtained either from the
        /// battle equipment or civilian equipment depending if the inventory is in war set or civilian set.
        /// After getting the character data and equipment, the character equipment can be upgraded
        /// with the best equipment.
        /// 
        /// </summary>
        /// <param name="character">hero</param>
        public void EquipCharacter(CharacterObject character)
        {
            _characterData = new CharacterData(character, SettingsLoader.Instance.GetCharacterSettingsByName(character.Name.ToString()));
            Equipment characterEquipment = _inventory.IsInWarSet ? character.FirstBattleEquipment : character.FirstCivilianEquipment;
            EquipCharacterEquipment(characterEquipment, !_inventory.IsInWarSet);
        }

        /// <summary>
        /// Equips the character's equipment with the best equipment
        /// </summary>
        /// <param name="equipment">character's equipment</param>
        /// <param name="isCivilian">equipment civilian type</param>
        private void EquipCharacterEquipment(Equipment equipment, bool isCivilian)
        {
            // Loops through the character's equipment to equip the character with the best items
            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
            {
                if (equipment[equipmentIndex].IsEmpty && equipmentIndex < EquipmentIndex.NonWeaponItemBeginSlot ||
                    equipment[EquipmentIndex.Horse].IsEmpty && equipmentIndex == EquipmentIndex.HorseHarness)
                    continue;

                EquipmentElement bestLeftEquipmentElement = new EquipmentElement();
                EquipmentElement bestRightEquipmentElement = new EquipmentElement();

                // Gets the best item from sides that are unlocked
                if (!SettingsLoader.Instance.Settings.IsLeftPanelLocked)
                {
                    bestLeftEquipmentElement = GetBetterItemFromSide(_inventory.LeftItemListVM, equipment[equipmentIndex], equipmentIndex, isCivilian);
                }
                if (!SettingsLoader.Instance.Settings.IsRightPanelLocked)
                {
                    bestRightEquipmentElement = GetBetterItemFromSide(_inventory.RightItemListVM, equipment[equipmentIndex], equipmentIndex, isCivilian);
                }

                // Unequip character's current equipment item
                if (!equipment[equipmentIndex].IsEmpty && (bestLeftEquipmentElement.Item != null || bestRightEquipmentElement.Item != null))
                {
                    TransferCommand transferCommand = TransferCommand.Transfer(
                        1,
                        InventoryLogic.InventorySide.Equipment,
                        InventoryLogic.InventorySide.PlayerInventory,
                        new ItemRosterElement(equipment[equipmentIndex], 1),
                        equipmentIndex,
                        EquipmentIndex.None,
                        _characterData.GetCharacterObject(),
                        isCivilian
                    );
                    _inventoryLogic.AddTransferCommand(transferCommand);
                }

                if (bestLeftEquipmentElement.Item != null || bestRightEquipmentElement.Item != null)
                {
                    if (ItemIndexCalculation(bestLeftEquipmentElement, equipmentIndex) > ItemIndexCalculation(bestRightEquipmentElement, equipmentIndex))
                    {
                        TransferCommand equipCommand = TransferCommand.Transfer(
                            1,
                            InventoryLogic.InventorySide.OtherInventory,
                            InventoryLogic.InventorySide.Equipment,
                            new ItemRosterElement(bestLeftEquipmentElement, 1),
                            EquipmentIndex.None,
                            equipmentIndex,
                            _characterData.GetCharacterObject(),
                            isCivilian
                        );

                        EquipMessage(equipmentIndex);
                        _inventoryLogic.AddTransferCommand(equipCommand);
                    }
                    else
                    {
                        TransferCommand equipCommand = TransferCommand.Transfer(
                            1,
                            InventoryLogic.InventorySide.PlayerInventory,
                            InventoryLogic.InventorySide.Equipment,
                            new ItemRosterElement(bestRightEquipmentElement, 1),
                            EquipmentIndex.None,
                            equipmentIndex,
                            _characterData.GetCharacterObject(),
                            isCivilian
                        );

                        EquipMessage(equipmentIndex);
                        _inventoryLogic.AddTransferCommand(equipCommand);
                    }
                }

                // Since items have been transfered, there could be items with zero amount.
                // This should remove items with zero counts in the inventory.
                _inventory.ExecuteRemoveZeroCounts();
            }

            // Reflection call the method to refresh information values in the inventory
            _inventory.GetMethod("RefreshInformationValues");
        }

        /// <summary>
        /// Outputs a message based on the equipment slot
        /// </summary>
        /// <param name="equipmentIndex">Equipment Index Slot</param>
        private void EquipMessage(EquipmentIndex equipmentIndex)
        {
            var name = _characterData.GetCharacterObject().Name.ToString();
            MBTextManager.SetTextVariable("Hero", name);
            switch (equipmentIndex)
            {
                case EquipmentIndex.Weapon0:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBIhdMZcrLgy}{Hero} equips weapon in the first slot").ToString()));
                    break;
                case EquipmentIndex.Weapon1:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBIImSfYBZIB}{Hero} equips weapon in the second slot").ToString()));
                    break;
                case EquipmentIndex.Weapon2:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBImnt3weSaE}{Hero} equips weapon in the third slot").ToString()));
                    break;
                case EquipmentIndex.Weapon3:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBIQWLelE3Fl}{Hero} equips weapon in the fourth slot").ToString()));
                    break;
                case EquipmentIndex.Head:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBIhHipqU9uB}{Hero} equips helmet").ToString()));
                    break;
                case EquipmentIndex.Body:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBI54XyvNFad}{Hero} equips body armor").ToString()));
                    break;
                case EquipmentIndex.Leg:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBI0tuZtFp3o}{Hero} equips boots").ToString()));
                    break;
                case EquipmentIndex.Gloves:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBIEPpXh4mnM}{Hero} equips gloves").ToString()));
                    break;
                case EquipmentIndex.Cape:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBIzM65FL7cP}{Hero} equips cape").ToString()));
                    break;
                case EquipmentIndex.Horse:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBIQYRUfhlVn}{Hero} equips horse").ToString()));
                    break;
                case EquipmentIndex.HorseHarness:
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=EBIbrmijAmqh}{Hero} equips horse harness").ToString()));
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Returns item value based on the filter settings and item properties
        /// </summary>
        /// <param name="sourceItem">inventory item</param>
        /// <param name="slot">equipment index slot</param>
        /// <returns>item value</returns>
        public float ItemIndexCalculation(EquipmentElement sourceItem, EquipmentIndex slot)
        {
            // Given a item is "empty", return with big negative number
            if (sourceItem.IsEmpty)
                return -9999f;

            float value = 0f;
            CharacterSettings characterSettings = _characterData.GetCharacterSettings();

            // Calculation for armor items
            if (sourceItem.Item.HasArmorComponent)
            {
                return _bestEquipmentCalculator.CalculateArmorValue(sourceItem,
                    _characterData.GetCharacterSettings().FilterArmor[GetEquipmentSlot(slot)]);
            }

            // Calculation for weapon items
            if (sourceItem.Item.PrimaryWeapon != null)
            {
                return _bestEquipmentCalculator.CalculateWeaponsValue(sourceItem,
                    _characterData.GetCharacterSettings().FilterWeapon[GetEquipmentSlot(slot)]);
            }

            // Calculation for horse component
            if (sourceItem.Item.HasHorseComponent)
            {
                return _bestEquipmentCalculator.CalculateHorseValue(sourceItem,
                    _characterData.GetCharacterSettings().FilterMount);
            }

            return value;
        }

        /// <summary>
        /// Returns integer based on the equipment slot
        /// </summary>
        /// <param name="slot">equipment slot</param>
        /// <returns>int</returns>
        public static int GetEquipmentSlot(EquipmentIndex slot)
        {
            switch (slot)
            {
                case EquipmentIndex.Weapon0:
                    return 0;
                case EquipmentIndex.Weapon1:
                    return 1;
                case EquipmentIndex.Weapon2:
                    return 2;
                case EquipmentIndex.Weapon3:
                    return 3;
                case EquipmentIndex.Head:
                    return 0;
                case EquipmentIndex.Cape:
                    return 1;
                case EquipmentIndex.Body:
                    return 2;
                case EquipmentIndex.Gloves:
                    return 3;
                case EquipmentIndex.Leg:
                    return 4;
                case EquipmentIndex.Horse:
                    return 0;
                case EquipmentIndex.HorseHarness:
                    return 5;
                default:
                    return 0;
            }
        }

        public void SetCharacterData(CharacterData characterData)
        {
            _characterData = characterData;
        }

        public CharacterData GetCharacterData()
        {
            return _characterData;
        }
    }
}
