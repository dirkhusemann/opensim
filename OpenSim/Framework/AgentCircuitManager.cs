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
using System.Collections.Generic;
using libsecondlife;

namespace OpenSim.Framework
{
    public class AgentCircuitManager
    {
        public Dictionary<uint, AgentCircuitData> AgentCircuits = new Dictionary<uint, AgentCircuitData>();

        public AgentCircuitManager()
        {
        }

        public virtual AuthenticateResponse AuthenticateSession(LLUUID sessionID, LLUUID agentID, uint circuitcode)
        {
            AgentCircuitData validcircuit = null;
            if (AgentCircuits.ContainsKey(circuitcode))
            {
                validcircuit = AgentCircuits[circuitcode];
            }
            AuthenticateResponse user = new AuthenticateResponse();
            if (validcircuit == null)
            {
                //don't have this circuit code in our list
                user.Authorised = false;
                return (user);
            }

            if ((sessionID == validcircuit.SessionID) && (agentID == validcircuit.AgentID))
            {
                user.Authorised = true;
                user.LoginInfo = new Login();
                user.LoginInfo.Agent = agentID;
                user.LoginInfo.Session = sessionID;
                user.LoginInfo.SecureSession = validcircuit.SecureSessionID;
                user.LoginInfo.First = validcircuit.firstname;
                user.LoginInfo.Last = validcircuit.lastname;
                user.LoginInfo.InventoryFolder = validcircuit.InventoryFolder;
                user.LoginInfo.BaseFolder = validcircuit.BaseFolder;
            }
            else
            {
                // Invalid
                user.Authorised = false;
            }

            return (user);
        }

        public virtual void AddNewCircuit(uint circuitCode, AgentCircuitData agentData)
        {
            if (AgentCircuits.ContainsKey(circuitCode))
            {
                AgentCircuits[circuitCode] = agentData;
            }
            else
            {
                AgentCircuits.Add(circuitCode, agentData);
            }
        }

        public LLVector3 GetPosition(uint circuitCode)
        {
            LLVector3 vec = new LLVector3();
            if (AgentCircuits.ContainsKey(circuitCode))
            {
                vec = AgentCircuits[circuitCode].startpos;
            }
            return vec;
        }

        public void UpdateAgentData(AgentCircuitData agentData)
        {
            if (AgentCircuits.ContainsKey((uint) agentData.circuitcode))
            {
                AgentCircuits[(uint) agentData.circuitcode].firstname = agentData.firstname;
                AgentCircuits[(uint) agentData.circuitcode].lastname = agentData.lastname;
                AgentCircuits[(uint) agentData.circuitcode].startpos = agentData.startpos;
                // Console.WriteLine("update user start pos is " + agentData.startpos.X + " , " + agentData.startpos.Y + " , " + agentData.startpos.Z);
            }
        }

        public void UpdateAgentChildStatus(uint circuitcode, bool childstatus)
        {
            if (AgentCircuits.ContainsKey(circuitcode))
            {
                AgentCircuits[circuitcode].child = childstatus;
            }
        }

        public bool GetAgentChildStatus(uint circuitcode)
        {
            if (AgentCircuits.ContainsKey(circuitcode))
            {
                return AgentCircuits[circuitcode].child;
            }
            return false;
        }
    }
}
