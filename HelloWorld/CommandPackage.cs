using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using HelloWorld.Common.Extension;
using HelloWorld.Form;
using HelloWorld.Model;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace HelloWorld
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(CommandPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class CommandPackage : AsyncPackage
    {
        /// <summary>
        /// CommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "d1dc94b9-1fe0-4c79-9e3d-456398fbd988";

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        public CommandPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                var menuCommandId = new CommandID(Command.CommandSet, Command.CommandId);

                var menuItem = new OleMenuCommand(AutoBuildEntityEvent, menuCommandId);
                mcs.AddCommand(menuItem);
            }

        }
        #region 初始化
        #endregion

        #endregion

        /// <summary>
        /// 获取选中的项目所有信息
        /// </summary>
        /// <returns></returns>
        private SelectedProject GetSelectedProject()
        {
            var dte = (DTE)GetService(typeof(SDTE));

            //获取选中项目信息
            var projectInfo = dte.GetSelectedProjectInfo();

            return projectInfo;
        }
        /// <summary>
        /// 按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoBuildEntityEvent(object sender, EventArgs e)
        {
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));

            //获取选中项目信息
            var autoBuildEntityContent = new AutoBuildEntityContent { SelectedProject = GetSelectedProject() };
            if (autoBuildEntityContent.SelectedProject == null)
            {
                uiShell.ShowMessageBox("获取项目信息失败");
                return;
            }

            //读取选中项目下的配置信息
            var entityXmlModel = new EntityXml(autoBuildEntityContent.SelectedProject.EntityXmlPath);
            entityXmlModel.Load();
            autoBuildEntityContent.EntityXml = entityXmlModel;

            try
            {
                //读取表集合
                autoBuildEntityContent.TablesName = GetTables(entityXmlModel.ConnString);
            }
            catch (Exception ex)
            {
                uiShell.ShowMessageBox(string.Format("数据库访问异常:{0}", ex.Message));
                return;
            }

            new MainForm(autoBuildEntityContent).ShowDialog();
        }
        /// <summary>
        /// 获取物理表
        /// </summary>
        /// <param name="sqlstr"></param>
        /// <returns></returns>
        private List<string> GetTables(string sqlstr)
        {
            var dbTable = new DbTable(sqlstr);

            return dbTable.QueryTablesName();
        }

    }
    public class ListViewItem
    {
        public string Name { get; set; }
    }
}
