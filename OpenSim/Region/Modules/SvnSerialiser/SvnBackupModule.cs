﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Timers;
using log4net;
using Nini.Config;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Modules.ExportSerialiser;
using OpenSim.Region.Environment.Modules.World.Terrain;
using OpenSim.Region.Environment.Scenes;
using PumaCode.SvnDotNet.AprSharp;
using PumaCode.SvnDotNet.SubversionSharp;
using Slash=System.IO.Path;

namespace OpenSim.Region.Modules.SvnSerialiser
{
    public class SvnBackupModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly List<Scene> m_scenes = new List<Scene>();
        private readonly Timer m_timer = new Timer();

        private bool m_enabled = false;
        private bool m_installBackupOnLoad = false;
        private IRegionSerialiser m_serialiser;
        private bool m_svnAutoSave = false;
        private SvnClient m_svnClient;
        private string m_svndir = "SVNmodule\\repo";
        private string m_svnpass = "password";

        private TimeSpan m_svnperiod = new TimeSpan(0, 0, 15, 0, 0);
        private string m_svnurl = "svn://insert.your.svn/here/";
        private string m_svnuser = "username";

        #region SvnModule Core

        /// <summary>
        /// Exports a specified scene to the SVN repo directory, then commits.
        /// </summary>
        /// <param name="scene">The scene to export</param>
        public void SaveRegion(Scene scene)
        {
            List<string> svnfilenames = CreateAndAddExport(scene);

            m_svnClient.Commit3(svnfilenames, true, false);
            m_log.Info("[SVNBACKUP]: Region backup successful (" + scene.RegionInfo.RegionName + ").");
        }

        /// <summary>
        /// Saves all registered scenes to the SVN repo, then commits.
        /// </summary>
        public void SaveAllRegions()
        {
            List<string> svnfilenames = new List<string>();
            List<string> regions = new List<string>();

            foreach (Scene scene in m_scenes)
            {
                svnfilenames.AddRange(CreateAndAddExport(scene));
                regions.Add("'" + scene.RegionInfo.RegionName + "' ");
            }

            m_svnClient.Commit3(svnfilenames, true, false);
            m_log.Info("[SVNBACKUP]: Server backup successful ( " + String.Concat(regions.ToArray()) + ").");
        }

        private List<string> CreateAndAddExport(Scene scene)
        {
            m_log.Info("[SVNBACKUP]: Saving a region to SVN with name " + scene.RegionInfo.RegionName);

            List<string> filenames = m_serialiser.SerialiseRegion(scene, m_svndir + Slash.DirectorySeparatorChar + scene.RegionInfo.RegionID + "\\");

            try
            {
                m_svnClient.Add3(m_svndir + Slash.DirectorySeparatorChar + scene.RegionInfo.RegionID, true, false, false);
            }
            catch (SvnException)
            {
            }

            List<string> svnfilenames = new List<string>();
            foreach (string filename in filenames)
                svnfilenames.Add(m_svndir + Slash.DirectorySeparatorChar + scene.RegionInfo.RegionID + Slash.DirectorySeparatorChar + filename);
            svnfilenames.Add(m_svndir + Slash.DirectorySeparatorChar + scene.RegionInfo.RegionID);

            return svnfilenames;
        }

        public void LoadRegion(Scene scene)
        {
            scene.LoadPrimsFromXml2(m_svndir + Slash.DirectorySeparatorChar + scene.RegionInfo.RegionID +
                                    Slash.DirectorySeparatorChar + "objects.xml");
            scene.RequestModuleInterface<ITerrainModule>().LoadFromFile(m_svndir + Slash.DirectorySeparatorChar + scene.RegionInfo.RegionID +
                                                                        Slash.DirectorySeparatorChar + "heightmap.r32");
            m_log.Info("[SVNBACKUP]: Region load successful (" + scene.RegionInfo.RegionName + ").");
        }

        private void CheckoutSvn()
        {
            m_svnClient.Checkout2(m_svnurl, m_svndir, Svn.Revision.Head, Svn.Revision.Head, true, false);
        }

        private void CheckoutSvn(SvnRevision revision)
        {
            m_svnClient.Checkout2(m_svnurl, m_svndir, revision, revision, true, false);
        }

        private void CheckoutSvnPartial(string subdir)
        {
            if (!Directory.Exists(m_svndir + Slash.DirectorySeparatorChar + subdir))
                Directory.CreateDirectory(m_svndir + Slash.DirectorySeparatorChar + subdir);

            m_svnClient.Checkout2(m_svnurl + "/" + subdir, m_svndir, Svn.Revision.Head, Svn.Revision.Head, true, false);
        }

        private void CheckoutSvnPartial(string subdir, SvnRevision revision)
        {
            if (!Directory.Exists(m_svndir + Slash.DirectorySeparatorChar + subdir))
                Directory.CreateDirectory(m_svndir + Slash.DirectorySeparatorChar + subdir);

            m_svnClient.Checkout2(m_svnurl + "/" + subdir, m_svndir, revision, revision, true, false);
        }

        #endregion

        #region SvnDotNet Callbacks

        private SvnError SimpleAuth(out SvnAuthCredSimple svnCredentials, IntPtr baton,
                                    AprString realm, AprString username, bool maySave, AprPool pool)
        {
            svnCredentials = SvnAuthCredSimple.Alloc(pool);
            svnCredentials.Username = new AprString(m_svnuser, pool);
            svnCredentials.Password = new AprString(m_svnpass, pool);
            svnCredentials.MaySave = false;
            return SvnError.NoError;
        }

        private SvnError GetCommitLogCallback(out AprString logMessage, out SvnPath tmpFile, AprArray commitItems, IntPtr baton, AprPool pool)
        {
            if (!commitItems.IsNull)
            {
                foreach (SvnClientCommitItem2 item in commitItems)
                {
                    m_log.Debug("[SVNBACKUP]: ... " + Path.GetFileName(item.Path.ToString()) + " (" + item.Kind.ToString() + ") r" + item.Revision.ToString());
                }
            }

            string msg = "Region Backup (" + System.Environment.MachineName + " at " + DateTime.UtcNow + " UTC)";

            m_log.Debug("[SVNBACKUP]: Saved with message: " + msg);

            logMessage = new AprString(msg, pool);
            tmpFile = new SvnPath(pool);

            return (SvnError.NoError);
        }

        #endregion

        #region IRegionModule Members

        public void Initialise(Scene scene, IConfigSource source)
        {
            try
            {
                if (!source.Configs["SVN"].GetBoolean("Enabled", false))
                    return;

                m_enabled = true;

                m_svndir = source.Configs["SVN"].GetString("Directory", m_svndir);
                m_svnurl = source.Configs["SVN"].GetString("URL", m_svnurl);
                m_svnuser = source.Configs["SVN"].GetString("Username", m_svnuser);
                m_svnpass = source.Configs["SVN"].GetString("Password", m_svnpass);
                m_installBackupOnLoad = source.Configs["SVN"].GetBoolean("ImportOnStartup", m_installBackupOnLoad);
                m_svnAutoSave = source.Configs["SVN"].GetBoolean("Autosave", m_svnAutoSave);
                m_svnperiod = new TimeSpan(0, source.Configs["SVN"].GetInt("AutosavePeriod", (int) m_svnperiod.TotalMinutes), 0);
            }
            catch (Exception)
            {
            }

            lock (m_scenes)
            {
                m_scenes.Add(scene);
            }

            scene.EventManager.OnPluginConsole += EventManager_OnPluginConsole;
        }

        public void PostInitialise()
        {
            if (m_enabled == false)
                return;

            if (m_svnAutoSave)
            {
                m_timer.Interval = m_svnperiod.TotalMilliseconds;
                m_timer.Elapsed += m_timer_Elapsed;
                m_timer.AutoReset = true;
                m_timer.Start();
            }

            m_log.Info("[SVNBACKUP]: Connecting to SVN server " + m_svnurl + " ...");
            SetupSvnProvider();

            m_log.Info("[SVNBACKUP]: Creating repository in " + m_svndir + ".");
            CreateSvnDirectory();
            CheckoutSvn();
            SetupSerialiser();

            if (m_installBackupOnLoad)
            {
                m_log.Info("[SVNBACKUP]: Importing latest SVN revision to scenes...");
                foreach (Scene scene in m_scenes)
                {
                    LoadRegion(scene);
                }
            }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "SvnBackupModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion

        private void EventManager_OnPluginConsole(string[] args)
        {
            if (args[0] == "svn" && args[1] == "save")
            {
                SaveAllRegions();
            }
            if (args.Length == 2)
            {
                if (args[0] == "svn" && args[1] == "load")
                {
                    LoadAllScenes();
                }
            }
            if (args.Length == 3)
            {
                if (args[0] == "svn" && args[1] == "load")
                {
                    LoadAllScenes(Int32.Parse(args[2]));
                }
            }
            if (args.Length == 3)
            {
                if (args[0] == "svn" && args[1] == "load-region")
                {
                    LoadScene(args[2]);
                }
            }
            if (args.Length == 4)
            {
                if (args[0] == "svn" && args[1] == "load-region")
                {
                    LoadScene(args[2], Int32.Parse(args[3]));
                }
            }
        }

        public void LoadScene(string name)
        {
            CheckoutSvn();

            foreach (Scene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionName.ToLower().Equals(name.ToLower()))
                {
                    LoadRegion(scene);
                    return;
                }
            }
            m_log.Warn("[SVNBACKUP]: No region loaded - unable to find matching name.");
        }

        public void LoadScene(string name, int revision)
        {
            CheckoutSvn(new SvnRevision(revision));

            foreach (Scene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionName.ToLower().Equals(name.ToLower()))
                {
                    LoadRegion(scene);
                    return;
                }
            }
            m_log.Warn("[SVNBACKUP]: No region loaded - unable to find matching name.");
        }

        public void LoadAllScenes()
        {
            CheckoutSvn();

            foreach (Scene scene in m_scenes)
            {
                LoadRegion(scene);
            }
        }


        public void LoadAllScenes(int revision)
        {
            CheckoutSvn(new SvnRevision(revision));

            foreach (Scene scene in m_scenes)
            {
                LoadRegion(scene);
            }
        }

        private void m_timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SaveAllRegions();
        }

        private void SetupSerialiser()
        {
            if (m_scenes.Count > 0)
                m_serialiser = m_scenes[0].RequestModuleInterface<IRegionSerialiser>();
        }

        private void SetupSvnProvider()
        {
            m_svnClient = new SvnClient();
            m_svnClient.AddUsernameProvider();
            m_svnClient.AddPromptProvider(new SvnAuthProviderObject.SimplePrompt(SimpleAuth), IntPtr.Zero, 2);
            m_svnClient.OpenAuth();
            m_svnClient.Context.LogMsgFunc2 = new SvnDelegate(new SvnClient.GetCommitLog2(GetCommitLogCallback));
        }

        private void CreateSvnDirectory()
        {
            if (!Directory.Exists(m_svndir))
                Directory.CreateDirectory(m_svndir);
        }
    }
}