using System.Windows.Forms;

namespace Machine
{
    /// <summary>
    /// 设备使用的基类Form，对Form进行扩展
    /// </summary>
    public class FormEx : Form
    {
        /// <summary>
        /// UI使能状态
        /// </summary>
        protected enum UIEnable
        {
            AllDisabled,            // 所有都禁用
            AllEnabled,             // 所有都启用
            AdminEnabled,           // 管理员状态启用
            MaintenanceEnabled,     // 维护员状态启用
            OperatorEnabled,        // 操作员状态启用
        }

        /// <summary>
        /// 进行额外初始化操作时调用
        /// </summary>
        /// <returns></returns>
        public virtual void InitializeForm()
        {
            
        }

        /// <summary>
        /// 关闭窗口前销毁自定义非托管资源
        /// </summary>
        /// <returns></returns>
        public virtual void DisposeForm()
        {
            
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public virtual void UIVisibleChanged(bool show)
        {
            this.Visible = show;
        }

        /// <summary>
        /// 当设备状态或用户权限改变时，更新UI界面的使能
        /// </summary>
        /// <param name="mc">j设备运行状态</param>
        /// <param name="level">用户等级</param>
        public virtual void UpdataUIEnable(SystemControlLibrary.MCState mc, SystemControlLibrary.UserLevelType level)
        {

        }
    }
}
