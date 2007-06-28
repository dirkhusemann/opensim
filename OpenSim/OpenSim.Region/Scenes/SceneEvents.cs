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

namespace OpenSim.Region.Scenes
{
    /// <summary>
    /// A class for triggering remote scene events.
    /// </summary>
    public class EventManager
    {
        public delegate void OnFrameDelegate();
        public event OnFrameDelegate OnFrame;

        public delegate void OnNewPresenceDelegate(ScenePresence presence);
        public event OnNewPresenceDelegate OnNewPresence;

        public delegate void OnNewPrimitiveDelegate(Primitive prim);
        public event OnNewPrimitiveDelegate OnNewPrimitive;

        public delegate void OnRemovePresenceDelegate(libsecondlife.LLUUID uuid);
        public event OnRemovePresenceDelegate OnRemovePresence;

        public void TriggerOnFrame()
        {
            if (OnFrame != null)
            {
                OnFrame();
            }
        }

        public void TriggerOnNewPrimitive(Primitive prim)
        {
            if (OnNewPrimitive != null)
                OnNewPrimitive(prim);
        }

        public void TriggerOnNewPresence(ScenePresence presence)
        {
            if (OnNewPresence != null)
                OnNewPresence(presence);
        }

        public void TriggerOnRemovePresence(libsecondlife.LLUUID uuid)
        {
            if (OnRemovePresence != null)
            {
                OnRemovePresence(uuid);
            }
        }
    }
}
