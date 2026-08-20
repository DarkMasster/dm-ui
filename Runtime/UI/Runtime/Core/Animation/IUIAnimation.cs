using Cysharp.Threading.Tasks;

namespace DM.UI
{
    public interface IUIAnimation
    {
        string[] Categories { get; }

        UniTask Play();

        UniTask PlayBackwards();
    }
}