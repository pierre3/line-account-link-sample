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
        protected override async Task OnMessageAsync(MessageEvent ev)
        {
            switch (ev.Message)
            {
                case TextEventMessage textMessage:

                    var status = await Status.FindAsync(ev.Source.Type.ToString(), ev.Source.Id);
                    //
                    //アカウント連携済みの場合
                    //
                    if (!string.IsNullOrEmpty(status?.AccountLinkNonce))
                    {
                        //nonceを使って、WebAppのAPIをたたく
                        var nonce = Uri.EscapeUriString(status.AccountLinkNonce);
                        var userInfo = await _httpClient.GetStringAsync($"http://lineaccountlinkapp.azurewebsites.net/api/user/info/{nonce}");
                        await Line.ReplyMessageAsync(ev.ReplyToken, userInfo);
                        break;
                    }
                    //
                    //アカウント未連携の場合
                    //
                    //LINEサーバーからLink Tokenを取得
                    var linkToken = await Line.IssueLinkTokenAsync(ev.Source.Id);
                    //WebAppへのログインが成功後の遷移先URLを指定
                    var returnUrl = Uri.EscapeUriString($"/Account/LineLink?linkToken={linkToken}");
                    //連携用のリンクをユーザーに返信
                    await Line.ReplyMessageAsync(ev.ReplyToken, new[]
                    {
                        new TemplateMessage("account link",
                            new ButtonsTemplate("アカウント連携をします。", null, "LINE Account Link", new[]
                        {
                            new UriTemplateAction("OK", $"http://lineaccountlinkapp.azurewebsites.net/Account/Login?returnUrl={returnUrl}")
                        }))
                    });
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
            var status = new BotStatus()
            {
                SourceType = ev.Source.Type.ToString(),
                SourceId = ev.Source.Id,
                AccountLinkNonce = ev.Link.Nonce
            };
            await Status.UpdateAsync(status);
        }
    }
}
