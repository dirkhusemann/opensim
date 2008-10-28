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
using System.Text;
using System.IO;

namespace PrimMesher
{
    public struct Quat
    {
        /// <summary>X value</summary>
        public float X;
        /// <summary>Y value</summary>
        public float Y;
        /// <summary>Z value</summary>
        public float Z;
        /// <summary>W value</summary>
        public float W;

        public Quat(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Quat(Coord axis, float angle)
        {
            axis = axis.Normalize();

            angle *= 0.5f;
            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);

            X = axis.X * s;
            Y = axis.Y * s;
            Z = axis.Z * s;
            W = c;

            Normalize();
        }

        public float Length()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
        }

        public Quat Normalize()
        {
            const float MAG_THRESHOLD = 0.0000001f;
            float mag = Length();

            // Catch very small rounding errors when normalizing
            if (mag > MAG_THRESHOLD)
            {
                float oomag = 1f / mag;
                X *= oomag;
                Y *= oomag;
                Z *= oomag;
                W *= oomag;
            }
            else
            {
                X = 0f;
                Y = 0f;
                Z = 0f;
                W = 1f;
            }

            return this;
        }

        public override string ToString()
        {
            return "< X: " + this.X.ToString() + ", Y: " + this.Y.ToString() + ", Z: " + this.Z.ToString() + ", W: " + this.W.ToString() + ">";
        }
    }

    public struct Coord
    {
        public float X;
        public float Y;
        public float Z;

        public Coord(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public float Length()
        {
            return (float)Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
        }

        public Coord Normalize()
        {
            const float MAG_THRESHOLD = 0.0000001f;
            float mag = Length();

            // Catch very small rounding errors when normalizing
            if (mag > MAG_THRESHOLD)
            {
                float oomag = 1.0f / mag;
                this.X *= oomag;
                this.Y *= oomag;
                this.Z *= oomag;
            }
            else
            {
                this.X = 0.0f;
                this.Y = 0.0f;
                this.Z = 0.0f;
            }

            return this;
        }

        public override string ToString()
        {
            return this.X.ToString() + " " + this.Y.ToString() + " " + this.Z.ToString();
        }

        public static Coord Cross(Coord c1, Coord c2)
        {
            return new Coord(
                c1.Y * c2.Z - c2.Y * c1.Z,
                c1.Z * c2.X - c2.Z * c1.X,
                c1.X * c2.Y - c2.X * c1.Y
                );
        }

        public static Coord operator +(Coord v, Coord a)
        {
            return new Coord(v.X + a.X, v.Y + a.Y, v.Z + a.Z);
        }

        public static Coord operator *(Coord v, Coord m)
        {
            return new Coord(v.X * m.X, v.Y * m.Y, v.Z * m.Z);
        }

        public static Coord operator *(Coord v, Quat q)
        {
            // From http://www.euclideanspace.com/maths/algebra/realNormedAlgebra/quaternions/transforms/

            Coord c2 = new Coord(0.0f, 0.0f, 0.0f);

            c2.X = q.W * q.W * v.X +
                2f * q.Y * q.W * v.Z -
                2f * q.Z * q.W * v.Y +
                     q.X * q.X * v.X +
                2f * q.Y * q.X * v.Y +
                2f * q.Z * q.X * v.Z -
                     q.Z * q.Z * v.X -
                     q.Y * q.Y * v.X;

            c2.Y =
                2f * q.X * q.Y * v.X +
                     q.Y * q.Y * v.Y +
                2f * q.Z * q.Y * v.Z +
                2f * q.W * q.Z * v.X -
                     q.Z * q.Z * v.Y +
                     q.W * q.W * v.Y -
                2f * q.X * q.W * v.Z -
                     q.X * q.X * v.Y;

            c2.Z =
                2f * q.X * q.Z * v.X +
                2f * q.Y * q.Z * v.Y +
                     q.Z * q.Z * v.Z -
                2f * q.W * q.Y * v.X -
                     q.Y * q.Y * v.Z +
                2f * q.W * q.X * v.Y -
                     q.X * q.X * v.Z +
                     q.W * q.W * v.Z;

            return c2;
        }
    }

    public struct UVCoord
    {
        public float U;
        public float V;


        public UVCoord(float u, float v)
        {
            this.U = u;
            this.V = v;
        }
    }

    public struct Face
    {
        public int primFace;

        // vertices
        public int v1;
        public int v2;
        public int v3;

        //normals
        public int n1;
        public int n2;
        public int n3;

        //// UVs
        //public int uv1;
        //public int uv2;
        //public int uv3;

        public Face(int v1, int v2, int v3)
        {
            primFace = 0;

            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;

            this.n1 = 0;
            this.n2 = 0;
            this.n3 = 0;

            //this.uv1 = 0;
            //this.uv2 = 0;
            //this.uv3 = 0;
        }

        public Face(int v1, int v2, int v3, int n1, int n2, int n3)
        {
            primFace = 0;

            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;

            this.n1 = n1;
            this.n2 = n2;
            this.n3 = n3;

            //this.uv1 = 0;
            //this.uv2 = 0;
            //this.uv3 = 0;
        }
    }

    public struct ViewerFace
    {
        public int primFaceNumber;

        public Coord v1;
        public Coord v2;
        public Coord v3;

        public Coord n1;
        public Coord n2;
        public Coord n3;

        public UVCoord uv1;
        public UVCoord uv2;
        public UVCoord uv3;

        public ViewerFace(int primFaceNumber)
        {
            this.primFaceNumber = primFaceNumber;

            this.v1 = new Coord();
            this.v2 = new Coord();
            this.v3 = new Coord();

            this.n1 = new Coord();
            this.n2 = new Coord();
            this.n3 = new Coord();

            this.uv1 = new UVCoord();
            this.uv2 = new UVCoord();
            this.uv3 = new UVCoord();
        }

        public void Scale(float x, float y, float z)
        {
            this.v1.X *= x;
            this.v1.Y *= y;
            this.v1.Z *= z;

            this.v2.X *= x;
            this.v2.Y *= y;
            this.v2.Z *= z;

            this.v3.X *= x;
            this.v3.Y *= y;
            this.v3.Z *= z;
        }

        public void AddRot(Quat q)
        {
            this.v1 *= q;
            this.v2 *= q;
            this.v3 *= q;

            this.n1 *= q;
            this.n2 *= q;
            this.n3 *= q;
        }

        public void CalcSurfaceNormal()
        {

            Coord edge1 = new Coord(this.n2.X - this.n1.X, this.n2.Y - this.n1.Y, this.n2.Z - this.n1.Z);
            Coord edge2 = new Coord(this.n3.X - this.n1.X, this.n3.Y - this.n1.Y, this.n3.Z - this.n1.Z);

            this.n1 = this.n2 = this.n3 = Coord.Cross(edge1, edge2).Normalize();
        }
    }

    internal struct Angle
    {
        internal float angle;
        internal float X;
        internal float Y;

        internal Angle(float angle, float x, float y)
        {
            this.angle = angle;
            this.X = x;
            this.Y = y;
        }
    }

    internal class AngleList
    {
        private float iX, iY; // intersection point

        private Angle[] angles3 =
        {
            new Angle(0.0f, 1.0f, 0.0f),
            new Angle(0.33333333333333333f, -0.5f, 0.86602540378443871f),
            new Angle(0.66666666666666667f, -0.5f, -0.86602540378443837f),
            new Angle(1.0f, 1.0f, 0.0f)
        };

        private Angle[] angles4 =
        {
            new Angle(0.0f, 1.0f, 0.0f),
            new Angle(0.25f, 0.0f, 1.0f),
            new Angle(0.5f, -1.0f, 0.0f),
            new Angle(0.75f, 0.0f, -1.0f),
            new Angle(1.0f, 1.0f, 0.0f)
        };

        private Angle[] angles24 =
        {
            new Angle(0.0f, 1.0f, 0.0f),
            new Angle(0.041666666666666664f, 0.96592582628906831f, 0.25881904510252074f),
            new Angle(0.083333333333333329f, 0.86602540378443871f, 0.5f),
            new Angle(0.125f, 0.70710678118654757f, 0.70710678118654746f),
            new Angle(0.16666666666666667f, 0.5f, 0.8660254037844386f),
            new Angle(0.20833333333333331f, 0.25881904510252096f, 0.9659258262890682f),
            new Angle(0.25f, 0.0f, 1.0f),
            new Angle(0.29166666666666663f, -0.25881904510252063f, 0.96592582628906831f),
            new Angle(0.33333333333333333f, -0.5f, 0.86602540378443871f),
            new Angle(0.375f, -0.70710678118654746f, 0.70710678118654757f),
            new Angle(0.41666666666666663f, -0.86602540378443849f, 0.5f),
            new Angle(0.45833333333333331f, -0.9659258262890682f, 0.25881904510252102f),
            new Angle(0.5f, -1.0f, 0.0f),
            new Angle(0.54166666666666663f, -0.96592582628906842f, -0.25881904510252035f),
            new Angle(0.58333333333333326f, -0.86602540378443882f, -0.5f),
            new Angle(0.62499999999999989f, -0.70710678118654791f, -0.70710678118654713f),
            new Angle(0.66666666666666667f, -0.5f, -0.86602540378443837f),
            new Angle(0.70833333333333326f, -0.25881904510252152f, -0.96592582628906809f),
            new Angle(0.75f, 0.0f, -1.0f),
            new Angle(0.79166666666666663f, 0.2588190451025203f, -0.96592582628906842f),
            new Angle(0.83333333333333326f, 0.5f, -0.86602540378443904f),
            new Angle(0.875f, 0.70710678118654735f, -0.70710678118654768f),
            new Angle(0.91666666666666663f, 0.86602540378443837f, -0.5f),
            new Angle(0.95833333333333326f, 0.96592582628906809f, -0.25881904510252157f),
            new Angle(1.0f, 1.0f, 0.0f)
        };

        private Angle interpolatePoints(float newPoint, Angle p1, Angle p2)
        {
            float m = (newPoint - p1.angle) / (p2.angle - p1.angle);
            return new Angle(newPoint, p1.X + m * (p2.X - p1.X), p1.Y + m * (p2.Y - p1.Y));
        }

        private void intersection(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        { // ref: http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline2d/
            double denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            double uaNumerator = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3);

            if (denom != 0.0)
            {
                double ua = uaNumerator / denom;
                iX = (float)(x1 + ua * (x2 - x1));
                iY = (float)(y1 + ua * (y2 - y1));
            }
        }

        internal List<Angle> angles;

        internal void makeAngles(int sides, float startAngle, float stopAngle)
        {
            angles = new List<Angle>();
            double twoPi = System.Math.PI * 2.0;
            float twoPiInv = 1.0f / (float)twoPi;

            if (sides < 1)
                throw new Exception("number of sides not greater than zero");
            if (stopAngle <= startAngle)
                throw new Exception("stopAngle not greater than startAngle");

            if ((sides == 3 || sides == 4 || sides == 24))
            {
                startAngle *= twoPiInv;
                stopAngle *= twoPiInv;

                Angle[] sourceAngles;
                if (sides == 3)
                    sourceAngles = angles3;
                else if (sides == 4)
                    sourceAngles = angles4;
                else sourceAngles = angles24;

                int startAngleIndex = (int)(startAngle * sides);
                int endAngleIndex = sourceAngles.Length - 1;
                if (stopAngle < 1.0f)
                    endAngleIndex = (int)(stopAngle * sides) + 1;
                if (endAngleIndex == startAngleIndex)
                    endAngleIndex++;

                for (int angleIndex = startAngleIndex; angleIndex < endAngleIndex + 1; angleIndex++)
                    angles.Add(sourceAngles[angleIndex]);

                if (startAngle > 0.0f)
                    angles[0] = interpolatePoints(startAngle, angles[0], angles[1]);

                if (stopAngle < 1.0f)
                {
                    int lastAngleIndex = angles.Count - 1;
                    angles[lastAngleIndex] = interpolatePoints(stopAngle, angles[lastAngleIndex - 1], angles[lastAngleIndex]);
                }
            }
            else
            {
                double stepSize = twoPi / sides;

                int startStep = (int)(startAngle / stepSize);
                double angle = stepSize * startStep;
                int step = startStep;
                double stopAngleTest = stopAngle;
                if (stopAngle < twoPi)
                {
                    stopAngleTest = stepSize * ((int)(stopAngle / stepSize) + 1);
                    if (stopAngleTest < stopAngle)
                        stopAngleTest += stepSize;
                    if (stopAngleTest > twoPi)
                        stopAngleTest = twoPi;
                }

                while (angle <= stopAngleTest)
                {
                    Angle newAngle;
                    newAngle.angle = (float)angle;
                    newAngle.X = (float)System.Math.Cos(angle);
                    newAngle.Y = (float)System.Math.Sin(angle);
                    angles.Add(newAngle);
                    step += 1;
                    angle = stepSize * step;
                }

                if (startAngle > angles[0].angle)
                {
                    Angle newAngle;
                    intersection(angles[0].X, angles[0].Y, angles[1].X, angles[1].Y, 0.0f, 0.0f, (float)Math.Cos(startAngle), (float)Math.Sin(startAngle));
                    newAngle.angle = startAngle;
                    newAngle.X = iX;
                    newAngle.Y = iY;
                    angles[0] = newAngle;
                }

                int index = angles.Count - 1;
                if (stopAngle < angles[index].angle)
                {
                    Angle newAngle;
                    intersection(angles[index - 1].X, angles[index - 1].Y, angles[index].X, angles[index].Y, 0.0f, 0.0f, (float)Math.Cos(stopAngle), (float)Math.Sin(stopAngle));
                    newAngle.angle = stopAngle;
                    newAngle.X = iX;
                    newAngle.Y = iY;
                    angles[index] = newAngle;
                }
            }
        }
    }

    /// <summary>
    /// generates a profile for extrusion
    /// </summary>
    public class Profile
    {
        private const float twoPi = 2.0f * (float)Math.PI;

        internal List<Coord> coords;
        internal List<Face> faces;
        internal List<Coord> vertexNormals;
        internal List<float> us;
        internal List<UVCoord> faceUVs;

        internal Coord faceNormal = new Coord(0.0f, 0.0f, 1.0f);
        internal Coord cutNormal1 = new Coord();
        internal Coord cutNormal2 = new Coord();

        internal int numOuterVerts = 0;
        internal int numHollowVerts = 0;

        internal bool calcVertexNormals = false;

        internal Profile()
        {
            this.coords = new List<Coord>();
            this.faces = new List<Face>();
            this.vertexNormals = new List<Coord>();
            this.us = new List<float>();
            this.faceUVs = new List<UVCoord>();
        }

        public Profile(int sides, float profileStart, float profileEnd, float hollow, int hollowSides, bool createFaces, bool calcVertexNormals)
        {
            this.calcVertexNormals = calcVertexNormals;
            this.coords = new List<Coord>();
            this.faces = new List<Face>();
            this.vertexNormals = new List<Coord>();
            this.us = new List<float>();
            this.faceUVs = new List<UVCoord>();
            Coord center = new Coord(0.0f, 0.0f, 0.0f);

            List<Coord> hollowCoords = new List<Coord>();
            List<Coord> hollowNormals = new List<Coord>();
            List<float> hollowUs = new List<float>();

            bool hasHollow = (hollow > 0.0f);

            bool hasProfileCut = (profileStart > 0.0f || profileEnd < 1.0f);

            AngleList angles = new AngleList();
            AngleList hollowAngles = new AngleList();

            float xScale = 0.5f;
            float yScale = 0.5f;
            if (sides == 4)  // corners of a square are sqrt(2) from center
            {
                xScale = 0.707f;
                yScale = 0.707f;
            }

            float startAngle = profileStart * twoPi;
            float stopAngle = profileEnd * twoPi;

            try { angles.makeAngles(sides, startAngle, stopAngle); }
            catch (Exception ex)
            {
                Console.WriteLine("makeAngles failed: Exception: " + ex.ToString());
                Console.WriteLine("sides: " + sides.ToString() + " startAngle: " + startAngle.ToString() + " stopAngle: " + stopAngle.ToString());
                return;
            }

            this.numOuterVerts = angles.angles.Count;

            // flag to create as few triangles as possible for 3 or 4 side profile
            bool simpleFace = (sides < 5 && !(hasHollow || hasProfileCut));

            if (hasHollow)
            {
                if (sides == hollowSides)
                    hollowAngles = angles;
                else
                {
                    try { hollowAngles.makeAngles(hollowSides, startAngle, stopAngle); }
                    catch (Exception ex)
                    {
                        Console.WriteLine("makeAngles failed: Exception: " + ex.ToString());
                        Console.WriteLine("sides: " + sides.ToString() + " startAngle: " + startAngle.ToString() + " stopAngle: " + stopAngle.ToString());
                        return;
                    }
                }
                this.numHollowVerts = hollowAngles.angles.Count;
            }
            else if (!simpleFace)
            {
                this.coords.Add(center);
                if (this.calcVertexNormals)
                    this.vertexNormals.Add(new Coord(0.0f, 0.0f, 1.0f));
                this.us.Add(0.0f);
            }

            float z = 0.0f;

            Angle angle;
            Coord newVert = new Coord();
            if (hasHollow && hollowSides != sides)
            {
                int numHollowAngles = hollowAngles.angles.Count;
                for (int i = 0; i < numHollowAngles; i++)
                {
                    angle = hollowAngles.angles[i];
                    newVert.X = hollow * xScale * angle.X;
                    newVert.Y = hollow * yScale * angle.Y;
                    newVert.Z = z;

                    hollowCoords.Add(newVert);
                    if (this.calcVertexNormals)
                    {
                        hollowNormals.Add(new Coord(-angle.X, -angle.Y, 0.0f));
                        hollowUs.Add(angle.angle);
                    }
                }
            }

            int index = 0;
            int numAngles = angles.angles.Count;

            for (int i = 0; i < numAngles; i++)
            {
                angle = angles.angles[i];
                newVert.X = angle.X * xScale;
                newVert.Y = angle.Y * yScale;
                newVert.Z = z;
                this.coords.Add(newVert);
                if (this.calcVertexNormals)
                {
                    this.vertexNormals.Add(new Coord(angle.X, angle.Y, 0.0f));
                    if (sides < 5)
                    {
                        float u = angle.angle;
                        this.us.Add(u);
                    }
                    else
                        this.us.Add(angle.angle);
                }

                if (hollow > 0.0f)
                {
                    if (hollowSides == sides)
                    {
                        newVert.X *= hollow;
                        newVert.Y *= hollow;
                        newVert.Z = z;
                        hollowCoords.Add(newVert);
                        if (this.calcVertexNormals)
                        {
                            hollowNormals.Add(new Coord(-angle.X, -angle.Y, 0.0f));
                            hollowUs.Add(angle.angle);
                        }
                    }
                }
                else if (!simpleFace && createFaces && angle.angle > 0.0001f)
                {
                    Face newFace = new Face();
                    newFace.v1 = 0;
                    newFace.v2 = index;
                    newFace.v3 = index + 1;

                    this.faces.Add(newFace);
                }
                index += 1;
            }

            if (hasHollow)
            {
                hollowCoords.Reverse();
                if (this.calcVertexNormals)
                {
                    hollowNormals.Reverse();
                    hollowUs.Reverse();
                }

                if (createFaces)
                {
                    int numOuterVerts = this.coords.Count;
                    int numHollowVerts = hollowCoords.Count;
                    int numTotalVerts = numOuterVerts + numHollowVerts;

                    if (numOuterVerts == numHollowVerts)
                    {
                        Face newFace = new Face();

                        for (int coordIndex = 0; coordIndex < numOuterVerts - 1; coordIndex++)
                        {
                            newFace.v1 = coordIndex;
                            newFace.v2 = coordIndex + 1;
                            newFace.v3 = numTotalVerts - coordIndex - 1;
                            this.faces.Add(newFace);

                            newFace.v1 = coordIndex + 1;
                            newFace.v2 = numTotalVerts - coordIndex - 2;
                            newFace.v3 = numTotalVerts - coordIndex - 1;
                            this.faces.Add(newFace);
                        }
                    }
                    else
                    {
                        if (numOuterVerts < numHollowVerts)
                        {
                            Face newFace = new Face();
                            int j = 0; // j is the index for outer vertices
                            int maxJ = numOuterVerts - 1;
                            for (int i = 0; i < numHollowVerts; i++) // i is the index for inner vertices
                            {
                                if (j < maxJ)
                                    if (angles.angles[j + 1].angle - hollowAngles.angles[i].angle <= hollowAngles.angles[i].angle - angles.angles[j].angle)
                                    {
                                        newFace.v1 = numTotalVerts - i - 1;
                                        newFace.v2 = j;
                                        newFace.v3 = j + 1;

                                        this.faces.Add(newFace);
                                        j += 1;
                                    }

                                newFace.v1 = j;
                                newFace.v2 = numTotalVerts - i - 2;
                                newFace.v3 = numTotalVerts - i - 1;

                                this.faces.Add(newFace);
                            }
                        }
                        else // numHollowVerts < numOuterVerts
                        {
                            Face newFace = new Face();
                            int j = 0; // j is the index for inner vertices
                            int maxJ = numHollowVerts - 1;
                            for (int i = 0; i < numOuterVerts; i++)
                            {
                                if (j < maxJ)
                                    if (hollowAngles.angles[j + 1].angle - angles.angles[i].angle <= angles.angles[i].angle - hollowAngles.angles[j].angle)
                                    {
                                        newFace.v1 = i;
                                        newFace.v2 = numTotalVerts - j - 2;
                                        newFace.v3 = numTotalVerts - j - 1;

                                        this.faces.Add(newFace);
                                        j += 1;
                                    }

                                newFace.v1 = numTotalVerts - j - 1;
                                newFace.v2 = i;
                                newFace.v3 = i + 1;

                                this.faces.Add(newFace);
                            }
                        }
                    }
                }

                this.coords.AddRange(hollowCoords);
                if (this.calcVertexNormals)
                {
                    this.vertexNormals.AddRange(hollowNormals);
                    this.us.AddRange(hollowUs);

                }
            }

            if (simpleFace && createFaces)
            {
                if (sides == 3)
                    this.faces.Add(new Face(0, 1, 2));
                else if (sides == 4)
                {
                    this.faces.Add(new Face(0, 1, 2));
                    this.faces.Add(new Face(0, 2, 3));
                }
            }

            if (calcVertexNormals && hasProfileCut)
            {
                if (hasHollow)
                {
                    this.cutNormal1.X = -this.vertexNormals[0].Y - this.vertexNormals[this.vertexNormals.Count - 1].Y;
                    this.cutNormal1.Y = this.vertexNormals[0].X - this.vertexNormals[this.vertexNormals.Count - 1].X;

                    int lastOuterVertIndex = this.numOuterVerts - 1;
                    this.cutNormal2.X = -this.vertexNormals[lastOuterVertIndex].Y - this.vertexNormals[lastOuterVertIndex + 1].Y;
                    this.cutNormal2.Y = this.vertexNormals[lastOuterVertIndex].X - this.vertexNormals[lastOuterVertIndex + 1].X;
                }
                else
                {
                    this.cutNormal1.X = this.vertexNormals[1].Y;
                    this.cutNormal1.Y = -this.vertexNormals[1].X;

                    this.cutNormal2.X = -this.vertexNormals[this.vertexNormals.Count - 1].Y;
                    this.cutNormal2.Y = this.vertexNormals[this.vertexNormals.Count - 1].X;

                }
                this.cutNormal1.Normalize();
                this.cutNormal2.Normalize();
            }

            this.MakeFaceUVs();

            hollowCoords = null;
            hollowNormals = null;
            hollowUs = null;

        }

        public void MakeFaceUVs()
        {
            this.faceUVs = new List<UVCoord>();
            foreach (Coord c in this.coords)
                this.faceUVs.Add(new UVCoord(0.5f + c.X, 0.5f - c.Y));
        }

        public Profile Clone()
        {
            return this.Clone(true);
        }

        public Profile Clone(bool needFaces)
        {
            Profile clone = new Profile();

            clone.coords.AddRange(this.coords);
            clone.faceUVs.AddRange(this.faceUVs);

            if (needFaces)
                clone.faces.AddRange(this.faces);
            if ((clone.calcVertexNormals = this.calcVertexNormals) == true)
            {
                clone.vertexNormals.AddRange(this.vertexNormals);
                clone.faceNormal = this.faceNormal;
                clone.cutNormal1 = this.cutNormal1;
                clone.cutNormal2 = this.cutNormal2;
                clone.us.AddRange(this.us);
            }
            clone.numOuterVerts = this.numOuterVerts;
            clone.numHollowVerts = this.numHollowVerts;

            return clone;
        }

        public void AddPos(Coord v)
        {
            this.AddPos(v.X, v.Y, v.Z);
        }

        public void AddPos(float x, float y, float z)
        {
            int i;
            int numVerts = this.coords.Count;
            Coord vert;

            for (i = 0; i < numVerts; i++)
            {
                vert = this.coords[i];
                vert.X += x;
                vert.Y += y;
                vert.Z += z;
                this.coords[i] = vert;
            }
        }

        public void AddRot(Quat q)
        {
            int i;
            int numVerts = this.coords.Count;

            for (i = 0; i < numVerts; i++)
                this.coords[i] *= q;

            if (this.calcVertexNormals)
            {
                int numNormals = this.vertexNormals.Count;
                for (i = 0; i < numNormals; i++)
                    this.vertexNormals[i] *= q;

                this.faceNormal *= q;
                this.cutNormal1 *= q;
                this.cutNormal2 *= q;

            }
        }

        public void Scale(float x, float y)
        {
            int i;
            int numVerts = this.coords.Count;
            Coord vert;

            for (i = 0; i < numVerts; i++)
            {
                vert = this.coords[i];
                vert.X *= x;
                vert.Y *= y;
                this.coords[i] = vert;
            }
        }

        /// <summary>
        /// Changes order of the vertex indices and negates the center vertex normal. Does not alter vertex normals of radial vertices
        /// </summary>
        public void FlipNormals()
        {
            int i;
            int numFaces = this.faces.Count;
            Face tmpFace;
            int tmp;

            for (i = 0; i < numFaces; i++)
            {
                tmpFace = this.faces[i];
                tmp = tmpFace.v3;
                tmpFace.v3 = tmpFace.v1;
                tmpFace.v1 = tmp;
                this.faces[i] = tmpFace;
            }

            if (this.calcVertexNormals)
            {
                int normalCount = this.vertexNormals.Count;
                if (normalCount > 0)
                {
                    Coord n = this.vertexNormals[normalCount - 1];
                    n.Z = -n.Z;
                    this.vertexNormals[normalCount - 1] = n;
                }
            }

            this.faceNormal.X = -this.faceNormal.X;
            this.faceNormal.Y = -this.faceNormal.Y;
            this.faceNormal.Z = -this.faceNormal.Z;

            int numfaceUVs = this.faceUVs.Count;
            for (i = 0; i < numfaceUVs; i++)
            {
                UVCoord uv = this.faceUVs[i];
                uv.U = 1.0f - uv.U;
                this.faceUVs[i] = uv;
            }
        }

        public void AddValue2FaceVertexIndices(int num)
        {
            int numFaces = this.faces.Count;
            Face tmpFace;
            for (int i = 0; i < numFaces; i++)
            {
                tmpFace = this.faces[i];
                tmpFace.v1 += num;
                tmpFace.v2 += num;
                tmpFace.v3 += num;

                this.faces[i] = tmpFace;
            }
        }

        public void AddValue2FaceNormalIndices(int num)
        {
            if (this.calcVertexNormals)
            {
                int numFaces = this.faces.Count;
                Face tmpFace;
                for (int i = 0; i < numFaces; i++)
                {
                    tmpFace = this.faces[i];
                    tmpFace.n1 += num;
                    tmpFace.n2 += num;
                    tmpFace.n3 += num;

                    this.faces[i] = tmpFace;
                }
            }
        }

        public void DumpRaw(String path, String name, String title)
        {
            if (path == null)
                return;
            String fileName = name + "_" + title + ".raw";
            String completePath = Path.Combine(path, fileName);
            StreamWriter sw = new StreamWriter(completePath);

            for (int i = 0; i < this.faces.Count; i++)
            {
                string s = this.coords[this.faces[i].v1].ToString();
                s += " " + this.coords[this.faces[i].v2].ToString();
                s += " " + this.coords[this.faces[i].v3].ToString();

                sw.WriteLine(s);
            }

            sw.Close();
        }
    }

    public class PrimMesh
    {
        private const float twoPi = 2.0f * (float)Math.PI;

        public List<Coord> coords;
        public List<Coord> normals;
        public List<Face> faces;

        public List<ViewerFace> viewerFaces;

        private int sides = 4;
        private int hollowSides = 4;
        private float profileStart = 0.0f;
        private float profileEnd = 1.0f;
        private float hollow = 0.0f;
        public int twistBegin = 0;
        public int twistEnd = 0;
        public float topShearX = 0.0f;
        public float topShearY = 0.0f;
        public float pathCutBegin = 0.0f;
        public float pathCutEnd = 1.0f;
        public float dimpleBegin = 0.0f;
        public float dimpleEnd = 1.0f;
        public float skew = 0.0f;
        public float holeSizeX = 1.0f; // called pathScaleX in pbs
        public float holeSizeY = 0.25f;
        public float taperX = 0.0f;
        public float taperY = 0.0f;
        public float radius = 0.0f;
        public float revolutions = 1.0f;
        public int stepsPerRevolution = 24;

        private bool hasProfileCut = false;
        private bool hasHollow = false;
        public bool calcVertexNormals = false;
        private bool normalsProcessed = false;
        public bool viewerMode = false;

        public string ParamsToDisplayString()
        {
            string s = "";
            s += "sides..................: " + this.sides.ToString();
            s += "\nhollowSides..........: " + this.hollowSides.ToString();
            s += "\nprofileStart.........: " + this.profileStart.ToString();
            s += "\nprofileEnd...........: " + this.profileEnd.ToString();
            s += "\nhollow...............: " + this.hollow.ToString();
            s += "\ntwistBegin...........: " + this.twistBegin.ToString();
            s += "\ntwistEnd.............: " + this.twistEnd.ToString();
            s += "\ntopShearX............: " + this.topShearX.ToString();
            s += "\ntopShearY............: " + this.topShearY.ToString();
            s += "\npathCutBegin.........: " + this.pathCutBegin.ToString();
            s += "\npathCutEnd...........: " + this.pathCutEnd.ToString();
            s += "\ndimpleBegin..........: " + this.dimpleBegin.ToString();
            s += "\ndimpleEnd............: " + this.dimpleEnd.ToString();
            s += "\nskew.................: " + this.skew.ToString();
            s += "\nholeSizeX............: " + this.holeSizeX.ToString();
            s += "\nholeSizeY............: " + this.holeSizeY.ToString();
            s += "\ntaperX...............: " + this.taperX.ToString();
            s += "\ntaperY...............: " + this.taperY.ToString();
            s += "\nradius...............: " + this.radius.ToString();
            s += "\nrevolutions..........: " + this.revolutions.ToString();
            s += "\nstepsPerRevolution...: " + this.stepsPerRevolution.ToString();

            return s;
        }


        public PrimMesh(int sides, float profileStart, float profileEnd, float hollow, int hollowSides)
        {
            this.coords = new List<Coord>();
            this.faces = new List<Face>();

            this.sides = sides;
            this.profileStart = profileStart;
            this.profileEnd = profileEnd;
            this.hollow = hollow;
            this.hollowSides = hollowSides;

            if (sides < 3)
                this.sides = 3;
            if (hollowSides < 3)
                this.hollowSides = 3;
            if (profileStart < 0.0f)
                this.profileStart = 0.0f;
            if (profileEnd > 1.0f)
                this.profileEnd = 1.0f;
            if (profileEnd < 0.02f)
                this.profileEnd = 0.02f;
            if (profileStart >= profileEnd)
                this.profileStart = profileEnd - 0.02f;
            if (hollow > 1.0f)
                this.hollow = 1.0f;
            if (hollow < 0.0f)
                this.hollow = 0.0f;

            this.hasProfileCut = (this.profileStart > 0.0f || this.profileEnd < 1.0f);
            this.hasHollow = (this.hollow > 0.001f);
        }

        public void ExtrudeLinear()
        {
            this.coords = new List<Coord>();
            this.faces = new List<Face>();

            if (this.viewerMode)
            {
                this.viewerFaces = new List<ViewerFace>();
                this.calcVertexNormals = true;
            }

            if (this.calcVertexNormals)
                this.normals = new List<Coord>();

            int step = 0;
            int steps = 1;

            float length = this.pathCutEnd - this.pathCutBegin;
            normalsProcessed = false;

            if (this.viewerMode && this.sides == 3)
            {
                // prisms don't taper well so add some vertical resolution
                // other prims may benefit from this but just do prisms for now
                if (Math.Abs(this.taperX) > 0.01 || Math.Abs(this.taperY) > 0.01)
                    steps = (int)(steps * 4.5 * length);
            }


            float twistBegin = this.twistBegin / 360.0f * twoPi;
            float twistEnd = this.twistEnd / 360.0f * twoPi;
            float twistTotal = twistEnd - twistBegin;
            float twistTotalAbs = Math.Abs(twistTotal);
            if (twistTotalAbs > 0.01f)
                steps += (int)(twistTotalAbs * 3.66); //  dahlia's magic number

            float start = -0.5f;
            float stepSize = length / (float)steps;
            float percentOfPathMultiplier = stepSize;
            float xProfileScale = 1.0f;
            float yProfileScale = 1.0f;
            float xOffset = 0.0f;
            float yOffset = 0.0f;
            float zOffset = start;
            float xOffsetStepIncrement = this.topShearX / steps;
            float yOffsetStepIncrement = this.topShearY / steps;

            float percentOfPath = this.pathCutBegin;
            zOffset += percentOfPath;

            float hollow = this.hollow;

            // sanity checks
            float initialProfileRot = 0.0f;
            if (this.sides == 3)
            {
                if (this.hollowSides == 4)
                {
                    if (hollow > 0.7f)
                        hollow = 0.7f;
                    hollow *= 0.707f;
                }
                else hollow *= 0.5f;
            }
            else if (this.sides == 4)
            {
                initialProfileRot = 1.25f * (float)Math.PI;
                if (this.hollowSides != 4)
                    hollow *= 0.707f;
            }
            else if (this.sides == 24 && this.hollowSides == 4)
                hollow *= 1.414f;

            Profile profile = new Profile(this.sides, this.profileStart, this.profileEnd, hollow, this.hollowSides, true, calcVertexNormals);

            int cut1Vert = -1;
            int cut2Vert = -1;
            if (hasProfileCut)
            {
                cut1Vert = hasHollow ? profile.coords.Count - 1 : 0;
                cut2Vert = hasHollow ? profile.numOuterVerts - 1 : profile.numOuterVerts;
            }

            if (initialProfileRot != 0.0f)
            {
                profile.AddRot(new Quat(new Coord(0.0f, 0.0f, 1.0f), initialProfileRot));
                if (viewerMode)
                    profile.MakeFaceUVs();
            }


            Coord lastCutNormal1 = new Coord();
            Coord lastCutNormal2 = new Coord();
            float lastV = 1.0f;

            bool done = false;
            while (!done)
            {
                Profile newLayer = profile.Clone();

                if (this.taperX == 0.0f)
                    xProfileScale = 1.0f;
                else if (this.taperX > 0.0f)
                    xProfileScale = 1.0f - percentOfPath * this.taperX;
                else xProfileScale = 1.0f + (1.0f - percentOfPath) * this.taperX;

                if (this.taperY == 0.0f)
                    yProfileScale = 1.0f;
                else if (this.taperY > 0.0f)
                    yProfileScale = 1.0f - percentOfPath * this.taperY;
                else yProfileScale = 1.0f + (1.0f - percentOfPath) * this.taperY;

                if (xProfileScale != 1.0f || yProfileScale != 1.0f)
                    newLayer.Scale(xProfileScale, yProfileScale);

                float twist = twistBegin + twistTotal * percentOfPath;
                if (twist != 0.0f)
                    newLayer.AddRot(new Quat(new Coord(0.0f, 0.0f, 1.0f), twist));

                newLayer.AddPos(xOffset, yOffset, zOffset);

                if (step == 0)
                {
                    newLayer.FlipNormals();

                    // add the top faces to the viewerFaces list here
                    if (this.viewerMode)
                    {
                        Coord faceNormal = newLayer.faceNormal;
                        ViewerFace newViewerFace = new ViewerFace();
                        newViewerFace.primFaceNumber = 0;
                        foreach (Face face in newLayer.faces)
                        {
                            newViewerFace.v1 = newLayer.coords[face.v1];
                            newViewerFace.v2 = newLayer.coords[face.v2];
                            newViewerFace.v3 = newLayer.coords[face.v3];

                            newViewerFace.n1 = faceNormal;
                            newViewerFace.n2 = faceNormal;
                            newViewerFace.n3 = faceNormal;

                            newViewerFace.uv1 = newLayer.faceUVs[face.v1];
                            newViewerFace.uv2 = newLayer.faceUVs[face.v2];
                            newViewerFace.uv3 = newLayer.faceUVs[face.v3];

                            this.viewerFaces.Add(newViewerFace);
                        }

                    }
                }

                // append this layer

                int coordsLen = this.coords.Count;
                newLayer.AddValue2FaceVertexIndices(coordsLen);

                this.coords.AddRange(newLayer.coords);

                if (this.calcVertexNormals)
                {
                    newLayer.AddValue2FaceNormalIndices(this.normals.Count);
                    this.normals.AddRange(newLayer.vertexNormals);
                }

                if (percentOfPath < this.pathCutBegin + 0.01f || percentOfPath > this.pathCutEnd - 0.01f)
                    this.faces.AddRange(newLayer.faces);

                // fill faces between layers

                int numVerts = newLayer.coords.Count;
                Face newFace = new Face();

                if (step > 0)
                {
                    int startVert = coordsLen + 1;
                    int endVert = this.coords.Count;

                    if (sides < 5 || this.hasProfileCut || hollow > 0.0f)
                        startVert--;

                    for (int i = startVert; i < endVert; i++)
                    {
                        int iNext = i + 1;
                        if (i == endVert - 1)
                            iNext = startVert;

                        int whichVert = i - startVert;

                        newFace.v1 = i;
                        newFace.v2 = i - numVerts;
                        newFace.v3 = iNext - numVerts;
                        this.faces.Add(newFace);

                        newFace.v2 = iNext - numVerts;
                        newFace.v3 = iNext;
                        this.faces.Add(newFace);

                        if (this.viewerMode)
                        {
                            // add the side faces to the list of viewerFaces here
                            int primFaceNum = 1;
                            if (whichVert >= sides)
                                primFaceNum = 2;
                            ViewerFace newViewerFace1 = new ViewerFace(primFaceNum);
                            ViewerFace newViewerFace2 = new ViewerFace(primFaceNum);
                            float u1 = newLayer.us[whichVert];
                            float u2 = 1.0f;
                            if (whichVert < newLayer.us.Count - 1)
                                u2 = newLayer.us[whichVert + 1];
                            if (sides < 5 && whichVert < sides)
                            {
                                u1 *= sides;
                                u2 *= sides;
                                u1 -= whichVert;
                                u2 -= whichVert;
                                if (u2 < 0.1f)
                                    u2 = 1.0f;

                                newViewerFace2.primFaceNumber = newViewerFace1.primFaceNumber = whichVert + 1;
                            }

                            if (whichVert == cut1Vert || whichVert == cut2Vert)
                            {
                                u1 = 0.0f;
                                u2 = 1.0f;
                            }

                            newViewerFace1.uv1.U = u1;
                            newViewerFace1.uv2.U = u1;
                            newViewerFace1.uv3.U = u2;

                            newViewerFace1.uv1.V = 1.0f - percentOfPath;
                            newViewerFace1.uv2.V = lastV;
                            newViewerFace1.uv3.V = lastV;

                            newViewerFace2.uv1.U = u1;
                            newViewerFace2.uv2.U = u2;
                            newViewerFace2.uv3.U = u2;

                            newViewerFace2.uv1.V = 1.0f - percentOfPath;
                            newViewerFace2.uv2.V = lastV;
                            newViewerFace2.uv3.V = 1.0f - percentOfPath;

                            newViewerFace1.v1 = this.coords[i];
                            newViewerFace1.v2 = this.coords[i - numVerts];
                            newViewerFace1.v3 = this.coords[iNext - numVerts];

                            newViewerFace2.v1 = this.coords[i];
                            newViewerFace2.v2 = this.coords[iNext - numVerts];
                            newViewerFace2.v3 = this.coords[iNext];

                            if (whichVert == cut1Vert)
                            {  // start profile cut faces

                                newViewerFace1.n2 = newViewerFace1.n1 = lastCutNormal1;
                                newViewerFace1.n3 = newLayer.cutNormal1;

                                newViewerFace2.n3 = newViewerFace2.n1 = newLayer.cutNormal1;
                                newViewerFace2.n2 = lastCutNormal1;
                            }

                            else // periphery faces
                            {
                                if ((sides < 5 && whichVert < newLayer.numOuterVerts) || (hollowSides < 5 && whichVert >= newLayer.numOuterVerts))
                                {
                                    newViewerFace1.CalcSurfaceNormal();
                                    newViewerFace2.CalcSurfaceNormal();
                                }
                                else
                                {
                                    newViewerFace1.n1 = this.normals[i];
                                    newViewerFace1.n2 = this.normals[i - numVerts];
                                    newViewerFace1.n3 = this.normals[iNext - numVerts];

                                    newViewerFace2.n1 = this.normals[i];
                                    newViewerFace2.n2 = this.normals[iNext - numVerts];
                                    newViewerFace2.n3 = this.normals[iNext];
                                }
                            }

                            this.viewerFaces.Add(newViewerFace1);
                            this.viewerFaces.Add(newViewerFace2);

                        }
                    }

                    if (this.hasProfileCut)
                    { // add the end cut face to the list of viewerFaces here
                        // the prior cut face was filled in the above loop
                        newFace.v1 = coordsLen - 1;
                        newFace.v2 = coordsLen - numVerts;
                        newFace.v3 = coordsLen;
                        this.faces.Add(newFace);

                        newFace.v1 = coordsLen + numVerts - 1;
                        newFace.v2 = coordsLen - 1;
                        newFace.v3 = coordsLen;
                        this.faces.Add(newFace);
                    }

                }

                lastCutNormal1 = newLayer.cutNormal1;
                lastCutNormal2 = newLayer.cutNormal2;
                lastV = 1.0f - percentOfPath;

                // calc the step for the next iteration of the loop

                if (step < steps)
                {
                    step += 1;
                    percentOfPath += percentOfPathMultiplier;
                    xOffset += xOffsetStepIncrement;
                    yOffset += yOffsetStepIncrement;
                    zOffset += stepSize;
                    if (percentOfPath > this.pathCutEnd)
                        done = true;
                }
                else done = true;

                if (done && viewerMode)
                {
                    // add the top faces to the viewerFaces list here
                    Coord faceNormal = newLayer.faceNormal;
                    ViewerFace newViewerFace = new ViewerFace();
                    newViewerFace.primFaceNumber = 0;
                    foreach (Face face in newLayer.faces)
                    {
                        newViewerFace.v1 = newLayer.coords[face.v1 - coordsLen];
                        newViewerFace.v2 = newLayer.coords[face.v2 - coordsLen];
                        newViewerFace.v3 = newLayer.coords[face.v3 - coordsLen];

                        newViewerFace.n1 = faceNormal;
                        newViewerFace.n2 = faceNormal;
                        newViewerFace.n3 = faceNormal;

                        newViewerFace.uv1 = newLayer.faceUVs[face.v1 - coordsLen];
                        newViewerFace.uv2 = newLayer.faceUVs[face.v2 - coordsLen];
                        newViewerFace.uv3 = newLayer.faceUVs[face.v3 - coordsLen];

                        this.viewerFaces.Add(newViewerFace);
                    }
                }
            }
        }

        public void ExtrudeCircular()
        {
            this.coords = new List<Coord>();
            this.faces = new List<Face>();

            if (this.viewerMode)
            {
                this.viewerFaces = new List<ViewerFace>();
                this.calcVertexNormals = true;
            }

            if (this.calcVertexNormals)
                this.normals = new List<Coord>();

            int step = 0;
            int steps = 24;

            normalsProcessed = false;

            float twistBegin = this.twistBegin / 360.0f * twoPi;
            float twistEnd = this.twistEnd / 360.0f * twoPi;
            float twistTotal = twistEnd - twistBegin;

            // if the profile has a lot of twist, add more layers otherwise the layers may overlap
            // and the resulting mesh may be quite inaccurate. This method is arbitrary and doesn't
            // accurately match the viewer
            float twistTotalAbs = Math.Abs(twistTotal);
            if (twistTotalAbs > 0.01f)
            {
                if (twistTotalAbs > Math.PI * 1.5f)
                    steps *= 2;
                if (twistTotalAbs > Math.PI * 3.0f)
                    steps *= 2;
            }

            float yPathScale = this.holeSizeY * 0.5f;
            float pathLength = this.pathCutEnd - this.pathCutBegin;
            float totalSkew = this.skew * 2.0f * pathLength;
            float skewStart = this.pathCutBegin * 2.0f * this.skew - this.skew;
            float xOffsetTopShearXFactor = this.topShearX * (0.25f + 0.5f * (0.5f - this.holeSizeY));
            float yShearCompensation = 1.0f + Math.Abs(this.topShearY) * 0.25f;

            // It's not quite clear what pushY (Y top shear) does, but subtracting it from the start and end
            // angles appears to approximate it's effects on path cut. Likewise, adding it to the angle used
            // to calculate the sine for generating the path radius appears to approximate it's effects there
            // too, but there are some subtle differences in the radius which are noticeable as the prim size
            // increases and it may affect megaprims quite a bit. The effect of the Y top shear parameter on
            // the meshes generated with this technique appear nearly identical in shape to the same prims when
            // displayed by the viewer.

            float startAngle = (twoPi * this.pathCutBegin * this.revolutions) - this.topShearY * 0.9f;
            float endAngle = (twoPi * this.pathCutEnd * this.revolutions) - this.topShearY * 0.9f;
            float stepSize = twoPi / this.stepsPerRevolution;

            step = (int)(startAngle / stepSize);
            int firstStep = step;
            float angle = startAngle;
            float hollow = this.hollow;

            // sanity checks
            float initialProfileRot = 0.0f;
            if (this.sides == 3)
            {
                initialProfileRot = (float)Math.PI;
                if (this.hollowSides == 4)
                {
                    if (hollow > 0.7f)
                        hollow = 0.7f;
                    hollow *= 0.707f;
                }
                else hollow *= 0.5f;
            }
            else if (this.sides == 4)
            {
                initialProfileRot = 0.25f * (float)Math.PI;
                if (this.hollowSides != 4)
                    hollow *= 0.707f;
            }
            else if (this.sides > 4)
            {
                initialProfileRot = (float)Math.PI;
                if (this.hollowSides == 4)
                {
                    if (hollow > 0.7f)
                        hollow = 0.7f;
                    hollow /= 0.7f;
                }
            }

            bool needEndFaces = false;
            if (this.pathCutBegin != 0.0f || this.pathCutEnd != 1.0f)
                needEndFaces = true;
            else if (this.taperX != 0.0f || this.taperY != 0.0f)
                needEndFaces = true;
            else if (this.skew != 0.0f)
                needEndFaces = true;
            else if (twistTotal != 0.0f)
                needEndFaces = true;
            else if (this.radius != 0.0f)
                needEndFaces = true;

            Profile profile = new Profile(this.sides, this.profileStart, this.profileEnd, hollow, this.hollowSides, needEndFaces, calcVertexNormals);

            if (initialProfileRot != 0.0f)
            {
                profile.AddRot(new Quat(new Coord(0.0f, 0.0f, 1.0f), initialProfileRot));
                if (viewerMode)
                    profile.MakeFaceUVs();
            }

            Coord lastCutNormal1 = new Coord();
            Coord lastCutNormal2 = new Coord();
            float lastV = 1.0f;

            bool done = false;
            while (!done) // loop through the length of the path and add the layers
            {
                bool isEndLayer = false;
                if (angle <= startAngle + .01f || angle >= endAngle - .01f)
                    isEndLayer = true;

                //Profile newLayer = profile.Clone(isEndLayer && needEndFaces);
                Profile newLayer = profile.Clone();

                float xProfileScale = (1.0f - Math.Abs(this.skew)) * this.holeSizeX;
                float yProfileScale = this.holeSizeY;

                float percentOfPath = angle / (twoPi * this.revolutions);
                float percentOfAngles = (angle - startAngle) / (endAngle - startAngle);

                if (this.taperX > 0.01f)
                    xProfileScale *= 1.0f - percentOfPath * this.taperX;
                else if (this.taperX < -0.01f)
                    xProfileScale *= 1.0f + (1.0f - percentOfPath) * this.taperX;

                if (this.taperY > 0.01f)
                    yProfileScale *= 1.0f - percentOfPath * this.taperY;
                else if (this.taperY < -0.01f)
                    yProfileScale *= 1.0f + (1.0f - percentOfPath) * this.taperY;

                if (xProfileScale != 1.0f || yProfileScale != 1.0f)
                    newLayer.Scale(xProfileScale, yProfileScale);

                float radiusScale = 1.0f;
                if (this.radius > 0.001f)
                    radiusScale = 1.0f - this.radius * percentOfPath;
                else if (this.radius < 0.001f)
                    radiusScale = 1.0f + this.radius * (1.0f - percentOfPath);

                float twist = twistBegin + twistTotal * percentOfPath;

                float xOffset = 0.5f * (skewStart + totalSkew * percentOfAngles);
                xOffset += (float)Math.Sin(angle) * xOffsetTopShearXFactor;

                float yOffset = yShearCompensation * (float)Math.Cos(angle) * (0.5f - yPathScale) * radiusScale;

                float zOffset = (float)Math.Sin(angle + this.topShearY) * (0.5f - yPathScale) * radiusScale;

                // next apply twist rotation to the profile layer
                if (twistTotal != 0.0f || twistBegin != 0.0f)
                    newLayer.AddRot(new Quat(new Coord(0.0f, 0.0f, 1.0f), twist));

                // now orient the rotation of the profile layer relative to it's position on the path
                // adding taperY to the angle used to generate the quat appears to approximate the viewer
                //newLayer.AddRot(new Quaternion(new Vertex(1.0f, 0.0f, 0.0f), angle + this.topShearY * 0.9f));
                newLayer.AddRot(new Quat(new Coord(1.0f, 0.0f, 0.0f), angle + this.topShearY));
                newLayer.AddPos(xOffset, yOffset, zOffset);

                if (isEndLayer && angle <= startAngle + .01f)
                {
                    newLayer.FlipNormals();

                    // add the top faces to the viewerFaces list here
                    if (this.viewerMode && needEndFaces)
                    {
                        Coord faceNormal = newLayer.faceNormal;
                        ViewerFace newViewerFace = new ViewerFace();
                        newViewerFace.primFaceNumber = 0;
                        foreach (Face face in newLayer.faces)
                        {
                            newViewerFace.v1 = newLayer.coords[face.v1];
                            newViewerFace.v2 = newLayer.coords[face.v2];
                            newViewerFace.v3 = newLayer.coords[face.v3];

                            newViewerFace.n1 = faceNormal;
                            newViewerFace.n2 = faceNormal;
                            newViewerFace.n3 = faceNormal;

                            newViewerFace.uv1 = newLayer.faceUVs[face.v1];
                            newViewerFace.uv2 = newLayer.faceUVs[face.v2];
                            newViewerFace.uv3 = newLayer.faceUVs[face.v3];

                            this.viewerFaces.Add(newViewerFace);
                        }

                    }
                }

                // append the layer and fill in the sides

                int coordsLen = this.coords.Count;
                newLayer.AddValue2FaceVertexIndices(coordsLen);

                this.coords.AddRange(newLayer.coords);

                if (this.calcVertexNormals)
                {
                    newLayer.AddValue2FaceNormalIndices(this.normals.Count);
                    this.normals.AddRange(newLayer.vertexNormals);
                }

                if (isEndLayer)
                    this.faces.AddRange(newLayer.faces);

                // fill faces between layers

                int numVerts = newLayer.coords.Count;
                Face newFace = new Face();
                if (step > firstStep)
                {
                    int startVert = coordsLen + 1;
                    int endVert = this.coords.Count;

                    if (sides < 5 || this.hasProfileCut || hollow > 0.0f)
                        startVert--;

                    for (int i = startVert; i < endVert; i++)
                    {
                        int iNext = i + 1;
                        if (i == endVert - 1)
                            iNext = startVert;

                        int whichVert = i - startVert;

                        newFace.v1 = i;
                        newFace.v2 = i - numVerts;
                        newFace.v3 = iNext - numVerts;
                        this.faces.Add(newFace);

                        newFace.v2 = iNext - numVerts;
                        newFace.v3 = iNext;
                        this.faces.Add(newFace);

                        if (this.viewerMode)
                        {
                            // add the side faces to the list of viewerFaces here
                            ViewerFace newViewerFace1 = new ViewerFace();
                            ViewerFace newViewerFace2 = new ViewerFace();
                            float u1 = newLayer.us[whichVert];
                            float u2 = 1.0f;
                            if (whichVert < newLayer.us.Count - 1)
                                u2 = newLayer.us[whichVert + 1];

                            newViewerFace1.uv1.U = u1;
                            newViewerFace1.uv2.U = u1;
                            newViewerFace1.uv3.U = u2;

                            newViewerFace1.uv1.V = 1.0f - percentOfPath;
                            newViewerFace1.uv2.V = lastV;
                            newViewerFace1.uv3.V = lastV;

                            newViewerFace2.uv1.U = u1;
                            newViewerFace2.uv2.U = u2;
                            newViewerFace2.uv3.U = u2;

                            newViewerFace2.uv1.V = 1.0f - percentOfPath;
                            newViewerFace2.uv2.V = lastV;
                            newViewerFace2.uv3.V = 1.0f - percentOfPath;

                            newViewerFace1.v1 = this.coords[i];
                            newViewerFace1.v2 = this.coords[i - numVerts];
                            newViewerFace1.v3 = this.coords[iNext - numVerts];

                            newViewerFace2.v1 = this.coords[i];
                            newViewerFace2.v2 = this.coords[iNext - numVerts];
                            newViewerFace2.v3 = this.coords[iNext];

                            if (whichVert == newLayer.numOuterVerts - 1 && hasProfileCut)
                            {  // start profile cut faces

                                newViewerFace1.n2 = newViewerFace1.n1 = lastCutNormal1;
                                newViewerFace1.n3 = newLayer.cutNormal1;

                                newViewerFace2.n3 = newViewerFace2.n1 = newLayer.cutNormal1;
                                newViewerFace2.n2 = lastCutNormal1;
                            }

                            else // periphery faces
                            {
                                if ((sides < 5 && whichVert < newLayer.numOuterVerts) || (hollowSides < 5 && whichVert >= newLayer.numOuterVerts))
                                {
                                    newViewerFace1.CalcSurfaceNormal();
                                    newViewerFace2.CalcSurfaceNormal();
                                }
                                else
                                {
                                    newViewerFace1.n1 = this.normals[i];
                                    newViewerFace1.n2 = this.normals[i - numVerts];
                                    newViewerFace1.n3 = this.normals[iNext - numVerts];

                                    newViewerFace2.n1 = this.normals[i];
                                    newViewerFace2.n2 = this.normals[iNext - numVerts];
                                    newViewerFace2.n3 = this.normals[iNext];
                                }
                            }

                            this.viewerFaces.Add(newViewerFace1);
                            this.viewerFaces.Add(newViewerFace2);

                        }
                    }
                }

                lastCutNormal1 = newLayer.cutNormal1;
                lastCutNormal2 = newLayer.cutNormal2;
                lastV = 1.0f - percentOfPath;

                // calculate terms for next iteration
                // calculate the angle for the next iteration of the loop

                if (angle >= endAngle)
                    done = true;
                else
                {
                    step += 1;
                    angle = stepSize * step;
                    if (angle > endAngle)
                        angle = endAngle;
                }

                if (done && viewerMode && needEndFaces)
                {
                    // add the bottom faces to the viewerFaces list here
                    Coord faceNormal = newLayer.faceNormal;
                    ViewerFace newViewerFace = new ViewerFace();
                    newViewerFace.primFaceNumber = 0;
                    foreach (Face face in newLayer.faces)
                    {
                        newViewerFace.v1 = newLayer.coords[face.v1 - coordsLen];
                        newViewerFace.v2 = newLayer.coords[face.v2 - coordsLen];
                        newViewerFace.v3 = newLayer.coords[face.v3 - coordsLen];

                        newViewerFace.n1 = faceNormal;
                        newViewerFace.n2 = faceNormal;
                        newViewerFace.n3 = faceNormal;

                        newViewerFace.uv1 = newLayer.faceUVs[face.v1 - coordsLen];
                        newViewerFace.uv2 = newLayer.faceUVs[face.v2 - coordsLen];
                        newViewerFace.uv3 = newLayer.faceUVs[face.v3 - coordsLen];

                        this.viewerFaces.Add(newViewerFace);
                    }
                }
            }
        }

        private Coord SurfaceNormal(Face face)
        {
            Coord c1 = coords[face.v1];
            Coord c2 = coords[face.v2];
            Coord c3 = coords[face.v3];

            Coord edge1 = new Coord(c2.X - c1.X, c2.Y - c1.Y, c2.Z - c1.Z);
            Coord edge2 = new Coord(c3.X - c1.X, c3.Y - c1.Y, c3.Z - c1.Z);

            Coord normal = Coord.Cross(edge1, edge2);

            normal.Normalize();

            return normal;
        }

        public Coord SurfaceNormal(int faceIndex)
        {
            int numFaces = faces.Count;
            if (faceIndex < 0 || faceIndex >= numFaces)
                throw new Exception("faceIndex out of range");

            //return new Coord(0.0f, 0.0f, 0.0f);

            return SurfaceNormal(faces[faceIndex]);
        }

        public void CalcNormals()
        {
            if (normalsProcessed)
                return;

            normalsProcessed = true;

            int numFaces = faces.Count;

            if (!this.calcVertexNormals)
                this.normals = new List<Coord>();

            for (int i = 0; i < numFaces; i++)
            {
                Face face = faces[i];

                this.normals.Add(SurfaceNormal(i).Normalize());

                int normIndex = normals.Count - 1;
                face.n1 = normIndex;
                face.n2 = normIndex;
                face.n3 = normIndex;

                this.faces[i] = face;
            }
        }

        public void AddPos(float x, float y, float z)
        {
            int i;
            int numVerts = this.coords.Count;
            Coord vert;

            for (i = 0; i < numVerts; i++)
            {
                vert = this.coords[i];
                vert.X += x;
                vert.Y += y;
                vert.Z += z;
                this.coords[i] = vert;
            }
        }

        public void AddRot(Quat q)
        {
            Console.WriteLine("AddRot(" + q.ToString() + ")");
            int i;
            int numVerts = this.coords.Count;

            for (i = 0; i < numVerts; i++)
                this.coords[i] *= q;

            if (this.normals != null)
            {
                int numNormals = this.normals.Count;
                for (i = 0; i < numNormals; i++)
                    this.normals[i] *= q;
            }

            if (this.viewerFaces != null)
            {
                int numViewerFaces = this.viewerFaces.Count;

                for (i = 0; i < numViewerFaces; i++)
                {
                    ViewerFace v = this.viewerFaces[i];
                    v.v1 *= q;
                    v.v2 *= q;
                    v.v3 *= q;

                    v.n1 *= q;
                    v.n2 *= q;
                    v.n3 *= q;
                    this.viewerFaces[i] = v;
                }
            }

        }

        public void Scale(float x, float y, float z)
        {
            int i;
            int numVerts = this.coords.Count;
            //Coord vert;

            Coord m = new Coord(x, y, z);
            for (i = 0; i < numVerts; i++)
                this.coords[i] *= m;
            //{
            //    vert = this.coords[i];
            //    vert.X *= x;
            //    vert.Y *= y;
            //    vert.Z *= z;
            //    this.coords[i] = vert;
            //}

            if (this.viewerFaces != null)
            {
                int numViewerFaces = this.viewerFaces.Count;
                for (i = 0; i < numViewerFaces; i++)
                {
                    ViewerFace v = this.viewerFaces[i];
                    v.v1 *= m;
                    v.v2 *= m;
                    v.v3 *= m;
                    this.viewerFaces[i] = v;
                }

            }

        }

        public void DumpRaw(String path, String name, String title)
        {
            if (path == null)
                return;
            String fileName = name + "_" + title + ".raw";
            String completePath = Path.Combine(path, fileName);
            StreamWriter sw = new StreamWriter(completePath);

            for (int i = 0; i < this.faces.Count; i++)
            {
                string s = this.coords[this.faces[i].v1].ToString();
                s += " " + this.coords[this.faces[i].v2].ToString();
                s += " " + this.coords[this.faces[i].v3].ToString();

                sw.WriteLine(s);
            }

            sw.Close();
        }
    }
}
