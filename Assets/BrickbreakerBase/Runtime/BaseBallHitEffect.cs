using UnityEngine;

namespace BrickbreakerBase
{
    // Attached alongside BallController -- decides what happens when that ball hits a brick,
    // including how much damage it deals. BallController doesn't know or care about damage; it
    // just reports the hit and this component handles the rest. Subclass this to add ball types
    // (e.g. fire that also hits neighbors via BrickManager.GetNeighbors).
    public abstract class BaseBallHitEffect : MonoBehaviour
    {
        public int AttackDamage = 1;

        public void OnHitBrick(BrickController brickController)
        {
            ResolveHit(brickController);
        }

        protected abstract void ResolveHit(BrickController brickController);

        protected void DealDamage(BrickController brickController)
        {
            if (brickController == null) return;

            brickController.DamageBrick(AttackDamage);
        }
    }
}
