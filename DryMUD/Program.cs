using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting network handler.");
        Network.ConnectionHandler.Listen();
    }
}
