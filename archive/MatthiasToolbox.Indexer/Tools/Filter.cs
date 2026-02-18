using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Indexer.Tools
{
    /// <summary>
    /// on our MS Windows Server 2008 x64. Just need to install x86 version of Microsoft Filters Pack. Standard exe won't allow you to do it, but if you extract files from x86 exe installer (I've used 7zip archiver for that) - there will be msi file that installs just fine. Reboot and docx search will work.
    /// There is an archive with extracted files along with MsiTools.exe which can help to install/repair/debug MSI if you can't install it by just clicking on it.
    /// http://strebkov.googlepages.com/MicrosoftFiltersPackx86.zip
    /// </summary>
    public class FilterProcessor // (TODO)
    {
        // IFilter Usage:

        //TextReader reader=new FilterReader(openFileDialog1.FileName);
        //using (reader)
        //{
        //  textBox1.Text=reader.ReadToEnd();
        //  label1.Text="Text loaded from "+openFileDialog1.FileName;
        //}
    }
}