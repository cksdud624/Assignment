using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Generated.Table
{
	public partial class CharacterStatusRecord
	{
		private const string Key = "Assets/Generated/Table/CharacterStatus.bytes";
		private List<CharacterStatusData> datas = new();
		private Dictionary<long, CharacterStatusData> datasById = new();
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
					CharacterStatusData data = new (reader);
					datas.Add(data);
					datasById.Add(data.Id, data);
				}
			}
			InitCustomRecord();
		}
		public CharacterStatusData GetRecord(long id)
		{
			datasById.TryGetValue(id, out var record);
			return record;
		}
		public List<CharacterStatusData> GetAllRecord()
		{
			return datas;
		}
	}

	public class CharacterStatusData
	{
		public long Id {get; private set;}
		public string Name {get; private set;}
		public float MoveSpeed {get; private set;}

		public CharacterStatusData(BinaryReader reader)
		{
			string[] tableDatas = reader.ReadString().Split('	');
			Id = long.TryParse(tableDatas[0], out long vLong0) ? vLong0 : 0L;
			Name = tableDatas[1];
			MoveSpeed = float.TryParse(tableDatas[2], out float vFloat2) ? vFloat2 : 0f;
		}
	}
}
