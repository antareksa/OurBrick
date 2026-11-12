namespace BrickbreakerBase
{
    public class BasicBallHitEffect : BaseBallHitEffect
    {
        protected override void ResolveHit(BrickController brickController)
        {
            DealDamage(brickController);
        }
    }
}
