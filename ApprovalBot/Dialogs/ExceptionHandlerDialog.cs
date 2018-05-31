using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace ApprovalBot.Dialogs
{
    [Serializable]
    public class ExceptionHandlerDialog<T> : IDialog<object>
    {
        private readonly IDialog<T> dialog;
        private readonly bool displayException =
            !string.IsNullOrEmpty(ConfigurationManager.AppSettings["DisplayExceptions"]) &&
            ConfigurationManager.AppSettings["DisplayExceptions"].ToLower() == "true";

        public ExceptionHandlerDialog(IDialog<T> dialog)
        {
            this.dialog = dialog;
        }

        public async Task StartAsync(IDialogContext context)
        {
            try
            {
                context.Call(dialog, ResumeAsync);
            }
            catch (Exception ex)
            {
                if (displayException)
                {
                    await DisplayException(context, ex).ConfigureAwait(false);
                }
            }
        }

        public async Task ResumeAsync(IDialogContext context, IAwaitable<T> result)
        {
            try
            {
                context.Done(await result);
            }
            catch (Exception ex)
            {
                if (displayException)
                {
                    await DisplayException(context, ex).ConfigureAwait(false);
                }
            }
        }

        private async Task DisplayException(IDialogContext context, Exception ex)
        {
            var message = ex.Message.Replace(Environment.NewLine, "**  \n**");
            var stack = ex.StackTrace.Replace(Environment.NewLine, "  \n");

            await context.PostAsync($"**{message}**  \n\n{stack}").ConfigureAwait(false);
        }
    }
}