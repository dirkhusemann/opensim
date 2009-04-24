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
using System.IO;
using System.Text;
using System.Xml;
using OpenMetaverse;
using OpenSim.Framework;
    
namespace OpenSim.Framework.Serialization.External
{        
    /// <summary>
    /// Serialize and deserialize user inventory items as an external format.
    /// </summary> 
    /// XXX: Please do not use yet.
    public class UserInventoryItemSerializer
    {
        protected static ASCIIEncoding m_asciiEncoding = new ASCIIEncoding();
        
        /// <summary>
        /// Deserialize item
        /// </summary>
        /// <param name="serializedSettings"></param>
        /// <returns></returns>
        /// <exception cref="System.Xml.XmlException"></exception>
        public static InventoryItemBase Deserialize(byte[] serialization)
        {
            return Deserialize(m_asciiEncoding.GetString(serialization, 0, serialization.Length));
        }
        
        /// <summary>
        /// Deserialize settings
        /// </summary>
        /// <param name="serializedSettings"></param>
        /// <returns></returns>
        /// <exception cref="System.Xml.XmlException"></exception>
        public static InventoryItemBase Deserialize(string serialization)
        {
            InventoryItemBase item = new InventoryItemBase();
            
            StringReader sr = new StringReader(serialization);
            XmlTextReader reader = new XmlTextReader(sr);
            
            reader.ReadStartElement("InventoryItem");
            reader.ReadStartElement("Name");
            item.Name = reader.ReadString();
            reader.ReadEndElement();
            reader.ReadStartElement("ID");
            item.ID = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("InvType");
            item.InvType = Convert.ToInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("CreatorUUID");
            item.CreatorId = reader.ReadString();
            reader.ReadEndElement();
            reader.ReadStartElement("CreationDate");
            item.CreationDate = Convert.ToInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("Owner");
            item.Owner = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadElementString("Description");
            reader.ReadStartElement("AssetType");
            item.AssetType = Convert.ToInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("AssetID");
            item.AssetID = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("SaleType");
            item.SaleType = Convert.ToByte(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("SalePrice");
            item.SalePrice = Convert.ToInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("BasePermissions");
            item.BasePermissions = Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("CurrentPermissions");
            item.CurrentPermissions = Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("EveryOnePermssions");
            item.EveryOnePermissions = Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("NextPermissions");
            item.NextPermissions = Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("Flags");
            item.Flags = Convert.ToUInt32(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("GroupID");
            item.GroupID = UUID.Parse(reader.ReadString());
            reader.ReadEndElement();
            reader.ReadStartElement("GroupOwned");
            item.GroupOwned = Convert.ToBoolean(reader.ReadString());
            reader.ReadEndElement();
            
            return item;
        }      
        
        public static string Serialize(InventoryItemBase inventoryItem)
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter writer = new XmlTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();

            writer.WriteStartElement("InventoryItem");

            writer.WriteStartElement("Name");
            writer.WriteString(inventoryItem.Name);
            writer.WriteEndElement();
            writer.WriteStartElement("ID");
            writer.WriteString(inventoryItem.ID.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("InvType");
            writer.WriteString(inventoryItem.InvType.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("CreatorUUID");
            writer.WriteString(inventoryItem.CreatorId);
            writer.WriteEndElement();
            writer.WriteStartElement("CreationDate");
            writer.WriteString(inventoryItem.CreationDate.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("Owner");
            writer.WriteString(inventoryItem.Owner.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("Description");
            writer.WriteString(inventoryItem.Description);
            writer.WriteEndElement();
            writer.WriteStartElement("AssetType");
            writer.WriteString(inventoryItem.AssetType.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("AssetID");
            writer.WriteString(inventoryItem.AssetID.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("SaleType");
            writer.WriteString(inventoryItem.SaleType.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("SalePrice");
            writer.WriteString(inventoryItem.SalePrice.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("BasePermissions");
            writer.WriteString(inventoryItem.BasePermissions.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("CurrentPermissions");
            writer.WriteString(inventoryItem.CurrentPermissions.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("EveryOnePermssions");
            writer.WriteString(inventoryItem.EveryOnePermissions.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("NextPermissions");
            writer.WriteString(inventoryItem.NextPermissions.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("Flags");
            writer.WriteString(inventoryItem.Flags.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("GroupID");
            writer.WriteString(inventoryItem.GroupID.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("GroupOwned");
            writer.WriteString(inventoryItem.GroupOwned.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
            
            writer.Close();
            sw.Close();
            
            return sw.ToString();
        }        
    }
}
