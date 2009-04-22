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
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Communications.Services;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.Interfaces;
using OpenSim.Framework.Statistics;
using OpenSim.Region.ClientStack;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;

namespace OpenSim
{
    /// <summary>
    /// Common OpenSim simulator code
    /// </summary>
    public class OpenSimBase : RegionApplicationBase
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // These are the names of the plugin-points extended by this
        // class during system startup.

        private const string PLUGIN_ASSET_CACHE = "/OpenSim/AssetCache";
        private const string PLUGIN_ASSET_SERVER_CLIENT = "/OpenSim/AssetClient";

        protected string proxyUrl;
        protected int proxyOffset = 0;

        protected bool m_autoCreateClientStack = true;

        /// <summary>
        /// The file used to load and save prim backup xml if no filename has been specified
        /// </summary>
        protected const string DEFAULT_PRIM_BACKUP_FILENAME = "prim-backup.xml";

        /// <summary>
        /// The file used to load and save an opensim archive if no filename has been specified
        /// </summary>
        protected const string DEFAULT_OAR_BACKUP_FILENAME = "scene.oar.tar.gz";

        public ConfigSettings ConfigurationSettings
        {
            get { return m_configSettings; }
            set { m_configSettings = value; }
        }

        protected ConfigSettings m_configSettings;

        protected ConfigurationLoader m_configLoader;

        protected GridInfoService m_gridInfoService;

        public ConsoleCommand CreateAccount = null;

        protected List<IApplicationPlugin> m_plugins = new List<IApplicationPlugin>();

        /// <value>
        /// The config information passed into the OpenSim region server.
        /// </value>        
        public OpenSimConfigSource ConfigSource
        {
            get { return m_config; }
            set { m_config = value; }
        }

        protected OpenSimConfigSource m_config;

        public List<IClientNetworkServer> ClientServers
        {
            get { return m_clientServers; }
        }

        protected List<IClientNetworkServer> m_clientServers = new List<IClientNetworkServer>();
       
        public uint HttpServerPort
        {
            get { return m_httpServerPort; }
        }

        public ModuleLoader ModuleLoader
        {
            get { return m_moduleLoader; }
            set { m_moduleLoader = value; }
        }

        protected ModuleLoader m_moduleLoader;

        protected IRegistryCore m_applicationRegistry = new RegistryCore();

        public IRegistryCore ApplicationRegistry
        {
            get { return m_applicationRegistry; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configSource"></param>
        public OpenSimBase(IConfigSource configSource) : base()
        {
            LoadConfigSettings(configSource);
        }

        protected virtual void LoadConfigSettings(IConfigSource configSource)
        {
            m_configLoader = new ConfigurationLoader();
            m_config = m_configLoader.LoadConfigSettings(configSource, out m_configSettings, out m_networkServersInfo);
            ReadExtraConfigSettings();
        }

        protected virtual void ReadExtraConfigSettings()
        {
            IConfig networkConfig = m_config.Source.Configs["Network"];
            if (networkConfig != null)
            {
                proxyUrl = networkConfig.GetString("proxy_url", "");
                proxyOffset = Int32.Parse(networkConfig.GetString("proxy_offset", "0"));
            }
        }

        protected virtual void LoadPlugins()
        {
            PluginLoader<IApplicationPlugin> loader =
                new PluginLoader<IApplicationPlugin>(new ApplicationPluginInitialiser(this));

            loader.Load("/OpenSim/Startup");
            m_plugins = loader.Plugins;
        }

        protected override List<string> GetHelpTopics()
        {
            List<string> topics = base.GetHelpTopics();
            Scene s = SceneManager.CurrentOrFirstScene;
            if (s != null && s.GetCommanders() != null)
                topics.AddRange(s.GetCommanders().Keys);

            return topics;
        }

        /// <summary>
        /// Performs startup specific to the region server, including initialization of the scene 
        /// such as loading configuration from disk.
        /// </summary>
        protected override void StartupSpecific()
        {
            IConfig startupConfig = m_config.Source.Configs["Startup"];
            if (startupConfig != null)
            {
                string pidFile = startupConfig.GetString("PIDFile", String.Empty);
                if (pidFile != String.Empty)
                    CreatePIDFile(pidFile);
            }

            base.StartupSpecific();

            m_stats = StatsManager.StartCollectingSimExtraStats();

            // Create a ModuleLoader instance
            m_moduleLoader = new ModuleLoader(m_config.Source);

            LoadPlugins();
            foreach (IApplicationPlugin plugin in m_plugins)
            {
                plugin.PostInitialise();
            }

            // Only enable logins to the regions once we have completely finished starting up (apart from scripts)
            if ((m_commsManager != null) && (m_commsManager.GridService != null))
            {
                m_commsManager.GridService.RegionLoginsEnabled = true;
            }

            AddPluginCommands();
        }

        protected virtual void AddPluginCommands()
        {
            // If console exists add plugin commands.
            if (m_console != null)
            {
                List<string> topics = GetHelpTopics();

                foreach (string topic in topics)
                {
                    m_console.Commands.AddCommand("plugin", false, "help " + topic,
                                                  "help " + topic,
                                                  "Get help on plugin command '" + topic + "'",
                                                  HandleCommanderHelp);

                    m_console.Commands.AddCommand("plugin", false, topic,
                                                  topic,
                                                  "Execute subcommand for plugin '" + topic + "'",
                                                  null);

                    ICommander commander = null;

                    Scene s = SceneManager.CurrentOrFirstScene;

                    if (s != null && s.GetCommanders() != null)
                    {
                        if (s.GetCommanders().ContainsKey(topic))
                            commander = s.GetCommanders()[topic];
                    }

                    if (commander == null)
                        continue;

                    foreach (string command in commander.Commands.Keys)
                    {
                        m_console.Commands.AddCommand(topic, false,
                                                      topic + " " + command,
                                                      topic + " " + commander.Commands[command].ShortHelp(),
                                                      String.Empty, HandleCommanderCommand);
                    }
                }
            }
        }

        private void HandleCommanderCommand(string module, string[] cmd)
        {
            m_sceneManager.SendCommandToPluginModules(cmd);
        }

        private void HandleCommanderHelp(string module, string[] cmd)
        {
            // Only safe for the interactive console, since it won't
            // let us come here unless both scene and commander exist
            //
            ICommander moduleCommander = SceneManager.CurrentOrFirstScene.GetCommander(cmd[1]);
            if (moduleCommander != null)
                m_console.Notice(moduleCommander.Help);
        }

        protected override void Initialize()
        {
            // Called from base.StartUp()

            m_httpServerPort = m_networkServersInfo.HttpListenerPort;
            InitialiseAssetCache();
            m_sceneManager.OnRestartSim += handleRestartRegion;
        }

        /// <summary>
        /// Initialises the asset cache. This supports legacy configuration values
        /// to ensure consistent operation, but values outside of that namespace
        /// are handled by the more generic resolution mechanism provided by 
        /// the ResolveAssetServer virtual method. If extended resolution fails, 
        /// then the normal default action is taken.
        /// Creation of the AssetCache is handled by ResolveAssetCache. This
        /// function accepts a reference to the instantiated AssetServer and
        /// returns an IAssetCache implementation, if possible. This is a virtual
        /// method.
        /// </summary>
        protected virtual void InitialiseAssetCache()
        {
            LegacyAssetClientPluginInitialiser linit = null;
            CryptoAssetClientPluginInitialiser cinit = null;
            AssetClientPluginInitialiser init = null;

            IAssetServer assetServer = null;
            string mode = m_configSettings.AssetStorage;

            if (mode == null | mode == String.Empty)
                mode = "default";

            // If "default" is specified, then the value is adjusted
            // according to whether or not the server is running in
            // standalone mode.
            if (mode.ToLower() == "default")
            {
                if (m_configSettings.Standalone == false)
                    mode = "grid";
                else
                    mode = "local";
            }

            switch (mode.ToLower())
            {
                    // If grid is specified then the grid server is chose regardless 
                    // of whether the server is standalone.
                case "grid":
                    linit = new LegacyAssetClientPluginInitialiser(m_configSettings, m_networkServersInfo.AssetURL);
                    assetServer = loadAssetServer("Grid", linit);
                    break;

                    // If cryptogrid is specified then the cryptogrid server is chose regardless 
                    // of whether the server is standalone.
                case "cryptogrid":
                    cinit = new CryptoAssetClientPluginInitialiser(m_configSettings, m_networkServersInfo.AssetURL,
                                                                   Environment.CurrentDirectory, true);
                    assetServer = loadAssetServer("Crypto", cinit);
                    break;

                    // If cryptogrid_eou is specified then the cryptogrid_eou server is chose regardless 
                    // of whether the server is standalone.
                case "cryptogrid_eou":
                    cinit = new CryptoAssetClientPluginInitialiser(m_configSettings, m_networkServersInfo.AssetURL,
                                                                   Environment.CurrentDirectory, false);
                    assetServer = loadAssetServer("Crypto", cinit);
                    break;

                    // If file is specified then the file server is chose regardless 
                    // of whether the server is standalone.
                case "file":
                    linit = new LegacyAssetClientPluginInitialiser(m_configSettings, m_networkServersInfo.AssetURL);
                    assetServer = loadAssetServer("File", linit);
                    break;

                    // If local is specified then we're going to use the local SQL server
                    // implementation. We drop through, because that will be the fallback
                    // for the following default clause too.
                case "local":
                    break;

                    // If the asset_database value is none of the previously mentioned strings, then we
                    // try to load a turnkey plugin that matches this value. If not we drop through to 
                    // a local default.
                default:
                    try
                    {
                        init = new AssetClientPluginInitialiser(m_configSettings);
                        assetServer = loadAssetServer(m_configSettings.AssetStorage, init);
                        break;
                    }
                    catch
                    {
                    }
                    m_log.Info("[OPENSIMBASE]: Default assetserver will be used");                
                    break;
            }

            // Open the local SQL-based database asset server
            if (assetServer == null)
            {
                init = new AssetClientPluginInitialiser(m_configSettings);
                SQLAssetServer sqlAssetServer = (SQLAssetServer) loadAssetServer("SQL", init);
                sqlAssetServer.LoadDefaultAssets(m_configSettings.AssetSetsXMLFile);
                assetServer = sqlAssetServer;
            }

            // Initialize the asset cache, passing a reference to the selected
            // asset server interface.
            m_assetCache = ResolveAssetCache(assetServer);
            
            assetServer.Start();
        }

        // This method loads the identified asset server, passing an approrpiately
        // initialized Initialise wrapper. There should to be exactly one match,
        // if not, then the first match is used.
        private IAssetServer loadAssetServer(string id, PluginInitialiserBase pi)
        {
            if (id != null && id != String.Empty)
            {
                m_log.DebugFormat("[OPENSIMBASE] Attempting to load asset server id={0}", id);

                try
                {
                    PluginLoader<IAssetServer> loader = new PluginLoader<IAssetServer>(pi);
                    loader.AddFilter(PLUGIN_ASSET_SERVER_CLIENT, new PluginProviderFilter(id));
                    loader.Load(PLUGIN_ASSET_SERVER_CLIENT);

                    if (loader.Plugins.Count > 0)
                    {
                        m_log.DebugFormat("[OPENSIMBASE] Asset server {0} loaded", id);
                        return (IAssetServer) loader.Plugins[0];
                    }
                }
                catch (Exception e)
                {
                    m_log.DebugFormat("[OPENSIMBASE] Asset server {0} not loaded ({1})", id, e.Message);
                }
            }
            return null;
        }

        /// <summary>
        /// Attempt to instantiate an IAssetCache implementation, using the
        /// provided IAssetServer reference.
        /// An asset cache implementation must provide a constructor that
        /// accepts two parameters;
        ///   [1] A ConfigSettings reference.
        ///   [2] An IAssetServer reference.
        /// The AssetCache value is obtained from the 
        /// [StartUp]/AssetCache value in the configuration file.
        /// </summary>
        protected virtual IAssetCache ResolveAssetCache(IAssetServer assetServer)
        {
            IAssetCache assetCache = null;
            if (m_configSettings.AssetCache != null && m_configSettings.AssetCache != String.Empty)
            {
                m_log.DebugFormat("[OPENSIMBASE]: Attempting to load asset cache id = {0}", m_configSettings.AssetCache);
                
                try
                {
                    PluginInitialiserBase init = new AssetCachePluginInitialiser(m_configSettings, assetServer);
                    PluginLoader<IAssetCache> loader = new PluginLoader<IAssetCache>(init);
                    loader.AddFilter(PLUGIN_ASSET_CACHE, new PluginProviderFilter(m_configSettings.AssetCache));

                    loader.Load(PLUGIN_ASSET_CACHE);
                    if (loader.Plugins.Count > 0)
                        assetCache = (IAssetCache) loader.Plugins[0];
                }
                catch (Exception e)
                {
                    m_log.Error("[OPENSIMBASE]: ResolveAssetCache failed");
                    m_log.Error(e);
                }
            }

            // If everything else fails, we force load the built-in asset cache
            return (IAssetCache) ((assetCache != null) ? assetCache : new AssetCache(assetServer));
        }

        public void ProcessLogin(bool LoginEnabled)
        {
            if (LoginEnabled)
            {
                m_log.Info("[LOGIN]: Login is now enabled.");
                m_commsManager.GridService.RegionLoginsEnabled = true;
            }
            else
            {
                m_log.Info("[LOGIN]: Login is now disabled.");
                m_commsManager.GridService.RegionLoginsEnabled = false;
            }
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, bool portadd_flag, out IScene scene)
        {
            return CreateRegion(regionInfo, portadd_flag, false, out scene);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, out IScene scene)
        {
            return CreateRegion(regionInfo, false, true, out scene);
        }

        /// <summary>
        /// Execute the region creation process.  This includes setting up scene infrastructure.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="portadd_flag"></param>
        /// <param name="do_post_init"></param>
        /// <returns></returns>
        public IClientNetworkServer CreateRegion(RegionInfo regionInfo, bool portadd_flag, bool do_post_init, out IScene mscene)
        {
            int port = regionInfo.InternalEndPoint.Port;

            // set initial RegionID to originRegionID in RegionInfo. (it needs for loding prims)
            // Commented this out because otherwise regions can't register with
            // the grid as there is already another region with the same UUID
            // at those coordinates. This is required for the load balancer to work.
            // --Mike, 2009.02.25
            //regionInfo.originRegionID = regionInfo.RegionID;

            // set initial ServerURI
            regionInfo.ServerURI = "http://" + regionInfo.ExternalHostName + ":" + regionInfo.InternalEndPoint.Port;
            regionInfo.HttpPort = m_httpServerPort;

            if ((proxyUrl.Length > 0) && (portadd_flag))
            {
                // set proxy url to RegionInfo
                regionInfo.proxyUrl = proxyUrl;
                regionInfo.ProxyOffset = proxyOffset;
                Util.XmlRpcCommand(proxyUrl, "AddPort", port, port + proxyOffset, regionInfo.ExternalHostName);
            }

            IClientNetworkServer clientServer;
            Scene scene = SetupScene(regionInfo, proxyOffset, m_config.Source, out clientServer);

            m_log.Info("[MODULES]: Loading Region's modules (old style)");

            List<IRegionModule> modules = m_moduleLoader.PickupModules(scene, ".");

            // This needs to be ahead of the script engine load, so the
            // script module can pick up events exposed by a module
            m_moduleLoader.InitialiseSharedModules(scene);

            // Use this in the future, the line above will be deprecated soon
            m_log.Info("[MODULES]: Loading Region's modules (new style)");
            IRegionModulesController controller;
            if (ApplicationRegistry.TryGet(out controller))
            {
                controller.AddRegionToModules(scene);
            }
            else m_log.Error("[MODULES]: The new RegionModulesController is missing...");

            scene.SetModuleInterfaces();

            // Prims have to be loaded after module configuration since some modules may be invoked during the load            
            scene.LoadPrimsFromStorage(regionInfo.originRegionID);

            scene.StartTimer();

            // moved these here as the terrain texture has to be created after the modules are initialized
            // and has to happen before the region is registered with the grid.
            scene.CreateTerrainTexture(false);

            try
            {
                scene.RegisterRegionWithGrid();
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[STARTUP]: Registration of region with grid failed, aborting startup - {0}", e);

                // Carrying on now causes a lot of confusion down the
                // line - we need to get the user's attention
                Environment.Exit(1);
            }

            // We need to do this after we've initialized the
            // scripting engines.
            scene.CreateScriptInstances();

            scene.loadAllLandObjectsFromStorage(regionInfo.originRegionID);
            scene.EventManager.TriggerParcelPrimCountUpdate();

            m_sceneManager.Add(scene);

            if (m_autoCreateClientStack)
            {
                m_clientServers.Add(clientServer);
                clientServer.Start();
            }

            if (do_post_init)
            {
                foreach (IRegionModule module in modules)
                {
                    module.PostInitialise();
                }
            }
            scene.EventManager.OnShutdown += delegate() { ShutdownRegion(scene); };

            mscene = scene;
            return clientServer;
        }

        private void ShutdownRegion(Scene scene)
        {
            m_log.DebugFormat("[SHUTDOWN]: Shutting down region {0}", scene.RegionInfo.RegionName);
            IRegionModulesController controller;
            if (ApplicationRegistry.TryGet<IRegionModulesController>(out controller))
            {
                controller.RemoveRegionFromModules(scene);
            }
        }

        public void RemoveRegion(Scene scene, bool cleanup)
        {
            // only need to check this if we are not at the
            // root level
            if ((m_sceneManager.CurrentScene != null) &&
                (m_sceneManager.CurrentScene.RegionInfo.RegionID == scene.RegionInfo.RegionID))
            {
                m_sceneManager.TrySetCurrentScene("..");
            }

            scene.DeleteAllSceneObjects();
            m_sceneManager.CloseScene(scene);

            if (!cleanup)
                return;

            if (!String.IsNullOrEmpty(scene.RegionInfo.RegionFile))
            {
                File.Delete(scene.RegionInfo.RegionFile);
                m_log.InfoFormat("[OPENSIM]: deleting region file \"{0}\"", scene.RegionInfo.RegionFile);
            }
        }

        public void RemoveRegion(string name, bool cleanUp)
        {
            Scene target;
            if (m_sceneManager.TryGetScene(name, out target))
                RemoveRegion(target, cleanUp);
        }

        /// <summary>
        /// Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="clientServer"> </param>
        /// <returns></returns>        
        protected Scene SetupScene(RegionInfo regionInfo, out IClientNetworkServer clientServer)
        {
            return SetupScene(regionInfo, 0, null, out clientServer);
        }

        /// <summary>
        /// Create a scene and its initial base structures.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <param name="proxyOffset"></param>
        /// <param name="configSource"></param>
        /// <param name="clientServer"> </param>
        /// <returns></returns>
        protected Scene SetupScene(
            RegionInfo regionInfo, int proxyOffset, IConfigSource configSource, out IClientNetworkServer clientServer)
        {
            AgentCircuitManager circuitManager = new AgentCircuitManager();
            IPAddress listenIP = regionInfo.InternalEndPoint.Address;
            //if (!IPAddress.TryParse(regionInfo.InternalEndPoint, out listenIP))
            //    listenIP = IPAddress.Parse("0.0.0.0");

            uint port = (uint) regionInfo.InternalEndPoint.Port;

            if (m_autoCreateClientStack)
            {
                clientServer
                    = m_clientStackManager.CreateServer(
                        listenIP, ref port, proxyOffset, regionInfo.m_allow_alternate_ports, configSource,
                        m_assetCache, circuitManager);
            }
            else
            {
                clientServer = null;
            }

            regionInfo.InternalEndPoint.Port = (int) port;

            Scene scene = CreateScene(regionInfo, m_storageManager, circuitManager);

            if (m_autoCreateClientStack)
            {
                clientServer.AddScene(scene);
            }

            scene.LoadWorldMap();

            scene.PhysicsScene = GetPhysicsScene(scene.RegionInfo.RegionName);
            scene.PhysicsScene.SetTerrain(scene.Heightmap.GetFloatsSerialised());
            scene.PhysicsScene.SetWaterLevel((float) regionInfo.RegionSettings.WaterHeight);

            // TODO: Remove this cruft once MasterAvatar is fully deprecated
            //Master Avatar Setup
            UserProfileData masterAvatar;
            if (scene.RegionInfo.MasterAvatarAssignedUUID == UUID.Zero)
            {
                masterAvatar =
                    m_commsManager.UserService.SetupMasterUser(scene.RegionInfo.MasterAvatarFirstName,
                                                               scene.RegionInfo.MasterAvatarLastName,
                                                               scene.RegionInfo.MasterAvatarSandboxPassword);
            }
            else
            {
                masterAvatar = m_commsManager.UserService.SetupMasterUser(scene.RegionInfo.MasterAvatarAssignedUUID);
                scene.RegionInfo.MasterAvatarFirstName = masterAvatar.FirstName;
                scene.RegionInfo.MasterAvatarLastName = masterAvatar.SurName;
            }

            if (masterAvatar == null)
            {
                m_log.Info("[PARCEL]: No master avatar found, using null.");
                scene.RegionInfo.MasterAvatarAssignedUUID = UUID.Zero;
            }
            else
            {
                m_log.InfoFormat("[PARCEL]: Found master avatar {0} {1} [" + masterAvatar.ID.ToString() + "]",
                                 scene.RegionInfo.MasterAvatarFirstName, scene.RegionInfo.MasterAvatarLastName);
                scene.RegionInfo.MasterAvatarAssignedUUID = masterAvatar.ID;
            }

            return scene;
        }

        protected override StorageManager CreateStorageManager()
        {
            return
                CreateStorageManager(m_configSettings.StorageConnectionString, m_configSettings.EstateConnectionString);
        }

        protected StorageManager CreateStorageManager(string connectionstring, string estateconnectionstring)
        {
            return new StorageManager(m_configSettings.StorageDll, connectionstring, estateconnectionstring);
        }

        protected override ClientStackManager CreateClientStackManager()
        {
            return new ClientStackManager(m_configSettings.ClientstackDll);
        }

        protected override Scene CreateScene(RegionInfo regionInfo, StorageManager storageManager,
                                             AgentCircuitManager circuitManager)
        {
            SceneCommunicationService sceneGridService = new SceneCommunicationService(m_commsManager);

            return new Scene(
                regionInfo, circuitManager, m_commsManager, sceneGridService,
                storageManager, m_moduleLoader, m_configSettings.DumpAssetsToFile, m_configSettings.PhysicalPrim,
                m_configSettings.See_into_region_from_neighbor, m_config.Source, m_version);
        }

        public void handleRestartRegion(RegionInfo whichRegion)
        {
            m_log.Info("[OPENSIM]: Got restart signal from SceneManager");

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
            IScene scene;
            CreateRegion(whichRegion, true, out scene);
        }

        # region Setup methods

        protected override PhysicsScene GetPhysicsScene(string osSceneIdentifier)
        {
            return GetPhysicsScene(
                m_configSettings.PhysicsEngine, m_configSettings.MeshEngineName, m_config.Source, osSceneIdentifier);
        }

        /// <summary>
        /// Handler to supply the current status of this sim
        /// </summary>
        /// Currently this is always OK if the simulator is still listening for connections on its HTTP service
        public class SimStatusHandler : IStreamedRequestHandler
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

        /// <summary>
        /// Handler to supply the current extended status of this sim 
        /// </summary>
        /// Currently prints the same a "show stats" plus the uptime of the sim
        public class XSimStatusHandler : IStreamedRequestHandler
        {
            OpenSimBase m_opensim;
        
            public XSimStatusHandler(OpenSimBase sim)
            // public XSimStatusHandler(BaseOpenSimServer sim)
            {
                m_opensim = sim;
            }
            
            public byte[] Handle(string path, Stream request,
                                 OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                return Encoding.UTF8.GetBytes(m_opensim.StatReport());
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
                get { return "/simstatusx/"; }
            }
        }


        #endregion

        /// <summary>
        /// Performs any last-minute sanity checking and shuts down the region server
        /// </summary>
        public override void ShutdownSpecific()
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

            try
            {
                m_sceneManager.Close();
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[SHUTDOWN]: Ignoring failure during shutdown - {0}", e);
            }
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
                IniConfigSource iniCon = (IniConfigSource) Source;
                iniCon.Save(path);
            }
            else if (Source is XmlConfigSource)
            {
                XmlConfigSource xmlCon = (XmlConfigSource) Source;
                xmlCon.Save(path);
            }
        }
    }
}
