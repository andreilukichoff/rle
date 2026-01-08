using Rle;

namespace Huffman.Tests;

public class RleCodecTests
{
    [Fact]
    public void Rle_ShouldEncodeAndDecodeCorrectly()
    {
        // Generate test data with edge cases
        byte[] testData = GenerateTestEdgeData(1024);

        // Create in-memory streams
        var inputStream = new MemoryStream(testData);
        var outputStream = new MemoryStream();

        // Create encoder
        var encoder = new RleCodec();

        // Process the input stream to the output stream
        encoder.Encode(inputStream.ToArray(), outputStream);

        // Verify the output
        outputStream.Position = 0;
        var outputBytes = new byte[outputStream.Length];
        outputStream.ReadExactly(outputBytes, 0, outputBytes.Length);

        // Reconstruct the original data from the encoded output
        var reconstructedData = ReconstructFromEncodedData(outputBytes);

        // Ensure the reconstructed data matches the original
        Assert.Equal(testData, reconstructedData);
    }

    private byte[] GenerateTestEdgeData(int length)
    {
        byte[] data = new byte[length];
        Random random = new Random();

        // Fill with random data
        for (int i = 0; i < length; i++)
        {
            data[i] = (byte)random.Next(0, 256);
        }

        // Add edge cases
        data[0] = RleCodec.EscapeSymbol; // Escape symbol at the beginning
        data[1] = RleCodec.EscapeSymbol; // Escape symbol at the second position
        data[2] = 255; // Maximum byte value
        data[3] = 0; // Minimum byte value
        data[4] = 127; // Middle byte value
        data[5] = 64; // Another middle byte value

        // Add a run of 255 identical bytes
        byte runByte = (byte)random.Next(0, 256);
        for (int i = 6; i < 6 + 255; i++)
        {
            data[i] = runByte;
        }

        // Add a run of 254 identical bytes
        byte runByte2 = (byte)random.Next(0, 256);
        for (int i = 6 + 255; i < 6 + 255 + 254; i++)
        {
            data[i] = runByte2;
        }

        // Add a run of 1 identical byte
        byte runByte3 = (byte)random.Next(0, 256);
        for (int i = 6 + 255 + 254; i < 6 + 255 + 254 + 1; i++)
        {
            data[i] = runByte3;
        }

        // Add a sequence of escape symbols
        for (int i = 6 + 255 + 254 + 1; i < 6 + 255 + 254 + 1 + 5; i++)
        {
            data[i] = RleCodec.EscapeSymbol;
        }

        return data;
    }

    private byte[] ReconstructFromEncodedData(byte[] encodedData)
    {
        var inputStream = new MemoryStream(encodedData);
        var outputStream = new MemoryStream();
        var decoder = new RleCodec();
        decoder.Decode(inputStream.ToArray(), outputStream);
        outputStream.Position = 0;
        var result = new byte[outputStream.Length];
        outputStream.ReadExactly(result, 0, result.Length);
        return result;
    }
}