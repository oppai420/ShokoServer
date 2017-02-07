﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JMMServer.Databases;
using JMMServer.Entities;
using JMMServer.Repositories.NHibernate;
using NHibernate;
using NLog;
using NutzCode.InMemoryIndex;

namespace JMMServer.Repositories.Cached
{
    public class AnimeSeriesRepository : BaseCachedRepository<AnimeSeries, int>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private PocoIndex<int, AnimeSeries, int> AniDBIds;
        private PocoIndex<int, AnimeSeries, int> Groups;

        private ChangeTracker<int> Changes = new ChangeTracker<int>();

        private AnimeSeriesRepository()
        {
            BeginDeleteCallback = (cr) =>
            {
                RepoFactory.AnimeSeries_User.Delete(RepoFactory.AnimeSeries_User.GetBySeriesID(cr.AnimeSeriesID));
                Changes.Remove(cr.AnimeSeriesID);
            };
            EndDeleteCallback = (cr) =>
            {
                cr.DeleteFromFilters();
                if (cr.AnimeGroupID > 0)
                {
                    logger.Trace("Updating group stats by group from AnimeSeriesRepository.Delete: {0}", cr.AnimeGroupID);
                    AnimeGroup oldGroup = RepoFactory.AnimeGroup.GetByID(cr.AnimeGroupID);
                    if (oldGroup != null)
                        RepoFactory.AnimeGroup.Save(oldGroup, true, true);
                }
            };

        }

        public static AnimeSeriesRepository Create()
        {
            return new AnimeSeriesRepository();
        }

        protected override int SelectKey(AnimeSeries entity)
        {
            return entity.AnimeSeriesID;
        }

        public override void PopulateIndexes()
        {
            Changes.AddOrUpdateRange(Cache.Keys);
            AniDBIds = Cache.CreateIndex(a => a.AniDB_ID);
            Groups = Cache.CreateIndex(a => a.AnimeGroupID);
        }

        public override void RegenerateDb()
        {
            int cnt = 0;
            List<AnimeSeries> sers =
                Cache.Values.Where(
                    a => a.ContractVersion < AnimeSeries.CONTRACT_VERSION || a.Contract?.AniDBAnime?.AniDBAnime == null)
                    .ToList();
            int max = sers.Count;
	        ServerState.Instance.CurrentSetupStatus = string.Format(JMMServer.Properties.Resources.Database_Cache, typeof(AnimeSeries).Name, " DbRegen");
	        if (max <= 0) return;
	        foreach (AnimeSeries s in sers)
            {
                try
                {
                    Save(s, false, false, true);
                }
                catch (Exception e)
                {
                }

                cnt++;
                if (cnt % 10 == 0)
                {
                    ServerState.Instance.CurrentSetupStatus = string.Format(JMMServer.Properties.Resources.Database_Cache, typeof(AnimeSeries).Name,
                        " DbRegen - " + cnt + "/" + max);
                }
            }
            ServerState.Instance.CurrentSetupStatus = string.Format(JMMServer.Properties.Resources.Database_Cache, typeof(AnimeSeries).Name,
                " DbRegen - " + max + "/" + max);
        }





        public ChangeTracker<int> GetChangeTracker()
        {
            return Changes;
        }

        //Disable base saves.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("...", false)]
        public override void Save(AnimeSeries obj) { throw new NotSupportedException(); }
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("...", false)]
        public override void Save(IReadOnlyCollection<AnimeSeries> objs) { throw new NotSupportedException(); }


        public void Save(AnimeSeries obj, bool onlyupdatestats)
        {
            Save(obj, true, onlyupdatestats);
        }

        public void Save(AnimeSeries obj, bool updateGroups, bool onlyupdatestats, bool skipgroupfilters = false, bool alsoupdateepisodes=false)
        {
            bool newSeries = false;
            AnimeGroup oldGroup = null;
			bool isMigrating = false;
            lock (obj)
            {
                if (obj.AnimeSeriesID == 0)
                    newSeries = true; // a new series
                else
                {
                    // get the old version from the DB
                    AnimeSeries oldSeries;
                    using (var session = DatabaseFactory.SessionFactory.OpenSession())
                    {
                        oldSeries = session.Get<AnimeSeries>(obj.AnimeSeriesID);
                    }
                    if (oldSeries != null)
                    {
                        // means we are moving series to a different group
                        if (oldSeries.AnimeGroupID != obj.AnimeGroupID)
                        {
                            oldGroup = RepoFactory.AnimeGroup.GetByID(oldSeries.AnimeGroupID);
							AnimeGroup newGroup = RepoFactory.AnimeGroup.GetByID(obj.AnimeGroupID);
							if (newGroup != null && newGroup.GroupName.Equals("AAA Migrating Groups AAA"))
								isMigrating = true;
                            newSeries = true;
                        }
                    }
                }
                if (newSeries && !isMigrating)
                {
                    obj.Contract = null;
                    base.Save(obj);
                }
                HashSet<GroupFilterConditionType> types = obj.UpdateContract(onlyupdatestats);
                base.Save(obj);
                if (!skipgroupfilters && !isMigrating)
                {
	                int endyear = obj.Contract.AniDBAnime.AniDBAnime.EndYear;
	                if (endyear == 0) endyear = DateTime.Today.Year;
	                HashSet<int> allyears = null;
	                if (obj.Contract.AniDBAnime.AniDBAnime.BeginYear != 0)
	                {
		                allyears = new HashSet<int>(Enumerable.Range(obj.Contract.AniDBAnime.AniDBAnime.BeginYear,
			                endyear - obj.Contract.AniDBAnime.AniDBAnime.BeginYear + 1));
	                }
	                RepoFactory.GroupFilter.CreateOrVerifyTagsAndYearsFilters(false,
                        obj.Contract?.AniDBAnime?.AniDBAnime?.AllTags, allyears);
                    //This call will create extra years or tags if the Group have a new year or tag
                    obj.UpdateGroupFilters(types, null);
                }
                Changes.AddOrUpdate(obj.AnimeSeriesID);
            }
            if (updateGroups && !isMigrating)
            {
                logger.Trace("Updating group stats by series from AnimeSeriesRepository.Save: {0}", obj.AnimeSeriesID);
                AnimeGroup grp = RepoFactory.AnimeGroup.GetByID(obj.AnimeGroupID);
                if (grp != null)
                    RepoFactory.AnimeGroup.Save(grp, true, true);

                if (oldGroup != null)
                {
                    logger.Trace("Updating group stats by group from AnimeSeriesRepository.Save: {0}", oldGroup.AnimeGroupID);
                    RepoFactory.AnimeGroup.Save(oldGroup, true, true);
                }
            }
            if (alsoupdateepisodes)
            {
                List<AnimeEpisode> eps = RepoFactory.AnimeEpisode.GetBySeriesID(obj.AnimeSeriesID);
                RepoFactory.AnimeEpisode.Save(eps);
            }
        }

        public void UpdateBatch(ISessionWrapper session, IReadOnlyCollection<AnimeSeries> seriesBatch)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (seriesBatch == null)
                throw new ArgumentNullException(nameof(seriesBatch));

            if (seriesBatch.Count == 0)
            {
                return;
            }

            foreach (AnimeSeries series in seriesBatch)
            {
                session.Update(series);
                Changes.AddOrUpdate(series.AnimeSeriesID);
            }
        }

        public AnimeSeries GetByAnimeID(int id)
        {
            return AniDBIds.GetOne(id);
        }




        public List<AnimeSeries> GetByGroupID(int groupid)
        {
            return Groups.GetMultiple(groupid);
        }


        public List<AnimeSeries> GetWithMissingEpisodes()
        {
            return
                Cache.Values.Where(a => a.MissingEpisodeCountGroups > 0)
                    .OrderByDescending(a => a.EpisodeAddedDate)
                    .ToList();
        }

        public List<AnimeSeries> GetMostRecentlyAdded(int maxResults)
        {
            return Cache.Values.OrderByDescending(a => a.DateTimeCreated).Take(maxResults + 15).ToList();
        }

    }
}