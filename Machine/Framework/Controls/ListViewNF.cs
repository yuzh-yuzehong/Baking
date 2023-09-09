using System.Windows.Forms;

namespace Machine
{
    /// <summary>
    /// ListViewNF（NF=Never/No Flickering）
    /// </summary>
    public class ListViewNF : ListView
    {
        public ListViewNF()
        {
            // Activate double buffering
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }
    }
}
