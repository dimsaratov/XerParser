// See https://aka.ms/new-console-template for more information
using System.Text;
using XerParserTest;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
string path = @"F:\Data\Level2\Level2_Light.xer";
string pathXer = @"F:\Data\Level2\Test.xer";
string pathSchemaXer = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SchemaXer.xsd");


ParserWrapper wrapper = new(pathSchemaXer);

again:
Console.Clear();
Console.WriteLine();
Console.WriteLine(new string('*', 75));
Console.WriteLine("1 Полная загрузка");
Console.WriteLine("2 Загрузка Project,Task,Projwbs");
Console.WriteLine("3 Построить Xer");
Console.WriteLine("4 Загрузить построеный Xer");
Console.WriteLine();
Console.WriteLine("Esc. Отмена");
Console.WriteLine(new string('*', 75));

ConsoleKey ki = Console.ReadKey(true).Key;

switch (ki)
{
    case ConsoleKey.NumPad1:
    case ConsoleKey.D1:
        Console.Clear();
        Console.WriteLine("Полная загрузка");
        await wrapper.Parse(path);
        Console.WriteLine("Нажми Enter чтобы продолжить");
        do
        {
            while (!Console.KeyAvailable)
            {
                // Do something
            }
        } while (Console.ReadKey(true).Key != ConsoleKey.Enter);
        goto again;
    case ConsoleKey.NumPad2:
    case ConsoleKey.D2:
        Console.Clear();
        Console.WriteLine("Выборочная загрузка");
        await wrapper.ParseCustom(path);
        Console.WriteLine("Нажми Enter чтобы продолжить");
        do
        {
            while (!Console.KeyAvailable)
            {
                // Do something
            }
        } while (Console.ReadKey(true).Key != ConsoleKey.Enter);
        goto again;
    case ConsoleKey.NumPad3:
    case ConsoleKey.D3:
        Console.Clear();
        Console.WriteLine("Построение Xer");
        await wrapper.Save(pathXer);
        Console.WriteLine("Нажми Enter чтобы продолжить");
        do
        {
            while (!Console.KeyAvailable)
            {
                // Do something
            }
        } while (Console.ReadKey(true).Key != ConsoleKey.Enter);
        goto again;
    case ConsoleKey.NumPad4:
    case ConsoleKey.D4:
        Console.Clear();
        Console.WriteLine("Перезагрузка построенного файла");
        await wrapper.Parse(pathXer);
        Console.WriteLine("Нажми Enter чтобы продолжить");
        do
        {
            while (!Console.KeyAvailable)
            {
                // Do something
            }
        } while (Console.ReadKey(true).Key != ConsoleKey.Enter);
        goto again;
    case ConsoleKey.Escape:
        break;
    default:
        Console.Clear();
        goto again;
}

