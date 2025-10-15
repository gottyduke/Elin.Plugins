using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Emmersive.Helper;

internal static class AesKeyBin
{
    [field: AllowNull]
    internal static byte[] KeyBin => field ??= GetAesKeyBin();

    private static byte[] GetAesKeyBin()
    {
        if (ResourceFetch.Context.Load<byte[]>(out var key, "aes_bin")) {
            if (key.Length == 32) {
                return key;
            }
        }

        key = new byte[32];
        RandomNumberGenerator.Fill(key);
        ResourceFetch.Context.Save(key, "aes_bin");

        return key;
    }

    extension(string data)
    {
        internal string DecryptAes(byte[]? key = null)
        {
            var buf = Convert.FromBase64String(data);
            var iv = new byte[16];
            Array.Copy(buf, iv, iv.Length);

            using var aes = Aes.Create();
            aes.Key = key ?? KeyBin;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ms = new MemoryStream(buf, iv.Length, buf.Length - iv.Length);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        }

        internal string EncryptAes(byte[]? key = null)
        {
            var buf = Encoding.UTF8.GetBytes(data);
            var iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            using var aes = Aes.Create();
            aes.Key = key ?? KeyBin;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);

            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(buf, 0, buf.Length);
            cs.FlushFinalBlock();

            return Convert.ToBase64String(ms.ToArray());
        }
    }
}