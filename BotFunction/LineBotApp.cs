using Line.Messaging;
using Line.Messaging.Webhooks;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BotFunction
{

    class LineBotApp : WebhookApplication
    {
        private static HttpClient _httpClient = new HttpClient();

        private LineMessagingClient Line { get; }
        private TableStorage<BotStatus> Status { get; }
        private TraceWriter Log { get; }

        public LineBotApp(LineMessagingClient lineMessagingClient, TableStorage<BotStatus> status, TraceWriter log)
        {
            Line = lineMessagingClient;
            Status = status;
            Log = log;
        }

        /// <summary>
        /// メッセージイベント
        /// </summary>
        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message)
            {
                case TextEventMessage textMessage:

                    var status = await Status.FindAsync(
                        partitionKey: BotStatus.DefaultPartitionKey,
                        rowKey: ev.Source.Id);

                    if (string.IsNullOrEmpty(status?.AccountLinkNonce))
                    {
                        //連携開始
                        await StartAccountLinkAsync(ev);
                    }
                    //アカウント連携済みの場合
                    else
                    {

                        if (textMessage.Text == "解除")
                        {
                            //連係解除
                            await UnlinkAsync(ev, status);
                            return;
                        }
                        //連携済みのWebサービスのAPIを利用
                        await InvokeWebApiAsync(ev, status);

                    }
                    break;
            }
        }

        /// <summary>
        /// アカウント連携イベント
        /// </summary>
        protected override async Task OnAccountLinkAsync(AccountLinkEvent ev)
        {
            if (ev.Link.Result == LinkResult.Failed)
            {
                await Line.ReplyMessageAsync(ev.ReplyToken, $"アカウント連携に失敗しました....orz");
                return;
            }

            await Line.ReplyMessageAsync(ev.ReplyToken, $"アカウント連携に成功しました！");

            //連携に成功したらNonceを保存しておく
            await Status.UpdateAsync(
                new BotStatus(
                    userId: ev.Source.Id,
                    accountLinkNonce: ev.Link.Nonce));
        }

        /// <summary>
        /// アカウント連携を開始する
        /// </summary>
        private async Task StartAccountLinkAsync(MessageEvent ev)
        {

            //LINEサーバーからLink Tokenを取得
            var linkToken = Uri.EscapeDataString(await Line.IssueLinkTokenAsync(ev.Source.Id));
            //連携用のリンクをユーザーに返信
            await Line.ReplyMessageAsync(ev.ReplyToken, new[]
            {
                        new TemplateMessage("account link",
                            new ButtonsTemplate("アカウント連携をします。", null, "LINE Account Link", new[]
                        {
                            new UriTemplateAction("OK", $"https://lineaccountlinkapp.azurewebsites.net/Account/Link?linkToken={linkToken}")
                        }))
                    });
        }

        /// <summary>
        /// アカウント連携を解除する
        /// </summary>
        private async Task UnlinkAsync(MessageEvent ev, BotStatus status)
        {
            var ret = await _httpClient.DeleteAsync($"https://lineaccountlinkapp.azurewebsites.net/Account/Unlink?nonce={Uri.EscapeDataString(status.AccountLinkNonce)}");
            if (!ret.IsSuccessStatusCode)
            {
                await Line.ReplyMessageAsync(ev.ReplyToken, "アカウントリンクの解除に失敗しました。");
            }
            else
            {
                await Status.DeleteAsync(ev.Source.Type.ToString(), ev.Source.Id);
                await Line.ReplyMessageAsync(ev.ReplyToken, "アカウントリンクを解除しました。");
            }
        }

        /// <summary>
        /// アカウント連携時に取得したNonceを利用してWebAppのAPIを実行
        /// </summary>
        private async Task InvokeWebApiAsync(MessageEvent ev, BotStatus status)
        {
            var nonce = Uri.EscapeDataString(status.AccountLinkNonce);
            var userInfo = await _httpClient.GetStringAsync($"https://lineaccountlinkapp.azurewebsites.net/api/user/info?nonce={nonce}");
            await Line.ReplyMessageAsync(ev.ReplyToken, userInfo);
        }


    }
}
