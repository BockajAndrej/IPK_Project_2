// Vypracoval: Andrej Bockaj
// xlogin: xbockaa00

using IPK25_CHAT;
using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

//TODO: zmenit hodnoty na null (tie nedefaultne)
ProgProperty progProperty = new ProgProperty(null,null,4567,250,3);

Input.Parser(args, ref progProperty);

ClientFsm clientFsm = new ClientFsm(progProperty);

await clientFsm.RunClient();

return 0;