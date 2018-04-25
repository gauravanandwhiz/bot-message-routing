﻿using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Underscore.Bot.Utils
{
    /// <summary>
    /// Utility methods.
    /// </summary>
    public class MessageRoutingUtils
    {
        public struct ConnectorClientAndMessageBundle
        {
            public ConnectorClient connectorClient;
            public IMessageActivity messageActivity;
        }

        /// <summary>
        /// Constructs a ConversationReference instance using the sender (from) of the given activity.
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="withTimestamps">If true, will construct a ConversationReference instance with timestamps
        /// instead of the regular ConversationReference. True by default.</param>
        /// <returns>A newly created ConversationReference instance.</returns>
        public static ConversationReference CreateSenderConversationReference(IActivity activity, bool withTimestamps = true)
        {
            if (withTimestamps)
            {
                return new ConversationReference(
                    null,
                    activity.From,
                    null,
                    activity.Conversation,
                    activity.ChannelId,
                    activity.ServiceUrl);
            }

            return new ConversationReference(
                null,
                activity.From,
                null,
                activity.Conversation,
                activity.ChannelId,
                activity.ServiceUrl);
        }

        /// <summary>
        /// Constructs a ConversationReference instance using the recipient of the given activity.
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="withTimestamps">If true, will construct a ConversationReference instance with timestamps
        /// instead of the regular ConversationReference. True by default.</param>
        public static ConversationReference CreateRecipientConversationReference(IActivity activity, bool withTimestamps = true)
        {
            if (withTimestamps)
            {
                return new ConversationReference(
                    null,
                    null,
                    activity.Recipient,
                    activity.Conversation,activity.ChannelId,
                    activity.ServiceUrl);
            }

            return new ConversationReference(
                null,
                null,
                activity.Recipient,
                activity.Conversation,
                activity.ChannelId,
                activity.ServiceUrl);
        }

        /// <summary>
        /// Replies to the given activity with the given message.
        /// </summary>
        /// <param name="activity">The activity to reply to.</param>
        /// <param name="message">The message.</param>
        public static async Task ReplyToActivityAsync(Activity activity, string message)
        {
            if (activity != null && !string.IsNullOrEmpty(message))
            {
                Activity replyActivity = activity.CreateReply(message);
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                await connector.Conversations.ReplyToActivityAsync(replyActivity);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Either the activity is null or the message is empty - Activity: {activity}; message: {message}");
            }
        }

        /// <summary>
        /// Creates a connector client with the given message activity for the given ConversationReference as the
        /// recipient. If this ConversationReference has an ID of a specific user (ChannelAccount is valid), then
        /// the that user is set as the recipient. Otherwise, the whole channel is addressed.
        /// </summary>
        /// <param name="serviceUrl">The service URL of the channel of the ConversationReference to send the message to.</param>
        /// <param name="newMessageActivity">The message activity to send.</param>
        /// <returns>A bundle containing a newly created connector client (that is used to send
        /// the message and the message activity (the content of the message).</returns>
        public static ConnectorClientAndMessageBundle CreateConnectorClientAndMessageActivity(
            string serviceUrl, IMessageActivity newMessageActivity)
        {
            ConnectorClient newConnectorClient = new ConnectorClient(new Uri(serviceUrl));

            ConnectorClientAndMessageBundle bundle = new ConnectorClientAndMessageBundle()
            {
                connectorClient = newConnectorClient,
                messageActivity = newMessageActivity
            };

            return bundle;
        }

        /// <summary>
        /// For convenience.
        /// </summary>
        /// <param name="ConversationReferenceToMessage">The ConversationReference to send the message to.</param>
        /// <param name="messageText">The message text content.</param>
        /// <param name="senderAccount">The channel account of the sender.</param>
        /// <returns>A bundle containing a newly created connector client (that is used to send
        /// the message and the message activity (the content of the message).</returns>
        public static ConnectorClientAndMessageBundle CreateConnectorClientAndMessageActivity(
            ConversationReference ConversationReferenceToMessage, string messageText, ChannelAccount senderAccount)
        {
            IMessageActivity newMessageActivity = Activity.CreateMessageActivity();
            newMessageActivity.Conversation = ConversationReferenceToMessage.Conversation;
            newMessageActivity.Text = messageText;

            if (senderAccount != null)
            {
                newMessageActivity.From = senderAccount;
            }

            if (ConversationReferenceToMessage.User!= null)
            {
                newMessageActivity.Recipient = ConversationReferenceToMessage.User;
            }

            return CreateConnectorClientAndMessageActivity(ConversationReferenceToMessage.ServiceUrl, newMessageActivity);
        }

        /// <summary>
        /// Strips the mentions from the message text.
        /// </summary>
        /// <param name="messageActivity"></param>
        /// <returns>The stripped message.</returns>
        public static string StripMentionsFromMessage(IMessageActivity messageActivity)
        {
            string strippedMessage = messageActivity.Text;

            if (!string.IsNullOrEmpty(strippedMessage))
            {
                Mention[] mentions = messageActivity.GetMentions();

                foreach (Mention mention in mentions)
                {
                    string mentionText = mention.Text;

                    if (!string.IsNullOrEmpty(mentionText))
                    {
                        while (strippedMessage.Contains(mentionText))
                        {
                            strippedMessage = strippedMessage.Remove(
                                strippedMessage.IndexOf(mentionText), mentionText.Length);
                        }

                    }
                }

                strippedMessage = strippedMessage.Trim();
            }

            return strippedMessage;
        }

        /// <summary>
        /// Checks if the given ConversationReference contains the ChannelAccount instance for a bot.
        /// </summary>
        /// <param name="conversationReference">The ConversationReference instance to check.</param>
        /// <returns>True, if the ChannelAccount instance for the bot is not null. False otherwise.</returns>
        public static bool IsBot(ConversationReference conversationReference)
        {
            return (conversationReference.Bot != null);
        }

        /// <summary>
        /// Resolves the non-null ChannelAccount instance from the given ConversationReference.
        /// </summary>
        /// <param name="conversationReference"></param>
        /// <returns>The non-null ChannelAccount (user or bot) or null, if both are null.</returns>
        public static ChannelAccount GetChannelAccount(ConversationReference conversationReference)
        {
            if (conversationReference.User != null)
            {
                return conversationReference.User;
            }

            if (conversationReference.Bot != null)
            {
                return conversationReference.Bot;
            }

            return null;
        }

        /// <summary>
        /// Compares the ChannelAccount instances of the given ConversationReferences.
        /// </summary>
        /// <param name="conversationReference1"></param>
        /// <param name="conversationReference2"></param>
        /// <returns>True, if the ChannelAccount instances (IDs) match. False otherwise.</returns>
        public static bool HasMatchingChannelAccounts(
            ConversationReference conversationReference1, ConversationReference conversationReference2)
        {
            if (conversationReference1.Bot != null && conversationReference2.Bot != null)
            {
                return conversationReference1.Bot.Id.Equals(conversationReference2.Bot.Id);
            }
            else if (conversationReference1.User != null && conversationReference2.User != null)
            {
                return conversationReference1.User.Id.Equals(conversationReference2.User.Id);
            }

            return false;
        }

        public static IList<ConversationReference> ResolveConversationReferencesWithMatchingChannelAccount(
            ConversationReference conversationReferenceToFind, IList<ConversationReference> conversationReferenceCandidates)
        {
            IList<ConversationReference> matchingParties = null;
            IEnumerable<ConversationReference> foundConversationReferences = null;

            try
            {
                foundConversationReferences = conversationReferenceCandidates.Where(conversationReference =>
                        HasMatchingChannelAccounts(conversationReferenceToFind, conversationReference));
            }
            catch (ArgumentNullException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to find a conversation reference: {e.Message}");
            }
            catch (InvalidOperationException e)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to find a conversation reference: {e.Message}");
            }

            if (foundConversationReferences != null)
            {
                matchingParties = foundConversationReferences.ToArray();
            }

            return matchingParties;
        }
    }
}