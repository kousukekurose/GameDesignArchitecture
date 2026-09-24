
namespace GameDesignArchitecture.Interfaces
{
    public interface IPlayerController
    {
        void ChangeState(IPlayerState newState);
        void TakeDamage(int damage);
        int CurrentHp{get;}
        bool IsFround{get; set;}
        int JunpCount{get; set;}
    }
}
