using System;
using System.Runtime.InteropServices;

namespace TMC.Core
{
    /// <summary>
    /// Represents an 8x8 array of pixel data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TilesetEntry
    {
		private const int SIZE = 8 * 8;
		private fixed int data[SIZE];
		
		public int this[int index] {
			get {
				if (index < 0 || index >= SIZE) throw new IndexOutOfRangeException();
				return data[index];
			}
			set {
				if (index < 0 || index >= SIZE) throw new IndexOutOfRangeException();
				data[index] = value;
			}
		}
		public int this[int x, int y] {
			get {
				int index = x + (y * 8);
				if (index < 0 || index >= SIZE) throw new IndexOutOfRangeException();
				return data[index];
			}
			set {
				int index = x + (y * 8);
				if (index < 0 || index >= SIZE) throw new IndexOutOfRangeException();
				data[index] = value;
			}
		}

		public unsafe bool Equals(ref TilesetEntry other, bool flipX = false, bool flipY = false) {
			for (int srcY = 0; srcY < 8; srcY++) {
				for (int srcX = 0; srcX < 8; srcX++) {
					var dstX = flipX ? (7 - srcX) : srcX;
					var dstY = flipY ? (7 - srcY) : srcY;

					if (this[srcX, srcY] != other[dstX, dstY]) {
						return false;
					}
				}
			}
			return true;
		}
		
	}
}
