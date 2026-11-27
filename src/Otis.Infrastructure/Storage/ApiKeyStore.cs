// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using System.Text;

namespace Otis.Infrastructure.Storage;

// The key is encrypted for the signed-in Windows user, so a copied data folder does not leak it.
public sealed class ApiKeyStore(string dataDirectory)
{
    private static readonly byte[] Entropy = "Otis.AnthropicApiKey"u8.ToArray();

    public bool HasKey => File.Exists(this.FilePath);

    private string FilePath => Path.Combine(dataDirectory, "anthropic.key");

    public string? Load()
    {
        if (!this.HasKey)
        {
            return null;
        }

        try
        {
            var plain = ProtectedData.Unprotect(File.ReadAllBytes(this.FilePath), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void Save(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Directory.CreateDirectory(dataDirectory);
        var secret = ProtectedData.Protect(Encoding.UTF8.GetBytes(key.Trim()), Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(this.FilePath, secret);
    }

    public void Remove() => File.Delete(this.FilePath);
}
