// Vypracoval: Andrej Bockaj
// xlogin: xbockaa00

using IPK25_CHAT;
using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

//TODO: zmenit hodnoty na null (tie nedefaultne)
ProgProperty property = new ProgProperty(true,"127.0.0.1",4567,250,3);

Input.Parser(args, property);

ClientFsm clientFsm = new ClientFsm(property);

await clientFsm.RunClient();


