﻿// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Text;
using XerParserTest;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
string path = @"F:\Data\Level2\Level2_Light.xer";
string pathXer = @"F:\Data\Level2\Test.xer";
string pathRsrc = @"F:\Data\Resources.xer";
string pathUser = @"F:\Data\KUR.0120.10UJA.JNG.TM.TB0002.L0003.xer";
const string lg = " с полным логом";
string stLog = string.Empty;

ParserWrapper wrapper = new();

again:

Console.Clear();
Console.WriteLine();
Console.WriteLine(new string('*', 75));
Console.WriteLine($"0 Загрузить{stLog} {pathUser}");
Console.WriteLine($"1 Полная загрузка{stLog} {path}");
Console.WriteLine($"2 Загрузка Project,Task,Projwbs{stLog} {path}");
Console.WriteLine($"3 Построить Xer {pathXer}");
Console.WriteLine($"4 Загрузить построеный Xer{stLog} {pathXer}");
Console.WriteLine($"5 Загрузить ресурсы Xer{stLog} {pathRsrc}");
Console.WriteLine($"+ С полным логом");
Console.WriteLine($"- Без полного лога");
Console.WriteLine();
Console.WriteLine("Esc. Отмена");
Console.WriteLine(new string('*', 75));

readKey:
ConsoleKey ki = Console.ReadKey(true).Key;


switch (ki)
{
    case ConsoleKey.NumPad1:
    case ConsoleKey.D1:
        Console.Clear();
        Console.WriteLine($"Полная загрузка{stLog}");
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
        Console.WriteLine($"Выборочная загрузка{stLog}");
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
        Console.WriteLine($"Перезагрузка построенного файла{stLog}");
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
    case ConsoleKey.NumPad5:
    case ConsoleKey.D5:
        Console.Clear();
        Console.WriteLine($"Загрузка ресурсов{stLog}");
        await wrapper.ParseRsrc(pathRsrc);
        Console.WriteLine("Нажми Enter чтобы продолжить");
        do
        {
            while (!Console.KeyAvailable)
            {
                // Do something
            }
        } while (Console.ReadKey(true).Key != ConsoleKey.Enter);
        goto again;
    case ConsoleKey.NumPad0:
    case ConsoleKey.D0:
        Console.Clear();
        Console.WriteLine($"Загрузка {pathUser}{stLog}");
        await wrapper.Parse(pathUser);
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
    case ConsoleKey.Subtract:
    case ConsoleKey.OemMinus:
        wrapper.WithFullLog = false;
        stLog = wrapper.WithFullLog ? lg : string.Empty;
        goto again;
    case ConsoleKey.OemPlus:
    case ConsoleKey.Add:
        wrapper.WithFullLog = true;
        stLog = wrapper.WithFullLog ? lg : string.Empty;
        goto again;
    default:        
        goto readKey;
}

