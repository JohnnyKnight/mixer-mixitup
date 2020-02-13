﻿using MixItUp.Base;
using MixItUp.Base.Actions;
using MixItUp.Base.Model.User;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Requirement;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.Actions
{
    /// <summary>
    /// Interaction logic for CurrencyActionControl.xaml
    /// </summary>
    public partial class CurrencyActionControl : ActionControlBase
    {
        private CurrencyAction action;

        public CurrencyActionControl() : base() { InitializeComponent(); }

        public CurrencyActionControl(CurrencyAction action) : this() { this.action = action; }

        public override Task OnLoaded()
        {
            List<object> currencyInventoryList = new List<object>();
            currencyInventoryList.AddRange(ChannelSession.Settings.Currencies.Values);
            currencyInventoryList.AddRange(ChannelSession.Settings.Inventories.Values);
            this.CurrencyTypeComboBox.ItemsSource = currencyInventoryList;
            this.CurrencyActionTypeComboBox.ItemsSource = Enum.GetValues(typeof(CurrencyActionTypeEnum))
                .Cast<CurrencyActionTypeEnum>()
                .OrderBy(s => EnumLocalizationHelper.GetLocalizedName(s));
            this.CurrencyPermissionsAllowedComboBox.ItemsSource = RoleRequirementViewModel.BasicUserRoleAllowedValues;

            this.CurrencyPermissionsAllowedComboBox.SelectedIndex = 0;

            if (this.action != null)
            {
                if (this.action.CurrencyID != Guid.Empty && ChannelSession.Settings.Currencies.ContainsKey(this.action.CurrencyID))
                {
                    this.CurrencyTypeComboBox.SelectedItem = ChannelSession.Settings.Currencies[this.action.CurrencyID];
                }
                else if (this.action.InventoryID != Guid.Empty && ChannelSession.Settings.Inventories.ContainsKey(this.action.InventoryID))
                {
                    this.CurrencyTypeComboBox.SelectedItem = ChannelSession.Settings.Inventories[this.action.InventoryID];
                }
                this.CurrencyActionTypeComboBox.SelectedItem = this.action.CurrencyActionType;
                this.InventoryItemNameComboBox.Text = this.action.ItemName;
                this.CurrencyAmountTextBox.Text = this.action.Amount;
                this.CurrencyUsernameTextBox.Text = this.action.Username;
                this.CurrencyPermissionsAllowedComboBox.SelectedItem = EnumHelper.GetEnumName(this.action.RoleRequirement);
                this.DeductFromUserToggleButton.IsChecked = this.action.DeductFromUser;
            }
            return Task.FromResult(0);
        }

        public override ActionBase GetAction()
        {
            if (this.CurrencyTypeComboBox.SelectedIndex >= 0 && this.CurrencyActionTypeComboBox.SelectedIndex >= 0)
            {
                UserCurrencyModel currency = this.GetSelectedCurrency();
                UserInventoryModel inventory = this.GetSelectedInventory();
                CurrencyActionTypeEnum actionType = (CurrencyActionTypeEnum)this.CurrencyActionTypeComboBox.SelectedItem;

                if (actionType == CurrencyActionTypeEnum.ResetForAllUsers || actionType == CurrencyActionTypeEnum.ResetForUser || !string.IsNullOrEmpty(this.CurrencyAmountTextBox.Text))
                {
                    if (actionType == CurrencyActionTypeEnum.AddToSpecificUser)
                    {
                        if (string.IsNullOrEmpty(this.CurrencyUsernameTextBox.Text))
                        {
                            return null;
                        }
                    }

                    UserRoleEnum roleRequirement = UserRoleEnum.User;
                    if (actionType == CurrencyActionTypeEnum.AddToAllChatUsers || actionType == CurrencyActionTypeEnum.SubtractFromAllChatUsers)
                    {
                        if (this.CurrencyPermissionsAllowedComboBox.SelectedIndex < 0)
                        {
                            return null;
                        }
                        roleRequirement = EnumHelper.GetEnumValueFromString<UserRoleEnum>((string)this.CurrencyPermissionsAllowedComboBox.SelectedItem);
                    }

                    if (currency != null)
                    {
                        return new CurrencyAction(currency, actionType, this.CurrencyAmountTextBox.Text, username: this.CurrencyUsernameTextBox.Text,
                            roleRequirement: roleRequirement, deductFromUser: this.DeductFromUserToggleButton.IsChecked.GetValueOrDefault());
                    }
                    else if (inventory != null)
                    {
                        if (actionType == CurrencyActionTypeEnum.ResetForAllUsers || actionType == CurrencyActionTypeEnum.ResetForUser)
                        {
                            return new CurrencyAction(inventory, actionType);
                        }
                        else if (!string.IsNullOrEmpty(this.InventoryItemNameComboBox.Text))
                        {
                            return new CurrencyAction(inventory, actionType, this.InventoryItemNameComboBox.Text, this.CurrencyAmountTextBox.Text,
                                username: this.CurrencyUsernameTextBox.Text, roleRequirement: roleRequirement, deductFromUser: this.DeductFromUserToggleButton.IsChecked.GetValueOrDefault());
                        }
                    }
                }
            }
            return null;
        }

        private void CurrencyTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.CurrencyTypeComboBox.SelectedIndex >= 0)
            {
                this.CurrencyActionTypeComboBox.IsEnabled = this.CurrencyUsernameTextBox.IsEnabled = this.CurrencyAmountTextBox.IsEnabled =
                    this.DeductFromUserTextBlock.IsEnabled = this.DeductFromUserToggleButton.IsEnabled = true;

                if (this.GetSelectedInventory() != null)
                {
                    this.InventoryItemNameComboBox.Visibility = Visibility.Visible;
                    this.InventoryItemNameComboBox.ItemsSource = this.GetSelectedInventory().Items.Keys;
                }
                else
                {
                    this.InventoryItemNameComboBox.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CurrencyActionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.CurrencyActionTypeComboBox.SelectedIndex >= 0)
            {
                CurrencyActionTypeEnum actionType = (CurrencyActionTypeEnum)this.CurrencyActionTypeComboBox.SelectedItem;
                this.GiveToGrid.Visibility = (actionType == CurrencyActionTypeEnum.AddToSpecificUser || actionType == CurrencyActionTypeEnum.AddToAllChatUsers ||
                    actionType == CurrencyActionTypeEnum.SubtractFromSpecificUser || actionType == CurrencyActionTypeEnum.SubtractFromAllChatUsers) ?
                    Visibility.Visible : Visibility.Collapsed;

                this.CurrencyUsernameTextBox.Visibility = (actionType == CurrencyActionTypeEnum.AddToSpecificUser || actionType == CurrencyActionTypeEnum.SubtractFromSpecificUser) ?
                    Visibility.Visible : Visibility.Collapsed;

                this.CurrencyPermissionsAllowedComboBox.Visibility = (actionType == CurrencyActionTypeEnum.AddToAllChatUsers || actionType == CurrencyActionTypeEnum.SubtractFromAllChatUsers) ?
                    Visibility.Visible : Visibility.Collapsed;

                this.DeductFromUserTextBlock.IsEnabled = this.DeductFromUserToggleButton.IsEnabled =
                    (actionType == CurrencyActionTypeEnum.AddToSpecificUser || actionType == CurrencyActionTypeEnum.AddToAllChatUsers) ? true : false;

                this.InventoryItemNameComboBox.IsEnabled = (actionType != CurrencyActionTypeEnum.ResetForAllUsers && actionType != CurrencyActionTypeEnum.ResetForUser);

                this.CurrencyAmountTextBox.IsEnabled = (actionType != CurrencyActionTypeEnum.ResetForAllUsers && actionType != CurrencyActionTypeEnum.ResetForUser);
            }
        }

        private UserCurrencyModel GetSelectedCurrency()
        {
            if (this.CurrencyTypeComboBox.SelectedIndex >= 0 && this.CurrencyTypeComboBox.SelectedItem is UserCurrencyModel)
            {
                return (UserCurrencyModel)this.CurrencyTypeComboBox.SelectedItem;
            }
            return null;
        }

        private UserInventoryModel GetSelectedInventory()
        {
            if (this.CurrencyTypeComboBox.SelectedIndex >= 0 && this.CurrencyTypeComboBox.SelectedItem is UserInventoryModel)
            {
                return (UserInventoryModel)this.CurrencyTypeComboBox.SelectedItem;
            }
            return null;
        }
    }
}
