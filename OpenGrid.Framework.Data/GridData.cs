using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGrid.Framework.Data
{
    public enum DataResponse
    {
        RESPONSE_OK,
        RESPONSE_AUTHREQUIRED,
        RESPONSE_INVALIDCREDENTIALS,
        RESPONSE_ERROR
    }

    /// <summary>
    /// A standard grid interface
    /// </summary>
    public interface IGridData
    {
        /// <summary>
        /// Returns a sim profile from a regionHandle
        /// </summary>
        /// <param name="regionHandle">A 64bit Region Handle</param>
        /// <returns>A simprofile</returns>
        SimProfileData GetProfileByHandle(ulong regionHandle);

        /// <summary>
        /// Returns a sim profile from a UUID
        /// </summary>
        /// <param name="UUID">A 128bit UUID</param>
        /// <returns>A sim profile</returns>
        SimProfileData GetProfileByLLUUID(libsecondlife.LLUUID UUID);

        /// <summary>
        /// Authenticates a sim by use of it's recv key. 
        /// WARNING: Insecure
        /// </summary>
        /// <param name="UUID">The UUID sent by the sim</param>
        /// <param name="regionHandle">The regionhandle sent by the sim</param>
        /// <param name="simrecvkey">The recieving key sent by the sim</param>
        /// <returns>Whether the sim has been authenticated</returns>
        bool AuthenticateSim(libsecondlife.LLUUID UUID, ulong regionHandle, string simrecvkey);

        /// <summary>
        /// Initialises the interface
        /// </summary>
        void Initialise();

        /// <summary>
        /// Closes the interface
        /// </summary>
        void Close();

        /// <summary>
        /// The plugin being loaded
        /// </summary>
        /// <returns>A string containing the plugin name</returns>
        string getName();

        /// <summary>
        /// The plugins version
        /// </summary>
        /// <returns>A string containing the plugin version</returns>
        string getVersion();

        /// <summary>
        /// Adds a new profile to the database
        /// </summary>
        /// <param name="profile">The profile to add</param>
        /// <returns>RESPONSE_OK if successful, error if not.</returns>
        DataResponse AddProfile(SimProfileData profile);
    }
}
