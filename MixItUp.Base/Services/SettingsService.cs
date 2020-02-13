﻿using Mixer.Base.Clients;
using Mixer.Base.Model.Channel;
using MixItUp.Base.Actions;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.MixPlay;
using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Model.Settings;
using MixItUp.Base.Model.User;
using MixItUp.Base.Remote.Models;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json.Linq;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public enum SettingsBackupRateEnum
    {
        None = 0,
        Daily,
        Weekly,
        Monthly,
    }

    public interface ISettingsService
    {
        void Initialize();

        Task<IEnumerable<SettingsV2Model>> GetAllSettings();

        Task<SettingsV2Model> Create(ExpandedChannelModel channel, bool isStreamer);

        Task Initialize(SettingsV2Model settings);

        Task<bool> SaveAndValidate(SettingsV2Model settings);

        Task Save(SettingsV2Model settings);

        Task SavePackagedBackup(SettingsV2Model settings, string filePath);

        Task PerformBackupIfApplicable(SettingsV2Model settings);
    }

    public class SettingsService : ISettingsService
    {
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public void Initialize() { Directory.CreateDirectory(SettingsV2Model.SettingsDirectoryName); }

        public async Task<IEnumerable<SettingsV2Model>> GetAllSettings()
        {
            // Check for old V1 settings
#pragma warning disable CS0612 // Type or member is obsolete
            List<SettingsV1Model> oldSettings = new List<SettingsV1Model>();

            string[] filePaths = Directory.GetFiles(SettingsV2Model.SettingsDirectoryName);
            if (filePaths.Any(filePath => filePath.EndsWith(SettingsV1Model.SettingsFileExtension)))
            {
                await DialogHelper.ShowMessage("We've detected version 1 settings in your installation and will now upgrade them to version 2. This will take some time depending on how large your settings data is, particularly the number of individual users we have data for from your stream."
                    + Environment.NewLine + Environment.NewLine + "If you have a large amount of user data, we suggest going to grab a cup of coffee and come back in a few minutes after dismissing this message. :)");

                foreach (string filePath in filePaths)
                {
                    if (filePath.EndsWith(SettingsV1Model.SettingsFileExtension))
                    {
                        try
                        {
                            SettingsV1Model setting = await SettingsV1Upgrader.UpgradeSettingsToLatest(filePath);

                            string oldSettingsPath = Path.Combine(SettingsV2Model.SettingsDirectoryName, "Old");
                            Directory.CreateDirectory(oldSettingsPath);

                            await ChannelSession.Services.FileService.CopyFile(filePath, Path.Combine(oldSettingsPath, Path.GetFileName(filePath)));
                            await ChannelSession.Services.FileService.CopyFile(setting.DatabaseFilePath, Path.Combine(oldSettingsPath, setting.DatabaseFileName));

                            await ChannelSession.Services.FileService.DeleteFile(filePath);
                            await ChannelSession.Services.FileService.DeleteFile(setting.DatabaseFilePath);
                            await ChannelSession.Services.FileService.DeleteFile(filePath + ".backup");
                            await ChannelSession.Services.FileService.DeleteFile(setting.DatabaseFilePath + ".backup");
                        }
                        catch (Exception ex) { Logger.Log(ex); }
                    }
                }
            }
#pragma warning restore CS0612 // Type or member is obsolete

            List<SettingsV2Model> settings = new List<SettingsV2Model>();
            foreach (string filePath in Directory.GetFiles(SettingsV2Model.SettingsDirectoryName))
            {
                if (filePath.EndsWith(SettingsV2Model.SettingsFileExtension))
                {
                    SettingsV2Model setting = null;
                    try
                    {
                        setting = await this.LoadSettings(filePath);
                        if (setting != null)
                        {
                            settings.Add(setting);
                        }
                    }
                    catch (Exception ex) { Logger.Log(ex); }
                }
            }
            return settings;
        }

        public Task<SettingsV2Model> Create(ExpandedChannelModel channel, bool isStreamer)
        {
            SettingsV2Model settings = new SettingsV2Model(channel, isStreamer);
            return Task.FromResult(settings);
        }

        public async Task Initialize(SettingsV2Model settings)
        {
            await settings.Initialize();
        }

        public async Task<bool> SaveAndValidate(SettingsV2Model settings)
        {
            try
            {
                await this.Save(settings);
                SettingsV2Model loadedSettings = await this.LoadSettings(settings.SettingsFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return false;
        }

        public async Task Save(SettingsV2Model settings)
        {
            await semaphore.WaitAndRelease(async () =>
            {
                settings.CopyLatestValues();
                await SerializerHelper.SerializeToFile(settings.SettingsFilePath, settings);
                await settings.SaveDatabaseData();
            });
        }

        public async Task SavePackagedBackup(SettingsV2Model settings, string filePath)
        {
            await this.Save(ChannelSession.Settings);

            if (Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (ZipArchive zipFile = ZipFile.Open(filePath, ZipArchiveMode.Create))
                {
                    zipFile.CreateEntryFromFile(settings.SettingsFilePath, Path.GetFileName(settings.SettingsFilePath));
                    zipFile.CreateEntryFromFile(settings.DatabaseFilePath, Path.GetFileName(settings.DatabaseFilePath));
                }
            }
        }

        public async Task PerformBackupIfApplicable(SettingsV2Model settings)
        {
            if (settings.SettingsBackupRate != SettingsBackupRateEnum.None && !string.IsNullOrEmpty(settings.SettingsBackupLocation))
            {
                DateTimeOffset newResetDate = settings.SettingsLastBackup;

                if (settings.SettingsBackupRate == SettingsBackupRateEnum.Daily) { newResetDate = newResetDate.AddDays(1); }
                else if (settings.SettingsBackupRate == SettingsBackupRateEnum.Weekly) { newResetDate = newResetDate.AddDays(7); }
                else if (settings.SettingsBackupRate == SettingsBackupRateEnum.Monthly) { newResetDate = newResetDate.AddMonths(1); }

                if (newResetDate < DateTimeOffset.Now)
                {
                    string filePath = Path.Combine(settings.SettingsBackupLocation, settings.MixerChannelID + "-Backup-" + DateTimeOffset.Now.ToString("MM-dd-yyyy") + "." + SettingsV2Model.SettingsBackupFileExtension);

                    await this.SavePackagedBackup(settings, filePath);

                    settings.SettingsLastBackup = DateTimeOffset.Now;
                }
            }
        }

        private async Task<SettingsV2Model> LoadSettings(string filePath)
        {
            return await SettingsV2Upgrader.UpgradeSettingsToLatest(filePath);
        }
    }

    public static class SettingsV2Upgrader
    {
        public static async Task<SettingsV2Model> UpgradeSettingsToLatest(string filePath)
        {
            int currentVersion = await GetSettingsVersion(filePath);
            if (currentVersion < 0)
            {
                // Settings file is invalid, we can't use this
                return null;
            }
            else if (currentVersion > SettingsV2Model.LatestVersion)
            {
                // Future build, like a preview build, we can't load this
                return null;
            }
            else if (currentVersion < SettingsV2Model.LatestVersion)
            {
                SettingsV2Model settings = await SerializerHelper.DeserializeFromFile<SettingsV2Model>(filePath, ignoreErrors: true);
            }
            return await SerializerHelper.DeserializeFromFile<SettingsV2Model>(filePath, ignoreErrors: true);
        }

        public static async Task<int> GetSettingsVersion(string filePath)
        {
            string fileData = await ChannelSession.Services.FileService.ReadFile(filePath);
            if (string.IsNullOrEmpty(fileData))
            {
                return -1;
            }
            JObject settingsJObj = JObject.Parse(fileData);
            return (int)settingsJObj["Version"];
        }
    }

    #region Settings V1 Upgrader

#pragma warning disable CS0612 // Type or member is obsolete
    internal static class SettingsV1Upgrader
    {
        internal static async Task<SettingsV1Model> UpgradeSettingsToLatest(string filePath)
        {
            await SettingsV1Upgrader.Version39Upgrade(filePath);

            SettingsV1Model settings = await SerializerHelper.DeserializeFromFile<SettingsV1Model>(filePath, ignoreErrors: true);
            settings.Version = SettingsV1Model.LatestVersion;
            await SerializerHelper.SerializeToFile<SettingsV1Model>(filePath, settings);

            return settings;
        }

        public static async Task Version39Upgrade(string filePath)
        {
            SettingsV1Model oldSettings = await SerializerHelper.DeserializeFromFile<SettingsV1Model>(filePath, ignoreErrors: true);
            await oldSettings.LoadUserData();

            SettingsV2Model newSettings = await SerializerHelper.DeserializeFromFile<SettingsV2Model>(filePath, ignoreErrors: true);
            await ChannelSession.Services.Settings.Initialize(newSettings);

            newSettings.MixerUserOAuthToken = oldSettings.OAuthToken;
            newSettings.MixerBotOAuthToken = oldSettings.BotOAuthToken;
            if (oldSettings.Channel != null)
            {
                newSettings.Name = oldSettings.Channel.token;
                newSettings.MixerChannelID = oldSettings.Channel.id;
            }
            newSettings.TelemetryUserID = oldSettings.TelemetryUserId;

            newSettings.RemoteProfileBoards = new Dictionary<Guid, RemoteProfileBoardsModel>(oldSettings.remoteProfileBoardsInternal);
            newSettings.FilteredWords = new List<string>(oldSettings.filteredWordsInternal);
            newSettings.BannedWords = new List<string>(oldSettings.bannedWordsInternal);
            newSettings.MixPlayUserGroups = new Dictionary<uint, List<MixPlayUserGroupModel>>(oldSettings.mixPlayUserGroupsInternal);
            newSettings.OverlayWidgets = new List<OverlayWidgetModel>(oldSettings.overlayWidgetModelsInternal);
            newSettings.Currencies = oldSettings.currenciesInternal;
            newSettings.Inventories = oldSettings.inventoriesInternal;
            newSettings.CooldownGroups = new Dictionary<string, int>(oldSettings.cooldownGroupsInternal);
            newSettings.PreMadeChatCommandSettings = new List<PreMadeChatCommandSettings>(oldSettings.preMadeChatCommandSettingsInternal);

            newSettings.ChatCommands = new LockedList<ChatCommand>(oldSettings.chatCommandsInternal);
            newSettings.EventCommands = new LockedList<EventCommand>(oldSettings.eventCommandsInternal);
            newSettings.MixPlayCommands = new LockedList<MixPlayCommand>(oldSettings.mixPlayCmmandsInternal);
            newSettings.TimerCommands = new LockedList<TimerCommand>(oldSettings.timerCommandsInternal);
            newSettings.ActionGroupCommands = new LockedList<ActionGroupCommand>(oldSettings.actionGroupCommandsInternal);
            newSettings.GameCommands = new LockedList<GameCommandBase>(oldSettings.gameCommandsInternal);
            newSettings.Quotes = new LockedList<UserQuoteViewModel>(oldSettings.userQuotesInternal);

            foreach (UserDataModel data in oldSettings.UserData.Values)
            {
                newSettings.UserData[data.ID] = data;
            }

            int quoteID = 1;
            foreach (UserQuoteViewModel quote in oldSettings.userQuotesInternal)
            {
                newSettings.Quotes.Add(quote);
                quote.ID = quoteID;
                quoteID++;
            }

            foreach (EventCommand command in newSettings.EventCommands)
            {
                if (command.OtherEventType != OtherEventTypeEnum.None)
                {
                    switch (command.OtherEventType)
                    {
                        case OtherEventTypeEnum.MixerChannelStreamStart:
                            command.EventCommandType = EventTypeEnum.MixerChannelStreamStart;
                            break;
                        case OtherEventTypeEnum.MixerChannelStreamStop:
                            command.EventCommandType = EventTypeEnum.MixerChannelStreamStop;
                            break;
                        case OtherEventTypeEnum.ChatUserUnfollow:
                            command.EventCommandType = EventTypeEnum.MixerChannelUnfollowed;
                            break;
                        case OtherEventTypeEnum.MixerSparksUsed:
                            command.EventCommandType = EventTypeEnum.MixerSparksUsed;
                            break;
                        case OtherEventTypeEnum.MixerEmbersUsed:
                            command.EventCommandType = EventTypeEnum.MixerEmbersUsed;
                            break;
                        case OtherEventTypeEnum.MixerSkillUsed:
                            command.EventCommandType = EventTypeEnum.MixerSkillUsed;
                            break;
                        case OtherEventTypeEnum.ChatUserFirstJoin:
                            command.EventCommandType = EventTypeEnum.ChatUserFirstJoin;
                            break;
                        case OtherEventTypeEnum.ChatUserJoined:
                            command.EventCommandType = EventTypeEnum.ChatUserJoined;
                            break;
                        case OtherEventTypeEnum.ChatUserLeft:
                            command.EventCommandType = EventTypeEnum.ChatUserLeft;
                            break;
                        case OtherEventTypeEnum.ChatUserPurge:
                            command.EventCommandType = EventTypeEnum.ChatUserPurge;
                            break;
                        case OtherEventTypeEnum.ChatUserBan:
                            command.EventCommandType = EventTypeEnum.ChatUserBan;
                            break;
                        case OtherEventTypeEnum.ChatMessageReceived:
                            command.EventCommandType = EventTypeEnum.ChatMessageReceived;
                            break;
                        case OtherEventTypeEnum.ChatMessageDeleted:
                            command.EventCommandType = EventTypeEnum.ChatMessageDeleted;
                            break;
                        case OtherEventTypeEnum.StreamlabsDonation:
                            command.EventCommandType = EventTypeEnum.StreamlabsDonation;
                            break;
                        case OtherEventTypeEnum.TipeeeStreamDonation:
                            command.EventCommandType = EventTypeEnum.TipeeeStreamDonation;
                            break;
                        case OtherEventTypeEnum.TreatStreamDonation:
                            command.EventCommandType = EventTypeEnum.TreatStreamDonation;
                            break;
                        case OtherEventTypeEnum.StreamJarDonation:
                            command.EventCommandType = EventTypeEnum.StreamJarDonation;
                            break;
                        case OtherEventTypeEnum.TiltifyDonation:
                            command.EventCommandType = EventTypeEnum.TiltifyDonation;
                            break;
                        case OtherEventTypeEnum.ExtraLifeDonation:
                            command.EventCommandType = EventTypeEnum.ExtraLifeDonation;
                            break;
                        case OtherEventTypeEnum.JustGivingDonation:
                            command.EventCommandType = EventTypeEnum.JustGivingDonation;
                            break;
                        case OtherEventTypeEnum.PatreonSubscribed:
                            command.EventCommandType = EventTypeEnum.PatreonSubscribed;
                            break;
                        case OtherEventTypeEnum.StreamlootsCardRedeemed:
                            command.EventCommandType = EventTypeEnum.StreamlootsCardRedeemed;
                            break;
                        case OtherEventTypeEnum.StreamlootsPackPurchased:
                            command.EventCommandType = EventTypeEnum.StreamlootsPackPurchased;
                            break;
                        case OtherEventTypeEnum.StreamlootsPackGifted:
                            command.EventCommandType = EventTypeEnum.StreamlootsPackGifted;
                            break;
                    }
                }
                else
                {
                    switch (command.EventType)
                    {
                        case ConstellationEventTypeEnum.channel__id__followed:
                            command.EventCommandType = EventTypeEnum.MixerChannelFollowed;
                            break;
                        case ConstellationEventTypeEnum.channel__id__hosted:
                            command.EventCommandType = EventTypeEnum.MixerChannelHosted;
                            break;
                        case ConstellationEventTypeEnum.channel__id__subscribed:
                            command.EventCommandType = EventTypeEnum.MixerChannelSubscribed;
                            break;
                        case ConstellationEventTypeEnum.channel__id__resubscribed:
                            command.EventCommandType = EventTypeEnum.MixerChannelResubscribed;
                            break;
                        case ConstellationEventTypeEnum.channel__id__subscriptionGifted:
                            command.EventCommandType = EventTypeEnum.MixerChannelSubscriptionGifted;
                            break;
                        case ConstellationEventTypeEnum.progression__id__levelup:
                            command.EventCommandType = EventTypeEnum.MixerFanProgressionLevelUp;
                            break;
                    }
                }
            }

            await ChannelSession.Services.Settings.Save(newSettings);

            newSettings = await SerializerHelper.DeserializeFromFile<SettingsV2Model>(newSettings.SettingsFilePath, ignoreErrors: true);
            await ChannelSession.Services.Settings.Initialize(newSettings);

            foreach (CommandBase command in GetAllCommands(newSettings))
            {
                foreach (ActionBase action in command.Actions)
                {
                    if (action is InteractiveAction)
                    {
                        InteractiveAction iaction = (InteractiveAction)action;
                        iaction.CooldownAmountString = iaction.CooldownAmount.ToString();
                    }
                    else if (action is CounterAction)
                    {
                        CounterAction cAction = (CounterAction)action;
                        newSettings.Counters[cAction.CounterName] = new CounterModel(cAction.CounterName);
                        newSettings.Counters[cAction.CounterName].SaveToFile = cAction.SaveToFile;
                        newSettings.Counters[cAction.CounterName].ResetOnLoad = cAction.ResetOnLoad;
                        if (File.Exists(newSettings.Counters[cAction.CounterName].GetCounterFilePath()))
                        {
                            string data = await ChannelSession.Services.FileService.ReadFile(newSettings.Counters[cAction.CounterName].GetCounterFilePath());
                            if (double.TryParse(data, out double amount))
                            {
                                newSettings.Counters[cAction.CounterName].Amount = amount;
                            }
                        }
                    }
                }
            }
        }

        private static UserRoleEnum ConvertLegacyRoles(UserRoleEnum legacyRole)
        {
            int legacyRoleID = (int)legacyRole;
            if ((int)UserRoleEnum.Custom == legacyRoleID)
            {
                return UserRoleEnum.Custom;
            }
            else
            {
                return (UserRoleEnum)(legacyRoleID * 10);
            }
        }

        private static IEnumerable<CommandBase> GetAllCommands(SettingsV2Model settings)
        {
            List<CommandBase> commands = new List<CommandBase>();

            commands.AddRange(settings.ChatCommands);
            commands.AddRange(settings.EventCommands);
            commands.AddRange(settings.MixPlayCommands);
            commands.AddRange(settings.TimerCommands);
            commands.AddRange(settings.ActionGroupCommands);
            commands.AddRange(settings.GameCommands);

            foreach (UserDataModel userData in settings.UserData.Values)
            {
                commands.AddRange(userData.CustomCommands);
                if (userData.EntranceCommand != null)
                {
                    commands.Add(userData.EntranceCommand);
                }
            }

            foreach (GameCommandBase gameCommand in settings.GameCommands)
            {
                commands.AddRange(gameCommand.GetAllInnerCommands());
            }

            foreach (UserCurrencyModel currency in settings.Currencies.Values)
            {
                if (currency.RankChangedCommand != null)
                {
                    commands.Add(currency.RankChangedCommand);
                }
            }

            foreach (UserInventoryModel inventory in settings.Inventories.Values)
            {
                commands.Add(inventory.ItemsBoughtCommand);
                commands.Add(inventory.ItemsSoldCommand);
            }

            foreach (OverlayWidgetModel widget in settings.OverlayWidgets)
            {
                if (widget.Item is OverlayStreamBossItemModel)
                {
                    OverlayStreamBossItemModel item = ((OverlayStreamBossItemModel)widget.Item);
                    if (item.NewStreamBossCommand != null)
                    {
                        commands.Add(item.NewStreamBossCommand);
                    }
                }
                else if (widget.Item is OverlayProgressBarItemModel)
                {
                    OverlayProgressBarItemModel item = ((OverlayProgressBarItemModel)widget.Item);
                    if (item.GoalReachedCommand != null)
                    {
                        commands.Add(item.GoalReachedCommand);
                    }
                }
                else if (widget.Item is OverlayTimerItemModel)
                {
                    OverlayTimerItemModel item = ((OverlayTimerItemModel)widget.Item);
                    if (item.TimerCompleteCommand != null)
                    {
                        commands.Add(item.TimerCompleteCommand);
                    }
                }
            }

            commands.Add(settings.GameQueueUserJoinedCommand);
            commands.Add(settings.GameQueueUserSelectedCommand);
            commands.Add(settings.GiveawayStartedReminderCommand);
            commands.Add(settings.GiveawayUserJoinedCommand);
            commands.Add(settings.GiveawayWinnerSelectedCommand);
            commands.Add(settings.ModerationStrike1Command);
            commands.Add(settings.ModerationStrike2Command);
            commands.Add(settings.ModerationStrike3Command);

            return commands.Where(c => c != null);
        }

        private static T GetOptionValue<T>(JObject jobj, string key)
        {
            if (jobj[key] != null)
            {
                return jobj[key].ToObject<T>();
            }
            return default(T);
        }
    }
#pragma warning restore CS0612 // Type or member is obsolete

    #endregion Settings V1 Upgrader
}
