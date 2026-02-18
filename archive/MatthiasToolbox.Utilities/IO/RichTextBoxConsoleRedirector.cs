using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace MatthiasToolbox.Utilities.IO
{
    public class RichTextBoxConsoleRedirector : TextWriter
    {
        #region over

        public override Encoding Encoding
        {
            get { return new UTF8Encoding(); }
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(value);
            target.AppendText(indentString + value.Replace(Environment.NewLine, Environment.NewLine + indentString) + NewLine);
            DoAutoScroll();
        }

        public override void Write(string value)
        {
            base.Write(value);
            target.AppendText(indentString + value.Replace(Environment.NewLine, Environment.NewLine + indentString));
            DoAutoScroll();
        }

        #endregion
        #region cvar

        private bool enabled;
        private bool autoScroll;
        private RichTextBox target;
        private int indent;
        private int indentSize;
        private string indentString;

        #endregion
        #region prop

        public bool AutoScroll
        {
            get { return autoScroll; }
            set { autoScroll = value; }
        }

        public int Indent
        {
            get { return indent; }
            set
            {
                indent = value;
                UpdateIndent();
            }
        }

        public int IndentSize
        {
            get { return indentSize; }
            set
            {
                indentSize = value;
                UpdateIndent();
            }
        }

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value && target == null) throw new InvalidOperationException("RichTextBoxConsoleRedirector cannot be enabled if no target is set.");
                enabled = value;
            }
        }

        public RichTextBox Target
        {
            get { return target; }
            set
            {
                target = value;
                enabled = target != null;
            }
        }

        #endregion
        #region ctor

        public RichTextBoxConsoleRedirector() { }

        public RichTextBoxConsoleRedirector(RichTextBox target, bool autoScroll = false, int indentSize = 4)
        {
            this.indentSize = indentSize;
            this.autoScroll = autoScroll;
            this.target = target;
            enabled = target != null;
            Console.SetOut(this);
        }

        #endregion
        #region impl

        public void Stop()
        {
            Enabled = false;
        }

        #endregion
        #region util

        private void DoAutoScroll()
        {
            if (autoScroll)
            {
                target.SelectionStart = target.TextLength;
                target.ScrollToCaret();
            }
        }

        private void UpdateIndent()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(' ', indent * indentSize);
            indentString = sb.ToString();
        }

        #endregion
    }
}