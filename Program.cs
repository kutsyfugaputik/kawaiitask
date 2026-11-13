using System.Text;
using KawaiiTasks.Services;
using KawaiiTasks.UI;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var storage = new JsonStorage();
var service = new ProjectService(storage);

new App(service, storage.LoadWarning).Run();
