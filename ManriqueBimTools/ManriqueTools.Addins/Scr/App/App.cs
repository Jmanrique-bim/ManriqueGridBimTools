using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RtView = Autodesk.Revit.DB.View;
using System.Drawing;
using Application = Autodesk.Revit.ApplicationServices.Application;
using Autodesk.Revit.DB.Events;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using Autodesk.Revit.UI.Events;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using SplitButton = Autodesk.Revit.UI.SplitButton;

namespace ManriqueBimTools
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    internal class ManriqueRevitApp : IExternalApplication
    {
        /// <TransactionMode.Manual>you may use combinations of Transactions as you please</TransactionMode.Manual>
        /// <RegenerationOption.Manual>you may want to regenerate manually</RegenerationOption.Manual>
        /// <IExternalApplication> c) CREATE A CLASS THAT INHERITE IExternalApplication. 2 main methods: OnStartUp() & OnShutDown() </IExternalApplication>
        /// <UIControlledApplication> d). This parameter allows customization of ribbon panels and controls and the addition of ribbon tabs upon start up.</UIControlledApplication>

        public static RibbonItem UMLibraryToggleBtn;
        public static bool LibraryVisible = false;
        private bool isFirstDocumentOpened = true;

        public static UIControlledApplication _cachedUiCtrApp;
        public static UIApplication _cachedUiApp;
        public static string SubVersionNumber;
        static ManriqueRevitApp()
        {
            // Subscribe to the AssemblyResolve event
            // AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }
        public Result OnStartup(UIControlledApplication application)
        {
            SubVersionNumber = application.ControlledApplication.SubVersionNumber;
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            # region 1. Create or lookup the appropriate Tab
            string tabName = "Manrique Tools Revit";
            try { application.CreateRibbonTab(tabName); }
            catch { }
            #endregion

            #region 2. Create Panels
            List<RibbonPanel> panelList = application.GetRibbonPanels(tabName);
            #endregion

            #region 2.1. Base tool Panel
            RibbonPanel basePanel = null;
            string basePanelName = "Grid Tools";
            foreach (RibbonPanel rpanel in panelList)
            {
                if (rpanel.Name == basePanelName) { basePanel = rpanel; break; }
            }

            if (basePanel == null) { basePanel = application.CreateRibbonPanel(tabName, basePanelName); }

            #endregion

            
            #region 3. Add buttons with icons to the selected panel
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            #endregion

            #region a) Unified Model Panel

            #region 3.0.GridButton
            Image buttonImage = Properties.Resources.icons8_grid_24;
            ImageSource buttonSource = GetImageSource(buttonImage);
            PushButtonData testButtonData = new PushButtonData("Grid-Based Numbering", "Grid-Based Numbering", assemblyPath, typeof(locateInstanceByGrid).FullName);
            testButtonData.ToolTip = "Locate instenaces by grid";
            testButtonData.Image = buttonSource;
            testButtonData.LargeImage = buttonSource;

            PushButton testButtonPush = basePanel.AddItem(testButtonData) as PushButton;

            #endregion

            #region 4. Cache the UIControlledApplication
            _cachedUiCtrApp = application;

            try
            {
                _cachedUiCtrApp.Idling += OnIdling;
            }
            catch (Exception ex)
            {
            }
            #endregion

            #endregion

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string dllDirectory = Path.GetDirectoryName(dllPath);
                string metadataFilePath = Path.Combine(dllDirectory, "metadata.json");
            }
            catch (Exception ex)
            {
            }

            return Result.Succeeded;
        }
        private void OnIdling(object sender, IdlingEventArgs e)
        {
            _cachedUiCtrApp.Idling -= OnIdling;

            UIApplication uiapp = sender as UIApplication;

            if (uiapp != null)
            {
                _cachedUiApp = uiapp;
            }
        }
        private BitmapSource GetImageSource(Image img)
        {
            BitmapImage bmp = new BitmapImage();

            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = null;
                bmp.StreamSource = ms;
                bmp.EndInit();
            }
            return bmp;
        }
        

    }
}