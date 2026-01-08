namespace Rle;

/// <summary>
/// Provides methods for encoding and decoding data using Run-Length Encoding (RLE).
/// </summary>
public class RleCodec
{
    /// <summary>
    /// The special symbol used to indicate that a run of repeated bytes follows.
    /// </summary>
    public const byte EscapeSymbol = 0x00;

    /// <summary>
    /// Encodes the input data using Run-Length Encoding (RLE) and writes the result to the output stream.
    /// </summary>
    /// <param name="input">The input data to encode as a span of bytes.</param>
    /// <param name="output">The stream to which the encoded data will be written.</param>
    public void Encode(ReadOnlySpan<byte> input, Stream output)
    {
        // Throw an exception if the output stream is null
        ArgumentNullException.ThrowIfNull(output);

        // If the input is empty, return immediately
        if (input.Length == 0)
            return;

        // Track the current run length and the symbol being repeated
        var encoderRunLength = 0;
        var encoderPreviousSymbol = -1;

        // Helper method to write a single symbol to the output stream
        void WriteSymbol(byte symbol)
        {
            // If the symbol is the escape symbol, we need to write it twice
            // to represent the literal escape symbol in the output
            if (symbol == EscapeSymbol)
            {
                output.WriteByte(symbol);
                output.WriteByte(1);
            }

            // Write the symbol to the output stream
            output.WriteByte(symbol);
        }

        // Helper method to flush the current run to the output stream
        void FlushRun(byte currentSymbol)
        {
            // Write the escape symbol to indicate a run of repeated bytes
            output.WriteByte(EscapeSymbol);
            // Write the length of the run
            output.WriteByte((byte)(encoderRunLength + 1));
            // Write the symbol that is being repeated
            output.WriteByte((byte)encoderPreviousSymbol);

            // Reset the run length and update the previous symbol
            encoderRunLength = 0;
            encoderPreviousSymbol = currentSymbol;
        }

        // Iterate through each symbol in the input data
        foreach (var symbol in input)
        {
            // Handle the first symbol in the input
            if (encoderPreviousSymbol == -1)
            {
                encoderPreviousSymbol = symbol;
                continue;
            }

            // Handle the first symbol after a new run
            if (encoderRunLength == 0)
            {
                // If the current symbol is different from the previous one,
                // write the previous symbol and start a new run
                if (encoderPreviousSymbol != symbol)
                    WriteSymbol((byte)encoderPreviousSymbol);
                else
                    encoderRunLength++;

                encoderPreviousSymbol = symbol;
                continue;
            }

            // If the current symbol is different or the run is at the maximum length,
            // flush the current run to the output stream
            if (encoderPreviousSymbol != symbol || encoderRunLength == 254)
            {
                FlushRun(symbol);
                continue;
            }

            // Otherwise, increment the run length
            encoderRunLength++;
        }

        // After processing all input symbols, flush any remaining run to the output
        if (encoderRunLength == 0)
        {
            // If there is no run, write the last symbol directly
            if (encoderPreviousSymbol != -1)
            {
                WriteSymbol((byte)encoderPreviousSymbol);
            }
        }
        else
        {
            // If there is a run, flush it to the output
            FlushRun((byte)encoderPreviousSymbol);
        }
    }

    /// <summary>
    /// Decodes RLE-encoded data from the input and writes the result to the output stream.
    /// </summary>
    /// <param name="input">The RLE-encoded data to decode as a span of bytes.</param>
    /// <param name="output">The stream to which the decoded data will be written.</param>
    /// <returns>The number of bytes decoded and written to the output stream.</returns>
    public int Decode(ReadOnlySpan<byte> input, Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);

        if (input.Length == 0)
            return 0;

        // Track the total number of symbols decoded
        var decodedSymbols = 0;

        // Flags to track the current decoding state
        var isReadingRepeatCount = false;
        var isReadingEncodedSymbol = false;

        // The number of times to repeat the next encoded symbol
        var repeatCount = 0;

        // Process each byte in the input data
        foreach (var symbol in input)
        {
            // If we are currently reading the repeat count
            if (isReadingRepeatCount)
            {
                // Store the repeat count and update decoding state
                repeatCount = symbol;

                // A repeat count of 0 is invalid in RLE encoding
                if (repeatCount == 0)
                    throw new InvalidDataException(
                        "Invalid RLE data: Escape symbol followed by a repeatCount of 0 is not allowed");

                isReadingRepeatCount = false;
                isReadingEncodedSymbol = true;
                continue;
            }

            // If we encounter the escape symbol and are not in the middle of reading an encoded symbol
            if (symbol == EscapeSymbol && !isReadingEncodedSymbol)
            {
                // Start reading the repeat count for the next symbol
                isReadingRepeatCount = true;
                continue;
            }

            // If we are currently reading the encoded symbol to be repeated
            if (isReadingEncodedSymbol)
            {
                // Write the symbol to the output stream the specified number of times
                for (var j = 0; j < repeatCount; j++)
                    output.WriteByte(symbol);
                decodedSymbols += repeatCount;

                // Reset decoding state
                isReadingEncodedSymbol = false;
            }
            else
            {
                // If we are not in a special decoding state, write the symbol directly to the output
                output.WriteByte(symbol);
                decodedSymbols++;
            }
        }

        // Return the total number of symbols decoded
        return decodedSymbols;
    }
}