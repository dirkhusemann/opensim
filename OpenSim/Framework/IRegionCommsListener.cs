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

namespace OpenSim.Framework
{
    public delegate void ExpectUserDelegate(ulong regionHandle, AgentCircuitData agent);

    public delegate bool ExpectPrimDelegate(ulong regionHandle, LLUUID primID, string objData, int XMLMethod);

    public delegate void UpdateNeighbours(List<RegionInfo> neighbours);

    public delegate void AgentCrossing(ulong regionHandle, LLUUID agentID, LLVector3 position, bool isFlying);

    public delegate void PrimCrossing(ulong regionHandle, LLUUID primID, LLVector3 position, bool isPhysical);

    public delegate void AcknowledgeAgentCross(ulong regionHandle, LLUUID agentID);

    public delegate void AcknowledgePrimCross(ulong regionHandle, LLUUID PrimID);

    public delegate bool CloseAgentConnection(ulong regionHandle, LLUUID agentID);

    public delegate bool RegionUp(RegionInfo region);

    public delegate bool ChildAgentUpdate(ulong regionHandle, ChildAgentDataUpdate cAgentData);

    public delegate void LogOffUser(ulong regionHandle, LLUUID agentID, LLUUID regionSecret, string message);

    public interface IRegionCommsListener
    {
        event ExpectUserDelegate OnExpectUser;
        event ExpectPrimDelegate OnExpectPrim;
        event GenericCall2 OnExpectChildAgent;
        event AgentCrossing OnAvatarCrossingIntoRegion;
        event PrimCrossing OnPrimCrossingIntoRegion;
        event AcknowledgeAgentCross OnAcknowledgeAgentCrossed;
        event AcknowledgePrimCross OnAcknowledgePrimCrossed;
        event UpdateNeighbours OnNeighboursUpdate;
        event CloseAgentConnection OnCloseAgentConnection;
        event RegionUp OnRegionUp;
        event ChildAgentUpdate OnChildAgentUpdate;
        event LogOffUser OnLogOffUser;
    }
}