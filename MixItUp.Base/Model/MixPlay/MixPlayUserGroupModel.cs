﻿using Mixer.Base.Model.MixPlay;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using StreamingClient.Base.Util;
using System.Runtime.Serialization;

namespace MixItUp.Base.Model.MixPlay
{
    [DataContract]
    public class MixPlayUserGroupModel
    {
        public const string DefaultName = "default";

        public MixPlayUserGroupModel() { }

        public MixPlayUserGroupModel(UserRoleEnum associatedUserRole)
            : this((associatedUserRole != UserRoleEnum.User) ? EnumHelper.GetEnumName(associatedUserRole) : DefaultName)
        {
            this.AssociatedUserRole = associatedUserRole;
        }

        public MixPlayUserGroupModel(string groupName) : this(groupName, MixPlayUserGroupModel.DefaultName) { }

        public MixPlayUserGroupModel(string groupName, string defaultScene)
        {
            this.GroupName = groupName;
            this.DefaultScene = defaultScene;
            this.AssociatedUserRole = UserRoleEnum.Custom;
        }

        public MixPlayUserGroupModel(MixPlayGroupModel group) : this(group.groupID, group.sceneID) { }

        [DataMember]
        public string GroupName { get; set; }
        [DataMember]
        public string DefaultScene { get; set; }
        [DataMember]
        public UserRoleEnum AssociatedUserRole { get; set; }

        [JsonIgnore]
        public string CurrentScene { get; set; }
    }
}
