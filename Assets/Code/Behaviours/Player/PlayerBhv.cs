using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerBhv : MonoBehaviour
{
    public Animator animation;

    public StateManager<PlayerState> states = new StateManager<PlayerState>();
    public StateManager<KeyCode> keyBuffer = new StateManager<KeyCode>();

    public Vector2 velocity;

    public bool facingRight = true;
    private Tilemap map;

    // Configuration
    public float speed = 3.2f;
    public float fallSpeed = 6f;
    public float jumpPower = 6f;
    public float maxJumpHeight = 2f;
    public float gravityPull = 0.3f;
    public float yCollisionCorrection = 0.20f;
    public float xCollisionCorrection = 0.65f;
    public float fastFallRate = 2f;
    public float dashLength = 3.2f;
    public float dashPower = 2.5f;
    public float attackDelay = 0.00f;
    public float landYAdjust = 0.00f;

    // State values
    public float currentJumpSize = 0f;
    public float currentDashSize = 0f;
    public float lastFirstAttack = 0f;

    // Animation EFfects
    private EffectConfig fastFallEffect;
    private EffectConfig jumpSmokeEffect;
    private EffectConfig landSmokeEffect;
    private EffectConfig dashEffect;

    void Start()
    {

        loadEffects();
        animation.Play("s_idl");
        velocity = new Vector2(0, -fallSpeed);
        map = GameObject.FindGameObjectWithTag("Map").GetComponent<Tilemap>();
    }

    void Update()
    {
        ReadInput();
        Physics();
        UpdateAnimations();
        states.ClearHistory();
    }

    #region Input
    public void ReadInput()
    {
        var inAir = states.Has(PlayerState.JUMPING) || states.Has(PlayerState.FALLING);

        // RIGHT
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (!states.Has(PlayerState.DASHING) || inAir)
            {
                states.Add(PlayerState.MOVING_RIGHT);
                states.Remove(PlayerState.MOVING_LEFT);
            }
            else if (states.Has(PlayerState.DASHING) && !facingRight)
            {
                ResetMovement();
            }
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            if (!states.Has(PlayerState.DASHING))
                states.Remove(PlayerState.MOVING_RIGHT);
        }

        // LEFT
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (!states.Has(PlayerState.DASHING) || inAir)
            {
                states.Add(PlayerState.MOVING_LEFT);
                states.Remove(PlayerState.MOVING_RIGHT);
            }
            else if (states.Has(PlayerState.DASHING) && facingRight)
            {
                ResetMovement();
            }
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            if (!states.Has(PlayerState.DASHING))
                states.Remove(PlayerState.MOVING_LEFT);
        }

        // DOWN
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (states.Has(PlayerState.FALLING))
            {
                states.Add(PlayerState.FAST_FALLING);

                EffectManager.Play(fastFallEffect);
            }
        }

        // ATTACK
        if (Input.GetKeyDown(KeyCode.K))
        {

            if(!states.Has(PlayerState.ATTACKING))
                states.Add(PlayerState.ATTACKING);
            else
                states.Add(PlayerState.CONTINUE_ATTACK);
        }

        // JUMP
        if (Input.GetKeyDown(KeyCode.J) && states.Has(PlayerState.ONGROUND))
        {

            // DASHING
            if (Input.GetKey(KeyCode.S))
            {

                if (currentDashSize > 0 && currentDashSize < dashLength * 0.8f)
                {
                    return;
                }

                currentDashSize = 0;
                states.Remove(PlayerState.DASHING);
                states.Add(PlayerState.DASHING);
                if (facingRight)
                    states.Add(PlayerState.MOVING_RIGHT);
                else
                    states.Add(PlayerState.MOVING_LEFT);
            }
            else
            {
                states.Add(PlayerState.JUMPING);
            }

        }
        else if (Input.GetKeyUp(KeyCode.J) && states.Has(PlayerState.JUMPING))
        {
            states.Remove(PlayerState.JUMPING);
            states.Add(PlayerState.FALLING);
        }
    }
    #endregion

    #region Animations
    public void UpdateAnimations()
    {
        if (!states.BeenModified())
            return;

        // Jumping animation
        if (states.WasAdded(PlayerState.JUMPING))
        {
            animation.Play("s_jump");
            EffectManager.Play(jumpSmokeEffect);
            return;
        }

        // Falling
        if (states.WasAdded(PlayerState.FALLING) && !states.Has(PlayerState.ATTACKING))
        {
            // Falling Roll
            animation.speed = 2 - (currentJumpSize * 100 / maxJumpHeight) / 100;

            if (states.Has(PlayerState.MOVING_RIGHT))
                if (facingRight)
                    animation.Play("s_roll_r");
                else
                    animation.Play("s_roll_l");
            else if (states.Has(PlayerState.MOVING_LEFT))
                if (facingRight)
                    animation.Play("s_roll_l");
                else
                    animation.Play("s_roll_r");
            else
                animation.Play("s_fall");

        }

        // Always revert animation speed changes when you hit the ground
        if (states.WasAdded(PlayerState.ONGROUND))
        {
            animation.speed = 1;
        }


        // Attacking animation
        if (states.WasAdded(PlayerState.ATTACKING))
        {
            var timeUntilLastAttack = Time.time - lastFirstAttack;
            var clipName = "s_sword";
            if (animation.GetCurrentAnimatorClipInfo(0)[0].clip.name == clipName || timeUntilLastAttack < 1)
            {
                clipName = "s_sword_2";
                lastFirstAttack = 0;
            }
            else
                lastFirstAttack = Time.time;

            animation.Play(clipName);
            StartCoroutine(FinishAttack());
        }

        // Running animation
        if (states.Has(PlayerState.ONGROUND))
        {

            // Dashing
            if (states.WasAdded(PlayerState.DASHING))
            {
                animation.Play("s_dash");
                dashEffect.flip = !facingRight;
                EffectManager.Play(dashEffect);
                EffectManager.Play(landSmokeEffect);
                return;
            }

            bool startedToMove = states.WasAdded(PlayerState.MOVING_LEFT) || states.WasAdded(PlayerState.MOVING_RIGHT);
            bool stoppedMove = states.WasRemoved(PlayerState.MOVING_LEFT) || states.WasRemoved(PlayerState.MOVING_RIGHT);
            bool isMoving = states.Has(PlayerState.MOVING_RIGHT) || states.Has(PlayerState.MOVING_LEFT);

            // Halting Movement
            if (stoppedMove && !isMoving)
            {
                animation.Play("s_idl");
                return;
            }

            // Starting movement
            if (!states.Has(PlayerState.ATTACKING) && startedToMove || states.WasAdded(PlayerState.ONGROUND))
            {
                if (states.Has(PlayerState.MOVING_RIGHT))
                {
                    TransformUtils.FaceRight(this, true);
                    animation.Play("s_run");
                }
                else if (states.Has(PlayerState.MOVING_LEFT))
                {
                    TransformUtils.FaceRight(this, false);
                    animation.Play("s_run");
                }
            }
        }
    }
    #endregion

    #region Physics
    public void Physics()
    {
        float delta = Time.deltaTime;

        // Begin jump
        if (states.WasAdded(PlayerState.JUMPING))
        {
            velocity.y = jumpPower;
            currentJumpSize = 0;
            if (states.Has(PlayerState.DASHING))
            {
                if (Input.GetKey(KeyCode.A))
                {
                    states.Remove(PlayerState.MOVING_RIGHT);
                    states.Add(PlayerState.MOVING_LEFT);
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    states.Add(PlayerState.MOVING_RIGHT);
                    states.Remove(PlayerState.MOVING_LEFT);
                }
            }
        }

        // Jumping
        if (states.Has(PlayerState.JUMPING))
        {
            currentJumpSize += velocity.y * delta;
            if (currentJumpSize >= maxJumpHeight)
            {
                states.Remove(PlayerState.JUMPING);
                states.Add(PlayerState.FALLING);
            }
        }

        // Moving
        velocity.x = 0;
        float correction = 0;
        if (velocity.y > 0)
            correction = -yCollisionCorrection;

        var moveSpeed = speed;
        if (states.Has(PlayerState.DASHING))
        {

            if (states.WasAdded(PlayerState.DASHING))
                currentDashSize = 0;

            moveSpeed = speed * dashPower;
            currentDashSize += moveSpeed * delta;
            if (states.Has(PlayerState.ONGROUND) && currentDashSize >= dashLength)
            {
                ResetMovement();
                moveSpeed = 0;
            }
        }
        if (states.Has(PlayerState.MOVING_RIGHT))
        {
            if (map.GetTile(GetPlayerTile(new Vector2(xCollisionCorrection, correction))) == null)
                velocity.x = moveSpeed;
        }
        else if (states.Has(PlayerState.MOVING_LEFT))
        {
            if (map.GetTile(GetPlayerTile(new Vector2(-xCollisionCorrection, correction))) == null)
                velocity.x = -moveSpeed;
        }

        // Falling
        if (states.Has(PlayerState.FALLING))
        {
            if (velocity.y > 0)
            {
                if (states.WasAdded(PlayerState.FAST_FALLING))
                    velocity.y /= 10;
                else if (states.WasAdded(PlayerState.FALLING))
                    velocity.y /= 4;
            }

            if (states.Has(PlayerState.FAST_FALLING))
            {
                if (velocity.y > -fallSpeed * fastFallRate)
                    velocity.y -= gravityPull * fastFallRate;
            }
            else
            {
                if (velocity.y > -fallSpeed)
                    velocity.y -= gravityPull;
            }

        }

        transform.Translate(velocity * delta);
    }
    #endregion

    #region Collisions
    public void OnCollisionLeave(BodyPart part, Collision2D uncollided)
    {
        var leftTag = uncollided.transform.tag;
        if (leftTag == "Map")
        {
            // Leaving Floor
            if (part == BodyPart.FEET)
            {
                states.Remove(PlayerState.ONGROUND);

                // Falling
                if (!states.Has(PlayerState.JUMPING))
                {
                    states.Remove(PlayerState.JUMPING);
                    states.Add(PlayerState.FALLING);
                }
            }
        }
    }

    public void OnCollide(BodyPart part, Collision2D collided, Vector3 point)
    {
        var collidedTag = collided.transform.tag;
        if (collidedTag == "CameraBounds")
        {
            // Switching rooms
            var bounds = collided.transform.GetComponent<BoxCollider2D>().bounds;
            SamuraiCamera.SetRoomBounds(bounds);
        }

        if (collidedTag == "Map")
        {
            if (part == BodyPart.FEET)
            {

                Debug.Log("LAND");

                // Landing
                states.Remove(PlayerState.FAST_FALLING);
                states.Remove(PlayerState.FALLING);
                states.Remove(PlayerState.JUMPING);
                states.Add(PlayerState.ONGROUND);

                velocity.y = 0;
                if (!states.Has(PlayerState.MOVING_LEFT) && !states.Has(PlayerState.MOVING_RIGHT))
                    animation.Play("s_land");
                else
                {
                    TransformUtils.FaceRight(this, states.Has(PlayerState.MOVING_RIGHT));
                    animation.Play("s_run");
                }
                transform.position = new Vector2(transform.position.x, point.y + landYAdjust);

                EffectManager.Play(landSmokeEffect);
            }
            else if (part == BodyPart.HEAD)
            {
                velocity.y = 0;
                states.Add(PlayerState.FALLING);
                states.Remove(PlayerState.JUMPING);
            }
        }
    }

    #endregion

    #region Functions

    public Vector3Int GetPlayerTile(Vector2? offset = null)
    {
        var position = transform.position;
        if (offset.HasValue)
            position += (Vector3)offset.Value;
        return new Vector3Int((int)Math.Round(position.x), (int)Math.Floor(position.y), 0);
    }

    public void loadEffects()
    {
        fastFallEffect = new EffectConfig()
        {
            align = EffectAlign.BOTTOM,
            offset = new Vector3(0, 0.2f, 0),
            animationName = "Star",
            attachToSprite = true,
            targetTransform = transform
        };
        jumpSmokeEffect = new EffectConfig()
        {
            align = EffectAlign.BOTTOM,
            offset = new Vector3(0, 0.3f, 0),
            animationName = "jump_smoke",
            targetTransform = transform
        };
        landSmokeEffect = new EffectConfig()
        {
            align = EffectAlign.BOTTOM,
            offset = new Vector3(0, 0.2f, 0),
            animationName = "land_smoke",
            targetTransform = transform
        };
        dashEffect = new EffectConfig()
        {
            align = EffectAlign.BOTTOM,
            offset = new Vector3(0, 0.4f, 0),
            animationName = "DashSmoke",
            targetTransform = transform
        };
    }

    private bool ResetMovement()
    {
        states.Remove(PlayerState.DASHING);
        states.Remove(PlayerState.MOVING_LEFT);
        states.Remove(PlayerState.MOVING_RIGHT);
        currentDashSize = 0;
        if (Input.GetKey(KeyCode.A))
        {
            states.Add(PlayerState.MOVING_LEFT);
            return true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            states.Add(PlayerState.MOVING_RIGHT);
            return true;
        }
      
        return false;
    }

    private IEnumerator FinishAttack()
    {
        yield return new WaitForSeconds(0.2f);
        states.Remove(PlayerState.ATTACKING);
        if (states.Has(PlayerState.CONTINUE_ATTACK))
        {
            Debug.Log("Continued Attack");
            states.Add(PlayerState.ATTACKING);
            states.Remove(PlayerState.CONTINUE_ATTACK);
        }
        else
        {
            if (!ResetMovement())
            {
                var clipInfo = animation.GetCurrentAnimatorClipInfo(0)[0];
                if (clipInfo.clip.name.Contains("s_sword"))
                    if (clipInfo.clip.name.Contains("2"))
                        animation.Play("s_sword_keep_2");
                    else
                        animation.Play("s_sword_keep");
            }
        }
    }

    #endregion  
}
