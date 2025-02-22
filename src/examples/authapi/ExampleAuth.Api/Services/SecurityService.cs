using System.Security.Cryptography;
using System.Text;
using ExampleAuth.Api.Services.Abstraction;

namespace ExampleAuth.Api.Services;

public class SecurityService : ISecurityService
{
    public string CreateHash(string plainText) =>
        Convert.ToBase64String(SHA3_384.HashData(Encoding.Unicode.GetBytes(plainText)));
}