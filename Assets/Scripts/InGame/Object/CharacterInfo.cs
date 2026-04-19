using Generated.Table;
using UniRx;

namespace InGame.Object
{
    public class CharacterInfo
    {
        public ReactiveProperty<int> MiningLevel { get; } = new(1);
        public ReactiveProperty<int> MiningItemCount { get; } = new(0);
        public ReactiveProperty<int> MaxMiningItemCount { get; } = new(0);
        public ReactiveProperty<int> HandCuffCount { get; } = new(0);
        public ReactiveProperty<int> Money { get; } = new(0);
        public CharacterStatusData Status { get; set; }
    }
}
