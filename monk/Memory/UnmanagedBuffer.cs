﻿/**
 *  Monk
 *  Copyright (C) Timothy Baxendale
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
**/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Monk.Memory
{
    /// <summary>
    /// Semi-safe handle to an unmanaged memory block. This is extremely dangerous and should be used with caution.
    /// </summary>
    internal unsafe class UnmanagedBuffer : IDisposable, IList<byte>
    {
        private byte* lpBlock;

        public bool Owned { get; }

        public int Length { get; }
        public IntPtr Pointer => new IntPtr(lpBlock);
        public bool Freed => lpBlock == null;

        public byte this[int index]
        {
            get {
                if ((uint)index >= (uint)Length) throw new ArgumentOutOfRangeException(nameof(index));
                return lpBlock[index];
            }
            set {
                if ((uint)index >= (uint)Length) throw new ArgumentOutOfRangeException(nameof(index));
                lpBlock[index] = value;
            }
        }

        public UnmanagedBuffer(int length)
        {
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));
            Owned = true;
            Length = length;
            lpBlock = (byte*)Marshal.AllocHGlobal(Length).ToPointer();
        }

        public UnmanagedBuffer(IntPtr ptr, int length)
            : this((byte*)ptr.ToPointer(), length)
        {
        }

        public UnmanagedBuffer(byte* ptr, int length)
        {
            Length = length;
            Owned = false;
            lpBlock = ptr;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                if (Owned && !Freed) {
                    Marshal.FreeHGlobal(Pointer);
                    lpBlock = null;
                }
            }
        }

        int ICollection<byte>.Count => Length;
        bool ICollection<byte>.IsReadOnly => false;

        public int IndexOf(byte item)
        {
            byte* lpPtr = (byte*)lpBlock;
            for (int idx = 0; idx < Length; ++idx) {
                if (lpPtr[idx] == item) return idx;
            }
            return -1;
        }

        public bool Contains(byte item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            if ((uint)arrayIndex >= (uint)array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if ((uint)(array.Length - arrayIndex) >= (uint)Length) throw new ArgumentException(nameof(array));
            fixed (byte* lpArr = array) {
                for (int i = 0, len = Length; i < len; ++i) {
                    lpArr[arrayIndex + i] = lpArr[i];
                }
            }
        }

        public void Clear()
        {
            for(int i = 0, len = Length; i < len; ++i) {
                lpBlock[i] = 0;
            }
        }

        public IEnumerator<byte> GetEnumerator()
        {
            IntPtr ptr = Pointer;
            for(int idx = 0; idx < Length; ++idx) {
                yield return Marshal.ReadByte(ptr, idx);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IList<byte>.Insert(int index, byte item)
        {
            throw new NotSupportedException();
        }

        void IList<byte>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection<byte>.Add(byte item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<byte>.Remove(byte item)
        {
            throw new NotSupportedException();
        }

        public Stream GetStream()
        {
            return new UnmanagedMemoryStream((byte*)lpBlock, Length);
        }

        public Stream GetStream(int offset)
        {
            var stream = GetStream();
            stream.Position = offset;
            return stream;
        }

        public byte* UnsafePtrAt(int index)
        {
            return &lpBlock[index];
        }
    }
}
