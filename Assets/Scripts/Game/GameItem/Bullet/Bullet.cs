using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class Bullet : MonoBehaviour
{
    bool isDestory;
    float damage;
    float playerKnockback;
    float fallingDistance = -0.06f;//초당 자연 낙하 거리

    Rigidbody2D myRigidbody;
    Collider2D myCollider;
    Animator animator;

    Player player;
    BulletPools bulletPool;

    //개체 풀로 재활용하는 데 걸리는 시간
    WaitForSeconds WaitSeconds = new WaitForSeconds(2f);

    void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        player = GameManager.Instance.player;
        bulletPool = player.bulletPool;
    }

    void Update()
    {
        //총알의 자연스러운 낙하 시뮬레이션
        if (!isDestory) { transform.Translate(0, fallingDistance * Time.deltaTime, 0, Space.World); }
    }

    /// <summary>
    /// 초기화, 피해 부여, 밀쳐내기, 범위
    /// </summary>
    public void Initialization()
    {
        isDestory = false;
        damage = player.Damage;
        playerKnockback = player.Knockback;
        //총알은 일정 시간이 지나면 자동 파괴를 유발
        Invoke("AutoDestroy", player.Range * 0.03f);
    }

    /// <summary>
    /// 자동으로 파기
    /// </summary>
    void AutoDestroy()
    {
        if (isDestory) { return; }
        //총알이 빠르게 떨어지며 짧은 시간 후에 파괴
        myRigidbody.gravityScale = 1.7f;
        Invoke("Destroy", 0.13f);
    }

    /// <summary>
    /// 파괴
    /// </summary>
    void Destroy()
    {
        //중력을 끄고 이동을 멈추고 충돌체를 끄고 사라지는 애니메이션을 재생한 후 개체 풀로 돌아감
        isDestory = true;
        myRigidbody.gravityScale = 0;
        myRigidbody.velocity = Vector2.zero;
        myCollider.enabled = false;
        animator.Play("Destroy");
        StartCoroutine(GoBackToPool());
    }

    /// <summary>
    /// 개체 풀로 재활용
    /// </summary>
    /// <returns></returns>
    IEnumerator GoBackToPool()
    {
        yield return WaitSeconds;
        transform.position = Vector2.zero;
        myCollider.enabled = true;
        animator.Play("Idle");
        animator.Update(0);
        bulletPool.Back(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //접촉 시 눈물을 제거하고 다른 방법을 실행하지 않음
        if (CommonUnit.TagCheck(collision.gameObject, player.TagThatDefaultByBullet) || CommonUnit.ComponentCheck(collision.gameObject, player.TypeThatDefaultByBullet))
        {
            Destroy();
        }

        if (CommonUnit.TagCheck(collision.gameObject, new string[] { }))
        {

        }
        //접촉 시 눈물을 제거하고 개체의 적중 방식을 트리거
        else if (CommonUnit.ComponentCheck(collision.gameObject, player.TypeThatCanBeAttackedByBullet))
        {
            Vector3 force = Vector3.Normalize(collision.transform.position - transform.position) * playerKnockback;
            IAttackable iAttackable = collision.GetComponent<IAttackable>();
            iAttackable.BeAttacked(damage, force);
            if (player.penetrating == false)
            {
                Destroy();
            }
        }
        //접촉 시 눈물을 제거하고 물체의 파괴 방법을 발동
        else if (CommonUnit.ComponentCheck(collision.gameObject, player.TypeThatCanBeDestroyedByBullet))
        {
            IDestructible destructible = collision.GetComponent<IDestructible>();
            destructible.DestorySelf();
            if (player.penetrating == false)
            {
                Destroy();
            }
        }
        //다른 사람과 접촉 후 반응 없음
    }
}
