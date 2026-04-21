using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.Object;

namespace InGame.Gameplay
{
    public interface IAIPickupSource
    {
        bool HasItems { get; }
        int ItemCount { get; }
        bool IsAIInZone(CharacterBase ai);
        UniTask PickupForAI(CharacterBase ai, int threshold, CancellationToken ct);
    }

    public interface IAIDeliverTarget
    {
        UniTask ReceiveFromAI(CharacterBase ai, CancellationToken ct);
    }

    public interface IAIActivatable
    {
        UniTask ActivateForAI(CharacterBase ai, CancellationToken ct);
    }
}
