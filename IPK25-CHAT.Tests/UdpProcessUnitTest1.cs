using IPK25_CHAT.Encryption;
using IPK25_CHAT.structs;

namespace IPK25_CHAT.Tests;

public class UdpProcessUnitTest1
{
    private UdpDecoder decoder;
    
    public UdpProcessUnitTest1()
    {
        decoder = new UdpDecoder();
    }
    
    [Fact]
    public void Test1()
    {
        Assert.Equal(decoder.ProcessMsg(new byte[] {0x00, 0x00, 0x00}), MessageTypes.Confirm);
    }

    [Fact]
    public void Test2()
    {
        Assert.Equal(decoder.ProcessMsg(new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00}), MessageTypes.ReplyNok);
        Assert.Equal(decoder.ProcessMsg(new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x6E,0x65,0x6E,0x65, 0x00}), MessageTypes.ReplyNok);
    }
    [Fact]
    public void Test3()
    {
        Assert.Equal(decoder.ProcessMsg(new byte[] {0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x00}), MessageTypes.ReplyOk);
    }
    [Fact]
    public void Test6()
    {
        Assert.Equal(decoder.ProcessMsg(new byte[] {0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00}), MessageTypes.Msg);
    }
    [Fact]
    public void Test7()
    {
        Assert.Equal(decoder.ProcessMsg(new byte[] {0xFE, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00}), MessageTypes.Err);
    }
    [Fact]
    public void Test8()
    {
        Assert.Equal(decoder.ProcessMsg(new byte[] {0xFF, 0x00, 0x00, 0x01, 0x00}), MessageTypes.Bye);
    }
    [Fact]
    public void Test9()
    {
        Assert.Equal(decoder.ProcessMsg(new byte[] {0xFD, 0x00, 0x00}), MessageTypes.Ping);
    }
}