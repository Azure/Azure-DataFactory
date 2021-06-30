using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;
using Renci.SshNet;
using System.Linq;
using System.Data.SqlClient;

namespace CustomActivities
{
    class ADF_SFTP_MetadataReader : IClass
    {
        static string sftpUserName;
        static string passWord;
        static string connectionHost;
        static string outPutTableName;
        static string outPutDbConnectionString;
        static string fileName;
        static string FileCat;
        static string FileExt;
        static string remoteDirectory;

        public void Execute()
        {
            dynamic activity = JsonConvert.DeserializeObject(File.ReadAllText("activity.json"));

            connectionHost = activity.typeProperties.extendedProperties.connectionHost;
            outPutTableName = activity.typeProperties.extendedProperties.outPutTableName;
            outPutDbConnectionString = activity.typeProperties.extendedProperties.outPutDbConnectionString;
            sftpUserName = activity.typeProperties.extendedProperties.sftpUserName;
            remoteDirectory = activity.typeProperties.extendedProperties.remoteDirectory;
            passWord = activity.typeProperties.extendedProperties.passWord;
            FileCat = activity.typeProperties.extendedProperties.FileCat;
            FileExt = activity.typeProperties.extendedProperties.FileExt;

            try
            {

                DataTable tempDataTable = SFTPMetadata(connectionHost, sftpUserName, passWord, remoteDirectory);

                SQLCopy(outPutDbConnectionString, tempDataTable, outPutTableName);

            }
            catch (Exception ex)
            {

                throw;
            }

        }


        public static DataTable SFTPMetadata(string connectionHost, string connectionUsername, string connectionPassword, string remoteDirectory)
        {


            using (var sftp = new SftpClient(connectionHost, connectionUsername, connectionPassword))
            {
                sftp.Connect();

                var files = sftp.ListDirectory(remoteDirectory);

                var file = files.Where(y => !y.Name.StartsWith(".")).OrderBy(x => x.LastAccessTime).Last();

                DataTable table = new DataTable();

                table.Columns.Add("FileType", typeof(string));

                table.Columns.Add("Source", typeof(string));

                table.Columns.Add("FileName", typeof(string));

                table.Columns.Add("DateTimeUTC", typeof(DateTime));

                Console.WriteLine("Last updated file :" + file.Name);

                fileName = file.Name;

                table.Rows.Add(FileExt, FileCat, fileName, DateTime.UtcNow);
                return table;
            }


        }


        public static void SQLCopy(string outPutDbConnectionString, DataTable tempDataTable, string DBTableName)
        {
            try
            {
                SqlConnection dbConnection = new SqlConnection(outPutDbConnectionString);

                dbConnection.Open();

                SqlBulkCopy bulkcopy = new SqlBulkCopy(dbConnection);
                bulkcopy.DestinationTableName = DBTableName;
                bulkcopy.BatchSize = 100;
                bulkcopy.BulkCopyTimeout = 1000;
                bulkcopy.WriteToServer(tempDataTable);
                dbConnection.Close();

            }
            catch (Exception ex)

            {

                throw;
            }


        }




    }

}