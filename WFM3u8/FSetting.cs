using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Provider;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WFM3u8
{
    public partial class FSetting : System.Windows.Forms.Form
    {
        public Action Reload;
        public FSetting()
        {
            InitializeComponent();
        }

        private void FSetting_Load(object sender, EventArgs e)
        {
            StaticData.Load();
            txtProxy.Text = StaticData.Proxy;
            txtExport.Text = StaticData.Export.ToString();
            txtFFmpeg.Text = StaticData.Ffmpeg.ToString();
            cbProxy.Checked = StaticData.UseProxy;
            if (StaticData.UseProxy)
            {
                txtProxy.Enabled = true;
            }
            else
            { 
                txtProxy.Enabled = false;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtProxy.Text))
            {
                MessageBox.Show("请输入代理地址");
                return;
            }
            if (string.IsNullOrEmpty(txtProxy.Text))
            {
                MessageBox.Show("请输入代理地址");
                return;
            }
            if (string.IsNullOrEmpty(txtExport.Text))
            {
                MessageBox.Show("请输入保存地址");
                return;
            }
            if (string.IsNullOrEmpty(txtFFmpeg.Text))
            {
                MessageBox.Show("请输入FFmpeg路径");
                return;
            }
            Uri uri1;
            if (Uri.TryCreate(txtProxy.Text, UriKind.Absolute, out uri1))
            {
                Uri uri = new Uri(txtProxy.Text);
                StaticData.ProxyHost = uri.Host;
                StaticData.ProxyPort = uri.Port;
            }
            else
            {
                MessageBox.Show("请输入有效代理地址");
                return;
            }
            StaticData.UseProxy = cbProxy.Checked;
            StaticData.Export = txtExport.Text;
            StaticData.Ffmpeg = txtFFmpeg.Text;
            StaticData.Update();
            if (Reload != null)
            {
                Reload.Invoke();
            }
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "请选择一个文件夹";
                folderDialog.ShowNewFolderButton = true;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtExport.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "请选择文件";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                openFileDialog.Filter = "可执行文件|*.exe|所有文件 (*.*)|*.*";
                openFileDialog.DefaultExt = "exe";
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFFmpeg.Text = openFileDialog.FileName;
                }
            }
        }

        private void cbProxy_CheckedChanged(object sender, EventArgs e)
        {
            if (cbProxy.Checked)
            {
                txtProxy.Enabled = true;
            }
            else
            {
                txtProxy.Enabled = false;
            }
        }
    }
}
