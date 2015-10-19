﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessorTestBase.cs" company="James South">
//   Copyright © James South and contributors.
//   Licensed under the Apache License, Version 2.0.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ImageProcessor.Tests
{
    using System.Collections.Generic;

    /// <summary>
    /// The processor test base.
    /// </summary>
    public abstract class ProcessorTestBase
    {
        /// <summary>
        /// The collection of image files to test against.
        /// </summary>
        public static readonly List<string> Files = new List<string>
        {
            //"../../TestImages/Formats/Jpg/Backdrop.jpg",
            "../../TestImages/Formats/Jpg/Calliphora.jpg",
            //"../../TestImages/Formats/Bmp/Car.bmp",
            //"../../TestImages/Formats/Png/cmyk.png",
            //"../../TestImages/Formats/Gif/leaf.gif"

            // { "../../TestImages/Formats/Gif/ani.gif" },
            // { "../../TestImages/Formats/Gif/ani2.gif" },
            // { "../../TestImages/Formats/Gif/giphy.gif" },
        };
    }
}