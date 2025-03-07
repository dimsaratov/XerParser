﻿// See https://aka.ms/new-console-template for more information
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using xer;
using XerLoader;


Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
string path = @"F:\Data\Level2\Level2_Light.xer";
string pathSchemaXer = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SchemaXer.xsd");


ParserWrapper wrapper = new(pathSchemaXer);

again:
Console.Clear();
Console.WriteLine(new string('*', 75));
Console.WriteLine("1 Полная загрузка");
Console.WriteLine("2 Загрузка Project,Task,Projwbs");
Console.WriteLine("Esc. Отмена");

ConsoleKey ki = Console.ReadKey(true).Key;

switch (ki)
{
    case ConsoleKey.NumPad1:
    case ConsoleKey.D1:
        Console.Clear();
        Console.WriteLine("Полная загрузка");
        wrapper.Parse(path);
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
        wrapper.ParseCustom(path);
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

