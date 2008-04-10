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
using libsecondlife;

namespace OpenSim.Framework
{
    /// <summary>
    /// Information about a particular user known to the userserver
    /// </summary>
    public class UserProfileData
    {
        /// <summary>
        /// The ID value for this user
        /// </summary>
        private LLUUID _id;

        /// <summary>
        /// The last used Web_login_key
        /// </summary>
        private LLUUID _webLoginKey;
        /// <summary>
        /// The first component of a users account name
        /// </summary>
        private string _firstname;

        /// <summary>
        /// The second component of a users account name
        /// </summary>
        private string _surname;

        /// <summary>
        /// A salted hash containing the users password, in the format md5(md5(password) + ":" + salt)
        /// </summary>
        /// <remarks>This is double MD5'd because the client sends an unsalted MD5 to the loginserver</remarks>
        private string _passwordHash;

        /// <summary>
        /// The salt used for the users hash, should be 32 bytes or longer
        /// </summary>
        private string _passwordSalt;

        /// <summary>
        /// The regionhandle of the users preffered home region. If multiple sims occupy the same spot, the grid may decide which region the user logs into
        /// </summary>
        public ulong HomeRegion
        {
            get { return Helpers.UIntsToLong((_homeRegionX * (uint)Constants.RegionSize), (_homeRegionY * (uint)Constants.RegionSize)); }
            set
            {
                _homeRegionX = (uint) (value >> 40);
                _homeRegionY = (((uint) (value)) >> 8);
            }
        }

        public LLUUID ID {
            get {
                return _id;
            }
            set {
                _id = value;
            }
        }

        public LLUUID WebLoginKey {
            get {
                return _webLoginKey;
            }
            set {
                _webLoginKey = value;
            }
        }

        public string FirstName {
            get {
                return _firstname;
            }
            set {
                _firstname = value;
            }
        }

        public string SurName {
            get {
                return _surname;
            }
            set {
                _surname = value;
            }
        }

        public string PasswordHash {
            get {
                return _passwordHash;
            }
            set {
                _passwordHash = value;
            }
        }

        public string PasswordSalt {
            get {
                return _passwordSalt;
            }
            set {
                _passwordSalt = value;
            }
        }

        public uint HomeRegionX {
            get {
                return _homeRegionX;
            }
            set {
                _homeRegionX = value;
            }
        }

        public uint HomeRegionY {
            get {
                return _homeRegionY;
            }
            set {
                _homeRegionY = value;
            }
        }

        public LLVector3 HomeLocation {
            get {
                return _homeLocation;
            }
            set {
                _homeLocation = value;
            }
        }

        public LLVector3 HomeLookAt {
            get {
                return _homeLookAt;
            }
            set {
                _homeLookAt = value;
            }
        }

        public int Created {
            get {
                return _created;
            }
            set {
                _created = value;
            }
        }

        public int LastLogin {
            get {
                return _lastLogin;
            }
            set {
                _lastLogin = value;
            }
        }

        public LLUUID RootInventoryFolderID {
            get {
                return _rootInventoryFolderID;
            }
            set {
                _rootInventoryFolderID = value;
            }
        }

        public string UserInventoryURI {
            get {
                return _userInventoryURI;
            }
            set {
                _userInventoryURI = value;
            }
        }

        public string UserAssetURI {
            get {
                return _userAssetURI;
            }
            set {
                _userAssetURI = value;
            }
        }

        public uint CanDoMask {
            get {
                return _profileCanDoMask;
            }
            set {
                _profileCanDoMask = value;
            }
        }

        public uint WantDoMask {
            get {
                return _profileWantDoMask;
            }
            set {
                _profileWantDoMask = value;
            }
        }

        public string AboutText {
            get {
                return _profileAboutText;
            }
            set {
                _profileAboutText = value;
            }
        }

        public string FirstLifeAboutText {
            get {
                return _profileFirstText;
            }
            set {
                _profileFirstText = value;
            }
        }

        public LLUUID Image {
            get {
                return _profileImage;
            }
            set {
                _profileImage = value;
            }
        }

        public LLUUID FirstLifeImage {
            get {
                return _profileFirstImage;
            }
            set {
                _profileFirstImage = value;
            }
        }

        public UserAgentData CurrentAgent {
            get {
                return _currentAgent;
            }
            set {
                _currentAgent = value;
            }
        }

        private uint _homeRegionX;
        private uint _homeRegionY;

        /// <summary>
        /// The coordinates inside the region of the home location
        /// </summary>
        private LLVector3 _homeLocation;

        /// <summary>
        /// Where the user will be looking when they rez.
        /// </summary>
        private LLVector3 _homeLookAt;

        /// <summary>
        /// A UNIX Timestamp (seconds since epoch) for the users creation
        /// </summary>
        private int _created;

        /// <summary>
        /// A UNIX Timestamp for the users last login date / time
        /// </summary>
        private int _lastLogin;

        private LLUUID _rootInventoryFolderID;

        /// <summary>
        /// A URI to the users inventory server, used for foreigners and large grids
        /// </summary>
        private string _userInventoryURI = String.Empty;

        /// <summary>
        /// A URI to the users asset server, used for foreigners and large grids.
        /// </summary>
        private string _userAssetURI = String.Empty;

        /// <summary>
        /// A uint mask containing the "I can do" fields of the users profile
        /// </summary>
        private uint _profileCanDoMask;

        /// <summary>
        /// A uint mask containing the "I want to do" part of the users profile
        /// </summary>
        private uint _profileWantDoMask; // Profile window "I want to" mask

        /// <summary>
        /// The about text listed in a users profile.
        /// </summary>
        private string _profileAboutText = String.Empty;

        /// <summary>
        /// The first life about text listed in a users profile
        /// </summary>
        private string _profileFirstText = String.Empty;

        /// <summary>
        /// The profile image for an avatar stored on the asset server
        /// </summary>
        private LLUUID _profileImage;

        /// <summary>
        /// The profile image for the users first life tab
        /// </summary>
        private LLUUID _profileFirstImage;

        /// <summary>
        /// The users last registered agent (filled in on the user server)
        /// </summary>
        private UserAgentData _currentAgent;
    }
}
