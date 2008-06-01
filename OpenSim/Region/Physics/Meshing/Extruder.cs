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
//#define SPAM

using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Physics.Meshing
{
    internal class Extruder
    {
        public float startParameter;
        public float stopParameter;
        public PhysicsVector size;

        public float taperTopFactorX = 1f;
        public float taperTopFactorY = 1f;
        public float taperBotFactorX = 1f;
        public float taperBotFactorY = 1f;

        public float pushX = 0f;
        public float pushY = 0f;

        // twist amount in radians.  NOT DEGREES.
        public float twistTop = 0;
        public float twistBot = 0;
        public float twistMid = 0;
        public float pathScaleX = 1.0f;
        public float pathScaleY = 0.5f;
        public float skew = 0.0f;
        public float radius = 0.0f;
        public float revolutions = 1.0f;

        public float pathCutBegin = 0.0f;
        public float pathCutEnd = 1.0f;

        public ushort pathBegin = 0;
        public ushort pathEnd = 0;

        public float pathTaperX = 0.0f;
        public float pathTaperY = 0.0f;

        public Mesh Extrude(Mesh m)
        {
            startParameter = float.MinValue;
            stopParameter = float.MaxValue;
            // Currently only works for iSteps=1;
            Mesh result = new Mesh();

            Mesh workingPlus = m.Clone();
            Mesh workingMiddle = m.Clone();
            Mesh workingMinus = m.Clone();

            Quaternion tt = new Quaternion();
            Vertex v2 = new Vertex(0, 0, 0);

            foreach (Vertex v in workingPlus.vertices)
            {
                if (v == null)
                    continue;

                // This is the top
                // Set the Z + .5 to match the rest of the scale of the mesh
                // Scale it by Size, and Taper the scaling
                v.Z = +.5f;
                v.X *= (size.X * taperTopFactorX);
                v.Y *= (size.Y * taperTopFactorY);
                v.Z *= size.Z;
                
                //Push the top of the object over by the Top Shear amount
                v.X += pushX * size.X;
                v.Y += pushY * size.Y;

                if (twistTop != 0)
                {
                    // twist and shout
                    tt = new Quaternion(new Vertex(0, 0, 1), twistTop);
                    v2 = v * tt;
                    v.X = v2.X;
                    v.Y = v2.Y;
                    v.Z = v2.Z;
                }
            }

            foreach (Vertex v in workingMiddle.vertices)
            {
                if (v == null)
                    continue;

                // This is the top
                // Set the Z + .5 to match the rest of the scale of the mesh
                // Scale it by Size, and Taper the scaling
                v.Z *= size.Z;
                v.X *= (size.X * ((taperTopFactorX + taperBotFactorX) /2));
                v.Y *= (size.Y * ((taperTopFactorY + taperBotFactorY) / 2));

                v.X += (pushX / 2) * size.X;
                v.Y += (pushY / 2) * size.Y;
                //Push the top of the object over by the Top Shear amount
                if (twistMid != 0)
                {
                    // twist and shout
                    tt = new Quaternion(new Vertex(0, 0, 1), twistMid);
                    v2 = v * tt;
                    v.X = v2.X;
                    v.Y = v2.Y;
                    v.Z = v2.Z;
                }
            }

            foreach (Vertex v in workingMinus.vertices)
            {
                if (v == null)
                    continue;

                // This is the bottom
                v.Z = -.5f;
                v.X *= (size.X * taperBotFactorX);
                v.Y *= (size.Y * taperBotFactorY);
                v.Z *= size.Z;

                if (twistBot != 0)
                {
                    // twist and shout
                    tt = new Quaternion(new Vertex(0, 0, 1), twistBot);
                    v2 = v * tt;
                    v.X = v2.X;
                    v.Y = v2.Y;
                    v.Z = v2.Z;
                }
            }

            foreach (Triangle t in workingMinus.triangles)
            {
                t.invertNormal();
            }

            result.Append(workingMinus);
            result.Append(workingMiddle);

            int iLastNull = 0;

            for (int i = 0; i < workingMiddle.vertices.Count; i++)
            {
                int iNext = (i + 1);

                if (workingMiddle.vertices[i] == null) // Can't make a simplex here
                {
                    iLastNull = i + 1;
                    continue;
                }

                if (i == workingMiddle.vertices.Count - 1) // End of list
                {
                    iNext = iLastNull;
                }

                if (workingMiddle.vertices[iNext] == null) // Null means wrap to begin of last segment
                {
                    iNext = iLastNull;
                }

                Triangle tSide;
                tSide = new Triangle(workingMiddle.vertices[i], workingMinus.vertices[i], workingMiddle.vertices[iNext]);
                result.Add(tSide);

                tSide =
                    new Triangle(workingMiddle.vertices[iNext], workingMinus.vertices[i], workingMinus.vertices[iNext]);
                result.Add(tSide);
            }
            //foreach (Triangle t in workingPlus.triangles)
            //{
                //t.invertNormal();
           // }
            result.Append(workingPlus);

            iLastNull = 0;
            for (int i = 0; i < workingPlus.vertices.Count; i++)
            {
                int iNext = (i + 1);

                if (workingPlus.vertices[i] == null) // Can't make a simplex here
                {
                    iLastNull = i + 1;
                    continue;
                }

                if (i == workingPlus.vertices.Count - 1) // End of list
                {
                    iNext = iLastNull;
                }

                if (workingPlus.vertices[iNext] == null) // Null means wrap to begin of last segment
                {
                    iNext = iLastNull;
                }

                Triangle tSide;
                tSide = new Triangle(workingPlus.vertices[i], workingMiddle.vertices[i], workingPlus.vertices[iNext]);
                result.Add(tSide);

                tSide =
                    new Triangle(workingPlus.vertices[iNext], workingMiddle.vertices[i], workingMiddle.vertices[iNext]);
                result.Add(tSide);
            }

            if (twistMid != 0)
            {
                foreach (Vertex v in result.vertices)
                {
                    // twist and shout
                    if (v != null)
                    {
                        tt = new Quaternion(new Vertex(0, 0, -1), twistMid*2);
                        v2 = v * tt;
                        v.X = v2.X;
                        v.Y = v2.Y;
                        v.Z = v2.Z;
                    }
                }
            }
            return result;
        }

        public Mesh ExtrudeCircularPath(Mesh m)
        {
            Mesh result = new Mesh();

            Quaternion tt = new Quaternion();
            Vertex v2 = new Vertex(0, 0, 0);

            Mesh newLayer;
            Mesh lastLayer = null;

            //int start = 0;
            int step;
            int steps = 24;

            float twistTotal = twistTop - twistBot;
            if (System.Math.Abs(twistTotal) > (float)System.Math.PI * 1.5) steps *= 2;
            if (System.Math.Abs(twistTotal) > (float)System.Math.PI * 3.0) steps *= 2;

            double percentOfPathMultiplier = 1.0 / steps;
            double angleStepMultiplier = System.Math.PI * 2.0 / steps;

            float yPathScale = pathScaleY * 0.5f;
            float pathLength = pathCutEnd - pathCutBegin;
            float totalSkew = skew * 2.0f * pathLength;
            float skewStart = (-skew) + pathCutBegin * 2.0f * skew;


            float startAngle = (float)(System.Math.PI * 2.0 * pathCutBegin * revolutions);
            float endAngle = (float)(System.Math.PI * 2.0 * pathCutEnd * revolutions);
            float stepSize = (float)0.2617993878; // 2*PI / 24 segments per revolution
            step = (int)(startAngle / stepSize);
            float angle = startAngle;

            float xProfileScale = 1.0f;
            float yProfileScale = 1.0f;

#if SPAM
            System.Console.WriteLine("Extruder: twistTop: " + twistTop.ToString() + " twistbot: " + twistBot.ToString() + " twisttotal: " + twistTotal.ToString());
            System.Console.WriteLine("Extruder: startAngle: " + startAngle.ToString() + " endAngle: " + endAngle.ToString() + " step: " + step.ToString());
            System.Console.WriteLine("Extruder: taperBotFactorX: " + taperBotFactorX.ToString() + " taperBotFactorY: " + taperBotFactorY.ToString()
                + " taperTopFactorX: " + taperTopFactorX.ToString() + " taperTopFactorY: " + taperTopFactorY.ToString());
            System.Console.WriteLine("Extruder: PathScaleX: " + pathScaleX.ToString() + " pathScaleY: " + pathScaleY.ToString());
#endif
            

            bool done = false;
            do
            {
                float percentOfPath = 1.0f;

                percentOfPath = (angle - startAngle) / (endAngle - startAngle); // endAngle should always be larger than startAngle

                if (pathTaperX > 0.001f) // can't really compare to 0.0f as the value passed is never exactly zero
                    xProfileScale = 1.0f - percentOfPath * pathTaperX;
                else if (pathTaperX < -0.001f)
                    xProfileScale = 1.0f + (1.0f - percentOfPath) * pathTaperX;
                else xProfileScale = 1.0f;

                if (pathTaperY > 0.001f)
                    yProfileScale = 1.0f - percentOfPath * pathTaperY;
                else if (pathTaperY < -0.001f)
                    yProfileScale = 1.0f + (1.0f - percentOfPath) * pathTaperY;
                else yProfileScale = 1.0f;

                float radiusScale;

                if (radius > 0.001f)
                    radiusScale = 1.0f - radius * percentOfPath;
                else if (radius < 0.001f)
                    radiusScale = 1.0f + radius * (1.0f - percentOfPath);
                else radiusScale = 1.0f;

                //radiusScale = 1.0f;

#if SPAM
                System.Console.WriteLine("Extruder: angle: " + angle.ToString() + " percentOfPath: " + percentOfPath.ToString()
                    + " radius: " + radius.ToString() + " radiusScale: " + radiusScale.ToString());
#endif

                float twist = twistBot + (twistTotal * (float)percentOfPath);

                float zOffset = (float)(System.Math.Sin(angle) * (0.5f - yPathScale)) * radiusScale;
                float yOffset = (float)(System.Math.Cos(angle) * (0.5f - yPathScale)) * radiusScale;
                float xOffset = 0.5f * (skewStart + totalSkew * (float)percentOfPath);

                newLayer = m.Clone();

                Vertex vTemp = new Vertex(0.0f, 0.0f, 0.0f);

                if (twistTotal != 0.0f || twistBot != 0.0f)
                {
                    Quaternion profileRot = new Quaternion(new Vertex(0.0f, 0.0f, -1.0f), twist);
                    foreach (Vertex v in newLayer.vertices)
                    {
                        if (v != null)
                        {
                            vTemp = v * profileRot;
                            v.X = vTemp.X;
                            v.Y = vTemp.Y;
                            v.Z = vTemp.Z;
                        }
                    }
                }

                Quaternion layerRot = new Quaternion(new Vertex(-1.0f, 0.0f, 0.0f), (float)angle);
                foreach (Vertex v in newLayer.vertices)
                {
                    if (v != null)
                    {
                        vTemp = v * layerRot;
                        v.X = xProfileScale * vTemp.X + xOffset;
                        v.Y = yProfileScale * vTemp.Y + yOffset;
                        v.Z = vTemp.Z + zOffset;
                    }
                }

                if (angle == startAngle) // last layer, invert normals
                    foreach (Triangle t in newLayer.triangles)
                    {
                        t.invertNormal();
                    }

                result.Append(newLayer);

                int iLastNull = 0;

                if (lastLayer != null)
                {
                    int i, count = newLayer.vertices.Count;

                    for (i = 0; i < count; i++)
                    {
                        int iNext = (i + 1);

                        if (lastLayer.vertices[i] == null) // cant make a simplex here
                            iLastNull = i + 1;
                        else
                        {
                            if (i == count - 1) // End of list
                                iNext = iLastNull;

                            if (lastLayer.vertices[iNext] == null) // Null means wrap to begin of last segment
                                iNext = iLastNull;

                            result.Add(new Triangle(newLayer.vertices[i], lastLayer.vertices[i], newLayer.vertices[iNext]));
                            result.Add(new Triangle(newLayer.vertices[iNext], lastLayer.vertices[i], lastLayer.vertices[iNext]));
                        }
                    }
                }
                lastLayer = newLayer;


                // calc the angle for the next interation of the loop
                if (angle >= endAngle)
                    done = true;
                else
                {
                    angle = stepSize * ++step;
                    if (angle > endAngle)
                        angle = endAngle;
                }

            } while (!done);

            // scale the mesh to the desired size
            float xScale = size.X;
            float yScale = size.Y;
            float zScale = size.Z;

            foreach (Vertex v in result.vertices)
            {
                if (v != null)
                {
                    v.X *= xScale;
                    v.Y *= yScale;
                    v.Z *= zScale;
                }
            }

            return result;
        }
    }
}
