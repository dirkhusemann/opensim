using System;
using System.Collections;
using System.Collections.Generic;
using libsecondlife;
using libsecondlife.Packets;
using Nwc.XmlRpc;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Timers;
using OpenSim.Framework.Interfaces;
using OpenSim.Framework.Types;
using OpenSim.Framework.Inventory;
using OpenSim.Framework.Utilities;
using OpenSim.world;
using OpenSim.Assets;

namespace OpenSim
{
    public partial class ClientView
    {
        protected virtual void RegisterLocalPacketHandlers()
        {
            this.AddLocalPacketHandler(PacketType.LogoutRequest, this.Logout);
            this.AddLocalPacketHandler(PacketType.AgentCachedTexture, this.AgentTextureCached);
            this.AddLocalPacketHandler(PacketType.MultipleObjectUpdate, this.MultipleObjUpdate);
        }

        protected virtual bool Logout(ClientView simClient, Packet packet)
        {
            OpenSim.Framework.Console.MainConsole.Instance.WriteLine(OpenSim.Framework.Console.LogPriority.LOW, "OpenSimClient.cs:ProcessInPacket() - Got a logout request");
            //send reply to let the client logout
            LogoutReplyPacket logReply = new LogoutReplyPacket();
            logReply.AgentData.AgentID = this.AgentID;
            logReply.AgentData.SessionID = this.SessionID;
            logReply.InventoryData = new LogoutReplyPacket.InventoryDataBlock[1];
            logReply.InventoryData[0] = new LogoutReplyPacket.InventoryDataBlock();
            logReply.InventoryData[0].ItemID = LLUUID.Zero;
            OutPacket(logReply);
            //tell all clients to kill our object
            KillObjectPacket kill = new KillObjectPacket();
            kill.ObjectData = new KillObjectPacket.ObjectDataBlock[1];
            kill.ObjectData[0] = new KillObjectPacket.ObjectDataBlock();
            kill.ObjectData[0].ID = this.ClientAvatar.localid;
            foreach (ClientView client in m_clientThreads.Values)
            {
                client.OutPacket(kill);
            }
            if (this.m_userServer != null)
            {
                this.m_inventoryCache.ClientLeaving(this.AgentID, this.m_userServer);
            }
            else
            {
                this.m_inventoryCache.ClientLeaving(this.AgentID, null);
            }

            m_gridServer.LogoutSession(this.SessionID, this.AgentID, this.CircuitCode);
            /*lock (m_world.Entities)
            {
                m_world.Entities.Remove(this.AgentID);
            }*/
            m_world.RemoveViewerAgent(this);
            //need to do other cleaning up here too
            m_clientThreads.Remove(this.CircuitCode);
            m_networkServer.RemoveClientCircuit(this.CircuitCode);
            this.ClientThread.Abort();
            return true;
        }

        protected bool AgentTextureCached(ClientView simclient, Packet packet)
        {
            // Console.WriteLine(packet.ToString());
            AgentCachedTexturePacket chechedtex = (AgentCachedTexturePacket)packet;
            AgentCachedTextureResponsePacket cachedresp = new AgentCachedTextureResponsePacket();
            cachedresp.AgentData.AgentID = this.AgentID;
            cachedresp.AgentData.SessionID = this.SessionID;
            cachedresp.AgentData.SerialNum = this.cachedtextureserial;
            this.cachedtextureserial++;
            cachedresp.WearableData = new AgentCachedTextureResponsePacket.WearableDataBlock[chechedtex.WearableData.Length];
            for (int i = 0; i < chechedtex.WearableData.Length; i++)
            {
                cachedresp.WearableData[i] = new AgentCachedTextureResponsePacket.WearableDataBlock();
                cachedresp.WearableData[i].TextureIndex = chechedtex.WearableData[i].TextureIndex;
                cachedresp.WearableData[i].TextureID = LLUUID.Zero;
                cachedresp.WearableData[i].HostName = new byte[0];
            }
            this.OutPacket(cachedresp);
            return true;
        }

        protected bool MultipleObjUpdate(ClientView simClient, Packet packet)
        {
            MultipleObjectUpdatePacket multipleupdate = (MultipleObjectUpdatePacket)packet;
            for (int i = 0; i < multipleupdate.ObjectData.Length; i++)
            {
                if (multipleupdate.ObjectData[i].Type == 9) //change position
                {
                    libsecondlife.LLVector3 pos = new LLVector3(multipleupdate.ObjectData[i].Data, 0);
                    OnUpdatePrimPosition(multipleupdate.ObjectData[i].ObjectLocalID, pos, this);
                    //should update stored position of the prim
                }
                else if (multipleupdate.ObjectData[i].Type == 10)//rotation
                {
                    libsecondlife.LLQuaternion rot = new LLQuaternion(multipleupdate.ObjectData[i].Data, 0, true);
                    OnUpdatePrimRotation(multipleupdate.ObjectData[i].ObjectLocalID, rot, this);
                }
                else if (multipleupdate.ObjectData[i].Type == 13)//scale
                {
                    libsecondlife.LLVector3 scale = new LLVector3(multipleupdate.ObjectData[i].Data, 12);
                    OnUpdatePrimScale(multipleupdate.ObjectData[i].ObjectLocalID, scale, this);            
                }
            }
            return true;
        }

        public void RequestMapLayer() //should be getting the map layer from the grid server
        {
            //send a layer covering the 800,800 - 1200,1200 area (should be covering the requested area)
            MapLayerReplyPacket mapReply = new MapLayerReplyPacket();
            mapReply.AgentData.AgentID = this.AgentID;
            mapReply.AgentData.Flags = 0;
            mapReply.LayerData = new MapLayerReplyPacket.LayerDataBlock[1];
            mapReply.LayerData[0] = new MapLayerReplyPacket.LayerDataBlock();
            mapReply.LayerData[0].Bottom = this.m_regionData.RegionLocY - 50;
            mapReply.LayerData[0].Left = this.m_regionData.RegionLocX - 50;
            mapReply.LayerData[0].Top = this.m_regionData.RegionLocY + 50;
            mapReply.LayerData[0].Right = this.m_regionData.RegionLocX + 50;
            mapReply.LayerData[0].ImageID = new LLUUID("00000000-0000-0000-9999-000000000005");
            this.OutPacket(mapReply);
        }

        public void RequestMapBlocks(int minX, int minY, int maxX, int maxY)
        {
            IList simMapProfiles = m_gridServer.RequestMapBlocks(minX, minY, maxX, maxY);
            int len;
            if (simMapProfiles == null)
                len = 0;
            else
                len = simMapProfiles.Count;

            int i;
            int mtu = 16; // Number of regions to send per packet. Will be more precise in future. ( TODO )
            for (i = 0; i < len; i += mtu)
            {
                MapBlockReplyPacket mbReply = new MapBlockReplyPacket();
                mbReply.AgentData.AgentID = this.AgentID;

                mbReply.Data = new MapBlockReplyPacket.DataBlock[Math.Min(mtu, len - i)];
                int iii;
                for (iii = 0; iii < mtu && i + iii < len; iii++)
                {
                    Hashtable mp = (Hashtable)simMapProfiles[iii];
                    mbReply.Data[iii] = new MapBlockReplyPacket.DataBlock();
                    mbReply.Data[iii].Name = libsecondlife.Helpers.StringToField((string)mp["name"]);
                    mbReply.Data[iii].Access = System.Convert.ToByte(mp["access"]);
                    mbReply.Data[iii].Agents = System.Convert.ToByte(mp["agents"]);
                    mbReply.Data[iii].MapImageID = new LLUUID((string)mp["map-image-id"]);
                    mbReply.Data[iii].RegionFlags = System.Convert.ToUInt32(mp["region-flags"]);
                    mbReply.Data[iii].WaterHeight = System.Convert.ToByte(mp["water-height"]);
                    mbReply.Data[iii].X = System.Convert.ToUInt16(mp["x"]);
                    mbReply.Data[iii].Y = System.Convert.ToUInt16(mp["y"]);
                }
                //Console.WriteLine("ADAMDEBUG: Queuing MapBlockReply #" + i.ToString() + " Contains " + iii.ToString() + " region(s)");
                //Console.WriteLine(mbReply.ToString());
                this.OutPacket(mbReply);
            }
        }
    }
}
