// Purpose: Low-level SQL Server access wrapper used by service and page code.
// All query helpers accept parameters to reduce SQL injection risk.
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace MindWell
{
    /// <summary>
    /// DatabaseHelper provides methods for database operations with parameterized queries
    /// to prevent SQL injection attacks.
    /// </summary>
    public class DatabaseHelper
    {
        private static string _connectionString = ConfigurationManager.ConnectionStrings["MindWellConnection"] != null 
            ? ConfigurationManager.ConnectionStrings["MindWellConnection"].ConnectionString 
            : "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=MindWellDB;Integrated Security=true;Connect Timeout=60;";

        /// <summary>
        /// Gets and opens a SQL connection to the database.
        /// </summary>
        /// <returns>Open SqlConnection</returns>
        public static SqlConnection GetConnection()
        {
            try
            {
                SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();
                return conn;
            }
            catch (SqlException ex)
            {
                throw new Exception("Database connection failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Executes a parameterized SQL query and returns a DataTable.
        /// </summary>
        /// <param name="query">SQL query with @Parameter placeholders</param>
        /// <param name="parameters">SqlParameter array</param>
        /// <returns>DataTable with query results</returns>
        public static DataTable ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            // Used for SELECT operations that return tables for dashboards, listings, and reports.
            using (SqlConnection conn = GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        /// <summary>
        /// Executes a parameterized SQL command (INSERT, UPDATE, DELETE) and returns affected rows.
        /// </summary>
        /// <param name="query">SQL command with @Parameter placeholders</param>
        /// <param name="parameters">SqlParameter array</param>
        /// <returns>Number of rows affected</returns>
        public static int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            // Used for INSERT, UPDATE, and DELETE operations in CRUD workflows.
            using (SqlConnection conn = GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a parameterized SQL query and returns the first column of the first row.
        /// </summary>
        /// <param name="query">SQL query with @Parameter placeholders</param>
        /// <param name="parameters">SqlParameter array</param>
        /// <returns>First column value or null</returns>
        public static object ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = GetConnection())
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Executes a stored procedure and returns a DataTable.
        /// </summary>
        /// <param name="procedureName">Stored procedure name</param>
        /// <param name="parameters">SqlParameter array</param>
        /// <returns>DataTable with results</returns>
        public static DataTable ExecuteStoredProcedure(string procedureName, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = GetConnection())
            {
                SqlCommand cmd = new SqlCommand(procedureName, conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }
    }
}
