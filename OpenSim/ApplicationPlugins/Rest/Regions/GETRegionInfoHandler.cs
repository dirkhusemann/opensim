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
using System.Xml.Serialization;
using OpenMetaverse;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.ApplicationPlugins.Rest.Regions
{
    public partial class RestRegionPlugin : RestPlugin
    {
        #region GET methods
        public string GetRegionInfoHandler(string request, string path, string param,
                                           OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            // foreach (string h in httpRequest.Headers.AllKeys)
            //     foreach (string v in httpRequest.Headers.GetValues(h))
            //         m_log.DebugFormat("{0} IsGod: {1} -> {2}", MsgID, h, v);

            MsgID = RequestID;
            m_log.DebugFormat("{0} GET path {1} param {2}", MsgID, path, param);

            try
            {
                // param empty: regions list
                // if (String.IsNullOrEmpty(param)) 
                return GetRegionInfoHandlerRegions(httpResponse);
                    
                // // param not empty: specific region
                // return GetRegionInfoHandlerRegion(httpResponse, param);
            }
            catch (Exception e)
            {
                return Failure(httpResponse, OSHttpStatusCode.ServerErrorInternalError, "GET", e);
            }
        }

        public string GetRegionInfoHandlerRegions(OSHttpResponse httpResponse)
        {
            // regions info
            XmlWriter.WriteStartElement(String.Empty, "regions", String.Empty);
            {
                // regions info: number of regions
                XmlWriter.WriteStartAttribute(String.Empty, "number", String.Empty);
                XmlWriter.WriteValue(App.SceneManager.Scenes.Count);
                XmlWriter.WriteEndAttribute();

                // regions info: max number of regions
                XmlWriter.WriteStartAttribute(String.Empty, "max", String.Empty);
                if (App.ConfigSource.Source.Configs["RemoteAdmin"] != null)
                {
                    XmlWriter.WriteValue(App.ConfigSource.Source.Configs["RemoteAdmin"].GetInt("region_limit", -1));
                }
                else
                {
                    XmlWriter.WriteValue(-1);
                }
                XmlWriter.WriteEndAttribute();
                
                // regions info: region
                foreach (Scene s in App.SceneManager.Scenes)
                {
                    XmlWriter.WriteStartElement(String.Empty, "region", String.Empty);
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "uuid", String.Empty);
                    XmlWriter.WriteString(s.RegionInfo.RegionID.ToString());
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "name", String.Empty);
                    XmlWriter.WriteString(s.RegionInfo.RegionName);
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "x", String.Empty);
                    XmlWriter.WriteValue(s.RegionInfo.RegionLocX);
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "y", String.Empty);
                    XmlWriter.WriteValue(s.RegionInfo.RegionLocY);
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "external_hostname", String.Empty);
                    XmlWriter.WriteString(s.RegionInfo.ExternalHostName);
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "master_name", String.Empty);
                    XmlWriter.WriteString(String.Format("{0} {1}", s.RegionInfo.MasterAvatarFirstName, s.RegionInfo.MasterAvatarLastName));
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "master_uuid", String.Empty);
                    XmlWriter.WriteString(s.RegionInfo.MasterAvatarAssignedUUID.ToString());
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "ip", String.Empty);
                    XmlWriter.WriteString(s.RegionInfo.InternalEndPoint.ToString());
                    XmlWriter.WriteEndAttribute();
                    
                    int users = s.GetAvatars().Count;
                    XmlWriter.WriteStartAttribute(String.Empty, "avatars", String.Empty);
                    XmlWriter.WriteValue(users);
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteStartAttribute(String.Empty, "objects", String.Empty);
                    XmlWriter.WriteValue(s.Entities.Count - users);
                    XmlWriter.WriteEndAttribute();
                    
                    XmlWriter.WriteEndElement();
                }
            }
            XmlWriter.WriteEndElement();

            return XmlWriterResult;
        }
        #endregion GET methods
    }
}
