﻿using FluentNHibernate.Mapping;
using JMMServer.Entities;
using Shoko.Models.Server;

namespace JMMServer.Mappings
{
    public class VideoLocal_UserMap : ClassMap<VideoLocal_User>
    {
        public VideoLocal_UserMap()
        {
            Not.LazyLoad();
            Id(x => x.VideoLocal_UserID);

            Map(x => x.JMMUserID).Not.Nullable();
            Map(x => x.VideoLocalID).Not.Nullable();
            Map(x => x.WatchedDate);
            Map(x => x.ResumePosition).Not.Nullable();
        }
    }
}