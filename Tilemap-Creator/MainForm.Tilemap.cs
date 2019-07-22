using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TMC.Core;

namespace TMC
{
    partial class MainForm
    {
        private static Brush[] palettemapBrushes;
        private static Font palettemapFont;

        private Tilemap tilemap;
        private DirectBitmap tilemapImage;

        private Point tilemapMouseAnchor = new Point(-1, -1);
        private Point tilemapMouseCurrent = new Point(-1, -1);
		
		private EditMode editMode = EditMode.Pencil;
		private Random rand = new Random();

		#region Drawing

		// Updates Tilemap image (forced redraw)
		private void UpdateTilemap()
        {
            if (tilemap != null && tileset != null)
            {
                ignore = true;

                // set size info
                textTilemapWidth.Text = tilemap.Width.ToString();
                textTilemapHeight.Text = tilemap.Height.ToString();

                // draw tilemap
                if (tilemapImage == null ||
                    tilemapImage.Width != tilemap.Width << 3 ||
                    tilemapImage.Height != tilemap.Height << 3)
                {
                    tilemapImage?.Dispose();
                    tilemapImage = new DirectBitmap(tilemap.Width << 3, tilemap.Height << 3);
                }

                tilemap.Draw(tilemapImage, tileset);

                // finished
                pTilemap.Size = new Size(tilemapImage.Width * zoom, tilemapImage.Height * zoom);
                pTilemap.Image = tilemapImage;
                ignore = false;
            }
        }

        private void DrawPalettemap(Graphics g, Rectangle clip)
        {
            if (!rModePalette.Checked) return;
			if (zoom == 0) return; //avoid divide by zero

			//int boundsX = clip.X / (8 * zoom);
			//int boundsY = clip.Y / (8 * zoom);
			//int boundsWidth = (clip.Width / (8 * zoom)) + 2;
			//int boundsHeight = (clip.Height / (8 * zoom)) + 2;

            if (palettemapBrushes == null)
            {
                palettemapBrushes = new SolidBrush[palettemapColors.Length];
                for (int i = 0; i < palettemapColors.Length; i++)
                    palettemapBrushes[i] = new SolidBrush(palettemapColors[i]);
            }

            if (palettemapFont == null)
                palettemapFont = new Font("Arial", 5.5f, FontStyle.Regular);

            for (int y = 0; y < tilemap.Height; y++)
            {
                for (int x = 0; x < tilemap.Width; x++)
                {
                    if (x < 0 || y < 0 || x >= tilemap.Width || y >= tilemap.Height)
                        continue;

                    g.FillRectangle(
                        palettemapBrushes[tilemap[x, y].Palette & 0xF],
                        x * 8 * zoom,
                        y * 8 * zoom,
                        8 * zoom,
                        8 * zoom
                    );

                    g.DrawString(
                        tilemap[x, y].Palette.ToString("X"),
                        palettemapFont,
                        Brushes.Black,
                        x * 8 * zoom + 1,
                        y * 8 * zoom
                    );
                }
            }
        }

        private void pTilemap_Paint(object sender, PaintEventArgs e)
        {
            if (tileset == null || tilemap == null) return;

            // Draw palettemap
            DrawPalettemap(
                e.Graphics,
                e.ClipRectangle
            );

            // Draw grid
            if (mnuGrid.Checked)
            {
                using (var pen = new Pen(new SolidBrush(GridColor), 1f))
                using (var penS = new Pen(new SolidBrush(GridSelectionColor), 1f))
                {
                    pen.DashPattern = new[] { 2f, 2f };
                    penS.DashPattern = new[] { 2f, 2f };

                    int size = zoom * Tileset.TileSize;
					int off = (zoom) / 2;

                    for (int x = 0; x < pTilemap.Width / size; x++)
                    {
						e.Graphics.DrawLine(x % 30 == 0 ? penS : pen, 
							x * size - off, 
							0, 
							x * size - off, 
							pTilemap.Height);
					}

                    for (int y = 0; y < pTilemap.Height / size; y++)
                    {
						e.Graphics.DrawLine(y % 20 == 0 ? penS : pen, 
							0, 
							y * size - off, 
							pTilemap.Width,
							y * size - off);
					}
                }
            }

            // Draw cursor based on tileset/palette selection
            if (tilemapMouseAnchor.X >= 0 && tilemapMouseAnchor.Y >= 0) {
				if (InTileMappingMode)
                {
					Point top = tilemapMouseAnchor;
					if (tilemapMouseCurrent.X < tilemapMouseAnchor.X || tilemapMouseCurrent.Y < tilemapMouseAnchor.Y) {
						top = tilemapMouseCurrent;
					}
					Rectangle r = new Rectangle {
						X = top.X * zoom * Tileset.TileSize - (zoom/2), 
						Y = top.Y * zoom * Tileset.TileSize - (zoom/2), 
						Width = tilesetSelection.Width * zoom * Tileset.TileSize,
						Height = tilesetSelection.Height * zoom * Tileset.TileSize,
					};
					e.Graphics.DrawRectangle(Pens.Red, r);
				}
			} 
			else if (tilemapMouseCurrent.X >= 0 && tilemapMouseCurrent.Y >= 0) {
                if (InTileMappingMode)
                {
					Rectangle r = new Rectangle{
						X = tilemapMouseCurrent.X * zoom * Tileset.TileSize - (zoom/2), 
						Y = tilemapMouseCurrent.Y * zoom * Tileset.TileSize - (zoom/2), 
						Width = tilesetSelection.Width * zoom * Tileset.TileSize,
						Height = tilesetSelection.Height * zoom * Tileset.TileSize,
					};
					e.Graphics.DrawRectangle(Pens.Red, r);

                    //e.Graphics.DrawRectangle(
                    //    Pens.Red,
                    //    tilemapMouseCurrent.X * zoom * 8 - (zoom/2),
                    //    tilemapMouseCurrent.Y * zoom * 8 - (zoom/2),
                    //    tilesetSelection.Width * zoom * 8,
                    //    tilesetSelection.Height * zoom * 8
                    //);
                }
                else
                {
                    e.Graphics.DrawRectangle(
                        Pens.Red,
                        tilemapMouseCurrent.X * zoom * 8 - (zoom/2),
                        tilemapMouseCurrent.Y * zoom * 8 - (zoom/2),
                        zoom * 8,
                        zoom * 8
                    );
                }
            }
        }

		#endregion

		#region Editing

		private void MouseEditTileMap_PencilMode() {
			if (InTileMappingMode)
			{
				// set selection rectangle
				for (int x = 0; x < tilesetSelection.Width; x++)
				{
					for (int y = 0; y < tilesetSelection.Height; y++)
					{
						// tilemap position
						var mapX = tilemapMouseCurrent.X + x;
						var mapY = tilemapMouseCurrent.Y + y;

						if (mapX < 0 || mapX >= tilemap.Width || mapY < 0 || mapY >= tilemap.Height)
							continue;

						// tile at position
						var tile = tilesetSelection[x, y];

						// ilegal tiles default to 0
						if (tile.Index >= tileset.Length)
							tile.Index = 0;

						// set selection
						tilemap[mapX, mapY] = tile;
						//tilemap[mapX, mapY].FlipX = btnTilesetFlipX.Checked;
						//tilemap[mapX, mapY].FlipY = btnTilesetFlipY.Checked;
					}
				}    
			}
			else
			{
				// set palette selection
                tilemap[tilemapMouseCurrent.X, tilemapMouseCurrent.Y].Palette = (byte)paletteSelection;
			}
		}


		private void MouseEditTileMap_FillMode() {
			// https://rosettacode.org/wiki/Bitmap/Flood_fill#C.23

			var queue = new Queue<Point>();

			// Fills all tiles going out from X, Y with the first tile selected
			if (InTileMappingMode)
			{
				var dst = tilesetSelection[0,0];
				var src = tilemap[tilemapMouseCurrent];
				if (src == dst) return;

				// create a queue to hold points
				queue.Enqueue(tilemapMouseCurrent);

				// queues all tiles left and right of x,y and such
				while (queue.Count > 0)
				{
					var n = queue.Dequeue();
					if (tilemap[n] != src)
						continue;

					Point w = n, e = new Point(n.X + 1, n.Y);
					while (w.X >= 0 && tilemap[w] == src)
					{
						tilemap[w] = dst;
						if (w.Y > 0 && tilemap[w.X, w.Y - 1] == src)
							queue.Enqueue(new Point(w.X, w.Y - 1));
						if (w.Y < tilemap.Height - 1 && tilemap[w.X, w.Y + 1] == src)
							queue.Enqueue(new Point(w.X, w.Y + 1));
						w.X--;
					}

					while (e.X <= tilemap.Width - 1 && tilemap[e] == src)
					{
						tilemap[e] = dst;
						if (e.Y > 0 && tilemap[e.X, e.Y - 1] == src)
							queue.Enqueue(new Point(e.X, e.Y - 1));
						if (e.Y < tilemap.Height - 1 && tilemap[e.X, e.Y + 1] == src)
							queue.Enqueue(new Point(e.X, e.Y + 1));
						e.X++;
					}
				}

				// Redraw entire tilemap (unknown amount of tiles changed)
				tilemap.Draw(tilemapImage, tileset, 0, 0, tilemap.Width, tilemap.Height);
			}
			else
			{
				var dst = paletteSelection;
				var src = tilemap[tilemapMouseCurrent].Palette;
				if (src == dst) return;
				// create a queue to hold points
				queue.Enqueue(tilemapMouseCurrent);

				// queues all tiles left and right of x,y and such
				while (queue.Count > 0)
				{
					var n = queue.Dequeue();
					if (tilemap[n].Palette != src)
						continue;

					Point w = n, e = new Point(n.X + 1, n.Y);
					while (w.X >= 0 && tilemap[w].Palette == src)
					{
						tilemap[w].Palette = (byte)dst;
						if (w.Y > 0 && tilemap[w.X, w.Y - 1].Palette == src)
							queue.Enqueue(new Point(w.X, w.Y - 1));
						if (w.Y < tilemap.Height - 1 && tilemap[w.X, w.Y + 1].Palette == src)
							queue.Enqueue(new Point(w.X, w.Y + 1));
						w.X--;
					}

					while (e.X <= tilemap.Width - 1 && tilemap[e].Palette == src)
					{
						tilemap[e].Palette = (byte)dst;
						if (e.Y > 0 && tilemap[e.X, e.Y - 1].Palette == src)
							queue.Enqueue(new Point(e.X, e.Y - 1));
						if (e.Y < tilemap.Height - 1 && tilemap[e.X, e.Y + 1].Palette == src)
							queue.Enqueue(new Point(e.X, e.Y + 1));
						e.X++;
					}
				}

				// Redraw entire tilemap (unknown amount of tiles changed)
				tilemap.Draw(tilemapImage, tileset, 0, 0, tilemap.Width, tilemap.Height);
			}
		}

		private void MouseEditTileMap_RectangleMode() {
			// TODO
		}

		private void MouseEditTileMap_RandomMode() {
			if (!InTileMappingMode) return;
			// set selection rectangle
			for (int x = 0; x < tilesetSelection.Width; x++)
			{
				for (int y = 0; y < tilesetSelection.Height; y++)
				{
					// tilemap position
					var mapX = tilemapMouseCurrent.X + x;
					var mapY = tilemapMouseCurrent.Y + y;

					if (mapX < 0 || mapX >= tilemap.Width || mapY < 0 || mapY >= tilemap.Height)
						continue;

					// tileset position
					var setX = rand.Next(tilesetSelection.Width);
					var setY = rand.Next(tilesetSelection.Height);

					// tile at position
					var tile = tilesetSelection[setX, setY];

					// ilegal tiles default to 0
					if (tile.Index >= tileset.Length)
						tile.Index = 0;

					// set selection
					tilemap[mapX, mapY] = tile;
					//tilemap[mapX, mapY].FlipX = btnTilesetFlipX.Checked;
					//tilemap[mapX, mapY].FlipY = btnTilesetFlipY.Checked;
				}
			}       
		}

		private void MouseEditTileMap()
		{
			switch (this.editMode)
			{
				case EditMode.Pencil:
					MouseEditTileMap_PencilMode();
					break;
				case EditMode.Fill:
					MouseEditTileMap_FillMode();
					break;
				case EditMode.Rectangle:
					MouseEditTileMap_RectangleMode();
					break;
				case EditMode.Random:
					MouseEditTileMap_RandomMode();
					break;
			}
		}

		#endregion

		#region Mouse Handling

		private void pTilemap_MouseDown(object sender, MouseEventArgs e)
        {
			tilemapMouseAnchor.X = e.X / (zoom * Tileset.TileSize);
			tilemapMouseAnchor.Y = e.Y / (zoom * Tileset.TileSize);

            pTilemap_MouseMove(sender, e);
        }

        private void pTilemap_MouseMove(object sender, MouseEventArgs me)
        {
            if (me.X < 0 || me.Y < 0 || me.X >= pTilemap.Width || me.Y >= pTilemap.Height)
                return;

            // update tilemap mouse position
            tilemapMouseCurrent.X = me.X / (zoom * Tileset.TileSize);
            tilemapMouseCurrent.Y = me.Y / (zoom * Tileset.TileSize);

            lPosition.Text = InTileMappingMode ?
                $"Tilemap: ({tilemapMouseCurrent.X}, {tilemapMouseCurrent.Y})" :
                $"Palettemap: ({tilemapMouseCurrent.X}, {tilemapMouseCurrent.Y})";

            if (tilemap == null)
                return;

            // Update status bar
            var mousedTile = tilemap[tilemapMouseCurrent];
            lTile.Text = $"Tile: {mousedTile.Index:X3}";
            lPalette.Text = $"Palette: {mousedTile.Palette:X}";
            lFlip.Text = "Flip: " + (mousedTile.FlipX ? mousedTile.FlipY ? "XY" : "X" : mousedTile.FlipY ? "Y" : "None");

            // Set tiles starting from X, Y
            if (me.Button == MouseButtons.Left)
            {
				MouseEditTileMap();

                // redraw just the portion of the tilemap that was edited
                tilemap.Draw(tilemapImage, tileset, tilemapMouseCurrent.X, tilemapMouseCurrent.Y,
                        tilesetSelection.Width, tilesetSelection.Height);
            }
            // Get tile at X, Y -- overrides selection
            else if (me.Button == MouseButtons.Right)
            {
                if (InTileMappingMode)
                {
					Point a = tilemapMouseAnchor;
					Point b = tilemapMouseCurrent;
					if (b.X < a.X || b.Y < a.Y) {
						a = tilemapMouseCurrent;
						b = tilemapMouseAnchor;
					}
					tilesetSelection.SizeTo(a, b);
					for (int x = 0; x < tilesetSelection.Width; x++) {
						for (int y = 0; y < tilesetSelection.Height; y++) {
							tilesetSelection[x, y] = tilemap[a.X + x, a.Y + y];
						}
					}

					//var t = tilemap[tilemapMouseCurrent];
					//var w = tilesetWidth;

					//tilesetSelection.Set(t.Index % w, t.Index / w, 1, 1);
					btnTilesetFlipX.Checked = false;
					btnTilesetFlipY.Checked = false;
				}
                else
                {
                    paletteSelection = tilemap[tilemapMouseCurrent].Palette;
                }

                lTilesetSelection.Text = InTileMappingMode ?
                        $"({tilesetSelection.X}, {tilesetSelection.Y}) to ({tilesetSelection.X + tilesetSelection.Width - 1}, {tilesetSelection.Y + tilesetSelection.Height - 1})" :
                        $"{paletteSelection}";

                pTileset.Invalidate();
            }

            //end:
            pTilemap.Invalidate();
        }

        private void pTilemap_MouseLeave(object sender, EventArgs e)
        {
            tilemapMouseCurrent.X = -1;
            pTilemap.Invalidate();
        }
		
		private void pTilemap_MouseUp(object sender, MouseEventArgs e) 
		{
			tilemapMouseAnchor.X = -1;
			tilemapMouseAnchor.Y = -1;
		}
		
		#endregion

        private void rMode_CheckedChanged(object sender, EventArgs e)
        {
            if (ignore) return;

            // TODO: change selection
            UpdateTileset(false);
            UpdateTilemap();
        }

		private bool InTileMappingMode {
			get => rModeTilemap.Checked;
		}
		
		private void changeEditMode(EditMode mode) 
		{
			this.editMode = mode;
			btnEditModePencil.Checked = this.editMode == EditMode.Pencil;
			btnEditModeRect.Checked = this.editMode == EditMode.Rectangle;
			btnEditModeRandom.Checked = this.editMode == EditMode.Random;
			btnEditModeFill.Checked = this.editMode == EditMode.Fill;
		}

		#region Button Handlers

		private void textTilemapWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar)))
            {
                e.Handled = true;
            }
        }

        private void textTilemapHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar)))
            {
                e.Handled = true;
            }
        }

        private void btnTilemapResize_Click(object sender, EventArgs e)
        {
            if (tilemap == null || tileset == null)
                return;

            if (short.TryParse(textTilemapWidth.Text, out var width) &&
                short.TryParse(textTilemapHeight.Text, out var height) &&
                (width != tilemap.Width || height != tilemap.Height))
            {
                if (width < 1)
                    width = 1;

                if (height < 1)
                    height = 1;

                tilemap.Resize(width, height);
                UpdateTilemap();
            }
        }

        private void btnTilemapShiftLeft_Click(object sender, EventArgs e)
        {
            if (tilemap != null)
            {
                tilemap.ShiftLeft();
                UpdateTilemap();
            }
        }

        private void btnTilemapShiftUp_Click(object sender, EventArgs e)
        {
            if (tilemap != null)
            {
                tilemap.ShiftUp();
                UpdateTilemap();
            }
        }

        private void btnTilemapShiftDown_Click(object sender, EventArgs e)
        {
            if (tilemap != null)
            {
                tilemap.ShiftDown();
                UpdateTilemap();
            }
        }

        private void btnTilemapShiftRight_Click(object sender, EventArgs e)
        {
            if (tilemap != null)
            {
                tilemap.ShiftRight();
                UpdateTilemap();
            }
        }

		private void btnEditModePencil_Click(object sender, EventArgs e) 
		{
			changeEditMode(EditMode.Pencil);
		}

		private void btnEditModeRect_Click(object sender, EventArgs e) 
		{
			changeEditMode(EditMode.Rectangle);
		}

		private void btnEditModeRandom_Click(object sender, EventArgs e) 
		{
			changeEditMode(EditMode.Random);
		}

		private void btnEditModeFill_Click(object sender, EventArgs e) {
			changeEditMode(EditMode.Fill);
		}

		#endregion
    }
}
