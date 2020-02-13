﻿using MixItUp.Base.Model.User;
using MixItUp.Base.ViewModel.User;
using System.Windows.Controls;

namespace MixItUp.WPF.Controls.Currency
{
    /// <summary>
    /// Interaction logic for UserInventoryEditorControl.xaml
    /// </summary>
    public partial class UserInventoryEditorControl : UserControl
    {
        private UserInventoryModel inventory;
        private UserInventoryDataViewModel inventoryData;

        public UserInventoryEditorControl(UserInventoryModel inventory, UserInventoryDataViewModel inventoryData)
        {
            this.inventory = inventory;
            this.inventoryData = inventoryData;

            InitializeComponent();

            this.Loaded += UserInventoryEditorControl_Loaded;

            this.DataContext = this.inventory;
        }

        private void UserInventoryEditorControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.InventoryItemsStackPanel.Children.Clear();
            foreach (UserInventoryItemModel item in this.inventory.Items.Values)
            {
                this.InventoryItemsStackPanel.Children.Add(new UserCurrencyIndividualEditorControl(item, this.inventoryData));
            }
        }
    }
}
