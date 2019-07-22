using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMC.Core {
	public class TilesetSelection {
		int x = 0, y = 0, w = 1, h = 1;
		bool flipX = false, flipY = false;
		int tilesetWrap = 16;
		TilemapEntry[] tileSelecton = null;

		public int X { get => this.x; set => this.x = value; }
		public int Y { get => this.y; set => this.y = value; }
		public int Width { get => this.w; set => this.w = value; }
		public int Height { get => this.h; set => this.h = value; }
		public int TilesetWrap { get => tilesetWrap; set => tilesetWrap=value; }
		public bool FlippedX { get=>flipX; set=>flipX=value; }
		public bool FlippedY { get=>flipY; set=>flipY=value; }

		public int Top { get => y; }
		public int Left { get => x; }
		public int Bottom { get => y+h; }
		public int Right { get => x+w; }
		public Size Size { get => new Size(w,h); set { w=value.Width; h=value.Height; } }
		public Point Location { get => new Point(w,h); set { x=value.X; y=value.Y; } }

		public bool IsEmpty { get => w==0 && h==0; }
		public bool IsConsecutive { get => tileSelecton==null; }

		public TilemapEntry this[int x, int y]
		{
			get {
				int x2 = (flipX) ? (w-1-x) % w : (x % w);
				int y2 = (flipY) ? (h-1-y) % h : (y % h);
				if (tileSelecton == null) {
					return new TilemapEntry {
						Index = (short)((this.x + x2) + ((this.y + y2) * tilesetWrap)),
						FlipX = flipX,
						FlipY = flipY,
					};
				} else {
					var tme = new TilemapEntry(tileSelecton[x2 + y2 * w]);
					tme.FlipX ^= flipX;
					tme.FlipY ^= flipY;
					return tme;
				}
			}
			set {
				if (tileSelecton == null) convertToList();
				if (x > w || y > h) throw new IndexOutOfRangeException("Invalid index!");
				tileSelecton[x + y * w] = value;
			}
		}

		public TilemapEntry this[Point p] => this[p.X, p.Y];

		public void Clear() {
			x = 0; y = 0; w = 1; h = 1;
			tileSelecton = null;
		}

		public void Set(int x, int y, int w, int h) {
			this.x = x; this.y = y;
			this.w = w; this.h = h;
			tileSelecton = null;
		}

		public void SizeTo(Point a, Point b) {
			this.x = 0; this.y = 0;
			this.w = Math.Max(Math.Abs(b.X - a.X), 1);
			this.h = Math.Max(Math.Abs(b.Y - a.Y), 1);
			this.flipX = false; this.flipY = false;
			tileSelecton = new TilemapEntry[w*h];
		}

		private void convertToList() {
			tileSelecton = new TilemapEntry[w*h];
			for (int a = x; a < x+w; a++) {
				for (int b = y; b < y+h; b++) {
					tileSelecton[a + b*w] = new TilemapEntry((short)(x+a + (y+b) * tilesetWrap), flipX, flipY);
				}
			}
		}
	}
}
