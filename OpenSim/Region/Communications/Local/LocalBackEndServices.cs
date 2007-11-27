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
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
* 
*/
using System.Collections;
using System.Collections.Generic;
using libsecondlife;
using OpenSim.Framework;
using OpenSim.Framework.Communications;

namespace OpenSim.Region.Communications.Local
{
    public class LocalBackEndServices : IGridServices, IInterRegionCommunications
    {
        protected Dictionary<ulong, RegionInfo> m_regions = new Dictionary<ulong, RegionInfo>();

        protected Dictionary<ulong, RegionCommsListener> m_regionListeners =
            new Dictionary<ulong, RegionCommsListener>();

        private Dictionary<ulong, RegionInfo> m_remoteRegionInfoCache = new Dictionary<ulong, RegionInfo>();

        public string _gdebugRegionName = "";

        public string gdebugRegionName
        {
            get { return _gdebugRegionName; }
            set
            {
                _gdebugRegionName = value;

            }
        }
        public string _rdebugRegionName = "";

        public string rdebugRegionName
        {
            get { return _rdebugRegionName; }
            set
            {
                _rdebugRegionName = value;

            }
        }
        public LocalBackEndServices()
        {
        }

        /// <summary>
        /// Register a region method with the BackEnd Services.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <returns></returns>
        public RegionCommsListener RegisterRegion(RegionInfo regionInfo)
        {
            //Console.WriteLine("CommsManager - Region " + regionInfo.RegionHandle + " , " + regionInfo.RegionLocX + " , "+ regionInfo.RegionLocY +" is registering");
            if (!m_regions.ContainsKey(regionInfo.RegionHandle))
            {
                //Console.WriteLine("CommsManager - Adding Region " + regionInfo.RegionHandle );
                m_regions.Add(regionInfo.RegionHandle, regionInfo);

                RegionCommsListener regionHost = new RegionCommsListener();
                if (m_regionListeners.ContainsKey(regionInfo.RegionHandle))
                {
                    OpenSim.Framework.Console.MainLog.Instance.Error("INTERREGION", "Error:Region registered twice as an Events listener for Interregion Communications but not as a listed region.  In Standalone mode this will cause BIG issues.  In grid mode, it means a region went down and came back up.");
                    m_regionListeners.Remove(regionInfo.RegionHandle);
                }
                m_regionListeners.Add(regionInfo.RegionHandle, regionHost);

                return regionHost;
            }
            else
            {
                // Already in our list, so the region went dead and restarted.
                m_regions.Remove(regionInfo.RegionHandle);
                OpenSim.Framework.Console.MainLog.Instance.Warn("INTERREGION", "Region registered twice. Region went down and came back up.");

                RegionCommsListener regionHost = new RegionCommsListener();
                if (m_regionListeners.ContainsKey(regionInfo.RegionHandle))
                {
                    m_regionListeners.Remove(regionInfo.RegionHandle);
                }
                m_regionListeners.Add(regionInfo.RegionHandle, regionHost);

                return regionHost;
            }
            return null;
        }

        public bool DeregisterRegion(RegionInfo regionInfo)
        {
            if (m_regions.ContainsKey(regionInfo.RegionHandle))
            {
                m_regions.Remove(regionInfo.RegionHandle);
                if (m_regionListeners.ContainsKey(regionInfo.RegionHandle))
                {
                    m_regionListeners.Remove(regionInfo.RegionHandle);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <returns></returns>
        public List<SimpleRegionInfo> RequestNeighbours(uint x, uint y)
        {
            // Console.WriteLine("Finding Neighbours to " + regionInfo.RegionHandle);
            List<SimpleRegionInfo> neighbours = new List<SimpleRegionInfo>();

            foreach (RegionInfo reg in m_regions.Values)
            {
                // Console.WriteLine("CommsManager- RequestNeighbours() checking region " + reg.RegionLocX + " , "+ reg.RegionLocY);
                if (reg.RegionLocX != x || reg.RegionLocY != y)
                {
                    //Console.WriteLine("CommsManager- RequestNeighbours() - found a different region in list, checking location");
                    if ((reg.RegionLocX > (x - 2)) && (reg.RegionLocX < (x + 2)))
                    {
                        if ((reg.RegionLocY > (y - 2)) && (reg.RegionLocY < (y + 2)))
                        {
                            neighbours.Add(reg);
                        }
                    }
                }
            }
            return neighbours;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        public RegionInfo RequestNeighbourInfo(ulong regionHandle)
        {
            if (m_regions.ContainsKey(regionHandle))
            {
                return m_regions[regionHandle];
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <returns></returns>
        public List<MapBlockData> RequestNeighbourMapBlocks(int minX, int minY, int maxX, int maxY)
        {
            List<MapBlockData> mapBlocks = new List<MapBlockData>();
            foreach (RegionInfo regInfo in m_regions.Values)
            {
                if (((regInfo.RegionLocX >= minX) && (regInfo.RegionLocX <= maxX)) &&
                    ((regInfo.RegionLocY >= minY) && (regInfo.RegionLocY <= maxY)))
                {
                    MapBlockData map = new MapBlockData();
                    map.Name = regInfo.RegionName;
                    map.X = (ushort) regInfo.RegionLocX;
                    map.Y = (ushort) regInfo.RegionLocY;
                    map.WaterHeight = (byte) regInfo.EstateSettings.waterHeight;
                    map.MapImageId = regInfo.EstateSettings.terrainImageID;
                    map.Agents = 1;
                    map.RegionFlags = 72458694;
                    map.Access = 13;
                    mapBlocks.Add(map);
                }
            }
            return mapBlocks;
        }

        /// <summary> 
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="agentData"></param>
        /// <returns></returns>
        /// 
        public bool RegionUp(RegionInfo region)
        {
            foreach (RegionCommsListener listener in m_regionListeners.Values)
            {
                listener.TriggerRegionUp(region);
            }
           
            return false;
        }

        public bool TriggerRegionUp(RegionInfo region)
        {

            return false;
        }

        public bool InformRegionOfChildAgent(ulong regionHandle, AgentCircuitData agentData)
            //should change from agentCircuitData
        {
            //Console.WriteLine("CommsManager- Trying to Inform a region to expect child agent");
            //OpenSim.Framework.Console.MainLog.Instance.Verbose("INTER", rdebugRegionName + ":Local BackEnd: Trying to inform region of child agent: " + agentData.firstname + " " + agentData.lastname);

            if (m_regionListeners.ContainsKey(regionHandle))
            {
                // Console.WriteLine("CommsManager- Informing a region to expect child agent");
                m_regionListeners[regionHandle].TriggerExpectUser(regionHandle, agentData);
                //OpenSim.Framework.Console.MainLog.Instance.Verbose("INTER", rdebugRegionName + ":Local BackEnd: Got Listener trigginering local event: " + agentData.firstname + " " + agentData.lastname);

                return true;
            }
            return false;
        }

        public bool InformRegionOfPrimCrossing(ulong regionHandle, LLUUID primID, string objData) 
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
                m_regionListeners[regionHandle].TriggerExpectPrim(regionHandle, primID, objData);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="agentID"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool ExpectAvatarCrossing(ulong regionHandle, LLUUID agentID, LLVector3 position, bool isFlying)
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
                // Console.WriteLine("CommsManager- Informing a region to expect avatar crossing");
                m_regionListeners[regionHandle].TriggerExpectAvatarCrossing(regionHandle, agentID, position, isFlying);
                return true;
            }
            return false;
        }
        public bool ExpectPrimCrossing(ulong regionHandle, LLUUID primID, LLVector3 position, bool isPhysical)
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
                m_regionListeners[regionHandle].TriggerExpectPrimCrossing(regionHandle, primID, position, isPhysical);
                return true;
            }
            return false;
        }

        public void TellRegionToCloseChildConnection(ulong regionHandle, LLUUID agentID)
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
              m_regionListeners[regionHandle].TriggerCloseAgentConnection(regionHandle, agentID);
            }
        }

        public bool AcknowledgeAgentCrossed(ulong regionHandle, LLUUID agentId)
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
                return true;
            }
            return false;
        }
        public bool AcknowledgePrimCrossed(ulong regionHandle, LLUUID primID)
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Is a Sandbox mode method, used by the local Login server to inform a region of a connection user/session
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="loginData"></param>
        /// <returns></returns>
        public void AddNewSession(ulong regionHandle, Login loginData)
        {
            AgentCircuitData agent = new AgentCircuitData();
            agent.AgentID = loginData.Agent;
            agent.firstname = loginData.First;
            agent.lastname = loginData.Last;
            agent.SessionID = loginData.Session;
            agent.SecureSessionID = loginData.SecureSession;
            agent.circuitcode = loginData.CircuitCode;
            agent.BaseFolder = loginData.BaseFolder;
            agent.InventoryFolder = loginData.InventoryFolder;
            agent.startpos = loginData.StartPos;

            agent.CapsPath = loginData.CapsPath;

            TriggerExpectUser(regionHandle, agent);
        }

        public void TriggerExpectUser(ulong regionHandle, AgentCircuitData agent)
        {
            //OpenSim.Framework.Console.MainLog.Instance.Verbose("INTER", rdebugRegionName + ":Local BackEnd: Other region is sending child agent our way: " + agent.firstname + " " + agent.lastname);

            if (m_regionListeners.ContainsKey(regionHandle))
            {
                //OpenSim.Framework.Console.MainLog.Instance.Verbose("INTER", rdebugRegionName + ":Local BackEnd: FoundLocalRegion To send it to: " + agent.firstname + " " + agent.lastname);

                m_regionListeners[regionHandle].TriggerExpectUser(regionHandle, agent);
            }
        }

        public void TriggerExpectPrim(ulong regionHandle, LLUUID primID, string objData)
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
                m_regionListeners[regionHandle].TriggerExpectPrim(regionHandle, primID, objData);
            }

        }

        public void PingCheckReply(Hashtable respData)
        {
            foreach (ulong region in m_regions.Keys)
            {
                Hashtable regData = new Hashtable();
                RegionInfo reg = m_regions[region];
                regData["status"] = "active";
                regData["handle"] = region.ToString();

                respData[reg.RegionID.ToStringHyphenated()] = regData;
            }
        }

        public bool TriggerExpectAvatarCrossing(ulong regionHandle, LLUUID agentID, LLVector3 position, bool isFlying)
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
                return m_regionListeners[regionHandle].TriggerExpectAvatarCrossing(regionHandle, agentID, position,
                                                                                   isFlying);
            }

            return false;
        }

        public bool TriggerExpectPrimCrossing(ulong regionHandle, LLUUID primID, LLVector3 position, bool isPhysical)
        {
            if (m_regionListeners.ContainsKey(regionHandle))
            {
                return m_regionListeners[regionHandle].TriggerExpectPrimCrossing(regionHandle, primID, position, isPhysical);
            }
            return false;
        }

        public bool IncomingChildAgent(ulong regionHandle, AgentCircuitData agentData)
        {
           // OpenSim.Framework.Console.MainLog.Instance.Verbose("INTER", rdebugRegionName + ":Local BackEnd: Other local region is sending child agent our way: " + agentData.firstname + " " + agentData.lastname);

            if (m_regionListeners.ContainsKey(regionHandle))
            {
                //OpenSim.Framework.Console.MainLog.Instance.Verbose("INTER", rdebugRegionName + ":Local BackEnd: found local region to trigger event on: " + agentData.firstname + " " + agentData.lastname);

                TriggerExpectUser(regionHandle, agentData);
                return true;
            }

            return false;
        }
    }
}