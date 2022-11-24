using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Doodle_203149T_v2
{
    public partial class MainForm_203149T : Form
    {
        Bitmap bm;
        Graphics g;
        int panIndex;
        string strText;
        int shapeIndex;
        bool flagDraw = false;
        bool flagText = false;
        bool flagFill = false;
        bool flagHand = false;
        bool flagErase = false;
        bool flagShaping = false;
        Point mouseDownCoords;
        PointF mouseMoveCoords;
        Bitmap brushPattern;
        PointF endP = new Point(0, 0);
        PointF startP = new Point(0, 0);
        Pen pen = new Pen(Color.Black, 5);
        FontStyle textStyle = FontStyle.Regular;
        SolidBrush brush = new SolidBrush(Color.Black);
        float mouseMoveX, mouseMoveY, mouseDownX, mouseDownY, shapeWidth, shapeHeight;

        float Zoom { get; set; }

        Size ImgSize { get; set; }

        public Matrix ScaleM { get; set; }

        public MainForm_203149T()
        {
            InitializeComponent();
        }

        private void NunezMylesBlasco_203149TToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var attribute = (GuidAttribute)assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0];
            Clipboard.SetText(attribute.Value.ToString());
        }

        private void MainForm_203149T_Load(object sender, EventArgs e)
        {
            bm = new Bitmap(picBoxMain.Width, picBoxMain.Height);
            picBoxMain.Image = bm;
            g = Graphics.FromImage(bm);
            Rectangle rect = picBoxMain.ClientRectangle;
            g.FillRectangle(new SolidBrush(Color.GhostWhite), rect);
            g.Dispose();
            picBoxMain.Invalidate();

            picBoxBold.Tag = "A";
            picBoxItalic.Tag = "A";
            picBoxUnderline.Tag = "A";
            picBoxCurrentTool.Tag = "A";

            cmb_FontSelect.DataSource = new InstalledFontCollection().Families;
            cmb_FontSelect.DisplayMember = "Name";
            cmb_FontSelect.SelectedIndex = 2;
            cmb_BrushSelect.SelectedItem = "Pen";
            cmb_TransformSelect.SelectedItem = "Light Red";
            cmb_GreyScaleSelect.SelectedItem = "Average";

            ScaleM = new Matrix();
            ImgSize = picBoxMain.Image.Size;
            picBoxMain.Size = ImgSize;
            SetZoom(100); // default zoom at 100%
            DoubleBuffered = true;
        }

        public void SetZoom(float zoomfactor)
        {
            Zoom = zoomfactor / 100f;

            ScaleM.Reset();
            ScaleM.Scale(Zoom, Zoom);
            if (ImgSize != Size.Empty)
            {
                float oldRatio = picBoxMain.Width * Zoom / panelZoomScrollBars.ClientRectangle.Width;
                PointF oldPanelCoords = new PointF(panelZoomScrollBars.ClientRectangle.Width / 2, panelZoomScrollBars.ClientRectangle.Height / 2);
                PointF oldPicBoxMainCoords = new PointF(-picBoxMain.Left + oldPanelCoords.X, -picBoxMain.Top + oldPanelCoords.Y);
                PointF oldImageCoords = new PointF((float)(oldPicBoxMainCoords.X / oldRatio), (float)(oldPicBoxMainCoords.Y / oldRatio));

                picBoxMain.Size = new Size((int)(ImgSize.Width * Zoom), (int)(ImgSize.Height * Zoom));

                float newRatio = picBoxMain.Width * Zoom / panelZoomScrollBars.ClientRectangle.Width;
                PointF newPicBoxMainCoords = new PointF((int)(oldImageCoords.X * newRatio), (int)(oldImageCoords.Y * newRatio));
                panelZoomScrollBars.AutoScrollPosition = new Point((int)(newPicBoxMainCoords.X - oldPanelCoords.X), (int)(newPicBoxMainCoords.Y - oldPanelCoords.Y));
            }
        }

        private void picBoxMain_MouseDown(object sender, MouseEventArgs e)
        {
            startP = ScalePoint(e.Location);
            if (flagShaping == true)
            {
                mouseDownX = startP.X;
                mouseDownY = startP.Y;
                picBoxMain.Invalidate();
                if (picBoxCurrentTool.Tag.ToString() == "B")
                    shapeIndex = 1;
                else if (picBoxCurrentTool.Tag.ToString() == "C")
                    shapeIndex = 2;
                else if (picBoxCurrentTool.Tag.ToString() == "D")
                    shapeIndex = 3;
                else if (picBoxCurrentTool.Tag.ToString() == "E")
                    shapeIndex = 4;
            }
            else if (flagHand == true)
            {
                panIndex = 1;
                mouseDownCoords = e.Location;
            }
            else if (flagText == true)
            {
                strText = txtBoxText.Text;
                if (txtBoxText.Text == "Enter text here...")
                    strText = "";
                g = Graphics.FromImage(bm);
                brush = new SolidBrush(picBoxBrushColor.BackColor);
                Font textFont = new Font((FontFamily)cmb_FontSelect.SelectedValue, float.Parse(numUD_SizeSelect.Text) * 5, textStyle);
                g.DrawString(strText, textFont, brush, startP.X, startP.Y);
                picBoxMain.Invalidate();
            }
            else if (flagFill == true)
            {
                FloodFill(e.Location, picBoxBrushColor.BackColor);
            }
            else if (e.Button == MouseButtons.Left)
            {
                flagDraw = true;
                if (cmb_BrushSelect.Text == "Marker")
                    brushPattern = TintImages(Properties.Resources.testbrush_10, picBoxBrushColor.BackColor);
                else if (cmb_BrushSelect.Text == "Calligraphy")
                    brushPattern = TintImages(Properties.Resources.testbrush_12, picBoxBrushColor.BackColor);
                else if (cmb_BrushSelect.Text == "Pencil")
                    brushPattern = TintImages(Properties.Resources.testbrush_16, picBoxBrushColor.BackColor);
            }
        }

        private void FloodFill(Point pt, Color replacementColor)
        {
            Color targetColor = bm.GetPixel(pt.X, pt.Y);
            if (targetColor.ToArgb().Equals(replacementColor.ToArgb()))
                return;
            Stack<Point> pixels = new Stack<Point>();
            pixels.Push(pt);
            while (pixels.Count != 0)
            {
                Point temp = pixels.Pop();
                int y1 = temp.Y;
                int x1 = temp.X;
                while (y1 >= 0 && bm.GetPixel(x1, y1) == targetColor)
                {
                    y1--;
                }
                y1++;
                bool spanLeft = false;
                bool spanRight = false;
                while (y1 < bm.Height && bm.GetPixel(x1, y1) == targetColor)
                {
                    bm.SetPixel(x1, y1, replacementColor);
                    if (!spanLeft && x1 > 0 && bm.GetPixel(x1 - 1, y1) == targetColor)
                    {
                        pixels.Push(new Point(x1 - 1, y1));
                        spanLeft = true;
                    }
                    else if (spanLeft && x1 - 1 == 0 && bm.GetPixel(x1 - 1, y1) != targetColor)
                    {
                        spanLeft = false;
                    }
                    if (!spanRight && x1 < bm.Width - 1 && bm.GetPixel(x1 + 1, y1) == targetColor)
                    {
                        pixels.Push(new Point(x1 + 1, y1));
                        spanRight = true;
                    }
                    else if (spanRight && x1 < bm.Width - 1 && bm.GetPixel(x1 + 1, y1) != targetColor)
                    {
                        spanRight = false;
                    }
                    y1++;
                }
            }
            picBoxMain.Refresh();
        }

        Bitmap TintImages(Bitmap bmp0, Color colors)
        {
            Bitmap tinted;
            Size sz = bmp0.Size;
            float f = 256f;
            float r = colors.R / f;
            float g = colors.G / f;
            float b = colors.B / f;

            float[][] colorMatrixElements = {
                new float[] {r,  0,  0,  0, 0},         // red scaling factor
                new float[] {0,  g,  0,  0, 0},        // green scaling factor 
                new float[] {0,  0,  b,  0, 0},       // blue scaling factor 
                new float[] {0,  0,  0,  1, 0},      // alpha scaling factor 
                new float[] {0,  0,  0,  0, 1}      // no further translations         
            };

            ImageAttributes imageAttributes = new ImageAttributes();
            ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);
            imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            Bitmap bmp = new Bitmap(sz.Width, sz.Height);
            using (Graphics gr = Graphics.FromImage(bmp))
            {
                gr.DrawImage(bmp0, new Rectangle(0, 0, sz.Width, sz.Height), 0, 0, sz.Width, sz.Height,
                    GraphicsUnit.Pixel, imageAttributes);
                tinted = bmp;
                gr.Dispose();
            }
            return tinted;
        }

        private void picBoxMain_MouseMove(object sender, MouseEventArgs e)
        {
            mouseMoveCoords = ScalePoint(e.Location);
            lblCursorCoords.Text = ((int)(mouseMoveCoords.X * 1.502)).ToString() + ", " + ((int)(mouseMoveCoords.Y * 1.554)).ToString() + "px";
            if (numUD_SizeSelect.Text == "")
            {
                numUD_SizeSelect.Text = "1";
                numUD_SizeSelect.Value = 1;
            }
            if (flagDraw == true)
            {
                endP = mouseMoveCoords;
                g = Graphics.FromImage(bm);
                if (flagErase == false)
                {
                    string brushType = cmb_BrushSelect.Text;
                    switch (brushType)
                    {
                        case "Pen":
                            Pen pen = new Pen(picBoxBrushColor.BackColor, float.Parse(numUD_SizeSelect.Text));
                            g.DrawLine(pen, startP, endP);
                            break;
                        case "Pencil":
                            g.TranslateTransform(-brushPattern.Width / (Zoom * 80), -brushPattern.Height / (Zoom * 80));
                            g.DrawImage(brushPattern, mouseMoveCoords.X, mouseMoveCoords.Y, float.Parse(numUD_SizeSelect.Text) * 2, float.Parse(numUD_SizeSelect.Text) * 2);
                            break;
                        case "Marker":
                            g.TranslateTransform(-brushPattern.Width / (Zoom * 200), -brushPattern.Height / (Zoom * 200));
                            g.DrawImage(brushPattern, mouseMoveCoords.X, mouseMoveCoords.Y, float.Parse(numUD_SizeSelect.Text) * (float)1.4, float.Parse(numUD_SizeSelect.Text) * (float)1.4);
                            break;
                        case "Calligraphy":
                            g.TranslateTransform(-brushPattern.Width / (Zoom * 110), -brushPattern.Height / (Zoom * 110));
                            g.DrawImage(brushPattern, mouseMoveCoords.X, mouseMoveCoords.Y, float.Parse(numUD_SizeSelect.Text) * (float)2.295, float.Parse(numUD_SizeSelect.Text) * (float)2.295);
                            break;
                        default:
                            break;
                    }
                }
                else
                    g.FillRectangle(brush, endP.X, endP.Y, float.Parse(numUD_SizeSelect.Text) * 3,
                        float.Parse(numUD_SizeSelect.Text) * 3);
                g.Dispose();
                picBoxMain.Invalidate();
            }
            else if (flagHand == true && panIndex == 1)
            {
                int X = mouseDownCoords.X - e.X;
                int Y = mouseDownCoords.Y - e.Y;
                panelZoomScrollBars.AutoScrollPosition = new Point(X - panelZoomScrollBars.AutoScrollPosition.X, Y - panelZoomScrollBars.AutoScrollPosition.Y);
            }
            else if (flagShaping == true)
            {
                PointF newCoords = mouseMoveCoords;
                mouseMoveX = newCoords.X;
                mouseMoveY = newCoords.Y;
                shapeWidth = mouseMoveX - mouseDownX;
                shapeHeight = mouseMoveY - mouseDownY;
            }
            startP = endP;
        }

        private void picBoxMain_MouseLeave(object sender, EventArgs e)
        {
            lblCursorCoords.Text = "";
        }

        PointF ScalePoint(PointF pt)
        {
            return new PointF(pt.X / Zoom, pt.Y / Zoom);
        }

        private void picBoxMain_MouseUp(object sender, MouseEventArgs e)
        {
            panIndex = 0;
            flagDraw = false;
            if (flagShaping == true)
            {
                shapeWidth = mouseMoveX - mouseDownX;
                shapeHeight = mouseMoveY - mouseDownY;
                Pen pen = new Pen(picBoxBrushColor.BackColor, float.Parse(numUD_SizeSelect.Text));
                g = Graphics.FromImage(bm);
                if (shapeIndex == 1)
                    g.DrawEllipse(pen, mouseDownX, mouseDownY, shapeWidth, shapeHeight);
                else if (shapeIndex == 2)
                    g.DrawRectangle(pen, mouseDownX, mouseDownY, shapeWidth, shapeHeight);
                else if (shapeIndex == 3)
                    g.DrawLine(pen, mouseDownX, mouseDownY, mouseMoveX, mouseMoveY);
                else if (shapeIndex == 4)
                    g.DrawImage(picBoxCurrentTool.Image, mouseDownX, mouseDownY, shapeWidth, shapeHeight);
                g.Dispose();
                picBoxMain.Invalidate();
                mouseDownX = mouseDownY = shapeWidth = shapeHeight = shapeIndex = 0;
            }
        }

        private void picBoxMain_MouseEnter(object sender, EventArgs e)
        {
            if (flagHand == false)
            {
                picBoxMain.Cursor = Cursors.Cross;
            }
        }

        private void picBoxMain_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.MultiplyTransform(ScaleM);
            Graphics g = e.Graphics;
            Pen pen = new Pen(picBoxBrushColor.BackColor);
            if (shapeIndex > 0)
            {
                if (shapeIndex == 1)
                    g.DrawEllipse(pen, mouseDownX, mouseDownY, shapeWidth, shapeHeight);
                else if (shapeIndex == 2)
                    g.DrawRectangle(pen, mouseDownX, mouseDownY, shapeWidth, shapeHeight);
                else if (shapeIndex == 3)
                    g.DrawLine(pen, mouseDownX, mouseDownY, mouseMoveX, mouseMoveY);
                else if (shapeIndex == 4)
                    g.DrawImage(picBoxCurrentTool.Image, mouseDownX, mouseDownY, shapeWidth, shapeHeight);
                picBoxMain.Invalidate();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfdlg = new SaveFileDialog())
            {
                sfdlg.Title = "Save Image";
                sfdlg.Filter = "Image Files(*.BMP)|*.BMP|All files (*.*)|*.*";
                if (sfdlg.ShowDialog(this) == DialogResult.OK)
                {
                    using (Bitmap bmp = new Bitmap(picBoxMain.Width, picBoxMain.Height))
                    {
                        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                        picBoxMain.DrawToBitmap(bmp, rect);
                        bmp.Save(sfdlg.FileName, ImageFormat.Bmp);
                        MessageBox.Show("File Saved Successfully");
                    }
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofdlg = new OpenFileDialog())
            {
                ofdlg.Title = "Open Image";
                ofdlg.Filter = "bmp files (*.BMP)|*.BMP|All files (*.*)|*.*";
                if (ofdlg.ShowDialog() == DialogResult.OK)
                {
                    picBoxBackupImage.Load(ofdlg.FileName);
                    Image img = new Bitmap(ofdlg.FileName);
                    g = Graphics.FromImage(bm);
                    panelZoomScrollBars.AutoScroll = false;
                    g.DrawImage(img, panelZoomScrollBars.ClientRectangle);
                    panelZoomScrollBars.AutoScroll = true;
                    g.Dispose();
                    picBoxMain.Invalidate();
                }
            }
        }

        private void picBoxText_Click(object sender, EventArgs e)
        {
            flagText = true;
            flagFill = false;
            flagDraw = false;
            flagHand = false;
            flagErase = false;
            flagShaping = false;
            picBoxCurrentTool.Tag = "A";
            picBoxCurrentTool.Image = picBoxText.Image;
        }

        private void picBoxClear_Click(object sender, EventArgs e)
        {
            g = Graphics.FromImage(bm);
            Rectangle rect = picBoxMain.ClientRectangle;
            g.FillRectangle(new SolidBrush(Color.GhostWhite), rect);
            g.Dispose();
            picBoxMain.Invalidate();
        }

        private void picBoxErase_Click(object sender, EventArgs e)
        {
            flagFill = false;
            flagText = false;
            flagErase = true;
            flagHand = false;
            flagShaping = false;
            picBoxCurrentTool.Tag = "A";
            brush = new SolidBrush(picBoxMain.BackColor);
            picBoxCurrentTool.Image = picBoxErase.Image;
        }

        private void picBoxBrush_Click(object sender, EventArgs e)
        {
            flagFill = false;
            flagText = false;
            flagHand = false;
            flagErase = false;
            flagShaping = false;
            picBoxCurrentTool.Tag = "A";
            pen.Color = picBoxBrushColor.BackColor;
            picBoxCurrentTool.Image = picBoxBrush.Image;
        }

        private void picBoxPenColor_Click(object sender, EventArgs e)
        {
            ColorDialog c = new ColorDialog();
            if (c.ShowDialog() == DialogResult.OK)
            {
                flagErase = false;
                pen.Color = c.Color;
                picBoxBrushColor.BackColor = pen.Color;
                // move custom color to palette after converting from COLORREF to ARGB 
                int[] customColor = { c.CustomColors[0] & 0xff, (c.CustomColors[0] >> 8) & 0xff, (c.CustomColors[0] >> 16) & 0xff, (c.CustomColors[0] >> 24) & 0xff };
                picBoxCustom1.BackColor = Color.FromArgb(255, customColor[0], customColor[1], customColor[2]);
            }
        }

        private void lblBrushesText_Click(object sender, EventArgs e)
        {
            cmb_BrushSelect.DroppedDown = true;
        }

        private void txtBoxText_Enter(object sender, EventArgs e)
        {
            if (txtBoxText.Text == "Enter text here...")
            {
                txtBoxText.Text = "";
                txtBoxText.ForeColor = Color.GhostWhite;
            }
        }

        private void picBoxBold_Click(object sender, EventArgs e)
        {
            if (picBoxBold.Tag.ToString() == "B")
            {
                picBoxBold.Image = Properties.Resources.bold_2;
                picBoxBold.Tag = "A";
                string removeBold = textStyle.ToString().Replace("Bold", "Regular");
                textStyle = (FontStyle)Enum.Parse(typeof(FontStyle), removeBold);
            }
            else
            {
                picBoxBold.Tag = "B";
                picBoxBold.Image = Properties.Resources.bold_click_red;
                textStyle |= FontStyle.Bold;
            }
        }

        private void picBoxItalic_Click(object sender, EventArgs e)
        {
            if (picBoxItalic.Tag.ToString() == "B")
            {
                picBoxItalic.Image = Properties.Resources.italic_2;
                picBoxItalic.Tag = "A";
                string removeItalic = textStyle.ToString().Replace("Italic", "Regular");
                textStyle = (FontStyle)Enum.Parse(typeof(FontStyle), removeItalic);
            }
            else
            {
                picBoxItalic.Tag = "B";
                picBoxItalic.Image = Properties.Resources.italic_click_red;
                textStyle |= FontStyle.Italic;
            }
        }

        private void picBoxUnderline_Click(object sender, EventArgs e)
        {
            if (picBoxUnderline.Tag.ToString() == "B")
            {
                picBoxUnderline.Image = Properties.Resources.underline_2;
                picBoxUnderline.Tag = "A";
                string removeUnderline = textStyle.ToString().Replace("Underline", "Regular");
                textStyle = (FontStyle)Enum.Parse(typeof(FontStyle), removeUnderline);
            }
            else
            {
                picBoxUnderline.Tag = "B";
                picBoxUnderline.Image = Properties.Resources.underline_click_red;
                textStyle |= FontStyle.Underline;
            }
        }

        private void picBoxEllipse_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Tag = "B";
            picBoxCurrentTool.Image = picBoxEllipse.Image;
            picBoxShapes();
        }
        private void picBoxRectangle_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Tag = "C";
            picBoxCurrentTool.Image = picBoxRectangle.Image;
            picBoxShapes();
        }

        private void picBoxLine_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Tag = "D";
            picBoxCurrentTool.Image = picBoxLine.Image;
            picBoxShapes();
        }

        private void picBoxShapes()
        {
            flagDraw = false;
            flagFill = false;
            flagText = false;
            flagHand = false;
            flagErase = false;
            flagShaping = true;
        }

        private void btnTransform_Click(object sender, EventArgs e)
        {
            Bitmap oImage = (Bitmap)picBoxBackupImage.Image;
            if (oImage == null)
                return;
            int pixelcol;
            Cursor.Current = Cursors.WaitCursor;
            for (int i = 0; i < oImage.Width; i++)
                for (int j = 0; j < oImage.Height; j++)
                {
                    pixelcol = oImage.GetPixel(i, j).ToArgb();
                    string str = cmb_TransformSelect.Text;
                    switch (str)
                    {
                        case "Light Red":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x00FF0000));
                            break;
                        case "Dark Red":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x005A0000));
                            break;
                        case "Light Blue":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x000000FF));
                            break;
                        case "Dark Blue":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x00000072));
                            break;
                        case "Magenta":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x00560056));
                            break;
                        case "White":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x00FFFFFF));
                            break;
                        case "Black":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x00000000));
                            break;
                        case "Light Grey":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x00A2A2A2));
                            break;
                        case "Dark Grey":
                            oImage.SetPixel(i, j, Color.FromArgb(pixelcol & 0x00121212));
                            break;
                        default:
                            break;
                    }
                }
            picBoxMain.Refresh();
            Image img = new Bitmap(picBoxBackupImage.Image);
            g = Graphics.FromImage(bm);
            g.DrawImage(img, picBoxMain.ClientRectangle);
            g.Dispose();
            picBoxMain.Invalidate();
            Cursor.Current = Cursors.Cross;
            picBoxBackupImage.Image = oImage;
        }

        private void btnGreyScale_Click(object sender, EventArgs e)
        {
            Bitmap oImage;
            oImage = (Bitmap)picBoxBackupImage.Image;
            if (oImage == null)
                return;
            Color col;
            int red, green, blue, gray;
            Cursor.Current = Cursors.WaitCursor;
            for (int i = 0; i < oImage.Width; i++)
                for (int j = 0; j < oImage.Height; j++)
                {
                    col = oImage.GetPixel(i, j);
                    red = col.R;
                    green = col.G;
                    blue = col.B;
                    if (cmb_GreyScaleSelect.Text == "Average")
                    {
                        gray = (red + green + blue) / 3;
                        oImage.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                    }
                    else if (cmb_GreyScaleSelect.Text == "Luminosity")
                    {
                        int gRed = (int)(0.21 * red);
                        int gGreen = (int)(0.72 * green);
                        int gBlue = (int)(0.07 * blue);
                        gray = (gRed + gGreen + gBlue) / 3;
                        oImage.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                        
                    }
                }
            picBoxMain.Refresh();
            Image img = new Bitmap(picBoxBackupImage.Image);
            g = Graphics.FromImage(bm);
            g.DrawImage(img, picBoxMain.ClientRectangle);
            g.Dispose();
            picBoxMain.Invalidate();
            Cursor.Current = Cursors.Cross;
            picBoxBackupImage.Image = oImage;
        }
        private void picBoxHand_Click(object sender, EventArgs e)
        {
            flagHand = true;
            flagDraw = false;
            flagFill = false;
            flagText = false;
            flagErase = false;
            flagShaping = false;
            picBoxCurrentTool.Tag = "A";
            picBoxMain.Cursor = Cursors.Hand;
            picBoxCurrentTool.Image = picBoxHand.Image;
        }

        private void lblShapeSection_Click(object sender, EventArgs e)
        {
            panelText.Visible = false;
            panelShapes.Visible = true;
            lblShapeSection.ForeColor = Color.Red;
            lblTextSection.ForeColor = Color.DarkGray;
        }

        private void lblTextSection_Click(object sender, EventArgs e)
        {
            panelText.Visible = true;
            panelShapes.Visible = false;
            lblTextSection.ForeColor = Color.Red;
            lblShapeSection.ForeColor = Color.DarkGray;
        }

        private void lblToolSection_Click(object sender, EventArgs e)
        {
            panelImage.Visible = false;
            panelTools.Visible = true;
            lblToolSection.ForeColor = Color.Red;
            lblImageSection.ForeColor = Color.DarkGray;
        }

        private void lblViewSection_Click(object sender, EventArgs e)
        {
            panelImage.Visible = true;
            panelTools.Visible = false;
            lblImageSection.ForeColor = Color.Red;
            lblToolSection.ForeColor = Color.DarkGray;
        }

        private void tBar_ZoomInOut_Scroll(object sender, EventArgs e)
        {
            ScaleM = new Matrix();
            ImgSize = picBoxMain.Image.Size;
            picBoxMain.Size = ImgSize;
            SetZoom(tBar_ZoomInOut.Value * 100);
            lblZoomPercent.Text = (tBar_ZoomInOut.Value * 100).ToString() + "%";
            if (tBar_ZoomInOut.Value > 1 && picBoxFill.Enabled == true)
            {
                // fill function does not work as expected when zoom in, reset to drawing and disable fill
                if (flagFill == true)
                {
                    flagFill = false;
                    pen.Color = picBoxBrushColor.BackColor;
                    picBoxCurrentTool.Image = picBoxBrush.Image;
                }
                picBoxFill.Enabled = false;
            }
            else if (tBar_ZoomInOut.Value == 1)
                picBoxFill.Enabled = true;
        }

        private void lblColorSection_Click(object sender, EventArgs e)
        {
            panelColors.Visible = true;
            panelClipart.Visible = false;
            lblColorSection.ForeColor = Color.Red;
            lblClipartSection.ForeColor = Color.DarkGray;
        }

        private void lblImageSection_Click(object sender, EventArgs e)
        {
            panelClipart.Visible = true;
            panelColors.Visible = false;
            lblClipartSection.ForeColor = Color.Red;
            lblColorSection.ForeColor = Color.DarkGray;
        }

        private void picBoxFill_Click(object sender, EventArgs e)
        {
            flagFill = true;
            flagDraw = false;
            flagText = false;
            flagHand = false;
            flagErase = false;
            flagShaping = false;
            picBoxCurrentTool.Tag = "A";
            picBoxCurrentTool.Image = picBoxFill.Image;
        }

        private void picBoxDarkRed_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxDarkRed.BackColor;
        }

        private void picBoxPurple_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxPurple.BackColor;
        }

        private void picBoxTurquoise_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxTurquoise.BackColor;
        }

        private void picBoxIndigo_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxIndigo.BackColor;
        }

        private void picBoxLightGrey_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxLightGrey.BackColor;
        }

        private void picBoxBrown_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxBrown.BackColor;
        }

        private void picBoxPink_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxPink.BackColor;
        }

        private void picBoxGold_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxGold.BackColor;
        }

        private void picBoxLightYellow_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxLightYellow.BackColor;
        }

        private void picBoxLime_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxLime.BackColor;
        }

        private void picBoxPaleTurquoise_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxPaleTurquoise.BackColor;
        }

        private void picBoxLightSteelBlue_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxLightSteelBlue.BackColor;
        }

        private void picBoxThistle_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxThistle.BackColor;
        }
        private void picBoxWhite_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxWhite.BackColor;
        }

        private void picBoxBlack_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxBlack.BackColor;
        }

        private void picBoxGreen_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxGreen.BackColor;
        }

        private void picBoxOrange_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxOrange.BackColor;
        }

        private void picBoxYellow_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxYellow.BackColor;
        }
        private void picBoxGrey_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxGrey.BackColor;
        }

        private void picBoxRed_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxRed.BackColor;
        }

        private void picBoxCustom1_Click(object sender, EventArgs e)
        {
            picBoxBrushColor.BackColor = picBoxCustom1.BackColor;
        }
        private void picBoxEmoji1_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji1.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji2_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji2.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji3_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji3.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji4_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji4.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji5_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji5.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji6_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji6.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji7_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji7.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji8_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji8.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji9_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji9.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji10_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji10.Image;
            picBoxEmojis();
        }

        private void txtBoxText_MouseLeave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBoxText.Text))
            {
                txtBoxText.Text = "Enter text here...";
                txtBoxText.ForeColor = Color.DarkGray;
                picBoxMain.Focus();
            }
        }

        private void picBoxMain_ParentChanged(object sender, EventArgs e)
        {

        }

        private void picBoxEmoji11_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji11.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji12_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji12.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji13_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji13.Image;
            picBoxEmojis();
        }

        private void picBoxEmoji14_Click(object sender, EventArgs e)
        {
            picBoxCurrentTool.Image = picBoxEmoji14.Image;
            picBoxEmojis();
        }

        private void picBoxEmojis()
        {
            flagDraw = false;
            flagFill = false;
            flagText = false;
            flagHand = false;
            flagErase = false;
            flagShaping = true;
            picBoxCurrentTool.Tag = "E";
        }
    }
}
