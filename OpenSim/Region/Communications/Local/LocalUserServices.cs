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
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS AS IS AND ANY
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
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.UserManagement;

namespace OpenSim.Region.Communications.Local
{
    public class LocalUserServices : UserManagerBase
    {
        private readonly NetworkServersInfo m_serversInfo;
        private readonly uint m_defaultHomeX;
        private readonly uint m_defaultHomeY;
        private IInventoryServices m_inventoryService;


        public LocalUserServices(NetworkServersInfo serversInfo, uint defaultHomeLocX, uint defaultHomeLocY,
                                 IInventoryServices inventoryService)
        {
            m_serversInfo = serversInfo;

            m_defaultHomeX = defaultHomeLocX;
            m_defaultHomeY = defaultHomeLocY;

            m_inventoryService = inventoryService;
        }

        public override UserProfileData SetupMasterUser(string firstName, string lastName)
        {
            return SetupMasterUser(firstName, lastName, "");
        }

        public override UserProfileData SetupMasterUser(string firstName, string lastName, string password)
        {
            UserProfileData profile = GetUserProfile(firstName, lastName);
            if (profile != null)
            {
                return profile;
            }

            Console.WriteLine("Unknown Master User. Sandbox Mode: Creating Account");
            AddUserProfile(firstName, lastName, password, m_defaultHomeX, m_defaultHomeY);

            profile = GetUserProfile(firstName, lastName);

            if (profile == null)
            {
                Console.WriteLine("Unknown Master User after creation attempt. No clue what to do here.");
            }
            else
            {
                m_inventoryService.CreateNewUserInventory(profile.UUID);
            }

            return profile;
        }
    }
}