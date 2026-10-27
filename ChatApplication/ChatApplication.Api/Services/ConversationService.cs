
using System.Data.Common;
using System.Linq.Expressions;
using System.Text.Json;
using ChatApplication.Api.Models.DTOs;
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
            catch (DbUpdateException)
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

    public async Task<Conversation> CreateGroupConversation(CreateGroupConversationRequest request, string requestId,
     CancellationToken token)
    {
        var defaultCondition = Expression.Equal(Expression.Constant(1), Expression.Constant(1));
        var parameter = Expression.Parameter(typeof(User));

        Expression memberCondition = Expression.Constant(false);
        memberCondition = Expression.OrElse(memberCondition,
        Expression.Equal(Expression.Property(parameter, nameof(User.Id)), Expression.Constant(request.UserId)));
        foreach (var member in request.Members)
        {
            memberCondition = Expression.OrElse(memberCondition,
            Expression.Equal(Expression.Property(parameter, nameof(User.Id)), Expression.Constant(member)));
        }
        defaultCondition = Expression.AndAlso(defaultCondition, memberCondition);
        var whereExpression = Expression.Lambda<Func<User, bool>>(defaultCondition, parameter);

        var memberValidation = await chatApplicationDbContext.Users.Where(whereExpression)
        .ToListAsync(token);
        if (memberValidation.Count != (1 + request.Members.Count()))
            throw new KeyNotFoundException("Some users do not exist.");
        else
        {
            var existingRequest = await chatApplicationDbContext
            .IdempotencyRequests.FirstOrDefaultAsync(e => e.Key == requestId, token);
            if (existingRequest != null)
                return JsonSerializer.Deserialize<Conversation>(existingRequest.ResponseBody)!;
            else
            {
                try
                {
                    using (var transaction = chatApplicationDbContext.Database.BeginTransaction())
                    {
                        var conversation = new Conversation
                        {
                            ConversationType = ConversationType.Group,
                            Name = request.GroupName,
                            CreatedBy = request.UserId.ToString(),
                        };
                        var idempotencyRequest = new IdempotencyRequest
                        {
                            Key = requestId,
                            Operation = "CreateGroupConversation",
                            UserId = request.UserId,
                            ResponseBody = JsonSerializer.Serialize(conversation),
                            RequestHash = "",
                            ResponseStatusCode = 201
                        };
                        chatApplicationDbContext.IdempotencyRequests.Add(idempotencyRequest);

                        chatApplicationDbContext.Conversations.Add(conversation);
                        chatApplicationDbContext.ConversationMembers.Add(
                            new ConversationMember
                            {
                                ConversationId = conversation.Id,
                                UserId = request.UserId
                            }
                        );
                        foreach (var member in request.Members)
                        {
                            chatApplicationDbContext.ConversationMembers.Add(
                                new ConversationMember
                                {
                                    ConversationId = conversation.Id,
                                    UserId = member
                                }
                            );
                        }

                        await chatApplicationDbContext.SaveChangesAsync(token);
                        return conversation;
                    }

                }
                catch (DbUpdateException)
                {
                    existingRequest = await chatApplicationDbContext
                                .IdempotencyRequests.FirstOrDefaultAsync(e => e.Key == requestId, token);
                    if (existingRequest != null)
                        return JsonSerializer.Deserialize<Conversation>(existingRequest.ResponseBody)!;

                    throw;
                }

            }
        }
    }

    private Conversation? FindDirect(Guid user1Id, Guid user2Id)
    {
        return chatApplicationDbContext.Conversations.FirstOrDefault(e => e.DirectConversationUser1Id == user1Id &&
        e.DirectConversationUser2Id == user2Id);
    }
}