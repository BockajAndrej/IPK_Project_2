using System.ComponentModel.DataAnnotations;

namespace IPK25_CHAT.structs;

public struct UserProperty()
{
    [RegularExpression(@"^[a-zA-Z0-9_]{1,65535}$")]
    public int MessageId = 0;

    [RegularExpression(@"^[a-zA-Z0-9_]{1,20}$")]
    public string Username { get; set; } = null!;

    [RegularExpression(@"^[a-zA-Z0-9_]{1,20}$")]
    public string ChanelId = null!;
    
    [RegularExpression(@"^[a-zA-Z0-9_]{1,128}$")]
    public string Secret = null!;
    
    [RegularExpression(@"^[\x21-\x7E]{1,20}$")]
    public string DisplayName = null!;
    
    [RegularExpression(@"^[\x0A\x20-\x7E]{1,6000}$")]
    public string MessageContent = null!;
}