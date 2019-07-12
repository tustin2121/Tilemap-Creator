﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TMC.Core;

namespace TMC
{
    partial class MainForm
    {
        Tileset tileset;
        DirectBitmap tilesetImage;

        Rectangle tilesetSelection = new Rectangle(0, 0, 1, 1);

        Point tilesetMouseStart = new Point(-1, -1), tilesetMouseCurrent = new Point(-1, -1);
        bool tilesetMouseDown = false;

        private static readonly Color[] palettemapColors = //new Color[]
        {
            Color.FromArgb(128, Color.White),
            Color.FromArgb(128, Color.Yellow),
            Color.FromArgb(128, Color.Red),
            Color.FromArgb(128, Color.Gray),
            Color.FromArgb(128, Color.Cyan),
            Color.FromArgb(128, Color.Blue),
            Color.FromArgb(128, Color.Magenta),
            Color.FromArgb(128, Color.LightYellow),
            Color.FromArgb(128, Color.Teal),
            Color.FromArgb(128, Color.LightSteelBlue),
            Color.FromArgb(128, Color.Violet),
            Color.FromArgb(128, Color.Orange),
            Color.FromArgb(128, Color.LightGray),
            Color.FromArgb(128, Color.SandyBrown),
            Color.FromArgb(128, Color.Purple),
            Color.FromArgb(128, Color.LightPink),
        };

        Bitmap palettesetImage;

        int paletteSelection = 0;

        // Updates Tileset size and image
        void UpdateTileset(bool clearSelection)
        {
            if (tileset == null) return;

            ignore = true;
            if (rModeTilemap.Checked)
            {
                // Get Tileset size
                short width;
                if (!short.TryParse(cmbTilesetWidth.Text, out width) || width <= 0)
                    width = 1;

                int height = (tileset.Length / width) + (tileset.Length % width > 0 ? 1 : 0);

                // Update height text
                txtTilesetHeight.Text = height.ToString();

                // Update Tileset image
                tilesetImage?.Dispose();
                tilesetImage = tileset.ToImage(width);

                pTileset.Size = new Size(tilesetImage.Width * zoom, tilesetImage.Height * zoom);
                pTileset.Image = tilesetImage;

                if (clearSelection)
                    tilesetSelection = new Rectangle(0, 0, 1, 1);

                lTilesetSelection.Text = rModeTilemap.Checked ?
                    $"({tilesetSelection.X}, {tilesetSelection.Y}) to ({tilesetSelection.X + tilesetSelection.Width - 1}, {tilesetSelection.Y + tilesetSelection.Height - 1})" :
                    $"{paletteSelection}";
            }
            else
            {
                pTileset.Size = new Size(palettesetImage.Width * zoom, palettesetImage.Height * zoom);
                pTileset.Image = palettesetImage;
            }
            ignore = false;
        }

        void DrawPalette()
        {
            palettesetImage?.Dispose();
            palettesetImage = new Bitmap(4 * 8, 4 * 8);

            using (var g = Graphics.FromImage(palettesetImage))
            using (var font = new Font("Arial", 5.5f, FontStyle.Regular))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

                for (int i = 0; i < 16; i++)
                {
                    using (var b = new SolidBrush(palettemapColors[i]))
                    {
                        g.FillRectangle(b, i % 4 * 8, i / 4 * 8, 8, 8);
                        g.DrawString(i.ToString("X"), font, Brushes.Black, 1 + i % 4 * 8, i / 4 * 8);
                    }
                }
            }
        }

        private void pTileset_Paint(object sender, PaintEventArgs e)
        {
            if (tileset == null) return;

            if (rModeTilemap.Checked)
            {
                // draw grid
                if (mnuGrid.Checked)
                {
                    using (var pen = new Pen(new SolidBrush(GridColor), 1f))
                    {
                        pen.DashPattern = new[] { 2f, 2f };

                        int size = zoom * Tileset.TileSize;
						int off = zoom / 2;

                        for (int x = 1; x < pTileset.Width / size; x++)
                        {
                            e.Graphics.DrawLine(pen, x * size - off, 0, x * size - off, pTileset.Height);
                        }

                        for (int y = 1; y < pTileset.Height / size; y++)
                        {
                            e.Graphics.DrawLine(pen, 0, y * size - off, pTileset.Width, y * size - off);
                        }
                    }
                }

                // draw current selection
                e.Graphics.DrawRectangle
				(
                    Pens.Yellow,
                    tilesetSelection.X * zoom * Tileset.TileSize - (zoom/2),
                    tilesetSelection.Y * zoom * Tileset.TileSize - (zoom/2),
                    tilesetSelection.Width * zoom * Tileset.TileSize,
                    tilesetSelection.Height * zoom * Tileset.TileSize
				);

                if (tilesetMouseDown)
                { 
                    // draw new selection
                    // corners of the selection rect
                    var upperLeft = new Point(Math.Min(tilesetMouseCurrent.X, tilesetMouseStart.X),
                        Math.Min(tilesetMouseCurrent.Y, tilesetMouseStart.Y));
                    var bottomRight = new Point(Math.Max(tilesetMouseCurrent.X, tilesetMouseStart.X),
                        Math.Max(tilesetMouseCurrent.Y, tilesetMouseStart.Y));

                    // create selection rect
                    var bounds = new Rectangle(upperLeft.X, upperLeft.Y,
                        bottomRight.X - upperLeft.X + 1, bottomRight.Y - upperLeft.Y + 1);

                    e.Graphics.DrawRectangle
					(
                        Pens.Red,
                        bounds.X * zoom * Tileset.TileSize - (zoom/2),
                        bounds.Y * zoom * Tileset.TileSize - (zoom/2),
                        bounds.Width * zoom * Tileset.TileSize,
                        bounds.Height * zoom * Tileset.TileSize
                    );
                }
            }
            else
            {
                // draw selection
                e.Graphics.DrawRectangle
				(
                    Pens.Red,
                    paletteSelection % 4 * zoom * 8, 
                    paletteSelection / 4 * zoom * 8, 
                    zoom * 7, 
                    zoom * 7
                );
            }
        }

        private void pTileset_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.X < 0 || e.Y < 0 || e.X >= pTileset.Width || e.Y >= pTileset.Height)
                return;

            var x = e.X / (zoom * Tileset.TileSize);
            var y = e.Y / (zoom * Tileset.TileSize);

            if (e.Button == MouseButtons.Left && rModeTilemap.Checked)
            {
                tilesetMouseStart.X = x;
                tilesetMouseStart.Y = y;

                tilesetMouseCurrent.X = x;
                tilesetMouseCurrent.Y = y;

                tilesetMouseDown = true;
                pTileset.Invalidate();
            }
        }

        private void pTileset_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.X < 0 || e.Y < 0 || e.X >= pTileset.Width || e.Y >= pTileset.Height)
                return;

            tilesetMouseCurrent.X = e.X / (zoom * Tileset.TileSize);
            tilesetMouseCurrent.Y = e.Y / (zoom * Tileset.TileSize);

            lPosition.Text = rModeTilemap.Checked ?
                $"Tileset: ({tilesetMouseCurrent.X}, {tilesetMouseCurrent.Y})" :
                $"Palette: ({tilesetMouseCurrent.X}, {tilesetMouseCurrent.Y})";

            lTile.Text = $"Tile: 000";
            lPalette.Text = $"Palette: 0";
            lFlip.Text = "Flip: None";

            if (tilesetMouseDown)
                pTileset.Invalidate();
        }

        private void pTileset_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (rModeTilemap.Checked && tilesetMouseDown)
                {
                    // corners of the selection rect
                    var upperLeft = new Point(Math.Min(tilesetMouseCurrent.X, tilesetMouseStart.X),
                        Math.Min(tilesetMouseCurrent.Y, tilesetMouseStart.Y));
                    var bottomRight = new Point(Math.Max(tilesetMouseCurrent.X, tilesetMouseStart.X),
                        Math.Max(tilesetMouseCurrent.Y, tilesetMouseStart.Y));

                    // create selection rect
                    tilesetSelection = new Rectangle(upperLeft.X, upperLeft.Y,
                        bottomRight.X - upperLeft.X + 1, bottomRight.Y - upperLeft.Y + 1);

                    lTilesetSelection.Text = rModeTilemap.Checked ?
                        $"({tilesetSelection.X}, {tilesetSelection.Y}) to ({tilesetSelection.X + tilesetSelection.Width - 1}, {tilesetSelection.Y + tilesetSelection.Height - 1})" :
                        $"{paletteSelection}";

                    tilesetMouseDown = false;
                }
                else
                {
                    // select new palette
                    paletteSelection = tilesetMouseCurrent.X + tilesetMouseCurrent.Y * 4;
                }

                pTileset.Invalidate();
            }
        }

        private void cmbTilesetWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar)))
            {
                e.Handled = true;
            }
        }

        private void cmbTilesetWidth_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ignore)
            {
                UpdateTileset(true);
            }
        }

        private void cmbTilesetWidth_TextChanged(object sender, EventArgs e)
        {
            if (!ignore)
            {
                UpdateTileset(true);
            }
        }
    }
}
