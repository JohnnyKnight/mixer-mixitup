﻿using MixItUp.Base.Services;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Overlay
{
    [DataContract]
    public class OverlayWidgetModel
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool IsEnabled { get; set; }

        [DataMember]
        public string OverlayName { get; set; }

        [DataMember]
        public OverlayItemModelBase Item { get; set; }

        [DataMember]
        public int RefreshTime { get; set; }

        public OverlayWidgetModel(string name, string overlayName, OverlayItemModelBase item, int refreshTime)
        {
            this.Name = name;
            this.OverlayName = overlayName;
            this.Item = item;
            this.RefreshTime = refreshTime;
            this.IsEnabled = true;
        }

        [JsonIgnore]
        public virtual bool SupportsTestData { get { return (this.Item != null) ? this.Item.SupportsTestData : false; } }

        [JsonIgnore]
        public virtual bool SupportsRefreshUpdating { get { return (this.Item != null) ? this.Item.SupportsRefreshUpdating : false; } }

        public async Task LoadTestData()
        {
            if (this.SupportsTestData)
            {
                await this.Item.LoadTestData();
                await this.UpdateItem();
                await this.Item.Reset();
            }
        }

        public async Task Initialize() { await this.Item.Initialize(); }

        public async Task Enable() { await this.Enable(await ChannelSession.GetCurrentUser(), new List<string>(), new Dictionary<string, string>()); }

        public async Task Enable(UserViewModel user, IEnumerable<string> arguments, Dictionary<string, string> extraSpecialIdentifiers)
        {
            this.IsEnabled = true;
            if (!this.Item.IsEnabled)
            {
                await this.Item.Enable();
                this.Item.OnChangeState += Item_OnChangeState;
                this.Item.OnSendUpdateRequired += Item_OnSendUpdateRequired;
                this.Item.OnHide += Item_OnHide;
            }
            await this.ShowItem();
        }

        public async Task Disable()
        {
            this.IsEnabled = false;
            if (this.Item.IsEnabled)
            {
                await this.Item.Disable();
                this.Item.OnChangeState -= Item_OnChangeState;
                this.Item.OnSendUpdateRequired -= Item_OnSendUpdateRequired;
                this.Item.OnHide -= Item_OnHide;
            }
            await this.HideItem();
        }

        public async Task LoadCachedData() { await this.Item.LoadCachedData(); }

        public async Task ShowItem() { await this.ShowItem(await ChannelSession.GetCurrentUser(), new List<string>(), new Dictionary<string, string>()); }

        public async Task ShowItem(UserViewModel user, IEnumerable<string> arguments, Dictionary<string, string> extraSpecialIdentifiers)
        {
            IOverlayEndpointService overlay = this.GetOverlay();
            if (overlay != null)
            {
                await overlay.ShowItem(this.Item, user, arguments, extraSpecialIdentifiers);
            }
        }

        public async Task UpdateItem() { await this.UpdateItem(await ChannelSession.GetCurrentUser(), new List<string>(), new Dictionary<string, string>()); }

        public async Task UpdateItem(UserViewModel user, IEnumerable<string> arguments, Dictionary<string, string> extraSpecialIdentifiers)
        {
            IOverlayEndpointService overlay = this.GetOverlay();
            if (overlay != null)
            {
                await overlay.UpdateItem(this.Item, user, arguments, extraSpecialIdentifiers);
            }
        }

        public async Task HideItem()
        {
            IOverlayEndpointService overlay = this.GetOverlay();
            if (overlay != null)
            {
                await overlay.HideItem(this.Item);
            }
        }

        private IOverlayEndpointService GetOverlay()
        {
            string overlayName = (string.IsNullOrEmpty(this.OverlayName)) ? ChannelSession.Services.Overlay.DefaultOverlayName : this.OverlayName;
            return ChannelSession.Services.Overlay.GetOverlay(overlayName);
        }

        private async void Item_OnChangeState(object sender, bool state)
        {
            if (state)
            {
                await this.Enable();
            }
            else
            {
                await this.Disable();
            }
        }

        private async void Item_OnSendUpdateRequired(object sender, System.EventArgs e)
        {
            await this.UpdateItem();
        }

        private async void Item_OnHide(object sender, System.EventArgs e)
        {
            await this.HideItem();
        }
    }
}
