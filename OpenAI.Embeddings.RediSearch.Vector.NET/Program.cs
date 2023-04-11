// See https://aka.ms/new-console-template for more information


using OpenAI;

Console.WriteLine("Hello, World!");


var auth = OpenAIAuthentication.LoadFromDirectory();

var openAIClient = new OpenAIClient(auth);
