using System.ComponentModel.DataAnnotations;

namespace IPK25_CHAT.structs;

public struct UserProperty()
{
    [RegularExpression(@"^[a-zA-Z0-9_]{1,65535}$")]
    public string? MessageId;

    [RegularExpression(@"^[a-zA-Z0-9_]{1,20}$")]
    public string? Username { get; set; }
    
    [RegularExpression(@"^[a-zA-Z0-9_]{1,20}$")]
    public string? ChanelId;
    
    [RegularExpression(@"^[a-zA-Z0-9_]{1,128}$")]
    public string? Secret;
    
    [RegularExpression(@"^[\x21-\x7E]{1,20}$")]
    public string? DisplayName;
    
    [RegularExpression(@"^[\x0A\x20-\x7E]{1,6000}$")]
    public string? MessageContent;
}