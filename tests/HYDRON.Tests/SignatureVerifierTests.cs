using NSec.Cryptography;
using System.Text;
using HYDRON.Cryptography;

namespace HYDRON.Tests;

public class SignatureVerifierTests
{
    private static readonly SignatureAlgorithm Ed25519 = SignatureAlgorithm.Ed25519;

    private static (string publicKeyBase64, string signatureBase64) Sign(string data)
    {
        using Key key = Key.Create(Ed25519, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        });
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] sig = Ed25519.Sign(key, dataBytes);
        byte[] pub = key.Export(KeyBlobFormat.RawPublicKey);
        return (Convert.ToBase64String(pub), Convert.ToBase64String(sig));
    }

    private static (string publicKeyBase64, string signatureBase64) SignBytes(byte[] data)
    {
        using Key key = Key.Create(Ed25519, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        });
        byte[] sig = Ed25519.Sign(key, data);
        byte[] pub = key.Export(KeyBlobFormat.RawPublicKey);
        return (Convert.ToBase64String(pub), Convert.ToBase64String(sig));
    }

    // --- Verify (string overload) ---

    [Fact]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        var (pub, sig) = Sign("hello hydron");
        Assert.True(SignatureVerifier.Verify("hello hydron", sig, pub));
    }

    [Fact]
    public void Verify_WrongKey_ReturnsFalse()
    {
        var (_, sig) = Sign("hello");
        var (wrongPub, _) = Sign("other");
        Assert.False(SignatureVerifier.Verify("hello", sig, wrongPub));
    }

    [Fact]
    public void Verify_TamperedData_ReturnsFalse()
    {
        var (pub, sig) = Sign("original");
        Assert.False(SignatureVerifier.Verify("tampered", sig, pub));
    }

    [Fact]
    public void Verify_EmptyData_ReturnsFalse()
    {
        var (pub, sig) = Sign("data");
        Assert.False(SignatureVerifier.Verify("", sig, pub));
    }

    [Fact]
    public void Verify_EmptySignature_ReturnsFalse()
    {
        var (pub, _) = Sign("data");
        Assert.False(SignatureVerifier.Verify("data", "", pub));
    }

    [Fact]
    public void Verify_EmptyPublicKey_ReturnsFalse()
    {
        var (_, sig) = Sign("data");
        Assert.False(SignatureVerifier.Verify("data", sig, ""));
    }

    [Fact]
    public void Verify_BadBase64Signature_ReturnsFalse()
    {
        var (pub, _) = Sign("data");
        Assert.False(SignatureVerifier.Verify("data", "not_base64!!!", pub));
    }

    [Fact]
    public void Verify_BadBase64PublicKey_ReturnsFalse()
    {
        var (_, sig) = Sign("data");
        Assert.False(SignatureVerifier.Verify("data", sig, "not_base64!!!"));
    }

    [Fact]
    public void Verify_WrongSignatureLength_ReturnsFalse()
    {
        var (pub, _) = Sign("data");
        string shortSig = Convert.ToBase64String(new byte[16]);
        Assert.False(SignatureVerifier.Verify("data", shortSig, pub));
    }

    [Fact]
    public void Verify_WrongPublicKeyLength_ReturnsFalse()
    {
        var (_, sig) = Sign("data");
        string shortKey = Convert.ToBase64String(new byte[16]);
        Assert.False(SignatureVerifier.Verify("data", sig, shortKey));
    }

    // --- VerifyBytes ---

    [Fact]
    public void VerifyBytes_ValidSignature_ReturnsTrue()
    {
        byte[] data = [0x01, 0x02, 0x03];
        var (pub, sig) = SignBytes(data);
        Assert.True(SignatureVerifier.VerifyBytes(data, sig, pub));
    }

    [Fact]
    public void VerifyBytes_TamperedData_ReturnsFalse()
    {
        byte[] data = [0x01, 0x02, 0x03];
        var (pub, sig) = SignBytes(data);
        Assert.False(SignatureVerifier.VerifyBytes([0xFF, 0x02, 0x03], sig, pub));
    }

    [Fact]
    public void VerifyBytes_NullData_ReturnsFalse()
    {
        var (pub, sig) = SignBytes([0x01]);
        Assert.False(SignatureVerifier.VerifyBytes(null, sig, pub));
    }

    [Fact]
    public void VerifyBytes_EmptyData_ReturnsFalse()
    {
        var (pub, sig) = SignBytes([0x01]);
        Assert.False(SignatureVerifier.VerifyBytes([], sig, pub));
    }
}
