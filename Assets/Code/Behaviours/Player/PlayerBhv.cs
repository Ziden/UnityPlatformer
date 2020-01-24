using System;
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
    public float yCollisionCorrection = 0.2f;
    public float fastFallRate = 2f;
    public float dashLength = 3.2f;
    public float dashPower = 2.5f;

    // State values
    public float currentJumpSize = 0f;
    public float currentDashSize = 0f;


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

    ////////////
    /// INPUT //
    ////////////

    public void ReadInput()
    {
        var inAir = states.Has(PlayerState.JUMPING) || states.Has(PlayerState.FALLING);

        // RIGHT
        if (Input.GetKeyDown(KeyCode.D))
        {
            if(!states.Has(PlayerState.DASHING) || inAir)
            {
                states.Add(PlayerState.MOVING_RIGHT);
                states.Remove(PlayerState.MOVING_LEFT);
            } else if(states.Has(PlayerState.DASHING) && !facingRight)
            {
                cancelDash();
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
                cancelDash();
            }
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            if (!states.Has(PlayerState.DASHING))
                states.Remove(PlayerState.MOVING_LEFT);
        }

        // DOWN
        if(Input.GetKeyDown(KeyCode.S))
        {
            if (states.Has(PlayerState.FALLING))
            {
                states.Add(PlayerState.FAST_FALLING);

                EffectManager.Play(fastFallEffect);
            } 
        }

        // JUMP
        if(Input.GetKeyDown(KeyCode.J) && states.Has(PlayerState.ONGROUND)) {

            // DASHING
            if (Input.GetKey(KeyCode.S))
            {

                if(currentDashSize > 0 && currentDashSize < dashLength * 0.8f)
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
            } else
            {
                states.Add(PlayerState.JUMPING);
            }
                
        } else if(Input.GetKeyUp(KeyCode.J) && states.Has(PlayerState.JUMPING))
        {
            states.Remove(PlayerState.JUMPING);
            states.Add(PlayerState.FALLING);
        }
    }

    /////////////////
    /// ANIMATIONS //
    /////////////////

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
        if (states.WasAdded(PlayerState.FALLING))
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

        if (states.WasAdded(PlayerState.ONGROUND))
        {
            animation.speed = 1;
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
            if (startedToMove || states.WasAdded(PlayerState.ONGROUND))
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

    /////////////////
    /// PHYSICS    //
    /////////////////

    public void Physics()
    {
        float delta = Time.deltaTime;

        // Begin jump
        if (states.WasAdded(PlayerState.JUMPING))
        {
            velocity.y = jumpPower;
            currentJumpSize = 0;
            if(states.Has(PlayerState.DASHING))
            {
                if(Input.GetKey(KeyCode.A))
                {
                    states.Remove(PlayerState.MOVING_RIGHT);
                    states.Add(PlayerState.MOVING_LEFT);
                } else if(Input.GetKey(KeyCode.D))
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

            if(states.WasAdded(PlayerState.DASHING))
                currentDashSize = 0;

            moveSpeed = speed * dashPower;
            currentDashSize += moveSpeed * delta;
            if(states.Has(PlayerState.ONGROUND) && currentDashSize >= dashLength)
            {
                cancelDash();
                moveSpeed = 0;
            }
        }
        if (states.Has(PlayerState.MOVING_RIGHT))
        {
            if (map.GetTile(GetPlayerTile(new Vector2(0.25f, correction))) == null)
                velocity.x = moveSpeed;
        }
        else if (states.Has(PlayerState.MOVING_LEFT))
        {
            if (map.GetTile(GetPlayerTile(new Vector2(-0.25f, correction))) == null)
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

    /////////////////
    /// COLLISIONS //
    /////////////////

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
                transform.position = new Vector2(transform.position.x, point.y + 0.42f);

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

    public Vector3Int GetPlayerTile(Vector2? offset = null)
    {
        var position = transform.position;
        if(offset.HasValue)
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

    private void cancelDash()
    {
        states.Remove(PlayerState.DASHING);
        states.Remove(PlayerState.MOVING_LEFT);
        states.Remove(PlayerState.MOVING_RIGHT);

        if (Input.GetKey(KeyCode.A))
        {
            states.Add(PlayerState.MOVING_LEFT);
        }
        else if (Input.GetKey(KeyCode.D))
        {
            states.Add(PlayerState.MOVING_RIGHT);
        }
        currentDashSize = 0;
    }
}
