using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AdaptiveCards;
using ApprovalBot.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApprovalBot.Helpers
{
    public static class GraphHelper
    {
        private static readonly bool LogGraphRequests =
            string.IsNullOrEmpty(ConfigurationManager.AppSettings["LogGraphRequests"]) ? false :
            Convert.ToBoolean(ConfigurationManager.AppSettings["LogGraphRequests"]);

        public static async Task<AdaptiveCard> GetFilePickerCardFromOneDrive(string accessToken)
        {
            var client = await GetAuthenticatedClient(accessToken);

            // Get the first 20 items from the root of user's OneDrive
            var driveItems = await client.Me.Drive.Root.Children.Request()
                .Select("id,name,file")
                .Top(20)
                .GetAsync();

            var fileList = new List<AdaptiveChoice>();

            while (driveItems != null)
            {
                foreach (var item in driveItems)
                {
                    // Only process files
                    if (item.File != null)
                    {
                        fileList.Add(new AdaptiveChoice()
                        {
                            Title = item.Name,
                            Value = item.Id
                        });
                    }
                }

                if (driveItems.NextPageRequest != null)
                {
                    driveItems = await driveItems.NextPageRequest.GetAsync();
                }
                else
                {
                    driveItems = null;
                }
            }

            var pickerCard = new AdaptiveCard();

            if (fileList.Count > 0)
            {
                pickerCard.Body.Add(new AdaptiveTextBlock()
                {
                    Text = "I found these in your OneDrive",
                    Weight = AdaptiveTextWeight.Bolder
                });

                pickerCard.Body.Add(new AdaptiveChoiceSetInput()
                {
                    Id = "selectedFile",
                    IsMultiSelect = false,
                    Style = AdaptiveChoiceInputStyle.Compact,
                    Choices = fileList
                });

                pickerCard.Actions.Add(new AdaptiveSubmitAction()
                {
                    Title = "OK",
                    DataJson = $@"{{ ""cardAction"": ""{CardActionTypes.SelectFile}"" }}"
                });

                return pickerCard;
            }

            return null;
        }

        public static async Task<AdaptiveCard> GetFileDetailCard(string accessToken, string fileId)
        {
            var client = await GetAuthenticatedClient(accessToken);

            // Get the file with thumbnails
            var file = await client.Me.Drive.Items[fileId].Request()
                .Expand("thumbnails")
                .GetAsync();

            // Get people user interacts with regularly
            var potentialApprovers = await client.Me.People.Request()
                // Only want organizational users, and do not want to send back to bot
                .Filter("personType/subclass eq 'OrganizationUser' and displayName ne 'Approval Bot'")
                .Top(10)
                .GetAsync();

            var fileCard = new AdaptiveCard();

            fileCard.Body.Add(new AdaptiveTextBlock()
            {
                Text = file.Name,
                Weight = AdaptiveTextWeight.Bolder
            });

            fileCard.Body.Add(new AdaptiveFactSet()
            {
                Facts = new List<AdaptiveFact>()
                {
                    new AdaptiveFact("Size", $"{file.Size / 1024} KB"),
                    new AdaptiveFact("Last modified", TimeZoneHelper.GetAdaptiveDateTimeString(file.LastModifiedDateTime.Value)),
                    new AdaptiveFact("Last modified by", file.LastModifiedBy.User.DisplayName)
                }
            });

            if (file.Thumbnails != null && file.Thumbnails.Count > 0)
            {
                fileCard.Body.Add(new AdaptiveImage()
                {
                    Size = AdaptiveImageSize.Stretch,
                    AltText = "File thumbnail",
                    Url = new Uri(file.Thumbnails[0].Large.Url)
                });
            }

            var recipientPromptCard = new AdaptiveCard();

            recipientPromptCard.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Who should I ask for approval?",
                Weight = AdaptiveTextWeight.Bolder
            });

            var recipientPicker = new AdaptiveChoiceSetInput()
            {
                Id = "approvers",
                IsMultiSelect = true,
                Style = AdaptiveChoiceInputStyle.Compact
            };

            foreach(var potentialApprover in potentialApprovers)
            {
                recipientPicker.Choices.Add(new AdaptiveChoice()
                {
                    Title = potentialApprover.DisplayName,
                    Value = potentialApprover.ScoredEmailAddresses.First().Address
                });
            }

            recipientPromptCard.Body.Add(recipientPicker);

            recipientPromptCard.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Send Request",
                DataJson = $@"{{ ""cardAction"": ""{CardActionTypes.SendApprovalRequest}"", ""selectedFile"": ""{fileId}"" }}"
            });

            fileCard.Actions.Add(new AdaptiveShowCardAction()
            {
                Title = "Yes",
                Card = recipientPromptCard
            });

            fileCard.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "No",
                DataJson = $@"{{ ""cardAction"": ""{CardActionTypes.WrongFile}"" }}"
            });

            return fileCard;
        }

        public static async Task<ApprovalFileInfo> GetFileInfo(string accessToken, string fileId)
        {
            var client = await GetAuthenticatedClient(accessToken);

            // Get the file with thumbnails
            var file = await client.Me.Drive.Items[fileId].Request()
                .Select("id,name")
                .Expand("thumbnails")
                .GetAsync();

            // Get a sharing link
            var sharingLink = await client.Me.Drive.Items[fileId]
                .CreateLink("view", "organization")
                .Request()
                .PostAsync();

            return new ApprovalFileInfo()
            {
                Id = fileId,
                Name = file.Name,
                SharingUrl = sharingLink.Link.WebUrl,
                ThumbnailUrl = (file.Thumbnails != null && file.Thumbnails.Count > 0) ? file.Thumbnails[0].Small.Url : null
            };
        }

        public static async Task<User> GetUser(string accessToken)
        {
            var client = await GetAuthenticatedClient(accessToken);

            return await client.Me.Request()
                .Select("displayName, mail")
                .GetAsync();
        }

        public static async Task<string> GetUserPhotoDataUri(string accessToken, string size)
        {
            var client = await GetAuthenticatedClient(accessToken);

            try
            {
                var photo = await client.Me.Photos[size].Content.Request().GetAsync();

                var photoStream = new MemoryStream();
                photo.CopyTo(photoStream);

                var photoBytes = photoStream.ToArray();

                return string.Format("data:image/png;base64,{0}",
                    Convert.ToBase64String(photoBytes));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<string> GetFileThumbnailDataUri(string accessToken, string fileId)
        {
            var client = await GetAuthenticatedClient(accessToken);

            try
            {
                var thumbnail = await client.Me.Drive.Items[fileId].Thumbnails["0"]["small"].Content.Request().GetAsync();

                var thumbnailStream = new MemoryStream();
                thumbnail.CopyTo(thumbnailStream);

                var thumbnailBytes = thumbnailStream.ToArray();

                return string.Format("data:image/png;base64,{0}",
                    Convert.ToBase64String(thumbnailBytes));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task SendRequestCard(string accessToken, AdaptiveCard card, string recipient, string sender)
        {
            var toRecipient = new Recipient()
            {
                EmailAddress = new EmailAddress() { Address = recipient }
            };

            var actionableMessage = new Message()
            {
                Subject = "Request for release approval",
                ToRecipients = new List<Recipient>() { toRecipient },
                Body = new ItemBody()
                {
                    ContentType = BodyType.Html,
                    Content = HtmlBodyFromCard(card)
                }
            };

            if (!string.IsNullOrEmpty(sender))
            {
                actionableMessage.From = new Recipient() {
                    EmailAddress = new EmailAddress() { Address = sender }
                };
            }

            var client = await GetAuthenticatedClient(accessToken);

            await client.Me.SendMail(actionableMessage, true).Request().PostAsync();
        }

        private const string htmlBodyTemplate = @"<html>
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
    <script type=""application/adaptivecard+json"">
        {0}
    </script>
</head>
<body>
    <div>You've received a request for file approval. If you cannot see the approval card in this message, please visit the <a href=""https://contoso.com/approvals"">Approval Portal</a> to respond to this request.</div>
</body>
</html>";

        private static string HtmlBodyFromCard(AdaptiveCard card)
        {
            var cardPayload = JsonConvert.SerializeObject(card);
            return string.Format(htmlBodyTemplate, cardPayload);
        }

        private static async Task<GraphServiceClient> GetAuthenticatedClient(string accessToken)
        {
            if (LogGraphRequests)
                return new GraphServiceClient(new DelegateAuthenticationProvider(
                    async (requestMessage) => 
                    {
                        requestMessage.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                    }),
                    new HttpProvider(new LoggingHttpProvider(), true, null));

            return new GraphServiceClient(new DelegateAuthenticationProvider(
                async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }));
        }
    }

    public class LoggingHttpProvider : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Log request
            string requestId = Guid.NewGuid().ToString();
            request.Headers.Add("client-request-id", requestId);
            var requestLog = new GraphLogEntry(request);
            await requestLog.LoadBody(request);
            await DatabaseHelper.AddGraphLog(requestLog);

            try
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                // Log response
                var responseLog = new GraphLogEntry(response);
                await responseLog.LoadBody(response);
                await DatabaseHelper.AddGraphLog(responseLog);

                return response;
            }
            catch (Exception ex)
            {
                await DatabaseHelper.AddGraphLog(new GraphLogEntry(ex, requestId));
                throw;
            }
        }
    }

    public class GraphLogEntry
    {
        public string RequestId { get; set; }
        public string RequestUrl { get; set; }
        public string RequestMethod { get; set; }
        public string Body { get; set; }

        public GraphLogEntry(HttpRequestMessage request)
        {
            RequestId = request.Headers.Where(h => string.Equals(h.Key, "client-request-id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value?.FirstOrDefault();
            RequestUrl = request.RequestUri.ToString();
            RequestMethod = request.Method.ToString();
        }

        public GraphLogEntry(HttpResponseMessage response)
        {
            RequestId = response.Headers.Where(h => string.Equals(h.Key, "client-request-id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Value?.FirstOrDefault();
        }

        public GraphLogEntry(Exception ex, string requestId)
        {
            RequestId = requestId;
            RequestMethod = "EXCEPTION";
            Body = ex.ToString();
        }

        public async Task LoadBody(HttpRequestMessage request)
        {
            if (request.Content != null)
            {
                Body = await request.Content.ReadAsStringAsync();
            }
        }

        public async Task LoadBody(HttpResponseMessage response)
        {
            if (response.Content != null)
            {
                Body = await response.Content.ReadAsStringAsync();
            }
        }
    }
}