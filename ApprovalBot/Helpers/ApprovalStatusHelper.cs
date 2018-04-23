using ApprovalBot.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AdaptiveCards;

namespace ApprovalBot.Helpers
{
    public class ApprovalStatusHelper
    {
        public static async Task<AdaptiveCard> GetApprovalsForUserCard(string accessToken, string userId)
        {
            // Get all approvals requested by current user
            var approvals = await DatabaseHelper.GetApprovalsAsync(a => a.Requestor == userId);

            if (approvals.Count() == 0)
            {
                // Return a simple message
                return null;
            }
            else if (approvals.Count() == 1)
            {
                // Return status card of the only approval
                return GetApprovalStatusCard(approvals.First());
            }
            else
            {
                var approvalListCard = new AdaptiveCard();

                approvalListCard.Body.Add(new AdaptiveTextBlock()
                {
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium,
                    Text = "Select an approval request to see its status"
                });

                var approvalListContainer = new AdaptiveContainer()
                {
                    Separator = true,
                    Style = AdaptiveContainerStyle.Emphasis
                };

                // Prompt user to choose an approval
                foreach (Approval approval in approvals)
                {
                    var approvalColumnSet = new AdaptiveColumnSet()
                    {
                        SelectAction = new AdaptiveSubmitAction()
                        {
                            Title = approval.File.Name,
                            DataJson = $@"{{ ""cardAction"": ""{CardActionTypes.SelectApproval}"", ""selectedApproval"": ""{approval.Id}"" }}"
                        }
                    };

                    // Get thumbnail
                    var thumbnailUri = await GraphHelper.GetFileThumbnailDataUri(accessToken, approval.File.Id);

                    var fileThumbCol = new AdaptiveColumn()
                    {
                        Width = AdaptiveColumnWidth.Auto
                    };

                    fileThumbCol.Items.Add(new AdaptiveImage()
                    {
                        Url = new Uri(thumbnailUri),
                        AltText = "File thumbnail",
                        Size = AdaptiveImageSize.Small
                    });

                    approvalColumnSet.Columns.Add(fileThumbCol);

                    var fileNameCol = new AdaptiveColumn()
                    {
                        Width = AdaptiveColumnWidth.Stretch
                    };

                    fileNameCol.Items.Add(new AdaptiveTextBlock()
                    {
                        Size = AdaptiveTextSize.Medium,
                        Text = approval.File.Name
                    });

                    fileNameCol.Items.Add(new AdaptiveTextBlock()
                    {
                        IsSubtle = true,
                        Spacing = AdaptiveSpacing.None,
                        Text = $"Requested: {approval.RequestDate.ToString()}"
                    });

                    approvalColumnSet.Columns.Add(fileNameCol);

                    approvalListContainer.Items.Add(approvalColumnSet);
                }

                approvalListCard.Body.Add(approvalListContainer);

                return approvalListCard;
            }
        }

        public static async Task<AdaptiveCard> GetApprovalStatusCard(string approvalId)
        {
            var approval = await DatabaseHelper.GetApprovalAsync(approvalId);
            return GetApprovalStatusCard(approval);
        }

        public static AdaptiveCard GetApprovalStatusCard(Approval approval)
        {
            var statusCard = new AdaptiveCard();

            statusCard.Body.Add(new AdaptiveTextBlock()
            {
                Weight = AdaptiveTextWeight.Bolder,
                Size = AdaptiveTextSize.Medium,
                Text = $"Status for {approval.File.Name}"
            });

            foreach (var approver in approval.Approvers)
            {
                var approverColumnSet = new AdaptiveColumnSet();

                var emailAddressColumn = new AdaptiveColumn()
                {
                    Width = AdaptiveColumnWidth.Stretch
                };

                emailAddressColumn.Items.Add(new AdaptiveTextBlock()
                {
                    Text = approver.EmailAddress
                });

                approverColumnSet.Columns.Add(emailAddressColumn);

                var responseColumn = new AdaptiveColumn()
                {
                    Width = AdaptiveColumnWidth.Auto
                };

                responseColumn.Items.Add(ResponseCardTextBlockFromResponse(approver.Response));

                approverColumnSet.Columns.Add(responseColumn);

                statusCard.Body.Add(approverColumnSet);

                if (!string.IsNullOrEmpty(approver.ResponseNote))
                {
                    var notesContainer = new AdaptiveContainer()
                    {
                        Style = AdaptiveContainerStyle.Emphasis
                    };

                    notesContainer.Items.Add(new AdaptiveTextBlock()
                    {
                        Weight = AdaptiveTextWeight.Bolder,
                        Text = "Note:"
                    });

                    notesContainer.Items.Add(new AdaptiveTextBlock()
                    {
                        Text = approver.ResponseNote,
                        Wrap = true
                    });

                    statusCard.Body.Add(notesContainer);
                }
            }

            return statusCard;
        }

        private static AdaptiveTextBlock ResponseCardTextBlockFromResponse(ResponseStatus response)
        {
            var textBlock = new AdaptiveTextBlock();

            switch (response)
            {
                case ResponseStatus.Approved:
                    textBlock.Color = AdaptiveTextColor.Good;
                    textBlock.Text = "Approved";
                    break;
                case ResponseStatus.Rejected:
                    textBlock.Color = AdaptiveTextColor.Attention;
                    textBlock.Text = "Rejected";
                    break;
                default:
                    textBlock.Color = AdaptiveTextColor.Warning;
                    textBlock.Text = "Not responded";
                    break;
            }

            return textBlock;
        }
    }
}