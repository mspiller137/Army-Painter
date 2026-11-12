
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AirbrushPaint : MonoBehaviour
{
    [Header("XR Interaction")]
    [SerializeField] private XRGrabInteractable grabInteractable;

    [Header("Spray Configuration")]
    [SerializeField] private Transform nozzle;
    [SerializeField] private float sprayDistance = 5f;
    [SerializeField] private float sprayAngle = 4f;
    [SerializeField] private float sprayRate = 60f;
    [SerializeField] private LayerMask paintableLayers = ~0;

    [Header("Paint Configuration")]
    [SerializeField] private Color paintColour = Color.red;
    [SerializeField] private float brushSize = 0.08f;
    [SerializeField] private float paintOpacity = 0.8f;
    [SerializeField] private int textureResolution = 64;

    [Header("Performance")]
    [SerializeField] private int maximumDecals = 1000; 

    private bool isSpraying;
    private float sprayTimer;

    private Texture2D brushTexture;
    private Shader decalShader;

    private readonly List<GameObject> decals = new();

    private void Awake()
    {
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        brushTexture = CreateBrushTexture();

        decalShader = Shader.Find("Sprites/Default");

        if (decalShader == null)
        {
            Debug.LogError("AirbrushPaint: Could not find Sprites/Default shader.");
        }
            
    }

    private void OnEnable()
    {
        if (grabInteractable == null)
        {
            return;
        }            

        grabInteractable.activated.AddListener(StartSpraying);
        grabInteractable.deactivated.AddListener(StopSpraying);
    }

    private void OnDisable()
    {
        if (grabInteractable == null)
        {
            return;
        }

        grabInteractable.activated.RemoveListener(StartSpraying);
        grabInteractable.deactivated.RemoveListener(StopSpraying);

        isSpraying = false;
    }

    private void Update()
    {
        if (!isSpraying || nozzle == null)
        {
            return;
        }

        sprayTimer += Time.deltaTime;

        float interval = 1f / Mathf.Max(sprayRate, 1f);

        while (sprayTimer >= interval)
        {
            sprayTimer -= interval;
            Spray();
        }
    }

    private void StartSpraying(ActivateEventArgs args)
    {
        isSpraying = true;
        sprayTimer = 0f;
        Spray();
    }

    private void StopSpraying(DeactivateEventArgs args)
    {
        isSpraying = false;
    }

    private void Spray()
    {
        Vector2 spread = Random.insideUnitCircle * Mathf.Tan(sprayAngle * Mathf.Deg2Rad);

        Vector3 direction = (nozzle.forward + nozzle.right * spread.x + nozzle.up * spread.y).normalized;

        if (!Physics.Raycast(nozzle.position, direction, out RaycastHit hit, sprayDistance, paintableLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        CreatePaintDecal(hit);
    }

    private void CreatePaintDecal(RaycastHit hit)
    {
        if (decalShader == null)
        {
            return;
        }

        GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Quad);

        Collider decalCollider = decal.GetComponent<Collider>();
        if (decalCollider != null)
        {
            Destroy(decalCollider);
        }           

        decal.name = "Airbrush Paint Decal";

        decal.transform.position = hit.point + hit.normal * 0.002f;

        decal.transform.rotation = Quaternion.LookRotation(hit.normal);

        float diameter = brushSize * Random.Range(0.7f, 1.3f);

        decal.transform.localScale = new Vector3(diameter, diameter, 1f);

        Renderer decalRenderer = decal.GetComponent<Renderer>();

        Material material = new Material(decalShader);
        material.mainTexture = brushTexture;
        material.color = new Color(paintColour.r, paintColour.g, paintColour.b, paintOpacity * Random.Range(0.75f, 1f));

        decalRenderer.material = material;

        decals.Add(decal);

        if (decals.Count > maximumDecals)
        {
            GameObject oldest = decals[0];
            decals.RemoveAt(0);
            Destroy(oldest);
        }
    }

    private Texture2D CreateBrushTexture()
    {
        int resolution = Mathf.Max(textureResolution, 8);

        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u = (x + 0.5f) / resolution * 2f - 1f;
                float v = (y + 0.5f) / resolution * 2f - 1f;

                float distance = Mathf.Sqrt(u * u + v * v);

                float alpha = 1f - Mathf.SmoothStep(0.65f, 1f, distance);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        if (brushTexture != null)
        {
            Destroy(brushTexture);
        }            
    }
}
