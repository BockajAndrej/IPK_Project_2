// Vypracoval: Andrej Bockaj
// xlogin: xbockaa00

using IPK25_CHAT;
using IPK25_CHAT.FSM;
using IPK25_CHAT.structs;

//TODO: zmenit hodnoty na null (tie nedefaultne)
ProgProperty progProperty = new ProgProperty(null,null,4567,250,3);

if (!Input.Parser(args, ref progProperty))
    return 0;

if(progProperty.IsTcp == true)
{
    TcpFsm tcpFsm = new TcpFsm(progProperty);
    await tcpFsm.RunClient();
}
else
{
    UdpFsm udpFsm = new UdpFsm(progProperty);
    await udpFsm.RunClient();
}

return 0;