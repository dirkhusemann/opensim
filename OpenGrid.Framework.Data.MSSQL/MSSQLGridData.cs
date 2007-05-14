using System;
using System.Collections.Generic;
using System.Text;
using OpenGrid.Framework.Data;

namespace OpenGrid.Framework.Data.MSSQL
{
    public class SqlGridData : IGridData
    {
        private MSSqlManager database;

        /// <summary>
        /// Initialises the Grid Interface
        /// </summary>
        public void Initialise()
        {
            database = new MSSqlManager("localhost", "db", "user", "password", "false");
        }

        /// <summary>
        /// Shuts down the grid interface
        /// </summary>
        public void Close()
        {
            database.Close();
        }

        public string getName()
        {
            return "Sql OpenGridData";
        }

        public string getVersion()
        {
            return "0.1";
        }

        public SimProfileData[] GetProfilesInRange(uint a, uint b, uint c, uint d)
        {
            return null;
        }

        /// <summary>
        /// Returns a sim profile from it's location
        /// </summary>
        /// <param name="handle">Region location handle</param>
        /// <returns>Sim profile</returns>
        public SimProfileData GetProfileByHandle(ulong handle)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param["handle"] = handle.ToString();

            System.Data.IDbCommand result = database.Query("SELECT * FROM regions WHERE handle = @handle", param);
            System.Data.IDataReader reader = result.ExecuteReader();

            SimProfileData row = database.getRow(reader);
            reader.Close();
            result.Dispose();

            return row;
        }

        /// <summary>
        /// Returns a sim profile from it's UUID
        /// </summary>
        /// <param name="uuid">The region UUID</param>
        /// <returns>The sim profile</returns>
        public SimProfileData GetProfileByLLUUID(libsecondlife.LLUUID uuid)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param["uuid"] = uuid.ToStringHyphenated();

            System.Data.IDbCommand result = database.Query("SELECT * FROM regions WHERE uuid = @uuid", param);
            System.Data.IDataReader reader = result.ExecuteReader();

            SimProfileData row = database.getRow(reader);
            reader.Close();
            result.Dispose();

            return row;
        }

        public DataResponse AddProfile(SimProfileData profile)
        {
            if (database.insertRow(profile))
            {
                return DataResponse.RESPONSE_OK;
            }
            else
            {
                return DataResponse.RESPONSE_ERROR;
            }
        }

        /// <summary>
        /// DEPRECIATED. Attempts to authenticate a region by comparing a shared secret.
        /// </summary>
        /// <param name="uuid">The UUID of the challenger</param>
        /// <param name="handle">The attempted regionHandle of the challenger</param>
        /// <param name="authkey">The secret</param>
        /// <returns>Whether the secret and regionhandle match the database entry for UUID</returns>
        public bool AuthenticateSim(libsecondlife.LLUUID uuid, ulong handle, string authkey)
        {
            bool throwHissyFit = false; // Should be true by 1.0

            if (throwHissyFit)
                throw new Exception("CRYPTOWEAK AUTHENTICATE: Refusing to authenticate due to replay potential.");

            SimProfileData data = GetProfileByLLUUID(uuid);

            return (handle == data.regionHandle && authkey == data.regionSecret);
        }

        /// <summary>
        /// NOT YET FUNCTIONAL. Provides a cryptographic authentication of a region
        /// </summary>
        /// <remarks>This requires a security audit.</remarks>
        /// <param name="uuid"></param>
        /// <param name="handle"></param>
        /// <param name="authhash"></param>
        /// <param name="challenge"></param>
        /// <returns></returns>
        public bool AuthenticateSim(libsecondlife.LLUUID uuid, ulong handle, string authhash, string challenge)
        {
            System.Security.Cryptography.SHA512Managed HashProvider = new System.Security.Cryptography.SHA512Managed();
            System.Text.ASCIIEncoding TextProvider = new ASCIIEncoding();

            byte[] stream = TextProvider.GetBytes(uuid.ToStringHyphenated() + ":" + handle.ToString() + ":" + challenge);
            byte[] hash = HashProvider.ComputeHash(stream);

            return false;
        }
    }


}
