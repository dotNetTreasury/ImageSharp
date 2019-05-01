﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.IO;
using Moq;
using Xunit;
// ReSharper disable InconsistentNaming

namespace SixLabors.ImageSharp.Tests
{
    public partial class ImageTests
    {
        /// <summary>
        /// Tests the <see cref="Image"/> class.
        /// </summary>
        public class DetectFormat : ImageLoadTestBase
        {
            private static readonly string ActualImagePath = TestFile.GetInputFileFullPath(TestImages.Bmp.F);

            private byte[] ActualImageBytes => TestFile.Create(TestImages.Bmp.F).Bytes;

            private ReadOnlySpan<byte> ActualImageSpan => this.ActualImageBytes.AsSpan();

            private byte[] ByteArray => this.DataStream.ToArray();

            private ReadOnlySpan<byte> ByteSpan => this.ByteArray.AsSpan();

            private IImageFormat LocalImageFormat => this.localImageFormatMock.Object;

            private static readonly IImageFormat ExpectedGlobalFormat =
                Configuration.Default.ImageFormatsManager.FindFormatByFileExtension("bmp");

            [Fact]
            public void FromFileSystemPath_GlobalConfiguration()
            {
                IImageFormat type = Image.DetectFormat(ActualImagePath);
                Assert.Equal(ExpectedGlobalFormat, type);
            }

            [Fact]
            public void FromFileSystemPath_CustomConfiguration()
            {
                IImageFormat type = Image.DetectFormat(this.LocalConfiguration, this.MockFilePath);
                Assert.Equal(this.LocalImageFormat, type);
            }

            [Fact]
            public void FromStream_GlobalConfiguration()
            {
                using (var stream = new MemoryStream(this.ActualImageBytes))
                {
                    IImageFormat type = Image.DetectFormat(stream);
                    Assert.Equal(ExpectedGlobalFormat, type);
                }
            }

            [Fact]
            public void FromStream_CustomConfiguration()
            {
                IImageFormat type = Image.DetectFormat(this.LocalConfiguration, this.DataStream);
                Assert.Equal(this.LocalImageFormat, type);
            }

            [Fact]
            public void WhenNoMatchingFormatFound_ReturnsNull()
            {
                IImageFormat type = Image.DetectFormat(new Configuration(), this.DataStream);
                Assert.Null(type);
            }
        }
    }
}
