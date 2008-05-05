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
using Axiom.Math;
using libsecondlife;
using Nini.Config;
using OpenSim.Framework.Console;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

//using OpenSim.Region.ScriptEngine.DotNetEngine.Compiler.LSL;

namespace OpenSim.Region.ScriptEngine.Common
{
    public class OSSL_BuilIn_Commands : LSL_BuiltIn_Commands, OSSL_BuilIn_Commands_Interface
    {
        public OSSL_BuilIn_Commands(ScriptEngineBase.ScriptEngine scriptEngine, SceneObjectPart host, uint localID,
                                    LLUUID itemID)
            : base(scriptEngine, host, localID, itemID)
        {
            Prim = new OSSLPrim(this);
        }

        public OSSLPrim Prim;

        [Serializable]
        public class OSSLPrim
        {
            internal OSSL_BuilIn_Commands OSSL;
            public OSSLPrim(OSSL_BuilIn_Commands bc)
            {
                OSSL = bc;
                Position = new OSSLPrim_Position(this);
                Rotation = new OSSLPrim_Rotation(this);
            }

            public OSSLPrim_Position Position;
            public OSSLPrim_Rotation Rotation;
            //public LSL_Types.Vector3 Position
            //{
            //    get { return OSSL.llGetPos(); }
            //    set { OSSL.llSetPos(value); }
            //}
            //public LSL_Types.Quaternion Rotation
            //{
            //    get { return OSSL.llGetRot(); }
            //    set { OSSL.llSetRot(value); }
            //}
            private TextStruct _text;
            public TextStruct Text
            {
                get { return _text; }
                set
                {
                    _text = value;
                    OSSL.llSetText(_text.Text, _text.color, _text.alpha);
                }
            }

            [Serializable]
            public struct TextStruct
            {
                public string Text;
                public LSL_Types.Vector3 color;
                public double alpha;
            }
        }

        [Serializable]
        public class OSSLPrim_Position
        {
            private OSSLPrim prim;
            private LSL_Types.Vector3 Position;
            public OSSLPrim_Position(OSSLPrim _prim)
            {
                prim = _prim;
            }
            private void Load()
            {
                Position = prim.OSSL.llGetPos();
            }
            private void Save()
            {
                if (Position.x > 255)
                    Position.x = 255;
                if (Position.x < 0)
                    Position.x = 0;
                if (Position.y > 255)
                    Position.y = 255;
                if (Position.y < 0)
                    Position.y = 0;
                if (Position.z > 768)
                    Position.z = 768;
                if (Position.z < 0)
                    Position.z = 0;
                prim.OSSL.llSetPos(Position);
            }

            public double x
            {
                get
                {
                    Load();
                    return Position.x;
                }
                set
                {
                    Load();
                    Position.x = value;
                    Save();
                }
            }
            public double y
            {
                get
                {
                    Load();
                    return Position.y;
                }
                set
                {
                    Load();
                    Position.y = value;
                    Save();
                }
            }
            public double z
            {
                get
                {
                    Load();
                    return Position.z;
                }
                set
                {
                    Load();
                    Position.z = value;
                    Save();
                }
            }
        }

        [Serializable]
        public class OSSLPrim_Rotation
        {
            private OSSLPrim prim;
            private LSL_Types.Quaternion Rotation;
            public OSSLPrim_Rotation(OSSLPrim _prim)
            {
                prim = _prim;
            }
            private void Load()
            {
                Rotation = prim.OSSL.llGetRot();
            }
            private void Save()
            {
                prim.OSSL.llSetRot(Rotation);
            }

            public double x
            {
                get
                {
                    Load();
                    return Rotation.x;
                }
                set
                {
                    Load();
                    Rotation.x = value;
                    Save();
                }
            }
            public double y
            {
                get
                {
                    Load();
                    return Rotation.y;
                }
                set
                {
                    Load();
                    Rotation.y = value;
                    Save();
                }
            }
            public double z
            {
                get
                {
                    Load();
                    return Rotation.z;
                }
                set
                {
                    Load();
                    Rotation.z = value;
                    Save();
                }
            }
            public double s
            {
                get
                {
                    Load();
                    return Rotation.s;
                }
                set
                {
                    Load();
                    Rotation.s = value;
                    Save();
                }
            }
        }
        //public struct OSSLPrim_Rotation
        //{
        //    public double X;
        //    public double Y;
        //    public double Z;
        //    public double R;
        //}


                //
        // OpenSim functions
        //

        public int osTerrainSetHeight(int x, int y, double val)
        {
            m_host.AddScriptLPS(1);
            if (x > 255 || x < 0 || y > 255 || y < 0)
                LSLError("osTerrainSetHeight: Coordinate out of bounds");

            if (World.Permissions.CanTerraform(m_host.OwnerID, new LLVector3(x, y, 0)))
            {
                World.Heightmap[x, y] = val;
                return 1;
            }
            else
            {
                return 0;
            }
        }
     
        public double osTerrainGetHeight(int x, int y)
        {
            m_host.AddScriptLPS(1);
            if (x > 255 || x < 0 || y > 255 || y < 0)
                LSLError("osTerrainGetHeight: Coordinate out of bounds");

            return World.Heightmap[x, y];
        }

        public int osRegionRestart(double seconds)
        {
            m_host.AddScriptLPS(1);
            if (World.Permissions.CanRestartSim(m_host.OwnerID))
            {
                World.Restart((float)seconds);
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public void osRegionNotice(string msg)
        {
            m_host.AddScriptLPS(1);
            World.SendGeneralAlert(msg);
        }

        public void osSetRot(LLUUID target, Quaternion rotation)
        {
            m_host.AddScriptLPS(1);
            if (World.Entities.ContainsKey(target))
            {
                World.Entities[target].Rotation = rotation;
            }
            else
            {
                LSLError("osSetRot: Invalid target");
            }
        }

        public string osSetDynamicTextureURL(string dynamicID, string contentType, string url, string extraParams,
                                             int timer)
        {
            m_host.AddScriptLPS(1);
            if (dynamicID == String.Empty)
            {
                IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
                LLUUID createdTexture =
                    textureManager.AddDynamicTextureURL(World.RegionInfo.RegionID, m_host.UUID, contentType, url,
                                                        extraParams, timer);
                return createdTexture.ToString();
            }
            else
            {
                //TODO update existing dynamic textures
            }

            return LLUUID.Zero.ToString();
        }

        public string osSetDynamicTextureURLBlend(string dynamicID, string contentType, string url, string extraParams,
                                             int timer, int alpha)
        {
            m_host.AddScriptLPS(1);
            if (dynamicID == String.Empty)
            {
                IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
                LLUUID createdTexture =
                    textureManager.AddDynamicTextureURL(World.RegionInfo.RegionID, m_host.UUID, contentType, url,
                                                        extraParams, timer, true, (byte) alpha );
                return createdTexture.ToString();
            }
            else
            {
                //TODO update existing dynamic textures
            }

            return LLUUID.Zero.ToString();
        }

        public string osSetDynamicTextureData(string dynamicID, string contentType, string data, string extraParams,
                                           int timer)
        {
            m_host.AddScriptLPS(1);
            if (dynamicID == String.Empty)
            {
                IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
                if (textureManager != null)
                {
                    LLUUID createdTexture =
                        textureManager.AddDynamicTextureData(World.RegionInfo.RegionID, m_host.UUID, contentType, data,
                                                            extraParams, timer);
                    return createdTexture.ToString();
                }
            }
            else
            {
                //TODO update existing dynamic textures
            }

            return LLUUID.Zero.ToString();
        }

        public string osSetDynamicTextureDataBlend(string dynamicID, string contentType, string data, string extraParams,
                                          int timer, int alpha)
        {
            m_host.AddScriptLPS(1);
            if (dynamicID == String.Empty)
            {
                IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
                if (textureManager != null)
                {
                    LLUUID createdTexture =
                        textureManager.AddDynamicTextureData(World.RegionInfo.RegionID, m_host.UUID, contentType, data,
                                                            extraParams, timer, true, (byte) alpha);
                    return createdTexture.ToString();
                }
            }
            else
            {
                //TODO update existing dynamic textures
            }

            return LLUUID.Zero.ToString();
        }

        public bool osConsoleCommand(string command)
        {
            m_host.AddScriptLPS(1);
            IConfigSource config = new IniConfigSource(Application.iniFilePath);
            if (config.Configs["LL-Functions"] == null)
                config.AddConfig("LL-Functions");

            if (config.Configs["LL-Functions"].GetBoolean("AllowosConsoleCommand", false))
            {
                if (World.Permissions.CanRunConsoleCommand(m_host.OwnerID))
                {
                    MainConsole.Instance.RunCommand(command);
                    return true;
                }
                return false;
            }
            return false;
        }
        public void osSetPrimFloatOnWater(int floatYN)
        {
            m_host.AddScriptLPS(1);
            if (m_host.ParentGroup != null)
            {
                if (m_host.ParentGroup.RootPart != null)
                {
                    m_host.ParentGroup.RootPart.SetFloatOnWater(floatYN);
                }
            }
        }

        // Adam's super super custom animation functions
        public void osAvatarPlayAnimation(string avatar, string animation)
        {
            m_host.AddScriptLPS(1);
            if (World.Entities.ContainsKey(avatar) && World.Entities[avatar] is ScenePresence)
            {
                ScenePresence target = (ScenePresence)World.Entities[avatar];
                target.AddAnimation(avatar);
            }
        }

        public void osAvatarStopAnimation(string avatar, string animation)
        {
            m_host.AddScriptLPS(1);
            if (World.Entities.ContainsKey(avatar) && World.Entities[avatar] is ScenePresence)
            {
                ScenePresence target = (ScenePresence)World.Entities[avatar];
                target.RemoveAnimation(animation);
            }
        }

        //Texture draw functions 
        public string osMovePen(string drawList, int x, int y)
        {
            m_host.AddScriptLPS(1);
            drawList += "MoveTo " + x + "," + y + ";";
            return drawList;
        }

        public string osDrawLine(string drawList, int startX, int startY, int endX, int endY)
        {
            m_host.AddScriptLPS(1);
            drawList += "MoveTo "+ startX+","+ startY +"; LineTo "+endX +","+endY +"; ";
            return drawList;
        }

        public string osDrawLine(string drawList, int endX, int endY)
        {
            m_host.AddScriptLPS(1);
            drawList += "LineTo " + endX + "," + endY + "; ";
            return drawList;
        }

        public string osDrawText(string drawList, string text)
        {
            m_host.AddScriptLPS(1);
            drawList += "Text " + text + "; ";
            return drawList;
        }

        public string osDrawEllipse(string drawList, int width, int height)
        {
            m_host.AddScriptLPS(1);
            drawList += "Ellipse " + width + "," + height + "; ";
            return drawList;
        }

        public string osDrawRectangle(string drawList, int width, int height)
        {
            m_host.AddScriptLPS(1);
            drawList += "Rectangle " + width + "," + height + "; ";
            return drawList;
        }

        public string osDrawFilledRectangle(string drawList, int width, int height)
        {
            m_host.AddScriptLPS(1);
            drawList += "FillRectangle " + width + "," + height + "; ";
            return drawList;
        }

        public string osSetFontSize(string drawList, int fontSize)
        {
            m_host.AddScriptLPS(1);
            drawList += "FontSize "+ fontSize +"; ";
            return drawList;
        }

        public string osSetPenSize(string drawList, int penSize)
        {
            m_host.AddScriptLPS(1);
            drawList += "PenSize " + penSize + "; ";
            return drawList;
        }

        public string osSetPenColour(string drawList, string colour)
        {
            m_host.AddScriptLPS(1);
            drawList += "PenColour " + colour + "; ";
            return drawList;
        }

        public string osDrawImage(string drawList, int width, int height, string imageUrl)
        {
           m_host.AddScriptLPS(1);
           drawList +="Image " +width + "," + height+ ","+ imageUrl +"; " ;
           return drawList;
        }

        public void osSetStateEvents(int events)
        {
            m_host.SetScriptEvents(m_itemID, events);
        }

    }
}
