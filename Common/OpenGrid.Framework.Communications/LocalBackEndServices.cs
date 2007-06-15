/*
* Copyright (c) Contributors, http://www.openmetaverse.org/
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

using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Types;

using libsecondlife;

namespace OpenGrid.Framework.Communications
{
    public class LocalBackEndServices
    {
        protected Dictionary<ulong, RegionInfo> regions = new Dictionary<ulong, RegionInfo>();
        protected Dictionary<ulong, RegionCommsHostBase> regionHosts = new Dictionary<ulong, RegionCommsHostBase>();

        public LocalBackEndServices()
        {

        }

        /// <summary>
        /// Register a region method with the BackEnd Services.
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <returns></returns>
        public RegionCommsHostBase RegisterRegion(RegionInfo regionInfo)
        {
            //Console.WriteLine("CommsManager - Region " + regionInfo.RegionHandle + " , " + regionInfo.RegionLocX + " , "+ regionInfo.RegionLocY +" is registering");
            if (!this.regions.ContainsKey((uint)regionInfo.RegionHandle))
            {
                //Console.WriteLine("CommsManager - Adding Region " + regionInfo.RegionHandle );
                this.regions.Add(regionInfo.RegionHandle, regionInfo);
                RegionCommsHostBase regionHost = new RegionCommsHostBase();
                this.regionHosts.Add(regionInfo.RegionHandle, regionHost);

                return regionHost;
            }

            //already in our list of regions so for now lets return null
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="regionInfo"></param>
        /// <returns></returns>
        public  List<RegionInfo> RequestNeighbours(RegionInfo regionInfo)
        {
            // Console.WriteLine("Finding Neighbours to " + regionInfo.RegionHandle);
            List<RegionInfo> neighbours = new List<RegionInfo>();

            foreach (RegionInfo reg in this.regions.Values)
            {
                // Console.WriteLine("CommsManager- RequestNeighbours() checking region " + reg.RegionLocX + " , "+ reg.RegionLocY);
                if (reg.RegionHandle != regionInfo.RegionHandle)
                {
                    //Console.WriteLine("CommsManager- RequestNeighbours() - found a different region in list, checking location");
                    if ((reg.RegionLocX > (regionInfo.RegionLocX - 2)) && (reg.RegionLocX < (regionInfo.RegionLocX + 2)))
                    {
                        if ((reg.RegionLocY > (regionInfo.RegionLocY - 2)) && (reg.RegionLocY < (regionInfo.RegionLocY + 2)))
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
            if (this.regions.ContainsKey(regionHandle))
            {
                return this.regions[regionHandle];
            }
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="agentData"></param>
        /// <returns></returns>
        public  bool InformNeighbourOfChildAgent(ulong regionHandle, AgentCircuitData agentData) //should change from agentCircuitData
        {
            //Console.WriteLine("CommsManager- Trying to Inform a region to expect child agent");
            if (this.regionHosts.ContainsKey(regionHandle))
            {
                // Console.WriteLine("CommsManager- Informing a region to expect child agent");
                this.regionHosts[regionHandle].TriggerExpectUser(regionHandle, agentData);
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
        public bool ExpectAvatarCrossing(ulong regionHandle, libsecondlife.LLUUID agentID, libsecondlife.LLVector3 position)
        {
            if (this.regionHosts.ContainsKey(regionHandle))
            {
                // Console.WriteLine("CommsManager- Informing a region to expect avatar crossing");
                this.regionHosts[regionHandle].ExpectAvatarCrossing(regionHandle, agentID, position);
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
        public bool AddNewSession(ulong regionHandle, Login loginData)
        {
            //Console.WriteLine(" comms manager been told to expect new user");
            AgentCircuitData agent = new AgentCircuitData();
            agent.AgentID = loginData.Agent;
            agent.firstname = loginData.First;
            agent.lastname = loginData.Last;
            agent.SessionID = loginData.Session;
            agent.SecureSessionID = loginData.SecureSession;
            agent.circuitcode = loginData.CircuitCode;
            agent.BaseFolder = loginData.BaseFolder;
            agent.InventoryFolder = loginData.InventoryFolder;
            agent.startpos = new LLVector3(128, 128, 70);

            if (this.regionHosts.ContainsKey(regionHandle))
            {
                this.regionHosts[regionHandle].TriggerExpectUser(regionHandle, agent);
                return true;
            }

            // region not found
            return false;
        }
    }
}
