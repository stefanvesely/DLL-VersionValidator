using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace VersionViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string FolderPath;
        private List<string> lstFiles = new List<string>();
        private List<dllPathVersion> lstFilesVersions = new List<dllPathVersion>();

        public MainWindow()
        {
            InitializeComponent();
            //GetFolderPath();
            //GetListOfDLLs(FolderPath);
            //GetVersionsAndGroup(lstFiles);
        }

        private void GetFolderPath()
        {
            using var FolderDialogue = new FolderBrowserDialog
            {
                Description = "Please Select a Folder.",
                UseDescriptionForTitle = true,
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                ShowNewFolderButton = true
            };
            if (FolderDialogue.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPath = FolderDialogue.SelectedPath;
            }
        }

        private void GetListOfDLLs(string sPath)
        {
            if (sPath != null && sPath != "" && Directory.Exists(sPath))
            {
                lstFiles = Directory.GetFiles(sPath, "*.dll").ToList();
            }
        }

        private void GetVersionsAndGroup(List<string> lstFilePaths)
        {
            foreach (string sPath in lstFilePaths)
            {
                lstFilesVersions.Add(new dllPathVersion(sPath));
            }
            foreach (dllPathVersion dlp in lstFilesVersions)
            {
                dlp.lstShared = dlp.OneToMany(dlp, lstFilesVersions);
            }
            foreach (dllPathVersion dlp in lstFilesVersions)
            {
                dlp.UpdateRating();
            }
        }

        public class dllPathVersion
        {
            private string sPath { get; set; }
            public string sVersion { get; set; }
            public string sFileName { get; set; }
            public string sRating { get; set; }
            public string sStandardDll { get; set; }
            public string sToUpdate { get; set; }
            private List<VersionNode> lstVersionNode = new List<VersionNode>();
            public bool isStandardDll = true;
            public List<dllSharedVersionViews> lstShared = new List<dllSharedVersionViews>();
            public decimal Rating = 0;

            public dllPathVersion(string sFilePath)
            {
                sRating = "true";
                if (sFilePath.Substring(sFilePath.Length - 4) == ".dll")
                {
                    sPath = sFilePath;
                    try
                    {
                        Assembly asCommonAssembly = Assembly.LoadFrom(sFilePath);
                        sVersion = asCommonAssembly.GetName().Version.ToString();
                    }
                    catch
                    {
                        sVersion = "0.0.0.0";
                        isStandardDll = false;
                        sStandardDll = "false";
                    }
                    string[] sVersionNodes = sVersion.Split('.');

                    foreach (string sVersionNode in sVersionNodes)
                    {
                        int iNodeVal = int.Parse(sVersionNode);
                        lstVersionNode.Add(new VersionNode { iNodeValue = iNodeVal });
                    }
                    sFileName = sFilePath.Substring(sFilePath.LastIndexOf('\\') + 1);
                }
            }

            private dllSharedVersionViews GetSharedVersionView(dllPathVersion pv1, dllPathVersion pv2)
            {
                dllSharedVersionViews dllsvvReturn = new dllSharedVersionViews();
                dllsvvReturn.sFileName = pv2.sFileName;
                bool isCanNode = true;
                for (int i = 0; i < pv1.lstVersionNode.Count; i++)
                {
                    if (pv1.lstVersionNode[i].iNodeValue == pv2.lstVersionNode[i].iNodeValue)
                    {
                        if (isCanNode)
                        {
                            dllsvvReturn.iSharedNodeDegreeFront++;
                        }
                        dllsvvReturn.iTotalSharedNodes++;
                    }
                    else
                    {
                        isCanNode = false;
                    }
                }
                for (int i = pv1.lstVersionNode.Count - 1; i > -1; i--)
                {
                    if (pv1.lstVersionNode[i].iNodeValue == pv2.lstVersionNode[i].iNodeValue)
                    {
                        dllsvvReturn.iSharedNodeDegreeBack++;
                    }
                    else
                    {
                        i = 0;
                    }
                }
                if (pv1.lstVersionNode.Count != pv2.lstVersionNode.Count)
                {
                    dllsvvReturn.iSharedNodeDegreeBack = 0;
                }
                return dllsvvReturn;
            }

            public List<dllSharedVersionViews> OneToMany(dllPathVersion pv1, List<dllPathVersion> lstPathVersion)
            {
                List<dllSharedVersionViews> dsvvReturn = new List<dllSharedVersionViews>();
                foreach (dllPathVersion dllPV in lstPathVersion)
                {
                    if (pv1.sFileName != dllPV.sFileName)
                    {
                        dsvvReturn.Add(GetSharedVersionView(pv1, dllPV));
                    }
                }
                return dsvvReturn;
            }

            public void UpdateRating()
            {
                if (lstShared.Count > 0)
                {
                    if (sStandardDll == "false")
                    {
                        sRating = "0";
                        Rating = 0;
                    }
                    else
                    {
                        decimal dcmNodeCount = lstVersionNode.Count();
                        decimal dcmTotalPossible = lstShared.Count * (dcmNodeCount * 3);
                        decimal dcmActualCount = 0;
                        foreach (dllSharedVersionViews dsvv in lstShared)
                        {
                            dcmActualCount += dsvv.iSharedNodeDegreeBack;
                            dcmActualCount += dsvv.iSharedNodeDegreeFront;
                            dcmActualCount += dsvv.iTotalSharedNodes;
                        }
                        sStandardDll = "true";
                        Rating = Math.Round(((dcmActualCount / dcmTotalPossible) * 100), 2);
                        sRating = Rating.ToString();
                    }
                }
            }

            public void ValidateRating(List<dllPathVersion> lstObj)
            {
                decimal dcmMaxRating = 0;
                foreach (dllPathVersion dlpv in lstObj)
                {
                    if (dlpv.Rating > dcmMaxRating)
                    {
                        dcmMaxRating = dlpv.Rating;
                    }
                }
                foreach (dllPathVersion dlpv in lstObj)
                {
                    if (dlpv.Rating == dcmMaxRating)
                    {
                        dlpv.sToUpdate = "Yes";
                    }
                    if (dlpv.Rating != dcmMaxRating)
                    {
                        dlpv.sToUpdate = "No";
                    }
                    if (dlpv.isStandardDll == false)
                    {
                        dlpv.sToUpdate = "File Broken";
                    }
                }
            }
        }

        public class VersionNode
        {
            public int iNodeValue;
        }

        public class dllSharedVersionViews
        {
            public string sFileName;
            public int iSharedNodeDegreeFront; //this is to compare from Major to Patch
            public int iSharedNodeDegreeBack; //this is to compare from Patch to Major
            public int iTotalSharedNodes;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderPath = "";
            lstFiles.Clear();
            lstFilesVersions.Clear();
            dgFiles.Items.Clear();
            GetFolderPath();
            GetListOfDLLs(FolderPath);
            GetVersionsAndGroup(lstFiles);
            foreach (dllPathVersion dlp in lstFilesVersions)
            {
                dlp.ValidateRating(lstFilesVersions);
            }
            lstFilesVersions = lstFilesVersions.OrderBy(o => o.Rating).ToList();
            foreach (dllPathVersion dpv in lstFilesVersions)
            {
                dgFiles.Items.Add(dpv);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult dlg = System.Windows.Forms.MessageBox.Show("Are you sure you wish to Exit?", "Exit", (MessageBoxButtons)MessageBoxButton.YesNo);
            if (dlg == System.Windows.Forms.DialogResult.Yes)
            {
                this.Close();
            }
        }
    }
}