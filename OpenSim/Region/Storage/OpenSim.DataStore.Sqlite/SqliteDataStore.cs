using System;
using System.Collections.Generic;
using System.Text;

using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Environment.LandManagement;
using OpenSim.Region.Environment;
using OpenSim.Region.Interfaces;
using OpenSim.Framework.Console;
using libsecondlife;

using System.Data;
// Yes, this won't compile on MS, need to deal with that later
using Mono.Data.SqliteClient;
using Primitive = OpenSim.Region.Environment.Scenes.Primitive;

namespace OpenSim.DataStore.SqliteStorage
{

    public class SqliteDataStore : IRegionDataStore
    {
        private const string primSelect = "select * from prims";
        private const string shapeSelect = "select * from primshapes";
        
        private DataSet ds;
        private SqliteDataAdapter primDa;
        private SqliteDataAdapter shapeDa;

        public void Initialise(string dbfile, string dbname)
        {
            // for us, dbfile will be the connect string
            MainLog.Instance.Verbose("DATASTORE", "Sqlite - connecting: " + dbfile);
            SqliteConnection conn = new SqliteConnection(dbfile);

            SqliteCommand primSelectCmd = new SqliteCommand(primSelect, conn);
            primDa = new SqliteDataAdapter(primSelectCmd);
            //            SqliteCommandBuilder primCb = new SqliteCommandBuilder(primDa);
            
            SqliteCommand shapeSelectCmd = new SqliteCommand(shapeSelect, conn);
            shapeDa = new SqliteDataAdapter(shapeSelectCmd);
            // SqliteCommandBuilder shapeCb = new SqliteCommandBuilder(shapeDa);

            ds = new DataSet();

            // We fill the data set, now we've got copies in memory for the information
            // TODO: see if the linkage actually holds.
            primDa.FillSchema(ds, SchemaType.Source, "PrimSchema");
            primDa.Fill(ds, "prims");
            DataTable prims = ds.Tables["prims"];
            prims.PrimaryKey = new DataColumn[] { prims.Columns["UUID"] };
            setupPrimCommands(primDa);
            
            shapeDa.FillSchema(ds, SchemaType.Source, "ShapeSchema");
            shapeDa.Fill(ds, "primshapes");
            
            return;
        }

        private void setupPrimCommands(SqliteDataAdapter da)
        {
            SqliteCommand delete = new SqliteCommand("delete from prims where UUID=@UUID");
            SqliteParameterCollection parms = delete.Parameters;
            parms.Add("@UUID", SqlDbType.VarChar);
            parms["@UUID"].SourceVersion=DataRowVersion.Original;
            da.DeleteCommand = delete;


            string sql = "insert into prims(" +
                "UUID, CreationDate, Name, PositionX, PositionY, PositionZ" +
                ") values(@UUID, @CreationDate, @Name, @PositionX, @PositionY, @PositionZ)";
            SqliteCommand insert = new SqliteCommand(sql);
            parms = insert.Parameters;
            parms.Add("@UUID", SqlDbType.VarChar);
            parms.Add("@CreationDate", SqlDbType.Int);
            parms.Add("@Name", SqlDbType.VarChar);
            parms.Add("@PositionX", SqlDbType.Float);
            parms.Add("@PositionY", SqlDbType.Float);
            parms.Add("@PositionZ", SqlDbType.Float);
            parms["@UUID"].SourceVersion=DataRowVersion.Original;
            da.InsertCommand = insert;

            // throw away for now until the rest works
            string updateSQL = "update prims set name='update'";
            SqliteCommand update = new SqliteCommand(updateSQL);
            da.UpdateCommand = update;

        }

        private void StoreSceneObject(SceneObject obj)
        {
            
        }

        public void StoreObject(AllNewSceneObjectPart2 obj)
        {
            // TODO: Serializing code
            DataTable prims = ds.Tables["prims"];
            DataTable shapes = ds.Tables["shapes"];
            
           
            
        }
        
        private void fillPrimRow(DataRow row, Primitive prim) 
        {
            row["UUID"] = prim.UUID;
            row["CreationDate"] = prim.CreationDate;
            row["Name"] = prim.Name;
            row["PositionX"] = prim.Pos.X;
            row["PositionY"] = prim.Pos.Y;
            row["PositionZ"] = prim.Pos.Z;
        }

        private void addPrim(Primitive prim)
        {
            DataTable prims = ds.Tables["prims"];
            DataTable shapes = ds.Tables["shapes"];
            
            DataRow row = prims.Rows.Find(prim.UUID);
            if (row == null) {
                row = prims.NewRow();
                fillPrimRow(row, prim);
                prims.Rows.Add(row);
            } else {
                fillPrimRow(row, prim);
            }
        }

        private void commit() 
        {
            DataTable prims = ds.Tables["prims"];
            DataTable shapes = ds.Tables["shapes"];

            
        }

        public void StoreObject(SceneObject obj)
        {
            foreach (Primitive prim in obj.Children.Values)
            {
                addPrim(prim);
            }
            
            primDa.Update(ds, "prims");
            MainLog.Instance.Verbose("Dump of prims:", ds.GetXml());
        }

        

        public void RemoveObject(LLUUID obj)
        {
            // TODO: remove code
        }

        public List<SceneObject> LoadObjects()
        {
            List<SceneObject> retvals = new List<SceneObject>();

            MainLog.Instance.Verbose("DATASTORE", "Sqlite - LoadObjects found " + " objects");

            return retvals;
        }
        
        public void StoreTerrain(double[,] ter)
        {

        }

        public double[,] LoadTerrain()
        {
            return null;
        }

        public void RemoveLandObject(uint id)
        {

        }

        public void StoreParcel(Land parcel)
        {

        }

        public List<Land> LoadLandObjects()
        {
            return new List<Land>();
        }

        public void Shutdown()
        {
            // TODO: DataSet commit
        }
    }
}
