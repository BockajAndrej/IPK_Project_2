using IPK25_CHAT.structs;

namespace IPK25_CHAT.Tests;

public class InputUnitTest1
{
    private ProgProperty _property;
    
    public InputUnitTest1()
    {
        _property = new ProgProperty();
    }
    
    [Fact]
    public void Test1()
    {
        Assert.True(Input.Parser(new [] { "-t", "tcp", "-s", "127.0.0.1"}, ref _property));
        Assert.Equal(true,  _property.IsTcp);
        Assert.Equal("127.0.0.1",  _property.Url);
    }
    
    [Fact]
    public void Test2()
    {
        Assert.True(Input.Parser(new [] { "-t", "tcp", "-s", "localhost", "-p", "4567"}, ref _property));
        Assert.Equal(true,  _property.IsTcp);
        Assert.Equal("localhost",  _property.Url);
        Assert.Equal(4567,  _property.Port);
    }
    
    [Fact]
    public void Test3()
    {
        Assert.True(Input.Parser(new [] { "-t", "udp", "-s", "localhost", "-p", "4567"}, ref _property));
        Assert.Equal(false,  _property.IsTcp);
        Assert.Equal("localhost",  _property.Url);
        Assert.Equal(4567,  _property.Port);
    }
}