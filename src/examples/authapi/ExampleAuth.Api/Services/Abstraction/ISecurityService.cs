namespace ExampleAuth.Api.Services.Abstraction;

public interface ISecurityService
{
    string CreateHash(string plainText);
}