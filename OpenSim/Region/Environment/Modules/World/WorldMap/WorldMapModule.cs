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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using OpenMetaverse.StructuredData;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Communications.Capabilities;
using OpenSim.Framework.Servers;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Environment.Types;
using Caps = OpenSim.Framework.Communications.Capabilities.Caps;

using OSD = OpenMetaverse.StructuredData.OSD;
using OSDMap = OpenMetaverse.StructuredData.OSDMap;
using OSDArray = OpenMetaverse.StructuredData.OSDArray;

namespace OpenSim.Region.Environment.Modules.World.WorldMap
{
    public class WorldMapModule : IRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string m_mapLayerPath = "0001/";

        private OpenSim.Framework.BlockingQueue<MapRequestState> requests = new OpenSim.Framework.BlockingQueue<MapRequestState>();

        //private IConfig m_config;
        protected Scene m_scene;
        private List<MapBlockData> cachedMapBlocks = new List<MapBlockData>();
        private int cachedTime = 0;
        private byte[] myMapImageJPEG;
        protected bool m_Enabled = false;
        private Dictionary<UUID, MapRequestState> m_openRequests = new Dictionary<UUID, MapRequestState>();
        private Dictionary<string, int> m_blacklistedurls = new Dictionary<string, int>();
        private Dictionary<ulong, int> m_blacklistedregions = new Dictionary<ulong, int>();
        private Dictionary<ulong, string> m_cachedRegionMapItemsAddress = new Dictionary<ulong, string>();
        private List<UUID> m_rootAgents = new List<UUID>();
        private Thread mapItemReqThread;
        private volatile bool threadrunning = false;

        //private int CacheRegionsDistance = 256;

        #region IRegionModule Members

        public virtual void Initialise(Scene scene, IConfigSource config)
        {
            IConfig startupConfig = config.Configs["Startup"];
            if (startupConfig.GetString("WorldMapModule", "WorldMap") ==
                    "WorldMap")
                m_Enabled = true;

            if (!m_Enabled)
                return;

            m_scene = scene;
        }

        public virtual void PostInitialise()
        {
            if (m_Enabled)
                AddHandlers();
        }

        public virtual void Close()
        {
        }

        public virtual string Name
        {
            get { return "WorldMapModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        protected virtual void AddHandlers()
        {
            myMapImageJPEG = new byte[0];

            string regionimage = "regionImage" + m_scene.RegionInfo.RegionID.ToString();
            regionimage = regionimage.Replace("-", "");
            m_log.Warn("[WORLD MAP]: JPEG Map location: http://" + m_scene.RegionInfo.ExternalEndPoint.Address.ToString() + ":" + m_scene.RegionInfo.HttpPort.ToString() + "/index.php?method=" + regionimage);

            m_scene.CommsManager.HttpServer.AddHTTPHandler(regionimage, OnHTTPGetMapImage);
            m_scene.CommsManager.HttpServer.AddLLSDHandler(
                "/MAP/MapItems/" + m_scene.RegionInfo.RegionHandle.ToString(), HandleRemoteMapItemRequest);

            m_scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            m_scene.EventManager.OnNewClient += OnNewClient;
            m_scene.EventManager.OnClientClosed += ClientLoggedOut;
            m_scene.EventManager.OnMakeChildAgent += MakeChildAgent;
            m_scene.EventManager.OnMakeRootAgent += MakeRootAgent;
        }

        public void OnRegisterCaps(UUID agentID, Caps caps)
        {
            m_log.DebugFormat("[WORLD MAP]: OnRegisterCaps: agentID {0} caps {1}", agentID, caps);
            string capsBase = "/CAPS/" + caps.CapsObjectPath;
            caps.RegisterHandler("MapLayer",
                                 new RestStreamHandler("POST", capsBase + m_mapLayerPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                           {
                                                               return MapLayerRequest(request, path, param,
                                                                                      agentID, caps);
                                                           }));
        }

        /// <summary>
        /// Callback for a map layer request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param"></param>
        /// <param name="agentID"></param>
        /// <param name="caps"></param>
        /// <returns></returns>
        public string MapLayerRequest(string request, string path, string param,
                                      UUID agentID, Caps caps)
        {
            //try
            //{
                //m_log.DebugFormat("[MAPLAYER]: request: {0}, path: {1}, param: {2}, agent:{3}",
                                  //request, path, param,agentID.ToString());

            // this is here because CAPS map requests work even beyond the 10,000 limit.
            ScenePresence avatarPresence = null;

            m_scene.TryGetAvatar(agentID, out avatarPresence);

            if (avatarPresence != null)
            {
                bool lookup = false;

                lock (cachedMapBlocks)
                {
                    if (cachedMapBlocks.Count > 0 && ((cachedTime + 1800) > Util.UnixTimeSinceEpoch()))
                    {
                        List<MapBlockData> mapBlocks;

                        mapBlocks = cachedMapBlocks;
                        avatarPresence.ControllingClient.SendMapBlock(mapBlocks, 0);
                    }
                    else
                    {
                        lookup = true;
                    }
                }
                if (lookup)
                {
                    List<MapBlockData> mapBlocks;

                    mapBlocks = m_scene.SceneGridService.RequestNeighbourMapBlocks((int)m_scene.RegionInfo.RegionLocX - 8, (int)m_scene.RegionInfo.RegionLocY - 8, (int)m_scene.RegionInfo.RegionLocX + 8, (int)m_scene.RegionInfo.RegionLocY + 8);
                    avatarPresence.ControllingClient.SendMapBlock(mapBlocks,0);

                    lock (cachedMapBlocks)
                        cachedMapBlocks = mapBlocks;

                    cachedTime = Util.UnixTimeSinceEpoch();
                }
            }
            LLSDMapLayerResponse mapResponse = new LLSDMapLayerResponse();
            mapResponse.LayerData.Array.Add(GetOSDMapLayerResponse());
            return mapResponse.ToString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mapReq"></param>
        /// <returns></returns>
        public LLSDMapLayerResponse GetMapLayer(LLSDMapRequest mapReq)
        {
            m_log.Debug("[WORLD MAP]: MapLayer Request in region: " + m_scene.RegionInfo.RegionName);
            LLSDMapLayerResponse mapResponse = new LLSDMapLayerResponse();
            mapResponse.LayerData.Array.Add(GetOSDMapLayerResponse());
            return mapResponse;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected static OSDMapLayer GetOSDMapLayerResponse()
        {
            OSDMapLayer mapLayer = new OSDMapLayer();
            mapLayer.Right = 5000;
            mapLayer.Top = 5000;
            mapLayer.ImageID = new UUID("00000000-0000-1111-9999-000000000006");

            return mapLayer;
        }
        #region EventHandlers

        /// <summary>
        /// Registered for event
        /// </summary>
        /// <param name="client"></param>
        private void OnNewClient(IClientAPI client)
        {
            client.OnRequestMapBlocks += RequestMapBlocks;
            client.OnMapItemRequest += HandleMapItemRequest;            
        }

        /// <summary>
        /// Client logged out, check to see if there are any more root agents in the simulator
        /// If not, stop the mapItemRequest Thread
        /// Event handler
        /// </summary>
        /// <param name="AgentId">AgentID that logged out</param>
        private void ClientLoggedOut(UUID AgentId)
        {
            List<ScenePresence> presences = m_scene.GetAvatars();
            int rootcount = 0;
            for (int i=0;i<presences.Count;i++)
            {
                if (presences[i] != null)
                {
                    if (!presences[i].IsChildAgent)
                        rootcount++;
                }
            }
            if (rootcount <= 1)
                StopThread();
            
            lock (m_rootAgents)
            {
                if (m_rootAgents.Contains(AgentId))
                {
                    m_rootAgents.Remove(AgentId);
                }
            }
        }
        #endregion

        /// <summary>
        /// Starts the MapItemRequest Thread
        /// Note that this only gets started when there are actually agents in the region
        /// Additionally, it gets stopped when there are none.
        /// </summary>
        /// <param name="o"></param>
        private void StartThread(object o)
        {
            if (threadrunning) return;
            threadrunning = true;
            m_log.Warn("[WORLD MAP]: Starting remote MapItem request thread");
            mapItemReqThread = new Thread(new ThreadStart(process));
            mapItemReqThread.IsBackground = true;
            mapItemReqThread.Name = "MapItemRequestThread";
            mapItemReqThread.Priority = ThreadPriority.BelowNormal;
            mapItemReqThread.SetApartmentState(ApartmentState.MTA);
            mapItemReqThread.Start();
            ThreadTracker.Add(mapItemReqThread);
        }

        /// <summary>
        /// Enqueues a 'stop thread' MapRequestState.  Causes the MapItemRequest thread to end
        /// </summary>
        private void StopThread()
        {
            MapRequestState st = new MapRequestState();
            st.agentID=UUID.Zero;
            st.EstateID=0;
            st.flags=0;
            st.godlike=false;
            st.itemtype=0;
            st.regionhandle=0;

            requests.Enqueue(st);
        }

        public virtual void HandleMapItemRequest(IClientAPI remoteClient, uint flags, 
            uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
        {
            lock (m_rootAgents)
            {
                if (!m_rootAgents.Contains(remoteClient.AgentId))
                    return;
            }
            uint xstart = 0;
            uint ystart = 0;
            Utils.LongToUInts(m_scene.RegionInfo.RegionHandle, out xstart, out ystart);
            if (itemtype == 6) // we only sevice 6 right now (avatar green dots)
            {
                if (regionhandle == 0 || regionhandle == m_scene.RegionInfo.RegionHandle)
                {    
                    // Local Map Item Request
                    List<ScenePresence> avatars = m_scene.GetAvatars();
                    int tc = System.Environment.TickCount;
                    List<mapItemReply> mapitems = new List<mapItemReply>();
                    mapItemReply mapitem = new mapItemReply();
                    if (avatars.Count == 0 || avatars.Count == 1)
                    {
                        mapitem = new mapItemReply();
                        mapitem.x = (uint)(xstart + 1);
                        mapitem.y = (uint)(ystart + 1);
                        mapitem.id = UUID.Zero;
                        mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
                        mapitem.Extra = 0;
                        mapitem.Extra2 = 0;
                        mapitems.Add(mapitem);
                    }
                    else
                    {
                        foreach (ScenePresence av in avatars)
                        {
                            // Don't send a green dot for yourself
                            if (av.UUID != remoteClient.AgentId)
                            {
                                mapitem = new mapItemReply();
                                mapitem.x = (uint)(xstart + av.AbsolutePosition.X);
                                mapitem.y = (uint)(ystart + av.AbsolutePosition.Y);
                                mapitem.id = UUID.Zero;
                                mapitem.name = Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString());
                                mapitem.Extra = 1;
                                mapitem.Extra2 = 0;
                                mapitems.Add(mapitem);
                            }
                        }
                    }
                    remoteClient.SendMapItemReply(mapitems.ToArray(), itemtype, flags);
                }
                else
                {
                    // Remote Map Item Request

                    // ensures that the blockingqueue doesn't get borked if the GetAgents() timing changes.
                    // Note that we only start up a remote mapItem Request thread if there's users who could 
                    // be making requests
                    if (!threadrunning)
                    {
                        m_log.Warn("[WORLD MAP]: Starting new remote request thread manually.  This means that AvatarEnteringParcel never fired!  This needs to be fixed!  Don't Mantis this, as the developers can see it in this message");
                        StartThread(new object());
                    }

                    RequestMapItems("",remoteClient.AgentId,flags,EstateID,godlike,itemtype,regionhandle);
                }
            }
        }

        /// <summary>
        /// Processing thread main() loop for doing remote mapitem requests
        /// </summary>
        public void process()
        {
            try
            {
                while (true)
                {
                    MapRequestState st = requests.Dequeue();
                    
                    // end gracefully
                    if (st.agentID == UUID.Zero)
                    {
                        ThreadTracker.Remove(mapItemReqThread);
                        break;
                    }
                    
                    bool dorequest = true;
                    lock (m_rootAgents)
                    {
                        if (!m_rootAgents.Contains(st.agentID))
                            dorequest = false;
                    }

                    if (dorequest)
                    {
                        OSDMap response = RequestMapItemsAsync("", st.agentID, st.flags, st.EstateID, st.godlike, st.itemtype, st.regionhandle);
                        RequestMapItemsCompleted(response);
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[WORLD MAP]: Map item request thread terminated abnormally with exception {0}", e);
            }
            
            threadrunning = false;
            m_log.Debug("[WORLD MAP]: Remote request thread exiting");
        }

        /// <summary>
        /// Enqueues the map item request into the processing thread
        /// </summary>
        /// <param name="state"></param>
        public void EnqueueMapItemRequest(MapRequestState state)
        {
            requests.Enqueue(state);
        }

        /// <summary>
        /// Sends the mapitem response to the IClientAPI
        /// </summary>
        /// <param name="response">The OSDMap Response for the mapitem</param>
        private void RequestMapItemsCompleted(OSDMap response)
        {
            UUID requestID = response["requestID"].AsUUID();

            if (requestID != UUID.Zero)
            {
                MapRequestState mrs = new MapRequestState();
                mrs.agentID = UUID.Zero;
                lock (m_openRequests)
                {
                    if (m_openRequests.ContainsKey(requestID))
                    {
                        mrs = m_openRequests[requestID];
                        m_openRequests.Remove(requestID);
                    }
                }

                if (mrs.agentID != UUID.Zero)
                {
                    ScenePresence av = null;
                    m_scene.TryGetAvatar(mrs.agentID, out av);
                    if (av != null)
                    {
                        if (response.ContainsKey(mrs.itemtype.ToString()))
                        {
                            List<mapItemReply> returnitems = new List<mapItemReply>();
                            OSDArray itemarray = (OSDArray)response[mrs.itemtype.ToString()];
                            for (int i = 0; i < itemarray.Count; i++)
                            {
                                OSDMap mapitem = (OSDMap)itemarray[i];
                                mapItemReply mi = new mapItemReply();
                                mi.x = (uint)mapitem["X"].AsInteger();
                                mi.y = (uint)mapitem["Y"].AsInteger();
                                mi.id = mapitem["ID"].AsUUID();
                                mi.Extra = mapitem["Extra"].AsInteger();
                                mi.Extra2 = mapitem["Extra2"].AsInteger();
                                mi.name = mapitem["Name"].AsString();
                                returnitems.Add(mi);
                            }
                            av.ControllingClient.SendMapItemReply(returnitems.ToArray(), mrs.itemtype, mrs.flags);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Enqueue the MapItem request for remote processing
        /// </summary>
        /// <param name="httpserver">blank string, we discover this in the process</param>
        /// <param name="id">Agent ID that we are making this request on behalf</param>
        /// <param name="flags">passed in from packet</param>
        /// <param name="EstateID">passed in from packet</param>
        /// <param name="godlike">passed in from packet</param>
        /// <param name="itemtype">passed in from packet</param>
        /// <param name="regionhandle">Region we're looking up</param>
        public void RequestMapItems(string httpserver, UUID id, uint flags,
            uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
        {
            MapRequestState st = new MapRequestState();
            st.agentID = id; 
            st.flags = flags; 
            st.EstateID = EstateID; 
            st.godlike = godlike; 
            st.itemtype = itemtype; 
            st.regionhandle = regionhandle;
            EnqueueMapItemRequest(st);
        }

        /// <summary>
        /// Does the actual remote mapitem request
        /// This should be called from an asynchronous thread
        /// Request failures get blacklisted until region restart so we don't 
        /// continue to spend resources trying to contact regions that are down.
        /// </summary>
        /// <param name="httpserver">blank string, we discover this in the process</param>
        /// <param name="id">Agent ID that we are making this request on behalf</param>
        /// <param name="flags">passed in from packet</param>
        /// <param name="EstateID">passed in from packet</param>
        /// <param name="godlike">passed in from packet</param>
        /// <param name="itemtype">passed in from packet</param>
        /// <param name="regionhandle">Region we're looking up</param>
        /// <returns></returns>
        private OSDMap RequestMapItemsAsync(string httpserver, UUID id, uint flags,
            uint EstateID, bool godlike, uint itemtype, ulong regionhandle)
        {
            bool blacklisted = false;
            lock (m_blacklistedregions)
            {
                if (m_blacklistedregions.ContainsKey(regionhandle))
                    blacklisted = true;
            }

            if (blacklisted)
                return new OSDMap();

            UUID requestID = UUID.Random();
            lock (m_cachedRegionMapItemsAddress)
            {
                if (m_cachedRegionMapItemsAddress.ContainsKey(regionhandle))
                    httpserver = m_cachedRegionMapItemsAddress[regionhandle];
            }
            if (httpserver.Length == 0)
            {
                RegionInfo mreg = m_scene.SceneGridService.RequestNeighbouringRegionInfo(regionhandle);
                
                if (mreg != null)
                {
                    httpserver = "http://" + mreg.ExternalEndPoint.Address.ToString() + ":" + mreg.HttpPort + "/MAP/MapItems/" + regionhandle.ToString();
                    lock (m_cachedRegionMapItemsAddress)
                    {
                        if (!m_cachedRegionMapItemsAddress.ContainsKey(regionhandle))
                            m_cachedRegionMapItemsAddress.Add(regionhandle, httpserver);
                    }
                }
                else
                {
                    lock (m_blacklistedregions)
                    {
                        if (!m_blacklistedregions.ContainsKey(regionhandle))
                            m_blacklistedregions.Add(regionhandle, System.Environment.TickCount);
                    }
                    m_log.WarnFormat("[WORLD MAP]: Blacklisted region {0}", regionhandle.ToString());
                }
            }

            blacklisted = false;
            lock (m_blacklistedurls)
            {
                if (m_blacklistedurls.ContainsKey(httpserver))
                    blacklisted = true;
            }

            // Can't find the http server
            if (httpserver.Length == 0 || blacklisted)
                return new OSDMap();

            MapRequestState mrs = new MapRequestState();
            mrs.agentID = id;
            mrs.EstateID = EstateID;
            mrs.flags = flags;
            mrs.godlike = godlike;
            mrs.itemtype=itemtype;
            mrs.regionhandle = regionhandle;

            lock (m_openRequests)
                m_openRequests.Add(requestID, mrs);

            WebRequest mapitemsrequest = WebRequest.Create(httpserver);
            mapitemsrequest.Method = "POST";
            mapitemsrequest.ContentType = "application/xml+llsd";
            OSDMap RAMap = new OSDMap();

            // string RAMapString = RAMap.ToString();
            OSD LLSDofRAMap = RAMap; // RENAME if this works

            byte[] buffer = OSDParser.SerializeLLSDXmlBytes(LLSDofRAMap);
            OSDMap responseMap = new OSDMap();
            responseMap["requestID"] = OSD.FromUUID(requestID);

            Stream os = null;
            try
            { // send the Post
                mapitemsrequest.ContentLength = buffer.Length;   //Count bytes to send
                os = mapitemsrequest.GetRequestStream();
                os.Write(buffer, 0, buffer.Length);         //Send it
                os.Close();
                //m_log.DebugFormat("[WORLD MAP]: Getting MapItems from Sim {0}", httpserver);
            }
            catch (WebException ex)
            {
                m_log.InfoFormat("[WORLD MAP]: Bad send on GetMapItems {0}", ex.Message);
                responseMap["connect"] = OSD.FromBoolean(false);
                lock (m_blacklistedurls)
                {
                    if (!m_blacklistedurls.ContainsKey(httpserver))
                        m_blacklistedurls.Add(httpserver, System.Environment.TickCount);
                }

                m_log.WarnFormat("[WORLD MAP]: Blacklisted {0}", httpserver);

                return responseMap;
            }

            string response_mapItems_reply = null;
            { // get the response
                try
                {
                    WebResponse webResponse = mapitemsrequest.GetResponse();
                    if (webResponse != null)
                    {
                        StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                        response_mapItems_reply = sr.ReadToEnd().Trim();
                    }
                    else
                    {
                        return new OSDMap();
                    }
                }
                catch (WebException)
                {
                    responseMap["connect"] = OSD.FromBoolean(false);
                    lock (m_blacklistedurls)
                    {
                        if (!m_blacklistedurls.ContainsKey(httpserver))
                            m_blacklistedurls.Add(httpserver, System.Environment.TickCount);
                    }

                    m_log.WarnFormat("[WORLD MAP]: Blacklisted {0}", httpserver);

                    return responseMap;
                }
                OSD rezResponse = null;
                try
                {
                    rezResponse = OSDParser.DeserializeLLSDXml(response_mapItems_reply);

                    responseMap = (OSDMap)rezResponse;
                    responseMap["requestID"] = OSD.FromUUID(requestID);
                }
                catch (Exception)
                {
                    //m_log.InfoFormat("[OGP]: exception on parse of rez reply {0}", ex.Message);
                    responseMap["connect"] = OSD.FromBoolean(false);

                    return responseMap;
                }
            }
            return responseMap;
        }

        /// <summary>
        /// Requests map blocks in area of minX, maxX, minY, MaxY in world cordinates
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        public virtual void RequestMapBlocks(IClientAPI remoteClient, int minX, int minY, int maxX, int maxY, uint flag)
        {
            List<MapBlockData> mapBlocks;
            if ((flag & 0x10000) != 0)  // user clicked on the map a tile that isn't visible
            {
                List<MapBlockData> response = new List<MapBlockData>();
                
                // this should return one mapblock at most. But make sure: Look whether the one we requested is in there
                mapBlocks = m_scene.SceneGridService.RequestNeighbourMapBlocks(minX, minY, maxX, maxY);
                if (mapBlocks != null)
                {
                    foreach (MapBlockData block in mapBlocks)
                    {
                        if (block.X == minX && block.Y == minY)
                        {
                            // found it => add it to response
                            response.Add(block);
                            break;
                        }
                    }
                }
                
                if (response.Count == 0)
                {
                    // response still empty => couldn't find the map-tile the user clicked on => tell the client
                    MapBlockData block = new MapBlockData();
                    block.X = (ushort)minX;
                    block.Y = (ushort)minY;
                    block.Access = 254; // == not there
                    response.Add(block);
                }
                remoteClient.SendMapBlock(response, 0);
            }
            else
            {
                // normal mapblock request. Use the provided values
                mapBlocks = m_scene.SceneGridService.RequestNeighbourMapBlocks(minX - 4, minY - 4, maxX + 4, maxY + 4);
                remoteClient.SendMapBlock(mapBlocks, flag);
            }
        }

        public Hashtable OnHTTPGetMapImage(Hashtable keysvals)
        {
            m_log.Info("[WORLD MAP]: Sending map image jpeg");
            Hashtable reply = new Hashtable();
            int statuscode = 200;
            byte[] jpeg = new byte[0];

            if (myMapImageJPEG.Length == 0)
            {
                MemoryStream imgstream = new MemoryStream();
                Bitmap mapTexture = new Bitmap(1,1);
                ManagedImage managedImage;
                Image image = (Image)mapTexture;

                try
                {
                    // Taking our jpeg2000 data, decoding it, then saving it to a byte array with regular jpeg data

                    imgstream = new MemoryStream();

                    // non-async because we know we have the asset immediately.
                    AssetBase mapasset = m_scene.AssetCache.GetAsset(m_scene.RegionInfo.lastMapUUID, true);

                    // Decode image to System.Drawing.Image
                    if (OpenJPEG.DecodeToImage(mapasset.Data, out managedImage, out image))
                    {
                        // Save to bitmap
                        mapTexture = new Bitmap(image);

                        EncoderParameters myEncoderParameters = new EncoderParameters();
                        myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

                        // Save bitmap to stream
                        mapTexture.Save(imgstream, GetEncoderInfo("image/jpeg"), myEncoderParameters);

                        // Write the stream to a byte array for output
                        jpeg = imgstream.ToArray();
                        myMapImageJPEG = jpeg;
                    }
                }
                catch (Exception)
                {
                    // Dummy!
                    m_log.Warn("[WORLD MAP]: Unable to generate Map image");
                }
                finally
                {
                    // Reclaim memory, these are unmanaged resources
                    mapTexture.Dispose();
                    image.Dispose();
                    imgstream.Close();
                    imgstream.Dispose();
                }
            }
            else
            {
                // Use cached version so we don't have to loose our mind
                jpeg = myMapImageJPEG;
            }

            reply["str_response_string"] = Convert.ToBase64String(jpeg);
            reply["int_response_code"] = statuscode;
            reply["content_type"] = "image/jpeg";

            return reply;
        }

        // From msdn
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (int j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        public OSD HandleRemoteMapItemRequest(string path, OSD request, string endpoint)
        {
            uint xstart = 0;
            uint ystart = 0;
            
            Utils.LongToUInts(m_scene.RegionInfo.RegionHandle,out xstart,out ystart);

            OSDMap responsemap = new OSDMap();
            List<ScenePresence> avatars = m_scene.GetAvatars();
            OSDArray responsearr = new OSDArray(avatars.Count);
            OSDMap responsemapdata = new OSDMap();
            int tc = System.Environment.TickCount;
            /* 
            foreach (ScenePresence av in avatars)
            {
                responsemapdata = new OSDMap();
                responsemapdata["X"] = OSD.FromInteger((int)(xstart + av.AbsolutePosition.X));
                responsemapdata["Y"] = OSD.FromInteger((int)(ystart + av.AbsolutePosition.Y));
                responsemapdata["ID"] = OSD.FromUUID(UUID.Zero);
                responsemapdata["Name"] = OSD.FromString("TH");
                responsemapdata["Extra"] = OSD.FromInteger(0);
                responsemapdata["Extra2"] = OSD.FromInteger(0);
                responsearr.Add(responsemapdata);
            }
            responsemap["1"] = responsearr;
            */
            if (avatars.Count == 0)
            {
                responsemapdata = new OSDMap();
                responsemapdata["X"] = OSD.FromInteger((int)(xstart + 1));
                responsemapdata["Y"] = OSD.FromInteger((int)(ystart + 1));
                responsemapdata["ID"] = OSD.FromUUID(UUID.Zero);
                responsemapdata["Name"] = OSD.FromString(Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString()));
                responsemapdata["Extra"] = OSD.FromInteger(0);
                responsemapdata["Extra2"] = OSD.FromInteger(0);
                responsearr.Add(responsemapdata);
                
                responsemap["6"] = responsearr;
            }
            else 
            {
                responsearr = new OSDArray(avatars.Count);
                foreach (ScenePresence av in avatars)
                {
                    responsemapdata = new OSDMap();
                    responsemapdata["X"] = OSD.FromInteger((int)(xstart + av.AbsolutePosition.X));
                    responsemapdata["Y"] = OSD.FromInteger((int)(ystart + av.AbsolutePosition.Y));
                    responsemapdata["ID"] = OSD.FromUUID(UUID.Zero);
                    responsemapdata["Name"] = OSD.FromString(Util.Md5Hash(m_scene.RegionInfo.RegionName + tc.ToString()));
                    responsemapdata["Extra"] = OSD.FromInteger(1);
                    responsemapdata["Extra2"] = OSD.FromInteger(0);
                    responsearr.Add(responsemapdata);
                }
                responsemap["6"] = responsearr;
            }
            return responsemap;
        }

        private void MakeRootAgent(ScenePresence avatar)
        { 
            // You may ask, why this is in a threadpool to start with..   
            // The reason is so we don't cause the thread to freeze waiting 
            // for the 1 second it costs to start a thread manually.
            
            m_log.Warn("[WORLD MAP]: MakeRootAgent Works!");
            if (!threadrunning)
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.StartThread));

            lock (m_rootAgents)
            {
                if (!m_rootAgents.Contains(avatar.UUID))
                {
                    m_rootAgents.Add(avatar.UUID);
                }
            }
        }

        private void MakeChildAgent(ScenePresence avatar)
        {
            List<ScenePresence> presences = m_scene.GetAvatars();
            int rootcount = 0;
            for (int i = 0; i < presences.Count; i++)
            {
                if (presences[i] != null)
                {
                    if (!presences[i].IsChildAgent)
                        rootcount++;
                }
            }
            if (rootcount <= 1)
                StopThread();

            lock (m_rootAgents)
            {
                if (m_rootAgents.Contains(avatar.UUID))
                {
                    m_rootAgents.Remove(avatar.UUID);
                }
            }
        }
    }

    public struct MapRequestState
    {
        public UUID agentID;
        public uint flags;
        public uint EstateID;
        public bool godlike;
        public uint itemtype;
        public ulong regionhandle;
    }
}
