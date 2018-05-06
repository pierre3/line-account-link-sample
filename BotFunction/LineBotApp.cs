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
        //アカウント連携していない場合のリッチメニューID
        private static readonly string LinkRichMenuId = "richmenu-3631a6e7eb1f293659ebb757b56ed86b";
        //アカウント連携済みの場合のリッチメニューID
        private static readonly string UnLinkRichMenuId = "richmenu-7128fe883b7b31cdc3e726ee4058d3e8";

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
                        await Line.LinkRichMenuToUserAsync(ev.Source.Id, LinkRichMenuId);
                        await Line.ReplyMessageAsync(ev.ReplyToken, "ボタンをタップしてWebサービスとユーザーアカウントを連携してね！");
                    }
                    else
                    {
                        await Line.LinkRichMenuToUserAsync(ev.Source.Id, UnLinkRichMenuId);
                        await Line.ReplyMessageAsync(ev.ReplyToken, "「ユーザー情報確認」をタップすると、Webサービスのアカウント情報の確認ができるよ");
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

            //メニュー切り替え
            await Line.LinkRichMenuToUserAsync(ev.Source.Id, UnLinkRichMenuId);

            //連携に成功したらNonceを保存しておく
            await Status.UpdateAsync(
                new BotStatus(
                    userId: ev.Source.Id,
                    accountLinkNonce: ev.Link.Nonce));
            
        }

        protected override async Task OnPostbackAsync(PostbackEvent ev)
        {
            var status = await Status.FindAsync(
                    partitionKey: BotStatus.DefaultPartitionKey,
                    rowKey: ev.Source.Id);
            switch (ev.Postback.Data)
            {
                case "account link":    //連携開始
                    await StartAccountLinkAsync(ev);
                    break;
                case "API(UserInfo)":   //連携済みのWebサービスのAPIを利用
                    await InvokeWebApiAsync(ev, status);
                    break;
                case "Unlink":          //連係解除
                    await UnlinkAsync(ev, status);
                    break;

            }
        }

        /// <summary>
        /// アカウント連携を開始する
        /// </summary>
        private async Task StartAccountLinkAsync(ReplyableEvent ev)
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
        private async Task UnlinkAsync(ReplyableEvent ev, BotStatus status)
        {
            var ret = await _httpClient.DeleteAsync($"https://lineaccountlinkapp.azurewebsites.net/Account/Unlink?nonce={Uri.EscapeDataString(status.AccountLinkNonce)}");
            if (!ret.IsSuccessStatusCode)
            {
                await Line.ReplyMessageAsync(ev.ReplyToken, "アカウントリンクの解除に失敗しました。");
            }
            else
            {
                await Status.DeleteAsync(BotStatus.DefaultPartitionKey, ev.Source.Id);
                await Line.ReplyMessageAsync(ev.ReplyToken, "アカウントリンクを解除しました。");
                //メニュー切り替え
                await Line.LinkRichMenuToUserAsync(ev.Source.Id, LinkRichMenuId);
            }
        }

        /// <summary>
        /// アカウント連携時に取得したNonceを利用してWebAppのAPIを実行
        /// </summary>
        private async Task InvokeWebApiAsync(ReplyableEvent ev, BotStatus status)
        {
            var nonce = Uri.EscapeDataString(status.AccountLinkNonce);
            var userInfo = await _httpClient.GetStringAsync($"https://lineaccountlinkapp.azurewebsites.net/api/user/info?nonce={nonce}");
            await Line.ReplyMessageAsync(ev.ReplyToken, userInfo);
        }


    }
}
