
public interface IPlayerState 
{
    void Enter();  // その状態の部屋に「入った瞬間」に1回だけやる仕事
    void Update(); // その状態の部屋に「いる間」、毎フレーム連打でやる仕事
    void FixedUpdate() {}
    void Exit();   // その状態の部屋から「出る瞬間」に1回だけやる仕事
}
