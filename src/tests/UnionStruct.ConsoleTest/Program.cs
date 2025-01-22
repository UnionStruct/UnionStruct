// See https://aka.ms/new-console-template for more information

using UnionStruct.ConsoleTest;

Console.WriteLine("Hello, World!");

var test1 = TestUnion<int>.AsValue(123);
var test2 = TestUnion<int>.AsFail(new Exception("sosi"));