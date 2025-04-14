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
        Assert.True(TcpDecoder.Parser(new [] { "-t", "tcp", "-s", "127.0.0.1"}, ref property));
        Assert.Equal(true,  property.IsTcp);
        Assert.Equal("127.0.0.1",  property.Url);
    }
    
    [Fact]
    public void Test2()
    {
        Assert.True(TcpDecoder.Parser(new [] { "-t", "tcp", "-s", "localhost", "-p", "4567"}, ref property));
        Assert.Equal(true,  property.IsTcp);
        Assert.Equal("localhost",  property.Url);
        Assert.Equal(4567,  property.Port);
    }
}