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

        // Raw rotation count, alongside the pre-formatted TripOdometerDescription
        // above - needed so a board opened straight from this cached snapshot (see
        // BoardListPage.BoardSelectedAsync) can reuse OWBoard's own
        // TripOdometerDescription computed property correctly instead of showing a
        // fabricated "0 km"/"0 mi".
        public ushort TripOdometer { get; set; }

        public float LifetimeOdometer { get; set; }
        public float LifetimeAmpHours { get; set; }
        public DateTime LastUpdated { get; set; }

        public void Save()
        {
            Database.Connection.InsertOrReplace(this);
        }
    }
}
