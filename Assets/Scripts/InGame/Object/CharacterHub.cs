using Common;
using InGame.Components;
using UniRx;
using static Common.GameDefine;

namespace InGame.Object
{
    public class CharacterHub : ObjectHub
    {
        public ControllerBase Controller { get; set; }
        public CharacterBehaviour Behaviour { get; set; }
        public CharacterInfo Info { get; set; }
        public ReactiveProperty<CharacterState> CharacterState { get; } = new(GameDefine.CharacterState.Idle);
    }
}
