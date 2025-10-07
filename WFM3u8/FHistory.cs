using System.Collections.Generic;
using WFM3u8.Models;

namespace WFM3u8
{
    public partial class FHistory : System.Windows.Forms.Form
    {
        public List<DownloadHistory> _downloadHistory = new List<DownloadHistory>();
        public FHistory(List<DownloadHistory> history)
        {
            _downloadHistory = history;
            InitializeComponent();
        }

        private void FHistory_Load(object sender, System.EventArgs e)
        {
            dataGridView1.DataSource = _downloadHistory;
        }
    }
}
