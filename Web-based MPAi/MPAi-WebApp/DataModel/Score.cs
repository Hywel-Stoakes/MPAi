﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MPAi_WebApp.DataModel
{
    public class Score
    {
        public int ScoreId { get; set; }

        public User user { get; set; }

        public int UserId { get; set; }

        public Word word { get; set; }

        public int WordId { get; set; }

        public double Percentage { get; set; }

        public DateTime Date { get; set; }
        // Add other fields if the scoreboard calls for it
    }
}