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

#region Header

// PointMetaEntity.cs created with MonoDevelop
// User: bongiojp at 3:03 PM 8/6/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

#endregion Header

using System;
using System.Collections.Generic;
using System.Drawing;

using OpenMetaverse;

using Nini.Config;

using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Physics.Manager;

using log4net;

namespace OpenSim.Region.Environment.Modules.ContentManagement
{
    public class PointMetaEntity : MetaEntity
    {
        #region Constructors

        public PointMetaEntity(Scene scene, uint LocalId, Vector3 groupPos, float transparency)
            : base()
        {
            CreatePointEntity(scene, UUID.Random(), LocalId, groupPos);
            SetPartTransparency(m_Entity.RootPart, transparency);
        }

        public PointMetaEntity(Scene scene, UUID uuid, uint LocalId, Vector3 groupPos, float transparency)
            : base()
        {
            CreatePointEntity(scene, uuid, LocalId, groupPos);
            SetPartTransparency(m_Entity.RootPart, transparency);
        }

        #endregion Constructors

        #region Private Methods

        private void CreatePointEntity(Scene scene, UUID uuid, uint LocalId, Vector3 groupPos)
        {
            SceneObjectGroup x = new SceneObjectGroup();
            SceneObjectPart y = new SceneObjectPart();

            //Initialize part
            y.Name = "Very Small Point";
            y.RegionHandle = scene.RegionInfo.RegionHandle;
            y.CreationDate = (Int32) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            y.OwnerID = UUID.Zero;
            y.CreatorID = UUID.Zero;
            y.LastOwnerID = UUID.Zero;
            y.UUID = uuid;

            y.LocalId = LocalId;

            y.Shape = PrimitiveBaseShape.CreateBox();
            y.Scale = new Vector3(0.01f,0.01f,0.01f);
            y.LastOwnerID = UUID.Zero;
            y.GroupPosition = groupPos;
            y.OffsetPosition = new Vector3(0, 0, 0);
            y.RotationOffset = new Quaternion(0,0,0,0);
            y.Velocity = new Vector3(0, 0, 0);
            y.RotationalVelocity = new Vector3(0, 0, 0);
            y.AngularVelocity = new Vector3(0, 0, 0);
            y.Acceleration = new Vector3(0, 0, 0);

            y.Flags = 0;
            y.TrimPermissions();

            //Initialize group and add part as root part
            x.SetScene(scene);
            y.SetParent(x);
            y.ParentID = 0;
            y.LinkNum = 0;
            x.Children.Add(y.UUID, y);
            x.RootPart = y;
            x.RegionHandle = scene.RegionInfo.RegionHandle;
            x.SetScene(scene);

            m_Entity = x;
        }

        #endregion Private Methods
    }
}
