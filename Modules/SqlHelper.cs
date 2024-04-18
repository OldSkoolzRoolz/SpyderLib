using KC.Apps.SpyderLib.Logging;
using JetBrains.Annotations;
using MySql.Data.MySqlClient;



namespace KC.Apps.SpyderLib.Modules;

public static class MySqlDatabase
{

    private const string CONNECTION_STRING = "server=127.0.0.1;user=plato;password=password;database=spyderlib;ConnectionTimeout=45;MaxPoolSize=200;";
   




    public static void ExecuteNonQuery(string query, [CanBeNull]params MySqlParameter[] commandParameters)
        {
            try
                {

                    MySqlHelper.ExecuteNonQuery(CONNECTION_STRING, query, commandParameters);

                }
            catch (Exception e)
                {
                    Console.WriteLine(e);
            Log.Debug(e.Message);
                }
        }
    
    

    public static async Task ExecuteNonQueryAsync(string query, params MySqlParameter[] commandParameters)
    {
       try
       {

         _ = await  MySqlHelper.ExecuteNonQueryAsync(CONNECTION_STRING,query,CancellationToken.None, commandParameters).ConfigureAwait(false);


       }
       catch (MySqlException ex)
       {
        
        Log.Debug(ex.Message);
       }
    }





public static async Task<MySqlDataReader>ExecuteMySqlReaderAsync(string query,[CanBeNull]params MySqlParameter[] commandParameters)
{

    try
    {
            MySqlDataReader reader;
            if (commandParameters != null)
            {
                 reader = await MySqlHelper.ExecuteReaderAsync(CONNECTION_STRING, query, commandParameters).ConfigureAwait(false);
            }
            else
            {
                reader = await MySqlHelper.ExecuteReaderAsync(CONNECTION_STRING, query).ConfigureAwait(false); 
            }
            return reader;
    }
    catch (MySqlException ex)
    {
            Log.Debug(ex.Message);
            return default;
        
    }
}

    internal static string ExecuteScalarText(string sql, params MySqlParameter[] mySqlParameter)
    {
        try
        {
            var filename = MySqlHelper.ExecuteScalar(CONNECTION_STRING, sql, mySqlParameter).ToString();
            return filename ?? string.Empty;
        }
        catch (MySqlException e)
        {
            Log.Debug(e.Message);
        }
        return string.Empty;
    }
}