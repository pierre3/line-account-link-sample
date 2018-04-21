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
                        if (textMessage.Text == "����")
                        {

                            var ret = await _httpClient.DeleteAsync($"https://lineaccountlinkapp.azurewebsites.net/Account/Unlink?nonce={Uri.EscapeDataString(status.AccountLinkNonce)}");
                            if (!ret.IsSuccessStatusCode)
                            {
                                await Line.ReplyMessageAsync(ev.ReplyToken, "�A�J�E���g�����N�̉����Ɏ��s���܂����B");
                            }
                            else
                            {
                                await Status.DeleteAsync(ev.Source.Type.ToString(), ev.Source.Id);
                                await Line.ReplyMessageAsync(ev.ReplyToken, "�A�J�E���g�����N���������܂����B");
                            }
                            return;
                        }

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
            var linkToken = Uri.EscapeDataString(await Line.IssueLinkTokenAsync(ev.Source.Id));
            //�A�g�p�̃����N�����[�U�[�ɕԐM
            await Line.ReplyMessageAsync(ev.ReplyToken, new[]
            {
                        new TemplateMessage("account link",
                            new ButtonsTemplate("�A�J�E���g�A�g�����܂��B", null, "LINE Account Link", new[]
                        {
                            new UriTemplateAction("OK", $"https://lineaccountlinkapp.azurewebsites.net/Account/Link?linkToken={linkToken}")
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
