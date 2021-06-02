using Newtonsoft.Json;

namespace Microsoft.ADF.DotNetActivityRunner.Models
{
    internal class TableTypeProperties
    {
        [JsonProperty("folderPath")]
        internal string FolderPath { get; set; }

        [JsonProperty("fileName")]
        internal string FileName { get; set; }
        
        [JsonProperty("tableName")]
        internal string TableName { get; set; }
    }
}