// Vypracoval: Andrej Bockaj
// xlogin: xbockaa00

using IPK25_CHAT;
using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

//TODO: zmenit hodnoty na null (tie nedefaultne)
ProgProperty progProperty = new ProgProperty(null,null,4567,250,3);

TcpDecoder.Parser(args, ref progProperty);

if(progProperty.IsTcp == true)
{
    TcpFsm tcpFsm = new TcpFsm(progProperty);
    await tcpFsm.RunClient();
}
else
{
    TcpFsm udpFsm = new TcpFsm(progProperty);
    await udpFsm.RunClient();
}

return 0;