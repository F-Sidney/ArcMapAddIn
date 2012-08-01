using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.esriSystem;

namespace ImportXML
{
    public class Button1 : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        IDockableWindow dock;
        public Button1()
        {
        }

        protected override void OnClick()
        {
            //
            //  TODO: Sample code showing how to access button host
            //
            if (dock == null)
            {
                UID dockWinID = new UIDClass();
                dockWinID.Value = ThisAddIn.IDs.DockableWindow1;
                dock = ArcMap.DockableWindowManager.GetDockableWindow(dockWinID);
            }
            else
            {
                dock.Show(!dock.IsVisible());
            }
            

            ArcMap.Application.CurrentTool = null;
        }
        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }

}
