using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class Player : MonoBehaviour, IAttackable
{
    //HP
    private int maxHealth;
    public int MaxHealth
    {
        get { return maxHealth; }
        private set { maxHealth = Mathf.Clamp(value, 0, 24); }
    }
    private int health;
    public int Health
    {
        get { return health; }
        private set { health = Mathf.Clamp(value, 0, maxHealth); }
    }
    private int soulHealth;
    public int SoulHealth
    {
        get { return soulHealth; }
        private set { soulHealth = value > 0 ? value : 0; }
    }
    public enum HealthType { Normal, Soul }

    //이동 속도
    public float Speed { get; set; }
    public float SpeedMultiple { get; set; }

    //범위
    private float range;
    public float Range
    {
        get { return range; }
        set { range = value > 5f ? value : 5f; }
    }

    //눈물
    //사격 지연을 계산하는 데 사용되며 일반 소품의 발사 속도 효과가 여기에 작용
    public float Tears { get; set; }
    //발사 지연은 발사 간격을 계산하는데 사용되며 일부 소품은 다중 및 추가에 작동
    public int TearsDelay
    {
        get
        {
            int temp;
            if (Tears >= 0)
            {
                temp = (int)(16 - 6 * Mathf.Sqrt(1.3f * Tears + 1));
            }
            else if (Tears >= -10f / 13f)
            {
                temp = (int)((16 - 6 * Mathf.Sqrt(1.3f * Tears + 1)) - 6 * Tears);
            }
            else
            {
                temp = (int)(16 - 6 * Tears);
            }
            temp = temp > 5 ? temp : 5;
            temp = temp * TearsDelayMultiple + TearsDelayAdded;
            return temp > 1 ? temp : 1;
        }
    }
    public int TearsDelayMultiple { get; set; }
    public int TearsDelayAdded { get; set; }
    //실제 사격 계산에 사용되는 사격 간격
    private float ShotCD
    {
        get { return 1f / (30f / (TearsDelay + 1)); }
    }
    //발사 타이밍
    private float shotTiming;

    //발사 속도
    private float shotSpeed;
    public float ShotSpeed
    {
        get { return shotSpeed; }
        set { shotSpeed = value > 0.6f ? value : 0.6f; }
    }

    //넉백
    public float Knockback { get; set; }

    //데미지  
    public float Damage
    {
        //피해 = 3.5 * 데미지 증가 * √(기본 데미지 * 1.2f + 1f)+ 추가 데미지
        get { return (float)Math.Round(3.5f * DamageMultiple * Mathf.Sqrt((DamageBase * 1.2f + 1f)) + DamageAdded, 2); }
    }
    public float DamageMultiple { get; set; }
    public float DamageBase { get; set; }
    public float DamageAdded { get; set; }

    //Luck
    public int Luck { get; set; }

    //획득한 아이템
    public ItemModel itemModle;
    public int CoinNum { get; set; }
    public int KeyNum { get; set; }
    public int BombNum { get; set; }

    //상태
    [HideInInspector]
    public bool isLive;
    bool isControllable;
    bool isInvincible;
    Vector2 moveInput;

    //총알과 상호 작용할 수 있는 개체 클래스 목록
    [HideInInspector]
    public List<string> TagThatDefaultByBullet;
    [HideInInspector]
    public List<Type> TypeThatDefaultByBullet;
    [HideInInspector]
    public List<Type> TypeThatCanBeAttackedByBullet;
    [HideInInspector]
    public List<Type> TypeThatCanBeDestroyedByBullet;
    //관통능력
    [HideInInspector]
    public bool penetrating = false;

    [Header("自身")]
    public Transform head;
    public Transform body;
    Rigidbody2D myRigidbody;
    SpriteRenderer bodyRanderer;
    SpriteRenderer headRanderer;
    Animator wholeAnimation;
    Animator headAnimation;
    Animator bodyAnimation;

    [Header("其他")]
    public BulletPools bulletPool;
    public Transform bulletContainer;
    public TheBomb bombPrefab;
    UIManager UI;
    Level level;

    void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        headRanderer = head.GetComponent<SpriteRenderer>();
        bodyRanderer = body.GetComponent<SpriteRenderer>();
        wholeAnimation = GetComponent<Animator>();
        headAnimation = head.GetComponent<Animator>();
        bodyAnimation = body.GetComponent<Animator>();
    }

    void Start()
    {
        level = GameManager.Instance.level;
        UI = UIManager.Instance;

        PlayerInitialize();
    }

    void Update()
    {
        //죽음과 부활 테스트
        if (Input.GetKey(KeyCode.O))
        {
            PlayerInitialize();
        }
        if (Input.GetKey(KeyCode.P))
        {
            PlayerDeath();
        }

        if (!isControllable) { return; }
        UpdateControl();
        UpdateMovement();
        UpdateAnimator();
    }

    /// <summary>
    /// 제어 입력
    /// </summary>
    void UpdateControl()
    {
        shotTiming += Time.deltaTime;
        if (shotTiming >= ShotCD)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                LaunchBullet(KeyCode.UpArrow);
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                LaunchBullet(KeyCode.DownArrow);
            }
            else if (Input.GetKey(KeyCode.LeftArrow))
            {
                LaunchBullet(KeyCode.LeftArrow);
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                LaunchBullet(KeyCode.RightArrow);
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            GenerateBomb();
        }
    }

    /// <summary>
    /// 이동
    /// </summary>
    void UpdateMovement()
    {
        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");
        moveInput = h * Vector2.right + v * Vector2.up;
        //정규화, 값 0 - 1, 대각선으로 걷는 속도는 이동 속도를 초과하지 않습니다.
        if (moveInput.magnitude > 1f)
        {
            moveInput.Normalize();
        }
        //myRigidbody.velocity = moveInput * Speed * SpeedMultiple * 1.7f;
        myRigidbody.velocity = moveInput * (0.5f + 0.5f * Speed) * SpeedMultiple * 1.7f;
    }

    /// <summary>
    /// 애니메이션 업데이트
    /// </summary>
    void UpdateAnimator()
    {
        if (moveInput.x < 0) { bodyRanderer.flipX = true; }
        if (moveInput.x > 0) { bodyRanderer.flipX = false; }
        bodyAnimation.SetFloat("Up&Down", Mathf.Abs(moveInput.y));
        bodyAnimation.SetFloat("Left&Right", Mathf.Abs(moveInput.x));
    }

    /// <summary>
    /// 버튼을 누르면 총알 발사
    /// </summary>
    void LaunchBullet(KeyCode key)
    {
        int force = 120;//총알의 주요 방향에 가해지는 힘
        int mainCorrect;//총알 주방향 보정 값
        int minorCorrect = 50;//총알 2차 방향 보정 값
        GameObject bullet = bulletPool.Take();
        Rigidbody2D rigidbody = bullet.GetComponent<Rigidbody2D>();
        bullet.GetComponent<Bullet>().Initialization();
        bullet.transform.position = transform.position + new Vector3(0, 0.0055f * Range - 0.13f, 0);

        if (key == KeyCode.UpArrow)
        {
            headAnimation.Play("Up");
            //발사 방향과 캐릭터의 현재 이동 방향을 기반으로 총알에 초기 힘을 부여
            //다음 공식을 예로 들면, 총알에 가해지는 힘은 다음과 같습니다. new Vector2(2차 방향의 힘, 주 방향의 힘)
            mainCorrect = 15;
            rigidbody.AddForce(new Vector2(moveInput.x * minorCorrect, moveInput.y * mainCorrect + force) * ShotSpeed);
        }
        else if (key == KeyCode.DownArrow)
        {
            headAnimation.Play("Down");
            mainCorrect = 15;
            rigidbody.AddForce(new Vector2(moveInput.x * minorCorrect, moveInput.y * mainCorrect - force) * ShotSpeed);
        }
        else if (key == KeyCode.LeftArrow)
        {
            headAnimation.Play("Left");
            //발사 방향이 캐릭터의 이동 방향과 동일하도록 mainCordirect를 수정합니다.
            //같은 방향에서는 추력이 더 크고 반대 방향에서는 저항이 더 작습니다.
            mainCorrect = moveInput.x >= 0 ? 5 : 40;
            rigidbody.AddForce(new Vector2(moveInput.x * mainCorrect - force, moveInput.y * minorCorrect) * ShotSpeed);
        }
        else if (key == KeyCode.RightArrow)
        {
            headAnimation.Play("Right");
            mainCorrect = moveInput.x >= 0 ? 40 : 5;
            rigidbody.AddForce(new Vector2(moveInput.x * mainCorrect + force, moveInput.y * minorCorrect) * ShotSpeed);
        }

        shotTiming = 0;
    }

    /// <summary>
    /// 폭탄 설치
    /// </summary>
    void GenerateBomb()
    {
        if (BombNum >= 1)
        {
            BombNum--;
            Vector2 pos = transform.position + new Vector3(0, -0.15f);
            Instantiate<TheBomb>(bombPrefab, pos, Quaternion.identity);
            UI.attributes.UpDateAttributes();
        }
    }

    /// <summary>
    /// 피해
    /// </summary>
    /// <param name="damage"></param>
    public void BeAttacked(float damage, Vector2 direction, float forceMultiple = 1)
    {
        if (isInvincible || !isLive) { return; }
        ReduceHealth((int)damage);
        if (isLive)
        {
            StartCoroutine(knockBackCoroutine(direction * forceMultiple));
            StartCoroutine(Invincible());
        }
    }

    /// <summary>
    /// 넉백
    /// </summary>
    /// <param name="force"></param>
    /// <returns></returns>
    IEnumerator knockBackCoroutine(Vector2 force)
    {
        //입력 작업으로 인한 이동량을 줄입니다.
        SpeedMultiple = 0.5f;

        float length = 0.3f;
        float overTime = 0.1f;
        float timeleft = overTime;
        while (timeleft > 0)
        {
            //초과 시간 내 방향 이동 * 길이 거리
            transform.Translate(force * length * Time.deltaTime / overTime);
            timeleft -= Time.deltaTime;
            yield return null;
        }

        //절감
        SpeedMultiple = 1;
    }
    /// <summary>
    /// 무적 상태에 들어가 플래시
    /// </summary>
    IEnumerator Invincible()
    {
        isInvincible = true;
        Color red = new Color(1, 0.2f, 0.2f, 1);

        float time = 0;//타이밍
        float flashCD = 0;//깜빡임 타이밍

        while (time < 1f)
        {
            time += Time.deltaTime;
            flashCD += Time.deltaTime;
            if (flashCD > 0)
            {
                if (bodyRanderer.color == Color.white)
                {
                    bodyRanderer.color = red;
                    headRanderer.color = red;
                }
                else if (bodyRanderer.color == red)
                {
                    bodyRanderer.color = Color.white;
                    headRanderer.color = Color.white;
                }
                flashCD -= 0.13f;
            }
            yield return null;
        }
        isInvincible = false;
    }

    /// <summary>
    /// 피를 더하다
    /// </summary>
    /// <param name="health"></param>
    /// <param name="type"></param>
    /// <param name="maxHealth"></param>
    public void AddHealth(int health, HealthType type, int maxHealth = 0)
    {
        this.MaxHealth += maxHealth;
        switch (type)
        {
            case HealthType.Normal:
                Health += health;
                break;
            case HealthType.Soul:
                SoulHealth += health;
                break;
            default:
                break;
        }

        UI.hp.UpdateHP();
    }
    /// <summary>
    /// 혈액을 빼다
    /// </summary>
    /// <param name="damage"></param>
    public void ReduceHealth(int damage)
    {
        int tempHealth;

        //앞으로 새로운 혈액량 유형을 추가할 경우 계산 레이어를 하나만 더 작성하면 됩니다.
        tempHealth = SoulHealth;
        SoulHealth -= damage;
        damage = damage - tempHealth > 0 ? damage - tempHealth : 0;

        Health -= damage;
        UI.hp.UpdateHP();
        if (Health == 0) { PlayerDeath(); }
    }

    /// <summary>
    /// 상태 초기화
    /// </summary>
    public void PlayerInitialize()
    {
        MaxHealth = 6;
        Health = MaxHealth;
        SoulHealth = 0;

        Speed = 1f;
        SpeedMultiple = 1f;

        Range = 23.75f;
        Tears = 0;
        TearsDelayMultiple = 1;
        TearsDelayAdded = 0;
        shotTiming = 0;
        ShotSpeed = 1;
        Knockback = 1;

        DamageMultiple = 1f;
        DamageBase = 0f;
        DamageAdded = 0f;

        Luck = 0;

        CoinNum = 10;
        KeyNum = 1;
        BombNum = 10;

        isLive = true;
        isControllable = true;
        isInvincible = false;
        bodyRanderer.enabled = true;
        headRanderer.enabled = true;
        wholeAnimation.SetBool("isLive", true);
        myRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;

        //백팩 초기화
        itemModle = new ItemModel();

        //접촉 시 눈물의 자가 제거를 유발하지만 다른 방법은 유발 안함: 암석, 철 블록
        TypeThatDefaultByBullet = new List<Type>() { typeof(Rock), typeof(MetalBlock) };
        //위와 동일. 클래스가 없기 때문에 태그를 사용하여 벽 등을 판단
        TagThatDefaultByBullet = new List<string>() { "Wall", "MoveCollider" };
        //접촉 후 충돌 물체를 유발하는 타격 방법: 몬스터, 불더미, 똥더미
        TypeThatCanBeAttackedByBullet = new List<Type>() { typeof(Monster), typeof(Poop), typeof(Fireplace) };
        //접촉 후 발생하는 충돌 객체의 파괴 방법: 기본적으로 비어 있음
        TypeThatCanBeDestroyedByBullet = new List<Type>() { };

        UI.PlayerUIInitialize();
    }
    public void PlayerDeath()
    {
        isLive = false;
        isControllable = false;
        bodyRanderer.enabled = false;
        headRanderer.enabled = false;
        wholeAnimation.SetBool("isLive", false);
        myRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
    }
    public void PlayerPause()
    {
        isControllable = false;
        myRigidbody.velocity = Vector2.zero;
        headAnimation.speed = 0;
        bodyAnimation.speed = 0;
    }
    public void PlayerQuitPause()
    {
        isControllable = true;
        headAnimation.speed = 1;
        bodyAnimation.speed = 1;
    }

    /// <summary>
    /// 충돌 대상을 판단하고 다음 방으로 이동
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter2D(Collision2D collision)
    {
        //다음 방으로 이동
        if (isControllable && collision.transform.CompareTag("MoveCollider"))
        {
            if (collision.transform.name == "Up")
            {
                level.MoveToNextRoom(Vector2.up);
            }
            else if (collision.transform.name == "Down")
            {
                level.MoveToNextRoom(Vector2.down);
            }
            else if (collision.transform.name == "Left")
            {
                level.MoveToNextRoom(Vector2.left);
            }
            else if (collision.transform.name == "Right")
            {
                level.MoveToNextRoom(Vector2.right);
            }
        }
    }
}