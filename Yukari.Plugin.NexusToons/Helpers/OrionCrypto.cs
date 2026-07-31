using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Yukari.Plugin.NexusToons.Helpers;

internal class OrionCrypto
{
    private const string SECRET = "OrionNexus2025CryptoKey!Secure";
    private byte[][]? _keys;
    private byte[][]? _rsboxes;
    private bool _initialized;

    public OrionCrypto() => InitFromSecret(SECRET);

    private void InitFromSecret(string secret)
    {
        _keys = new byte[5][];
        _rsboxes = new byte[5][];

        for (int i = 0; i < 5; i++)
        {
            string input = $"_orion_key_{i}_v2_{secret}";
            byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

            byte[] sbox = new byte[256];
            for (int j = 0; j < 256; j++)
                sbox[j] = (byte)j;

            int jIdx = 0;
            for (int j = 0; j < 256; j++)
            {
                jIdx = (jIdx + sbox[j] + keyBytes[j % keyBytes.Length]) % 256;
                (sbox[j], sbox[jIdx]) = (sbox[jIdx], sbox[j]);
            }

            byte[] rsbox = new byte[256];
            for (int j = 0; j < 256; j++)
                rsbox[sbox[j]] = (byte)j;

            _keys[i] = keyBytes;
            _rsboxes[i] = rsbox;
        }

        _initialized = true;
    }

    private byte RotateRight(byte value, int shift)
    {
        shift %= 8;
        return (byte)(((value >> shift) | (value << (8 - shift))) & 0xFF);
    }

    public string Decrypt(int keyIndex, string base64Data)
    {
        if (!_initialized)
            throw new InvalidOperationException("OrionCrypto não foi inicializado.");

        byte[] key = _keys![keyIndex];
        byte[] rsbox = _rsboxes![keyIndex];

        byte[] data = Convert.FromBase64String(base64Data);
        byte[] output = new byte[data.Length];
        int keyLen = key.Length;

        for (int c = data.Length - 1; c >= 0; c--)
        {
            int b = data[c];

            b ^= (c > 0) ? data[c - 1] : key[keyLen - 1];
            b = rsbox[b];
            int rot = ((key[(c + 3) % keyLen] + (c & 0xFF)) & 0xFF) % 7 + 1;
            b = RotateRight((byte)b, rot);
            b ^= key[c % keyLen];

            output[c] = (byte)b;
        }

        return Encoding.UTF8.GetString(output);
    }

    public bool IsEncryptedResponse(JsonElement json)
    {
        return json.ValueKind == JsonValueKind.Object
            && json.TryGetProperty("d", out JsonElement d)
            && d.ValueKind == JsonValueKind.String
            && json.TryGetProperty("k", out JsonElement k)
            && k.ValueKind == JsonValueKind.Number
            && json.TryGetProperty("v", out JsonElement v)
            && v.ValueKind == JsonValueKind.Number
            && (v.GetInt32() == 1 || v.GetInt32() == 2);
    }

    public string ProcessResponse(string jsonString)
    {
        using var doc = JsonDocument.Parse(jsonString);
        JsonElement root = doc.RootElement;

        if (!IsEncryptedResponse(root))
            return jsonString;

        int v = root.GetProperty("v").GetInt32();
        int k = root.GetProperty("k").GetInt32();
        int keyIndex = (v == 1) ? 0 : k;
        string d = root.GetProperty("d").GetString()!;

        return Decrypt(keyIndex, d);
    }

    public string ProcessResponse(JsonDocument document)
    {
        JsonElement root = document.RootElement;
        if (!IsEncryptedResponse(root))
            return root.GetRawText();

        int v = root.GetProperty("v").GetInt32();
        int k = root.GetProperty("k").GetInt32();
        int keyIndex = (v == 1) ? 0 : k;
        string d = root.GetProperty("d").GetString()!;

        return Decrypt(keyIndex, d);
    }
}
