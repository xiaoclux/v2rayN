namespace v2rayN.Web.Tests.Auth;

public class CredentialVerifierTests
{
    private const string Credential = "correct-horse-battery";

    private static Func<string, string?> Env(string? credential)
    {
        return name => name == EnvNames.WebCredential ? credential : null;
    }

    [Test]
    public async Task Verify_ShouldAcceptOnlyTheConfiguredCredential()
    {
        var verifier = CredentialVerifier.FromEnvironment(Env(Credential));

        await verifier.Verify(Credential).Should().BeTrue();
        await verifier.Verify("wrong").Should().BeFalse();
        await verifier.Verify(string.Empty).Should().BeFalse();
        await verifier.Verify(null).Should().BeFalse();
        await verifier.Verify(new string('a', AuthConsts.MaxCredentialLength + 1)).Should().BeFalse();
    }

    [Test]
    public async Task Stamp_ShouldBeStableForSameCredentialAndDifferOtherwise()
    {
        var first = CredentialVerifier.FromEnvironment(Env(Credential));
        var second = CredentialVerifier.FromEnvironment(Env(Credential));
        var other = CredentialVerifier.FromEnvironment(Env(Credential + "!"));

        await first.Stamp.Should().BeEqualTo(second.Stamp);
        await first.Stamp.Should().NotBeEqualTo(other.Stamp);
    }

    [Test]
    public async Task FromEnvironment_ShouldRefuseToStartWithoutCredential()
    {
        var act = () => CredentialVerifier.FromEnvironment(Env(null));

        await act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task FromEnvironment_ShouldReadCredentialFileAndTrimNewline()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, Credential + "\n");
            var verifier = CredentialVerifier.FromEnvironment(name => name == EnvNames.WebCredentialFile ? path : null);

            await verifier.Verify(Credential).Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
