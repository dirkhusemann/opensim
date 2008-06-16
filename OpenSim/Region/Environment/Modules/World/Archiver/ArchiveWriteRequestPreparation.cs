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

using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Modules.World.Serialiser;
using OpenSim.Region.Environment.Scenes;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using libsecondlife;
using log4net;
using Nini.Config;

namespace OpenSim.Region.Environment.Modules.World.Archiver
{
    /// <summary>
    /// Prepare to write out an archive.
    /// </summary>
    public class ArchiveWriteRequestPreparation
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Scene m_scene;
        protected string m_savePath;   
        
        /// <summary>
        /// Used as a temporary store of an asset which represents an object.  This can be a null if no appropriate
        /// asset was found by the asset service.
        /// </summary>
        protected AssetBase m_requestedObjectAsset;
        
        /// <summary>
        /// Signal whether we are currently waiting for the asset service to deliver an asset.
        /// </summary>
        protected bool m_waitingForObjectAsset;

        /// <summary>
        /// Constructor
        /// </summary>
        public ArchiveWriteRequestPreparation(Scene scene, string savePath)
        {
            m_scene = scene;
            m_savePath = savePath;
        }
        
        /// <summary>
        /// The callback made when we request the asset for an object from the asset service.
        /// </summary>
        public void AssetRequestCallback(LLUUID assetID, AssetBase asset)
        {
            lock (this)
            {
                m_requestedObjectAsset = asset;
                m_waitingForObjectAsset = false;
                Monitor.Pulse(this);
            }
        }
        
        /// <summary>
        /// Get all the asset uuids associated with a given object.  This includes both those directly associated with
        /// it (e.g. face textures) and recursively, those of items within it's inventory (e.g. objects contained
        /// within this object).
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <param name="assetUuids"></param>
        protected void GetSceneObjectAssetUuids(SceneObjectGroup sceneObject, IDictionary<LLUUID, int> assetUuids)
        {
            m_log.DebugFormat(
                "[ARCHIVER]: Getting assets for object {0}, {1}", sceneObject.RootPart.Name, sceneObject.UUID);
            
            foreach (SceneObjectPart part in sceneObject.GetParts())
            {
                m_log.DebugFormat(
                    "[ARCHIVER]: Getting part {0}, {1} for object {2}", part.Name, part.UUID, sceneObject.UUID);
                
                LLObject.TextureEntry textureEntry = part.Shape.Textures;
                
                // Get the prim's default texture.  This will be used for faces which don't have their own texture
                assetUuids[textureEntry.DefaultTexture.TextureID] = 1;
                
                // XXX: Not a great way to iterate through face textures, but there's no
                // other method available to tell how many faces there actually are
                int i = 0;
                foreach (LLObject.TextureEntryFace texture in textureEntry.FaceTextures)
                {
                    if (texture != null)
                    {
                        m_log.DebugFormat("[ARCHIVER]: Got face {0}", i++);
                        assetUuids[texture.TextureID] = 1;
                    }
                }                                

                foreach (TaskInventoryItem tii in part.TaskInventory.Values)
                {
                    if (!assetUuids.ContainsKey(tii.AssetID))
                    {
                        assetUuids[tii.AssetID] = 1;
                        
                        if (tii.Type != (int)InventoryType.Object)
                        {
                            m_log.DebugFormat("[ARCHIVER]: Recording asset {0} in object {1}", tii.AssetID, part.UUID);                        
                        }
                        else
                        {
                            m_waitingForObjectAsset = true;
                            m_scene.AssetCache.GetAsset(tii.AssetID, AssetRequestCallback, true);
                            
                            // The asset cache callback can either 
                            //
                            // 1. Complete on the same thread (if the asset is already in the cache) or 
                            // 2. Come in via a different thread (if we need to go fetch it).
                            //
                            // The code below handles both these alternatives.
                            lock (this)
                            {
                                if (m_waitingForObjectAsset)
                                {
                                    Monitor.Wait(this);                            
                                    m_waitingForObjectAsset = false;
                                }
                            }
                            
                            if (null != m_requestedObjectAsset)
                            {
                                string xml = Helpers.FieldToUTF8String(m_requestedObjectAsset.Data);
                                SceneObjectGroup sog = new SceneObjectGroup(m_scene, m_scene.RegionInfo.RegionHandle, xml);
                                GetSceneObjectAssetUuids(sog, assetUuids);
                            }
                        }
                    }
                }
            }
        }        

        public void ArchiveRegion()
        {
            Dictionary<LLUUID, int> assetUuids = new Dictionary<LLUUID, int>();

            List<EntityBase> entities = m_scene.GetEntities();

            foreach (EntityBase entity in entities)
            {
                if (entity is SceneObjectGroup)
                {
                    GetSceneObjectAssetUuids((SceneObjectGroup)entity, assetUuids);
                }
            }

            string serializedEntities = SerializeObjects(entities);

            if (serializedEntities != null && serializedEntities.Length > 0)
            {
                m_log.DebugFormat("[ARCHIVER]: Successfully got serialization for {0} entities", entities.Count);
                m_log.DebugFormat("[ARCHIVER]: Requiring save of {0} assets", assetUuids.Count);

                // Asynchronously request all the assets required to perform this archive operation
                ArchiveWriteRequestExecution awre = new ArchiveWriteRequestExecution(serializedEntities, m_savePath);                
                new AssetsRequest(assetUuids.Keys, m_scene.AssetCache, awre.ReceivedAllAssets).Execute();
            }
        }

        /// <summary>
        /// Get an xml representation of the given scene objects.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        protected static string SerializeObjects(List<EntityBase> entities)
        {
            string serialization = "<scene>";

            List<string> serObjects = new List<string>();

            foreach (EntityBase ent in entities)
            {
                if (ent is SceneObjectGroup)
                {
                    serObjects.Add(((SceneObjectGroup) ent).ToXmlString2());
                }
            }

            foreach (string serObject in serObjects)
                serialization += serObject;

            serialization += "</scene>";

            return serialization;
        }
    }
}
