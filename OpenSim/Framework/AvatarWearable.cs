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
using libsecondlife;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace OpenSim.Framework
{
    [Serializable]
    public class AvatarWearable : ISerializable
    {
        public LLUUID AssetID = new LLUUID("00000000-0000-0000-0000-000000000000");
        public LLUUID ItemID = new LLUUID("00000000-0000-0000-0000-000000000000");

        public AvatarWearable()
        {
        }

        public AvatarWearable(LLUUID itemId, LLUUID assetId)
        {
            AssetID = assetId;
            ItemID = itemId;
        }

        public static AvatarWearable[] DefaultWearables
        {
            get
            {
                AvatarWearable[] defaultWearables = new AvatarWearable[13]; //should be 13 of these
                for (int i = 0; i < 13; i++)
                {
                    defaultWearables[i] = new AvatarWearable();
                }
                defaultWearables[0].AssetID = new LLUUID("66c41e39-38f9-f75a-024e-585989bfab73");
                defaultWearables[0].ItemID = new LLUUID("66c41e39-38f9-f75a-024e-585989bfaba9");

                defaultWearables[1].ItemID = new LLUUID("77c41e39-38f9-f75a-024e-585989bfabc9");
                defaultWearables[1].AssetID = new LLUUID("77c41e39-38f9-f75a-024e-585989bbabbb");

                defaultWearables[4].ItemID = new LLUUID("77c41e39-38f9-f75a-0000-585989bf0000");
                defaultWearables[4].AssetID = new LLUUID("00000000-38f9-1111-024e-222222111110");

                defaultWearables[5].ItemID = new LLUUID("77c41e39-38f9-f75a-0000-5859892f1111");
                defaultWearables[5].AssetID = new LLUUID("00000000-38f9-1111-024e-222222111120");
                return defaultWearables;
            }
        }
        protected AvatarWearable(SerializationInfo info, StreamingContext context)
        {
            //System.Console.WriteLine("AvatarWearable Deserialize BGN");
            if (info == null)
            {
                throw new System.ArgumentNullException("info");
            }

            AssetID = new LLUUID((Guid)info.GetValue("AssetID", typeof(Guid)));
            ItemID = new LLUUID((Guid)info.GetValue("ItemID", typeof(Guid)));

            //System.Console.WriteLine("AvatarWearable Deserialize END");
        }

        [SecurityPermission(SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(
                        SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new System.ArgumentNullException("info");
            }

            info.AddValue("AssetID", AssetID.UUID);
            info.AddValue("ItemID", ItemID.UUID);
        }
    }
}
