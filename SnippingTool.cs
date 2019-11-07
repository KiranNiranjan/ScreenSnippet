//Licensed to the Apache Software Foundation(ASF) under one
//or more contributor license agreements.See the NOTICE file
//distributed with this work for additional information
//regarding copyright ownership.The ASF licenses this file
//to you under the Apache License, Version 2.0 (the
//"License"); you may not use this file except in compliance
//with the License.  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing,
//software distributed under the License is distributed on an
//"AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
//KIND, either express or implied.  See the License for the
//specific language governing permissions and limitations
//under the License.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Paragon.Plugins.ScreenCapture
{
    /// <summary>
    /// Utility for getting a screen-shot of a select portion of the screen.
    /// </summary>
    public sealed partial class SnippingTool : Form
    {
        public Rectangle SelectedRectangle = new Rectangle();
        private Point _pntStart;

        public SnippingTool(Image screenShot, Rectangle position)
        {
            InitializeComponent();
            base.BackgroundImage = screenShot;
            base.ShowInTaskbar = false;
            base.FormBorderStyle = FormBorderStyle.None;
            base.Left = position.Left;
            base.Top = position.Top;
            base.Size = position.Size;
            base.DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
            // if window loses focus for whatever reason, exit
            Deactivate += OnSnippingToolDeactivate;
            base.Load += OnLoad;
        }

        public Image Image { get; set; }

        internal static InternalSnippet TakeSnippet()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var height = SystemInformation.VirtualScreen.Height;
                var width = SystemInformation.VirtualScreen.Width;
                var X = SystemInformation.VirtualScreen.X;
                var Y = SystemInformation.VirtualScreen.Y;
                var rectangle = new Rectangle(X, Y, width, height);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(rectangle.ToString());
                sb.AppendLine("Starting to create new bitmap");
                File.AppendAllText(desktopPath + "\\screensnippetlogs.txt", sb.ToString());
                using (var bmp = new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format32bppPArgb))
                {
                    sb.AppendLine("Graphics.FromImage converting bmp to graphic");
                    File.AppendAllText(desktopPath + "\\screensnippetlogs.txt", sb.ToString());
                    using (var graphics = Graphics.FromImage(bmp))
                    {
                        sb.AppendLine("opening SnippingTool window for editing");
                        sb.AppendLine(bmp.Width.ToString());
                        sb.AppendLine(bmp.Height.ToString());
                        File.AppendAllText(desktopPath + "\\screensnippetlogs.txt", sb.ToString());
                        using (var snipper = new SnippingTool(bmp, rectangle))
                        {
                            graphics.CopyFromScreen(X, Y, 0, 0, bmp.Size);

                            if (snipper.ShowDialog() == DialogResult.OK)
                            {
                                return new InternalSnippet(snipper.Image, snipper.SelectedRectangle);
                            }

                            return null;
                        }
                    }
                }
            }
            catch (Exception err) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Expection TakeSnippet");
                sb.AppendLine(err.ToString());
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                File.AppendAllText(desktopPath + "\\screensnippetlogs.txt", sb.ToString());
                return null;
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            base.Load -= OnLoad;
            base.Cursor = Cursors.Cross;
        }

        private void OnSnippingToolDeactivate(object sender, EventArgs e)
        {
            base.DialogResult = DialogResult.Cancel;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Start the snip on mouse down
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            _pntStart = e.Location;
            SelectedRectangle = new Rectangle(e.Location, new Size(0, 0));
            base.Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // Modify the selection on mouse move
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            var x1 = Math.Min(e.X, _pntStart.X);
            var y1 = Math.Min(e.Y, _pntStart.Y);
            var x2 = Math.Max(e.X, _pntStart.X);
            var y2 = Math.Max(e.Y, _pntStart.Y);
            SelectedRectangle = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            base.Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // Complete the snip on mouse-up
            if (SelectedRectangle.Width <= 0 || SelectedRectangle.Height <= 0)
            {
                return;
            }

            // unbind so that the correct result is returned
            Deactivate -= OnSnippingToolDeactivate;
            try
            {
                Image = new Bitmap(SelectedRectangle.Width, SelectedRectangle.Height);
                using (var gr = Graphics.FromImage(Image))
                {
                    gr.DrawImage(base.BackgroundImage, new Rectangle(0, 0, Image.Width, Image.Height),
                        SelectedRectangle, GraphicsUnit.Pixel);
                }
                DialogResult = DialogResult.OK;
            }
            catch (Exception err)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Expection TakeSnippet");
                sb.AppendLine(err.ToString());
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                File.AppendAllText(desktopPath+ "\\screensnippetlogs.txt", sb.ToString());
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw the current selection
            using (Brush br = new SolidBrush(Color.FromArgb(120, Color.White)))
            {
                var x1 = SelectedRectangle.X;
                var x2 = SelectedRectangle.X + SelectedRectangle.Width;
                var y1 = SelectedRectangle.Y;
                var y2 = SelectedRectangle.Y + SelectedRectangle.Height;
                e.Graphics.FillRectangle(br, new Rectangle(0, 0, x1, base.Height));
                e.Graphics.FillRectangle(br, new Rectangle(x2, 0, base.Width - x2, base.Height));
                e.Graphics.FillRectangle(br, new Rectangle(x1, 0, x2 - x1, y1));
                e.Graphics.FillRectangle(br, new Rectangle(x1, y2, x2 - x1, base.Height - y2));
            }
            using (var pen = new Pen(Color.Red, 3))
            {
                e.Graphics.DrawRectangle(pen, SelectedRectangle);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Allow canceling the snip with the Escape key
            if (keyData == Keys.Escape)
            {
                base.DialogResult = DialogResult.Cancel;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
