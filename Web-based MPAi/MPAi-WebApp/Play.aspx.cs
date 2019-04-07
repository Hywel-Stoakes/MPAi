﻿using MPAi_WebApp.DataModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace MPAi_WebApp
{
    /// <summary>
    /// Retrieves a recording from the server.
    /// </summary>    
    public partial class Play : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Get target word name and category from server.
            string name = Request.Form["wordName"];
            string category = Request.Form["wordCategory"];

            Debug.WriteLine("Count: " + Request.Form.Count);
            Debug.WriteLine("respond: " + name);
            Debug.WriteLine("Category: " + category);

            // Create list of recording objects.
            MPAiSQLite context = new MPAiSQLite();
            List<Recording> recordingList = context.GenerateRecordingList(name, category);
            
            // make a new Dataset containing recordings.
            // This way was selected as it serialises into JSON well.
            DataSet newDataSet = new DataSet("newDataSet");
            newDataSet.Namespace = "MPAi_WebApp";
            DataTable newDataTable = new DataTable("resultJsonTable");
            DataColumn nameColumn = new DataColumn("name", typeof(string));
            newDataTable.Columns.Add(nameColumn);
            DataColumn categoryColumn = new DataColumn("category", typeof(string));
            newDataTable.Columns.Add(categoryColumn);
            DataColumn pathColumn = new DataColumn("path", typeof(string));
            newDataTable.Columns.Add(pathColumn);
            newDataSet.Tables.Add(newDataTable);

            // Add each recording object to the DataTable.
            foreach (Recording r in recordingList)
            {
                DataRow newRow = newDataTable.NewRow();
                newRow["name"] = r.Word.WordName;
                newRow["category"] = Enum.GetName(typeof(Speaker), r.Speaker);
                newRow["path"] = Path.Combine("audio" , Path.GetFileName(r.FilePath));
                newDataTable.Rows.Add(newRow);
            }

            // Serialise the DataTable
            string newJson;
            if (newDataTable.Rows.Count == 0)
            {
                newJson = "nothing";
            }
            else
            {
                newJson = JsonConvert.SerializeObject(newDataSet, Formatting.Indented);
            }
            
            // Output result as JSON
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(newJson);
            Response.End();
        }
    }
}