using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Generated.Table
{
	public partial class MiningEquipmentsRecord
	{
		private const string Key = "Assets/Generated/Table/MiningEquipments.bytes";
		private List<MiningEquipmentsData> datas = new();
		private Dictionary<long, MiningEquipmentsData> datasById = new();
		partial void InitCustomRecord();
		public async UniTask Init()
		{
			var asset = await Addressables.LoadAssetAsync<TextAsset>(Key).ToUniTask();
			if(asset == null)
				throw new System.OperationCanceledException($"Load failed: {Key}");
			using (MemoryStream ms = new MemoryStream(asset.bytes))
			using (BinaryReader reader = new BinaryReader(ms))
			{
				while (reader.BaseStream.Position < reader.BaseStream.Length)
				{
					MiningEquipmentsData data = new (reader);
					datas.Add(data);
					datasById.Add(data.Id, data);
				}
			}
			InitCustomRecord();
		}
		public MiningEquipmentsData GetRecord(long id)
		{
			datasById.TryGetValue(id, out var record);
			return record;
		}
		public List<MiningEquipmentsData> GetAllRecord()
		{
			return datas;
		}
	}

	public class MiningEquipmentsData
	{
		public long Id {get; private set;}
		public string Name {get; private set;}
		public int Level {get; private set;}
		public Vector3 Center {get; private set;}
		public Vector3 Range {get; private set;}
		public float MiningTime {get; private set;}
		public float MiningTrigger {get; private set;}
		public int MaxMiningItemCount {get; private set;}
		public int RequiredLevelUp {get; private set;}

		public MiningEquipmentsData(BinaryReader reader)
		{
			string[] tableDatas = reader.ReadString().Split('	');
			Id = long.TryParse(tableDatas[0], out long vLong0) ? vLong0 : 0L;
			Name = tableDatas[1];
			Level = int.TryParse(tableDatas[2], out int vInt2) ? vInt2 : 0;
			string[] items3 = tableDatas[3].Split(';');
			if (items3.Length == 3)
			{
				float.TryParse(items3[0], out float resultX3);
				float.TryParse(items3[1], out float resultY3);
				float.TryParse(items3[2], out float resultZ3);
				Center = new Vector3(resultX3, resultY3, resultZ3);
			}
			else
			{
				Center = Vector3.zero;
				Debug.LogError(Center + "is not Vector3");
			}
			string[] items4 = tableDatas[4].Split(';');
			if (items4.Length == 3)
			{
				float.TryParse(items4[0], out float resultX4);
				float.TryParse(items4[1], out float resultY4);
				float.TryParse(items4[2], out float resultZ4);
				Range = new Vector3(resultX4, resultY4, resultZ4);
			}
			else
			{
				Range = Vector3.zero;
				Debug.LogError(Range + "is not Vector3");
			}
			MiningTime = float.TryParse(tableDatas[5], out float vFloat5) ? vFloat5 : 0f;
			MiningTrigger = float.TryParse(tableDatas[6], out float vFloat6) ? vFloat6 : 0f;
			MaxMiningItemCount = int.TryParse(tableDatas[7], out int vInt7) ? vInt7 : 0;
			RequiredLevelUp = int.TryParse(tableDatas[8], out int vInt8) ? vInt8 : 0;
		}
	}
}
