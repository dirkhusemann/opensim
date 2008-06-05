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

using System.Collections.Generic;
using NUnit.Framework;
using OpenSim.Tests.Common;
using OpenSim.Region.ScriptEngine.Common;

namespace OpenSim.Region.ScriptEngine.Common.Tests
{
    [TestFixture]
    public class LSL_TypesTestLSLString
    {
        /// <summary>
        /// Tests constructing a LSLString from an LSLFloat.
        /// </summary>
        [Test]
        public void TestConstructFromLSLFloat()
        {
            // The numbers we test for.
            Dictionary<double, string> numberSet = new Dictionary<double, string>();
            numberSet.Add(2, "2.000000");
            numberSet.Add(-2, "-2.000000");
            numberSet.Add(0, "0.000000");
            numberSet.Add(1, "1.000000");
            numberSet.Add(-1, "-1.000000");
            numberSet.Add(999999999, "999999999.000000");
            numberSet.Add(-99999999, "-99999999.000000");
            numberSet.Add(0.5, "0.500000");
            numberSet.Add(0.0005, "0.000500");
            numberSet.Add(0.6805, "0.680500");
            numberSet.Add(-0.5, "-0.500000");
            numberSet.Add(-0.0005, "-0.000500");
            numberSet.Add(-0.6805, "-0.680500");
            numberSet.Add(548.5, "548.500000");
            numberSet.Add(2.0005, "2.000500");
            numberSet.Add(349485435.6805, "349485435.680500");
            numberSet.Add(-548.5, "-548.500000");
            numberSet.Add(-2.0005, "-2.000500");
            numberSet.Add(-349485435.6805, "-349485435.680500");

            LSL_Types.LSLString testString;

            foreach(KeyValuePair<double, string> number in numberSet)
            {
                testString = new LSL_Types.LSLString(new LSL_Types.LSLFloat(number.Key));
                Assert.AreEqual(number.Value, testString.m_string);
            }
        }

        /// <summary>
        /// Tests constructing a LSLString from an LSLFloat.
        /// </summary>
        [Test]
        public void TestExplicitCastLSLFloatToLSLString()
        {
            // The numbers we test for.
            Dictionary<double, string> numberSet = new Dictionary<double, string>();
            numberSet.Add(2, "2.000000");
            numberSet.Add(-2, "-2.000000");
            numberSet.Add(0, "0.000000");
            numberSet.Add(1, "1.000000");
            numberSet.Add(-1, "-1.000000");
            numberSet.Add(999999999, "999999999.000000");
            numberSet.Add(-99999999, "-99999999.000000");
            numberSet.Add(0.5, "0.500000");
            numberSet.Add(0.0005, "0.000500");
            numberSet.Add(0.6805, "0.680500");
            numberSet.Add(-0.5, "-0.500000");
            numberSet.Add(-0.0005, "-0.000500");
            numberSet.Add(-0.6805, "-0.680500");
            numberSet.Add(548.5, "548.500000");
            numberSet.Add(2.0005, "2.000500");
            numberSet.Add(349485435.6805, "349485435.680500");
            numberSet.Add(-548.5, "-548.500000");
            numberSet.Add(-2.0005, "-2.000500");
            numberSet.Add(-349485435.6805, "-349485435.680500");

            LSL_Types.LSLString testString;

            foreach(KeyValuePair<double, string> number in numberSet)
            {
                testString = (LSL_Types.LSLString) new LSL_Types.LSLFloat(number.Key);
                Assert.AreEqual(number.Value, testString.m_string);
            }
        }
    }
}
