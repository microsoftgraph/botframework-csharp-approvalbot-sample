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
        public static async Task<List<AdaptiveCard>> GetApprovalsForUserCard(string accessToken, string userId)
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
                return new List<AdaptiveCard>() { GetApprovalStatusCard(approvals.First()) };
            }
            else
            {
                var approvalCardList = new List<AdaptiveCard>();

                foreach (var approval in approvals)
                {
                    var approvalDetailCard = new AdaptiveCard();

                    var approvalColumnSet = new AdaptiveColumnSet();

                    // Get thumbnail
                    var thumbnailUri = await GraphHelper.GetFileThumbnailDataUri(accessToken, approval.File.Id);

                    var fileThumbCol = new AdaptiveColumn()
                    {
                        Width = AdaptiveColumnWidth.Auto.ToLower()
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
                        Width = AdaptiveColumnWidth.Stretch.ToLower()
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
                        Text = $"Requested: {TimeZoneHelper.GetAdaptiveDateTimeString(approval.RequestDate)}"
                    });

                    approvalColumnSet.Columns.Add(fileNameCol);

                    approvalDetailCard.Body.Add(approvalColumnSet);

                    approvalDetailCard.Actions.Add(new AdaptiveSubmitAction()
                    {
                        Title = "This one",
                        DataJson = $@"{{ ""cardAction"": ""{CardActionTypes.SelectApproval}"", ""selectedApproval"": ""{approval.Id}"" }}"
                    });

                    approvalCardList.Add(approvalDetailCard);
                }

                return approvalCardList;
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
                    Width = AdaptiveColumnWidth.Stretch.ToLower()
                };

                emailAddressColumn.Items.Add(new AdaptiveTextBlock()
                {
                    Text = approver.EmailAddress
                });

                approverColumnSet.Columns.Add(emailAddressColumn);

                var responseColumn = new AdaptiveColumn()
                {
                    Width = AdaptiveColumnWidth.Auto.ToLower()
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