using UnityEngine;
using System.Collections;

public class CellOxygenEffect : MonoBehaviour
{
    public enum OxygenState { Deoxygenated, Oxygenated }

    [Header("State")]
    public OxygenState currentState = OxygenState.Deoxygenated;

    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem oxygenatedEffect;   // red — deoxy → oxy
    [SerializeField] private ParticleSystem deoxygenatedEffect; // blue — oxy → deoxy

    [Header("Sound Effects")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip oxygenatedSFX;
    [SerializeField] private AudioClip deoxygenatedSFX;
    [SerializeField] private float sfxVolume = 1f;

    [Header("Oxygenated Sparkle (Red)")]
    [SerializeField] private Color oxygenatedColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private int oxygenatedBurstCount = 50;
    [SerializeField] private float oxygenatedSpeed = 5f;
    [SerializeField] private float oxygenatedSize = 2f;
    [SerializeField] private float oxygenatedLifetime = 0.6f;
    [SerializeField] private float oxygenatedSpread = 50f;

    [Header("Deoxygenated Sparkle (Blue)")]
    [SerializeField] private Color deoxygenatedColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private int deoxygenatedBurstCount = 50;
    [SerializeField] private float deoxygenatedSpeed = 5f;
    [SerializeField] private float deoxygenatedSize = 2f;
    [SerializeField] private float deoxygenatedLifetime = 0.6f;
    [SerializeField] private float deoxygenatedSpread = 50f;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

    private OxygenState lastState;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;

        if (oxygenatedEffect == null)
            oxygenatedEffect = BuildParticleSystem("OxygenatedSparkle", oxygenatedColor);

        if (deoxygenatedEffect == null)
            deoxygenatedEffect = BuildParticleSystem("DeoxygenatedSparkle", deoxygenatedColor);

        ApplyParticleSettings(oxygenatedEffect, oxygenatedColor, oxygenatedBurstCount,
            oxygenatedSpeed, oxygenatedSize, oxygenatedLifetime, oxygenatedSpread);

        ApplyParticleSettings(deoxygenatedEffect, deoxygenatedColor, deoxygenatedBurstCount,
            deoxygenatedSpeed, deoxygenatedSize, deoxygenatedLifetime, deoxygenatedSpread);

        lastState = currentState;

        Debug.Log("[CellOxygenEffect] Ready. Both particle systems and audio initialized.");
    }

    private void Update()
    {
        if (currentState != lastState)
        {
            if (currentState == OxygenState.Oxygenated)
            {
                PlayBurst(oxygenatedEffect);
                PlaySound(oxygenatedSFX);
            }
            else
            {
                PlayBurst(deoxygenatedEffect);
                PlaySound(deoxygenatedSFX);
            }

            lastState = currentState;
        }
    }

    public void SetOxygenated(bool isOxygenated)
    {
        currentState = isOxygenated ? OxygenState.Oxygenated : OxygenState.Deoxygenated;
    }

    public void PlayOxygenated()
    {
        PlayBurst(oxygenatedEffect);
        PlaySound(oxygenatedSFX);
    }

    public void PlayDeoxygenated()
    {
        PlayBurst(deoxygenatedEffect);
        PlaySound(deoxygenatedSFX);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[CellOxygenEffect] No sound effect assigned for this state.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("[CellOxygenEffect] AudioSource is missing.");
            return;
        }

        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void PlayBurst(ParticleSystem ps)
    {
        if (ps == null)
        {
            Debug.LogError("[CellOxygenEffect] ParticleSystem is NULL!");
            return;
        }

        ps.transform.position = transform.position + spawnOffset;

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();

        StartCoroutine(LogCountNextFrame(ps));

        Debug.Log($"[CellOxygenEffect] PlayBurst() called on {ps.gameObject.name}.");
    }

    IEnumerator LogCountNextFrame(ParticleSystem ps)
    {
        yield return null;
        Debug.Log($"[CellOxygenEffect] One frame after burst — particleCount={ps.particleCount} on {ps.gameObject.name}");
    }

    private void OnDestroy()
    {
        if (oxygenatedEffect != null)
            Destroy(oxygenatedEffect.gameObject);

        if (deoxygenatedEffect != null)
            Destroy(deoxygenatedEffect.gameObject);
    }

    private ParticleSystem BuildParticleSystem(string name, Color color)
    {
        GameObject go = new GameObject(name);

        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(color);

        return ps;
    }

    private Material CreateParticleMaterial(Color color)
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Particles/Additive");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        Debug.Log($"[CellOxygenEffect] Using shader: {shader?.name ?? "NULL"}");

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private void ApplyParticleSettings(ParticleSystem ps, Color color, int burstCount,
        float speed, float size, float lifetime, float spread)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = -0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.rateOverDistance = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)burstCount)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spread;
        shape.radius = 0.1f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color,       0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(color,       1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        colorOverLife.color = new ParticleSystem.MinMaxGradient(g);

        var rend = ps.GetComponent<ParticleSystemRenderer>();

        if (rend.material == null || rend.material.name.Contains("Default-Material"))
            rend.material = CreateParticleMaterial(color);

        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sortingOrder = 10;
    }
}