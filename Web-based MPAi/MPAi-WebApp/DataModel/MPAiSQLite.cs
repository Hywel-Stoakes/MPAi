﻿using MPAi_WebApp.DataModel;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Web;

namespace MPAi_WebApp.DataModel
{
    public class MPAiSQLite
    {
        /// <summary>
        /// Default constructor. If the database file does not exist, create it.
        /// </summary>
        public MPAiSQLite()
        {
            if (!(File.Exists(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite"))))
            {
                initaliseDatabase();
            }
        }

        /// <summary>
        /// Creates the database file if needed, and builds the database.
        /// </summary>
        public void initaliseDatabase()
        {
            if (!(File.Exists(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite"))))
            {
                SQLiteConnection.CreateFile(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite"));
            }
            createTables();
            populatetables();
        }

        /// <summary>
        /// Using the .wav files in the Audio directory, populates the Word and Recording tables.
        /// </summary>
        private void populatetables()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Audio"));
            foreach (FileInfo fInfo in dirInfo.GetFiles("*.wav", SearchOption.AllDirectories))   // Also searches subdirectories.
            {
                if (fInfo.Extension.Contains("wav"))
                {
                    // Dynamically create recordings and words.
                    String fileName = Path.GetFileName(fInfo.FullName);
                    String wordName = NameParser.WordNameFromFile(fileName);
                    Speaker speaker = NameParser.SpeakerFromFile(fileName);

                    // Create the word if it doesn't exist, get the name from the filename.
                    using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite") + "; Version=3;"))
                    {
                        connection.Open();
                        
                        // Select the number of words that match the current word.
                        string sql = "select count(*) from Word " +
                            "where wordName = '" + wordName + "'";
                        SQLiteCommand command = new SQLiteCommand(sql, connection);
                        int count = Int32.Parse(command.ExecuteScalar().ToString());

                        // If the word is not currently in the database, add it.
                        if (count <= 0)
                        {
                            sql = "insert into Word (wordName)" +
                                "values('" + wordName +
                                "')";
                            command = new SQLiteCommand(sql, connection);
                            command.ExecuteNonQuery();
                        }

                        // Select the number of recordings that match the current recording.
                        sql = "select count(*) from Recording " +
                            "where filePath = '" + fInfo.FullName + "'";
                        command = new SQLiteCommand(sql, connection);
                        count = Int32.Parse(command.ExecuteScalar().ToString());

                        // If the recording is not currently in the database, add it, and associate it with the word we just made.
                        if (count <= 0)
                        {
                            sql = "select wordId from Word " +
                           "where wordName = '" + wordName + "'";
                            command = new SQLiteCommand(sql, connection);
                            int wordID = Int32.Parse(command.ExecuteScalar().ToString());

                            sql = "insert into Recording(speaker, wordId, filePath) " +
                                "values(" + Convert.ToInt32(speaker).ToString() +
                                ", " + wordID.ToString() +
                                ", '" + fInfo.FullName + "')";
                            command = new SQLiteCommand(sql, connection);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates the tables used by MPAi. If the table aleady exists, it will not be overridden.
        /// </summary>
        void createTables()
        {
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite") + "; Version=3;"))
            {
                connection.Open();

                // Word table - corresponds to the fields in Word.cs
                string sql = "create table if not exists Word(" +
                "wordId integer primary key," +
                "wordName text unique not null" +
                ")";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();

                // User table - corresponds to the fields in User.cs
                sql = "create table if not exists User(" +
                    "userId integer primary key," +
                    "username text unique not null" +
                    ")";
                command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();

                // Score table - corresponds to the fields in Score.cs
                // Many-to-one link with Word
                // Many-to-one link with User
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

                // Recording table - corresponds to the fields in Recording.cs
                // Many-to-one link with Word
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

        /// <summary>
        /// Queries the database for every recording of a specific word spoken by a class of speaker.
        /// </summary>
        /// <param name="name">The name of the word to get recordings for</param>
        /// <param name="category">The category of speaker to get recordings for</param>
        /// <returns>  A list of recording objects representing the values in the database.</returns>
        public List<Recording> GenerateRecordingList(String name, String category)
        {
           List<Recording> recordingSet = new List<Recording>();

            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite") + "; Version=3;"))
            {
                connection.Open();

                // Assign the speaker enum.
                Speaker speaker;
                if (!(Enum.TryParse(category, out speaker)))
                {
                    // If the speaker is invalid, assign the UNIDENTIFIED enum to avoid NullPointerExceptions
                    speaker = Speaker.UNIDENTIFIED;
                }
                
                // Join the word and recording tables, and query on the name of the word, and the speaker. 
                string sql = "select * " +
                    "from Word natural join Recording " +
                    "where wordName = '" + name + "' " +
                    "and speaker = " + Convert.ToInt32(speaker).ToString();
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                // For each of the recordings returned by the query...
                while (reader.Read())
                {
                    // Create a word object of the returned values
                    Word newWord = new Word()
                    {
                        WordId = Int32.Parse(reader["wordId"].ToString()),
                        WordName = reader["wordName"].ToString()
                    };

                    // Create a recording object of the returned values.
                    Recording newRecording = new Recording()
                    {
                        RecordingId = Int32.Parse(reader["recordingId"].ToString()),
                        Speaker = (Speaker)Int32.Parse(reader["speaker"].ToString()),
                        Word = newWord,
                        WordId = newWord.WordId,
                        FilePath = reader["filePath"].ToString()
                    };

                    // Add the new recording object to the list of recordings to return.
                    recordingSet.Add(newRecording);
                }
            }
            return recordingSet;
        }

        /// <summary>
        /// Returns a list of every word in the database.
        /// </summary>
        /// <returns>  A list of Word objects. </returns>
        public List<Word> GenerateWordList()
        {
            List<Word> wordSet = new List<Word>();
            
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite") + "; Version=3;"))
            {
                connection.Open();
                
                // Query the database for all word objects.
                string sql = "select * from Word";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();
                
                // For each word...
                while (reader.Read())
                {
                    // Create a new word object with the values in the database.
                    Word newWord = new Word()
                    {
                        WordId = Int32.Parse(reader["wordId"].ToString()),
                        WordName = reader["wordName"].ToString()
                    };

                    // Add to list of returned words.
                    wordSet.Add(newWord);
                }
            }
            return wordSet;
        }

        /// <summary>
        /// Adds a user to the database.
        /// </summary>
        /// <param name="username">The username of the new user.</param>
        public void AddUser(String username)
        {
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite") + "; Version=3;"))
            {
                connection.Open();
                // Insert a user with the specified name into the User table.
                string sql = "insert into User(username) " +
                    "values('" + username.ToLower() + "')";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Saves a score into the database. 
        /// A link between the score and word is not used in the 2017 version of MPAi, but is kept as a future feature.
        /// </summary>
        /// <param name="username">The name of the current user</param>
        /// <param name="wordname">The current word</param>
        /// <param name="percentage">The score the user got for that word</param>
        public void SaveScore(string username, string wordName, int percentage)
        {
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite") + "; Version=3;"))
            {
                connection.Open();
                
                // Retrieve the current user's information from the database. 
                string sql = "select * " +
                    "from User " +
                    "where username = '" + username.ToLower() + "'";
                    // To search by other factors, add conditions here.
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                // Usernames should be unique; so only read first result and create a user object.
                reader.Read();
                User newUser = new User()
                {
                    UserId = Int32.Parse(reader["userId"].ToString()),
                    Username = reader["username"].ToString()
                };

                // Retreive the information on the current word from the database.
                sql = "select * " +
                    "from Word " +
                    "where wordName = '" + wordName + "'";
                command = new SQLiteCommand(sql, connection);
                reader = command.ExecuteReader();

                // Word names should be unique; so only read first result and create a word object.
                reader.Read();
                Word newWord = new Word()
                {
                    WordId = Int32.Parse(reader["wordId"].ToString()),
                    WordName = reader["wordName"].ToString()
                };

                // Create a score entry using the user, score, percantage and date to allow different types of graph filtering.
                sql = "insert into Score(wordId, userId, percentage, date) " +
                    "values(" + newWord.WordId.ToString() + ", " +
                    newUser.UserId.ToString() + ", " +
                    percentage.ToString() + ", '" +
                    DateTime.Now.ToString() + "')";
                command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Retrieves all the scores for a user.
        /// </summary>
        /// <param name="username"> The user to return scores for.</param>
        /// <returns>  A list of score objects.</returns>
        public List<Score> GenerateScoreList(string username)
        {
            // Get all scores from the database for the current user.
            List<Score> scoreList = new List<Score>();
            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "MPAiDb.sqlite") + "; Version=3;"))
            {
                connection.Open();
                // Select the current user out of the database.
                string sql = "select * " +
                    "from User " +
                    "where username = '" + username.ToLower() + "'";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                SQLiteDataReader reader = command.ExecuteReader();

                // Usernames should be unique; so only read first result and create a User object.
                reader.Read();
                User newUser = new User()
                {
                    UserId = Int32.Parse(reader["userId"].ToString()),
                    Username = reader["username"].ToString()
                };

                // Select every score for the current user.
                sql = "select * " +
                    "from Score " +
                    "where userId = " + newUser.UserId.ToString();
                command = new SQLiteCommand(sql, connection);
                reader = command.ExecuteReader();

                // Create score objects from every returned record, and add to recording list.
                while (reader.Read())
                {
                    scoreList.Add(new Score()
                    {
                        ScoreId = Int32.Parse(reader["scoreId"].ToString()),
                        user = newUser,
                        UserId = newUser.UserId,
                        WordId = Int32.Parse(reader["wordId"].ToString()),
                        Percentage = Int32.Parse(reader["percentage"].ToString()),
                        Date = DateTime.Parse(reader["date"].ToString())
                    });
                }
            }
            return scoreList;
        }
    }
}
/// <summary>
/// The audio files used by MPAi are stored in a specific data format. 
/// This class holds methods to convert from this format to usable data.
/// </summary>
static class NameParser
{
    /// <summary>
    /// Returns a word from the given file path, according to MPAi naming conventions.
    /// </summary>
    /// <param name="fileName">The recording file name</param>
    /// <returns>A speaker object representing the speaker of the recording.</returns>
    public static String WordNameFromFile(String fileName)
    {
        return fileName.Split('-')[2];
    }

    /// <summary>
    /// Returns a Speaker object from the given file path, according to MPAi naming conventions.
    /// </summary>
    /// <param name="fileName">The recording file name</param>
    /// <returns>A speaker object representing the speaker of the recording.</returns>
    public static Speaker SpeakerFromFile(String fileName)
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
}