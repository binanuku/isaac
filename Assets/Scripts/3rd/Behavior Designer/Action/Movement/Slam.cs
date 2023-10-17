using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorDesigner.Runtime.Tasks.Custom
{
    [TaskDescription("往特定方向冲撞并反弹,不使用寻路")]
    [TaskCategory("移动")]
    public class Slam : NormalMovement
    {
        public SharedFloat movementSpeed;
        public SharedVector2 direction;
        private Vector2 oldDirection;//方向缓存，具体看OnCollisionEnter2D内解释

        public override void OnStart()
        {
            direction.Value = Random.insideUnitCircle.normalized;
            oldDirection = direction.Value;
        }

        public override TaskStatus OnUpdate()
        {
            transform.Translate(direction.Value * movementSpeed.Value * Time.deltaTime);
            oldDirection = direction.Value;
            return TaskStatus.Running;
        }

        public override void OnCollisionEnter2D(Collision2D collision)
        {
            if (CommonUnit.LayerCheck(collision.gameObject, "Wall", "Obstacle"))
            {
                float x = collision.GetContact(0).normal.x;

                /* 두 개 이상의 충돌체가 동시에 접촉되면 이 메서드가 여러 번 트리거되어 방향이 원래 방향으로 여러 번 업데이트됩니다.
                 * 여기서는 OldDirection을 소개하는데, 방향이 업데이트되면 원래 방향으로 변경되지 않고 oldDirection으로 변경됩니다.
                 * oldDirection在碰撞结束后的下一帧再随direction跟新。这样即使多次调用也能得到同样的结果。
                 */

                //碰撞点为左右
                if (Mathf.Abs(x) > 0.9f) { direction.Value = new Vector2(-oldDirection.x, oldDirection.y); }
                //上下
                else if (Mathf.Abs(x) < 0.1f) { direction.Value = new Vector2(oldDirection.x, -oldDirection.y); }
                //其他
                else { direction.Value = -oldDirection; }

            }
        }
    }
}