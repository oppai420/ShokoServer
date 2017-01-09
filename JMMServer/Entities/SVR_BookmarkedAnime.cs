﻿using JMMServer.Repositories;
using Shoko.Models.Client;
using Shoko.Models.Server;

namespace JMMServer.Entities
{
    public class SVR_BookmarkedAnime : BookmarkedAnime
    {

        public CL_BookmarkedAnime ToClient()
        {
            CL_BookmarkedAnime contract = this.CloneToClient();
            contract.Anime = null;
            SVR_AniDB_Anime an = RepoFactory.AniDB_Anime.GetByAnimeID(AnimeID);
            if (an != null)
                contract.Anime = an.Contract.AniDBAnime;

            return contract;
        }
    }
}