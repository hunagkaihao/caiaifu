using Ecs.ConfigTool;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace Ecs.LogTool;

public static class SqliteLogHelper
{
    private static readonly object locker = new object();

    private static SqliteConnection connection = new SqliteConnection(Settings.Options.SqliteLogConnString);

    public static void WriteLog(SqliteLogItem logItem)
    {
        lock (locker)
        {
            try
            {
                if(connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO logitems (Date, Grade, Message, Source) VALUES ($Date, $Grade, $Message, $Source)";
                    command.Parameters.AddWithValue("$Date", logItem.Date);
                    command.Parameters.AddWithValue("$Grade", logItem.Grade);
                    command.Parameters.AddWithValue("$Message", logItem.Message);
                    command.Parameters.AddWithValue("$Source", logItem.Source);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }            
    }

    public static int GetLogItemCount()
    {
        lock(locker)
        {
            try
            {
                if(connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(*) FROM logitems";
                    var reader = command.ExecuteReader();
                    reader.Read();
                    int ret = reader.GetInt32(0);
                    reader.Close();
                    return ret;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 0;
            }
        }
    }

    public static List<SqliteLogItem> GetLogItems(string partOfLogContent, string grade, int logCntMax = 500)
    {
        lock(locker)
        {
            try
            {
                if(connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    if (grade == "%")
                    {
                        List<SqliteLogItem> result = new List<SqliteLogItem>();
                        if (partOfLogContent != "%")
                        {
                            command.CommandText = $"SELECT Id, Date, Grade, Message, Source FROM logitems WHERE Message LIKE $partOfLogContent ORDER BY Id DESC LIMIT $cnt";
                            command.Parameters.AddWithValue("$partOfLogContent", $"%{partOfLogContent}%");
                            command.Parameters.AddWithValue("$cnt", logCntMax);
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                SqliteLogItem item = new SqliteLogItem()
                                {
                                    Id = reader.GetInt32(0),
                                    Date = reader.GetString(1),
                                    Grade = reader.GetString(2),
                                    Message = reader.GetString(3),
                                    Source = reader.GetString(4)
                                };
                                result.Add(item);
                            }
                        }
                        else
                        {
                            command.CommandText = $"SELECT Id, Date, Grade, Message, Source FROM logitems ORDER BY Id DESC LIMIT $cnt";
                            command.Parameters.AddWithValue("$cnt", logCntMax);
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                SqliteLogItem item = new SqliteLogItem()
                                {
                                    Id = reader.GetInt32(0),
                                    Date = reader.GetString(1),
                                    Grade = reader.GetString(2),
                                    Message = reader.GetString(3),
                                    Source = reader.GetString(4)
                                };
                                result.Add(item);
                            }
                        }
                        return result;
                    }
                    else
                    {
                        List<SqliteLogItem> result = new List<SqliteLogItem>();
                        if (partOfLogContent != "%")
                        {
                            command.CommandText = $"SELECT Id, Date, Grade, Message, Source FROM logitems WHERE Grade = $grade AND Message LIKE $partOfLogContent ORDER BY Id DESC LIMIT $cnt";
                            command.Parameters.AddWithValue("$grade", grade);
                            command.Parameters.AddWithValue("$partOfLogContent", $"%{partOfLogContent}%");
                            command.Parameters.AddWithValue("$cnt", logCntMax);
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                SqliteLogItem item = new SqliteLogItem()
                                {
                                    Id = reader.GetInt32(0),
                                    Date = reader.GetString(1),
                                    Grade = reader.GetString(2),
                                    Message = reader.GetString(3),
                                    Source = reader.GetString(4)
                                };
                                result.Add(item);
                            }
                        }
                        else
                        {
                            command.CommandText = $"SELECT Id, Date, Grade, Message, Source FROM logitems WHERE Grade = $grade ORDER BY Id DESC LIMIT $cnt";
                            command.Parameters.AddWithValue("$grade", grade);
                            command.Parameters.AddWithValue("$cnt", logCntMax);
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                SqliteLogItem item = new SqliteLogItem()
                                {
                                    Id = reader.GetInt32(0),
                                    Date = reader.GetString(1),
                                    Grade = reader.GetString(2),
                                    Message = reader.GetString(3),
                                    Source = reader.GetString(4)
                                };
                                result.Add(item);
                            }
                        }
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<SqliteLogItem>();
            }
        }        
    }

    public static SqliteLogItem GetLastLogItem()
    {
        lock(locker)
        {
            try
            {
                if(connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();
                    
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT Id, Date, Grade, Message, Source FROM logitems ORDER BY Id DESC LIMIT 1";
                    var reader = command.ExecuteReader();
                    if(!reader.Read())
                        return null;
                        
                    SqliteLogItem item = new SqliteLogItem()
                    {
                        Id = reader.GetInt32(0),
                        Date = reader.GetString(1),
                        Grade = reader.GetString(2),
                        Message = reader.GetString(3),
                        Source = reader.GetString(4)
                    };
                    return item;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        } 
    }

    public static void DeleteLogItems(int remainNum)
    {
        lock(locker)
        {
            if (remainNum < 0)
            {
                return;
            }
            int totalNum = GetLogItemCount();
            if (totalNum <= remainNum)
            {
                return;
            }
            try
            {
                SqliteLogItem lastItem = GetLastLogItem();
                if(lastItem == null)
                    return;

                if(connection.State == System.Data.ConnectionState.Closed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"Delete From logitems Where Id <= $id";
                    command.Parameters.AddWithValue("$id", lastItem.Id - remainNum);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

}