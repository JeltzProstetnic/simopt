///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: 
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Dienstag, 8. Mai 2007 Matthias Gruber original version
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MatthiasToolbox.Delta.Utilities
{
    class FormsDraw
    {
        #region Structures
            public enum DrawFeedback
            {
                N_A = 0,
                AOK = 1,
                ERR = 2,
                OUT = 3
            }   
            public enum CoordinateMode
            {
                Absolute = 0,
                Relative = 1
            }   
            private struct FormBounds
            {
                public float xMin;
                public float yMin;
                public float xMax;
                public float yMax;
            }
        #endregion
        #region Classvars
            private Graphics G;
            private Color myBackColor = Color.Black;
            private Color myLastColor = Color.Black;
            private CoordinateMode myMode;
            private FormBounds myBounds = new FormBounds();
        #endregion
        #region Properties
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public Color BackColor
            {
                get
                {
                    return myBackColor;
                }
                set
                {
                    myBackColor = value;
                }
            }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public Color LastColor
            {
                get
                {
                    return myLastColor;
                }
            }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public CoordinateMode Mode
            {
                get
                {
                    return myMode;
                }
                set
                {
                    myMode = value;
                }
            }
        #endregion
        #region Constructor
            //############################################
            //## Bounds can be used to draw on relative ##
            //## coordinates from 0 to 100 on both axes ##
            //############################################

            public FormsDraw(Bitmap img)
            {
                G = Graphics.FromImage(img);
                myBounds.xMin = 0;
                myBounds.xMax = img.Width;
                myBounds.yMin = 0;
                myBounds.yMax = img.Height;
                G.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            } 
            
            public FormsDraw(Form Window)
            {
                G = Graphics.FromHwnd(Window.Handle);
                myBounds.xMin = 0;
                myBounds.xMax = Window.Width;
                myBounds.yMin = 0;
                myBounds.yMax = Window.Height;
                G.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            }
            public FormsDraw(Control Window)
            {
                G = Graphics.FromHwnd(Window.Handle);
                myBounds.xMin = 0;
                myBounds.xMax = Window.Width;
                myBounds.yMin = 0;
                myBounds.yMax = Window.Height;
                G.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            }
            public FormsDraw(IntPtr Handle, Rectangle Bounds)
            {
                G = Graphics.FromHwnd(Handle);
                myBounds.xMin = Bounds.Left;
                myBounds.xMax = Bounds.Width;
                myBounds.yMin = Bounds.Top;
                myBounds.yMax = Bounds.Height;
                G.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            }
            public FormsDraw(IntPtr Handle , int xMin, int yMin, int xMax, int yMax)
            {
                G = Graphics.FromHwnd(Handle);
                myBounds.xMin = xMin;
                myBounds.xMax = xMax;
                myBounds.yMin = yMin;
                myBounds.yMax = yMax;
                G.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            }
        #endregion
        #region Disposer
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void Dispose()
            {
                G.Dispose();
            }
        #endregion
        #region Transformers
            
        //#####################################
        //## no effect on absolute draw mode ##
        //#####################################
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private PointF Transform(float x, float y)
        {
            if(myMode == CoordinateMode.Relative)
            {
                return
                    new PointF(myBounds.xMin + (myBounds.xMax - myBounds.xMin)*x/100,
                               myBounds.yMin + (myBounds.yMax - myBounds.yMin)*y/100);
            }
            else
            {
                return new PointF(x, y);
            }
        }
        private void TransformRef(ref PointF p)
        {
            if(myMode == CoordinateMode.Relative)
            {
                p.X = myBounds.xMin + (myBounds.xMax - myBounds.xMin)*p.X/100;
                p.Y = myBounds.yMin + (myBounds.yMax - myBounds.yMin)*p.Y/100;
            }
        }
        private void TransformRef(ref float x, ref float y)
        {
            if(myMode == CoordinateMode.Relative)
            {
                x = myBounds.xMin + (myBounds.xMax - myBounds.xMin)*x/100;
                y = myBounds.yMin + (myBounds.yMax - myBounds.yMin)*y/100;
            }
        }
            
        #endregion
        #region LINE
        
        //############################################
        //## Draw a line                            ##
        //############################################
        public DrawFeedback Line(Color c, float w, PointF p1, PointF p2)
        {
            TransformRef(ref p1);
            TransformRef(ref p2);
            using(Pen p = new Pen(c,w))
            {
                G.DrawLine(p, p1, p2);
            }
            myLastColor = c;
            return DrawFeedback.AOK;
        }
        public DrawFeedback Line(int w, PointF p1, PointF p2)
        {
            TransformRef(ref p1);
            TransformRef(ref p2);
            using(Pen p = new Pen(LastColor, w))
            {
                G.DrawLine(p, p1, p2);
            }
            return DrawFeedback.AOK;
        }
        public DrawFeedback Line(PointF p1, PointF p2)
        {
            TransformRef(ref p1);
            TransformRef(ref p2);
            using (Pen p = new Pen(LastColor, 1))
            {
                G.DrawLine(p, p1, p2);
            }
            return DrawFeedback.AOK;
        }
            
        #endregion
        #region ARROW
        
        //############################################
        //## Draw an arrow                          ##
        //############################################
        public DrawFeedback Arrow(Color c, float w, PointF p1, PointF p2)
        {
            TransformRef(ref p1);
            TransformRef(ref p2);
            float x = Math.Abs(p1.X - p2.X);
            // float y = Math.Abs(p1.Y - p2.Y);
            float x1 = p2.X - x/4;
            float y1 = p2.Y - x/20;
            float x2 = p2.X - x / 20;
            float y2 = p2.Y - x / 4;
            PointF p3 = new PointF(x1, y1);
            PointF p4 = new PointF(x2, y2);
            
            using(Pen p = new Pen(c, w))
            {
                G.DrawLine(p, p1, p2);
                G.DrawLine(p, p2, p3);
                G.DrawLine(p, p2, p4);
                G.DrawLine(p, p3, p4);
            }
            myLastColor = c;
            return DrawFeedback.AOK;
        }

        #endregion
        #region PSET
        //############################################
        //## PSET - Point Setting Extra Traditional ##
        //############################################
        public DrawFeedback Pset(Color c, float w, float x, float y)
        {
            TransformRef(ref x, ref y);
            using (Pen p = new Pen(c, w))
            {
                G.DrawEllipse(p, x, y, w, w);
            }
            myLastColor = c;
            return DrawFeedback.AOK;
        }
         
        public DrawFeedback Pset(Color c, float w, PointF p)
        {
            TransformRef(ref p);
            using (Pen pen = new Pen(c, w))
            {
                G.DrawEllipse(pen, p.X, p.Y, w, w);
            }
            myLastColor = c;
            return DrawFeedback.AOK;
        }   
            
        public DrawFeedback Pset(float w, float x, float y)
        {
            TransformRef(ref x, ref y);
            using (Pen pen = new Pen(myLastColor, w))
            {
                G.DrawEllipse(pen, x, y, w, w);
            }
            return DrawFeedback.AOK;
        }
        public DrawFeedback Pset(float x, float y)
        {
            TransformRef(ref x, ref y);
            using (Pen pen = new Pen(myLastColor, 1))
            {
                G.DrawEllipse(pen, x, y, 1, 1);
            }
            return DrawFeedback.AOK;
        }
    #endregion
        #region TEXT
        //
        //
        //
        public SizeF Write(String Text, String FontName, int FontSize, Color c, float x, float y)
        {
            SizeF tmp;
            TransformRef(ref x, ref y);
            using (Font f = new Font(FontName, FontSize))
            {
                using (SolidBrush sb = new SolidBrush(c))
                {
                    G.DrawString(Text, f, sb, x, y);
                }
                tmp = G.MeasureString(Text, f);
            }
            return tmp;
        }
        #endregion
        #region MAIN

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public void Clear()
            {
                G.Clear(myBackColor);
            }
            
        #endregion
    }
}