﻿// -------------------------------------------------------------------------------------------
// <copyright file="LayoutHelper.cs" company="MapWindow OSS Team - www.mapwindow.org">
//  MapWindow OSS Team - 2015
// </copyright>
// -------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using MW5.Plugins.Printing.Enums;
using MW5.Plugins.Printing.Model.Elements;
using MW5.Plugins.Printing.Properties;
using MW5.Plugins.Printing.Services;
using MW5.Plugins.Services;

namespace MW5.Plugins.Printing.Helpers
{
    internal static class LayoutHelper
    {
        /// <summary>
        /// Shows a load dialog box and prompts the user to open a layout file
        /// </summary>
        public static string ChooseLayoutFile(IWin32Window parent)
        {
            var ofd = new OpenFileDialog
                          {
                              Title = Strings.LayoutLoadDialogTitle,
                              CheckFileExists = true,
                              Multiselect = false
                          };

            return ofd.ShowDialog(parent) == DialogResult.OK ? ofd.FileName : string.Empty;
        }

        public static LayoutBitmap ConvertElementToBitmap(LayoutElement le, string filename)
        {
            if (le is LayoutBitmap) return null;
            int padding = le is LayoutLegend ? 10 : 0;

            var temp = new Bitmap(Convert.ToInt32(le.Size.Width * 3 + 0.5) + padding,
                Convert.ToInt32(le.Size.Height * 3 + 0.5), PixelFormat.Format32bppArgb);
            temp.SetResolution(96, 96);
            temp.MakeTransparent();
            var g = Graphics.FromImage(temp);
            g.PageUnit = GraphicsUnit.Pixel;
            g.ScaleTransform(300F / 100F, 300F / 100F);
            g.TranslateTransform(-le.LocationF.X, -le.LocationF.Y);
            LayoutElement.DrawElement(le, g, false, false);
            g.Dispose();
            temp.SetResolution(300, 300);
            temp.Save(filename);
            temp.Dispose();
            return new LayoutBitmap { Rectangle = le.Rectangle, Name = le.Name, Filename = filename };
        }

        /// <summary>
        /// Calculates which edge of a rectangle the point intersects with, within a certain limit
        /// </summary>
        public static Edge IntersectElementEdge(RectangleF screen, PointF pt, float limit)
        {
            var ptRect = new RectangleF(pt.X - limit, pt.Y - limit, 2F * limit, 2F * limit);
            if ((pt.X >= screen.X - limit && pt.X <= screen.X + limit) &&
                (pt.Y >= screen.Y - limit && pt.Y <= screen.Y + limit)) return Edge.TopLeft;
            if ((pt.X >= screen.X + screen.Width - limit && pt.X <= screen.X + screen.Width + limit) &&
                (pt.Y >= screen.Y - limit && pt.Y <= screen.Y + limit)) return Edge.TopRight;
            if ((pt.X >= screen.X + screen.Width - limit && pt.X <= screen.X + screen.Width + limit) &&
                (pt.Y >= screen.Y + screen.Height - limit && pt.Y <= screen.Y + screen.Height + limit)) return Edge.BottomRight;
            if ((pt.X >= screen.X - limit && pt.X <= screen.X + limit) &&
                (pt.Y >= screen.Y + screen.Height - limit && pt.Y <= screen.Y + screen.Height + limit)) return Edge.BottomLeft;
            if (ptRect.IntersectsWith(new RectangleF(screen.X, screen.Y, screen.Width, 1F))) return Edge.Top;
            if (ptRect.IntersectsWith(new RectangleF(screen.X, screen.Y, 1F, screen.Height))) return Edge.Left;
            if (ptRect.IntersectsWith(new RectangleF(screen.X, screen.Y + screen.Height, screen.Width, 1F))) return Edge.Bottom;
            if (ptRect.IntersectsWith(new RectangleF(screen.X + screen.Width, screen.Y, 1F, screen.Height))) return Edge.Right;
            return Edge.None;
        }

        /// <summary>
        /// Shows a save dialog box and prompts the user to save a layout file
        /// </summary>
        public static void SaveLayout(
            bool promptSaveAs,
            ref string filename,
            PrinterSettings printerSettings,
            int paperWidth,
            int paperHeight,
            IWin32Window parent,
            List<LayoutElement> elements)
        {
            if (string.IsNullOrWhiteSpace(filename) || promptSaveAs)
            {
                string initPath = Path.GetDirectoryName(Application.ExecutablePath) + @"\Templates\";

                try
                {
                    if (!Directory.Exists(initPath))
                    {
                        Directory.CreateDirectory(initPath);
                    }
                }
                catch
                {
                }

                var sfd = new SaveFileDialog
                              {
                                  Title = Strings.LayoutSaveDialogTitle,
                                  AddExtension = true,
                                  OverwritePrompt = true,
                                  InitialDirectory = initPath
                              };

                if (sfd.ShowDialog(parent) == DialogResult.OK)
                {
                    string tempFilename = sfd.FileName;
                    switch (sfd.FilterIndex)
                    {
                        case 1:
                            try
                            {
                                if (!LayoutSerializer.SaveLayout(tempFilename, printerSettings, elements)) return;

                                filename = tempFilename;
                                MessageService.Current.Info("Layout is saved: " + filename);
                            }
                            catch (Exception e)
                            {
                                MessageService.Current.Warn(Strings.LayoutErrorSave + e.Message);
                            }
                            break;
                        case 2:
                            var outputBitmap = new Bitmap(paperWidth, paperHeight);
                            var g = Graphics.FromImage(outputBitmap);
                            var paperRect = new RectangleF(0F, 0F, paperWidth, paperHeight);
                            g.FillRectangle(Brushes.White, paperRect.X, paperRect.Y, paperRect.Width, paperRect.Height);
                            g.DrawRectangle(Pens.Black, paperRect.X, paperRect.Y, paperRect.Width - 1,
                                paperRect.Height - 1);
                            outputBitmap.Save(sfd.FileName);
                            g.Dispose();
                            outputBitmap.Dispose();
                            break;
                    }
                }
            }
            else
            {
                try
                {
                    if (LayoutSerializer.SaveLayout(filename, printerSettings, elements))
                    {
                        MessageService.Current.Info("Layout is saved: " + filename);
                    }
                }
                catch (Exception e)
                {
                    MessageService.Current.Warn(Strings.LayoutErrorSave + e.Message);
                }
            }
        }
    }
}