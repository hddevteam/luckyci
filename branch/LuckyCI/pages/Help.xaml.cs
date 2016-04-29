using System.Windows.Xps.Packaging;

namespace LuckyCI.pages
{
    /// <summary>
    /// Help.xaml 的交互逻辑,读取Help.xps文件
    /// </summary>
    public partial class Help
    {
        public Help()
        {
            InitializeComponent();
            XpsDocument doc = new XpsDocument("../../../common/res/help.xps", System.IO.FileAccess.Read);
            DocumentviewWord.Document = doc.GetFixedDocumentSequence();
        }
    }
}

