using Microsoft.WindowsAzure.Storage.Table;
namespace BotFunction
{


    public class BotStatus : TableEntity
    {
        [IgnoreProperty]
        public string SourceType { get => PartitionKey; set => PartitionKey = value; }
        [IgnoreProperty]
        public string SourceId { get => RowKey; set => RowKey = value; }

        public string AccountLinkNonce { get; set; }
        public BotStatus()
        {

        }
    }

}
