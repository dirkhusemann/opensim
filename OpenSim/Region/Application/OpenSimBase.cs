/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using libsecondlife;
using log4net;
using Mono.Addins;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Statistics;
using OpenSim.Region.ClientStack;
using OpenSim.Region.Communications.Local;
using OpenSim.Region.Communications.OGS1;
using OpenSim.Region.Environment;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Physics.Manager;

namespace OpenSim
{
    /// <summary>
    /// Common OpenSim region service code
    /// </summary>
    public class OpenSimBase : RegionApplicationBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected string proxyUrl;
        protected int proxyOffset = 0;

        /// <summary>
        /// The file used to load and save prim backup xml if none has been specified
        /// </summary>
        protected const string DEFAULT_PRIM_BACKUP_FILENAME = "prim-backup.xml";

        /// <summary>
        /// The file use to load and save an opensim archive if none has been specified
        /// </summary>
        protected const string DEFAULT_OAR_BACKUP_FILENAME = "scene.oar.tar";

        public string m_physicsEngine;
        public string m_meshEngineName;
        public string m_scriptEngine;
        public bool m_sandbox;
        public bool user_accounts;
        public bool m_gridLocalAsset;
        public bool m_see_into_region_from_neighbor;

        protected LocalLoginService m_loginService;

        protected string m_storageDll;
        protected string m_clientstackDll;

        protected List<IClientNetworkServer> m_clientServers = new List<IClientNetworkServer>();
        protected List<RegionInfo> m_regionData = new List<RegionInfo>();

        protected bool m_physicalPrim;

        protected bool m_standaloneAuthenticate = false;
        protected string m_standaloneWelcomeMessage = null;
        protected string m_standaloneInventoryPlugin;
        protected string m_standaloneAssetPlugin;
        protected string m_standaloneUserPlugin;

        private string m_standaloneInventorySource;
        private string m_standaloneAssetSource;
        private string m_standaloneUserSource;

        protected string m_assetStorage = "local";

        public ConsoleCommand CreateAccount = null;
        protected bool m_dumpAssetsToFile;

        protected List<IApplicationPlugin> m_plugins = new List<IApplicationPlugin>();

        protected IConfigSource m_finalConfig = null;

        //protected IniConfigSource m_config;
        protected OpenSimConfigSource m_config;

        public OpenSimConfigSource ConfigSource
        {
            get { return m_config; }
            set { m_config = value; }
        }

        public List<IClientNetworkServer> ClientServers
        {
            get { return m_clientServers; }
        }

        public List<RegionInfo> RegionData
        {
            get { return m_regionData; }
        }

        public new BaseHttpServer HttpServer
        {
            get { return m_httpServer; }
        }

// 6/28 Cfk: Commented out the new in this next line. It used to be
// 6/28 cfk: public new uint HttpServerPort and it was causing a warning indicating there should not be a new
// 6/28 cfk: There is still a new in the declaration above and it is unclear if it should be there or not.
        public uint HttpServerPort
        {
            get { return m_httpServerPort; }
        }

        protected ModuleLoader m_moduleLoader;

        public ModuleLoader ModuleLoader
        {
            get { return m_moduleLoader; }
            set { m_moduleLoader = value; }
        }

        public OpenSimBase(IConfigSource configSource)
            : base()
        {
            IConfig startupConfig = configSource.Configs["Startup"];

            Application.iniFilePath = startupConfig.GetString("inifile", "OpenSim.ini");

            m_config = new OpenSimConfigSource();
            m_config.Source = new IniConfigSource();
            // IConfigSource icong;
            
            //check for .INI file (either default or name passed in command line)
            if (File.Exists(Application.iniFilePath))
            {
                m_config.Source.Merge(new IniConfigSource(Application.iniFilePath));
                m_config.Source.Merge(configSource);
            }
            else
            {
                Application.iniFilePath = Path.Combine(Util.configDir(), Application.iniFilePath);
                if (File.Exists(Application.iniFilePath))
                {
                    m_config.Source.Merge(new IniConfigSource(Application.iniFilePath));
                    m_config.Source.Merge(configSource);
                }
                else
                {
                    if (File.Exists("OpenSim.xml"))
                    {
                        //check for a xml config file
                        Application.iniFilePath = "OpenSim.xml";
                        m_config.Source = new XmlConfigSource();
                        m_config.Source.Merge(new XmlConfigSource(Application.iniFilePath));
                        m_config.Source.Merge(configSource);
                    }
                    else
                    {
                        //Application.iniFilePath = "OpenSim.xml";
                       // m_config.ConfigSource = new XmlConfigSource();
                        // no default config files, so set default values, and save it
                        m_config.Source.Merge(DefaultConfig());
                        m_config.Source.Merge(configSource);
                        m_config.Save(Application.iniFilePath);
                    }
                }
            }

            ReadConfigSettings();
        }

        public static IConfigSource DefaultConfig()
        {
            IConfigSource DefaultConfig = new IniConfigSource();
            if (DefaultConfig.Configs["Startup"] == null)
                DefaultConfig.AddConfig("Startup");
            IConfig config = DefaultConfig.Configs["Startup"];
            if (config != null)
            {
                config.Set("gridmode", false);
                config.Set("physics", "basicphysics");
                config.Set("physical_prim", true);
                config.Set("see_into_this_sim_from_neighbor", true);
                config.Set("serverside_object_permissions", false);
                config.Set("storage_plugin", "OpenSim.Data.SQLite.dll");
                config.Set("storage_connection_string", "URI=file:OpenSim.db,version=3");
                config.Set("storage_prim_inventories", true);
                config.Set("startup_console_commands_file", String.Empty);
                config.Set("shutdown_console_commands_file", String.Empty);
                config.Set("script_engine", "OpenSim.Region.ScriptEngine.DotNetEngine.dll");
                config.Set("asset_database", "sqlite");
                config.Set("clientstack_plugin", "OpenSim.Region.ClientStack.LindenUDP.dll");
            }

            if (DefaultConfig.Configs["StandAlone"] == null)
                DefaultConfig.AddConfig("StandAlone");

            config = DefaultConfig.Configs["StandAlone"];
            if (config != null)
            {
                config.Set("accounts_authenticate", false);
                config.Set("welcome_message", "Welcome to OpenSimulator");
                config.Set("inventory_plugin", "OpenSim.Data.SQLite.dll");
                config.Set("inventory_source", "");
                config.Set("userDatabase_plugin", "OpenSim.Data.SQLite.dll");
                config.Set("user_source", "");
                config.Set("asset_plugin", "OpenSim.Data.SQLite.dll");
                config.Set("asset_source", "");
                config.Set("dump_assets_to_file", false);
            }

            if (DefaultConfig.Configs["Network"] == null)
                DefaultConfig.AddConfig("Network");
            config = DefaultConfig.Configs["Network"];
            if (config != null)
            {
                config.Set("default_location_x", 1000);
                config.Set("default_location_y", 1000);
                config.Set("http_listener_port", NetworkServersInfo.DefaultHttpListenerPort);
                config.Set("remoting_listener_port", NetworkServersInfo.RemotingListenerPort);
                config.Set("grid_server_url", "http://127.0.0.1:" + GridConfig.DefaultHttpPort.ToString());
                config.Set("grid_send_key", "null");
                config.Set("grid_recv_key", "null");
                config.Set("user_server_url", "http://127.0.0.1:" + UserConfig.DefaultHttpPort.ToString());
                config.Set("user_send_key", "null");
                config.Set("user_recv_key", "null");
                config.Set("asset_server_url", "http://127.0.0.1:" + AssetConfig.DefaultHttpPort.ToString());
                config.Set("inventory_server_url", "http://127.0.0.1:" + InventoryConfig.DefaultHttpPort.ToString());
            }

            if (DefaultConfig.Configs["RemoteAdmin"] == null)
                DefaultConfig.AddConfig("RemoteAdmin");
            config = DefaultConfig.Configs["RemoteAdmin"];
            if (config != null)
            {
                config.Set("enabled", "false");
            }

            if (DefaultConfig.Configs["Voice"] == null)
                DefaultConfig.AddConfig("Voice");
            config = DefaultConfig.Configs["Voice"];
            if (config != null)
            {
                config.Set("enabled", "false");
            }
            return DefaultConfig;

        }

        protected virtual void ReadConfigSettings()
        {
            m_networkServersInfo = new NetworkServersInfo();

            IConfig startupConfig = m_config.Source.Configs["Startup"];

            if (startupConfig != null)
            {
                m_sandbox = !startupConfig.GetBoolean("gridmode", false);
                m_physicsEngine = startupConfig.GetString("physics", "basicphysics");
                m_meshEngineName = startupConfig.GetString("meshing", "ZeroMesher");

                m_physicalPrim = startupConfig.GetBoolean("physical_prim", true);

                m_see_into_region_from_neighbor = startupConfig.GetBoolean("see_into_this_sim_from_neighbor", true);

                m_storageDll = startupConfig.GetString("storage_plugin", "OpenSim.Data.SQLite.dll");
                if (m_storageDll == "OpenSim.DataStore.MonoSqlite.dll")
                {
                    m_storageDll = "OpenSim.Data.SQLite.dll";
                    Console.WriteLine("WARNING: OpenSim.DataStore.MonoSqlite.dll is deprecated. Set storage_plugin to OpenSim.Data.SQLite.dll.");
                    Thread.Sleep(3000);
                }
                m_storageConnectionString
                    = startupConfig.GetString("storage_connection_string", "URI=file:OpenSim.db,version=3");
                m_storagePersistPrimInventories
                    = startupConfig.GetBoolean("storage_prim_inventories", true);

                m_scriptEngine = startupConfig.GetString("script_engine", "OpenSim.Region.ScriptEngine.DotNetEngine.dll");
                m_assetStorage = startupConfig.GetString("asset_database", "local");
                m_clientstackDll = startupConfig.GetString("clientstack_plugin", "OpenSim.Region.ClientStack.LindenUDP.dll");
            }

            IConfig standaloneConfig = m_config.Source.Configs["StandAlone"];
            if (standaloneConfig != null)
            {
                m_standaloneAuthenticate = standaloneConfig.GetBoolean("accounts_authenticate", false);
                m_standaloneWelcomeMessage = standaloneConfig.GetString("welcome_message", "Welcome to OpenSim");
                m_standaloneInventoryPlugin =
                    standaloneConfig.GetString("inventory_plugin", "OpenSim.Data.SQLite.dll");
                m_standaloneInventorySource =
                    standaloneConfig.GetString("inventory_source","");
                m_standaloneUserPlugin =
                    standaloneConfig.GetString("userDatabase_plugin", "OpenSim.Data.SQLite.dll");
                m_standaloneUserSource =
                    standaloneConfig.GetString("user_source","");
                m_standaloneAssetPlugin =
                    standaloneConfig.GetString("asset_plugin", "OpenSim.Data.SQLite.dll");
                m_standaloneAssetSource =
                    standaloneConfig.GetString("asset_source","");

                m_dumpAssetsToFile = standaloneConfig.GetBoolean("dump_assets_to_file", false);
            }

            m_networkServersInfo.loadFromConfiguration(m_config.Source);
        }

        protected void plugin_initialiser_ (IPlugin plugin)
        {
            IApplicationPlugin p = plugin as IApplicationPlugin;
            p.Initialise (this);
        }

        protected void LoadPlugins()
        {
            PluginLoader<IApplicationPlugin> loader = new PluginLoader<IApplicationPlugin> (".");
            loader.Load ("/OpenSim/Startup", plugin_initialiser_);
            m_plugins = loader.Plugins;
        }

        /// <summary>
        /// Performs initialisation of the scene, such as loading configuration from disk.
        /// </summary>
        public override void Startup()
        {
            base.Startup();
            
            m_stats = StatsManager.StartCollectingSimExtraStats();

            // StandAlone mode? m_sandbox is determined by !startupConfig.GetBoolean("gridmode", false)
            if (m_sandbox)
            {
                LocalInventoryService inventoryService = new LocalInventoryService();
                inventoryService.AddPlugin(m_standaloneInventoryPlugin, m_standaloneInventorySource);

                LocalUserServices userService =
                    new LocalUserServices(m_networkServersInfo, m_networkServersInfo.DefaultHomeLocX,
                                          m_networkServersInfo.DefaultHomeLocY, inventoryService);
                userService.AddPlugin(m_standaloneUserPlugin, m_standaloneUserSource);

                LocalBackEndServices backendService = new LocalBackEndServices();

                CommunicationsLocal localComms =
                    new CommunicationsLocal(m_networkServersInfo, m_httpServer, m_assetCache, userService,
                                            inventoryService, backendService, backendService, m_dumpAssetsToFile);
                m_commsManager = localComms;

                m_loginService =
                    new LocalLoginService(userService, m_standaloneWelcomeMessage, localComms, m_networkServersInfo,
                                          m_standaloneAuthenticate);
                m_loginService.OnLoginToRegion += backendService.AddNewSession;

                // set up XMLRPC handler for client's initial login request message
                m_httpServer.AddXmlRPCHandler("login_to_simulator", m_loginService.XmlRpcLoginMethod);

                // provides the web form login
                m_httpServer.AddHTTPHandler("login", m_loginService.ProcessHTMLLogin);

                // Provides the LLSD login
                m_httpServer.SetLLSDHandler(m_loginService.LLSDLoginMethod);

                CreateAccount = localComms.doCreate;
            }
            else
            {
                // We are in grid mode
                m_commsManager = new CommunicationsOGS1(m_networkServersInfo, m_httpServer, m_assetCache);
                m_httpServer.AddStreamHandler(new SimStatusHandler());
            }

            proxyUrl = ConfigSource.Source.Configs["Network"].GetString("proxy_url", "");
            proxyOffset = Int32.Parse(ConfigSource.Source.Configs["Network"].GetString("proxy_offset", "0"));

            // Create a ModuleLoader instance
            m_moduleLoader = new ModuleLoader(m_config.Source);

            LoadPlugins();
        }

        protected override void Initialize()
        {
            //
            // Called from base.StartUp()
            //

            m_httpServerPort = m_networkServersInfo.HttpListenerPort;

            IAssetServer assetServer;
            if (m_assetStorage == "grid")
            {
                assetServer = new GridAssetClient(m_networkServersInfo.AssetURL);
            }
            else
            {
                SQLAssetServer sqlAssetServer = new SQLAssetServer(m_standaloneAssetPlugin, m_standaloneAssetSource);
                sqlAssetServer.LoadDefaultAssets();
                assetServer = sqlAssetServer;
            }

            m_assetCache = new AssetCache(assetServer);

            m_sceneManager.OnRestartSim += handleRestartRegion;
        }

        public LLUUID CreateUser(string tempfirstname, string templastname, string tempPasswd, uint regX, uint regY)
        {
            return m_commsManager.AddUser(tempfirstname,templastname,tempPasswd,regX,regY);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, bool portadd_flag)
        {
            return CreateRegion(regionInfo, portadd_flag, false);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo)
        {
            return CreateRegion(regionInfo, false, true);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <param name="do_post_init"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, bool portadd_flag, bool do_post_init)
        {
            int port = regionInfo.InternalEndPoint.Port;

            // set initial RegionID to originRegionID in RegionInfo. (it needs for loding prims)
            regionInfo.originRegionID = regionInfo.RegionID;

            // set initial ServerURI
            regionInfo.ServerURI = "http://" + regionInfo.ExternalHostName
                + ":" + regionInfo.InternalEndPoint.Port.ToString();

            regionInfo.HttpPort = m_httpServerPort;

            if ((proxyUrl.Length > 0) && (portadd_flag))
            {
                // set proxy url to RegionInfo
                regionInfo.proxyUrl = proxyUrl;
                Util.XmlRpcCommand(proxyUrl, "AddPort", port, port + proxyOffset, regionInfo.ExternalHostName);
            }

            IClientNetworkServer clientServer;
            Scene scene = SetupScene(regionInfo, proxyOffset, out clientServer);

            m_log.Info("[MODULES]: Loading Region's modules");

            List<IRegionModule> modules = m_moduleLoader.PickupModules(scene, ".");
            // This needs to be ahead of the script engine load, so the
            // script module can pick up events exposed by a module
            m_moduleLoader.InitialiseSharedModules(scene);

            //m_moduleLoader.PickupModules(scene, "ScriptEngines");
            //m_moduleLoader.LoadRegionModules(Path.Combine("ScriptEngines", m_scriptEngine), scene);

            if (string.IsNullOrEmpty(m_scriptEngine))
            {
                m_log.Info("[MODULES]: No script engien module specified");
            }
            else
            {
                m_log.Info("[MODULES]: Loading scripting engine modules");
                foreach (string module in m_scriptEngine.Split(','))
                {
                    string mod = module.Trim(" \t".ToCharArray()); // Clean up name
                    m_log.Info("[MODULES]: Loading scripting engine: " + mod);
                    try
                    {
                        m_moduleLoader.LoadRegionModules(Path.Combine("ScriptEngines", mod), scene);
                    }
                    catch (Exception ex)
                    {
                        m_log.Error("[MODULES]: Failed to load script engine: " + ex.ToString());
                    }
                }
            }

            scene.SetModuleInterfaces();

            //moved these here as the terrain texture has to be created after the modules are initialized
            // and has to happen before the region is registered with the grid.
            scene.CreateTerrainTexture(false);
            scene.LoadRegionBanlist();

            try
            {
                scene.RegisterRegionWithGrid();
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[STARTUP]: Registration of region with grid failed, aborting startup - {0}", e);

                // Carrying on now causes a lot of confusion down the line - we need to get the user's attention
                System.Environment.Exit(1);
            }

            // We need to do this after we've initialized the scripting engines.
            scene.CreateScriptInstances();

            scene.loadAllLandObjectsFromStorage(regionInfo.originRegionID);
            scene.EventManager.TriggerParcelPrimCountUpdate();

            m_sceneManager.Add(scene);

            m_clientServers.Add(clientServer);
            m_regionData.Add(regionInfo);
            clientServer.Start();

            if (do_post_init)
            {
                foreach (IRegionModule module in modules)
                {
                    module.PostInitialise();
                }
            }

            return clientServer;
        }

        protected override StorageManager CreateStorageManager(string connectionstring)
        {
            return new StorageManager(m_storageDll, connectionstring, m_storagePersistPrimInventories);
        }

        protected override ClientStackManager CreateClientStackManager()
        {
            return new ClientStackManager(m_clientstackDll);
        }

        protected override Scene CreateScene(RegionInfo regionInfo, StorageManager storageManager,
                                             AgentCircuitManager circuitManager)
        {
            SceneCommunicationService sceneGridService = new SceneCommunicationService(m_commsManager);
            return
                new Scene(regionInfo, circuitManager, m_commsManager, sceneGridService, m_assetCache,
                          storageManager, m_httpServer,
                          m_moduleLoader, m_dumpAssetsToFile, m_physicalPrim, m_see_into_region_from_neighbor, m_config.Source,
                          m_version);

        }

        public void handleRestartRegion(RegionInfo whichRegion)
        {
            m_log.Error("[OPENSIM MAIN]: Got restart signal from SceneManager");
            
            // Shutting down the client server
            bool foundClientServer = false;
            int clientServerElement = 0;

            for (int i = 0; i < m_clientServers.Count; i++)
            {
                if (m_clientServers[i].HandlesRegion(new Location(whichRegion.RegionHandle)))
                {
                    clientServerElement = i;
                    foundClientServer = true;
                    break;
                }
            }
            if (foundClientServer)
            {
                m_clientServers[clientServerElement].Server.Close();
                m_clientServers.RemoveAt(clientServerElement);
            }

            //Removing the region from the sim's database of regions..
            int RegionHandleElement = -1;
            for (int i = 0; i < m_regionData.Count; i++)
            {
                if (whichRegion.RegionHandle == m_regionData[i].RegionHandle)
                {
                    RegionHandleElement = i;
                }
            }
            if (RegionHandleElement >= 0)
            {
                m_regionData.RemoveAt(RegionHandleElement);
            }

            CreateRegion(whichRegion, true);
        }

        # region Setup methods

        protected override PhysicsScene GetPhysicsScene()
        {
            return GetPhysicsScene(m_physicsEngine, m_meshEngineName, m_config.Source);
        }

        /// <summary>
        /// Handler to supply the current status of this sim
        ///
        /// Currently this is always OK if the simulator is still listening for connections on its HTTP service
        /// </summary>
        protected class SimStatusHandler : IStreamedRequestHandler
        {
            public byte[] Handle(string path, Stream request,
                                 OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return Encoding.UTF8.GetBytes("OK");
            }

            public string ContentType
            {
                get { return "text/plain"; }
            }

            public string HttpMethod
            {
                get { return "GET"; }
            }

            public string Path
            {
                get { return "/simstatus/"; }
            }
        }

        #endregion

        /// <summary>
        /// Performs any last-minute sanity checking and shuts down the region server
        /// </summary>
        public override void Shutdown()
        {
            if (proxyUrl.Length > 0)
            {
                Util.XmlRpcCommand(proxyUrl, "Stop");
            }

            m_log.Info("[SHUTDOWN]: Closing all threads");
            m_log.Info("[SHUTDOWN]: Killing listener thread");
            m_log.Info("[SHUTDOWN]: Killing clients");
            // TODO: implement this
            m_log.Info("[SHUTDOWN]: Closing console and terminating");

            m_sceneManager.Close();

            base.Shutdown();
        }

        /// <summary>
        /// Get the start time and up time of Region server
        /// </summary>
        /// <param name="starttime">The first out parameter describing when the Region server started</param>
        /// <param name="uptime">The second out parameter describing how long the Region server has run</param>
        public void GetRunTime(out string starttime, out string uptime)
        {
            starttime = m_startuptime.ToString();
            uptime = (DateTime.Now - m_startuptime).ToString();
        }

        /// <summary>
        /// Get the number of the avatars in the Region server
        /// </summary>
        /// <param name="usernum">The first out parameter describing the number of all the avatars in the Region server</param>
        public void GetAvatarNumber(out int usernum)
        {
            usernum = m_sceneManager.GetCurrentSceneAvatars().Count;
        }

        /// <summary>
        /// Get the number of regions
        /// </summary>
        /// <param name="regionnum">The first out parameter describing the number of regions</param>
        public void GetRegionNumber(out int regionnum)
        {
            regionnum = m_sceneManager.Scenes.Count;
        }
    }

    public class OpenSimConfigSource
    {
        public IConfigSource Source;

        public void Save(string path)
        {
            if (Source is IniConfigSource)
            {
                IniConfigSource iniCon = (IniConfigSource)Source;
                iniCon.Save(path);
            }
            else if (Source is XmlConfigSource)
            {
                XmlConfigSource xmlCon = (XmlConfigSource)Source;
                xmlCon.Save(path);
            }
        }
    }
}



