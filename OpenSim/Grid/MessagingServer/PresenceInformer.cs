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
using System.Net;
using System.Net.Sockets;
using System.Text;
using libsecondlife;
using Nwc.XmlRpc;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Data;
using OpenSim.Framework.Servers;

namespace OpenSim.Grid.MessagingServer
{
    public class PresenceInformer
    {
        public UserPresenceData presence1 = null;
        public UserPresenceData presence2 = null;
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public PresenceInformer()
        {

        }
        public void go(object o)
        {
            if (presence1 != null && presence2 != null)
            {
                SendRegionPresenceUpdate(presence1, presence2);
            }

        }

        /// <summary>
        /// Informs a region about an Agent
        /// </summary>
        /// <param name="TalkingAbout">User to talk about</param>
        /// <param name="UserToUpdate">User we're sending this too (contains the region)</param>
        public void SendRegionPresenceUpdate(UserPresenceData TalkingAbout, UserPresenceData UserToUpdate)
        {
            // TODO: Fill in pertenant Presence Data from 'TalkingAbout'

            RegionProfileData whichRegion = UserToUpdate.regionData;
            //whichRegion.httpServerURI

            Hashtable PresenceParams = new Hashtable();
            ArrayList SendParams = new ArrayList();
            SendParams.Add(PresenceParams);

            m_log.Info("[PRESENCE]: Informing " + whichRegion.regionName + " at " + whichRegion.httpServerURI);
            // Send
            XmlRpcRequest RegionReq = new XmlRpcRequest("presence_update", SendParams);
            XmlRpcResponse RegionResp = RegionReq.Send(whichRegion.httpServerURI, 6000);
        }


    }
}
