namespace TeamSpeak3QueryApi.Net
{
    /// <summary>Represents a query error that comes with every API response.</summary>
    public class QueryError
    {
        /// <summary>The ID of the error. An ID of 0 means that there is no error.</summary>
        /// <returns>The ID of the error.</returns>
        public int Id { get; internal set; }

        /// <summary>The error message. Is only set if there is an error message.</summary>
        /// <returns>The error message. </returns>
        public string Message { get; internal set; }

        /// <summary>If the cause of the error was a missing permission, this property represents the ID of the permission the client does not have. A value of 0 means that there was no permission error.</summary>
        /// <returns>The ID of the missing permission. If there is none, 0.</returns>
        /// <remarks>Check the <see cref="QueryError.Id"/> of the <see cref="TeamSpeak3QueryApi.Net.QueryError"/> to determine if there was a permission error.</remarks>
        public int FailedPermissionId { get; internal set; }
    }
}
