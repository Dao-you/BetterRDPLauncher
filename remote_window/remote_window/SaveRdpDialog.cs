using System;
using System.Windows.Forms;

namespace remote_window
{
    public partial class SaveRdpDialog : Form
    {
        public string FileName => textBoxFileName.Text.Trim();
        public bool SavePassword => checkBoxSavePassword.Checked;

        public SaveRdpDialog()
        {
            InitializeComponent();
        }
    }
}
