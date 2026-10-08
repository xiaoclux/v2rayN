namespace v2rayN.Web.Auth;

/// <summary>
/// Holds a PBKDF2 hash of the admin credential (never the plaintext) and verifies login
/// attempts in constant time. Also exposes a deterministic stamp so that changing the
/// credential and restarting invalidates every existing session.
/// </summary>
public sealed class CredentialVerifier
{
    private readonly byte[] _salt;
    private readonly byte[] _hash;

    private CredentialVerifier(string credential)
    {
        _salt = RandomNumberGenerator.GetBytes(AuthConsts.SaltSizeBytes);
        _hash = Derive(credential, _salt);
        var stampBytes = Derive(credential, Encoding.UTF8.GetBytes(AuthConsts.StampSalt));
        Stamp = Convert.ToHexString(stampBytes, 0, AuthConsts.StampSizeBytes);
    }

    /// <summary>Opaque value stored in the session cookie; changes when the credential changes.</summary>
    public string Stamp { get; }

    /// <summary>
    /// Builds the verifier from <see cref="EnvNames.WebCredential"/> or the file named by
    /// <see cref="EnvNames.WebCredentialFile"/> (Docker secrets).
    /// </summary>
    /// <exception cref="InvalidOperationException">No usable credential is configured.</exception>
    public static CredentialVerifier FromEnvironment(Func<string, string?> getEnv)
    {
        var credential = getEnv(EnvNames.WebCredential);
        var credentialFile = getEnv(EnvNames.WebCredentialFile);
        if (string.IsNullOrEmpty(credential) && !string.IsNullOrEmpty(credentialFile))
        {
            if (!File.Exists(credentialFile))
            {
                throw new InvalidOperationException($"{EnvNames.WebCredentialFile} points to a missing file.");
            }
            credential = File.ReadAllText(credentialFile).TrimEnd('\r', '\n');
        }
        if (string.IsNullOrEmpty(credential))
        {
            throw new InvalidOperationException(
                $"Set {EnvNames.WebCredential} or {EnvNames.WebCredentialFile} before starting the web UI.");
        }
        if (credential.Length > AuthConsts.MaxCredentialLength)
        {
            throw new InvalidOperationException($"The web UI credential must not exceed {AuthConsts.MaxCredentialLength} characters.");
        }
        return new CredentialVerifier(credential);
    }

    /// <summary>Returns true when <paramref name="attempt"/> matches the configured credential.</summary>
    public bool Verify(string? attempt)
    {
        if (string.IsNullOrEmpty(attempt) || attempt.Length > AuthConsts.MaxCredentialLength)
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(Derive(attempt, _salt), _hash);
    }

    private static byte[] Derive(string value, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(value, salt, AuthConsts.Pbkdf2Iterations, HashAlgorithmName.SHA256, AuthConsts.HashSizeBytes);
    }
}
