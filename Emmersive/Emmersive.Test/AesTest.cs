using System.Security.Cryptography;
using Emmersive.Helper;

namespace Emmersive.Test;

[TestClass]
public sealed class AesTest
{
    [DataTestMethod]
    [DynamicData(nameof(RandomStrings), DynamicDataSourceType.Method)]
    public void AesKeyBin(string input)
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);

        var encrypted = input.EncryptAes(key);
        Assert.IsNotNull(encrypted);

        var decrypted = encrypted.DecryptAes(key);
        Assert.IsNotNull(decrypted);

        Assert.AreEqual(input, decrypted);
    }

    public static IEnumerable<object[]> RandomStrings()
    {
        for (var i = 0; i < 5; i++)  {
            yield return [GetRandomString(Random.Shared.Next(15, 25))];
        }
    }

    private static string GetRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-_";
        var bytes = RandomNumberGenerator.GetBytes(length)
            .Select(b => chars[b % chars.Length])
            .ToArray();
        return new(bytes);
    }
}