using System;
using SQLite;

namespace OWCE
{
    // Last-known snapshot of a board's data, persisted so it's still viewable when
    // not actively connected (see #34). Keyed per-board (by ID) rather than a single
    // "most recent" row, since a user may own more than one Onewheel.
    public class CachedBoardData
    {
        [PrimaryKey]
        public string BoardID { get; set; }

        public string Name { get; set; }
        public OWBoardType BoardType { get; set; }
        public int BatteryPercent { get; set; }
        public string RideModeString { get; set; }
        public string TripOdometerDescription { get; set; }
        public float LifetimeOdometer { get; set; }
        public float LifetimeAmpHours { get; set; }
        public DateTime LastUpdated { get; set; }

        public void Save()
        {
            Database.Connection.InsertOrReplace(this);
        }
    }
}
