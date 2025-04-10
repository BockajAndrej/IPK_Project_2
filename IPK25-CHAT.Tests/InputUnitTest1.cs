using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Tests;

public class InputUnitTest1
{
    private ProgProperty property;
    
    public InputUnitTest1()
    {
        property = new ProgProperty();
    }
    
    [Fact]
    public void Test1()
    {
        Assert.True(Input.Parser(new [] { "-t", "tcp", "-s", "127.0.0.1"}, property));
    }
}