﻿/*
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using libsecondlife;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;

namespace OpenSim.Region.Environment.Modules.World.Archiver
{
    /// <summary>
    /// Dearchives assets
    /// </summary>    
    public class AssetsDearchiver
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected static System.Text.ASCIIEncoding m_asciiEncoding = new System.Text.ASCIIEncoding();
        
        /// <summary>
        /// Store for asset data we received before we get the metadata
        /// </summary>
        protected Dictionary<string, byte[]> m_assetDataAwaitingMetadata = new Dictionary<string, byte[]>();

        /// <summary>
        /// Asset metadata.  Is null if asset metadata isn't yet available.
        /// </summary>
        protected Dictionary<string, AssetMetadata> m_metadata;
        
        /// <summary>
        /// Cache to which dearchived assets will be added
        /// </summary>
        protected AssetCache m_cache;
        
        public AssetsDearchiver(AssetCache cache)            
        {
            m_cache = cache;
        }
        
        /// <summary>
        /// Add asset data to the dearchiver
        /// </summary>
        /// <param name="assetFilename"></param>
        /// <param name="data"></param>
        public void AddAssetData(string assetFilename, byte[] data)
        {
            if (null == m_metadata)
            {
                m_assetDataAwaitingMetadata[assetFilename] = data;
            }
            else
            {
                ResolveAssetData(assetFilename, data);
            }
        }
                 
        /// <summary>
        /// Add asset metadata xml
        /// </summary>
        /// <param name="xml"></param>
        public void AddAssetMetadata(string xml)
        {
            m_metadata = new Dictionary<string, AssetMetadata>();
            
            StringReader sr = new StringReader(xml);
            XmlTextReader reader = new XmlTextReader(sr);   
            
            reader.ReadStartElement("assets");
            reader.Read();
            
            m_log.DebugFormat("next node {0}", reader.Name);
            while (reader.Name.Equals("asset"))
            {
                reader.Read();
                
                AssetMetadata metadata = new AssetMetadata();
                
                string filename = reader.ReadElementString("filename");
                m_log.DebugFormat("[DEARCHIVER]: Reading node {0}", filename);
                
                metadata.Name = reader.ReadElementString("name");
                metadata.Description = reader.ReadElementString("description");
                metadata.AssetType = Convert.ToSByte(reader.ReadElementString("asset-type"));
                metadata.AssetType = Convert.ToSByte(reader.ReadElementString("inventory-type"));                                                
                
                m_metadata[filename] = metadata;
                
                // Read asset end tag
                reader.ReadEndElement();
                
                reader.Read();
            }
            
            m_log.DebugFormat("[DEARCHIVER]: Resolved {0} items of asset metadata", m_metadata.Count);
            
            ResolvePendingAssetData();
        }
        
        /// <summary>
        /// Resolve asset data that we collected before receiving the metadata
        /// </summary>
        protected void ResolvePendingAssetData()
        {
            foreach (string filename in m_assetDataAwaitingMetadata.Keys)
            {
                ResolveAssetData(filename, m_assetDataAwaitingMetadata[filename]);                
            }
        }
        
        /// <summary>
        /// Resolve a new piece of asset data against stored metadata
        /// </summary>
        /// <param name="assetFilename"></param>
        /// <param name="data"></param>
        protected void ResolveAssetData(string assetPath, byte[] data)
        {
            // Right now we're nastily obtaining the lluuid from the filename
            string filename = assetPath.Remove(0, ArchiveConstants.ASSETS_PATH.Length);
            
            if (m_metadata.ContainsKey(filename))
            {
                AssetMetadata metadata = m_metadata[filename];
                
                string rawId = filename.Remove(filename.Length - ArchiveConstants.TEXTURE_EXTENSION.Length);

                m_log.DebugFormat("[ARCHIVER]: Importing asset {0}", rawId);

                AssetBase asset = new AssetBase(new LLUUID(rawId), metadata.Name);
                asset.Description = metadata.Description;
                asset.Type = metadata.AssetType;
                asset.InvType = metadata.InventoryType;
                asset.Data = data;

                m_cache.AddAsset(asset);
            }
            else
            {
                m_log.ErrorFormat(
                    "[DEARCHIVER]: Tried to dearchive data with filename {0} without any corresponding metadata", 
                    assetPath);
            }
        }
        
        /// <summary>
        /// Metadata for an asset
        /// </summary>
        protected struct AssetMetadata
        {
            public string Name;
            public string Description;
            public sbyte AssetType;
            public sbyte InventoryType;
        }
    }
}
