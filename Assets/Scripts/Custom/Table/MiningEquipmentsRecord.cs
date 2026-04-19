using System.Collections.Generic;
using System.Linq;

namespace Generated.Table
{
    public partial class MiningEquipmentsRecord
    {
        private Dictionary<int, MiningEquipmentsData> datasByLevel = new();

        partial void InitCustomRecord()
        {
            foreach (var data in datas)
            {
                datasByLevel.Add(data.Level, data);
            }
        }

        public MiningEquipmentsData GetRecordByLevel(int level)
        {
            datasByLevel.TryGetValue(level, out var record);
            return record;
        }
    }
}
