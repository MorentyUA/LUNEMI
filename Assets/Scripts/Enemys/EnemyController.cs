using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float patrolDistance = 4f;

    [Header("Настройки прыжка NPC")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpForwardBoost = 3.5f;
    [SerializeField] private float jumpCooldown = 0.8f;

    [Tooltip("Радиус проверки земли под NPC")]
    [SerializeField] private float groundCheckRadius = 0.18f;

    [Tooltip("Насколько высоко проверяем свободное место над препятствием")]
    [SerializeField] private float jumpClearanceHeight = 1.2f;

    [Tooltip("Насколько далеко вперед проверяем место приземления после прыжка")]
    [SerializeField] private float landingCheckDistance = 1.2f;

    [Tooltip("Насколько вниз ищем землю после препятствия")]
    [SerializeField] private float landingGroundRayDistance = 2.5f;

    [Header("Фикс прыжковой анимации")]
    [Tooltip("Сколько секунд после прыжка NPC игнорирует groundCheck")]
    [SerializeField] private float ignoreGroundAfterJumpTime = 0.18f;

    [Tooltip("Через сколько секунд после прыжка можно принудительно включить falling")]
    [SerializeField] private float forceFallingAfterJumpTime = 0.35f;

    private float ignoreGroundUntilTime;
    private float jumpStartedTime;
    private bool jumpAnimationActive;

    [Header("Безопасное спрыгивание")]
    [Tooltip("Сколько NPC проверяет вниз перед собой для безопасного спуска")]
    [SerializeField] private float safeDropRayDistance = 3.0f;

    [Tooltip("Если земля ниже не дальше этого расстояния — NPC может спрыгнуть. Для 2 блоков ставь 2.2 - 2.8")]
    [SerializeField] private float maxSafeDropHeight = 2.45f;

    [Tooltip("Скорость движения, когда NPC аккуратно спрыгивает с края")]
    [SerializeField] private float dropMoveSpeedMultiplier = 0.85f;

    [Header("Умная позиция атаки")]
    [Tooltip("На каком расстоянии сбоку от игрока NPC хочет стоять для атаки")]
    [SerializeField] private float sideAttackOffset = 0.9f;

    [Tooltip("Если игрок почти под NPC по X, NPC не будет прыгать ему на голову")]
    [SerializeField] private float playerUnderXThreshold = 0.75f;

    [Tooltip("Насколько ниже должен быть игрок, чтобы считаться под NPC")]
    [SerializeField] private float playerBelowThreshold = 0.35f;

    [Tooltip("Максимальная разница по высоте для обычной атаки сбоку")]
    [SerializeField] private float maxAttackHeightDifference = 0.8f;

    [Header("Ожидание приземления игрока")]
    [Tooltip("Если игрок в воздухе рядом/над NPC, NPC ждёт и не бежит под него")]
    [SerializeField] private bool waitForPlayerLanding = true;

    [Tooltip("По X насколько близко игрок должен быть, чтобы NPC ждал его приземления")]
    [SerializeField] private float waitPlayerLandingXRange = 1.25f;

    [Tooltip("Если игрок выше NPC хотя бы на это значение и в воздухе — NPC ждёт")]
    [SerializeField] private float playerAboveWaitThreshold = 0.35f;

    [Tooltip("Луч вниз от игрока. Если земли рядом нет — считаем, что игрок в воздухе")]
    [SerializeField] private float playerGroundRayDistance = 0.85f;

    [Tooltip("Насколько долго NPC может стоять и ждать, чтобы не зависнуть навсегда")]
    [SerializeField] private float maxWaitForPlayerLandingTime = 1.2f;

    private float waitForLandingTimer;

    [Header("Боевые настройки")]
    [SerializeField] private float agroRange = 5f;
    [SerializeField] private float closeAgroRange = 1.5f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float damageValue = 15f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.6f;
    [SerializeField] private float headBounceForce = 12f;

    [Header("Память преследования")]
    [Tooltip("Сколько секунд NPC продолжает преследовать игрока, если тот спрятался за стену")]
    [SerializeField] private float chaseMemoryTime = 2.5f;

    [Tooltip("Если true, во время памяти NPC может преследовать игрока через стены, но только в agroRange")]
    [SerializeField] private bool chaseThroughWallsDuringMemory = true;

    private float chaseMemoryTimer;

    [Header("Аварийный выход из прыжка")]
    [Tooltip("Через сколько секунд после потери цели в воздухе NPC принудительно включает falling")]
    [SerializeField] private float maxJumpStateTimeAfterLostTarget = 0.8f;

    private float lostTargetAirTimer;

    [Header("Слои и маски")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Точки проверки")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform pitCheck;

    [Tooltip("Поставь эту точку строго под ногами NPC. НЕ спереди.")]
    [SerializeField] private Transform groundCheck;

    [Header("Фикс зависания анимации")]
    [Tooltip("Если NPC приземлился, принудительно сбрасываем jump/falling/run")]
    [SerializeField] private bool forceResetAirStateOnLanding = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform targetPlayer;

    private Vector2 startPoint;

    private bool isFacingRight = true;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isWaiting = false;
    private bool isGrounded = false;
    private bool wasGroundedLastFrame = false;
    private bool isJumpingObstacle = false;
    private bool lostTargetWhileAirborne = false;

    private float lastJumpTime = -999f;

    private readonly int RunHash = Animator.StringToHash("run");
    private readonly int AttackHash = Animator.StringToHash("attack");
    private readonly int DeadTriggerHash = Animator.StringToHash("dead");

    private readonly int JumpHash = Animator.StringToHash("jump");
    private readonly int NearGroundHash = Animator.StringToHash("nearGround");
    private readonly int FallingHash = Animator.StringToHash("falling");

    // Новый параметр Animator
    private readonly int AgroHash = Animator.StringToHash("agro");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        startPoint = transform.position;

        SetAgro(false);
    }

    private void Update()
    {
        wasGroundedLastFrame = isGrounded;

        CheckGroundStatus();
        UpdateAirAnimations();
        ResetAirStateOnLanding();
        HandleLostTargetAirState();

        if (isDead || isAttacking)
        {
            return;
        }

        bool canSeePlayerNormally = FindTargetWithLineOfSight();

        if (canSeePlayerNormally)
        {
            chaseMemoryTimer = chaseMemoryTime;
            SetAgro(true);
        }

        if (targetPlayer != null)
        {
            PlayerHealth playerHP = targetPlayer.GetComponent<PlayerHealth>();

            if (playerHP != null && playerHP.IsDead())
            {
                LoseTarget();
                return;
            }

            bool targetInsideAgroRange = Vector2.Distance(transform.position, targetPlayer.position) <= agroRange;

            bool canUseMemory =
                chaseThroughWallsDuringMemory &&
                targetInsideAgroRange &&
                chaseMemoryTimer > 0f;

            if (canSeePlayerNormally || canUseMemory)
            {
                SetAgro(true);

                if (!canSeePlayerNormally)
                {
                    chaseMemoryTimer -= Time.deltaTime;
                }

                if (ShouldWaitForPlayerLanding())
                {
                    StopMoving();
                    anim.SetBool(RunHash, false);
                    return;
                }

                if (CanAttackTarget())
                {
                    StopMoving();
                    FaceTarget();
                    TryAttack();
                }
                else
                {
                    ChaseTarget();
                }
            }
            else
            {
                LoseTarget();
            }
        }
        else if (!isWaiting)
        {
            SetAgro(false);
            PatrolLogic();
        }
    }

    private void SetAgro(bool value)
    {
        anim.SetBool(AgroHash, value);
    }

    private void LoseTarget()
    {
        targetPlayer = null;
        chaseMemoryTimer = 0f;
        waitForLandingTimer = 0f;

        SetAgro(false);

        anim.SetBool(RunHash, false);

        if (!isGrounded)
        {
            lostTargetWhileAirborne = true;
            lostTargetAirTimer = 0f;

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            anim.ResetTrigger(JumpHash);
            anim.SetBool(NearGroundHash, false);

            bool shouldFall = rb.linearVelocity.y < -0.05f;
            anim.SetBool(FallingHash, shouldFall);
        }
        else
        {
            lostTargetWhileAirborne = false;
            lostTargetAirTimer = 0f;

            StopMoving();
            ResetAirAnimatorParams();
        }
    }

    private void HandleLostTargetAirState()
    {
        if (!lostTargetWhileAirborne)
        {
            return;
        }

        lostTargetAirTimer += Time.deltaTime;

        SetAgro(false);

        anim.SetBool(RunHash, false);

        if (!isGrounded)
        {
            bool forceFalling = lostTargetAirTimer >= maxJumpStateTimeAfterLostTarget;
            bool falling = rb.linearVelocity.y < -0.05f || forceFalling;

            anim.ResetTrigger(JumpHash);
            anim.SetBool(FallingHash, falling);
            anim.SetBool(NearGroundHash, false);

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            lostTargetWhileAirborne = false;
            lostTargetAirTimer = 0f;

            StopMoving();
            ResetAirAnimatorParams();

            SetAgro(false);
        }
    }

    private bool FindTargetWithLineOfSight()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, agroRange, playerLayer);

        foreach (var hit in hits)
        {
            PlayerHealth hp = hit.GetComponent<PlayerHealth>();

            if (hp != null && hp.IsDead())
            {
                continue;
            }

            float distToTarget = Vector2.Distance(transform.position, hit.transform.position);
            bool playerTooHigh = hit.transform.position.y > transform.position.y + 2.5f;

            Vector2 direction = (hit.transform.position - transform.position).normalized;

            if (distToTarget <= closeAgroRange && !playerTooHigh)
            {
                if (!Physics2D.Raycast(transform.position, direction, distToTarget, obstacleLayer))
                {
                    targetPlayer = hit.transform;
                    return true;
                }
            }

            float dot = Vector2.Dot(isFacingRight ? Vector2.right : Vector2.left, direction);

            if (dot > 0f && !playerTooHigh)
            {
                if (!Physics2D.Raycast(transform.position, direction, distToTarget, obstacleLayer))
                {
                    targetPlayer = hit.transform;
                    return true;
                }
            }
        }

        return false;
    }

    private void PatrolLogic()
    {
        if (!isGrounded)
        {
            return;
        }

        bool isWall = IsWallAhead();
        bool isPit = IsPitAhead();

        if (isWall || isPit)
        {
            StartCoroutine(WaitAtPoint());
            return;
        }

        float targetX = startPoint.x + (isFacingRight ? patrolDistance : -patrolDistance);

        if (Mathf.Abs(transform.position.x - targetX) < 0.2f)
        {
            StartCoroutine(WaitAtPoint());
        }
        else
        {
            MoveTowards(targetX, moveSpeed);
        }
    }

    private void ChaseTarget()
    {
        if (targetPlayer == null)
        {
            return;
        }

        if (!isGrounded)
        {
            AirChaseControl();
            return;
        }

        float smartTargetX = GetSmartChaseTargetX();
        float xDifference = smartTargetX - transform.position.x;

        if (Mathf.Abs(xDifference) < 0.08f)
        {
            StopMoving();
            return;
        }

        float directionX = xDifference > 0f ? 1f : -1f;

        FaceDirection(directionX);

        bool wallAhead = IsWallAhead();
        bool pitAhead = IsPitAhead();

        if (!IsPlayerUnderMe() && wallAhead && CanJumpOverObstacle(directionX))
        {
            JumpOverObstacle(directionX);
            return;
        }

        if (pitAhead)
        {
            if (CanStepDownSafely(directionX))
            {
                MoveTowards(smartTargetX, chaseSpeed * dropMoveSpeedMultiplier);
                return;
            }

            StopMoving();
            return;
        }

        MoveTowards(smartTargetX, chaseSpeed);
    }

    private void AirChaseControl()
    {
        if (targetPlayer == null)
        {
            return;
        }

        if (ShouldWaitForPlayerLanding())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            anim.SetBool(RunHash, false);
            return;
        }

        float xDifference = targetPlayer.position.x - transform.position.x;

        if (Mathf.Abs(xDifference) <= playerUnderXThreshold)
        {
            anim.SetBool(RunHash, false);
            return;
        }

        float directionX = xDifference > 0f ? 1f : -1f;

        FaceDirection(directionX);

        float airControlSpeed = chaseSpeed * 0.35f;

        rb.linearVelocity = new Vector2(directionX * airControlSpeed, rb.linearVelocity.y);

        anim.SetBool(RunHash, false);
    }

    private bool ShouldWaitForPlayerLanding()
    {
        if (!waitForPlayerLanding)
        {
            return false;
        }

        if (targetPlayer == null)
        {
            waitForLandingTimer = 0f;
            return false;
        }

        if (!isGrounded)
        {
            return false;
        }

        float xDist = Mathf.Abs(targetPlayer.position.x - transform.position.x);
        float yDelta = targetPlayer.position.y - transform.position.y;

        bool playerIsCloseByX = xDist <= waitPlayerLandingXRange;
        bool playerIsAbove = yDelta >= playerAboveWaitThreshold;
        bool playerIsAirborne = IsTargetPlayerAirborne();

        if (playerIsCloseByX && playerIsAbove && playerIsAirborne)
        {
            waitForLandingTimer += Time.deltaTime;

            if (waitForLandingTimer <= maxWaitForPlayerLandingTime)
            {
                return true;
            }

            return false;
        }

        waitForLandingTimer = 0f;
        return false;
    }

    private bool IsTargetPlayerAirborne()
    {
        if (targetPlayer == null)
        {
            return false;
        }

        Rigidbody2D playerRb = targetPlayer.GetComponent<Rigidbody2D>();

        if (playerRb != null && Mathf.Abs(playerRb.linearVelocity.y) > 0.1f)
        {
            return true;
        }

        Vector2 rayStart = targetPlayer.position + Vector3.down * 0.45f;

        RaycastHit2D hit = Physics2D.Raycast(
            rayStart,
            Vector2.down,
            playerGroundRayDistance,
            groundLayer
        );

        return hit.collider == null;
    }

    private float GetSmartChaseTargetX()
    {
        if (targetPlayer == null)
        {
            return transform.position.x;
        }

        if (IsPlayerUnderMe())
        {
            return GetBestSidePositionNearPlayer();
        }

        float xDist = Mathf.Abs(targetPlayer.position.x - transform.position.x);
        float yDelta = targetPlayer.position.y - transform.position.y;

        if (xDist <= playerUnderXThreshold && yDelta < 0.2f)
        {
            return GetBestSidePositionNearPlayer();
        }

        return targetPlayer.position.x;
    }

    private float GetBestSidePositionNearPlayer()
    {
        float leftSideX = targetPlayer.position.x - sideAttackOffset;
        float rightSideX = targetPlayer.position.x + sideAttackOffset;

        bool leftHasGround = HasGroundAtX(leftSideX);
        bool rightHasGround = HasGroundAtX(rightSideX);

        float distanceToLeft = Mathf.Abs(transform.position.x - leftSideX);
        float distanceToRight = Mathf.Abs(transform.position.x - rightSideX);

        if (leftHasGround && !rightHasGround)
        {
            return leftSideX;
        }

        if (rightHasGround && !leftHasGround)
        {
            return rightSideX;
        }

        if (leftHasGround && rightHasGround)
        {
            return distanceToLeft <= distanceToRight ? leftSideX : rightSideX;
        }

        return distanceToLeft <= distanceToRight ? leftSideX : rightSideX;
    }

    private bool HasGroundAtX(float x)
    {
        Vector2 rayStart = new Vector2(x, transform.position.y + 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(
            rayStart,
            Vector2.down,
            safeDropRayDistance,
            groundLayer
        );

        return hit.collider != null;
    }

    private bool IsPlayerUnderMe()
    {
        if (targetPlayer == null)
        {
            return false;
        }

        float xDist = Mathf.Abs(targetPlayer.position.x - transform.position.x);
        float yDelta = targetPlayer.position.y - transform.position.y;

        bool playerAlmostUnderNpc = xDist <= playerUnderXThreshold;
        bool playerIsLower = yDelta < -playerBelowThreshold;

        return playerAlmostUnderNpc && playerIsLower;
    }

    private bool CanAttackTarget()
    {
        if (targetPlayer == null)
        {
            return false;
        }

        if (!isGrounded)
        {
            return false;
        }

        if (isJumpingObstacle)
        {
            return false;
        }

        if (IsPlayerUnderMe())
        {
            return false;
        }

        if (ShouldWaitForPlayerLanding())
        {
            return false;
        }

        float xDist = Mathf.Abs(targetPlayer.position.x - transform.position.x);
        float yDist = Mathf.Abs(targetPlayer.position.y - transform.position.y);

        bool closeEnoughByX = xDist <= attackRange;
        bool closeEnoughByY = yDist <= maxAttackHeightDifference;

        return closeEnoughByX && closeEnoughByY;
    }

    private bool IsWallAhead()
    {
        if (wallCheck == null)
        {
            return false;
        }

        return Physics2D.OverlapCircle(wallCheck.position, 0.1f, groundLayer | obstacleLayer);
    }

    private bool IsPitAhead()
    {
        if (pitCheck == null)
        {
            return false;
        }

        return !Physics2D.Raycast(pitCheck.position, Vector2.down, 0.5f, groundLayer);
    }

    private bool CanStepDownSafely(float directionX)
    {
        if (pitCheck == null)
        {
            return false;
        }

        Vector2 dropCheckStart = new Vector2(
            pitCheck.position.x + directionX * 0.25f,
            pitCheck.position.y + 0.15f
        );

        RaycastHit2D hit = Physics2D.Raycast(
            dropCheckStart,
            Vector2.down,
            safeDropRayDistance,
            groundLayer
        );

        if (hit.collider == null)
        {
            return false;
        }

        float dropDistance = hit.distance;

        return dropDistance <= maxSafeDropHeight;
    }

    private bool CanJumpOverObstacle(float directionX)
    {
        if (IsPlayerUnderMe())
        {
            return false;
        }

        if (!isGrounded)
        {
            return false;
        }

        if (isJumpingObstacle)
        {
            return false;
        }

        if (Time.time < lastJumpTime + jumpCooldown)
        {
            return false;
        }

        if (wallCheck == null)
        {
            return false;
        }

        Vector2 wallPoint = wallCheck.position;

        bool wallLow = Physics2D.OverlapCircle(wallPoint, 0.1f, groundLayer | obstacleLayer);

        if (!wallLow)
        {
            return false;
        }

        Vector2 upperPoint = wallPoint + Vector2.up * jumpClearanceHeight;

        bool blockedAbove = Physics2D.OverlapCircle(upperPoint, 0.14f, groundLayer | obstacleLayer);

        if (blockedAbove)
        {
            return false;
        }

        Vector2 landingRayStart = new Vector2(
            transform.position.x + directionX * landingCheckDistance,
            transform.position.y + 0.4f
        );

        bool hasGroundAfterObstacle = Physics2D.Raycast(
            landingRayStart,
            Vector2.down,
            landingGroundRayDistance,
            groundLayer
        );

        if (!hasGroundAfterObstacle)
        {
            return false;
        }

        if (targetPlayer != null)
        {
            float targetDir = Mathf.Sign(targetPlayer.position.x - transform.position.x);

            if (Mathf.Sign(directionX) != targetDir)
            {
                return false;
            }
        }

        return true;
    }

    private void JumpOverObstacle(float directionX)
    {
        lastJumpTime = Time.time;
        isJumpingObstacle = true;
        lostTargetWhileAirborne = false;
        lostTargetAirTimer = 0f;

        jumpAnimationActive = true;
        jumpStartedTime = Time.time;
        ignoreGroundUntilTime = Time.time + ignoreGroundAfterJumpTime;

        SetAgro(true);

        FaceDirection(directionX);

        anim.SetBool(RunHash, false);
        anim.SetBool(FallingHash, false);
        anim.SetBool(NearGroundHash, false);

        anim.ResetTrigger(JumpHash);
        anim.SetTrigger(JumpHash);

        rb.linearVelocity = new Vector2(directionX * jumpForwardBoost, jumpForce);

        StartCoroutine(ResetJumpObstacleState());
    }

    private IEnumerator ResetJumpObstacleState()
    {
        yield return new WaitForSeconds(0.15f);

        while (!isGrounded && !isDead)
        {
            yield return null;
        }

        isJumpingObstacle = false;
    }

    private void MoveTowards(float targetX, float speed)
    {
        float directionX = targetX > transform.position.x ? 1f : -1f;

        if (Mathf.Abs(targetX - transform.position.x) > 0.3f)
        {
            FaceDirection(directionX);
        }

        rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);

        bool shouldRun = isGrounded && !isJumpingObstacle && Mathf.Abs(rb.linearVelocity.x) > 0.05f;

        anim.SetBool(RunHash, shouldRun);
    }

    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetBool(RunHash, false);
    }

    private IEnumerator WaitAtPoint()
    {
        isWaiting = true;

        StopMoving();

        yield return new WaitForSeconds(patrolWaitTime);

        if (!isDead && targetPlayer == null && isGrounded)
        {
            Flip();
        }

        isWaiting = false;
    }

    private void TryAttack()
    {
        if (!isGrounded)
        {
            return;
        }

        if (IsPlayerUnderMe())
        {
            return;
        }

        if (ShouldWaitForPlayerLanding())
        {
            return;
        }

        FaceTarget();

        isAttacking = true;

        StopMoving();

        anim.SetTrigger(AttackHash);

        Invoke(nameof(ResetAttack), 1.2f);
    }

    public void AnimationEvent_Hit()
    {
        if (isDead)
        {
            return;
        }

        if (targetPlayer != null)
        {
            FaceTarget();
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);

        foreach (var hit in hits)
        {
            PlayerHealth hp = hit.GetComponent<PlayerHealth>();

            if (hp != null)
            {
                hp.TakeDamage(damageValue, transform.position);
            }
        }
    }

    private void ResetAttack()
    {
        isAttacking = false;
    }

    private void FaceTarget()
    {
        if (targetPlayer == null)
        {
            return;
        }

        float directionX = targetPlayer.position.x > transform.position.x ? 1f : -1f;

        FaceDirection(directionX);
    }

    private void FaceDirection(float directionX)
    {
        if (directionX > 0f && !isFacingRight)
        {
            Flip();
        }
        else if (directionX < 0f && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void CheckGroundStatus()
    {
        Vector2 checkPos;

        if (groundCheck != null)
        {
            checkPos = groundCheck.position;
        }
        else
        {
            checkPos = transform.position + Vector3.down * 0.55f;
        }

        bool rawGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

        if (Time.time < ignoreGroundUntilTime)
        {
            isGrounded = false;
        }
        else
        {
            isGrounded = rawGrounded;
        }

        anim.SetBool(NearGroundHash, isGrounded);
    }

    private void UpdateAirAnimations()
    {
        bool falling = !isGrounded && rb.linearVelocity.y < -0.05f;

        if (jumpAnimationActive)
        {
            bool jumpTimeExpired = Time.time >= jumpStartedTime + forceFallingAfterJumpTime;
            bool startedGoingDown = rb.linearVelocity.y <= 0.05f;

            if (!isGrounded && (startedGoingDown || jumpTimeExpired))
            {
                falling = true;
            }

            if (isGrounded && Time.time > ignoreGroundUntilTime)
            {
                jumpAnimationActive = false;
            }
        }

        if (isGrounded)
        {
            falling = false;
        }

        anim.SetBool(FallingHash, falling);

        if (!isGrounded)
        {
            anim.SetBool(RunHash, false);
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"NPC: agro={anim.GetBool(AgroHash)}, grounded={isGrounded}, wasGrounded={wasGroundedLastFrame}, " +
                $"yVel={rb.linearVelocity.y}, falling={falling}, jumpAnim={jumpAnimationActive}, " +
                $"ignoreGround={Time.time < ignoreGroundUntilTime}, memory={chaseMemoryTimer}, " +
                $"jumpingObstacle={isJumpingObstacle}, lostTargetAir={lostTargetWhileAirborne}, " +
                $"lostAirTimer={lostTargetAirTimer}, waitLanding={waitForLandingTimer}, playerUnder={IsPlayerUnderMe()}"
            );
        }
    }

    private void ResetAirStateOnLanding()
    {
        if (!forceResetAirStateOnLanding)
        {
            return;
        }

        bool justLanded = !wasGroundedLastFrame && isGrounded;

        if (!justLanded)
        {
            return;
        }

        isJumpingObstacle = false;
        lostTargetWhileAirborne = false;
        lostTargetAirTimer = 0f;

        ResetAirAnimatorParams();
    }

    private void ResetAirAnimatorParams()
    {
        jumpAnimationActive = false;
        ignoreGroundUntilTime = 0f;
        jumpStartedTime = 0f;

        anim.ResetTrigger(JumpHash);
        anim.SetBool(FallingHash, false);
        anim.SetBool(NearGroundHash, true);
        anim.SetBool(RunHash, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.transform.position.y > transform.position.y + 0.6f)
            {
                PlayerController pControl = collision.gameObject.GetComponent<PlayerController>();
                Rigidbody2D pRb = collision.gameObject.GetComponent<Rigidbody2D>();

                if (pControl != null && pRb != null)
                {
                    if (pControl.TryBounce())
                    {
                        float playerLookDir = Mathf.Sign(collision.transform.localScale.x);

                        pRb.linearVelocity = Vector2.zero;
                        pRb.AddForce(new Vector2(playerLookDir * 8f, headBounceForce), ForceMode2D.Impulse);

                        StopMoving();
                    }
                    else
                    {
                        pControl.TakeSelfDamage(damageValue, transform.position);

                        float slideDir = collision.transform.position.x > transform.position.x ? 1f : -1f;

                        pRb.AddForce(new Vector2(slideDir * 4f, 2f), ForceMode2D.Impulse);

                        Debug.Log("Абуз головы! Игрок получил урон.");
                    }
                }
            }
        }
    }

    public void OnDeath()
    {
        isDead = true;

        SetAgro(false);

        StopMoving();

        rb.linearVelocity = Vector2.zero;

        jumpAnimationActive = false;
        ignoreGroundUntilTime = 0f;
        jumpStartedTime = 0f;

        anim.SetBool(RunHash, false);
        anim.SetBool(FallingHash, false);
        anim.SetBool(NearGroundHash, true);
        anim.ResetTrigger(JumpHash);

        anim.SetTrigger(DeadTriggerHash);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, agroRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, closeAgroRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(wallCheck.position, 0.1f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(wallCheck.position + Vector3.up * jumpClearanceHeight, 0.14f);
        }

        if (pitCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pitCheck.position, pitCheck.position + Vector3.down * 0.5f);

            float dir = isFacingRight ? 1f : -1f;

            Vector3 dropCheckStart = new Vector3(
                pitCheck.position.x + dir * 0.25f,
                pitCheck.position.y + 0.15f,
                pitCheck.position.z
            );

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(dropCheckStart, dropCheckStart + Vector3.down * safeDropRayDistance);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        else
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.55f, groundCheckRadius);
        }

        float landingDir = isFacingRight ? 1f : -1f;

        Vector3 landingRayStart = new Vector3(
            transform.position.x + landingDir * landingCheckDistance,
            transform.position.y + 0.4f,
            transform.position.z
        );

        Gizmos.color = Color.white;
        Gizmos.DrawLine(landingRayStart, landingRayStart + Vector3.down * landingGroundRayDistance);

        if (targetPlayer != null)
        {
            Gizmos.color = Color.red;

            Vector3 leftSide = new Vector3(
                targetPlayer.position.x - sideAttackOffset,
                targetPlayer.position.y,
                targetPlayer.position.z
            );

            Vector3 rightSide = new Vector3(
                targetPlayer.position.x + sideAttackOffset,
                targetPlayer.position.y,
                targetPlayer.position.z
            );

            Gizmos.DrawWireSphere(leftSide, 0.12f);
            Gizmos.DrawWireSphere(rightSide, 0.12f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                targetPlayer.position + Vector3.down * 0.45f,
                targetPlayer.position + Vector3.down * (0.45f + playerGroundRayDistance)
            );
        }
    }
}
