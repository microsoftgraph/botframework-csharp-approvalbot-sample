using AdaptiveCards;
using ApprovalBot.Helpers;
using ApprovalBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace ApprovalBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private static string ConnectionName = ConfigurationManager.AppSettings["ConnectionName"];

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
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
                await context.SignOutUserAsync(ConnectionName);
                await context.PostAsync("You are now logged out.");
                context.Wait(MessageReceivedAsync);
            }

            else if (userText.StartsWith("reset demo please"))
            {
                await ResetDemo(context, activity);
                await context.PostAsync("DEMO RESET");
                context.Wait(MessageReceivedAsync);
            }

            // Anything else requires auth
            // The text can be empty when receiving a message back from a card
            // button.
            else if (string.IsNullOrEmpty(userText))
            {
                // Handle card action data
                var actionData = ActionData.Parse(activity.Value);

                // When the user selects a file in the select file card
                if (actionData.CardAction == CardActionTypes.SelectFile)
                {
                    // Save selected file to conversation state
                    context.ConversationData.SetValue("selectedFile", actionData.SelectedFile);
                    // Show file detail card and confirm selection
                    context.Call(CreateGetTokenDialog(), ConfirmFile);
                    //reply = await ConfirmFile(context, activity, accessToken.Token, actionData.SelectedFile);
                }
                // When the user clicks the "send approval request" button
                else if (actionData.CardAction == CardActionTypes.SendApprovalRequest)
                {
                    // Check approvers
                    if (string.IsNullOrEmpty(actionData.Approvers))
                    {
                        SaveMissingInfoState(context, "approvers", actionData);
                        await context.PostAsync("I need at least one approver email address to send to. Who should I send to?");
                        context.Wait(MessageReceivedAsync);
                    }

                    else
                    {
                        string[] approvers = EmailHelper.ConvertDelimitedAddressStringToArray(actionData.Approvers.Trim());
                        if (approvers == null)
                        {
                            SaveMissingInfoState(context, "approvers", actionData);
                            await context.PostAsync($"One or more values in **{actionData.Approvers}** is not a valid SMTP email address. Can you give me the list of approvers again?");
                            context.Wait(MessageReceivedAsync);
                        }
                        else
                        {
                            //await ShowTyping(context, activity);
                            // Save user ID, selected file, and approvers to conversation state
                            context.ConversationData.SetValue("approvalRequestor", activity.From.Id);
                            context.ConversationData.SetValue("selectedFile", actionData.SelectedFile);
                            context.ConversationData.SetValue("approvers", approvers);

                            context.Call(CreateGetTokenDialog(), SendApprovalRequest);
                        }
                    }
                }
                // User clicked the "no" button when confirming the file.
                else if (actionData.CardAction == CardActionTypes.WrongFile)
                {
                    // Re-prompt for a file
                    context.Call(CreateGetTokenDialog(), PromptForFile);
                }
                //  User selected a pending approval to check status
                else if (actionData.CardAction == CardActionTypes.SelectApproval)
                {
                    // Save the selected approval to conversation state
                    context.ConversationData.SetValue("selectedApproval", actionData.SelectedApproval);
                    context.Call(CreateGetTokenDialog(), GetApprovalStatus);
                }
                else
                {
                    await context.PostAsync(@"I'm sorry, I don't understand what you want me to do. Type ""help"" to see a list of things I can do.");
                    context.Wait(MessageReceivedAsync);
                }
            }
            else if (userText.StartsWith("get approval"))
            {
                RemoveMissingInfoState(context);
                context.Call(CreateGetTokenDialog(), PromptForFile);
            }
            else if (userText.StartsWith("check status"))
            {
                RemoveMissingInfoState(context);
                // Save user ID to conversation state
                context.ConversationData.SetValue("approvalRequestor", activity.From.Id);
                context.Call(CreateGetTokenDialog(), PromptForApprovalRequest);
            }
            else if (!string.IsNullOrEmpty(ExpectedMissingInfo(context)))
            {
                string missingField = ExpectedMissingInfo(context);

                if (missingField == "approvers")
                {
                    // Validate input
                    string[] approvers = EmailHelper.ConvertDelimitedAddressStringToArray(userText.Trim());
                    if (approvers == null)
                    {
                        await context.PostAsync(@"Sorry, I'm still having trouble. Please enter the approvers again, keeping in mind:
- Use full SMTP email addresses, like `bob@contsoso.com`
- Separate multiple email addresses with a semicolon (`;`), like `bob@contoso.com;allie@contoso.com`");
                    }
                    else
                    {
                        ActionData actionData = context.UserData.GetValue<ActionData>("actionData");
                        RemoveMissingInfoState(context);
                        //await ShowTyping(context, activity);

                        // Save user ID, selected file, and approvers to conversation state
                        context.ConversationData.SetValue("approvalRequestor", activity.From.Id);
                        context.ConversationData.SetValue("selectedFile", actionData.SelectedFile);
                        context.ConversationData.SetValue("approvers", approvers);

                        context.Call(CreateGetTokenDialog(), SendApprovalRequest);
                    }
                }
            }
            else
            {
                await context.PostAsync(@"I'm sorry, I don't understand what you want me to do. Type ""help"" to see a list of things I can do.");
                context.Wait(MessageReceivedAsync);
            }
        }

        private bool IsMessageEmpty(Activity activity)
        {
            return string.IsNullOrWhiteSpace(activity.Text) && activity.Value == null;
        }

        private void SaveMissingInfoState(IDialogContext context, string field, ActionData actionData)
        {
            context.ConversationData.SetValue("missingField", field);
            context.ConversationData.SetValue("actionData", actionData);
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

        private void ClearConversationData(IDialogContext context, params object[] values)
        {
            foreach (string value in values)
            {
                context.ConversationData.RemoveValue(value);
            }
        }

        private async Task PromptForFile(IDialogContext context, IAwaitable<GetTokenResponse> tokenResponse)
        {
            //await ShowTyping(context, activity);
            var accessToken = await tokenResponse;

            if (accessToken == null || string.IsNullOrEmpty(accessToken.Token))
            {
                await context.PostAsync(Apologize("I could not get an access token"));
                return;
            }

            // Get a list of files to choose from
            AdaptiveCard pickerCard = null;
            try
            {
                pickerCard = await GraphHelper.GetFilePickerCardFromOneDrive(accessToken.Token);
            }
            catch (Microsoft.Graph.ServiceException ex)
            {
                if (ex.Error.Code == "UnknownError" && ex.Message.Contains("Invalid Hostname"))
                {
                    // retry
                    pickerCard = await GraphHelper.GetFilePickerCardFromOneDrive(accessToken.Token);
                }
                else
                {
                    throw;
                }
            }
            
            if (pickerCard != null)
            {
                var reply = context.MakeMessage();
                reply.Attachments = new List<Attachment>()
                {
                    new Attachment() { ContentType = AdaptiveCard.ContentType, Content = pickerCard }
                };

                await context.PostAsync(reply);
            }
            else
            {
                await context.PostAsync("I couldn't find any files in your OneDrive. Please add the file you want approved and try again.");
            }
        }

        private async Task ConfirmFile(IDialogContext context, IAwaitable<GetTokenResponse> tokenResponse)
        {
            var accessToken = await tokenResponse;
            var fileId = context.ConversationData.GetValueOrDefault<string>("selectedFile");

            ClearConversationData(context, "selectedFile");

            if (accessToken == null || string.IsNullOrEmpty(accessToken.Token))
            {
                await context.PostAsync(Apologize("I could not get an access token"));
                return;
            }

            if (string.IsNullOrEmpty(fileId))
            {
                await context.PostAsync(Apologize("I could not find a selected file ID in the conversation state"));
                return;
            }

            //await ShowTyping(context, activity);
            var reply = context.MakeMessage();
            AdaptiveCard fileDetailCard = null;

            try
            {
                fileDetailCard = await GraphHelper.GetFileDetailCard(accessToken.Token, fileId);
            }
            catch (Microsoft.Graph.ServiceException ex)
            {
                if (ex.Error.Code == "UnknownError" && ex.Message.Contains("Invalid Hostname"))
                {
                    // retry
                    fileDetailCard = await GraphHelper.GetFileDetailCard(accessToken.Token, fileId);
                }
                else
                {
                    throw;
                }
            }

            reply.Attachments = new List<Attachment>()
            {
                new Attachment() { ContentType = AdaptiveCard.ContentType, Content = fileDetailCard }
            };

            await context.PostAsync(reply);
        }

        private async Task PromptForApprovalRequest(IDialogContext context, IAwaitable<GetTokenResponse> tokenResponse)
        {
            //await ShowTyping(context, activity);
            var accessToken = await tokenResponse;
            var userId = context.ConversationData.GetValueOrDefault<string>("approvalRequestor");

            ClearConversationData(context, "approvalRequestor");

            if (accessToken == null || string.IsNullOrEmpty(accessToken.Token))
            {
                await context.PostAsync(Apologize("I could not get an access token"));
                return;
            }

            if (string.IsNullOrEmpty(userId))
            {
                await context.PostAsync(Apologize("I could not find a user ID in the conversation state"));
                return;
            }

            var statusCardList = await ApprovalStatusHelper.GetApprovalsForUserCard(accessToken.Token, userId);
            if (statusCardList == null)
            {
                await context.PostAsync("I'm sorry, but I didn't find any approvals requested by you.");
            }
            else
            {
                var reply = context.MakeMessage();

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

                await context.PostAsync(reply);
            }
        }

        private async Task SendApprovalRequest(IDialogContext context, IAwaitable<GetTokenResponse> tokenResponse)
        {
            var accessToken = await tokenResponse;
            var userId = context.ConversationData.GetValueOrDefault<string>("approvalRequestor");
            var fileId = context.ConversationData.GetValueOrDefault<string>("selectedFile");
            var approvers = context.ConversationData.GetValueOrDefault<string[]>("approvers");

            ClearConversationData(context, "approvalRequestor", "selectedFile", "approvers");

            if (accessToken == null || string.IsNullOrEmpty(accessToken.Token))
            {
                await context.PostAsync(Apologize("I could not get an access token"));
                return;
            }

            if (string.IsNullOrEmpty(userId))
            {
                await context.PostAsync(Apologize("I could not find a user ID in the conversation state"));
                return;
            }

            if (string.IsNullOrEmpty(fileId))
            {
                await context.PostAsync(Apologize("I could not find a selected file ID in the conversation state"));
                return;
            }

            if (string.IsNullOrEmpty(fileId))
            {
                await context.PostAsync(Apologize("I could not find list of approvers in the conversation state"));
                return;
            }

            try
            {
                await ApprovalRequestHelper.SendApprovalRequest(accessToken.Token, userId, fileId, approvers);
            }
            catch (Microsoft.Graph.ServiceException ex)
            {
                if (ex.Error.Code == "UnknownError" && ex.Message.Contains("Invalid Hostname"))
                {
                    // retry
                    await ApprovalRequestHelper.SendApprovalRequest(accessToken.Token, userId, fileId, approvers);
                }
                else
                {
                    throw;
                }
            }

            await context.PostAsync(@"I've sent the request. You can check the status of your request by typing ""check status"".");
        }

        private async Task GetApprovalStatus(IDialogContext context, IAwaitable<GetTokenResponse> tokenResponse)
        {
            //await ShowTyping(context, activity);
            var approvalId = context.ConversationData.GetValueOrDefault<string>("selectedApproval");
            ClearConversationData(context, "selectedApproval");

            if (string.IsNullOrEmpty(approvalId))
            {
                await context.PostAsync(Apologize("I could not find a selected approval ID in the conversation state"));
                return;
            }

            var statusCard = await ApprovalStatusHelper.GetApprovalStatusCard(approvalId);

            await context.PostAsync("Here's what I found:");

            var reply = context.MakeMessage();
            reply.Attachments = new List<Attachment>()
            {
                new Attachment() { ContentType = AdaptiveCard.ContentType, Content = statusCard }
            };

            await context.PostAsync(reply);
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

        private async Task ResetDemo(IDialogContext context, Activity activity)
        {
            // Remove any pending approvals
            await DatabaseHelper.DeleteAllUserApprovals(activity.From.Id);

            context.UserData.Clear();
        }

        private GetTokenDialog CreateGetTokenDialog()
        {
            return new GetTokenDialog(
                ConnectionName,
                $"Please sign in to ApprovalBot to proceed.",
                "Sign In",
                2,
                "Hmm. Something went wrong, let's try again.");
        }

        private string Apologize(string moreInfo)
        {
            return $"I'm sorry, I seem to have gotten my circuits crossed. Please tell a human that ${moreInfo}.";
        }
    }
}