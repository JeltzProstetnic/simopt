using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace MatthiasToolbox.Player.Data
{
    // TODO: ███ MOVE TO LOGGING
    [Table(Name = "tblDatabaseLog")]
    public class DatabaseLog
    {
        public DatabaseLog() { }

        public DatabaseLog(long timeStamp, int severity, string messageClass, string message, string sender)
        {
            SessionID = Database.SessionID;
            TimeStampTicks = timeStamp;
            Severity = severity;
            MessageClass = messageClass;
            Message = message;
            SenderName = sender;
        }

        #region data

#pragma warning disable 0649
        private int id;
#pragma warning restore 0649

        [Column(Storage = "id",
                AutoSync = AutoSync.OnInsert,
                IsPrimaryKey = true,
                IsDbGenerated = true)]
        public int ID
        {
            get { return id; }
        }

        [Column]
        public long SessionID { get; set; }

        [Column]
        public long TimeStampTicks { get; set; }

        [Column]
        public int Severity { get; set; }

        [Column]
        public string MessageClass { get; set; }

        [Column]
        public string Message { get; set; }

        [Column]
        public string SenderName { get; set; }

        #endregion
    }
}