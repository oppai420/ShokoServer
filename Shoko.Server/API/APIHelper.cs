﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Shoko.Models.PlexAndKodi;
using Shoko.Server.API.v2.Models.common;
using Shoko.Server.ImageDownload;
using Shoko.Server.Models;
using Shoko.Server.PlexAndKodi;
using Shoko.Server.Repositories;

namespace Shoko.Server.API
{
    public static class APIHelper
    {
        #region Contructors

        public static string ConstructUnsortUrl(HttpContext ctx, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "/api/file/unsort", short_url);
        }

        [Obsolete]
        public static string ConstructGroupIdUrl(HttpContext ctx, string gid, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "__TEST__" + (int) JMMType.Group + "/" + gid, short_url);
        }

        [Obsolete]
        public static string ConstructSerieIdUrl(HttpContext ctx, string sid, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "__TEST__" + (int) JMMType.Serie + " / " + sid, short_url);
        }

        [Obsolete]
        public static string ConstructVideoUrl(HttpContext ctx, string vid, JMMType type, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "__TEST__" + (int) type + "/" + vid, short_url);
        }

        public static string ConstructFilterIdUrl(HttpContext ctx, int groupfilter_id, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "/api/filter?id=" + groupfilter_id, short_url);
        }

        public static string ConstructFilterUrl(HttpContext ctx, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "/api/filter", short_url);
        }

        [Obsolete]
        public static string ConstructFiltersUrl(HttpContext ctx, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "__TEST__", short_url);
        }

        [Obsolete]
        public static string ConstructSearchUrl(HttpContext ctx, string limit, string query, bool searchTag, bool short_url = false)
        {
            if (searchTag)
            {
                return APIHelper.ProperURL(ctx, "/api/searchTag/" + limit + "/" + System.Net.WebUtility.UrlEncode(query),
                    short_url);
            }
            else
            {
                return APIHelper.ProperURL(ctx, "/api/search/" + limit + "/" + System.Net.WebUtility.UrlEncode(query),
                    short_url);
            }
        }

        [Obsolete]
        public static string ConstructPlaylistUrl(HttpContext ctx, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "/api/metadata/" + (int) JMMType.Playlist + "/0", short_url);
        }

        [Obsolete]
        public static string ConstructPlaylistIdUrl(HttpContext ctx, int pid, bool short_url = false)
        {
            return APIHelper.ProperURL(ctx, "/api/metadata/" + (int) JMMType.Playlist + "/" + pid, short_url);
        }

        public static string ConstructSupportImageLink(HttpContext ctx, string name, bool short_url = true)
        {
            return APIHelper.ProperURL(ctx, "/api/image/support/" + name, short_url);
        }

        public static string ConstructImageLinkFromRest(HttpContext ctx, string path, bool short_url = true)
        {
            return ConvertRestImageToNonRestUrl(ctx, path, short_url);
        }

        public static string ConstructImageLinkFromTypeAndId(HttpContext ctx, int type, int id, bool short_url = true)
        {
            return APIHelper.ProperURL(ctx, "/api/image/" + type.ToString() + "/" + id.ToString(), short_url);
        }

        public static string ConstructVideoLocalStream(HttpContext ctx, int userid, string vid, string name, bool autowatch)
        {
            return APIHelper.ProperURL(ctx, "/Stream/" + vid + "/" + userid + "/" + autowatch + "/" + name);
        }

        #endregion

        #region Converters

        private static string ConvertRestImageToNonRestUrl(HttpContext ctx, string url, bool short_url)
        {
            // Rest URLs should always end in either type/id or type/id/ratio
            // Regardless of ',' or '.', ratio will not parse as int
            if (string.IsNullOrEmpty(url)) return null;
            string link = url.ToLower();
            string[] split = link.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            int type;
            if (int.TryParse(split[split.Length - 1], out int id)) // no ratio
            {
                if (int.TryParse(split[split.Length - 2], out type))
                {
                    return ConstructImageLinkFromTypeAndId(ctx, type, id, short_url);
                }
            }
            else if (int.TryParse(split[split.Length - 2], out id)) // ratio
            {
                if (int.TryParse(split[split.Length - 3], out type))
                {
                    return ConstructImageLinkFromTypeAndId(ctx, type, id, short_url);
                }
            }
            return null; // invalid url, which did not end in type/id[/ratio]
        }

        public static Filter FilterFromGroupFilter(HttpContext ctx, SVR_GroupFilter gg, int uid)
        {
            Filter ob = new Filter
            {
                name = gg.GroupFilterName,
                id = gg.GroupFilterID,
                url = APIHelper.ConstructFilterIdUrl(ctx, gg.GroupFilterID)
            };
            if (gg.GroupsIds.ContainsKey(uid))
            {
                HashSet<int> groups = gg.GroupsIds[uid];
                if (groups.Count != 0)
                {
                    ob.size = groups.Count;
                    ob.viewed = 0;

                    foreach (int grp in groups)
                    {
                        SVR_AnimeGroup ag = Repo.Instance.AnimeGroup.GetByID(grp);
                        Video v = ag.GetPlexContract(uid);
                        if (v?.Art != null && v.Thumb != null)
                        {
                            ob.art.fanart.Add(new Art() {url = APIHelper.ConstructImageLinkFromRest(ctx, v.Art), index = 0});
                            ob.art.thumb.Add(new Art()
                            {
                                url = APIHelper.ConstructImageLinkFromRest(ctx, v.Thumb),
                                index = 0
                            });
                            break;
                        }
                    }
                }
            }
            return ob;
        }

        public static Filter FilterFromAnimeGroup(HttpContext ctx, SVR_AnimeGroup grp, int uid)
        {
            Filter ob = new Filter
            {
                name = grp.GroupName,
                id = grp.AnimeGroupID,
                url = APIHelper.ConstructFilterIdUrl(ctx, grp.AnimeGroupID),
                size = -1,
                viewed = -1
            };
            foreach (SVR_AnimeSeries ser in grp.GetSeries().Randomize())
            {
                SVR_AniDB_Anime anim = ser.GetAnime();
                if (anim != null)
                {
                    ImageDetails fanart = anim.GetDefaultFanartDetailsNoBlanks();
                    ImageDetails banner = anim.GetDefaultWideBannerDetailsNoBlanks();

                    if (fanart != null)
                    {
                        ob.art.fanart.Add(new Art()
                        {
                            url = APIHelper.ConstructImageLinkFromTypeAndId(ctx, (int) fanart.ImageType, fanart.ImageID),
                            index = ob.art.fanart.Count
                        });
                        ob.art.thumb.Add(new Art()
                        {
                            url = APIHelper.ConstructImageLinkFromTypeAndId(ctx, (int) fanart.ImageType, fanart.ImageID),
                            index = ob.art.thumb.Count
                        });
                    }

                    if (banner != null)
                    {
                        ob.art.banner.Add(new Art()
                        {
                            url = APIHelper.ConstructImageLinkFromTypeAndId(ctx, (int) banner.ImageType, banner.ImageID),
                            index = ob.art.banner.Count
                        });
                    }

                    if (ob.art.fanart.Count > 0)
                    {
                        break;
                    }
                }
            }
            return ob;
        }

        #endregion

        private static string ProperURL(HttpContext ctx, string path, bool short_url = false)
        {
            if (!string.IsNullOrEmpty(path))
            {
                return !short_url
                    ? ctx.Request.Scheme + "://" + ctx.Request.Host.Host + ":" + ctx.Request.Host.Port + path
                    : path;
            }
            return string.Empty;
        }
    }
}