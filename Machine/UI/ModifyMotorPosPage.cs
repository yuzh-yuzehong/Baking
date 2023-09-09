using System.Windows.Forms;

namespace Machine
{
    public partial class ModifyMotorPosPage : Form
    {
        public ModifyMotorPosPage()
        {
            InitializeComponent();
        }

        public void SetPosNameValue(string name, string value)
        {
            this.textBoxPosName.Text = name;
            this.textBoxPosValue.Text = value;
        }

        public string GetPosName()
        {
            return this.textBoxPosName.Text;
        }

        public string GetPosValue()
        {
            return this.textBoxPosValue.Text;
        }

        private void buttonSave_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
