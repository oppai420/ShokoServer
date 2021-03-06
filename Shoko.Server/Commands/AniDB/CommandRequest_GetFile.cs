﻿using System;
using System.Xml;
using AniDBAPI;
using Shoko.Commons.Queue;
using Shoko.Models.Queue;
using Shoko.Models.Server;
using Shoko.Server.Commands.AniDB;
using Shoko.Server.Models;
using Shoko.Server.Repositories;

namespace Shoko.Server.Commands
{
    [Serializable]
    [Command(CommandRequestType.AniDB_GetFileUDP)]
    public class CommandRequest_GetFile : CommandRequestImplementation
    {
        public int VideoLocalID { get; set; }
        public bool ForceAniDB { get; set; }

        private SVR_VideoLocal vlocal;

        public override CommandRequestPriority DefaultPriority => CommandRequestPriority.Priority3;

        public override QueueStateStruct PrettyDescription
        {
            get
            {
                if (vlocal != null)
                    return new QueueStateStruct
                    {
                        queueState = QueueStateEnum.GetFileInfo,
                        extraParams = new[] {vlocal.FileName}
                    };
                return new QueueStateStruct
                {
                    queueState = QueueStateEnum.GetFileInfo,
                    extraParams = new[] {VideoLocalID.ToString()}
                };
            }
        }

        public CommandRequest_GetFile()
        {
        }

        public CommandRequest_GetFile(int vidLocalID, bool forceAniDB)
        {
            VideoLocalID = vidLocalID;
            ForceAniDB = forceAniDB;
            Priority = (int) DefaultPriority;

            GenerateCommandID();
        }

        public override void ProcessCommand()
        {
            logger.Info("Get AniDB file info: {0}", VideoLocalID);


            try
            {
                if (vlocal == null) vlocal = Repo.Instance.VideoLocal.GetByID(VideoLocalID);
                if (vlocal == null) return;
                lock (vlocal)
                {
                    SVR_AniDB_File aniFile = Repo.Instance.AniDB_File.GetByHashAndFileSize(vlocal.Hash, vlocal.FileSize);

                    Raw_AniDB_File fileInfo = null;
                    if (aniFile == null || ForceAniDB)
                        fileInfo = ShokoService.AnidbProcessor.GetFileInfo(vlocal);

                    if (fileInfo != null)
                    {
                        using (var upd = Repo.Instance.AniDB_File.BeginAddOrUpdate(() => aniFile))
                        {
                            upd.Entity.Populate_RA(fileInfo);

                            //overwrite with local file name
                            string localFileName = vlocal.FileName;
                            upd.Entity.FileName = localFileName;

                            aniFile = upd.Commit();
                        }
                        aniFile.CreateLanguages();
                        aniFile.CreateCrossEpisodes(vlocal.FileName);

                        //TODO: Look at why this might be worth it?
                        //SVR_AniDB_Anime anime = Repo.Instance.AniDB_Anime.GetByAnimeID(aniFile.AnimeID);
                        //if (anime != null) Repo.Instance.AniDB_Anime.Save(anime); 
                        SVR_AnimeSeries series = Repo.Instance.AnimeSeries.GetByAnimeID(aniFile.AnimeID);
                        series.UpdateStats(true, true, true);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error processing CommandRequest_GetFile: {0} - {1}", VideoLocalID, ex);
            }
        }

        /// <summary>
        /// This should generate a unique key for a command
        /// It will be used to check whether the command has already been queued before adding it
        /// </summary>
        public override void GenerateCommandID()
        {
            CommandID = $"CommandRequest_GetFile_{VideoLocalID}";
        }

        public override bool LoadFromDBCommand(CommandRequest cq)
        {
            CommandID = cq.CommandID;
            CommandRequestID = cq.CommandRequestID;
            Priority = cq.Priority;
            CommandDetails = cq.CommandDetails;
            DateTimeUpdated = cq.DateTimeUpdated;

            // read xml to get parameters
            if (CommandDetails.Trim().Length > 0)
            {
                XmlDocument docCreator = new XmlDocument();
                docCreator.LoadXml(CommandDetails);

                // populate the fields
                VideoLocalID = int.Parse(TryGetProperty(docCreator, "CommandRequest_GetFile", "VideoLocalID"));
                ForceAniDB = bool.Parse(TryGetProperty(docCreator, "CommandRequest_GetFile", "ForceAniDB"));
                vlocal = Repo.Instance.VideoLocal.GetByID(VideoLocalID);
            }

            return true;
        }

        public override CommandRequest ToDatabaseObject()
        {
            GenerateCommandID();

            CommandRequest cq = new CommandRequest
            {
                CommandID = CommandID,
                CommandType = CommandType,
                Priority = Priority,
                CommandDetails = ToXML(),
                DateTimeUpdated = DateTime.Now
            };
            return cq;
        }
    }
}
