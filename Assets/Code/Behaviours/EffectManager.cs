using UnityEngine;
using System.Collections;

public class EffectConfig
{
    public Transform targetTransform;
    public string animationName;
    public EffectAlign align;
    public Vector3 offset = Vector3.zero;
    public bool attachToSprite;
    public bool flip;
}

public class EffectManager : MonoBehaviour
{
    private static EffectManager instance = null;

    private Animator animations;

    public static void Play(EffectConfig effectConfig)
    {
        Get().PlayEffect(effectConfig);
    }

    private static EffectManager Get()
    {
        return instance;

    }

    public void PlayEffect(EffectConfig config)
    {
        StartCoroutine(PlayRoutine(config));
    }

    private IEnumerator PlayRoutine(EffectConfig cfg)
    {
        var animObject = Instantiate(this.gameObject);
        var fx = animObject.GetComponent<EffectManager>();
        var fxAnim = fx.GetComponent<Animator>();

        var offsetv = cfg.offset;

        fxAnim.Play(cfg.animationName);

        if (cfg.attachToSprite)
            animObject.transform.SetParent(cfg.targetTransform);

        if (cfg.flip)
            TransformUtils.Flip(animObject.transform);

        if (cfg.align == EffectAlign.CENTER)
            animObject.transform.position = cfg.targetTransform.position + offsetv;
        else if (cfg.align == EffectAlign.BOTTOM)
        {
            var trans = cfg.targetTransform;
            var targetObjectSprite = trans.Find("Animation").GetComponent<SpriteRenderer>();
            var animationSprite = animObject.GetComponent<SpriteRenderer>();
            var spriteBottom = trans.position.y - targetObjectSprite.bounds.size.y/2;
            animObject.transform.position = new Vector3(trans.position.x, spriteBottom) + offsetv;
        }

        yield return new WaitForSeconds(GetClip(cfg.animationName).length);
        Destroy(animObject);
    }

    public AnimationClip GetClip(string name)
    {
        AnimationClip[] clips = animations.runtimeAnimatorController.animationClips;
        foreach(var clip in clips)
        {
            if(clip.name == name)
            {
                return clip;
            }
        }
        return null;
    }

    // Use this for initialization
    void Awake()
    {
        if(instance==null)
            instance = this;
        animations = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}

public enum EffectAlign
{
    BOTTOM, CENTER
}
