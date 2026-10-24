
using System.Data.Common;
using ChatApplication.Api.Models.Entities;
using ChatApplication.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChatApplication.Api.Services;

public class ConversationService
{
    private readonly ChatApplicationDbContext chatApplicationDbContext;

    public ConversationService(ChatApplicationDbContext chatApplicationDbContext)
    {
        this.chatApplicationDbContext = chatApplicationDbContext;
    }

    public async Task<Conversation> CreateDirectConversation(Guid userId, Guid otherUserId, CancellationToken token)
    {
        if (userId == otherUserId)
            throw new ArgumentException(
                "A direct conversation requires two different users.");

        var users = await chatApplicationDbContext
        .Users.Where(e => e.Id == userId || e.Id == otherUserId)
        .ToListAsync(token);

        if (users.Count != 2)
            throw new KeyNotFoundException("One or both users do not exist.");

        var user1Id = userId.CompareTo(otherUserId) > 0 ? otherUserId : userId;
        var user2Id = userId.CompareTo(otherUserId) > 0 ? userId : otherUserId;

        var conversation = FindDirect(user1Id, user2Id);
        if (conversation != null)
            return conversation;

        else
        {
            //create conversation
            conversation = new Conversation
            {
                ConversationType = ConversationType.Direct,
                CreatedBy = user1Id.ToString(),
                Name = $"{user1Id}_{user2Id}",
                DirectConversationUser1Id = user1Id,
                DirectConversationUser2Id = user2Id,
            };
            chatApplicationDbContext.Conversations.Add(conversation);

            chatApplicationDbContext.ConversationMembers.AddRange(new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = user1Id
            },
            new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = user2Id
            });

            try
            {
                await chatApplicationDbContext.SaveChangesAsync(token);
                return conversation;
            }
            catch (DbException)
            {
                //may be another transaction added conversation
                chatApplicationDbContext.ChangeTracker.Clear();

                conversation = this.FindDirect(user1Id, user2Id);

                if (conversation != null)
                    return conversation;

                throw;
            }
        }

    }

    private Conversation? FindDirect(Guid user1Id, Guid user2Id)
    {
        return chatApplicationDbContext.Conversations.FirstOrDefault(e => e.DirectConversationUser1Id == user1Id &&
        e.DirectConversationUser2Id == user2Id);
    }
}