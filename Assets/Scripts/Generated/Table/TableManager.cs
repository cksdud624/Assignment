using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Generated.Table
{
	public class TableManager : MonoBehaviour
	{
		public CharacterStatusRecord CharacterStatusRecord {get; private set;}
		public MiningEquipmentsRecord MiningEquipmentsRecord {get; private set;}

		public async UniTask Init()
		{
			CharacterStatusRecord = new ();
			await CharacterStatusRecord.Init();
			MiningEquipmentsRecord = new ();
			await MiningEquipmentsRecord.Init();
		}
	}
}
