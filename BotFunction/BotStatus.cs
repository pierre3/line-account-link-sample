using Microsoft.WindowsAzure.Storage.Table;
namespace BotFunction
{


    public class BotStatus : TableEntity
    {
        [IgnoreProperty]
        public static string DefaultPartitionKey => "account link";

        [IgnoreProperty]
        public string UserId { get => RowKey; set => RowKey = value; }

        public string AccountLinkNonce { get; set; }

        public BotStatus()
        {
            PartitionKey = DefaultPartitionKey;
        }

        public BotStatus(string userId, string accountLinkNonce) : this()
        {
            UserId = userId;
            AccountLinkNonce = accountLinkNonce;
        }
    }

}
