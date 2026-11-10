

using System.Security.Cryptography;
using System.Text;

namespace ChatApplication.Api.Models.DTOs;

public class CreateGroupConversationRequest
{
    public Guid UserId { get; set; }

    public required string GroupName { get; set; }

    public required IEnumerable<Guid> Members { get; set; }

    public string GetHash()
    {
        var members = this.Members.Select(e => e.ToString("N"));
        var objectHash = UserId.ToString("N") + GroupName + string.Join(',', members);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(objectHash));
        return Convert.ToHexString(hash);
    }
}