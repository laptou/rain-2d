using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.Shared;

namespace Ibinimator.Service
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DontSerializeAttribute : Attribute
    {
    }

    public class FileService
    {
        public static async Task<Layer> Serialize(Layer root)
        {
            var ms = new MemoryStream();
            await Write(root, ms);
            ms.Position = 0;
            var l = await Read<Layer>(ms);
            return l;
        }

        public static async Task Write<T>(T data, Stream target)
        {
            if (typeof(T).IsValueType || typeof(T) == typeof(string))
                await WriteValue(data, target, typeof(T));
            else
                await Write(data, target, typeof(T));
        }

        private static async Task Write(object data, Stream target, Type type)
        {
            if (type.IsValueType || type == typeof(string))
                throw new InvalidOperationException();

            if (data == null)
            {
                target.WriteByte((byte) TypeMarker.Null);
                return;
            }

            target.WriteByte((byte) TypeMarker.Reference);

            var props =
                from p in type.GetProperties()
                where p.CanRead && p.CanWrite
                where p.CustomAttributes.All(
                    attr => attr.AttributeType != typeof(DontSerializeAttribute))
                select p;

            foreach (var prop in props)
            {
                var propValue = prop.GetValue(data);

                if (prop.PropertyType.IsValueType || prop.PropertyType == typeof(string))
                    await WriteValue(propValue, target, prop.PropertyType);
                else if (typeof(ICollection).IsAssignableFrom(prop.PropertyType))
                    await WriteCollection(propValue as ICollection, target, prop.PropertyType);
                else
                    await Write(propValue, target, prop.PropertyType);
            }
        }

        private static async Task WriteValue(object data, Stream target, Type type)
        {
            if (!type.IsValueType && type != typeof(string))
                throw new InvalidOperationException();

            if (data == null)
            {
                target.WriteByte((byte)TypeMarker.Null);
                return;
            }

            target.WriteByte((byte)TypeMarker.Value);

            var size = type != typeof(string) ? Marshal.SizeOf(type) : ((string)data).Length;
            var bytes = new byte[size];

            if (type == typeof(byte))
                bytes[0] = (byte) data;
            else if (type == typeof(short))
                bytes = BitConverter.GetBytes((short)data);
            else if (type == typeof(int))
                bytes = BitConverter.GetBytes((int)data);
            else if (type == typeof(long))
                bytes = BitConverter.GetBytes((long)data);
            else if (type == typeof(sbyte))
                bytes[0] = (byte) data;
            else if (type == typeof(ushort))
                bytes = BitConverter.GetBytes((ushort)data);
            else if (type == typeof(uint))
                bytes = BitConverter.GetBytes((uint)data);
            else if (type == typeof(ulong))
                bytes = BitConverter.GetBytes((ulong)data);
            else if (type == typeof(float))
                bytes = BitConverter.GetBytes((float)data);
            else if (type == typeof(double))
                bytes = BitConverter.GetBytes((double)data);
            else if (type == typeof(bool))
                bytes = BitConverter.GetBytes((bool)data);
            else if (type == typeof(string))
            {
                bytes = Encoding.Unicode.GetBytes((string) data);
                target.WriteInt(bytes.Length);
            }
            else if (type == typeof(decimal))
                throw new InvalidDataException("decimal is not yet supported.");
            else
            {
                var ptr = Marshal.AllocHGlobal(size);

                Marshal.StructureToPtr(data, ptr, false);
                Marshal.Copy(ptr, bytes, 0, size);
                Marshal.FreeHGlobal(ptr);
            }

            await target.WriteAsync(bytes, 0, bytes.Length);
        }

        private static async Task WriteCollection(ICollection collection, Stream target, Type collectionType)
        {
            if (collection == null)
            {
                target.WriteByte((byte)TypeMarker.Null);
                return;
            }


            Type elementType = typeof(object);
            if (collectionType.IsArray)
            {
                elementType = collectionType.GetElementType();
            }
            else if (collectionType.IsGenericType)
            {
                elementType = collectionType.GenericTypeArguments[0];
                target.WriteByte((byte)TypeMarker.List);
            }

            target.WriteInt(collection.Count);

            if (elementType.IsValueType || elementType == typeof(string))
            {
                foreach (var element in collection)
                {
                    await WriteValue(element, target, elementType);
                }
            }
            else
            {
                foreach (var element in collection)
                {
                    await Write(element, target, elementType);
                }
            }
        }

        public static async Task<T> Read<T>(Stream target)
        {
            return (T) (typeof(T).IsValueType || typeof(T) == typeof(string) ? await ReadValue(target, typeof(T)) : await Read(target, typeof(T)));
        }

        private static async Task<object> Read(Stream target, Type type)
        {
            if (type.IsValueType || type == typeof(string))
                throw new InvalidOperationException();

            switch ((TypeMarker)target.ReadByte())
            {
                case TypeMarker.Null: // nothing to see here
                    return null;
                case TypeMarker.Reference: // good
                    break;
                default:
                    throw new InvalidDataException();
            }

            var data = Activator.CreateInstance(type);
            var props =
                from p in type.GetProperties()
                where p.CanRead && p.CanWrite
                where p.CustomAttributes.All(
                    attr => attr.AttributeType != typeof(DontSerializeAttribute))
                select p;

            foreach (var prop in props)
                //if (prop.PropertyType == typeof(byte))
                //    prop.SetValue(data, await ReadValue(target, typeof(byte)));
                //else if (prop.PropertyType == typeof(short))
                //    prop.SetValue(data, await ReadValue(target, typeof(short)));
                //else if (prop.PropertyType == typeof(int))
                //    prop.SetValue(data, await ReadValue(target, typeof(int)));
                //else if (prop.PropertyType == typeof(long))
                //    prop.SetValue(data, await ReadValue(target, typeof(long)));
                //else if (prop.PropertyType == typeof(sbyte))
                //    prop.SetValue(data, await ReadValue(target, typeof(sbyte)));
                //else if (prop.PropertyType == typeof(ushort))
                //    prop.SetValue(data, await ReadValue(target, typeof(ushort)));
                //else if (prop.PropertyType == typeof(uint))
                //    prop.SetValue(data, await ReadValue(target, typeof(uint)));
                //else if (prop.PropertyType == typeof(ulong))
                //    prop.SetValue(data, await ReadValue(target, typeof(ulong)));
                //else if (prop.PropertyType == typeof(float))
                //    prop.SetValue(data, await ReadValue(target, typeof(float)));
                //else if (prop.PropertyType == typeof(double))
                //    prop.SetValue(data, await ReadValue(target, typeof(double)));
                //else if (prop.PropertyType == typeof(decimal))
                //    prop.SetValue(data, await ReadValue(target, typeof(decimal)));
                //else if (prop.PropertyType == typeof(bool))
                //    prop.SetValue(data, await ReadValue(target, typeof(bool)));
                if (prop.PropertyType == typeof(string) || prop.PropertyType.IsValueType)
                    prop.SetValue(data, await ReadValue(target, prop.PropertyType));
                else
                    prop.SetValue(data, await Read(target, prop.PropertyType));

            return data;
        }

        private static async Task<object> ReadValue(Stream target, Type type)
        {
            object data;

            switch ((TypeMarker) target.ReadByte())
            {
                case TypeMarker.Null:
                    return null;
                case TypeMarker.Value:
                    break;
                default:
                    throw new InvalidDataException();
            }

            if (!type.IsValueType && type != typeof(string))
                throw new InvalidOperationException();

            var size = type != typeof(string) ? Marshal.SizeOf(type) : target.ReadInt();

            var bytes = new byte[size];

            await target.ReadAsync(bytes, 0, bytes.Length);

            if (type == typeof(byte))
                data = bytes[0];
            else if (type == typeof(short))
                data = BitConverter.ToInt16(bytes, 0);
            else if (type == typeof(int))
                data = BitConverter.ToInt32(bytes, 0);
            else if (type == typeof(long))
                data = BitConverter.ToInt64(bytes, 0);
            else if (type == typeof(sbyte))
                data = (sbyte)bytes[0];
            else if (type == typeof(ushort))
                data = BitConverter.ToUInt16(bytes, 0);
            else if (type == typeof(uint))
                data = BitConverter.ToUInt32(bytes, 0);
            else if (type == typeof(ulong))
                data = BitConverter.ToUInt64(bytes, 0);
            else if (type == typeof(float))
                data = BitConverter.ToSingle(bytes, 0);
            else if (type == typeof(double))
                data = BitConverter.ToDouble(bytes, 0);
            else if (type == typeof(bool))
                data = BitConverter.ToBoolean(bytes, 0);
            else if (type == typeof(string))
                data = Encoding.Unicode.GetString(bytes);
            else if (type == typeof(decimal))
                throw new InvalidDataException("decimal is not yet supported.");
            else
            {
                var ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(ptr, bytes, 0, size);
                data = Marshal.PtrToStructure(ptr, type);
                Marshal.FreeHGlobal(ptr);
            }

            return data;
        }

        private static async Task<ICollection<object>> ReadCollection(Stream target, Type collectionType)
        {
            switch ((TypeMarker)target.ReadByte())
            {
                case TypeMarker.Null:
                    return null;
                case TypeMarker.Array:
                case TypeMarker.List:
                    break;
                default:
                    throw new InvalidDataException();
            }

            if (!collectionType.IsArray && !typeof(IList).IsAssignableFrom(collectionType))
                throw new InvalidOperationException();

            var length = target.ReadInt();

            if (collectionType.IsArray)
            {
                var array = (Array) Activator.CreateInstance(collectionType, length);
                for (int i = 0; i < length; i++)
                {
                    array.SetValue(ReadValue(target, elementType)); = null;
                }
            }
        }

        private static int Size(object obj, Type type)
        {
            return type != typeof(string) ? Marshal.SizeOf(type) : ((string)obj).Length;
        }
    }

    public enum TypeMarker : byte
    {
        Null,
        Value,
        Reference,
        Array,
        List
    }
}