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
using System.Drawing;
using OpenSim.Region.Environment.Interfaces;

namespace OpenSim.Region.Environment.Modules.Terrain.FileLoaders
{
    public class GenericSystemDrawing : ITerrainLoader
    {
        #region ITerrainLoader Members

        public virtual ITerrainChannel LoadFile(string filename)
        {
            Bitmap file = new Bitmap(filename);

            ITerrainChannel retval = new TerrainChannel(file.Width, file.Height);

            int x, y;
            for (x = 0; x < file.Width; x++)
            {
                for (y = 0; y < file.Height; y++)
                {
                    retval[x, y] = file.GetPixel(x, y).GetBrightness() * 128;
                }
            }

            return retval;
        }

        public ITerrainChannel LoadFile(string filename, int x, int y, int fileWidth, int fileHeight, int w, int h)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "SYS.DRAWING";
        }

        protected Bitmap CreateGrayscaleBitmapFromMap(ITerrainChannel map)
        {
            Bitmap bmp = new Bitmap(map.Width, map.Height);

            int pallete = 256;

            Color[] grays = new Color[pallete];
            for (int i = 0; i < grays.Length; i++)
            {
                grays[i] = Color.FromArgb(i, i, i);
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    // 512 is the largest possible height before colours clamp
                    int colorindex = (int)(Math.Max(Math.Min(1.0, map[x, y] / 128.0), 0.0) * (pallete - 1));

                    bmp.SetPixel(x, map.Height - y - 1, grays[colorindex]);
                }
            }
            return bmp;
        }


        protected Bitmap CreateBitmapFromMap(ITerrainChannel map)
        {
            Bitmap gradientmapLd = new Bitmap("defaultstripe.png");

            int pallete = gradientmapLd.Height;

            Bitmap bmp = new Bitmap(map.Width, map.Height);
            Color[] colours = new Color[pallete];

            for (int i = 0; i < pallete; i++)
            {
                colours[i] = gradientmapLd.GetPixel(0, i);
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    // 512 is the largest possible height before colours clamp
                    int colorindex = (int)(Math.Max(Math.Min(1.0, map[x, y] / 512.0), 0.0) * (pallete - 1));
                    bmp.SetPixel(x, map.Height - y - 1, colours[colorindex]);
                }
            }
            return bmp;
        }

        public virtual void SaveFile(string filename, ITerrainChannel map)
        {
            Bitmap colours = CreateGrayscaleBitmapFromMap(map);

            colours.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
        }

        #endregion
    }
}
