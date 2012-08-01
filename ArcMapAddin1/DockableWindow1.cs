using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DaveChambers.FolderBrowserDialogEx;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseDistributed;
using System.IO;
using System.Security.AccessControl;

namespace ImportXML
{
    /// <summary>
    /// Designer class of the dockable window add-in. It contains user interfaces that
    /// make up the dockable window.
    /// </summary>
    public partial class DockableWindow1 : UserControl
    {
        private string xmlFolder;
        private string fileGDBName;
        public DockableWindow1(object hook)
        {
            InitializeComponent();
            this.Hook = hook;
            this.timer1.Interval = 10*1000;
        }

        /// <summary>
        /// Host object of the dockable window
        /// </summary>
        private object Hook
        {
            get;
            set;
        }

        /// <summary>
        /// Implementation class of the dockable window add-in. It is responsible for 
        /// creating and disposing the user interface class of the dockable window.
        /// </summary>
        public class AddinImpl : ESRI.ArcGIS.Desktop.AddIns.DockableWindow
        {
            private DockableWindow1 m_windowUI;

            public AddinImpl()
            {
                
            }

            protected override IntPtr OnCreateChild()
            {
                m_windowUI = new DockableWindow1(this.Hook);
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing)
            {
                if (m_windowUI != null)
                    m_windowUI.Dispose(disposing);

                base.Dispose(disposing);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialogEx fb = new FolderBrowserDialogEx();
            fb.ShowEditbox = true;
            //fb.RootFolder = 
            if (fb.ShowDialog(this) == DialogResult.OK)
            {
                this.textBox1.Text = fb.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialogEx fd = new FolderBrowserDialogEx();
            fd.ShowEditbox = true;
            //fd.DefaultExt = "ArcGIS File Geodatabase(*.gdb)|*.gdb";
            if (fd.ShowDialog(this) == DialogResult.OK)
	        {
                this.textBox2.Text = fd.SelectedPath;
	        }            
        }

        private void button3_Click(object sender, EventArgs e)
        {            
            string folder = this.textBox1.Text.Trim();
            string filegdbPath = this.textBox2.Text.Trim();

            if (string.IsNullOrEmpty(folder))           
            {
                MessageBox.Show("Please input valid XML file folder!","Information",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return;
            }

            if (!System.IO.Directory.Exists(folder))
            {
                MessageBox.Show("The XML file folder is not exist!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(filegdbPath))
            {
                MessageBox.Show("Please select file geodatabae path!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!System.IO.Directory.Exists(filegdbPath))
            {
                MessageBox.Show("The file geodatabase is not exist!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            this.fileGDBName = filegdbPath;
            this.xmlFolder = folder;
            this.textBox3.Text += DateTime.Now.ToString() + " Start import XML files From folder:\n\""+folder+"\n\" to file geodatabase:\n\""+filegdbPath+"\"\n";
            this.timer1.Start();
            this.button3.Enabled = false;
            //this.progressBar1.Style = ProgressBarStyle.Continuous;
            //this.progressBar1
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want stop import?","Stop import",MessageBoxButtons.OKCancel,MessageBoxIcon.Question) == DialogResult.Cancel)
            {
                return;
            }

            if (this.timer1.Enabled)
            {
                this.timer1.Stop();
                this.textBox3.Text += DateTime.Now.ToString() + " Stop process.\n";
                this.button3.Enabled = true;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string[] fileNameList = System.IO.Directory.GetFiles(this.xmlFolder);
            foreach (string fileName in fileNameList)
	        {
                if (string.Compare(System.IO.Path.GetExtension(fileName),".xml",true) == 0)
                {
                    try
                    {
                        
                        ImportXmlWorkspaceDocument(this.fileGDBName, ConvetFileToUTF8(fileName));
                    }
                    catch (Exception ex)
                    {
                        this.textBox3.Text += DateTime.Now.ToString() + "Error:" + ex.Message;
                        continue;
                    }
                }
	        }
        }

        private string ConvetFileToUTF8(string workspaceDocPath)
        {
            string fileName = System.IO.Path.GetFileName(workspaceDocPath);
            this.textBox3.Text += "xmlFileName:" + fileName + "\n";
            Encoding arabic = Encoding.GetEncoding(1256);
            StreamReader SR = new StreamReader(workspaceDocPath, arabic, true);
            string perFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string tempXMLFolder = perFolder + "\\tempXML\\";
            if (!System.IO.Directory.Exists(tempXMLFolder))
            {
                System.IO.Directory.CreateDirectory(tempXMLFolder);
            }

            string tempXMLFile = tempXMLFolder + fileName;
            if (System.IO.File.Exists(tempXMLFile))
            {
                System.IO.File.Delete(tempXMLFile);
            }

            System.IO.File.WriteAllText(tempXMLFile, SR.ReadToEnd(), Encoding.UTF8);

            SR.Close();

            return tempXMLFile;            
        }

        private void ImportXMLToGDB(string xmlFileName, string gdbName)
        {
            this.textBox3.Text += "xmlFileName:" + System.IO.Path.GetFileName( xmlFileName)+"\n";
            this.textBox3.SelectionStart = this.textBox3.Text.Length;
            this.textBox3.ScrollToCaret();
            IWorkspaceFactory pWSF = new FileGDBWorkspaceFactoryClass();
            IWorkspace pWS = pWSF.OpenFromFile(gdbName,0);
            
            IGdbXmlImport pImport = new GdbImporterClass();

            IEnumNameMapping pEnumNM = null;
            bool bHasConflicts = pImport.GenerateNameMapping(xmlFileName, pWS, out pEnumNM);
            pImport.ImportWorkspace(xmlFileName, pEnumNM, pWS, false);
        }

        private void ImportXmlWorkspaceDocument(String fileGdbPath, String workspaceDocPath)
        {
            // Open the target file geodatabase and create a name object for it.
            

            Type factoryType = Type.GetTypeFromProgID(
                "esriDataSourcesGDB.FileGDBWorkspaceFactory");
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance
                (factoryType);
            IWorkspace workspace = workspaceFactory.OpenFromFile(fileGdbPath, 0);
            IDataset workspaceDataset = (IDataset)workspace;
            IName workspaceName = workspaceDataset.FullName;

            // Create a GdbImporter and use it to generate name mappings.
            IGdbXmlImport gdbXmlImport = new GdbImporterClass();
            IEnumNameMapping enumNameMapping = null;
            Boolean conflictsFound = gdbXmlImport.GenerateNameMapping(workspaceDocPath,
                workspace, out enumNameMapping);

            // Check for conflicts.
            if (conflictsFound)
            {
                // Iterate through each name mapping.
                INameMapping nameMapping = null;
                enumNameMapping.Reset();
                while ((nameMapping = enumNameMapping.Next()) != null)
                {
                    // Resolve the mapping's conflict (if there is one).
                    if (nameMapping.NameConflicts)
                    {
                        nameMapping.TargetName = nameMapping.GetSuggestedName(workspaceName);
                    }

                    // See if the mapping's children have conflicts.
                    IEnumNameMapping childEnumNameMapping = nameMapping.Children;
                    if (childEnumNameMapping != null)
                    {
                        childEnumNameMapping.Reset();

                        // Iterate through each child mapping.
                        INameMapping childNameMapping = null;
                        while ((childNameMapping = childEnumNameMapping.Next()) != null)
                        {
                            if (childNameMapping.NameConflicts)
                            {
                                childNameMapping.TargetName =
                                    childNameMapping.GetSuggestedName(workspaceName);
                            }
                        }
                    }
                }
            }

            // Import the workspace document, including both schema and data.
            gdbXmlImport.ImportWorkspace(workspaceDocPath, enumNameMapping, workspace, false);
        }

        private void DockableWindow1_Load(object sender, EventArgs e)
        {

        }
    }
}
