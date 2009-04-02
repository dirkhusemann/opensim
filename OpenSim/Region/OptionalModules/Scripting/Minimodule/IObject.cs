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
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Drawing;
using OpenMetaverse;

namespace OpenSim.Region.OptionalModules.Scripting.Minimodule
{
    public interface IObject
    {
        bool Exists { get; }
        uint LocalID { get; }
        UUID GlobalID { get; }

        String Name { get; set; }
        String Description { get; set; }

        

        /// <summary>
        /// Returns the root object of a linkset. If this object is the root, it will return itself.
        /// </summary>
        IObject Root { get; }

        /// <summary>
        /// Returns a collection of objects which are linked to the current object. Does not include the root object.
        /// </summary>
        IObject[] Children { get; }

        /// <summary>
        /// Returns a list of materials attached to this object. Each may contain unique texture 
        /// and other visual information. For primitive based objects, this correlates with 
        /// Object Faces. For mesh based objects, this correlates with Materials.
        /// </summary>
        IObjectMaterial[] Materials { get; }

        /// <summary>
        /// The bounding box of the object. Primitive and Mesh objects alike are scaled to fit within these bounds.
        /// </summary>
        Vector3 Scale { get; set; }

        /// <summary>
        /// The rotation of the object relative to the Scene
        /// </summary>
        Quaternion Rotation { get; set; }

        /// <summary>
        /// The position of the object relative to the Scene
        /// </summary>
        Vector3 Position { get; set; }


        Vector3 SitTarget { get; set; }
        String SitTargetText { get; set; }

        String TouchText { get; set; }

        String Text { get; set; }

        bool IsPhysical { get; set; } // SetStatus(PHYSICS)
        bool IsPhantom { get; set; } // SetStatus(PHANTOM)
        bool IsRotationLockedX { get; set; } // SetStatus(!ROTATE_X)
        bool IsRotationLockedY { get; set; } // SetStatus(!ROTATE_Y)
        bool IsRotationLockedZ { get; set; } // SetStatus(!ROTATE_Z)
        bool IsSandboxed { get; set; } // SetStatus(SANDBOX)
        bool IsImmotile { get; set; } // SetStatus(BLOCK_GRAB)
        bool IsAlwaysReturned { get; set; } // SetStatus(!DIE_AT_EDGE)
        bool IsTemporary { get; set; } // TEMP_ON_REZ

        bool IsFlexible { get; set; }

        PrimType PrimShape { get; set; }
        // TODO:
        // PrimHole
        // Repeats, Offsets, Cut/Dimple/ProfileCut
        // Hollow, Twist, HoleSize,
        // Taper[A+B], Shear[A+B], Revolutions,
        // RadiusOffset, Skew

        PhysicsMaterial PhysicsMaterial { get; set; }
    }

    public enum PhysicsMaterial
    {
        Default,
        Glass,
        Metal,
        Plastic,
        Wood,
        Rubber,
        Stone,
        Flesh
    }

    public enum PrimType
    {
        NotPrimitive = 255,
        Box = 0,
        Cylinder = 1,
        Prism = 2,
        Sphere = 3,
        Torus = 4,
        Tube = 5,
        Ring = 6,
        Sculpt = 7
    }

    public enum TextureMapping
    {
        Default,
        Planar
    }

    public interface IObjectMaterial
    {
        Color Color { get; set; }
        UUID Texture { get; set; }
        TextureMapping Mapping { get; set; } // SetPrimParms(PRIM_TEXGEN)
        bool Bright { get; set; } // SetPrimParms(FULLBRIGHT)
        double Bloom { get; set; } // SetPrimParms(GLOW)
        bool Shiny { get; set; } // SetPrimParms(SHINY)
        bool BumpMap { get; set; } // SetPrimParms(BUMPMAP) [DEPRECIATE IN FAVOUR OF UUID?]
    }
}
