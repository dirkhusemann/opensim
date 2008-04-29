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

using libsecondlife;

namespace OpenSim.Framework
{
    /// <summary>
    /// Inventory Item - contains all the properties associated with an individual inventory piece.
    /// </summary>
    public class InventoryItemBase
    {
        /// <summary>
        /// The UUID of the associated asset on the asset server
        /// </summary>
        private LLUUID _assetID;

        /// <summary>
        /// This is an enumerated value determining the type of asset (eg Notecard, Sound, Object, etc)
        /// </summary>
        private int _assetType;

        /// <summary>
        /// 
        /// </summary>
        private uint _basePermissions;

        /// <summary>
        /// The creator of this item
        /// </summary>
        private LLUUID _creator;

        /// <summary>
        /// A mask containing permissions for the current owner (cannot be enforced)
        /// </summary>
        private uint _currentPermissions;

        /// <summary>
        /// The description of the inventory item (must be less than 64 characters)
        /// </summary>
        private string _description;

        /// <summary>
        /// 
        /// </summary>
        private uint _everyOnePermissions;

        /// <summary>
        /// The folder this item is contained in 
        /// </summary>
        private LLUUID _folder;

        /// <summary>
        /// A UUID containing the ID for the inventory item itself
        /// </summary>
        private LLUUID _id;

        /// <summary>
        /// The type of inventory item. (Can be slightly different to the asset type
        /// </summary>
        private int _invType;

        /// <summary>
        /// The name of the inventory item (must be less than 64 characters)
        /// </summary>
        private string _name;

        /// <summary>
        /// A mask containing the permissions for the next owner (cannot be enforced)
        /// </summary>
        private uint _nextPermissions;

        /// <summary>
        /// The owner of this inventory item
        /// </summary>
        private LLUUID _owner;

        public LLUUID ID
        {
            get { return _id; }
            set { _id = value; }
        }

        public int InvType
        {
            get { return _invType; }
            set { _invType = value; }
        }

        public LLUUID Folder
        {
            get { return _folder; }
            set { _folder = value; }
        }

        public LLUUID Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        public LLUUID Creator
        {
            get { return _creator; }
            set { _creator = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public uint NextPermissions
        {
            get { return _nextPermissions; }
            set { _nextPermissions = value; }
        }

        public uint CurrentPermissions
        {
            get { return _currentPermissions; }
            set { _currentPermissions = value; }
        }

        public uint BasePermissions
        {
            get { return _basePermissions; }
            set { _basePermissions = value; }
        }

        public uint EveryOnePermissions
        {
            get { return _everyOnePermissions; }
            set { _everyOnePermissions = value; }
        }

        public int AssetType
        {
            get { return _assetType; }
            set { _assetType = value; }
        }

        public LLUUID AssetID
        {
            get { return _assetID; }
            set { _assetID = value; }
        }
    }
}