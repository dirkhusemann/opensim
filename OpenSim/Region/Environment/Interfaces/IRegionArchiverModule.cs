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

using System.IO;

namespace OpenSim.Region.Environment.Interfaces
{
    /// <summary>
    /// Interface to region archive functionality
    /// </summary>
    public interface IRegionArchiverModule
    {
        /// <summary>
        /// Archive the region to the given path
        /// </summary>
        /// <param name="savePath"></param>
        void ArchiveRegion(string savePath);

        /// <summary>
        /// Archive the region to a stream.
        /// </summary>
        /// 
        /// This may be a little problematic to use right now since saves happen asynchronously and there is not yet
        /// a mechanism to signal completion to the caller (possibly other than continually checking whether the 
        /// stream has any data in it).  TODO: Address this.
        /// 
        /// <param name="saveStream"></param>
        void ArchiveRegion(Stream saveStream);

        /// <summary>
        /// Dearchive the given region archive into the scene
        /// </summary>
        /// <param name="loadPath"></param>
        void DearchiveRegion(string loadPath);
        
        /// <summary>
        /// Dearchive a region from a stream
        /// </summary>
        /// <param name="loadStream"></param>
        void DearchiveRegion(Stream loadStream);
    }
}
