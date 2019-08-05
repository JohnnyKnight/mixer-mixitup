﻿using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Util;

namespace MixItUp.Base.ViewModel.Controls.Overlay
{
    public class OverlaySongRequestsListItemViewModel : OverlayListItemViewModelBase
    {
        public OverlaySongRequestsListItemViewModel()
            : base()
        {
            this.HTML = OverlaySongRequestsListItemModel.HTMLTemplate;
        }

        public OverlaySongRequestsListItemViewModel(OverlaySongRequestsListItemModel item)
            : base(item.TotalToShow, 0, item.Width, item.Height, item.TextFont, item.TextColor, item.BorderColor, item.BackgroundColor, item.Alignment, item.Effects.EntranceAnimation, item.Effects.ExitAnimation, item.HTML)
        { }

        public override OverlayItemModelBase GetOverlayItem()
        {
            if (this.Validate())
            {
                this.TextColor = ColorSchemes.GetColorCode(this.TextColor);
                this.BorderColor = ColorSchemes.GetColorCode(this.BorderColor);
                this.BackgroundColor = ColorSchemes.GetColorCode(this.BackgroundColor);

                return new OverlaySongRequestsListItemModel(this.HTML, totalToShow, this.Font, this.width, this.height, this.BorderColor, this.BackgroundColor, this.TextColor, this.alignment, this.entranceAnimation, this.exitAnimation);
            }
            return null;
        }
    }
}