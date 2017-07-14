﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MPAi_WebApp
{
    public partial class Statistics : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Json\dummy.json");
            string json = File.ReadAllText(jsonPath);

            // Output result as JSON
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write(json);
            Response.End();
        }
    }
}