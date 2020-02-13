﻿using MixItUp.Base.Model.User;
using MixItUp.Base.ViewModel.User;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.Currency
{
    /// <summary>
    /// Interaction logic for CurrencyUserEditorControl.xaml
    /// </summary>
    public partial class UserCurrencyIndividualEditorControl : UserControl
    {
        private UserCurrencyDataViewModel currencyData;

        private UserInventoryItemModel inventoryItem;
        private UserInventoryDataViewModel inventoryData;

        public UserCurrencyIndividualEditorControl(UserCurrencyDataViewModel currencyData)
            : this()
        {
            this.currencyData = currencyData;
        }

        public UserCurrencyIndividualEditorControl(UserInventoryItemModel inventoryItem, UserInventoryDataViewModel inventoryData)
            : this()
        {
            this.inventoryItem = inventoryItem;
            this.inventoryData = inventoryData;
        }

        private UserCurrencyIndividualEditorControl()
        {
            InitializeComponent();

            this.Loaded += UserCurrencyIndividualEditorControl_Loaded;
        }

        private void UserCurrencyIndividualEditorControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.currencyData != null)
            {
                this.NameTextBlock.Text = this.currencyData.Currency.Name;
                this.AmountTextBox.Text = this.currencyData.Amount.ToString();
            }
            else if (this.inventoryItem != null && this.inventoryData != null)
            {
                this.NameTextBlock.Text = this.inventoryItem.Name;
                this.AmountTextBox.Text = this.inventoryData.GetAmount(this.inventoryItem).ToString();
            }
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.AmountTextBox.Text) && int.TryParse(this.AmountTextBox.Text, out int amount) && amount >= 0)
            {
                if (this.currencyData != null)
                {
                    this.currencyData.Amount = amount;
                }
                else if (this.inventoryItem != null && this.inventoryData != null)
                {
                    this.inventoryData.SetAmount(this.inventoryItem, amount);
                }
            }
        }
    }
}
