﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour {

    public State PlayerState
    {
        get
        {
            return playerState;
        }
        set
        {
            playerState = value;
        }
    }

    public GameObject Cursor
    {
        get
        {
            return arrow;
        }
    }

    public int Attack
    {
        get
        {
            return attack;
        }
    }

    public float KnockbackStrength
    {
        get
        {
            return knockbackStrength;
        }
    }

    public float AttackMultiplier
    {
        get
        {
            return attackMultiplier;
        }
    }

    public Vector3 AimDirection
    {
        get 
        {
            return aimDirection;
        }
    }

    public event System.Action<int, int> OnHealthChanged;
    public event System.Action<int, int> OnExpChanged;

    public event System.Action<int> OnLevelChanged;

    [Header("Stats"), SerializeField] float speed = 1f;

    // Player Slots for item and skill
    BaseSkill playerSkill;
    BaseItem playerItem;

    Vector3 moveDirection;
    Vector3 lastValidMoveDir;
    Vector3 aimDirection;

    Vector3 velocity;

    Animator anim;

    int level;
    int exp = 0;
    int expToNextLevel;

    [SerializeField] int baseAttack = 3;
    [SerializeField] int attackGainPerLevel = 3;
    int attack;
    float attackMultiplier = 1f;
    float attackStartedTime;
    [SerializeField] float attackDuration = 0.5f;
    [SerializeField] float attackTwoDuration = 0.5f;
    [Range(1, 5), SerializeField] float attackTwoDamageMultiplier = 1.5f;
    [SerializeField] float attackThreeDuration = 0.5f;
    [Range(1, 10), SerializeField] float attackThreeDamageMultiplier = 2f;
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] float knockbackStrength = 1f;

    [SerializeField] int baseHealth = 5;
    [SerializeField] int healthGainPerLevel = 3;
    int health;
    int maxHealth;
    bool keepAttacking = false;

    float knockBackStarted;
    float knockBackDuration;
    Vector3 knockbackDir;

    [SerializeField] float dashForce = 1f;

    PlayerInput input;

    Camera cam;

    LayerMask projectileLayer;

    [SerializeField] GameObject arrow;
    [SerializeField] GameObject hitbox;
    [SerializeField] ParticleSystem footprints;
    ParticleSystem.MainModule footprintsMainModule;
    ParticleSystem.ShapeModule footprintsShapeModule;
    ParticleSystem.EmissionModule footprintsEmissionModule;
    ParticleSystem.MinMaxCurve standardRateOverDistance;

    [SerializeField] ParticleSystem dash;
    ParticleSystem.EmissionModule dashEmission;

    public GameObject projectile;

    public float yOffset;
    public float by;
    float layer;
    SpriteRenderer rend;
    Vector3 centerBottom;

    public enum State
    {
        freeToMove,
        dashing,
        attacking,
        attackingTwo,
        attackingThree,
        knockedBack
    }
    State playerState = State.freeToMove;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        cam = Camera.main;
        anim = GetComponent<Animator>();

        attack = baseAttack;
        health = baseHealth;
        maxHealth = baseHealth;

        expToNextLevel = (int)(Mathf.Pow(level, 2) * 2f);

        footprintsMainModule = footprints.main;
        footprintsShapeModule = footprints.shape;
        footprintsEmissionModule = footprints.emission;

        dashEmission = dash.emission;

        rend = GetComponent<SpriteRenderer>();

        int layer = LayerMask.NameToLayer("PlayerProjectiles");
        projectileLayer = 1 << layer;
    }

    private void Start()
    {
        if (OnExpChanged != null)
        {
            OnExpChanged(expToNextLevel, exp);
        }
        if (OnHealthChanged != null)
        {
            OnHealthChanged(health, maxHealth);
        }
        if (OnLevelChanged != null)
        {
            OnLevelChanged(level);
        }
    }

    private void Update()
    {
        CalculateOrderInLayer();

        //unparent the particle system and it does work
        footprints.transform.position = transform.position + new Vector3(0, -0.8f);
        dash.transform.position = transform.position;
        
        if (exp >= expToNextLevel)
        {
            LevelUp();
        }

        if (playerState == State.freeToMove)
        {
            footprintsEmissionModule.rateOverDistance = 1f;
            if (dash)
            {
                dashEmission.rateOverDistance = 0f;
            }
            GetInput();
            velocity = moveDirection * speed;
            if (input.UseItem && playerItem)
            {
                playerItem.Use();
            }
            if (input.UseSkill && playerSkill)
            {
                playerSkill.Use();
            }
            if (input.Attack && Time.realtimeSinceStartup > attackStartedTime + attackDuration + attackCooldown)
            {
                keepAttacking = false;
                playerState = State.attacking;
                attackMultiplier = 1f;
                anim.SetTrigger("Attack");
                attackStartedTime = Time.realtimeSinceStartup;
            }
        }
        else if (playerState == State.attacking)
        {
            GetInput();
            velocity = moveDirection * speed;
            if (input.Attack)
            {
                keepAttacking = true;
            }
            if (Time.realtimeSinceStartup > attackStartedTime + attackDuration && !keepAttacking)
            {
                playerState = State.freeToMove;
            }
            else if (Time.realtimeSinceStartup > attackStartedTime + attackDuration && keepAttacking)
            {
                keepAttacking = false;
                playerState = State.attackingTwo;
                attackMultiplier = attackTwoDamageMultiplier;
                anim.SetTrigger("AttackTwo");
                attackStartedTime = Time.realtimeSinceStartup;
            }
        }
        else if (playerState == State.attackingTwo)
        {
            GetInput();
            velocity = moveDirection * speed;
            if (input.Attack)
            {
                keepAttacking = true;
            }
            if (Time.realtimeSinceStartup > attackStartedTime + attackTwoDuration && !keepAttacking)
            {
                playerState = State.freeToMove;
            }
            else if (Time.realtimeSinceStartup > attackStartedTime + attackTwoDuration && keepAttacking)
            {
                playerState = State.attackingThree;
                attackMultiplier = attackThreeDamageMultiplier;
                anim.SetTrigger("AttackThree");
                attackStartedTime = Time.realtimeSinceStartup;
            }
        }
        else if (playerState == State.attackingThree)
        {
            GetInput();
            velocity = moveDirection * speed;
            if (Time.realtimeSinceStartup > attackStartedTime + attackThreeDuration)
            {
                playerState = State.freeToMove;
            }
        }
        else if (playerState == State.dashing)
        {
            velocity = lastValidMoveDir.normalized * dashForce;
            footprintsEmissionModule.rateOverDistance = 0f;
            if (dash)
            {
                dashEmission.rateOverDistance = 2f;
            }
            // TODO Set the dash animation
        }
        else if (playerState == State.knockedBack)
        {
            if (Time.realtimeSinceStartup <= knockBackStarted + knockBackDuration)
            {
                velocity = knockbackDir * ((knockBackStarted + knockBackDuration) - Time.realtimeSinceStartup) * Time.deltaTime;
            }
            else
            {
                playerState = State.freeToMove;
            }
            StartCoroutine(FlashSprite(0.1f));
        }
        transform.position += velocity * Time.deltaTime;
    }

    IEnumerator FlashSprite(float offtime)
    {
        rend.enabled = false;
        yield return new WaitForSeconds(offtime);
        rend.enabled = true;
    }

    private void CalculateOrderInLayer()
    {
        centerBottom = transform.TransformPoint(rend.sprite.bounds.min);

        layer = centerBottom.y + yOffset;

        rend.sortingOrder = -(int)(layer * 10);
    }

    private void OnDrawGizmos()
    {
        Debug.DrawLine(transform.position, transform.position + aimDirection);
    }

    public void GainExp(int expGain)
    {
        exp += expGain;
        if(OnExpChanged != null)
        {
            OnExpChanged(expToNextLevel, exp);
        }
    }

    void LevelUp()
    {
        attack += attackGainPerLevel;
        maxHealth += healthGainPerLevel;
        health = maxHealth;
        level++;
        exp = exp - expToNextLevel;
        expToNextLevel = (int)(Mathf.Pow(level, 2) * 2f);
        if(OnExpChanged != null)
        {
            OnExpChanged(expToNextLevel, exp);
        }
        if(OnHealthChanged != null)
        {
            OnHealthChanged(health, maxHealth);
        }
        if(OnLevelChanged != null)
        {
            OnLevelChanged(level);
        }
        // TODO call delegate to update level ui number
    }
    
    void GetInput()
    {
        moveDirection.x = input.Horizontal;
        if(moveDirection.x < 0f)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if(moveDirection.x > 0f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        moveDirection.y = input.Vertical;
        // Create the angle for the movement vector
        float moveAngle = Vector3.Angle(Vector3.up, lastValidMoveDir);
        if (moveDirection.x < 0f)
        {
            moveAngle = -moveAngle;
        }
        if (dash)
        {
            dash.transform.up = -lastValidMoveDir;
        }
        if (footprints)
        {
            footprintsShapeModule.rotation = new Vector3(0f, moveAngle, 0f);
            footprintsMainModule.startRotation = 0.0175f * moveAngle;
        }
        // Only overwrite lastValidMoveDir if the player is not standing still. To always dash in a direction
        if(!HelperMethods.V3Equal(moveDirection, Vector3.zero, 0.01f))
        {
            lastValidMoveDir = moveDirection;
        }
        if(GameManager.Instance.IsControllerInput)
        {
            if(!HelperMethods.V3Equal(moveDirection, Vector3.zero, 0.1f))
            {
                aimDirection = moveDirection;
            }
        }
        else
        {
            Vector3 mousePosInWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 targetAim = (mousePosInWorld - transform.position);
            aimDirection.x = targetAim.normalized.x;
            aimDirection.y = targetAim.normalized.y;
        }
        SetArrow();
        SetHitbox();
    }

    // Set the arrow position and rotation
    void SetArrow()
    {
        arrow.transform.position = transform.position + aimDirection.normalized;
        arrow.transform.rotation = Quaternion.FromToRotation(arrow.transform.up, aimDirection) * arrow.transform.rotation;
    }

    // Set the hitbox position and rotation
    void SetHitbox()
    {
        hitbox.transform.position = transform.position + aimDirection.normalized;
        hitbox.transform.rotation = Quaternion.FromToRotation(hitbox.transform.up, aimDirection) * hitbox.transform.rotation;
    }

    // Subtracts damage from the player health and knocks him back
    public void TakeDamage(int damage, Vector3 knockback, float time, float duration)
    {
        // Player only takes damage, if he isnt already knocked back
        if(playerState != State.knockedBack)
        {
            health -= damage;
            if (health <= 0)
            {
                Die();
            }
            knockbackDir = knockback;
            knockBackStarted = time;
            knockBackDuration = duration;
            playerState = State.knockedBack;
            if (OnHealthChanged != null)
            {
                OnHealthChanged(health, maxHealth);
            }
        }
    }

    void Die()
    {
        //TODO make the player die and open gameover menu
    }

    // Collects Items or Skills if player walks over them if nothing is equipped before
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<BaseItem>() && !playerItem)
        {
            collision.GetComponent<BaseItem>().Equipped = true;
            playerItem = collision.GetComponent<BaseItem>();
        }
        else if(collision.GetComponent<BaseSkill>() && !playerSkill)
        {
            collision.GetComponent<BaseSkill>().Equipped = true;
            playerSkill = collision.GetComponent<BaseSkill>();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.GetComponent<BaseItem>())
        {
            if (input.SwitchPowerup && playerItem)
            {
                playerItem.Equipped = false;
                collision.GetComponent<BaseItem>().Equipped = true;
                playerItem = collision.GetComponent<BaseItem>();
            }
        }
        else if (collision.GetComponent<BaseSkill>())
        {
            if(input.SwitchPowerup && playerSkill)
            {
                playerSkill.Equipped = false;
                collision.GetComponent<BaseSkill>().Equipped = true;
                playerSkill = collision.GetComponent<BaseSkill>();
            }
        }
    }
}
