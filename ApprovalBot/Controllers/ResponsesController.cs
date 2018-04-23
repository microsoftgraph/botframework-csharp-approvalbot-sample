using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using AdaptiveCards;
using ApprovalBot.Helpers;
using ApprovalBot.Models;
using Microsoft.O365.ActionableMessages.Utilities;

namespace ApprovalBot.Controllers
{
    public class ResponsesController : ApiController
    {
        private readonly string baseUrl = ConfigurationManager.AppSettings["AppRootUrl"];
        private readonly string amSender = ConfigurationManager.AppSettings["SenderEmail"];
        public async Task<IHttpActionResult> PostResponse(ActionableEmailResponse response)
        {
            // Validate the authorization header
            bool isTokenValid = await ValidateAuthorizationHeader(Request.Headers.Authorization,
                baseUrl, response.UserEmail);
            if (!isTokenValid)
            {
                return Unauthorized();
            }

            // Get the approval request
            var approval = await DatabaseHelper.GetApprovalAsync(response.ApprovalId);
            if (approval == null)
            {
                return BadRequest("Invalid approval ID");
            }

            // Find the user in approvers
            bool userIsAnApprover = false;
            foreach(var approver in approval.Approvers)
            {
                if (approver.EmailAddress == response.UserEmail)
                {
                    userIsAnApprover = true;
                    approver.Response = response.Response == "approved" ? ResponseStatus.Approved : ResponseStatus.Rejected;
                    approver.ResponseNote = response.Notes;
                }
            }

            if (userIsAnApprover)
            {
                // Update database
                await DatabaseHelper.UpdateApprovalAsync(approval.Id, approval);
                return GenerateResponseCard(approval);
            }

            return BadRequest("User is not an approver");
        }

        private IHttpActionResult GenerateResponseCard(Approval approval)
        {
            var responseCard = ApprovalStatusHelper.GetApprovalStatusCard(approval);

            // Modify card for this use case
            responseCard.Body.Insert(0, new AdaptiveTextBlock()
            {
                Text = $"Response status as of {DateTimeOffset.UtcNow}:"
            });

            responseCard.Body.Insert(0, new AdaptiveTextBlock()
            {
                Text = "Thanks for responding. Your response has been recorded."
            });

            HttpResponseMessage refreshCardResponse = new HttpResponseMessage(HttpStatusCode.OK);
            refreshCardResponse.Headers.Add("CARD-UPDATE-IN-BODY", "true");

            refreshCardResponse.Content = new StringContent(responseCard.ToJson(), System.Text.Encoding.UTF8, "application/json");
            return ResponseMessage(refreshCardResponse);
        }

        private async Task<bool> ValidateAuthorizationHeader(AuthenticationHeaderValue authHeader, string targetUrl, string userId)
        {
            // Validate that we have a bearer token
            if (authHeader == null ||
                !string.Equals(authHeader.Scheme, "bearer", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(authHeader.Parameter))
            {
                return false;
            }

            // Validate the token
            ActionableMessageTokenValidator validator = new ActionableMessageTokenValidator();
            ActionableMessageTokenValidationResult result = await validator.ValidateTokenAsync(authHeader.Parameter, targetUrl);
            if (!result.ValidationSucceeded)
            {
                return false;
            }

            // Token is valid, now check the sender and action performer
            // Both should equal the user
            if (!string.Equals(result.ActionPerformer, userId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(result.Sender, string.IsNullOrEmpty(amSender) ? userId : amSender, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
