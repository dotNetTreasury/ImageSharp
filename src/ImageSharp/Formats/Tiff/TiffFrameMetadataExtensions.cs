// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;

using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace SixLabors.ImageSharp.Formats.Experimental.Tiff
{
    /// <summary>
    /// The tiff metadata extensions
    /// </summary>
    internal static class TiffFrameMetadataExtensions
    {
        public static T[] GetArray<T>(this TiffFrameMetadata meta, ExifTag tag, bool optional = false)
            where T : struct
        {
            if (meta.TryGetArray(tag, out T[] result))
            {
                return result;
            }

            if (!optional)
            {
                TiffThrowHelper.ThrowTagNotFound(nameof(tag));
            }

            return null;
        }

        public static bool TryGetArray<T>(this TiffFrameMetadata meta, ExifTag tag, out T[] result)
            where T : struct
        {
            foreach (IExifValue entry in meta.FrameTags)
            {
                if (entry.Tag == tag)
                {
                    DebugGuard.IsTrue(entry.IsArray, "Expected array entry");

                    result = (T[])entry.GetValue();
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static TEnum[] GetEnumArray<TEnum, TTagValue>(this TiffFrameMetadata meta, ExifTag tag, bool optional = false)
          where TEnum : struct
          where TTagValue : struct
        {
            if (meta.TryGetArray(tag, out TTagValue[] result))
            {
                return result.Select(a => (TEnum)(object)a).ToArray();
            }

            if (!optional)
            {
                TiffThrowHelper.ThrowTagNotFound(nameof(tag));
            }

            return null;
        }

        public static string GetString(this TiffFrameMetadata meta, ExifTag tag)
        {
            foreach (IExifValue entry in meta.FrameTags)
            {
                if (entry.Tag == tag)
                {
                    DebugGuard.IsTrue(entry.DataType == ExifDataType.Ascii, "Expected string entry");
                    object value = entry.GetValue();
                    DebugGuard.IsTrue(value is string, "Expected string entry");

                    return (string)value;
                }
            }

            return null;
        }

        public static bool SetString(this TiffFrameMetadata meta, ExifTag tag, string value)
        {
            IExifValue obj = FindOrCreate(meta, tag);
            DebugGuard.IsTrue(obj.DataType == ExifDataType.Ascii, "Expected string entry");

            return obj.TrySetValue(value);
        }

        public static TEnum? GetSingleEnumNullable<TEnum, TTagValue>(this TiffFrameMetadata meta, ExifTag tag)
          where TEnum : struct
          where TTagValue : struct
        {
            if (!meta.TryGetSingle(tag, out TTagValue value))
            {
                return null;
            }

            return (TEnum)(object)value;
        }

        public static TEnum GetSingleEnum<TEnum, TTagValue>(this TiffFrameMetadata meta, ExifTag tag, TEnum? defaultValue = null)
            where TEnum : struct
            where TTagValue : struct
        => meta.GetSingleEnumNullable<TEnum, TTagValue>(tag) ?? (defaultValue != null ? defaultValue.Value : throw TiffThrowHelper.TagNotFound(nameof(tag)));

        public static bool SetSingleEnum<TEnum, TTagValue>(this TiffFrameMetadata meta, ExifTag tag, TEnum value)
        where TEnum : struct
        where TTagValue : struct
        {
            IExifValue obj = FindOrCreate(meta, tag);

            object val = (TTagValue)(object)value;
            return obj.TrySetValue(val);
        }

        public static T GetSingle<T>(this TiffFrameMetadata meta, ExifTag tag)
            where T : struct
        {
            if (meta.TryGetSingle(tag, out T result))
            {
                return result;
            }

            throw TiffThrowHelper.TagNotFound(nameof(tag));
        }

        public static bool TryGetSingle<T>(this TiffFrameMetadata meta, ExifTag tag, out T result)
            where T : struct
        {
            foreach (IExifValue entry in meta.FrameTags)
            {
                if (entry.Tag == tag)
                {
                    DebugGuard.IsTrue(!entry.IsArray, "Expected non array entry");

                    object value = entry.GetValue();

                    result = (T)value;
                    return true;
                }
            }

            result = default;
            return false;
        }

        public static bool SetSingle<T>(this TiffFrameMetadata meta, ExifTag tag, T value)
            where T : struct
        {
            IExifValue obj = FindOrCreate(meta, tag);
            DebugGuard.IsTrue(!obj.IsArray, "Expected non array entry");

            object val = (T)(object)value;
            return obj.TrySetValue(val);
        }

        public static bool Remove(this TiffFrameMetadata meta, ExifTag tag)
        {
            IExifValue obj = null;
            foreach (IExifValue entry in meta.FrameTags)
            {
                if (entry.Tag == tag)
                {
                    obj = entry;
                    break;
                }
            }

            if (obj != null)
            {
                return meta.FrameTags.Remove(obj);
            }

            return false;
        }

        private static IExifValue FindOrCreate(TiffFrameMetadata meta, ExifTag tag)
        {
            IExifValue obj = null;
            foreach (IExifValue entry in meta.FrameTags)
            {
                if (entry.Tag == tag)
                {
                    obj = entry;
                    break;
                }
            }

            if (obj == null)
            {
                obj = ExifValues.Create(tag);
                meta.FrameTags.Add(obj);
            }

            return obj;
        }
    }
}