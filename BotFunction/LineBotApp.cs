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
        //�A�J�E���g�A�g���Ă��Ȃ��ꍇ�̃��b�`���j���[ID
        private static readonly string LinkRichMenuId = "richmenu-3631a6e7eb1f293659ebb757b56ed86b";
        //�A�J�E���g�A�g�ς݂̏ꍇ�̃��b�`���j���[ID
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
        /// ���b�Z�[�W�C�x���g
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
                        await Line.ReplyMessageAsync(ev.ReplyToken, "�{�^�����^�b�v����Web�T�[�r�X�ƃ��[�U�[�A�J�E���g��A�g���ĂˁI");
                    }
                    else
                    {
                        await Line.LinkRichMenuToUserAsync(ev.Source.Id, UnLinkRichMenuId);
                        await Line.ReplyMessageAsync(ev.ReplyToken, "�u���[�U�[���m�F�v���^�b�v����ƁAWeb�T�[�r�X�̃A�J�E���g���̊m�F���ł����");
                    }
                    break;
            }
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

            //���j���[�؂�ւ�
            await Line.LinkRichMenuToUserAsync(ev.Source.Id, UnLinkRichMenuId);

            //�A�g�ɐ���������Nonce��ۑ����Ă���
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
                case "account link":    //�A�g�J�n
                    await StartAccountLinkAsync(ev);
                    break;
                case "API(UserInfo)":   //�A�g�ς݂�Web�T�[�r�X��API�𗘗p
                    await InvokeWebApiAsync(ev, status);
                    break;
                case "Unlink":          //�A�W����
                    await UnlinkAsync(ev, status);
                    break;

            }
        }

        /// <summary>
        /// �A�J�E���g�A�g���J�n����
        /// </summary>
        private async Task StartAccountLinkAsync(ReplyableEvent ev)
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
        /// �A�J�E���g�A�g����������
        /// </summary>
        private async Task UnlinkAsync(ReplyableEvent ev, BotStatus status)
        {
            var ret = await _httpClient.DeleteAsync($"https://lineaccountlinkapp.azurewebsites.net/Account/Unlink?nonce={Uri.EscapeDataString(status.AccountLinkNonce)}");
            if (!ret.IsSuccessStatusCode)
            {
                await Line.ReplyMessageAsync(ev.ReplyToken, "�A�J�E���g�����N�̉����Ɏ��s���܂����B");
            }
            else
            {
                await Status.DeleteAsync(BotStatus.DefaultPartitionKey, ev.Source.Id);
                await Line.ReplyMessageAsync(ev.ReplyToken, "�A�J�E���g�����N���������܂����B");
                //���j���[�؂�ւ�
                await Line.LinkRichMenuToUserAsync(ev.Source.Id, LinkRichMenuId);
            }
        }

        /// <summary>
        /// �A�J�E���g�A�g���Ɏ擾����Nonce�𗘗p����WebApp��API�����s
        /// </summary>
        private async Task InvokeWebApiAsync(ReplyableEvent ev, BotStatus status)
        {
            var nonce = Uri.EscapeDataString(status.AccountLinkNonce);
            var userInfo = await _httpClient.GetStringAsync($"https://lineaccountlinkapp.azurewebsites.net/api/user/info?nonce={nonce}");
            await Line.ReplyMessageAsync(ev.ReplyToken, userInfo);
        }


    }
}
