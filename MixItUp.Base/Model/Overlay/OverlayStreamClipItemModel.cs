﻿using Mixer.Base.Model.Clips;
using MixItUp.Base.Actions;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Overlay
{
    [DataContract]
    public class OverlayStreamClipItemModel : OverlayFileItemModelBase
    {
        [DataMember]
        public int Volume { get; set; }

        [DataMember]
        public double Duration { get; set; }

        private ClipModel lastClip = null;
        private string lastClipURL = null;

        public OverlayStreamClipItemModel() : base() { }

        public OverlayStreamClipItemModel(int width, int height, int volume, OverlayItemEffectEntranceAnimationTypeEnum entranceAnimation, OverlayItemEffectExitAnimationTypeEnum exitAnimation)
            : base(OverlayItemModelTypeEnum.StreamClip, string.Empty, width, height)
        {
            this.Width = width;
            this.Height = height;
            this.Volume = volume;
            this.Effects = new OverlayItemEffectsModel(entranceAnimation, OverlayItemEffectVisibleAnimationTypeEnum.None, exitAnimation, 0);
        }

        [DataMember]
        public override string FullLink { get { return this.lastClipURL; } set { } }

        [DataMember]
        public override string FileType { get { return "video"; } set { } }

        [DataMember]
        public double VolumeDecimal { get { return ((double)this.Volume / 100.0); } set { } }

        [JsonIgnore]
        public override bool SupportsTestData { get { return true; } }

        public override Task LoadTestData()
        {
            this.lastClip = new ClipModel()
            {
                contentLocators = new List<ClipLocatorModel>()
                {
                    new ClipLocatorModel()
                    {
                        locatorType = MixerClipsAction.VideoFileContentLocatorType,
                        uri = "https://raw.githubusercontent.com/SaviorXTanren/mixer-mixitup/master/Wiki/MixerTestClip/manifest.m3u8"
                    }
                },
                durationInSeconds = 10
            };
            return Task.FromResult(0);
        }

        public override async Task Enable()
        {
            GlobalEvents.OnMixerClipCreated += GlobalEvents_OnMixerClipCreated;

            await base.Enable();
        }

        public override async Task Disable()
        {
            GlobalEvents.OnMixerClipCreated -= GlobalEvents_OnMixerClipCreated;

            await base.Disable();
        }

        public override async Task<JObject> GetProcessedItem(UserViewModel user, IEnumerable<string> arguments, Dictionary<string, string> extraSpecialIdentifiers)
        {
            JObject jobj = null;
            if (this.lastClip != null)
            {
                ClipLocatorModel clipLocator = this.lastClip.contentLocators.FirstOrDefault(cl => cl.locatorType.Equals(MixerClipsAction.VideoFileContentLocatorType));
                if (clipLocator != null)
                {
                    this.lastClipURL = clipLocator.uri;
                    this.Effects.Duration = Math.Max(0, this.lastClip.durationInSeconds - 1);
                    jobj = await base.GetProcessedItem(user, arguments, extraSpecialIdentifiers);
                }

                this.lastClip = null;
                this.lastClipURL = null;
            }
            return jobj;
        }

        private void GlobalEvents_OnMixerClipCreated(object sender, ClipModel clip)
        {
            this.lastClip = clip;
            this.SendUpdateRequired();
        }
    }
}
