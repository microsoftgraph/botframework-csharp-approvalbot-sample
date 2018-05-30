using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using AdaptiveCards;
using ApprovalBot.Models;
using ApprovalBot.OutlookAdaptive;
using Microsoft.Graph;

namespace ApprovalBot.Helpers
{
    public static class ApprovalRequestHelper
    {
        private static readonly string originatorId = ConfigurationManager.AppSettings["OriginatorId"];
        private static readonly string messageSender = ConfigurationManager.AppSettings["SenderEmail"];
        private static readonly string actionBaseUrl = ConfigurationManager.AppSettings["AppRootUrl"];
        private static readonly string ngrokUrl = ConfigurationManager.AppSettings["NgrokRootUrl"];

        public static async Task SendApprovalRequest(string accessToken, string userId, string fileId, string[] approvers)
        {
            // Get file info
            var fileInfo = await GraphHelper.GetFileInfo(accessToken, fileId);

            // Info on requestor
            var requestor = await GraphHelper.GetUser(accessToken);

            // Requestor pic URI
            var requestorPicUri = await GraphHelper.GetUserPhotoDataUri(accessToken, "48x48");

            // File thumbnail URI
            var thumbnailUri = await GraphHelper.GetFileThumbnailDataUri(accessToken, fileInfo.Id);

            // Create an approval to store in the database
            var approval = new Approval()
            {
                Requestor = userId,
                File = fileInfo,
                Approvers = new List<ApproverInfo>(),
                RequestDate = DateTimeOffset.UtcNow
            };

            // Add approvers
            foreach (string approver in approvers)
            {
                approval.Approvers.Add(new ApproverInfo()
                {
                    EmailAddress = approver,
                    Response = Models.ResponseStatus.NotResponded,
                    ResponseNote = string.Empty
                });
            }

            // Add to database
            var dbApproval = await DatabaseHelper.CreateApprovalAsync(approval);

            // Build and send card for each recipient
            foreach (string approver in approvers)
            {
                var approvalRequestCard = BuildRequestCard(requestor, requestorPicUri,
                    fileInfo, thumbnailUri, approver, dbApproval.Id);

                await GraphHelper.SendRequestCard(accessToken, approvalRequestCard, approver, messageSender);
            }
        }

        private static AdaptiveCard BuildRequestCard(User requestor, string requestorPicUri, ApprovalFileInfo fileInfo, string thumbnailUri, string recipient, string approvalId)
        {
            // Build actionable email card
            var approvalRequestCard = new AdaptiveCard();

            // Outlook-specific property on AdaptiveCard
            if (!string.IsNullOrEmpty(originatorId))
            {
                approvalRequestCard.AdditionalProperties.Add("originator", originatorId);
            }

            approvalRequestCard.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Pending Approval",
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Large
            });

            approvalRequestCard.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Requested by"
            });


            var requestorColumnSet = new AdaptiveColumnSet();

            if (!string.IsNullOrEmpty(requestorPicUri))
            {
                var requestorPic = new AdaptiveImage()
                {
                    Style = AdaptiveImageStyle.Person,
                    Url = new Uri(requestorPicUri)
                };

                // Outlook-specific property on Image
                requestorPic.AdditionalProperties.Add("pixelWidth", "48");

                var requestorPicCol = new AdaptiveColumn()
                {
                    Width = AdaptiveColumnWidth.Auto.ToLower()
                };

                requestorPicCol.Items.Add(requestorPic);

                requestorColumnSet.Columns.Add(requestorPicCol);
            }

            var requestorNameCol = new AdaptiveColumn()
            {
                Width = AdaptiveColumnWidth.Stretch.ToLower()
            };

            // Outlook-specific property on Column
            requestorNameCol.AdditionalProperties.Add("verticalContentAlignment", "center");

            requestorNameCol.Items.Add(new AdaptiveTextBlock()
            {
                Size = AdaptiveTextSize.Medium,
                Text = requestor.DisplayName
            });

            requestorNameCol.Items.Add(new AdaptiveTextBlock()
            {
                IsSubtle = true,
                Spacing = AdaptiveSpacing.None,
                Text = requestor.Mail
            });

            requestorColumnSet.Columns.Add(requestorNameCol);

            approvalRequestCard.Body.Add(requestorColumnSet);

            // File info
            approvalRequestCard.Body.Add(new AdaptiveTextBlock()
            {
                Text = "File needing approval",
                Separator = true
            });

            var fileColumnSet = new AdaptiveColumnSet()
            {
                SelectAction = new AdaptiveOpenUrlAction()
                {
                    Title = fileInfo.Name,
                    Url = new Uri(fileInfo.SharingUrl)
                }
            };

            if (!string.IsNullOrEmpty(thumbnailUri))
            {
                var fileThumb = new AdaptiveImage()
                {
                    Url = new Uri(thumbnailUri)
                };

                // Outlook-specific property on Image
                fileThumb.AdditionalProperties.Add("pixelWidth", "48");

                var fileThumbCol = new AdaptiveColumn()
                {
                    Width = AdaptiveColumnWidth.Auto.ToLower()
                };

                fileThumbCol.Items.Add(fileThumb);

                fileColumnSet.Columns.Add(fileThumbCol);
            }

            var fileNameCol = new AdaptiveColumn()
            {
                Width = AdaptiveColumnWidth.Stretch.ToLower()
            };

            // Outlook-specific property on Column
            fileNameCol.AdditionalProperties.Add("verticalContentAlignment", "center");

            fileNameCol.Items.Add(new AdaptiveTextBlock()
            {
                Size = AdaptiveTextSize.Medium,
                Text = fileInfo.Name
            });

            fileNameCol.Items.Add(new AdaptiveTextBlock()
            {
                IsSubtle = true,
                Spacing = AdaptiveSpacing.None,
                Text = "(tap to view)"
            });

            fileColumnSet.Columns.Add(fileNameCol);

            approvalRequestCard.Body.Add(fileColumnSet);

            // Respond button

            // Response form (hiddden initially)
            var responseForm = new AdaptiveCard();
            responseForm.AdditionalProperties.Add("style", "emphasis");

            responseForm.Body.Add(new AdaptiveTextInput()
            {
                Id = "notes",
                IsMultiline = true,
                Placeholder = "Enter any notes for the requestor"
            });

            responseForm.Actions.Add(new AdaptiveHttpAction()
            {
                Title = "Approve",
                Method = AdaptiveHttpActionMethod.POST,
                Url = new Uri($"{(string.IsNullOrEmpty(ngrokUrl) ? actionBaseUrl : ngrokUrl) }/api/responses"),
                Body = $@"{{ ""userEmail"": ""{recipient}"", ""approvalId"": ""{approvalId}"", ""response"": ""approved"", ""notes"": ""{{{{notes.value}}}}"" }}s"
            });

            responseForm.Actions.Add(new AdaptiveHttpAction()
            {
                Title = "Reject",
                Method = AdaptiveHttpActionMethod.POST,
                Url = new Uri($"{(string.IsNullOrEmpty(ngrokUrl) ? actionBaseUrl : ngrokUrl)}/api/responses"),
                Body = $@"{{ ""userEmail"": ""{recipient}"", ""approvalId"": ""{approvalId}"", ""response"": ""rejected"", ""notes"": ""{{{{notes.value}}}}"" }}"
            });

            approvalRequestCard.Actions.Add(new AdaptiveShowCardAction() {
                Title = "Respond",
                Card = responseForm
            });

            return approvalRequestCard;
        }
    }
}