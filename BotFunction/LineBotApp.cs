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

                    //�A�J�E���g�A�g���Ă��Ȃ��ꍇ
                    if (string.IsNullOrEmpty(status?.AccountLinkNonce))
                    {
                        await StartAccountLinkAsync(ev);
                    }
                    //�A�J�E���g�A�g�ς݂̏ꍇ
                    else
                    {
                        await GetWebAppUserInfoAsync(ev, status);
                    }
                    break;
            }
        }

        /// <summary>
        /// �A�J�E���g�A�g���J�n����
        /// </summary>
        private async Task StartAccountLinkAsync(MessageEvent ev)
        {

            //LINE�T�[�o�[����Link Token���擾
            var linkToken = await Line.IssueLinkTokenAsync(ev.Source.Id);
            //WebApp�ւ̃��O�C����������̑J�ڐ�URL���w��
            var returnUrl = Uri.EscapeDataString($"/Account/LineLink?linkToken={linkToken}");
            //�A�g�p�̃����N�����[�U�[�ɕԐM
            await Line.ReplyMessageAsync(ev.ReplyToken, new[]
            {
                        new TemplateMessage("account link",
                            new ButtonsTemplate("�A�J�E���g�A�g�����܂��B", null, "LINE Account Link", new[]
                        {
                            new UriTemplateAction("OK", $"https://lineaccountlinkapp.azurewebsites.net/Account/Login?returnUrl={returnUrl}")
                        }))
                    });
        }

        /// <summary>
        /// �A�J�E���g�A�g���Ɏ擾����Nonce�𗘗p����WebApp��API�����s
        /// </summary>
        private async Task GetWebAppUserInfoAsync(MessageEvent ev, BotStatus status)
        {
            var nonce = Uri.EscapeDataString(status.AccountLinkNonce);
            var userInfo = await _httpClient.GetStringAsync($"https://lineaccountlinkapp.azurewebsites.net/api/user/info?nonce={nonce}");
            await Line.ReplyMessageAsync(ev.ReplyToken, userInfo);
        }

        /// <summary>
        /// �A�J�E���g�A�g�C�x���g
        /// </summary>
        protected override async Task OnAccountLinkAsync(AccountLinkEvent ev)
        {
            if (ev.Link.Result == LinkResult.Failed)
            {
                await Line.ReplyMessageAsync(ev.ReplyToken, $"�A�J�E���g�A�g�Ɏ��s���܂���....orz");
                return;
            }

            await Line.ReplyMessageAsync(ev.ReplyToken, $"�A�J�E���g�A�g�ɐ������܂����I");
            //�A�g�ɐ���������Nonce��ۑ����Ă���
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
