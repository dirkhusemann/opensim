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
using System.Reflection;
using System.Collections.Generic;
using System.Net;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Environment.Scenes.Hypergrid;
using OpenMetaverse;
using log4net;
using Nini.Config;

namespace OpenSim.Region.Environment.Modules.World.WorldMap
{
    public class MapSearchModule : IRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Scene m_scene = null; // only need one for communication with GridService
        private Random random;

        #region IRegionModule Members
        public void Initialise(Scene scene, IConfigSource source)
        {
            if (m_scene == null)
            {
                m_scene = scene;
                random = new Random();
            }

            scene.EventManager.OnNewClient += OnNewClient;
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
            m_scene = null;
        }

        public string Name
        {
            get { return "MapSearchModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion

        private void OnNewClient(IClientAPI client)
        {
            client.OnMapNameRequest += OnMapNameRequest;
        }

        private void OnMapNameRequest(IClientAPI remoteClient, string mapName)
        {
            if (mapName.Length < 3)
            {
                remoteClient.SendAlertMessage("Use a search string with at least 3 characters");
                return;
            }
            
            // try to fetch from GridServer
            List<RegionInfo> regionInfos = m_scene.SceneGridService.RequestNamedRegions(mapName, 20);
            if (regionInfos == null)
            {
                m_log.Warn("[MAPSEARCHMODULE]: RequestNamedRegions returned null. Old gridserver?");
                // service wasn't available; maybe still an old GridServer. Try the old API, though it will return only one region
                regionInfos = new List<RegionInfo>();
                RegionInfo info = m_scene.SceneGridService.RequestClosestRegion(mapName);
                if (info != null) regionInfos.Add(info);
            }

            if ((regionInfos.Count == 0) && IsHypergridOn())
            {
                // OK, we tried but there are no regions matching that name.
                // Let's check quickly if this is a domain name, and if so link to it
                if (mapName.Contains(".") && mapName.Contains(":"))
                {
                    // It probably is a domain name. Try to link to it.
                    TryLinkRegion(remoteClient, mapName, regionInfos);
                }
            }

            List<MapBlockData> blocks = new List<MapBlockData>();

            MapBlockData data;
            if (regionInfos.Count > 0)
            {
                foreach (RegionInfo info in regionInfos)
                {
                    data = new MapBlockData();
                    data.Agents = 0;
                    data.Access = 21; // TODO what's this?
                    data.MapImageId = info.RegionSettings.TerrainImageID;
                    data.Name = info.RegionName;
                    data.RegionFlags = 0; // TODO not used?
                    data.WaterHeight = 0; // not used
                    data.X = (ushort)info.RegionLocX;
                    data.Y = (ushort)info.RegionLocY;
                    blocks.Add(data);
                }
            }

            // final block, closing the search result
            data = new MapBlockData();
            data.Agents = 0;
            data.Access = 255;
            data.MapImageId = UUID.Zero;
            data.Name = mapName;
            data.RegionFlags = 0;
            data.WaterHeight = 0; // not used
            data.X = 0;
            data.Y = 0;
            blocks.Add(data);

            remoteClient.SendMapBlock(blocks, 0);
        }

        private bool IsHypergridOn()
        {
            return (m_scene.SceneGridService is HGSceneCommunicationService);
        }

        private void TryLinkRegion(IClientAPI client, string mapName, List<RegionInfo> regionInfos)
        {
            string host = "127.0.0.1";
            string portstr;
            uint port = 9000;
            string[] parts = mapName.Split(new char[] { ':' });
            if (parts.Length >= 1)
            {
                host = parts[0];
            }
            if (parts.Length >= 2)
            {
                portstr = parts[1];
                UInt32.TryParse(portstr, out port);
            }

            // Sanity check. Don't ever link to this sim.
            IPAddress ipaddr = null;
            try
            {
                ipaddr = Util.GetHostFromDNS(host);
            }
            catch { }

            if ((ipaddr != null) &&
                !((m_scene.RegionInfo.ExternalEndPoint.Address.Equals(ipaddr)) && (m_scene.RegionInfo.HttpPort == port)))
            {
                uint xloc = (uint)(random.Next(0, Int16.MaxValue));
                RegionInfo regInfo;
                bool success = TryCreateLink(client, xloc, 0, port, host, out regInfo);
                if (success)
                {
                    regInfo.RegionName = mapName;
                    regionInfos.Add(regInfo);
                }
            }
        }

        private bool TryCreateLink(IClientAPI client, uint xloc, uint yloc, uint externalPort, string externalHostName, out RegionInfo regInfo)
        {
            m_log.DebugFormat("[HGrid]: Dynamic link to {0}:{1}, in {2}-{3}", externalHostName, externalPort, xloc, yloc);

            regInfo = new RegionInfo();
            regInfo.RegionLocX = xloc; 
            regInfo.RegionLocY = yloc;
            regInfo.ExternalHostName = externalHostName;
            regInfo.HttpPort = externalPort;
            try
            {
                regInfo.InternalEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), (int)0);
            }
            catch (Exception e)
            {
                m_log.Warn("[HGrid] Wrong format for link-region: " + e.Message);
                return false;
            }
            //regInfo.RemotingAddress = regInfo.ExternalEndPoint.Address.ToString();

            // Finally, link it
            try
            {
                m_scene.CommsManager.GridService.RegisterRegion(regInfo);
            }
            catch (Exception e)
            {
                m_log.Warn("[HGrid] Unable to dynamically link region: " + e.Message);
                return false;
            }

            if (!Check4096(client, regInfo))
            {
                return false;
            }

            m_log.Debug("[HGrid] Dynamic link region succeeded");
            return true;
        }

        /// <summary>
        /// Cope with this viewer limitation.
        /// </summary>
        /// <param name="regInfo"></param>
        /// <returns></returns>
        private bool Check4096(IClientAPI client, RegionInfo regInfo)
        {
            ulong realHandle;
            if (UInt64.TryParse(regInfo.regionSecret, out realHandle))
            {
                uint x, y;
                Utils.LongToUInts(realHandle, out x, out y);
                x = x / Constants.RegionSize;
                y = y / Constants.RegionSize;

                if ((Math.Abs((int)m_scene.RegionInfo.RegionLocX - (int)x) >= 4096) ||
                    (Math.Abs((int)m_scene.RegionInfo.RegionLocY - (int)y) >= 4096))
                {
                    m_scene.CommsManager.GridService.DeregisterRegion(regInfo);
                    m_log.Debug("[HGrid]: Region deregistered.");
                    client.SendAlertMessage("Region is too far (" + x + ", " + y + ")");
                    return false;
                }
                return true;
            }
            else
            {
                m_scene.CommsManager.GridService.RegisterRegion(regInfo);
                m_log.Debug("[HGrid]: Gnomes. Region deregistered.");
                return false;
            }
        }

    }
}
