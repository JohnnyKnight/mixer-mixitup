﻿using Mixer.Base.Clients;
using Mixer.Base.Model.Channel;
using Mixer.Base.Model.Constellation;
using Mixer.Base.Model.Patronage;
using Mixer.Base.Model.User;
using MixItUp.Base.Model;
using MixItUp.Base.Model.Chat;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.Chat.Mixer;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json.Linq;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Services.Mixer
{
    public interface IMixerEventService
    {
        event EventHandler<ConstellationLiveEventModel> OnEventOccurred;

        LockedDictionary<Guid, MixerSkillPayloadModel> SkillEventsTriggered { get; }

        Task<ExternalServiceResult> Connect();
        Task Disconnect();
    }

    public class MixerEventService : MixerPlatformServiceBase, IMixerEventService
    {
        public static ConstellationEventType ChannelUpdateEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__update, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ChannelFollowEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__followed, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ChannelHostedEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__hosted, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ChannelSubscribedEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__subscribed, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ChannelResubscribedEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__resubscribed, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ChannelResubscribedSharedEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__resubShared, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ChannelSubscriptionGiftedEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__subscriptionGifted, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ChannelSkillEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__skill, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ChannelPatronageUpdateEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.channel__id__patronageUpdate, ChannelSession.MixerChannel.id); } }
        public static ConstellationEventType ProgressionLevelupEvent { get { return new ConstellationEventType(ConstellationEventTypeEnum.progression__id__levelup, ChannelSession.MixerChannel.id); } }

        private static readonly List<ConstellationEventTypeEnum> subscribedEvents = new List<ConstellationEventTypeEnum>()
        {
            ConstellationEventTypeEnum.channel__id__followed, ConstellationEventTypeEnum.channel__id__hosted, ConstellationEventTypeEnum.channel__id__subscribed,
            ConstellationEventTypeEnum.channel__id__resubscribed, ConstellationEventTypeEnum.channel__id__resubShared, ConstellationEventTypeEnum.channel__id__subscriptionGifted,
            ConstellationEventTypeEnum.channel__id__update, ConstellationEventTypeEnum.channel__id__skill, ConstellationEventTypeEnum.channel__id__patronageUpdate,
            ConstellationEventTypeEnum.progression__id__levelup,
        };

        public event EventHandler<ConstellationLiveEventModel> OnEventOccurred = delegate { };

        public LockedDictionary<Guid, MixerSkillPayloadModel> SkillEventsTriggered { get; private set; } = new LockedDictionary<Guid, MixerSkillPayloadModel>();

        public ConstellationClient Client { get; private set; }

        private List<PatronageMilestoneModel> allPatronageMilestones = new List<PatronageMilestoneModel>();
        private List<PatronageMilestoneModel> remainingPatronageMilestones = new List<PatronageMilestoneModel>();
        private SemaphoreSlim patronageMilestonesSemaphore = new SemaphoreSlim(1);

        public MixerEventService()
        {
            GlobalEvents.OnSparkUseOccurred += GlobalEvents_OnSparkUseOccurred;
            GlobalEvents.OnEmberUseOccurred += GlobalEvents_OnEmberUseOccurred;
            GlobalEvents.OnSkillUseOccurred += GlobalEvents_OnSkillUseOccurred;
        }

        public async Task<ExternalServiceResult> Connect()
        {
            if (ChannelSession.MixerUserConnection != null)
            {
                return await this.AttemptConnect(async () =>
                {
                    this.Client = await ConstellationClient.Create(ChannelSession.MixerUserConnection.Connection);
                    if (this.Client != null && await this.RunAsync(this.Client.Connect()))
                    {
                        this.Client.OnDisconnectOccurred += ConstellationClient_OnDisconnectOccurred;
                        if (ChannelSession.Settings.DiagnosticLogging)
                        {
                            this.Client.OnPacketSentOccurred += WebSocketClient_OnPacketSentOccurred;
                            this.Client.OnMethodOccurred += WebSocketClient_OnMethodOccurred;
                            this.Client.OnReplyOccurred += WebSocketClient_OnReplyOccurred;
                            this.Client.OnEventOccurred += WebSocketClient_OnEventOccurred;
                        }
                        this.Client.OnSubscribedEventOccurred += ConstellationClient_OnSubscribedEventOccurred;

                        await this.SubscribeToEvents(MixerEventService.subscribedEvents.Select(e => new ConstellationEventType(e, ChannelSession.MixerChannel.id)));

                        PatronageStatusModel patronageStatus = await ChannelSession.MixerUserConnection.GetPatronageStatus(ChannelSession.MixerChannel);
                        if (patronageStatus != null)
                        {
                            PatronagePeriodModel patronagePeriod = await ChannelSession.MixerUserConnection.GetPatronagePeriod(patronageStatus);
                            if (patronagePeriod != null)
                            {
                                this.allPatronageMilestones = new List<PatronageMilestoneModel>(patronagePeriod.milestoneGroups.SelectMany(mg => mg.milestones));
                                this.remainingPatronageMilestones = new List<PatronageMilestoneModel>(this.allPatronageMilestones.Where(m => m.target > patronageStatus.patronageEarned));
                                return new ExternalServiceResult();
                            }
                        }

                        await this.Disconnect();
                        return new ExternalServiceResult("Failed to get Mixer patronage information");
                    }
                    else
                    {
                        await this.Disconnect();
                        return new ExternalServiceResult("Failed to connect to Mixer Constellation");
                    }
                });
            }
            return new ExternalServiceResult("Mixer connection has not been established");
        }

        public async Task Disconnect()
        {
            await this.RunAsync(async () =>
            {
                if (this.Client != null)
                {
                    this.Client.OnDisconnectOccurred -= ConstellationClient_OnDisconnectOccurred;
                    if (ChannelSession.Settings.DiagnosticLogging)
                    {
                        this.Client.OnPacketSentOccurred -= WebSocketClient_OnPacketSentOccurred;
                        this.Client.OnMethodOccurred -= WebSocketClient_OnMethodOccurred;
                        this.Client.OnReplyOccurred -= WebSocketClient_OnReplyOccurred;
                        this.Client.OnEventOccurred -= WebSocketClient_OnEventOccurred;
                    }
                    this.Client.OnSubscribedEventOccurred -= ConstellationClient_OnSubscribedEventOccurred;

                    await this.RunAsync(this.Client.Disconnect());
                }
                this.Client = null;
            });
        }

        private async Task SubscribeToEvents(IEnumerable<ConstellationEventType> events) { await this.RunAsync(this.Client.SubscribeToEvents(events)); }

        private async Task UnsubscribeToEvents(IEnumerable<ConstellationEventType> events) { await this.RunAsync(this.Client.UnsubscribeToEvents(events)); }

        private async void ConstellationClient_OnSubscribedEventOccurred(object sender, ConstellationLiveEventModel e)
        {
            try
            {
                uint userID = 0;
                UserViewModel user = null;
                bool? followed = null;
                ChannelModel channel = null;

                JToken payloadToken;
                if (e.payload.TryGetValue("user", out payloadToken))
                {
                    UserModel userPayload = payloadToken.ToObject<UserModel>();
                    user = ChannelSession.Services.User.GetUserByMixerID(userPayload.id);
                    if (user == null)
                    {
                        user = new UserViewModel(userPayload);
                    }

                    JToken subscribeStartToken;
                    if (e.payload.TryGetValue("since", out subscribeStartToken))
                    {
                        user.Data.MixerSubscribeDate = subscribeStartToken.ToObject<DateTimeOffset>();
                    }

                    if (e.payload.TryGetValue("following", out JToken followedToken))
                    {
                        followed = (bool)followedToken;
                    }
                }
                else if (e.payload.TryGetValue("hoster", out payloadToken))
                {
                    channel = payloadToken.ToObject<ChannelModel>();
                    user = ChannelSession.Services.User.GetUserByMixerID(channel.userId);
                    if (user == null)
                    {
                        user = new UserViewModel(channel);
                    }
                }
                else if (e.payload.TryGetValue("userId", out JToken id))
                {
                    userID = id.ToObject<uint>();
                    user = ChannelSession.Services.User.GetUserByMixerID(userID);
                    if (user == null)
                    {
                        UserModel userModel = await ChannelSession.MixerUserConnection.GetUser(userID);
                        if (userModel != null)
                        {
                            user = await ChannelSession.Services.User.AddOrUpdateUser(userModel);
                        }
                    }
                }

                if (user != null)
                {
                    user.UpdateLastActivity();
                }

                if (e.channel.Equals(MixerEventService.ChannelUpdateEvent.ToString()))
                {
                    if (e.payload["online"] != null)
                    {
                        bool online = e.payload["online"].ToObject<bool>();
                        if (online)
                        {
                            await ChannelSession.Services.Events.PerformEvent(new EventTrigger(EventTypeEnum.MixerChannelStreamStart));
                        }
                        else
                        {
                            await ChannelSession.Services.Events.PerformEvent(new EventTrigger(EventTypeEnum.MixerChannelStreamStop));
                        }
                    }

                    if (e.payload["name"] != null)
                    {
                        string streamTitle = e.payload["name"].ToObject<string>();
                        if (!ChannelSession.Settings.RecentStreamTitles.Contains(streamTitle))
                        {
                            ChannelSession.Settings.RecentStreamTitles.Add(streamTitle);
                            if (ChannelSession.Settings.RecentStreamTitles.Count > 5)
                            {
                                ChannelSession.Settings.RecentStreamTitles.RemoveAt(0);
                            }
                        }
                    }
                }
                else if (e.channel.Equals(MixerEventService.ChannelFollowEvent.ToString()))
                {
                    if (user != null)
                    {
                        if (followed.GetValueOrDefault())
                        {
                            EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerChannelFollowed, user);
                            if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                            {
                                ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestFollowerUserData] = user.Data;

                                foreach (UserCurrencyModel currency in ChannelSession.Settings.Currencies.Values)
                                {
                                    currency.AddAmount(user.Data, currency.OnFollowBonus);
                                }

                                GlobalEvents.FollowOccurred(user);
                                await ChannelSession.Services.Events.PerformEvent(trigger);
                            }
                            await this.AddAlertChatMessage(user, string.Format("{0} Followed", user.Username));
                        }
                        else
                        {
                            EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerChannelUnfollowed, user);
                            if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                            {
                                GlobalEvents.UnfollowOccurred(user);
                                await ChannelSession.Services.Events.PerformEvent(trigger);
                            }
                            await this.AddAlertChatMessage(user, string.Format("{0} Unfollowed", user.Username));
                        }
                    }
                }
                else if (e.channel.Equals(MixerEventService.ChannelHostedEvent.ToString()))
                {
                    if (user != null)
                    {
                        int viewerCount = 0;
                        if (channel != null)
                        {
                            viewerCount = (int)channel.viewersCurrent;
                        }

                        bool isAutoHost = false;
                        if (e.payload.ContainsKey("auto"))
                        {
                            bool.TryParse(e.payload["auto"].ToString(), out isAutoHost);
                        }

                        EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerChannelHosted, user);
                        if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                        {
                            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestHostUserData] = user.Data;
                            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestHostViewerCountData] = viewerCount;

                            foreach (UserCurrencyModel currency in ChannelSession.Settings.Currencies.Values)
                            {
                                currency.AddAmount(user.Data, currency.OnHostBonus);
                            }

                            GlobalEvents.HostOccurred(new Tuple<UserViewModel, int>(user, viewerCount));

                            trigger.SpecialIdentifiers["hostviewercount"] = viewerCount.ToString();
                            trigger.SpecialIdentifiers["isautohost"] = isAutoHost.ToString();
                            await ChannelSession.Services.Events.PerformEvent(trigger);
                        }
                        await this.AddAlertChatMessage(user, string.Format("{0} Hosted With {1} Viewers", user.Username, viewerCount));
                    }
                }
                else if (e.channel.Equals(MixerEventService.ChannelSubscribedEvent.ToString()))
                {
                    if (user != null)
                    {
                        EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerChannelSubscribed, user);
                        if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                        {
                            user.Data.MixerSubscribeDate = DateTimeOffset.Now;
                            foreach (UserCurrencyModel currency in ChannelSession.Settings.Currencies.Values)
                            {
                                currency.AddAmount(user.Data, currency.OnSubscribeBonus);
                            }
                            user.Data.TotalMonthsSubbed++;

                            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestSubscriberUserData] = user.Data;
                            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestSubscriberSubMonthsData] = 1;

                            GlobalEvents.SubscribeOccurred(user);

                            await ChannelSession.Services.Events.PerformEvent(trigger);
                        }
                        await this.AddAlertChatMessage(user, string.Format("{0} Subscribed", user.Username));
                    }
                }
                else if (e.channel.Equals(MixerEventService.ChannelResubscribedEvent.ToString()) || e.channel.Equals(MixerEventService.ChannelResubscribedSharedEvent.ToString()))
                {
                    if (user != null)
                    {
                        int resubMonths = 0;
                        if (e.payload.TryGetValue("totalMonths", out JToken resubMonthsToken))
                        {
                            resubMonths = (int)resubMonthsToken;
                        }

                        EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerChannelResubscribed, user);
                        if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                        {
                            foreach (UserCurrencyModel currency in ChannelSession.Settings.Currencies.Values)
                            {
                                currency.AddAmount(user.Data, currency.OnSubscribeBonus);
                            }
                            user.Data.TotalMonthsSubbed++;

                            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestSubscriberUserData] = user.Data;
                            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestSubscriberSubMonthsData] = resubMonths;

                            GlobalEvents.ResubscribeOccurred(new Tuple<UserViewModel, int>(user, resubMonths));

                            trigger.SpecialIdentifiers["usersubmonths"] = resubMonths.ToString();
                            await ChannelSession.Services.Events.PerformEvent(trigger);
                        }
                        await this.AddAlertChatMessage(user, string.Format("{0} Re-Subscribed For {1} Months", user.Username, resubMonths));
                    }
                }
                else if (e.channel.Equals(MixerEventService.ChannelSubscriptionGiftedEvent.ToString()))
                {
                    if (e.payload.TryGetValue("gifterId", out JToken gifterID) && e.payload.TryGetValue("giftReceiverId", out JToken receiverID))
                    {
                        UserViewModel gifterUser = ChannelSession.Services.User.GetUserByMixerID(gifterID.ToObject<uint>());
                        if (gifterUser == null)
                        {
                            UserModel gifterUserModel = await ChannelSession.MixerUserConnection.GetUser(gifterID.ToObject<uint>());
                            if (gifterUserModel != null)
                            {
                                gifterUser = await ChannelSession.Services.User.AddOrUpdateUser(gifterUserModel);
                            }
                        }

                        UserViewModel receiverUser = ChannelSession.Services.User.GetUserByMixerID(receiverID.ToObject<uint>());
                        if (gifterUser == null)
                        {
                            UserModel receiverUserModel = await ChannelSession.MixerUserConnection.GetUser(receiverID.ToObject<uint>());
                            if (receiverUserModel != null)
                            {
                                receiverUser = await ChannelSession.Services.User.AddOrUpdateUser(receiverUserModel);
                            }
                        }

                        if (gifterUser != null && receiverUser != null)
                        {
                            EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerChannelSubscriptionGifted, gifterUser);
                            if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                            {
                                gifterUser.Data.TotalSubsGifted++;
                                receiverUser.Data.TotalSubsReceived++;
                                receiverUser.Data.TotalMonthsSubbed++;

                                ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestSubscriberUserData] = receiverUser.Data;
                                ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestSubscriberSubMonthsData] = 1;

                                trigger.Arguments.Add(receiverUser.Username);
                                await ChannelSession.Services.Events.PerformEvent(trigger);

                                await this.AddAlertChatMessage(gifterUser, string.Format("{0} Gifted A Subscription To {1}", gifterUser.Username, receiverUser.Username));

                                GlobalEvents.SubscriptionGiftedOccurred(gifterUser, receiverUser);
                            }
                        }
                    }
                }
                else if (e.channel.Equals(MixerEventService.ProgressionLevelupEvent.ToString()))
                {
                    if (user != null)
                    {
                        UserFanProgressionModel fanProgression = e.payload.ToObject<UserFanProgressionModel>();
                        if (fanProgression != null)
                        {
                            EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerFanProgressionLevelUp, user);
                            if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                            {
                                trigger.SpecialIdentifiers["userfanprogressionnext"] = fanProgression.level.nextLevelXp.GetValueOrDefault().ToString();
                                trigger.SpecialIdentifiers["userfanprogressionrank"] = fanProgression.level.level.ToString();
                                trigger.SpecialIdentifiers["userfanprogressioncolor"] = fanProgression.level.color.ToString();
                                trigger.SpecialIdentifiers["userfanprogressionimage"] = fanProgression.level.LargeGIFAssetURL.ToString();
                                trigger.SpecialIdentifiers["userfanprogression"] = fanProgression.level.currentXp.GetValueOrDefault().ToString();

                                await ChannelSession.Services.Events.PerformEvent(trigger);
                            }

                            user.MixerFanProgression = fanProgression;

                            GlobalEvents.ProgressionLevelUpOccurred(user);

                            foreach (UserCurrencyModel fanProgressionCurrency in ChannelSession.Settings.Currencies.Values.Where(c => c.IsTrackingFanProgression))
                            {
                                fanProgressionCurrency.SetAmount(user.Data, (int)fanProgression.level.level);
                            }
                        }
                    }
                }
                else if (e.channel.Equals(MixerEventService.ChannelSkillEvent.ToString()))
                {
                    MixerSkillPayloadModel skillPayload = e.payload.ToObject<MixerSkillPayloadModel>();
                    this.SkillEventsTriggered[skillPayload.executionId] = skillPayload;
                }
                else if (e.channel.Equals(MixerEventService.ChannelPatronageUpdateEvent.ToString()))
                {
                    PatronageStatusModel patronageStatus = e.payload.ToObject<PatronageStatusModel>();
                    if (patronageStatus != null)
                    {
                        GlobalEvents.PatronageUpdateOccurred(patronageStatus);

                        bool milestoneUpdateOccurred = await this.patronageMilestonesSemaphore.WaitAndRelease(() =>
                        {
                            return Task.FromResult(this.remainingPatronageMilestones.RemoveAll(m => m.target <= patronageStatus.patronageEarned) > 0);
                        });

                        if (milestoneUpdateOccurred)
                        {
                            PatronageMilestoneModel milestoneReached = this.allPatronageMilestones.OrderByDescending(m => m.target).FirstOrDefault(m => m.target <= patronageStatus.patronageEarned);
                            if (milestoneReached != null)
                            {
                                GlobalEvents.PatronageMilestoneReachedOccurred(milestoneReached);

                                EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerMilestoneReached, user);
                                if (ChannelSession.Services.Events.CanPerformEvent(trigger))
                                {
                                    trigger.SpecialIdentifiers[SpecialIdentifierStringBuilder.MilestoneSpecialIdentifierHeader + "amount"] = milestoneReached.target.ToString();
                                    trigger.SpecialIdentifiers[SpecialIdentifierStringBuilder.MilestoneSpecialIdentifierHeader + "reward"] = milestoneReached.PercentageAmountText();
                                }
                                await ChannelSession.Services.Events.PerformEvent(trigger);
                            }
                        }
                    }
                }

                if (this.OnEventOccurred != null)
                {
                    this.OnEventOccurred(this, e);
                }
            }
            catch (Exception ex) { Logger.Log(ex); }
        }

        private async void GlobalEvents_OnSparkUseOccurred(object sender, Tuple<UserViewModel, uint> sparkUsage)
        {
            sparkUsage.Item1.Data.TotalSparksSpent += (uint)sparkUsage.Item2;

            foreach (UserCurrencyModel sparkCurrency in ChannelSession.Settings.Currencies.Values.Where(c => c.IsTrackingSparks))
            {
                sparkCurrency.AddAmount(sparkUsage.Item1.Data, (int)sparkUsage.Item2);
            }

            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestSparkUsageUserData] = sparkUsage.Item1.Data;
            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestSparkUsageAmountData] = sparkUsage.Item2;

            EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerSparksUsed, sparkUsage.Item1);
            trigger.SpecialIdentifiers["sparkamount"] = sparkUsage.Item2.ToString();
            await ChannelSession.Services.Events.PerformEvent(trigger);
        }

        private async void GlobalEvents_OnEmberUseOccurred(object sender, UserEmberUsageModel emberUsage)
        {
            emberUsage.User.Data.TotalEmbersSpent += (uint)emberUsage.Amount;

            foreach (UserCurrencyModel emberCurrency in ChannelSession.Settings.Currencies.Values.Where(c => c.IsTrackingEmbers))
            {
                emberCurrency.AddAmount(emberUsage.User.Data, (int)emberUsage.Amount);
            }

            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestEmberUsageUserData] = emberUsage.User.Data;
            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestEmberUsageAmountData] = emberUsage.Amount;

            EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerSparksUsed, emberUsage.User);
            trigger.SpecialIdentifiers["emberamount"] = emberUsage.Amount.ToString();
            await ChannelSession.Services.Events.PerformEvent(trigger);
        }

        private async void GlobalEvents_OnSkillUseOccurred(object sender, MixerSkillChatMessageViewModel skill)
        {
            skill.User.Data.TotalSkillsUsed++;

            EventTrigger trigger = new EventTrigger(EventTypeEnum.MixerSkillUsed, skill.User, skill.Skill.GetSpecialIdentifiers());
            trigger.SpecialIdentifiers["skillmessage"] = skill.PlainTextMessage;
            await ChannelSession.Services.Events.PerformEvent(trigger);
        }

        private async Task AddAlertChatMessage(UserViewModel user, string message)
        {
            if (ChannelSession.Settings.ChatShowEventAlerts)
            {
                await ChannelSession.Services.Chat.AddMessage(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.Mixer, user, message, ChannelSession.Settings.ChatEventAlertsColorScheme));
            }
        }

        private async void ConstellationClient_OnDisconnectOccurred(object sender, WebSocketCloseStatus e)
        {
            ChannelSession.DisconnectionOccurred("Constellation");

            ExternalServiceResult result;
            do
            {
                await Task.Delay(2500);

                result = await this.Connect();
            }
            while (!result.Success);

            ChannelSession.ReconnectionOccurred("Constellation");
        }
    }
}