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
* 
*/
using System;
using libsecondlife;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Console;
using OpenSim.Region.Terrain;
using OpenSim.Region.Environment.Interfaces;

namespace OpenSim.Region.Environment.Scenes
{
    public abstract class SceneBase : IScene
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Events

        public event restart OnRestart;

        #endregion

        #region Fields

        private readonly ClientManager m_clientManager = new ClientManager();

        public ClientManager ClientManager
        {
            get { return m_clientManager; }
        }

        protected ulong m_regionHandle;
        protected string m_regionName;
        protected RegionInfo m_regInfo;

        public TerrainEngine Terrain;
        public ITerrainChannel Heightmap;

        protected EventManager m_eventManager;

        public EventManager EventManager
        {
            get { return m_eventManager; }
        }

        protected string m_datastore;

        private uint m_nextLocalId = 8880000;
        
        private AssetCache m_assetCache;

        public AssetCache AssetCache
        {
            get { return m_assetCache; }
            set { m_assetCache = value; }
        }
        
        protected RegionStatus m_regStatus;        

        public RegionStatus Region_Status
        {
            get { return m_regStatus; }
            set { m_regStatus = value; }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Normally called once every frame/tick to let the world preform anything required (like running the physics simulation)
        /// </summary>
        public abstract void Update();

        #endregion

        #region Terrain Methods

        /// <summary>
        /// Loads the World heightmap
        /// </summary>
        public abstract void LoadWorldMap();

        /// <summary>
        /// Send the region heightmap to the client
        /// </summary>
        /// <param name="RemoteClient">Client to send to</param>
        public virtual void SendLayerData(IClientAPI RemoteClient)
        {
            RemoteClient.SendLayerData(Terrain.GetHeights1D());
        }

        #endregion

        #region Add/Remove Agent/Avatar

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="agentID"></param>
        /// <param name="child"></param>
        public abstract void AddNewClient(IClientAPI client, bool child);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="agentID"></param>
        public abstract void RemoveClient(LLUUID agentID);

        public abstract void CloseAllAgents(uint circuitcode);

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual RegionInfo RegionInfo
        {
            get { return m_regInfo; }
        }

        public uint NextLocalId
        {
            get { return m_nextLocalId++; }
        }

        #region admin stuff

        /// <summary>
        /// Region Restart - Seconds till restart.
        /// </summary>
        /// <param name="seconds"></param>
        public virtual void Restart(int seconds)
        {
            m_log.Error("[REGION]: passing Restart Message up the namespace");
            restart handler001 = OnRestart;
            if (handler001 != null) 
                handler001(RegionInfo);
        }

        public virtual bool PresenceChildStatus(LLUUID avatarID)
        {
            return false;
        }
        public abstract bool OtherRegionUp(RegionInfo thisRegion);

        public virtual string GetSimulatorVersion()
        {
            return "OpenSimulator v0.5 SVN";
        }

        #endregion

        #region Shutdown

        /// <summary>
        /// Tidy before shutdown
        /// </summary>
        public virtual void Close()
        {
            try
            {
                EventManager.TriggerShutdown();
            }
            catch (Exception e)
            {
                m_log.Error("[SCENE]: SceneBase.cs: Close() - Failed with exception " + e.ToString());
            }
        }

        #endregion
    }
}