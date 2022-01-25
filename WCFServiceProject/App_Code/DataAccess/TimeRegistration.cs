using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using Dapper;
using System.Threading.Tasks;
using WCFServiseProject.Models;
using System;
using System.Configuration;

namespace WCFServiseProject.DataAccess
{
    public class TimeRegistrationRepository
    {
        /// <summary>
        /// Realization of the GetAll service method
        /// </summary>
        /// <returns>All TimeRegistration records</returns>
        public static async Task<IList<TimeRegistration>> GetAll() 
        {
            using (SqlConnection connection = GetConnetion())
            {

                const string querry = @"SELECT * FROM TimeRegistration ORDER BY TimeIn;";

                connection.Open();
                var result = await connection.QueryAsync<TimeRegistration>(querry);
                return result.ToList();
            }
        }

        /// <summary>
        /// Realization of the GetById service method
        /// </summary>
        /// <param name="id">ID of requested TimeRegistration record</param>
        /// <returns>TimeRegistration record by registration ID</returns>
        public static async Task<TimeRegistration> Get(int id) //
        {
            using (SqlConnection connection = GetConnetion())
            {

                var querry = String.Format("SELECT * FROM TimeRegistration WHERE ID = {0}", id);

                connection.Open();

                var result = await connection.QueryAsync<TimeRegistration>(querry);
                return result.Single();
            }
        }

        /// <summary>
        /// Realization of the Insert service method
        /// </summary>
        /// <param name="timeIn">TimeIn for new TimeRegistration record</param>
        /// <param name="timeOut">TimeOut for new TimeRegistration record</param>
        /// <returns>Return new TimeRegistration record</returns>
        public static async Task<int> Insert(DateTime timeIn, DateTime timeOut) 
        {
            using (SqlConnection connection = GetConnetion())
            {
                var querry = String.Format(
                    "SELECT * FROM TimeRegistration WHERE (TimeOut >= '{0:s}' AND TimeOut <= '{1:s}') OR (TimeIn <= '{1:s}' AND TimeIn >= '{0:s}') OR (TimeIn < '{0:s}' AND TImeOut > '{1:s}')",
                    timeIn, timeOut);

                connection.Open();

                var timeCrossingRecords = (await connection.QueryAsync<TimeRegistration>(querry)).ToList();

                if (timeCrossingRecords.Count > 0)
                {
                    await ProcessCrossings(connection, timeIn, timeOut, timeCrossingRecords);
                }

                return await Insert(connection, timeIn, timeOut);
            }
        }

        /// <summary>
        /// Realization of the Update service method
        /// </summary>
        /// <param name="id">ID of requested TimeRegistration record</param>
        /// <param name="timeIn">New TimeIn for TimeRegistration record</param>
        /// <param name="timeOut">New TimeOut for TimeRegistration record</param>
        /// <exception cref="ArgumentException">Error reporting the absence of a record behind the incoming ID</exception>
        public static async Task Update(int id, DateTime timeIn, DateTime timeOut) 
        {
            using (SqlConnection connection = GetConnetion())
            {
                var existanceQuerryRecord = String.Format(
                    "SELECT * FROM TimeRegistration WHERE Id = {0}",
                    id);
                connection.Open();
                var existanceRecord = (await connection.QueryAsync<TimeRegistration>(existanceQuerryRecord)).SingleOrDefault();
                
                if (existanceRecord == null)
                {
                    throw new ArgumentException("Record not found");
                }
                
                var crossingQuerry = String.Format(
                        "SELECT * FROM TimeRegistration WHERE ((TimeOut >= '{0:s}' AND TimeOut <= '{1:s}') OR (TimeIn <= '{1:s}' AND TimeIn >= '{0:s}') OR (TimeIn < '{0:s}' AND TImeOut > '{1:s}')) AND Id != {2}",
                        timeIn, timeOut, id);
                var timeCrossingRecords = (await connection.QueryAsync<TimeRegistration>(crossingQuerry)).ToList();

                if (timeCrossingRecords.Count > 0)
                {
                    await ProcessCrossings(connection, timeIn, timeOut, timeCrossingRecords);
                }

                existanceRecord.Id = id;
                existanceRecord.TimeIn = timeIn;
                existanceRecord.TimeOut = timeOut; 
                await Update(connection, existanceRecord);
            }
        }

        /// <summary>
        /// Realization of the Delete service method
        /// </summary>
        /// <param name="id">ID of requested TimeRegistration record</param>
        public static async Task Delete(int id) 
        {
            using (SqlConnection connection = GetConnetion())
            {
                connection.Open();

                await Delete(connection, id);
            }
        }

        private static async Task<int> Insert(SqlConnection connection, DateTime timeIn, DateTime timeOut) //Creation new recodrs 
        {
            var insert = String.Format(
                "INSERT TimeRegistration (TimeIn,TimeOut) VALUES ('{0:s}','{1:s}') SELECT IDENT_CURRENT('TimeRegistration') AS [IDENT_CURRENT]",
                timeIn, timeOut);
            var result = await connection.ExecuteScalarAsync<int>(insert);

            return result;
        }

        private static async Task Update(SqlConnection connection, TimeRegistration existanceRecord) //Update records by ID
        {
            string update = String.Format(
                "UPDATE TimeRegistration SET TimeIn = '{1:s}', TimeOut = '{2:s}' WHERE Id = {0}",
                existanceRecord.Id,existanceRecord.TimeIn,existanceRecord.TimeOut);

            await connection.ExecuteAsync(update);
        }

        private static async Task Delete(SqlConnection connection, int id) //Delete Records by ID
        {
            var delete = String.Format(
                "DELETE FROM TimeRegistration WHERE Id = {0}",
                id);

            await connection.ExecuteAsync(delete);
        }

        /// <summary>
        /// Check on crossing records
        /// </summary>
        /// <param name="connection">Represents a connection to a SQL Server</param>
        /// <param name="timeIn">New TimeIn for TimeRegistration record</param>
        /// <param name="timeOut">New TimeOut for TimeRegistration record</param>
        /// <param name="timeCrossingRecords"></param>
        private static async Task ProcessCrossings(SqlConnection connection, DateTime timeIn, DateTime timeOut, List<TimeRegistration> timeCrossingRecords) 
        {
            foreach (var registration in timeCrossingRecords)
            {
                var crossingType = GetCrossingType(registration, timeIn, timeOut);

                switch (crossingType)
                {
                    case CrossingType.StartTime: 
                        await ProcessStartTimeCrossing(registration, timeIn, connection);
                        break;
                    case CrossingType.EndTime: 
                        await ProcessEndTimeCrossing(registration, timeOut, connection);
                        break;
                    case CrossingType.Inserted: 
                        await ProcessInsertedCrossing(registration, timeIn, timeOut, connection);
                        break;
                    case CrossingType.Covered: 
                        await ProcessCoveredCrossing(registration, connection);
                        break;
                }
            }
        }

        /// <summary>
        /// If overlaps other records
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="connection">Represents a connection to a SQL Server</param>
        private static async Task ProcessCoveredCrossing(TimeRegistration registration, SqlConnection connection) 
        {
            await Delete(connection, registration.Id);
        }

        /// <summary>
        /// If crossing is inside other records 
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="timeIn">New TimeIn for TimeRegistration record</param>
        /// <param name="timeOut">New TimeOut for TimeRegistration record</param>
        /// <param name="connection">Represents a connection to a SQL Server</param>
        private static async Task ProcessInsertedCrossing(TimeRegistration registration, DateTime timeIn, DateTime timeOut, SqlConnection connection)
        {
            var oldTimeIn = registration.TimeIn;
            var oldTimeOut = registration.TimeOut;

            if (oldTimeIn == timeIn)
            {
                registration.TimeIn = timeOut;

                await Update(connection, registration);
            }
            else 
            {
                registration.TimeOut = timeIn;

                await Update(connection, registration);

                if (oldTimeOut != timeOut)
                {
                    await Insert(connection, timeOut, oldTimeOut);
                }
            }
        }

        /// <summary>
        /// If crossing is at the ending of other records
        /// </summary>
        /// <param name="registration">TimeRegistration record by requirement task</param>
        /// <param name="timeOut">New TimeOut for TimeRegistration record</param>
        /// <param name="connection">Represents a connection to a SQL Server</param>
        private static async Task ProcessEndTimeCrossing(TimeRegistration registration, DateTime timeOut, SqlConnection connection)
        {
            registration.TimeIn = timeOut;

            await Update(connection, registration);
        }

        /// <summary>
        /// If crossing is at the beginning of other records
        /// </summary>
        /// <param name="registration">TimeRegistration record by requirement task</param>
        /// <param name="timeIn">New TimeIn for TimeRegistration record</param>
        /// <param name="connection">Represents a connection to a SQL Server</param>
        private static async Task ProcessStartTimeCrossing(TimeRegistration registration, DateTime timeIn, SqlConnection connection)
        {
            registration.TimeOut = timeIn;

            await Update(connection, registration);
        }

        /// <summary>
        /// Get SQL connection
        /// </summary>
        private static SqlConnection GetConnetion()  
        {
            var connectionString = ConfigurationManager.ConnectionStrings["RegistrationDBContext"].ConnectionString;

            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Method to check crossing
        /// </summary>
        /// <param name="registration">TimeRegistration record by requirement task</param>
        /// <param name="timeIn">New TimeIn for TimeRegistration record</param>
        /// <param name="timeOut">New TimeOut for TimeRegistration record</param>
        /// <exception cref="ArgumentException">Error for unexcepted crossing</exception>
        private static CrossingType GetCrossingType(TimeRegistration registration, DateTime timeIn, DateTime timeOut) 
        {

            if (timeIn <= registration.TimeIn && timeOut >= registration.TimeOut)
            {
                return CrossingType.Covered;
            }

            if (timeIn < registration.TimeOut && timeOut > registration.TimeOut)
            {
                return CrossingType.StartTime;
            }

            if (timeOut > registration.TimeIn && timeIn < registration.TimeIn)
            {
                return CrossingType.EndTime;
            }

            if (timeIn >= registration.TimeIn && timeOut <= registration.TimeOut)
            {
                return CrossingType.Inserted;
            }

            throw new ArgumentException("Unexcepted crosing type found");
        }
    }

    internal enum CrossingType
    {
        StartTime,
        EndTime,
        Inserted,
        Covered
    }
}

