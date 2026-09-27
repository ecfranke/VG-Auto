using System;
using System.Data.Common;
using Carmasters.Core.Domain;

namespace Carmasters.Core.Application.Errors
{
    public class JsonErrorDto
    {
        /// <summary>
        /// When false (production default) internal exception messages and stack traces are not sent to clients.
        /// </summary>
        public static bool IncludeDetails { get; set; }

        public const string GenericMessage = "An unexpected error occurred. See the server log for details.";

        public JsonErrorDto(string message, string exceptionDetails)
        {
            ExceptionMessage = message;
            ExceptionDetails = IncludeDetails ? exceptionDetails : null;
        }

        public JsonErrorDto(Exception exception)
        {
            IsUserError = exception is UserException;
            ExceptionMessage = IsUserError || IncludeDetails ? exception.Message : GenericMessage;
            ExceptionDetails = IncludeDetails ? exception.ToString() : null;

            if (IsForeignKeyViolation(exception))
            {
                IsUserError = true;
                ExceptionMessage = "Problem occured while deleting data, there is other data associated with it, preventing the removal.";
            }
        }

        private static bool IsForeignKeyViolation(Exception exception)
        {
            for (var e = exception; e != null; e = e.InnerException)
            {
                if (e is DbException db)
                {
                    // PostgreSQL: 23503, MySQL: 23000 + error 1451 (cannot delete or update a parent row)
                    if (db.SqlState == "23503") return true;
                    if (db.SqlState == "23000" && db.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        public bool IsUserError { get; }
        public string ExceptionMessage { get; }
        public string ExceptionDetails { get; }
    }
}
