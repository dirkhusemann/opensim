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

using System.Collections.Generic;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.Framework;

namespace OpenSim.Region.Environment.Scenes
{
    public partial class Scene
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="fromPos"></param>
        /// <param name="fromName"></param>
        /// <param name="fromAgentID"></param>
        public void SimChat(byte[] message, ChatTypeEnum type, int channel, LLVector3 fromPos, string fromName,
                            LLUUID fromAgentID)
        {
            if (m_simChatModule != null)
            {
                ChatFromViewerArgs args = new ChatFromViewerArgs();

                args.Message = Helpers.FieldToUTF8String(message);
                args.Channel = channel;
                args.Type = type;
                args.Position = fromPos;
                args.SenderUUID = fromAgentID;


                ScenePresence user = GetScenePresence(fromAgentID);
                if (user != null)
                    args.Sender = user.ControllingClient;
                else
                    args.Sender = null;

                args.From = fromName;
                //args.

                m_simChatModule.SimChat(this, args);
            }
        }

        /// <summary>
        /// Invoked when the client selects a prim.
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="remoteClient"></param>
        public void SelectPrim(uint primLocalID, IClientAPI remoteClient)
        {
            List<EntityBase> EntitieList = GetEntities();

            foreach (EntityBase ent in EntitieList)
            {
                if (ent is SceneObjectGroup)
                {                    
                    if (((SceneObjectGroup) ent).LocalId == primLocalID)
                    {
                        // A prim is only tainted if it's allowed to be edited by the person clicking it.
                        if (Permissions.CanEditObjectPosition(remoteClient.AgentId, ((SceneObjectGroup)ent).UUID) || Permissions.CanEditObject(remoteClient.AgentId, ((SceneObjectGroup)ent).UUID))
                        {
                            ((SceneObjectGroup) ent).GetProperties(remoteClient);
                            ((SceneObjectGroup) ent).IsSelected = true;
                            LandChannel.SetPrimsTainted();
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="remoteClient"></param>
        public void DeselectPrim(uint primLocalID, IClientAPI remoteClient)
        {
            List<EntityBase> EntitieList = GetEntities();

            foreach (EntityBase ent in EntitieList)
            {
                if (ent is SceneObjectGroup)
                {
                    if (((SceneObjectGroup) ent).LocalId == primLocalID)
                    {
                        if (Permissions.CanEditObjectPosition(remoteClient.AgentId, ((SceneObjectGroup)ent).UUID) || Permissions.CanEditObject(remoteClient.AgentId, ((SceneObjectGroup)ent).UUID))
                        {
                            ((SceneObjectGroup) ent).IsSelected = false;
                            LandChannel.SetPrimsTainted();
                            ((SceneObjectGroup)ent).ScheduleGroupForTerseUpdate();
                            break;
                        }
                    }
                }
            }
        }

        public virtual void ProcessMoneyTransferRequest(LLUUID source, LLUUID destination, int amount, int transactiontype, string description)
        {
            EventManager.MoneyTransferArgs args = new EventManager.MoneyTransferArgs(
                source, destination, amount, transactiontype, description);

            EventManager.TriggerMoneyTransfer(this, args);
        }

        public virtual void ProcessParcelBuy(LLUUID agentId, LLUUID groupId, bool final, bool groupOwned,
                bool removeContribution, int parcelLocalID, int parcelArea, int parcelPrice, bool authenticated)
        {
            EventManager.LandBuyArgs args = new EventManager.LandBuyArgs(
               agentId, groupId, final, groupOwned, removeContribution, parcelLocalID, parcelArea, parcelPrice, authenticated);

            // First, allow all validators a stab at it
            m_eventManager.TriggerValidateLandBuy(this, args);        

            // Then, check validation and transfer
            m_eventManager.TriggerLandBuy(this, args);        
        }

        public virtual void ProcessObjectGrab(uint localID, LLVector3 offsetPos, IClientAPI remoteClient)
        {

            List<EntityBase> EntitieList = GetEntities();

            foreach (EntityBase ent in EntitieList)
            {
                if (ent is SceneObjectGroup)
                {
                    SceneObjectGroup obj = ent as SceneObjectGroup;
                    if (obj != null)
                    {
                        // Is this prim part of the group
                        if (obj.HasChildPrim(localID))
                        {
                            // Currently only grab/touch for the single prim
                            // the client handles rez correctly
                            obj.ObjectGrabHandler(localID, offsetPos, remoteClient);

                            SceneObjectPart part = obj.GetChildPart(localID);

                            // If the touched prim handles touches, deliver it
                            // If not, deliver to root prim
                            if ((part.ScriptEvents & scriptEvents.touch_start) != 0)
                                EventManager.TriggerObjectGrab(part.LocalId, part.OffsetPosition, remoteClient);
                            else
                                EventManager.TriggerObjectGrab(obj.RootPart.LocalId, part.OffsetPosition, remoteClient);

                            return;
                        }
                    }
                }
            }
        }

        public virtual void ProcessObjectDeGrab(uint localID, IClientAPI remoteClient)
        {

            List<EntityBase> EntitieList = GetEntities();

            foreach (EntityBase ent in EntitieList)
            {
                if (ent is SceneObjectGroup)
                {
                    SceneObjectGroup obj = ent as SceneObjectGroup;

                    // Is this prim part of the group
                    if (obj.HasChildPrim(localID))
                    {
                        SceneObjectPart part=obj.GetChildPart(localID);
                        if (part != null)
                        {
                            // If the touched prim handles touches, deliver it
                            // If not, deliver to root prim
                            if ((part.ScriptEvents & scriptEvents.touch_end) != 0)
                                EventManager.TriggerObjectDeGrab(part.LocalId, remoteClient);
                            else
                                EventManager.TriggerObjectDeGrab(obj.RootPart.LocalId, remoteClient);

                            return;
                        }
                        return;
                    }
                }
            }
        }

        public void ProcessAvatarPickerRequest(IClientAPI client, LLUUID avatarID, LLUUID RequestID, string query)
        {
            //EventManager.TriggerAvatarPickerRequest();

            List<AvatarPickerAvatar> AvatarResponses = new List<AvatarPickerAvatar>();
            AvatarResponses = m_sceneGridService.GenerateAgentPickerRequestResponse(RequestID, query);

            AvatarPickerReplyPacket replyPacket = (AvatarPickerReplyPacket) PacketPool.Instance.GetPacket(PacketType.AvatarPickerReply);
            // TODO: don't create new blocks if recycling an old packet

            AvatarPickerReplyPacket.DataBlock[] searchData =
                new AvatarPickerReplyPacket.DataBlock[AvatarResponses.Count];
            AvatarPickerReplyPacket.AgentDataBlock agentData = new AvatarPickerReplyPacket.AgentDataBlock();

            agentData.AgentID = avatarID;
            agentData.QueryID = RequestID;
            replyPacket.AgentData = agentData;
            //byte[] bytes = new byte[AvatarResponses.Count*32];

            int i = 0;
            foreach (AvatarPickerAvatar item in AvatarResponses)
            {
                LLUUID translatedIDtem = item.AvatarID;
                searchData[i] = new AvatarPickerReplyPacket.DataBlock();
                searchData[i].AvatarID = translatedIDtem;
                searchData[i].FirstName = Helpers.StringToField((string) item.firstName);
                searchData[i].LastName = Helpers.StringToField((string) item.lastName);
                i++;
            }
            if (AvatarResponses.Count == 0)
            {
                searchData = new AvatarPickerReplyPacket.DataBlock[0];
            }
            replyPacket.Data = searchData;

            AvatarPickerReplyAgentDataArgs agent_data = new AvatarPickerReplyAgentDataArgs();
            agent_data.AgentID = replyPacket.AgentData.AgentID;
            agent_data.QueryID = replyPacket.AgentData.QueryID;

            List<AvatarPickerReplyDataArgs> data_args = new List<AvatarPickerReplyDataArgs>();
            for (i = 0; i < replyPacket.Data.Length; i++)
            {
                AvatarPickerReplyDataArgs data_arg = new AvatarPickerReplyDataArgs();
                data_arg.AvatarID = replyPacket.Data[i].AvatarID;
                data_arg.FirstName = replyPacket.Data[i].FirstName;
                data_arg.LastName = replyPacket.Data[i].LastName;
                data_args.Add(data_arg);
            }
            client.SendAvatarPickerReply(agent_data, data_args);
        }
    }
}
