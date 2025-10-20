using System.Security.Cryptography;
using System.Text;

namespace Api.Services;

public class PasswordService
{
    public (byte[] Hash, byte[] Salt) CreateHash(string password)
    {
        using var hmac = new HMACSHA512();
        return (hmac.ComputeHash(Encoding.UTF8.GetBytes(password)), hmac.Key);
    }
    public bool Verify(string password, byte[] hash, byte[] salt)
    {
        using var hmac = new HMACSHA512(salt);
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computed.SequenceEqual(hash);
    }
}
