// See https://aka.ms/new-console-template for more information

using IPK25_CHAT;
using IPK25_CHAT.ioStream;
using IPK25_CHAT.structs;

ClientFsm clientFsm = new ClientFsm();
Input input = new Input();
NetworkUtils networkUtils = new NetworkUtils();

ProgProperty property = new ProgProperty(true,"127.0.0.1",4567,250,3);

//input.Parser(args, property);

//clientFsm.RunClientFsm();

await networkUtils.Connect(property);