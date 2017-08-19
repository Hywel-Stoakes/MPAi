﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Web;

namespace MPAi_WebApp.DataModel
{
    public class MPAiSQLite
    {
        SQLiteConnection connection;
        public MPAiSQLite()
        {
            if (!(System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MPAiDb.sqlite"))))
            {
                SQLiteConnection.CreateFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MPAiDb.sqlite"));
            }
            connection = new SQLiteConnection("Data Source="+Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MPAiDb.sqlite") +"; Version=3;");
            connection.Open();
            createTables();
            populatetables();
        }
        /// <summary>
        /// Returns a word from the given file path, according to MPAi naming conventions.
        /// </summary>
        /// <param name="fileName">The recording file name</param>
        /// <returns>A speaker object representing the speaker of the recording.</returns>
        private String WordNameFromFile(String fileName)
        {
            return fileName.Split('-')[2];
        }
        /// <summary>
        /// Returns a Speaker object from the given file path, according to MPAi naming conventions.
        /// </summary>
        /// <param name="fileName">The recording file name</param>
        /// <returns>A speaker object representing the speaker of the recording.</returns>
        private Speaker SpeakerFromFile(String fileName)
        {
            switch (fileName.Split('-')[0])
            {
                case ("oldfemale"):
                    return Speaker.KUIA_FEMALE;
                case ("oldmale"):
                    return Speaker.KAUMATUA_MALE;
                case ("youngfemale"):
                    return Speaker.MODERN_FEMALE;
                case ("youngmale"):
                    return Speaker.MODERN_MALE;
                default:
                    return Speaker.UNIDENTIFIED;
            }
        }
        private void populatetables()
        {
            //Set audio folder properly
            DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Audio"));
            foreach (FileInfo fInfo in dirInfo.GetFiles("*.wav", SearchOption.AllDirectories))   // Also searches subdirectories.
            {
                if (fInfo.Extension.Contains("wav"))
                {
                    // Dynamically create recordings and words.
                    String fileName = Path.GetFileName(fInfo.FullName);
                    String wordName = WordNameFromFile(fileName);
                    Speaker speaker = SpeakerFromFile(fileName);

                    // Create the word if it doesn't exist, get the name from the filename.

                    string sql = "select count(*) from Word " +
                        "where wordName = '" + fileName + "'";
                    SQLiteCommand command = new SQLiteCommand(sql, connection);
                    int count = Int32.Parse(command.ExecuteScalar().ToString());
                    if (count <= 0)
                    {
                        sql = "insert into Word (wordName)" +
                            "values('" + fileName +
                            "')";
                        command = new SQLiteCommand(sql, connection);
                        command.ExecuteNonQuery();
                    }

                    // Create the recording if it doesn't exist, associate it with the Word.

                    sql = "select count(*) from Recording " +
                        "where filePath = '" + fInfo.FullName + "'";
                    command = new SQLiteCommand(sql, connection);
                    count = Int32.Parse(command.ExecuteScalar().ToString());
                    if (count <= 0)
                    {
                        sql = "select wordId from Word " +
                       "where wordName = '" + fileName + "'";
                        command = new SQLiteCommand(sql, connection);
                        int wordID = Int32.Parse(command.ExecuteScalar().ToString());

                        sql = "insert into Recording(speaker, wordId, filePath) " +
                            "values(" + Convert.ToInt32(speaker).ToString() +
                            ", " + wordID.ToString() +
                            ", '" + fInfo.FullName + "')";
                    }
                }
            }
        }

        void createTables()
        {
            string sql = "create table if not exists Word(" +
                "wordId integer primary key," +
                "wordName text unique not null" +
                ")";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();

            sql = "create table if not exists User(" +
                "userId integer primary key," +
                "username text unique not null" +
                ")";
            command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();

            sql = "create table if not exists Score(" +
                "scoreId integer primary key," +
                "wordId integer, " +
                "userId integer, " +
                "percentage integer not null, " +
                "date text not null, " +
                "foreign key(wordId) references Word(wordId)," +
                "foreign key(userId) references User(userId)" + 
                ")";
            command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();

            sql = "create table if not exists Recording(" +
                "recordingId integer primary key, " +
                "speaker integer not null, " +
                "wordId integer, " +
                "filePath text unique not null, " +
                "foreign key(wordId) references Word(wordId) " +
                ")";
            command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}