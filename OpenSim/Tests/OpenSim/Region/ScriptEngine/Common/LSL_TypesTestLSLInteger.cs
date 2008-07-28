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
    public class LSL_TypesTestLSLInteger
    {
        private Dictionary<double, int> m_doubleIntSet;

        /// <summary>
        /// Sets up dictionaries and arrays used in the tests.
        /// </summary>
        [TestFixtureSetUp]
        public void SetUpDataSets()
        {
            m_doubleIntSet = new Dictionary<double, int>();
            m_doubleIntSet.Add(2.0, 2);
            m_doubleIntSet.Add(-2.0, -2);
            m_doubleIntSet.Add(0.0, 0);
            m_doubleIntSet.Add(1.0, 1);
            m_doubleIntSet.Add(-1.0, -1);
            m_doubleIntSet.Add(999999999.0, 999999999);
            m_doubleIntSet.Add(-99999999.0, -99999999);
        }

        /// <summary>
        /// Tests LSLInteger is correctly cast explicitly to LSLFloat.
        /// </summary>
        [Test]
        public void TestExplicitCastLSLFloatToLSLInteger()
        {
            LSL_Types.LSLInteger testInteger;

            foreach (KeyValuePair<double, int> number in m_doubleIntSet)
            {
                testInteger = (LSL_Types.LSLInteger) new LSL_Types.LSLFloat(number.Key);
                Assert.AreEqual(testInteger.value, number.Value);
            }
        }
    }
}
