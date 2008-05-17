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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.Framework;

namespace OpenSim.Framework
{
    [Serializable]
    public class AvatarAppearance : ISerializable
    {
        // these are guessed at by the list here -
        // http://wiki.secondlife.com/wiki/Avatar_Appearance.  We'll
        // correct them over time for when were are wrong.
        public readonly static int BODY = 0;
        public readonly static int SKIN = 1;
        public readonly static int HAIR = 2;
        public readonly static int EYES = 3;
        public readonly static int SHIRT = 4;
        public readonly static int PANTS = 5;
        public readonly static int SHOES = 6;
        public readonly static int SOCKS = 7;
        public readonly static int JACKET = 8;
        public readonly static int GLOVES = 9;
        public readonly static int UNDERSHIRT = 10;
        public readonly static int UNDERPANTS = 11;
        public readonly static int SKIRT = 12;

        private readonly static int MAX_WEARABLES = 13;

        private static LLUUID BODY_ASSET = new LLUUID("66c41e39-38f9-f75a-024e-585989bfab73");
        private static LLUUID BODY_ITEM = new LLUUID("66c41e39-38f9-f75a-024e-585989bfaba9");
        private static LLUUID SKIN_ASSET = new LLUUID("77c41e39-38f9-f75a-024e-585989bbabbb");
        private static LLUUID SKIN_ITEM = new LLUUID("77c41e39-38f9-f75a-024e-585989bfabc9");
        private static LLUUID SHIRT_ASSET = new LLUUID("00000000-38f9-1111-024e-222222111110");
        private static LLUUID SHIRT_ITEM = new LLUUID("77c41e39-38f9-f75a-0000-585989bf0000");
        private static LLUUID PANTS_ASSET = new LLUUID("00000000-38f9-1111-024e-222222111120");
        private static LLUUID PANTS_ITEM = new LLUUID("77c41e39-38f9-f75a-0000-5859892f1111");

        public readonly static int VISUALPARAM_COUNT = 218;

        protected LLUUID m_owner;

        public LLUUID Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }
        protected int m_serial = 1;

        public int Serial
        {
            get { return m_serial; }
            set { m_serial = value; }
        }

        protected byte[] m_visualparams;

        public byte[] VisualParams
        {
            get { return m_visualparams; }
            set { m_visualparams = value; }
        }

        protected AvatarWearable[] m_wearables;

        public AvatarWearable[] Wearables
        {
            get { return m_wearables; }
            set { m_wearables = value; }
        }

        public LLUUID BodyItem {
            get { return m_wearables[BODY].ItemID; }
            set { m_wearables[BODY].ItemID = value; }
        }
        public LLUUID BodyAsset {
            get { return m_wearables[BODY].AssetID; }
            set { m_wearables[BODY].AssetID = value; }
        }
        public LLUUID SkinItem {
            get { return m_wearables[SKIN].ItemID; }
            set { m_wearables[SKIN].ItemID = value; }
        }
        public LLUUID SkinAsset {
            get { return m_wearables[SKIN].AssetID; }
            set { m_wearables[SKIN].AssetID = value; }
        }
        public LLUUID HairItem {
            get { return m_wearables[HAIR].ItemID; }
            set { m_wearables[HAIR].ItemID = value; }
        }
        public LLUUID HairAsset {
            get { return m_wearables[HAIR].AssetID; }
            set { m_wearables[HAIR].AssetID = value; }
        }
        public LLUUID EyesItem {
            get { return m_wearables[EYES].ItemID; }
            set { m_wearables[EYES].ItemID = value; }
        }
        public LLUUID EyesAsset {
            get { return m_wearables[EYES].AssetID; }
            set { m_wearables[EYES].AssetID = value; }
        }
        public LLUUID ShirtItem {
            get { return m_wearables[SHIRT].ItemID; }
            set { m_wearables[SHIRT].ItemID = value; }
        }
        public LLUUID ShirtAsset {
            get { return m_wearables[SHIRT].AssetID; }
            set { m_wearables[SHIRT].AssetID = value; }
        }
        public LLUUID PantsItem {
            get { return m_wearables[PANTS].ItemID; }
            set { m_wearables[PANTS].ItemID = value; }
        }
        public LLUUID PantsAsset {
            get { return m_wearables[BODY].AssetID; }
            set { m_wearables[BODY].AssetID = value; }
        }
        public LLUUID ShoesItem {
            get { return m_wearables[SHOES].ItemID; }
            set { m_wearables[SHOES].ItemID = value; }
        }
        public LLUUID ShoesAsset {
            get { return m_wearables[SHOES].AssetID; }
            set { m_wearables[SHOES].AssetID = value; }
        }
        public LLUUID SocksItem {
            get { return m_wearables[SOCKS].ItemID; }
            set { m_wearables[SOCKS].ItemID = value; }
        }
        public LLUUID SocksAsset {
            get { return m_wearables[SOCKS].AssetID; }
            set { m_wearables[SOCKS].AssetID = value; }
        }
        public LLUUID JacketItem {
            get { return m_wearables[JACKET].ItemID; }
            set { m_wearables[JACKET].ItemID = value; }
        }
        public LLUUID JacketAsset {
            get { return m_wearables[JACKET].AssetID; }
            set { m_wearables[JACKET].AssetID = value; }
        }
        public LLUUID GlovesItem {
            get { return m_wearables[GLOVES].ItemID; }
            set { m_wearables[GLOVES].ItemID = value; }
        }
        public LLUUID GlovesAsset {
            get { return m_wearables[GLOVES].AssetID; }
            set { m_wearables[GLOVES].AssetID = value; }
        }
        public LLUUID UnderShirtItem {
            get { return m_wearables[UNDERSHIRT].ItemID; }
            set { m_wearables[UNDERSHIRT].ItemID = value; }
        }
        public LLUUID UnderShirtAsset {
            get { return m_wearables[UNDERSHIRT].AssetID; }
            set { m_wearables[UNDERSHIRT].AssetID = value; }
        }
        public LLUUID UnderPantsItem {
            get { return m_wearables[UNDERPANTS].ItemID; }
            set { m_wearables[UNDERPANTS].ItemID = value; }
        }
        public LLUUID UnderPantsAsset {
            get { return m_wearables[UNDERPANTS].AssetID; }
            set { m_wearables[UNDERPANTS].AssetID = value; }
        }
        public LLUUID SkirtItem {
            get { return m_wearables[SKIRT].ItemID; }
            set { m_wearables[SKIRT].ItemID = value; }
        }
        public LLUUID SkirtAsset {
            get { return m_wearables[SKIRT].AssetID; }
            set { m_wearables[SKIRT].AssetID = value; }
        }

        public void SetDefaultWearables()
        {
            m_wearables[BODY].AssetID = BODY_ASSET;
            m_wearables[BODY].ItemID = BODY_ITEM;
            m_wearables[SKIN].AssetID = SKIN_ASSET;
            m_wearables[SKIN].ItemID = SKIN_ITEM;
            m_wearables[SHIRT].AssetID = SHIRT_ASSET;
            m_wearables[SHIRT].ItemID = SHIRT_ITEM;
            m_wearables[PANTS].AssetID = PANTS_ASSET;
            m_wearables[PANTS].ItemID = PANTS_ITEM;
        }

        protected LLObject.TextureEntry m_texture;

        public LLObject.TextureEntry Texture
        {
            get { return m_texture; }
            set { m_texture = value; }
        }

        protected float m_avatarHeight = 0;

        public float AvatarHeight
        {
            get { return m_avatarHeight; }
            set { m_avatarHeight = value; }
        }

        public AvatarAppearance()
        {
            m_wearables = new AvatarWearable[MAX_WEARABLES];
            for (int i = 0; i < MAX_WEARABLES; i++)
            {
                // this makes them all null
                m_wearables[i] = new AvatarWearable();
            }
            m_serial = 0;
            m_owner = LLUUID.Zero;
            m_visualparams = new byte[VISUALPARAM_COUNT];
            SetDefaultWearables();
            m_texture = GetDefaultTexture();
        }

        public AvatarAppearance(LLUUID avatarID, AvatarWearable[] wearables, byte[] visualParams)
        {
            m_owner = avatarID;
            m_serial = 1;
            m_wearables = wearables;
            m_visualparams = visualParams;
            m_texture = GetDefaultTexture();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="visualParam"></param>
        public void SetAppearance(byte[] texture, List<byte> visualParam)
        {
            LLObject.TextureEntry textureEnt = new LLObject.TextureEntry(texture, 0, texture.Length);
            m_texture = textureEnt;

            m_visualparams = visualParam.ToArray();

            // Teravus : Nifty AV Height Getting Maaaaagical formula.  Oh how we love turning 0-255 into meters.
            // (float)m_visualParams[25] = Height
            // (float)m_visualParams[125] = LegLength
            m_avatarHeight = (1.50856f + (((float) m_visualparams[25]/255.0f)*(2.525506f - 1.50856f)))
                + (((float) m_visualparams[125]/255.0f)/1.5f);
        }

        public void SetWearable(int wearableId, AvatarWearable wearable)
        {
            m_wearables[wearableId] = wearable;
        }

        public static LLObject.TextureEntry GetDefaultTexture()
        {
            LLObject.TextureEntry textu = new LLObject.TextureEntry(new LLUUID("C228D1CF-4B5D-4BA8-84F4-899A0796AA97"));
            textu.CreateFace(0).TextureID = new LLUUID("00000000-0000-1111-9999-000000000012");
            textu.CreateFace(1).TextureID = new LLUUID("5748decc-f629-461c-9a36-a35a221fe21f");
            textu.CreateFace(2).TextureID = new LLUUID("5748decc-f629-461c-9a36-a35a221fe21f");
            textu.CreateFace(3).TextureID = new LLUUID("6522E74D-1660-4E7F-B601-6F48C1659A77");
            textu.CreateFace(4).TextureID = new LLUUID("7CA39B4C-BD19-4699-AFF7-F93FD03D3E7B");
            textu.CreateFace(5).TextureID = new LLUUID("00000000-0000-1111-9999-000000000010");
            textu.CreateFace(6).TextureID = new LLUUID("00000000-0000-1111-9999-000000000011");
            return textu;
        }

        public override String ToString()
        {
            String s = "[Wearables] =>";
            s += "Body Item: " + BodyItem.ToString() + ";";
            s += "Skin Item: " + SkinItem.ToString() + ";";
            s += "Shirt Item: " + ShirtItem.ToString() + ";";
            s += "Pants Item: " + PantsItem.ToString() + ";";
            return s;
        }

        protected AvatarAppearance(SerializationInfo info, StreamingContext context)
        {
            //System.Console.WriteLine("AvatarAppearance Deserialize BGN");

            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            m_owner = new LLUUID((Guid)info.GetValue("m_scenePresenceID", typeof(Guid)));
            m_serial = (int)info.GetValue("m_wearablesSerial", typeof(int));
            m_visualparams = (byte[])info.GetValue("m_visualParams", typeof(byte[]));
            m_wearables = (AvatarWearable[])info.GetValue("m_wearables", typeof(AvatarWearable[]));

            byte[] m_textureEntry_work = (byte[])info.GetValue("m_textureEntry", typeof(byte[]));
            m_texture = new LLObject.TextureEntry(m_textureEntry_work, 0, m_textureEntry_work.Length);

            m_avatarHeight = (float)info.GetValue("m_avatarHeight", typeof(float));

            //System.Console.WriteLine("AvatarAppearance Deserialize END");
        }

        // this is used for OGS1
        public Hashtable ToHashTable()
        {
            Hashtable h = new Hashtable();
            h["owner"] = Owner.ToString();
            h["serial"] = Serial.ToString();
            h["visual_params"] = VisualParams;
            h["texture"] = Texture.ToBytes();
            h["avatar_height"] = AvatarHeight.ToString();
            h["body_item"] = BodyItem.ToString();
            h["body_asset"] = BodyAsset.ToString();
            h["skin_item"] = SkinItem.ToString();
            h["skin_asset"] = SkinAsset.ToString();
            h["hair_item"] = HairItem.ToString();
            h["hair_asset"] = HairAsset.ToString();
            h["eyes_item"] = EyesItem.ToString();
            h["eyes_asset"] = EyesAsset.ToString();
            h["shirt_item"] = ShirtItem.ToString();
            h["shirt_asset"] = ShirtAsset.ToString();
            h["pants_item"] = PantsItem.ToString();
            h["pants_asset"] = PantsAsset.ToString();
            h["shoes_item"] = ShoesItem.ToString();
            h["shoes_asset"] = ShoesAsset.ToString();
            h["socks_item"] = SocksItem.ToString();
            h["socks_asset"] = SocksAsset.ToString();
            h["jacket_item"] = JacketItem.ToString();
            h["jacket_asset"] = JacketAsset.ToString();
            h["gloves_item"] = GlovesItem.ToString();
            h["gloves_asset"] = GlovesAsset.ToString();
            h["undershirt_item"] = UnderShirtItem.ToString();
            h["undershirt_asset"] = UnderShirtAsset.ToString();
            h["underpants_item"] = UnderPantsItem.ToString();
            h["underpants_asset"] = UnderPantsAsset.ToString();
            h["skirt_item"] = SkirtItem.ToString();
            h["skirt_asset"] = SkirtAsset.ToString();
            return h;
        }

        public AvatarAppearance(Hashtable h)
        {
            Owner = new LLUUID((string)h["owner"]);
            Serial = Convert.ToInt32((string)h["serial"]);
            VisualParams = (byte[])h["visual_params"];
            Texture = new LLObject.TextureEntry((byte[])h["texture"], 0, ((byte[])h["texture"]).Length);
            AvatarHeight = (float)Convert.ToDouble((string)h["avatar_height"]);
            
            m_wearables = new AvatarWearable[MAX_WEARABLES];
            for (int i = 0; i < MAX_WEARABLES; i++)
            {
                // this makes them all null
                m_wearables[i] = new AvatarWearable();
            }

            BodyItem = new LLUUID((string)h["body_item"]);
            BodyAsset = new LLUUID((string)h["body_asset"]);
            SkinItem = new LLUUID((string)h["skin_item"]);
            SkinAsset = new LLUUID((string)h["skin_asset"]);
            HairItem = new LLUUID((string)h["hair_item"]);
            HairAsset = new LLUUID((string)h["hair_asset"]);
            EyesItem = new LLUUID((string)h["eyes_item"]);
            EyesAsset = new LLUUID((string)h["eyes_asset"]);
            ShirtItem = new LLUUID((string)h["shirt_item"]);
            ShirtAsset = new LLUUID((string)h["shirt_asset"]);
            PantsItem = new LLUUID((string)h["pants_item"]);
            PantsAsset = new LLUUID((string)h["pants_asset"]);
            ShoesItem = new LLUUID((string)h["shoes_item"]);
            ShoesAsset = new LLUUID((string)h["shoes_asset"]);
            SocksItem = new LLUUID((string)h["socks_item"]);
            SocksAsset = new LLUUID((string)h["socks_asset"]);
            JacketItem = new LLUUID((string)h["jacket_item"]);
            JacketAsset = new LLUUID((string)h["jacket_asset"]);
            GlovesItem = new LLUUID((string)h["gloves_item"]);
            GlovesAsset = new LLUUID((string)h["gloves_asset"]);
            UnderShirtItem = new LLUUID((string)h["undershirt_item"]);
            UnderShirtAsset = new LLUUID((string)h["undershirt_asset"]);
            UnderPantsItem = new LLUUID((string)h["underpants_item"]);
            UnderPantsAsset = new LLUUID((string)h["underpants_asset"]);
            SkirtItem = new LLUUID((string)h["skirt_item"]);
            SkirtAsset = new LLUUID((string)h["skirt_asset"]);
        }

        [SecurityPermission(SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public virtual void GetObjectData(
                        SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("m_scenePresenceID", m_owner.UUID);
            info.AddValue("m_wearablesSerial", m_serial);
            info.AddValue("m_visualParams", m_visualparams);
            info.AddValue("m_wearables", m_wearables);
            info.AddValue("m_textureEntry", m_texture.ToBytes());
            info.AddValue("m_avatarHeight", m_avatarHeight);
        }
    }
}
