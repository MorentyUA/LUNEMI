using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Настройки атаки")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private float pickaxeDamage = 20f;
    [SerializeField] private float knockbackForceEnemy = 5f;
    [SerializeField] private LayerMask damageableLayers;

    [Header("Гибридная атака киркой")]
    [Tooltip("Длина одного удара киркой. Поставь примерно длину твоей анимации удара.")]
    [SerializeField] private float singleTapAttackDuration = 0.45f;

    [Tooltip("Если игрок держит ЛКМ дольше этого времени — это уже считается зажатой атакой.")]
    [SerializeField] private float holdToLoopAttackTime = 0.18f;

    [Tooltip("Если true — при зажатии ЛКМ атака будет лупиться.")]
    [SerializeField] private bool allowHoldToAttackLoop = true;

    [Tooltip("Если true — если игрок держал ЛКМ и отпустил, атака сбрасывается сразу.")]
    [SerializeField] private bool cancelHeldAttackImmediatelyOnRelease = true;

    [Header("Смещения точки атаки (Offsets)")]
    [SerializeField] private Vector2 attackOffsetForward = new Vector2(0.8f, 0f);
    [SerializeField] private Vector2 attackOffsetUp = new Vector2(0f, 1.2f);
    [SerializeField] private Vector2 attackOffsetDown = new Vector2(0f, -1.2f);

    [Header("Проверка земли (Двойной Raycast)")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Расстояние между левым и правым лучом. Сделай меньше ширины персонажа (например, 0.35).")]
    [SerializeField] private float raycastSpacing = 0.35f;

    [Tooltip("Длина лучей, бьющих вниз.")]
    [SerializeField] private float rayDistance = 0.15f;

    [SerializeField] private LayerMask groundLayer;

    [Header("Умная проверка земли")]
    [Tooltip("Триггеры не будут считаться землёй. Включи, чтобы Pickup Trigger не ломал grounded.")]
    [SerializeField] private bool ignoreTriggerGroundColliders = true;

    [Tooltip("Минимальный normal.y для земли. 0.5 = поверхность должна смотреть вверх, а не быть боком.")]
    [SerializeField] private float minGroundNormalY = 0.5f;

    [Header("Партиклы пыли / Dust")]
    [SerializeField] private ParticleSystem landingDustPrefab;
    [SerializeField] private Transform landingDustPoint;
    [SerializeField] private float minLandingSpeedForDust = 1.5f;

    [Space(8)]
    [SerializeField] private ParticleSystem footstepDustPrefab;
    [SerializeField] private Transform[] footstepDustPoints;

    [Tooltip("Удалять созданные партиклы через это время. Если 0, время будет рассчитано автоматически.")]
    [SerializeField] private float dustDestroyDelay = 0f;

    [Tooltip("Показывать сообщения в Console при спавне пыли")]
    [SerializeField] private bool debugDust = true;

    private int currentFootstepPointIndex;
    private float lastAirYVelocity;
    private float lastJumpTime;

    [Header("Толкание / Push")]
    [SerializeField] private Transform pushCheck;
    [SerializeField] private LayerMask pushLayers;
    [SerializeField] private float pushCheckDistance = 0.45f;
    [SerializeField] private float pushCheckRadius = 0.12f;
    [SerializeField] private bool onlyPushWhenGrounded = true;

    [Header("Кнопка Push")]
    [Tooltip("0 = ЛКМ, 1 = ПКМ, 2 = средняя кнопка мыши")]
    [SerializeField] private int pushMouseButton = 1;

    [Header("Коллайдер при Push")]
    [SerializeField] private CapsuleCollider2D playerCapsuleCollider;

    [Tooltip("Насколько увеличить капсулу по ширине во время Push")]
    [SerializeField] private float pushColliderExtraWidth = 0.25f;

    [Tooltip("Насколько сдвинуть капсулу вперёд во время Push")]
    [SerializeField] private float pushColliderForwardOffset = 0.125f;

    [Header("Визуальные эффекты урона")]
    [SerializeField] private Color hurtColor = Color.red;
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private float hurtKnockbackPower = 4f;

    [Header("Кнопки осмотра")]
    [SerializeField] private KeyCode lookUpKey = KeyCode.W;
    [SerializeField] private KeyCode lookDownKey = KeyCode.S;

    [Header("Хватание за уступ / Ledge Grab")]
    [SerializeField] private bool enableLedgeGrab = true;

    [Tooltip("Слой уступов. Обычно сюда ставим тот же слой, что и земля/стены.")]
    [SerializeField] private LayerMask ledgeLayer;

    [Tooltip("Нижняя точка проверки: примерно уровень груди/рук.")]
    [SerializeField] private Transform ledgeLowerCheck;

    [Tooltip("Верхняя точка проверки: выше нижней точки. Тут должно быть пусто.")]
    [SerializeField] private Transform ledgeUpperCheck;

    [Tooltip("Насколько далеко перед игроком искать стену/уступ.")]
    [SerializeField] private float ledgeCheckDistance = 0.35f;

    [Tooltip("Радиус проверочных кружков для уступа.")]
    [SerializeField] private float ledgeCheckRadius = 0.08f;

    [Tooltip("Хвататься только если игрок нажимает в сторону стены.")]
    [SerializeField] private bool requireInputTowardLedge = true;

    [Tooltip("Игрок может хвататься, только когда падает или почти не летит вверх.")]
    [SerializeField] private float maxVerticalSpeedForLedgeGrab = 0.2f;

    [Tooltip("Кулдаун после взбирания, чтобы не цепляться за тот же уступ сразу.")]
    [SerializeField] private float ledgeGrabCooldown = 0.18f;

    [Tooltip("Если true — при захвате игрок чуть сдвинется в позицию висения.")]
    [SerializeField] private bool snapOnLedgeGrab = false;

    [Tooltip("Смещение при захвате. X автоматически зеркалится.")]
    [SerializeField] private Vector2 ledgeHangSnapOffset = new Vector2(0f, 0f);

    [Tooltip("Запасное смещение после взбирания, если скрипт не смог определить Collider уступа. X автоматически зеркалится.")]
    [SerializeField] private Vector2 ledgeClimbFinishOffset = new Vector2(0.45f, 1.05f);

    [Tooltip("Если в анимацию не добавлен Animation Event, скрипт сам завершит climb через это время.")]
    [SerializeField] private bool useLedgeClimbFallbackTimer = true;

    [SerializeField] private float ledgeClimbFallbackTime = 0.55f;

    [Header("Плавное завершение взбирания")]
    [SerializeField] private bool smoothLedgeClimbFinish = true;

    [Tooltip("Длительность плавного перемещения игрока на блок после анимации.")]
    [SerializeField] private float ledgeClimbMoveDuration = 0.16f;

    [Tooltip("Насколько далеко от края блока поставить игрока внутрь платформы.")]
    [SerializeField] private float ledgeStandInsetFromEdge = 0.18f;

    [Tooltip("Дополнительная поправка по Y, если игрок стоит чуть выше/ниже после взбирания.")]
    [SerializeField] private float ledgeStandYOffset = 0.02f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private float horizontalInput;
    private float verticalInput;

    private bool isGrounded;
    private bool isFacingRight = true;
    private bool isMining;
    private bool isHurt;
    private bool canControl = true;
    private bool canBounce = true;

    private bool isPushing;
    private bool isPushWalking;

    private bool isLedgeHanging;
    private float savedGravityScale;
    private float lastLedgeGrabTime = -999f;
    private Coroutine ledgeClimbFallbackRoutine;
    private Coroutine ledgeSmoothFinishRoutine;
    private Collider2D grabbedLedgeCollider;
    private int grabbedLedgeDirection;

    private bool attackButtonHeld;
    private bool attackIsLoopingByHold;
    private bool shortTapWaitingToFinish;
    private float attackButtonDownTime;
    private int lockedAttackDir;
    private Coroutine shortTapStopRoutine;

    private Vector2 originalCapsuleSize;
    private Vector2 originalCapsuleOffset;
    private bool capsuleDataSaved;

    private Color originalSpriteColor;

    private readonly int WalkHash = Animator.StringToHash("walk");
    private readonly int PickaxeHash = Animator.StringToHash("pickaxe");
    private readonly int JumpHash = Animator.StringToHash("jump");
    private readonly int FallingHash = Animator.StringToHash("falling");
    private readonly int NearGroundHash = Animator.StringToHash("nearGround");
    private readonly int HurtHash = Animator.StringToHash("hurt");
    private readonly int AttackDirHash = Animator.StringToHash("attackDir");

    private readonly int PushHash = Animator.StringToHash("push");
    private readonly int PushWalkHash = Animator.StringToHash("push walk");

    private readonly int LookUpHash = Animator.StringToHash("lookup");
    private readonly int LookDownHash = Animator.StringToHash("lookdown");

    private readonly int HugHash = Animator.StringToHash("hug");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        originalSpriteColor = spriteRenderer.color;

        if (playerCapsuleCollider == null)
            playerCapsuleCollider = GetComponent<CapsuleCollider2D>();

        SaveOriginalCapsuleData();

        if (ledgeLayer.value == 0)
            ledgeLayer = groundLayer;
    }

    private void Update()
    {
        if (!canControl || isHurt)
        {
            ResetInputs();
            return;
        }

        CheckGroundStatus();

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (isLedgeHanging)
        {
            rb.linearVelocity = Vector2.zero;
            SetBasicAirAnimationsOff();
            return;
        }

        TryStartLedgeGrab();

        if (isLedgeHanging)
            return;

        UpdatePushState();

        if (Input.GetButtonDown("Jump") && isGrounded && !isMining && !isPushing && !isLedgeHanging)
        {
            ResetLookParameters();
            Jump();
        }

        HandleAttackInput();

        if (isMining)
        {
            ApplyLockedAttackDirection();
        }

        HandleFlip();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!canControl || isHurt)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Lerp(rb.linearVelocity.x, 0f, 0.1f),
                rb.linearVelocity.y
            );

            return;
        }

        if (isLedgeHanging)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isMining)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void HandleAttackInput()
    {
        if (isPushing || isLedgeHanging)
        {
            StopMining();
            return;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            attackButtonHeld = true;
            attackIsLoopingByHold = false;
            shortTapWaitingToFinish = false;
            attackButtonDownTime = Time.time;

            StopShortTapRoutine();

            if (!isMining)
            {
                ResetLookParameters();
                StartAttack();
            }
        }

        if (Input.GetButton("Fire1") && isMining)
        {
            float heldTime = Time.time - attackButtonDownTime;

            if (allowHoldToAttackLoop && heldTime >= holdToLoopAttackTime)
            {
                attackIsLoopingByHold = true;
                shortTapWaitingToFinish = false;
                StopShortTapRoutine();
            }

            if (attackIsLoopingByHold)
            {
                LockAttackDirection();
            }
        }

        if (Input.GetButtonUp("Fire1"))
        {
            attackButtonHeld = false;

            float heldTime = Time.time - attackButtonDownTime;
            bool wasRealHold = heldTime >= holdToLoopAttackTime;

            if (wasRealHold && attackIsLoopingByHold)
            {
                if (cancelHeldAttackImmediatelyOnRelease)
                {
                    StopMining();
                }
                else
                {
                    shortTapWaitingToFinish = true;
                    StartShortTapStopRoutine();
                }

                return;
            }

            shortTapWaitingToFinish = true;
            StartShortTapStopRoutine();
        }
    }

    private void StartAttack()
    {
        if (!canControl || isHurt || isPushing || isLedgeHanging)
            return;

        if (isMining)
            return;

        isMining = true;

        SetPushState(false, false);

        LockAttackDirection();
        ApplyLockedAttackDirection();

        anim.SetBool(PickaxeHash, true);
    }

    private void LockAttackDirection()
    {
        if (verticalInput > 0.1f)
        {
            lockedAttackDir = 1;
        }
        else if (verticalInput < -0.1f)
        {
            lockedAttackDir = 2;
        }
        else
        {
            lockedAttackDir = 0;
        }
    }

    private void ApplyLockedAttackDirection()
    {
        if (attackPoint == null)
            return;

        if (lockedAttackDir == 1)
        {
            anim.SetInteger(AttackDirHash, 1);
            attackPoint.localPosition = attackOffsetUp;
        }
        else if (lockedAttackDir == 2)
        {
            anim.SetInteger(AttackDirHash, 2);
            attackPoint.localPosition = attackOffsetDown;
        }
        else
        {
            anim.SetInteger(AttackDirHash, 0);
            attackPoint.localPosition = attackOffsetForward;
        }
    }

    private void StopMining()
    {
        isMining = false;
        attackButtonHeld = false;
        attackIsLoopingByHold = false;
        shortTapWaitingToFinish = false;
        lockedAttackDir = 0;

        StopShortTapRoutine();

        anim.SetBool(PickaxeHash, false);
        anim.SetInteger(AttackDirHash, 0);
    }

    private void StartShortTapStopRoutine()
    {
        StopShortTapRoutine();
        shortTapStopRoutine = StartCoroutine(ShortTapStopRoutine());
    }

    private void StopShortTapRoutine()
    {
        if (shortTapStopRoutine != null)
        {
            StopCoroutine(shortTapStopRoutine);
            shortTapStopRoutine = null;
        }
    }

    private IEnumerator ShortTapStopRoutine()
    {
        yield return new WaitForSeconds(singleTapAttackDuration);

        shortTapStopRoutine = null;

        if (shortTapWaitingToFinish && !attackButtonHeld && !attackIsLoopingByHold)
        {
            StopMining();
        }
    }

    public void AnimationEvent_PickaxeHit()
    {
        if (isHurt || !canControl)
            return;

        if (!isMining)
            return;

        if (attackPoint == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRadius,
            damageableLayers
        );

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHP = hit.GetComponent<EnemyHealth>();

            if (enemyHP != null)
            {
                Vector2 knockDir = (hit.transform.position - transform.position).normalized;

                enemyHP.TakeDamage(
                    pickaxeDamage,
                    new Vector2(Mathf.Sign(knockDir.x), 0.2f) * knockbackForceEnemy
                );

                continue;
            }

            BreakableBlock block = hit.GetComponent<BreakableBlock>();

            if (block != null)
            {
                block.TakeDamage(pickaxeDamage);
            }
        }
    }

    public void AnimationEvent_PickaxeAttackFinished()
    {
        if (!isMining)
            return;

        if (isHurt || !canControl || isPushing || isLedgeHanging)
        {
            StopMining();
            return;
        }

        if (attackButtonHeld && attackIsLoopingByHold && allowHoldToAttackLoop)
        {
            LockAttackDirection();
            ApplyLockedAttackDirection();
            return;
        }

        if (shortTapWaitingToFinish || !attackButtonHeld)
        {
            StopMining();
        }
    }

    private void UpdatePushState()
    {
        if (isMining || isHurt || !canControl || isLedgeHanging)
        {
            SetPushState(false, false);
            return;
        }

        if (onlyPushWhenGrounded && !isGrounded)
        {
            SetPushState(false, false);
            return;
        }

        bool holdingPushButton = Input.GetMouseButton(pushMouseButton);

        if (!holdingPushButton)
        {
            SetPushState(false, false);
            return;
        }

        bool wallInFront = CheckWallInFront();

        if (!wallInFront)
        {
            SetPushState(false, false);
            return;
        }

        bool pressingIntoWall = IsPressingIntoFacingDirection();

        SetPushState(true, pressingIntoWall);
    }

    private bool CheckWallInFront()
    {
        Vector2 origin = pushCheck != null ? pushCheck.position : transform.position;
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;

        Vector2 checkPosition = origin + direction * pushCheckDistance;

        Collider2D hit = Physics2D.OverlapCircle(
            checkPosition,
            pushCheckRadius,
            pushLayers
        );

        return hit != null;
    }

    private bool IsPressingIntoFacingDirection()
    {
        if (isFacingRight && horizontalInput > 0.1f)
            return true;

        if (!isFacingRight && horizontalInput < -0.1f)
            return true;

        return false;
    }

    private void SetPushState(bool push, bool pushWalk)
    {
        isPushing = push;
        isPushWalking = pushWalk;

        anim.SetBool(PushHash, isPushing);
        anim.SetBool(PushWalkHash, isPushWalking);

        ApplyPushCollider(isPushing || isPushWalking);
    }

    private void SaveOriginalCapsuleData()
    {
        if (playerCapsuleCollider == null)
            return;

        originalCapsuleSize = playerCapsuleCollider.size;
        originalCapsuleOffset = playerCapsuleCollider.offset;
        capsuleDataSaved = true;
    }

    private void ApplyPushCollider(bool enablePushCollider)
    {
        if (playerCapsuleCollider == null)
            return;

        if (!capsuleDataSaved)
            SaveOriginalCapsuleData();

        if (enablePushCollider)
        {
            Vector2 newSize = originalCapsuleSize;
            newSize.x = originalCapsuleSize.x + pushColliderExtraWidth;

            Vector2 newOffset = originalCapsuleOffset;
            float pushDirection = isFacingRight ? 1f : -1f;
            newOffset.x = originalCapsuleOffset.x + (pushColliderForwardOffset * pushDirection);

            playerCapsuleCollider.size = newSize;
            playerCapsuleCollider.offset = newOffset;
        }
        else
        {
            playerCapsuleCollider.size = originalCapsuleSize;
            playerCapsuleCollider.offset = originalCapsuleOffset;
        }
    }

    private void TryStartLedgeGrab()
    {
        if (!enableLedgeGrab)
            return;

        if (isLedgeHanging || isGrounded || isMining || isPushing || isHurt || !canControl)
            return;

        if (Time.time - lastLedgeGrabTime < ledgeGrabCooldown)
            return;

        if (rb.linearVelocity.y > maxVerticalSpeedForLedgeGrab)
            return;

        if (requireInputTowardLedge && !IsPressingIntoFacingDirection())
            return;

        if (ledgeLowerCheck == null || ledgeUpperCheck == null)
            return;

        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;

        Vector2 lowerCheckPosition = (Vector2)ledgeLowerCheck.position + direction * ledgeCheckDistance;
        Vector2 upperCheckPosition = (Vector2)ledgeUpperCheck.position + direction * ledgeCheckDistance;

        Collider2D wallAtLowerCheck = Physics2D.OverlapCircle(
            lowerCheckPosition,
            ledgeCheckRadius,
            ledgeLayer
        );

        Collider2D wallAtUpperCheck = Physics2D.OverlapCircle(
            upperCheckPosition,
            ledgeCheckRadius,
            ledgeLayer
        );

        bool hasWallForHands = wallAtLowerCheck != null;
        bool hasFreeSpaceAboveHands = wallAtUpperCheck == null;

        if (!hasWallForHands || !hasFreeSpaceAboveHands)
            return;

        grabbedLedgeCollider = wallAtLowerCheck;
        grabbedLedgeDirection = isFacingRight ? 1 : -1;

        StartLedgeGrab();
    }

    private void StartLedgeGrab()
    {
        isLedgeHanging = true;
        lastLedgeGrabTime = Time.time;

        StopMining();
        SetPushState(false, false);
        ResetLookParameters();

        savedGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        SetBasicAirAnimationsOff();

        if (snapOnLedgeGrab)
        {
            float direction = isFacingRight ? 1f : -1f;
            transform.position += new Vector3(ledgeHangSnapOffset.x * direction, ledgeHangSnapOffset.y, 0f);
        }

        anim.ResetTrigger(JumpHash);
        anim.SetTrigger(HugHash);

        StartLedgeFallbackTimer();
    }

    private void StartLedgeFallbackTimer()
    {
        StopLedgeFallbackTimer();

        if (useLedgeClimbFallbackTimer && ledgeClimbFallbackTime > 0f)
        {
            ledgeClimbFallbackRoutine = StartCoroutine(LedgeClimbFallbackRoutine());
        }
    }

    private void StopLedgeFallbackTimer()
    {
        if (ledgeClimbFallbackRoutine != null)
        {
            StopCoroutine(ledgeClimbFallbackRoutine);
            ledgeClimbFallbackRoutine = null;
        }
    }

    private IEnumerator LedgeClimbFallbackRoutine()
    {
        yield return new WaitForSeconds(ledgeClimbFallbackTime);

        ledgeClimbFallbackRoutine = null;
        FinishLedgeClimb();
    }

    public void AnimationEvent_LedgeClimbFinished()
    {
        FinishLedgeClimb();
    }

    public void AnimationEvent_HugFinished()
    {
        FinishLedgeClimb();
    }

    private void FinishLedgeClimb()
    {
        if (!isLedgeHanging)
            return;

        StopLedgeFallbackTimer();

        if (ledgeSmoothFinishRoutine != null)
        {
            StopCoroutine(ledgeSmoothFinishRoutine);
            ledgeSmoothFinishRoutine = null;
        }

        if (smoothLedgeClimbFinish)
        {
            ledgeSmoothFinishRoutine = StartCoroutine(SmoothFinishLedgeClimbRoutine());
        }
        else
        {
            Vector3 targetPosition = CalculateLedgeStandPosition();
            CompleteLedgeClimbAtPosition(targetPosition);
        }
    }

    private IEnumerator SmoothFinishLedgeClimbRoutine()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = CalculateLedgeStandPosition();

        float timer = 0f;
        float duration = Mathf.Max(0.01f, ledgeClimbMoveDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;
            t = Mathf.Clamp01(t);

            t = t * t * (3f - 2f * t);

            Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, t);

            rb.MovePosition(nextPosition);

            yield return null;
        }

        CompleteLedgeClimbAtPosition(targetPosition);

        ledgeSmoothFinishRoutine = null;
    }

    private Vector3 CalculateLedgeStandPosition()
    {
        float direction = grabbedLedgeDirection != 0 ? grabbedLedgeDirection : (isFacingRight ? 1f : -1f);

        Vector3 fallbackPosition = transform.position + new Vector3(
            ledgeClimbFinishOffset.x * direction,
            ledgeClimbFinishOffset.y,
            0f
        );

        if (grabbedLedgeCollider == null || playerCapsuleCollider == null)
            return fallbackPosition;

        Bounds ledgeBounds = grabbedLedgeCollider.bounds;

        float playerHalfHeight = playerCapsuleCollider.bounds.extents.y;
        float playerHalfWidth = playerCapsuleCollider.bounds.extents.x;

        float targetY = ledgeBounds.max.y + playerHalfHeight + ledgeStandYOffset;

        float targetX;

        if (direction > 0f)
        {
            targetX = ledgeBounds.min.x + playerHalfWidth + ledgeStandInsetFromEdge;
        }
        else
        {
            targetX = ledgeBounds.max.x - playerHalfWidth - ledgeStandInsetFromEdge;
        }

        return new Vector3(targetX, targetY, transform.position.z);
    }

    private void CompleteLedgeClimbAtPosition(Vector3 targetPosition)
    {
        isLedgeHanging = false;
        lastLedgeGrabTime = Time.time;

        rb.gravityScale = savedGravityScale;
        rb.linearVelocity = Vector2.zero;

        rb.position = targetPosition;

        lastJumpTime = Time.time;
        isGrounded = false;

        grabbedLedgeCollider = null;
        grabbedLedgeDirection = 0;

        SetBasicAirAnimationsOff();
    }

    private void CancelLedgeGrab()
    {
        if (!isLedgeHanging)
            return;

        StopLedgeFallbackTimer();

        if (ledgeSmoothFinishRoutine != null)
        {
            StopCoroutine(ledgeSmoothFinishRoutine);
            ledgeSmoothFinishRoutine = null;
        }

        isLedgeHanging = false;
        lastLedgeGrabTime = Time.time;

        rb.gravityScale = savedGravityScale;
        rb.linearVelocity = Vector2.zero;

        grabbedLedgeCollider = null;
        grabbedLedgeDirection = 0;

        SetBasicAirAnimationsOff();
    }

    private void SetBasicAirAnimationsOff()
    {
        anim.SetBool(WalkHash, false);
        anim.SetBool(FallingHash, false);
        anim.SetBool(NearGroundHash, false);
        anim.SetBool(PushHash, false);
        anim.SetBool(PushWalkHash, false);
    }

    public bool TryBounce()
    {
        if (canBounce)
        {
            canBounce = false;
            return true;
        }

        return false;
    }

    public void TakeSelfDamage(float damage, Vector2 attackerPos)
    {
        PlayerHealth hp = GetComponent<PlayerHealth>();

        if (hp != null)
        {
            hp.TakeDamage(damage, attackerPos);
        }
    }

    private void Jump()
    {
        lastJumpTime = Time.time;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        anim.SetTrigger(JumpHash);
    }

    private void CheckGroundStatus()
    {
        if (groundCheck == null)
            return;

        if (isLedgeHanging)
        {
            isGrounded = false;
            anim.SetBool(NearGroundHash, false);
            return;
        }

        if (Time.time - lastJumpTime < 0.1f)
        {
            isGrounded = false;
            anim.SetBool(NearGroundHash, false);
            return;
        }

        bool wasGrounded = isGrounded;

        Vector3 origin = groundCheck.position;
        Vector3 leftRayOrigin = origin + Vector3.left * (raycastSpacing * 0.5f);
        Vector3 rightRayOrigin = origin + Vector3.right * (raycastSpacing * 0.5f);

        bool groundedNow =
            HasGroundBelow(leftRayOrigin) ||
            HasGroundBelow(rightRayOrigin);

        if (!wasGrounded && groundedNow)
        {
            anim.ResetTrigger(JumpHash);

            if (debugDust)
            {
                Debug.Log("Игрок приземлился. lastAirYVelocity = " + lastAirYVelocity);
            }

            if (lastAirYVelocity <= -minLandingSpeedForDust)
            {
                PlayLandingDust();
            }
        }

        isGrounded = groundedNow;

        if (!isGrounded)
        {
            lastAirYVelocity = rb.linearVelocity.y;
        }

        anim.SetBool(NearGroundHash, isGrounded);

        if (isGrounded)
        {
            canBounce = true;
        }
    }

    private bool HasGroundBelow(Vector2 origin)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            Vector2.down,
            rayDistance,
            groundLayer
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (ignoreTriggerGroundColliders && hit.collider.isTrigger)
                continue;

            if (hit.normal.y < minGroundNormalY)
                continue;

            return true;
        }

        return false;
    }

    private void PlayLandingDust()
    {
        if (landingDustPrefab == null)
        {
            if (debugDust)
            {
                Debug.LogWarning("Landing Dust Prefab не назначен в PlayerController!");
            }

            return;
        }

        Transform point = landingDustPoint != null ? landingDustPoint : transform;

        SpawnDust(landingDustPrefab, point.position, "Landing Dust");
    }

    public void AnimationEvent_FootstepDust()
    {
        if (!isGrounded)
        {
            if (debugDust)
            {
                Debug.Log("Footstep Dust не появился: игрок не на земле.");
            }

            return;
        }

        if (footstepDustPrefab == null)
        {
            if (debugDust)
            {
                Debug.LogWarning("Footstep Dust Prefab не назначен в PlayerController!");
            }

            return;
        }

        Transform point = transform;

        if (footstepDustPoints != null && footstepDustPoints.Length > 0)
        {
            Transform selectedPoint = footstepDustPoints[currentFootstepPointIndex];

            if (selectedPoint != null)
            {
                point = selectedPoint;
            }

            currentFootstepPointIndex++;

            if (currentFootstepPointIndex >= footstepDustPoints.Length)
            {
                currentFootstepPointIndex = 0;
            }
        }

        SpawnDust(footstepDustPrefab, point.position, "Footstep Dust");
    }

    private void SpawnDust(ParticleSystem prefab, Vector3 position, string dustName)
    {
        if (prefab == null)
            return;

        ParticleSystem dust = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        dust.gameObject.SetActive(true);

        dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        dust.Play(true);

        if (debugDust)
        {
            Debug.Log(dustName + " spawned at " + position);
        }

        float destroyTime = dustDestroyDelay;

        if (destroyTime <= 0f)
        {
            ParticleSystem.MainModule main = dust.main;

            float maxLifetime = 1f;

            if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            {
                maxLifetime = main.startLifetime.constant;
            }
            else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            {
                maxLifetime = main.startLifetime.constantMax;
            }
            else
            {
                maxLifetime = 2f;
            }

            destroyTime = main.duration + maxLifetime + 0.5f;
        }

        Destroy(dust.gameObject, destroyTime);
    }

    public void GetHit(float freezeTime, Vector2 attackerPos)
    {
        if (isHurt || !canControl)
            return;

        CancelLedgeGrab();
        ResetLookParameters();
        StartCoroutine(HurtStunRoutine(freezeTime, attackerPos));
    }

    private IEnumerator HurtStunRoutine(float duration, Vector2 attackerPos)
    {
        isHurt = true;

        StopMining();
        ResetInputs();

        anim.SetTrigger(HurtHash);

        float knockbackDir = transform.position.x > attackerPos.x ? 1f : -1f;

        rb.linearVelocity = new Vector2(
            knockbackDir * hurtKnockbackPower,
            4f
        );

        float timePerBlink = duration / (blinkCount * 2);

        for (int i = 0; i < blinkCount; i++)
        {
            spriteRenderer.color = hurtColor;
            yield return new WaitForSeconds(timePerBlink);

            spriteRenderer.color = new Color(
                originalSpriteColor.r,
                originalSpriteColor.g,
                originalSpriteColor.b,
                0.3f
            );

            yield return new WaitForSeconds(timePerBlink);
        }

        spriteRenderer.color = originalSpriteColor;
        isHurt = false;
    }

    private void HandleFlip()
    {
        if (isMining || isHurt || isPushing || isLedgeHanging)
            return;

        if (horizontalInput > 0f && !isFacingRight)
        {
            Flip();
        }
        else if (horizontalInput < 0f && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1f;
        transform.localScale = currentScale;
    }

    private void UpdateAnimations()
    {
        if (isLedgeHanging)
        {
            SetBasicAirAnimationsOff();
            ResetLookParameters();
            return;
        }

        bool walking =
            Mathf.Abs(horizontalInput) > 0.1f &&
            isGrounded &&
            !isMining &&
            !isPushing &&
            !isPushWalking;

        anim.SetBool(WalkHash, walking);

        bool falling = !isGrounded && rb.linearVelocity.y < -0.1f;
        anim.SetBool(FallingHash, falling);

        bool isCurrentlyIdle = isGrounded && !walking && !isMining && !isPushing;

        if (isCurrentlyIdle)
        {
            bool pressingUp = Input.GetKey(lookUpKey);
            bool pressingDown = Input.GetKey(lookDownKey);

            if (pressingUp && pressingDown)
            {
                anim.SetBool(LookUpHash, false);
                anim.SetBool(LookDownHash, false);
            }
            else
            {
                anim.SetBool(LookUpHash, pressingUp);
                anim.SetBool(LookDownHash, pressingDown);
            }
        }
        else
        {
            ResetLookParameters();
        }
    }

    private void ResetLookParameters()
    {
        if (anim != null)
        {
            anim.SetBool(LookUpHash, false);
            anim.SetBool(LookDownHash, false);
        }
    }

    private void ResetInputs()
    {
        horizontalInput = 0f;
        verticalInput = 0f;

        isMining = false;
        attackButtonHeld = false;
        attackIsLoopingByHold = false;
        shortTapWaitingToFinish = false;
        lockedAttackDir = 0;

        StopShortTapRoutine();
        StopLedgeFallbackTimer();

        if (ledgeSmoothFinishRoutine != null)
        {
            StopCoroutine(ledgeSmoothFinishRoutine);
            ledgeSmoothFinishRoutine = null;
        }

        if (isLedgeHanging)
        {
            isLedgeHanging = false;
            rb.gravityScale = savedGravityScale;
        }

        grabbedLedgeCollider = null;
        grabbedLedgeDirection = 0;

        SetPushState(false, false);
        ResetLookParameters();

        anim.ResetTrigger(JumpHash);

        anim.SetBool(WalkHash, false);
        anim.SetBool(PickaxeHash, false);
        anim.SetInteger(AttackDirHash, 0);
    }

    public void DisableControl()
    {
        canControl = false;

        StopMining();
        ResetInputs();

        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = originalSpriteColor;
    }

    public void EnableControl()
    {
        canControl = true;

        ResetInputs();

        spriteRenderer.color = originalSpriteColor;
    }

    public bool CanControl()
    {
        return canControl;
    }

    public bool IsFacingRight()
    {
        return isFacingRight;
    }

    public bool IsPushing()
    {
        return isPushing;
    }

    public bool IsPushWalking()
    {
        return isPushWalking;
    }

    public bool IsLedgeHanging()
    {
        return isLedgeHanging;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        if (groundCheck != null)
        {
            Vector3 origin = groundCheck.position;
            Vector3 leftRayOrigin = origin + Vector3.left * (raycastSpacing * 0.5f);
            Vector3 rightRayOrigin = origin + Vector3.right * (raycastSpacing * 0.5f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(leftRayOrigin, leftRayOrigin + Vector3.down * rayDistance);
            Gizmos.DrawLine(rightRayOrigin, rightRayOrigin + Vector3.down * rayDistance);

            Gizmos.DrawSphere(leftRayOrigin, 0.02f);
            Gizmos.DrawSphere(rightRayOrigin, 0.02f);
        }

        Vector2 originPush = pushCheck != null ? pushCheck.position : transform.position;
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 checkPosition = originPush + direction * pushCheckDistance;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(checkPosition, pushCheckRadius);
        Gizmos.DrawLine(originPush, checkPosition);

        if (ledgeLowerCheck != null && ledgeUpperCheck != null)
        {
            Vector2 ledgeDirection = isFacingRight ? Vector2.right : Vector2.left;

            Vector2 lowerCheckPosition = (Vector2)ledgeLowerCheck.position + ledgeDirection * ledgeCheckDistance;
            Vector2 upperCheckPosition = (Vector2)ledgeUpperCheck.position + ledgeDirection * ledgeCheckDistance;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lowerCheckPosition, ledgeCheckRadius);
            Gizmos.DrawLine(ledgeLowerCheck.position, lowerCheckPosition);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(upperCheckPosition, ledgeCheckRadius);
            Gizmos.DrawLine(ledgeUpperCheck.position, upperCheckPosition);
        }

        if (landingDustPoint != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(landingDustPoint.position, 0.08f);
        }

        if (footstepDustPoints != null)
        {
            Gizmos.color = Color.white;

            foreach (Transform point in footstepDustPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.06f);
                }
            }
        }
    }
}
