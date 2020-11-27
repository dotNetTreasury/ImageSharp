// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Png.Zlib;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace SixLabors.ImageSharp.Formats.Tiff
{
    /// <summary>
    /// Utility class for writing TIFF data to a <see cref="Stream"/>.
    /// </summary>
    internal class TiffWriter : IDisposable
    {
        private readonly Stream output;

        private readonly MemoryAllocator memoryAllocator;

        private readonly Configuration configuration;

        private readonly byte[] paddingBytes = new byte[4];

        private readonly List<long> references = new List<long>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TiffWriter"/> class.
        /// </summary>
        /// <param name="output">The output stream.</param>
        /// <param name="memoryMemoryAllocator">The memory allocator.</param>
        /// <param name="configuration">The configuration.</param>
        public TiffWriter(Stream output, MemoryAllocator memoryMemoryAllocator, Configuration configuration)
        {
            this.output = output;
            this.memoryAllocator = memoryMemoryAllocator;
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets a value indicating whether the architecture is little-endian.
        /// </summary>
        public bool IsLittleEndian => BitConverter.IsLittleEndian;

        /// <summary>
        /// Gets the current position within the stream.
        /// </summary>
        public long Position => this.output.Position;

        /// <summary>
        /// Writes an empty four bytes to the stream, returning the offset to be written later.
        /// </summary>
        /// <returns>The offset to be written later</returns>
        public long PlaceMarker()
        {
            long offset = this.output.Position;
            this.Write(0u);
            return offset;
        }

        /// <summary>
        /// Writes an array of bytes to the current stream.
        /// </summary>
        /// <param name="value">The bytes to write.</param>
        public void Write(byte[] value)
        {
            this.output.Write(value, 0, value.Length);
        }

        /// <summary>
        /// Writes a byte to the current stream.
        /// </summary>
        /// <param name="value">The byte to write.</param>
        public void Write(byte value)
        {
            this.output.Write(new byte[] { value }, 0, 1);
        }

        /// <summary>
        /// Writes a two-byte unsigned integer to the current stream.
        /// </summary>
        /// <param name="value">The two-byte unsigned integer to write.</param>
        public void Write(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            this.output.Write(bytes, 0, 2);
        }

        /// <summary>
        /// Writes a four-byte unsigned integer to the current stream.
        /// </summary>
        /// <param name="value">The four-byte unsigned integer to write.</param>
        public void Write(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            this.output.Write(bytes, 0, 4);
        }

        /// <summary>
        /// Writes an array of bytes to the current stream, padded to four-bytes.
        /// </summary>
        /// <param name="value">The bytes to write.</param>
        public void WritePadded(byte[] value)
        {
            this.output.Write(value, 0, value.Length);

            if (value.Length < 4)
            {
                this.output.Write(this.paddingBytes, 0, 4 - value.Length);
            }
        }

        /// <summary>
        /// Writes a four-byte unsigned integer to the specified marker in the stream.
        /// </summary>
        /// <param name="offset">The offset returned when placing the marker</param>
        /// <param name="value">The four-byte unsigned integer to write.</param>
        public void WriteMarker(long offset, uint value)
        {
            long currentOffset = this.output.Position;
            this.output.Seek(offset, SeekOrigin.Begin);
            this.Write(value);
            this.output.Seek(currentOffset, SeekOrigin.Begin);
        }

        /// <summary>
        /// Writes the image data as RGB to the stream.
        /// </summary>
        /// <typeparam name="TPixel">The pixel data.</typeparam>
        /// <param name="image">The image to write to the stream.</param>
        /// <param name="padding">The padding bytes for each row.</param>
        /// <param name="compression">The compression to use.</param>
        /// <returns>The number of bytes written.</returns>
        public int WriteRgbImageData<TPixel>(Image<TPixel> image, int padding, TiffEncoderCompression compression)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            using IManagedByteBuffer row = this.AllocateRow(image.Width, 3, padding);
            Span<byte> rowSpan = row.GetSpan();
            int bytesWritten = 0;
            if (compression == TiffEncoderCompression.Deflate)
            {
                using var memoryStream = new MemoryStream();

                // TODO: move zlib compression from png to a common place?
                using var deflateStream = new ZlibDeflateStream(this.memoryAllocator, memoryStream, PngCompressionLevel.Level6); // TODO: make compression level configurable

                for (int y = 0; y < image.Height; y++)
                {
                    Span<TPixel> pixelRow = image.GetPixelRowSpan(y);
                    PixelOperations<TPixel>.Instance.ToRgb24Bytes(this.configuration, pixelRow, rowSpan, pixelRow.Length);
                    deflateStream.Write(rowSpan);
                }

                deflateStream.Flush();

                byte[] buffer = memoryStream.ToArray();
                this.output.Write(buffer);
                bytesWritten += buffer.Length;
                return bytesWritten;
            }

            // No compression.
            for (int y = 0; y < image.Height; y++)
            {
                Span<TPixel> pixelRow = image.GetPixelRowSpan(y);
                PixelOperations<TPixel>.Instance.ToRgb24Bytes(this.configuration, pixelRow, rowSpan, pixelRow.Length);
                this.output.Write(rowSpan);
                bytesWritten += rowSpan.Length;
            }

            return bytesWritten;
        }

        /// <summary>
        /// Writes the image data as indices into a color map to the stream.
        /// </summary>
        /// <typeparam name="TPixel">The pixel data.</typeparam>
        /// <param name="image">The image to write to the stream.</param>
        /// <param name="quantizer">The quantizer to use.</param>
        /// <param name="padding">The padding bytes for each row.</param>
        /// <param name="colorMap">The color map.</param>
        /// <returns>The number of bytes written.</returns>
        public int WritePalettedRgbImageData<TPixel>(Image<TPixel> image, IQuantizer quantizer, int padding, out IExifValue colorMap)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            using IManagedByteBuffer row = this.AllocateRow(image.Width, 1, padding);
            using IQuantizer<TPixel> frameQuantizer = quantizer.CreatePixelSpecificQuantizer<TPixel>(this.configuration);
            using IndexedImageFrame<TPixel> quantized = frameQuantizer.BuildPaletteAndQuantizeFrame(image.Frames.RootFrame, image.Bounds());
            using IMemoryOwner<byte> colorPaletteBuffer = this.memoryAllocator.AllocateManagedByteBuffer(256 * 2 * 3);
            Span<byte> colorPalette = colorPaletteBuffer.GetSpan();

            ReadOnlySpan<TPixel> quantizedColors = quantized.Palette.Span;
            int quantizedColorBytes = quantizedColors.Length * 3 * 2;

            // In the ColorMap, black is represented by 0,0,0 and white is represented by 65535, 65535, 65535.
            Span<Rgb48> quantizedColorRgb48 = MemoryMarshal.Cast<byte, Rgb48>(colorPalette.Slice(0, quantizedColorBytes));
            PixelOperations<TPixel>.Instance.ToRgb48(this.configuration, quantizedColors, quantizedColorRgb48);

            // In a TIFF ColorMap, all the Red values come first, followed by the Green values,
            // then the Blue values. Convert the quantized palette to this format.
            var palette = new ushort[quantizedColorBytes];
            int paletteIdx = 0;
            for (int i = 0; i < quantizedColors.Length; i++)
            {
                palette[paletteIdx++] = quantizedColorRgb48[i].R;
            }

            for (int i = 0; i < quantizedColors.Length; i++)
            {
                palette[paletteIdx++] = quantizedColorRgb48[i].G;
            }

            for (int i = 0; i < quantizedColors.Length; i++)
            {
                palette[paletteIdx++] = quantizedColorRgb48[i].B;
            }

            colorMap = new ExifShortArray(ExifTagValue.ColorMap)
            {
                Value = palette
            };

            int bytesWritten = 0;
            for (int y = 0; y < image.Height; y++)
            {
                ReadOnlySpan<byte> pixelSpan = quantized.GetPixelRowSpan(y);
                this.output.Write(pixelSpan);
                bytesWritten += pixelSpan.Length;

                for (int i = 0; i < padding; i++)
                {
                    this.output.WriteByte(0);
                    bytesWritten++;
                }
            }

            return bytesWritten;
        }

        /// <summary>
        /// Writes the image data as 8 bit gray to the stream.
        /// </summary>
        /// <typeparam name="TPixel">The pixel data.</typeparam>
        /// <param name="image">The image to write to the stream.</param>
        /// <param name="padding">The padding bytes for each row.</param>
        /// <returns>The number of bytes written.</returns>
        public int WriteGrayImageData<TPixel>(Image<TPixel> image, int padding)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            using IManagedByteBuffer row = this.AllocateRow(image.Width, 1, padding);
            Span<byte> rowSpan = row.GetSpan();
            int bytesWritten = 0;
            for (int y = 0; y < image.Height; y++)
            {
                Span<TPixel> pixelRow = image.GetPixelRowSpan(y);
                PixelOperations<TPixel>.Instance.ToL8Bytes(this.configuration, pixelRow, rowSpan, pixelRow.Length);
                this.output.Write(rowSpan);
                bytesWritten += rowSpan.Length;
            }

            return bytesWritten;
        }

        private IManagedByteBuffer AllocateRow(int width, int bytesPerPixel, int padding) => this.memoryAllocator.AllocatePaddedPixelRowBuffer(width, bytesPerPixel, padding);

        /// <summary>
        /// Disposes <see cref="TiffWriter"/> instance, ensuring any unwritten data is flushed.
        /// </summary>
        public void Dispose()
        {
            this.output.Flush();
        }
    }
}