using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using AdaptiveCards;
using BotAuth.AADv2;
using BotAuth.Models;
using System.Configuration;
using BotAuth.Dialogs;
using System.Threading;
using ApprovalBot.Helpers;
using ApprovalBot.Models;
using Newtonsoft.Json.Linq;

namespace ApprovalBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private static AuthenticationOptions authOptions = new AuthenticationOptions()
        {
            ClientId = ConfigurationManager.AppSettings["MicrosoftAppId"],
            ClientSecret = ConfigurationManager.AppSettings["MicrosoftAppPassword"],
            Scopes = new string[] { "User.Read", "Files.ReadWrite", "Mail.Send", "People.Read", "MailboxSettings.Read" },
            RedirectUrl = $"{ConfigurationManager.AppSettings["AppRootUrl"]}/callback",
        };

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            try
            {
                var activity = await result as Activity;
                string userText = string.IsNullOrEmpty(activity.Text) ? string.Empty : activity.Text.ToLower();

                // Handle the text as appropriate
                if (IsMessageEmpty(activity) || userText.StartsWith("help"))
                {
                    await ShowHelp(context);
                    context.Wait(MessageReceivedAsync);
                }

                else if (userText.StartsWith("hi") || userText.StartsWith("hello") || userText.StartsWith("hey"))
                {
                    await context.PostAsync(@"Hi! What can I do for you? (try ""get approval"" to start an approval request)");
                    context.Wait(MessageReceivedAsync);
                }

                else if (userText.StartsWith("logout"))
                {
                    await new MSALAuthProvider().Logout(authOptions, context);
                    await context.PostAsync("You are now logged out.");
                    context.Wait(MessageReceivedAsync);
                }

                else if (userText.StartsWith("reset demo please"))
                {
                    await ResetDemo(context, activity);
                    await context.PostAsync("DEMO RESET");
                    context.Wait(MessageReceivedAsync);
                }

                else
                {
                    // Anything else requires auth
                    // See if we already have a token
                    string accessToken = await GetAccessToken(context);
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        // Save the user's input
                        context.UserData.SetValue("preAuthCommand", activity);
                        // Do prompt
                        await context.Forward(new AuthDialog(new MSALAuthProvider(), authOptions),
                        ResumeAfterAuth, activity, CancellationToken.None);
                    }
                    else
                    {
                        var reply = await HandleMessage(context, activity, accessToken);
                        // return our reply to the user
                        await context.PostAsync(reply);
                        context.Wait(MessageReceivedAsync);
                    }
                }
            }
            catch (Microsoft.Graph.ServiceException ex)
            {
                await context.PostAsync("EXCEPTION OCCURRED");
                await context.PostAsync(ex.ToString());
                context.Wait(MessageReceivedAsync);
            }
        }

        private bool IsMessageEmpty(Activity activity)
        {
            return string.IsNullOrWhiteSpace(activity.Text) && activity.Value == null;
        }

        private async Task<Activity> HandleMessage(IDialogContext context, Activity activity, string accessToken)
        {
            Activity reply = null;
            string message = string.IsNullOrEmpty(activity.Text) ? string.Empty : activity.Text.ToLower();

            if (string.IsNullOrEmpty(message))
            {
                // Handle card action data
                var actionData = ActionData.Parse(activity.Value);

                if (actionData.CardAction == CardActionTypes.SelectFile)
                {
                    // Show file detail card and confirm selection
                    reply = await ConfirmFile(context, activity, accessToken, actionData.SelectedFile);
                }
                else if (actionData.CardAction == CardActionTypes.SendApprovalRequest)
                {
                    // Check approvers
                    if (string.IsNullOrEmpty(actionData.Approvers))
                    {
                        reply = PromptForMissingInfo(context, activity,
                            "I need at least one approver email address to send to. Who should I send to?",
                            "approvers", actionData);
                    }

                    else
                    {
                        string[] approvers = EmailHelper.ConvertDelimitedAddressStringToArray(actionData.Approvers.Trim());
                        if (approvers == null)
                        {
                            reply = PromptForMissingInfo(context, activity,
                                $"One or more values in **{actionData.Approvers}** is not a valid SMTP email address. Can you give me the list of approvers again?",
                                "approvers", actionData);
                        }
                        else
                        {
                            await ShowTyping(context, activity);
                            await ApprovalRequestHelper.SendApprovalRequest(accessToken, activity.From.Id, actionData.SelectedFile, approvers);
                            reply = activity.CreateReply(@"I've sent the request. You can check the status of your request by typing ""check status"".");
                        }
                    }
                }
                else if (actionData.CardAction == CardActionTypes.WrongFile)
                {
                    // Re-prompt for a file
                    reply = await PromptForFile(context, activity, accessToken);
                }
                else if (actionData.CardAction == CardActionTypes.SelectApproval)
                {
                    reply = await GetApprovalStatus(context, activity, actionData.SelectedApproval);
                }
            }
            else
            {
                if (message.StartsWith("get approval"))
                {
                    RemoveMissingInfoState(context);
                    reply = await PromptForFile(context, activity, accessToken);
                }
                else if (message.StartsWith("check status"))
                {
                    RemoveMissingInfoState(context);
                    reply = await PromptForApprovalRequest(context, activity, accessToken);
                }
                else if (!string.IsNullOrEmpty(ExpectedMissingInfo(context)))
                {
                    string missingField = ExpectedMissingInfo(context);

                    if (missingField == "approvers")
                    {
                        // Validate input
                        string[] approvers = EmailHelper.ConvertDelimitedAddressStringToArray(message.Trim());
                        if (approvers == null)
                        {
                            reply = activity.CreateReply(@"Sorry, I'm still having trouble. Please enter the approvers again, keeping in mind:
- Use full SMTP email addresses, like `bob@contsoso.com`
- Separate multiple email addresses with a semicolon (`;`), like `bob@contoso.com;allie@contoso.com`");
                        }
                        else
                        {
                            ActionData actionData = context.UserData.GetValue<ActionData>("actionData");
                            RemoveMissingInfoState(context);
                            await ShowTyping(context, activity);
                            await ApprovalRequestHelper.SendApprovalRequest(accessToken, activity.From.Id, actionData.SelectedFile, approvers);
                            reply = activity.CreateReply(@"I've sent the request. You can check the status of your request by typing ""check status"".");
                        }
                    }
                }
            }

            if (reply == null)
            {
                reply = activity.CreateReply(@"I'm sorry, I don't understand what you want me to do. Type ""help"" to see a list of things I can do.");
            }

            return reply;
        }

        private Activity PromptForMissingInfo(IDialogContext context, Activity activity, string prompt, string field, ActionData actionData)
        {
            context.ConversationData.SetValue("missingField", field);
            context.ConversationData.SetValue("actionData", actionData);

            return activity.CreateReply(prompt);
        }

        private string ExpectedMissingInfo(IDialogContext context)
        {
            return context.ConversationData.GetValueOrDefault<string>("missingField");
        }

        private void RemoveMissingInfoState(IDialogContext context)
        {
            context.ConversationData.RemoveValue("missingField");
            context.ConversationData.RemoveValue("actionData");
        }

        private async Task<Activity> PromptForFile(IDialogContext context, Activity activity, string accessToken)
        {
            await ShowTyping(context, activity);
            // Get a list of files to choose from
            var pickerCard = await GraphHelper.GetFilePickerCardFromOneDrive(accessToken);
            if (pickerCard != null)
            {
                var reply = activity.CreateReply("Get approval for which file?");
                reply.Attachments = new List<Attachment>()
                {
                    new Attachment() { ContentType = AdaptiveCard.ContentType, Content = pickerCard }
                };

                return reply;
            }
            else
            {
                return activity.CreateReply("I couldn't find any files in your OneDrive. Please add the file you want approved and try again.");
            }
        }

        private async Task<Activity> ConfirmFile(IDialogContext context, Activity activity, string accessToken, string fileId)
        {
            await ShowTyping(context, activity);
            var reply = activity.CreateReply("Get approval for this file?");
            var fileDetailCard = await GraphHelper.GetFileDetailCard(accessToken, fileId);
            reply.Attachments = new List<Attachment>()
            {
                new Attachment() { ContentType = AdaptiveCard.ContentType, Content = fileDetailCard }
            };

            return reply;
        }

        private async Task<Activity> PromptForApprovalRequest(IDialogContext context, Activity activity, string accessToken)
        {
            await ShowTyping(context, activity);
            var statusCardList = await ApprovalStatusHelper.GetApprovalsForUserCard(accessToken, activity.From.Id);
            if (statusCardList == null)
            {
                return activity.CreateReply("I'm sorry, but I didn't find any approvals requested by you.");
            }
            else
            {
                var reply = activity.CreateReply();

                if (statusCardList.Count > 1)
                {
                    reply.Text = "Select an approval to see its status.";
                    reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                }

                reply.Attachments = new List<Attachment>();

                foreach(var card in statusCardList)
                {
                    reply.Attachments.Add(new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card
                    });
                }

                return reply;
            }
        }

        private async Task<Activity> GetApprovalStatus(IDialogContext context, Activity activity, string approvalId)
        {
            await ShowTyping(context, activity);
            var statusCard = await ApprovalStatusHelper.GetApprovalStatusCard(approvalId);
            var reply = activity.CreateReply("Here's what I found:");
            reply.Attachments = new List<Attachment>()
            {
                new Attachment() { ContentType = AdaptiveCard.ContentType, Content = statusCard }
            };
            return reply;
        }

        private async Task ShowTyping(IDialogContext context, Activity activity)
        {
            var typing = activity.CreateReply();
            typing.Type = ActivityTypes.Typing;
            await context.PostAsync(typing);
        }

        private async Task ShowHelp(IDialogContext context)
        {
            await context.PostAsync("I am ApprovalBot. I can help you get public release approval for any documents in your OneDrive for Business.");
            await context.PostAsync(@"Try one of the following commands to get started:

- ""get approval""
- ""check status""");
        }

        protected async Task<string> GetAccessToken(IDialogContext context)
        {
            var provider = new MSALAuthProvider();
            var authResult = await provider.GetAccessToken(authOptions, context);

            return (authResult == null ? string.Empty : authResult.AccessToken);
        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<AuthResult> result)
        {
            var message = await result;

            // See if we've saved a command
            var preAuthCommand = context.UserData.GetValueOrDefault<Activity>("preAuthCommand", null);
            if (preAuthCommand != null)
            {
                // remove it
                context.UserData.RemoveValue("preAuthCommand");
                var reply = await HandleMessage(context, preAuthCommand, message.AccessToken);
                await context.PostAsync(reply);
            }
            else
            {
                await context.PostAsync("Now that you're logged in, what can I do for you?");
            }

            context.Wait(MessageReceivedAsync);
        }

        private async Task ResetDemo(IDialogContext context, Activity activity)
        {
            // Remove any pending approvals
            await DatabaseHelper.DeleteAllUserApprovals(activity.From.Id);

            context.UserData.Clear();
        }
    }
}