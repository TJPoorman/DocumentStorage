using LiteDB;

namespace DocumentStorage.Infrastructure.LiteDb
{
    public class LiteDbContextOptions
    {
        /// <summary>
        /// Return how engine will be open (default: Shared)
        /// </summary>
        public ConnectionType Connection { get; set; } = ConnectionType.Shared;

        /// <summary>
        /// Full path or relative path from DLL directory
        /// </summary>
        public string Filename { get; set; } = "";

        /// <summary>
        /// Database password used to encrypt/decypted data pages
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// If database is new, initialize with allocated space - support KB, MB, GB (default: 0)
        /// </summary>
        public long InitialSize { get; set; }

        /// <summary>
        /// Open datafile in readonly mode (default: false)
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Check if data file is an old version and convert before open (default: false)
        /// </summary>
        public bool Upgrade { get; set; }

        /// <summary>
        /// If last close database exception resulted in an invalid data state, rebuild datafile on next open (default: false)
        /// </summary>
        public bool AutoRebuild { get; set; }

        /// <summary>
        /// Set default collation on database creation (default: "[CurrentCulture]/IgnoreCase")
        /// </summary>
        public Collation Collation { get; set; }

        /// <summary>
        /// The BsonMapper to use for the repositories
        /// </summary>
        public BsonMapper? BsonMapper { get; set; } = null;
    }
}
