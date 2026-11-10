
using System.Globalization;
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

    public async Task<Conversation> CreateGroupConversation(CreateGroupConversationRequest request, string requestId, string operationName,
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
            var requestHash = request.GetHash();
            var existingRequest = await chatApplicationDbContext
            .IdempotencyRequests.FirstOrDefaultAsync(e => e.Key == requestId && e.UserId == request.UserId
            && e.Operation == operationName && e.RequestHash == requestHash, token);
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
                            Operation = operationName,
                            UserId = request.UserId,
                            ResponseBody = JsonSerializer.Serialize(conversation),
                            RequestHash = request.GetHash(),
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
                        await transaction.CommitAsync(token);
                        return conversation;
                    }

                }
                catch (DbUpdateException)
                {
                    chatApplicationDbContext.ChangeTracker.Clear();
                    existingRequest = await chatApplicationDbContext
                                .IdempotencyRequests.FirstOrDefaultAsync(e => e.Key == requestId && e.UserId == request.UserId
            && e.Operation == operationName && e.RequestHash == requestHash, token);
                    if (existingRequest != null)
                        return JsonSerializer.Deserialize<Conversation>(existingRequest.ResponseBody)!;

                    throw;
                }

            }
        }
    }


    public async Task<IEnumerable<ConversationResponse>> UserConversations(string userId, CancellationToken token)
    {
        var result = new List<ConversationResponse>();

        return await this.chatApplicationDbContext.Conversations
        .Join(this.chatApplicationDbContext.ConversationMembers, c => c.Id, cm => cm.ConversationId,
        (c, cm) => new { c, cm })
        .Where(r => r.cm.UserId.ToString().ToLower() == userId.ToLower())
        .GroupBy(e => e.c)
        .Select(r => new ConversationResponse
        {
            Id = r.Key.Id,
            ConversationType = r.Key.ConversationType.ToString(),
            CreatedBy = r.Key.CreatedBy,
            Name = r.Key.Name,
            CreatedAt = r.Key.CreatedAt,
            ConversationMemberResponses = r.Select(e => new ConversationMemberResponse
            {
                JoinedAt = e.cm.JoinedAt,
                LeftAt = e.cm.LeftAt,
                UserId = e.cm.UserId
            })
        })
        .ToListAsync(token);
    }

    public async Task<IEnumerable<MessageResponse>> GetDirectMessages(Guid user1, Guid user2, CancellationToken token, string? next)
    {
        var validUserCount = this.chatApplicationDbContext.Users.Count(e => e.Id == user1 || e.Id == user2);
        if (validUserCount != 2)
            throw new KeyNotFoundException("One or both users do not exist.");
        else
        {
            var userId1 = user1.CompareTo(user2) > 0 ? user1 : user2;
            var userId2 = user1.CompareTo(user2) > 0 ? user2 : user1;
            var conversation = FindDirect(userId1, userId2);
            if (conversation != null)
            {
                var filterDateTime = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(next))
                    filterDateTime = DateTime.Parse(next, CultureInfo.InvariantCulture);

                var messages = await this.chatApplicationDbContext.Messages
                .Where(e => e.ConversationId == conversation.Id && e.CreatedAt < filterDateTime)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync(token);

                return messages.Select(m => new MessageResponse
                {
                    Content = m.Content,
                    ClientMessageId = m.ClientMessageId,
                    CreatedAt = m.CreatedAt,
                    Id = m.Id,
                    SenderId = m.SenderId
                });
            }

            return new List<MessageResponse>();
        }
    }

    public async Task<IEnumerable<MessageResponse>> GetGroupMessages(string groupName, Guid userId, CancellationToken token, string? next)
    {
        var conversation = this.chatApplicationDbContext.Conversations.Join(this.chatApplicationDbContext.ConversationMembers,
        c => c.Id, cm => cm.ConversationId, (c, cm) => new { c, cm })
        .FirstOrDefault(e => e.c.Name == groupName && e.cm.UserId == userId);
        if (conversation == null)
            throw new KeyNotFoundException("User doesn't belong to this group");
        else
        {
            var filterDateTime = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(next))
                filterDateTime = DateTime.Parse(next, CultureInfo.InvariantCulture);

            var messages = await this.chatApplicationDbContext.Messages
            .Where(e => e.ConversationId == conversation.c.Id && e.CreatedAt < filterDateTime)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .ToListAsync(token);

            return messages.Select(m => new MessageResponse
            {
                Content = m.Content,
                ClientMessageId = m.ClientMessageId,
                CreatedAt = m.CreatedAt,
                Id = m.Id,
                SenderId = m.SenderId
            });
        }
    }

    public async Task<MessageResponse> CreateConversationMessage(Guid conversationId, SendMessageRequest request, CancellationToken token)
    {
        var isMember = await chatApplicationDbContext.ConversationMembers.AnyAsync(e =>
        e.ConversationId == conversationId &&
        e.UserId == request.SenderId &&
        e.LeftAt == DateTime.MinValue,
        token);
        if (!isMember)
            throw new KeyNotFoundException("Cannot send message to this conversation.");

        var existingConversation = await this.chatApplicationDbContext.Conversations
        .FirstOrDefaultAsync(e => e.Id == conversationId, token);
        if (existingConversation == null)
            throw new KeyNotFoundException("Conversation Id not found");
        else
        {
            var message = await this.chatApplicationDbContext.Messages.FirstOrDefaultAsync(e => e.ConversationId == conversationId
            && e.ClientMessageId == request.ClientMessageId && e.SenderId == request.SenderId, token);
            if (message != null)
                return new MessageResponse
                {
                    Content = message.Content,
                    ClientMessageId = message.ClientMessageId,
                    SenderId = message.SenderId
                };
            else
            {
                //new message
                message = new Message
                {
                    ConversationId = conversationId,
                    ClientMessageId = request.ClientMessageId,
                    Content = request.Content
                };
                this.chatApplicationDbContext.Messages.Add(message);
                try
                {
                    await this.chatApplicationDbContext.SaveChangesAsync(token);
                    return new MessageResponse
                    {
                        Content = message.Content,
                        ClientMessageId = message.ClientMessageId,
                        SenderId = message.SenderId
                    };
                }
                catch (DbUpdateException)
                {
                    chatApplicationDbContext.ChangeTracker.Clear();
                    message = await this.chatApplicationDbContext.Messages.FirstOrDefaultAsync(e => e.ConversationId == conversationId
                    && e.ClientMessageId == request.ClientMessageId && e.SenderId == request.SenderId, token);
                    if (message != null)
                        return new MessageResponse
                        {
                            Content = message.Content,
                            ClientMessageId = message.ClientMessageId,
                            SenderId = message.SenderId
                        };

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