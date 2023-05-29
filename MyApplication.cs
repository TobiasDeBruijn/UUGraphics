using OpenTK.Mathematics;

namespace Template;

internal static class VecUtil {
    public static Vector3 FromFloat3(float f) {
        return new Vector3(f, f, f);
    }

    public static Vector2 FromFloat2(float f) {
        return new Vector2(f, f);
    }

    public static Vector3 Min(this Vector3 left, float right) {
        return new Vector3(
            Math.Min(left.X, right),
            Math.Min(left.Y, right),
            Math.Min(left.Z, right)
        );
    }

    public static Vector3 Max(this Vector3 left, float right) {
        return new Vector3(
            Math.Max(left.X, right),
            Math.Max(left.Y, right),
            Math.Max(left.Z, right)
        );
    }
}

public enum MaterialType {
    Diffuse,
    Glossy,
}

internal struct Material {
    public readonly Vector3 diffuseColor;
    public readonly Vector3 ambientColor;
    public readonly MaterialType materialType;
    public readonly float reflectance;

    private Material(Vector3 diffuseColor, MaterialType materialType, float reflectance, Vector3 ambientColor) {
        this.diffuseColor = diffuseColor;
        this.materialType = materialType;
        this.reflectance = reflectance;
        this.ambientColor = ambientColor;
    }

    public static Material Diffuse(Vector3 color, float reflectance) {
        return new Material(color, MaterialType.Diffuse, reflectance, color);
    }
}

internal struct TraceResult {
    public readonly bool collision;
    public readonly float distance;

    private TraceResult(bool collision, float distance) {
        this.collision = collision;
        this.distance = distance;
    }

    public static TraceResult Collide(float distance) {
        return new TraceResult(true, distance);
    }

    public static TraceResult Miss() {
        return new TraceResult(false, 0f);
    }
}

internal struct Light {
    public readonly Vector3 position;
    public readonly float intensity;

    public Light(Vector3 position, float intensity) {
        this.position = position;
        this.intensity = intensity;
    }
}

internal struct Sphere {
    public readonly Vector3 center;
    public readonly float radius;
    public readonly Material material;
    public readonly float diameter;
    public readonly float radiusSquared;

    public Sphere(Vector3 center, float radius, Material material) {
        this.center = center;
        this.radius = radius;
        this.material = material;
        this.diameter = radius * 2;
        this.radiusSquared = radius * radius;
    }
}

internal struct Ray {
    public Vector3 origin;
    public Vector3 direction;

    public Ray(Vector3 origin, Vector3 direction) {
        this.origin = origin;
        this.direction = direction;
    }
}

internal struct HitInfo {
    public readonly bool didHit;
    public Vector3 hitPoint;
    public float distance;
    public Vector3 color;

    public HitInfo(bool didHit, Vector3 hitPoint, float distance, Vector3 color) {
        this.didHit = didHit;
        this.hitPoint = hitPoint;
        this.distance = distance;
        this.color = color;
    }
}

internal class MyApplication {
    private readonly Sphere[] _spheres = {
        new(new Vector3(0, 0, 8), 1, Material.Diffuse(new Vector3(1, 0, 0), 1.0f)),
        new(new Vector3(1, -3, 8), 1, Material.Diffuse(new Vector3(0, 1, 0), 0.5f))
    };

    private readonly Light[] _lights = {
        new(new Vector3(-5, 0, 0), 1f),
        // new(new Vector3(-4, 0, 0), 1f),
    };

    private readonly Vector3 _ambientLightColor = VecUtil.FromFloat3(43f / 255f);
    
    private const float NearClip = 0.3f;
    private const float FieldOfView = 60f;

    private static readonly Vector3 CameraPosition = new(0.0f, 0.0f, 0.0f);

    private const float Yaw = 0;
    private const float Pitch = 0;

    // Source: https://gamedev.stackexchange.com/a/190058
    private static readonly Vector3 CameraForwardDirection = new((float)(Math.Cos(Pitch) * Math.Sin(Yaw)),
        (float)-Math.Sin(Pitch), (float)(Math.Cos(Pitch) * Math.Cos(Yaw)));

    private static readonly Vector3 CameraRightDirection = new((float)Math.Cos(Yaw), 0, (float)-Math.Sin(Yaw));
    private static readonly Vector3 CameraUpDirection = Vector3.Cross(CameraForwardDirection, CameraRightDirection);


    public Surface screen;

    public MyApplication(Surface screen) {
        this.screen = screen;
    }

    public void Init() {

    }

    /// <summary>
    /// Returns the intensity of the light at the provided hit point from the provided light.
    /// Does not take distance attenuation into account.
    /// </summary>
    /// <param name="hitPoint">The point to calculate the intensity for</param>
    /// <param name="light">The light to check from</param>
    /// <returns>The intensity of the light if there's an unobstructed path</returns>
    private float IntersectShadowLight(Vector3 hitPoint, Light light) {
        Ray ray = new Ray(hitPoint, light.position);

        bool intersectsObstruction = false;
        foreach(Sphere sphere in _spheres) {
            if (IntersectsSphere(ray, sphere, 0.001f).collision) {
                intersectsObstruction = true;
            }
        }

        if (intersectsObstruction) {
            return 0.0f;
        }

        return light.intensity;
    }

    private TraceResult IntersectsSphere(Ray ray, Sphere sphere, float epsilon = 0.0f) {
        Vector3 offsetOrigin = ray.origin - sphere.center;

        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2 * Vector3.Dot(offsetOrigin, ray.direction);
        float c = Vector3.Dot(offsetOrigin, offsetOrigin) - sphere.radiusSquared;
        
        float d = b * b - 4 * a * c;
        if (d >= 0) {
            float dSqrt = (float) Math.Sqrt(d);
            float a2 = 2 * a;
            
            float distance2 = (-b + dSqrt) / a2;
            float distance1 = (-b - dSqrt) / a2;

            float d1Eps = distance1 - epsilon;
            float d2Eps = distance2 - epsilon;

            float distance = Math.Min(Math.Max(distance1, 0), Math.Max(distance2, 0));
            float distanceEps = Math.Min(Math.Max(d1Eps, 0), Math.Max(d2Eps, 0));
            
            return distanceEps > 0 ? TraceResult.Collide(distance) : TraceResult.Miss();
        }

        return TraceResult.Miss();
    }

    private HitInfo IntersectSphere(Ray ray, Sphere sphere) {
        TraceResult traceResult = IntersectsSphere(ray, sphere);
        
        // At least one intersection
        if (traceResult.collision && traceResult.distance >= 0) {
            // Point on the surfac of the sphere
            Vector3 hitPoint = ray.origin + ray.direction * traceResult.distance;
            Vector3 color = Vector3.Zero;
            
            // Compute the contributions from each light
            foreach (Light light in _lights) {
                float lightIntensity = IntersectShadowLight(hitPoint, light);
                
                // The light reflected depends on the material type
                Vector3 materialLight;
                switch (sphere.material.materialType) {
                    case MaterialType.Diffuse: {
                        // Angle between the surface normal and the light
                        float angle = Vector3.Dot(
                            Vector3.Normalize(hitPoint - sphere.center), // Surface normal
                            Vector3.Normalize(light.position - hitPoint) // Light direction
                        );

                        materialLight =
                            sphere.material.diffuseColor // Kd
                            * Math.Max(0, angle); // max(0, N * L) 
                        
                        break;
                    }
                    case MaterialType.Glossy: {
                        materialLight = Vector3.Zero;
                        break;
                    }
                    default:
                        throw new NotImplementedException();
                }
                
                Vector3 intensityRgb = VecUtil.FromFloat3(lightIntensity);
                float distanceAttenuation = 1 / sphere.radiusSquared;
                
                color +=
                    (intensityRgb * distanceAttenuation * materialLight)
                    .Max(0.0f); // Make sure the color stays positive
            }
            
            // Add ambient light
            color += _ambientLightColor * sphere.material.ambientColor;
            
            return new HitInfo(true, hitPoint, traceResult.distance, color);
        }

        return new HitInfo(false, Vector3.Zero, 0, Vector3.Zero);
    }

    public void Tick() {
        screen.Clear(0);

        float planeHeight = NearClip * (float)Math.Tan(MathHelper.DegreesToRadians(FieldOfView * 0.5f)) * 2;
        float aspectRatio = (float)screen.width / screen.height;
        float planeWidth = planeHeight * aspectRatio;

        var viewParams = new Vector3(planeWidth, planeHeight, NearClip);

        for (int x = 0; x < screen.width; x++) {
            for (int y = 0; y < screen.height; y++) {
                Vector2 vplXy = new Vector2(x, y) / new Vector2(screen.width, screen.height) -
                                new Vector2(0.5f, 0.5f);
                Vector3 viewPointLocal = new Vector3(vplXy.X, vplXy.Y, 1f) * viewParams;

                Vector3 viewPoint = CameraPosition + CameraRightDirection * viewPointLocal.X +
                                    CameraUpDirection * viewPointLocal.Y +
                                    CameraForwardDirection * viewPointLocal.Z;

                var cameraPrimaryRay = new Ray(CameraPosition, Vector3.Normalize(viewPoint - CameraPosition));

                foreach (Sphere sphere in _spheres) {
                    HitInfo hitInfo = IntersectSphere(cameraPrimaryRay, sphere);
                    if (hitInfo.didHit) {
                        SetPixel(new Vector2i(x, y), hitInfo.color);
                    }
                }
            }
        }
    }

    private void SetPixel(Vector2i position, Vector3 color) {
        screen.pixels[position.Y * screen.width + position.X] = ShiftColor(color);
    }

    private int ShiftColor(Vector3 color) {
        var clamped = new Vector3i((int)Math.Floor(color.X * 255), (int)Math.Floor(color.Y * 255),
            (int)Math.Floor(color.Z * 255));
        return ((byte)clamped.X << 16) | ((byte)clamped.Y << 8) | (byte)clamped.Z;
    }
}